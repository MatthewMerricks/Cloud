/// <reference path="DataSource.js" />
/// <dictionary target='comment'>recompute</dictionary>

// Copyright (c) Microsoft.  All rights reserved.
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation
// files (the "Software"), to deal  in the Software without restriction, including without limitation the rights  to use, copy,
// modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR  IMPLIED, INCLUDING BUT NOT LIMITED TO THE
// WARRANTIES OF MERCHANTABILITY,  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
// COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE,
// ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

//TODO Encapsulate the namespace creation into a helper function (maybe named DefineNs?)
var Microsoft = Microsoft || [];
Microsoft.ServiceModel = Microsoft.ServiceModel || [];
Microsoft.ServiceModel.DomainServices = Microsoft.ServiceModel.DomainServices || [];

var MSD = Microsoft.ServiceModel.DomainServices;

MSD.LocalDataSource = function (dataSource, options) {
    /// <param name="dataSource" type="DataSource"></param>

    this._dataSource = dataSource;

    // Should be in DataSource, but AssociatedEntitiesDataSource supplies non-empty entityCollection.
    if (options && options.entityCollection && options.entityCollection.length !== 0) {
        throw "NYI -- Currently, entity collection must be empty to bind to a data source.";
    }

    var self = this;
    this._observer = {
        refreshStart: function () { self._onRefreshStart(); },
        refresh: function (entities, totalCount) { self._onRefresh(entities); },
        propertyChanged: function (entity, property, newValue) { self._onPropertyChanged(entity, property, newValue); },
        entityStateChanged: function (entity, state) { self._onEntityStateChanged(entity, state); },
        commitSucceeded: function () { self._onCommitSucceeded(); },
        resultsStale: function () { self._onResultsStale(); }
    };
    dataSource.addObserver(this._observer);

    this._innerArrayChangeHandler = function (changeEvent, change) {
        self._handleInnerCollectionChange(changeEvent, change);
    };
    $([ dataSource.getEntities() ]).bind("arrayChange", { first: true }, this._innerArrayChangeHandler);
    // "first" ensures that our handler is notified ahead of non-"first" so the DataSource can bring
    // itself into a consistent state _before_ any non-DataSource clients are notified.

    var inputData = {
        getEntities: function () { return self._dataSource.getEntities(); },
        getEntityState: function (entity) { return self._dataSource.getEntityState(entity); },
        getEntityValidationRules: function () { return self._dataSource.getEntityValidationRules(); },
        isPropertyChanged: function (entity, propertyName) { return self._dataSource.isPropertyChanged(entity, propertyName); },
        getErrors: function () { return self._dataSource.getErrors(); },
        revertChange: function (entity, propertyName) { return self._dataSource.revertChange(entity, propertyName); },
        revertChanges: function (all) { return self._dataSource.revertChanges(all); }
    };
    MSD.DataSource.prototype._init.call(this, inputData, options);
}

MSD.LocalDataSource.prototype = $.extend({}, new MSD.DataSource(), {

    // Private fields

    // Init-time values
    _dataSource: null,
    _observer: null,
    _innerArrayChangeHandler: null,

    // Optional query options
    _sort: null,
    _filter: null,

    // State
    _refreshAllInProgress: false,
    _resultsStale: true,
    _deferredResultsStaleEvent: false,


    // Public methods

    dispose: function () {
        MSD.DataSource.prototype.dispose.apply(this);
        if (this._observer) {
            this._dataSource.removeObserver(this._observer);
            this._observer = null;
        }
        if (this._innerArrayChangeHandler) {
            $([ this._dataSource.getEntities() ]).unbind("arrayChange", this._innerArrayChangeHandler);
            this._innerArrayChangeHandler = null;
        }
    },

    getDataContext: function () {
        return this._dataSource.getDataContext();
    },

    setSort: function (sort) {
        // TODO -- Should really raise "results stale" event when changed (throughout).
        // TODO -- Validate sort specification?
        this._sort = sort;
    },

    setFilter: function (filter) {
        // TODO -- Should really raise "results stale" event when changed (throughout).
        this._filter = (!filter || $.isFunction(filter)) ? filter : this._createFilterFunction(filter);
    },

    refresh: function (options) {
        this._raiseRefreshStartEvent();

        var self = this;

        if (options && !!options.all) {
            // N.B.  "all" is a helper, in the sense that it saves a client from doing a serverDataSource.refresh and then, 
            // in response to serverDataSource.onRefresh, calling localDataSource.refresh.  Also, it allows the app to listen
            // on refreshStart/refresh events from this LDS alone (and not the inner SDS as well).
            this._refreshAllInProgress = true;
            this._dataSource.refresh({
                all: true,
                completed: function (entities) {
                    completeRefresh(entities);
                    self._refreshAllInProgress = false;
                }
            });
        } else {
            // We do this refresh asynchronously so that, if this refresh was called during a callback,
            // the app receives remaining callbacks first, before the new batch of callbacks with respect to this refresh.
            // TODO -- We should only refresh once in response to N>1 "refresh" calls.
            setTimeout(function () { completeRefresh(self._dataSource.getEntities()); });
        }

        function completeRefresh(entities) {
            self._resultsStale = false;

            var results = self._applyQuery(entities);
            self._completeRefresh(results.entities, results.totalCount, options);
        };
    },

    areResultsStale: function () {
        return this._resultsStale;
    },


    // Private methods

    _onRefreshStart: function () {
        // Don't translate this directly to the client.  Only client-inflicted refreshes should event to the client.
        //// this._raiseRefreshStartEvent();
    },

    _onRefresh: function (entities) {
        if (this._refreshAllInProgress) {
            // We don't want to event "results stale" due to a "refresh all".
            // Rather, we want to issue "refresh completed".
            return;
        }

        if (!this._resultsStale) {
            // TODO -- We could consider a "inner data source refreshed" callback 
            // instead, to save the app the cost of diff'ing here.  Many apps will
            // just explicitly refresh in response to "results stale" anyways.

            var resultsStale = false,
                results = this._applyQuery(entities);

            if (this.totalCount !== results.totalCount) {
                resultsStale = true;
            } else {
                var oldEntities = this.getEntities(),
                    newEntities = results.entities;

                if (oldEntities.length !== newEntities.length) {
                    resultsStale = true;
                } else {
                    for (var i = 0; i < oldEntities.length; i++) {
                        // Reference comparison is enough here.  "property changed" catches deeper causes of "results stale".
                        if (oldEntities[i] !== newEntities[i]) {
                            resultsStale = true;
                            break;
                        }
                    }
                }
            }

            if (resultsStale) {
                // Don't recompute and translate into "on refresh" event.  That would violate the principle
                // of having only direct refresh or add/delete calls change the result set membership.
                this._setResultsStale();
            }
        }
    },

    _normalizePropertyValue: function (entity, property) {
        // TODO -- Should do this based on metadata and return default value of the correct scalar type.
        return entity[property] || "";
    },

    _onPropertyChanged: function (entity, property, newValue) {
        MSD.DataSource.prototype._onPropertyChanged.apply(this, arguments);

        if (this._refreshAllInProgress) {
            // We don't want to event "results stale" due to a "refresh all".
            // Rather, we want to issue "refresh completed".
            return;
        }

        if (!this._resultsStale) {
            var resultsStale = false;
            if (this._filter && !this._filter(entity)) {
                resultsStale = true;
            }
            if (this._getCachedEntityByEntity(entity) && this._sort) {
                if ($.isFunction(this._sort)) {
                    resultsStale = true;
                } else if (Object.prototype.toString.call(this._sort) === "[object Array]") {
                    resultsStale = $.grep(this._sort, function (sortPart) {
                        return sortPart.property === property;
                    }).length > 0;
                } else {
                    resultsStale = this._sort.property === property;
                }
            }

            if (resultsStale) {
                // No need to defer events here for internal/client updates.
                // This will already have been done by the EntitySet or LocalDataSource
                // raising "property changed".
                this._setResultsStale();
            }
        }
    },

    _onCommitSucceeded: function () {
        // "Commit succeeded" should probably only be sourced from server data source.
        // The only benefit would be for an LDS chain to be treated just as a lone SDS.
        //// this._raiseCommitSucceededEvent();
    },

    _onResultsStale: function () {
        // Clients need to listen on their inner data source directly for indications that
        // it needs to be refreshed.
        // Since server data sources won't know whether a given update invalidates their cached
        // result, any "results stale" signal from server data source will be too pessimistic.
        // Further, a commit of internal versus external changes could both plausibly trigger some
        // "results stale" from the inner data source, and it's more likely that the app wants
        // to know the difference here.
        //// this._raiseResultsStaleEvent();
    },

    _raiseResultsStaleEvent: function () {
        this._deferredResultsStaleEvent = false;
        this._raiseEvent("resultsStale");
    },

    _createFilterFunction: function (filter) {
        var self = this;

        if (Object.prototype.toString.call(filter) === "[object Array]") {
            var comparisonFunctions = $.map(filter, function (filterPart) { 
                return createFunction(filterPart);
            });
            return function (entity) {
                for (var i = 0; i < comparisonFunctions.length; i++) {
                    if (!comparisonFunctions[i](entity)) {
                        return false;
                    }
                }
                return true;
            };
        } else {
            return createFunction(filter);
        }

        function createFunction (filterPart) {
            var processedFilter = self._processFilter(filterPart),
                filterProperty = processedFilter.filterProperty,
                filterOperator = processedFilter.filterOperator,
                filterValue = processedFilter.filterValue;

            var comparer;
            switch (filterOperator) {
                case "<": comparer = function (propertyValue) { return propertyValue < filterValue; }; break;
                case "<=": comparer = function (propertyValue) { return propertyValue <= filterValue; }; break;
                case "==": comparer = function (propertyValue) { return propertyValue == filterValue; }; break;
                case "!=": comparer = function (propertyValue) { return propertyValue != filterValue; }; break;
                case ">=": comparer = function (propertyValue) { return propertyValue >= filterValue; }; break;
                case ">": comparer = function (propertyValue) { return propertyValue > filterValue; }; break;
                case "Contains": comparer = function (propertyValue) { return propertyValue.indexOf(filterValue) >= 0; }; break;
                default: throw "Unrecognized filter operator.";
            };
     
            return function (entity) { 
                // Can't trust added entities, for instance, to have all required property values.
                var propertyValue = self._normalizePropertyValue(entity, filterProperty);
                return comparer(propertyValue);
            };
        };
    },

    _getSortFunction: function () {
        var self = this;
        if (!this._sort) {
            return null;
        } else if ($.isFunction(this._sort)) {
            return this._sort;
        } else if (Object.prototype.toString.call(this._sort) === "[object Array]") {
            var sortFunction;
            $.each(this._sort, function (unused, sortPart) {
                var sortPartFunction = getSortPartFunction(sortPart);
                if (!sortFunction) {
                    sortFunction = sortPartFunction;
                } else {
                    sortFunction = function (sortPartFunction1, sortPartFunction2) {
                        return function (entity1, entity2) {
                            var result = sortPartFunction1(entity1, entity2);
                            return result === 0 ? sortPartFunction2(entity1, entity2) : result;
                        };
                    }(sortFunction, sortPartFunction);
                }
            });
            return sortFunction;
        } else {
            return getSortPartFunction (this._sort);
        }

        function getSortPartFunction (sortPart) {
            return function (entity1, entity2) {
                var isAscending = (sortPart.direction || "asc").toLowerCase().indexOf("asc") === 0,
                    propertyValue1 = self._normalizePropertyValue(entity1, sortPart.property),
                    propertyValue2 = self._normalizePropertyValue(entity2, sortPart.property);
                if (propertyValue1 == propertyValue2) {
                    return 0;
                } else if (propertyValue1 > propertyValue2) {
                    return isAscending ? 1 : -1;
                } else {
                    return isAscending ? -1 : 1;
                }
            }
        };
    },

    _applyQuery: function (entities) {
        var self = this;

        var filteredEntities;
        if (this._filter) {
            filteredEntities = $.grep(entities, function (entity, index) { 
                return self._filter(entity);
            });
        } else {
            filteredEntities = entities;
        }

        var sortFunction = this._getSortFunction(),
            sortedEntities;
        if (sortFunction) {
            sortedEntities = filteredEntities.sort(sortFunction);
        } else {
            sortedEntities = filteredEntities;
        }

        var skip = this._skip || 0,
            pagedEntities = sortedEntities.slice(skip);
        if (this._take) {
            pagedEntities = pagedEntities.slice(0, this._take);
        }
        var totalCount = this._includeTotalCount ? sortedEntities.length : undefined;

        return { entities: pagedEntities, totalCount: totalCount };
    },

    _handleInnerCollectionChange: function (changeEvent, change) {
        if (this._refreshAllInProgress) {
            // We don't want to event "results stale" due to a "refresh all".
            // Rather, we want to issue "refresh completed".
            return;
        }

        if (!this._resultsStale) {
            // See if the state change should cause us to raise the "stale query result" event.
            var self = this,
                resultsStale = false,
                anyEntitiesInCache = function (entities) {
                    return $.grep(entities, function (entity) { 
                        return !!self._getCachedEntityByEntity(entity); 
                    }).length > 0;
                };

            switch (change.change) {
                case "add":
                    var addedEntities = change.newItems;
                    if (addedEntities.length > 0) {
                        var filter = this._filter || function (entity) { return true; },
                            anyExternallyAddedEntitiesMatchFilter = $.grep(addedEntities, function (entity) {
                                return filter(entity) && 
                                    $.grep(self._entities, function (cachedEntity) {
                                        return cachedEntity.entity === entity && cachedEntity.added;
                                    }).length === 0;
                            }).length > 0;
                        if (anyExternallyAddedEntitiesMatchFilter) {
                            resultsStale = true;
                        }
                    }
                    break;

                case "remove":
                    if (anyEntitiesInCache(change.oldItems)) {
                        resultsStale = true;
                    }
                    break;

                case "reset":
                    resultsStale = true;
                    break;

                case "move":
                    if (!this._sort && anyEntitiesInCache(change.oldItems)) {
                        resultsStale = true;
                    }
                    break;

                default:
                    throw "Unknown array operation '" + change.change + "'.";
            }

            if (resultsStale) {
                // No need to defer the "results stale" event here, as the intent of this function
                // is to weed out self-inflicted collection changes that this data source merely 
                // forwarded to the input entity collection.
                this._setResultsStale();
            }
        }
    },

    _setResultsStale: function (deferEvent) {
        /// <param name="deferEvent" optional="true"></param>

        this._resultsStale = true;

        if (deferEvent) {
            this._deferredResultsStaleEvent = true;
        } else {
            this._raiseResultsStaleEvent();
        }
    },

    _addEntity: function (entity) {
        MSD.DataSource.prototype._addEntity.apply(this, arguments);
        
        if (this._filter && !this._filter(entity)) {
            this._setResultsStale();
        }
    },

    _flushDeferredEvents: function () {
        if (this._deferredResultsStaleEvent) {
            this._raiseResultsStaleEvent();
        }
    }
});
