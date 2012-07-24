/// <reference path="EntitySet.js" />
/// <reference path="DomainServiceProxy.js" />

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

MSD.DataContext = function (serviceUrl, bufferChanges) {
    this._serviceUrl = serviceUrl;
    this._changeTracking = !!bufferChanges;
    this._domainServiceProxy = new MSD.DomainServiceProxy(serviceUrl);
    
    this._observers = [];
    this._entitySets = {};
    this._metadata = {};
}

MSD.DataContext.prototype = {

    // Private fields

    // Init-time values
    _serviceUrl: null,
    _changeTracking: null,
    _domainServiceProxy: null,

    // State
    _observers: null,
    _entitySets: null,
    _flushingDeferredEvents: false,
    _metadata: null,

    // Public methods

    dispose: function () {
        // TODO -- Do we want a dispose protocol to unbind our EntitySet "arrayChange" handlers?
    },

    // TODO -- We should align with jQuery binding/events here.
    addObserver: function (observer) {
        if ($.inArray(observer, this._observers) < 0) {
            this._observers.push(observer);
        }
    },

    removeObserver: function (observer) {
        this._observers =
            $.grep(this._observers, function (element, index) {
                return element !== observer;
            });
    },

    getEntitySet: function (entityType) {
        var entitySet = this._entitySets[entityType];
        if (!entitySet) {
            entitySet = this._entitySets[entityType] = new MSD.EntitySet(this, entityType);
        }
        return entitySet;
    },

    getErrors: function () {
        var errors = [];
        $.each(this._entitySets, function (type, entitySet) {
            var spliceArguments = [errors.length, 0].concat(entitySet.getErrors());
            Array.prototype.splice.apply(errors, spliceArguments);
        });
        return errors;
    },

    commitChanges: function () {
        if (!this._changeTracking) {
            throw "Data context must be in change-tracking mode to explicitly commit changes.";
        }

        var editedEntities = [];
        $.each(this._entitySets, function (type, entitySet) {
            var editedEntitiesT = $.map(entitySet.__getEditedEntities(), function (entity) {
                return { entitySet: entitySet, entity: entity };
            });
            var spliceArguments = [editedEntities.length, 0].concat(editedEntitiesT);
            Array.prototype.splice.apply(editedEntities, spliceArguments);
        });

        var self = this;
        this._executeWithDeferredEvents(function (deferredEvents) {
            self._submitChanges(editedEntities, deferredEvents);
        });
    },

    revertChanges: function () {
        var self = this;
        this._executeWithDeferredEvents(function (deferredEvents) {
            $.each(self._entitySets, function (type, entitySet) {
                entitySet.__revertChanges(deferredEvents);
            });
        });
    },

    // TODO -- We have no mechanism to similarly clear data sources.
    //// clear: function () {
    ////     $.each(this._entitySets, function (type, entitySet) {
    ////         entitySet.__clear();
    ////     });
    //// },


    // Internal methods

    __load: function (query) {
        $.each(this._entitySets, function (type, entitySet) {
            if (entitySet.__hasUncommittedEdits()) {
                throw "Load is not allowed while the data source contains uncommitted edits.";
            }
        });

        var self = this,
            success = function (queryResult) {
                var metadata = queryResult.Metadata;
                self._addMetadata(metadata);
                // By convention, we assume metadata for the returned type is the first.
                var entityType = metadata[0].type;

                var entitySet = self.getEntitySet(entityType);

                var loadedEntities = self._executeWithDeferredEvents(function (deferredEvents) {
                    // Load included entities.
                    if (queryResult.IncludedResults) {
                        // Group included entities by type.
                        var includedEntities = {};
                        $.each(queryResult.IncludedResults, function (unused, entity) {
                            var entityType = entity.__type;
                            if (!includedEntities[entityType]) {
                                includedEntities[entityType] = [];
                            }
                            includedEntities[entityType].push(entity);
                        });

                        // Load the included entities into their entity set.
                        for (var includedEntityType in includedEntities) {
                            var includedEntitySet = self.getEntitySet(includedEntityType);
                            includedEntitySet.__loadEntities(includedEntities[includedEntityType], deferredEvents);
                        }
                    }

                    // Load the entities that are the result of this query.
                    var entities = entitySet.__loadEntities(queryResult.RootResults, deferredEvents);
                    return entities;
                });

                // For some reason DomainService sends no TotalCount for some queries when there are
                // no queryResult.RootResults.
                var totalCount = !!query.includeTotalCount && queryResult.TotalCount === undefined ? 0 : queryResult.TotalCount;
                query.success(entitySet, loadedEntities, totalCount);
            },
            getQueryParts = function (queryBase) {
                if (query.filters) {
                    $.each(query.filters, function (index, filter) {
                        queryBase.where(filter.filterProperty, filter.filterValue, filter.filterOperator);
                    });
                }
                if (query.sort) {
                    queryBase.orderBy(query.sort);
                }
                if (query.skip > 0) {
                    queryBase.skip(query.skip);
                }
                if (query.take !== null && query.take !== undefined) {
                    queryBase.take(query.take);
                }
                if (!!query.includeTotalCount) {
                    queryBase.includeTotalCount();
                }

                return queryBase;
            };
        this._domainServiceProxy.query(query.queryName, success, getQueryParts, query.queryParameters);
        // TODO -- Need failure callback here.  How does it affect events/callbacks?
    },

    __commitEntityIfImplicit: function (entitySet, entity, deferredEvents) {
        if (!this._changeTracking) {
            this._submitChanges([{ entitySet: entitySet, entity: entity}], deferredEvents);
        }
    },

    __getMetadata: function (entityType) {
        return this._metadata[entityType];
    },

    __entitySetChanged: function (entityType, deferredEventsCollector) {
        this._distributeEntitySetChanged(entityType, deferredEventsCollector);
    },


    // Private methods

    _raiseCommitSucceededEvent: function () {
        this._raiseEvent("commitSucceeded");
    },

    _raiseEvent: function (eventType) {
        var eventArguments = Array.prototype.slice.call(arguments, 1),  // "arguments" isn't an Array.
            toNotify = this._observers.slice();
        $.each(toNotify, function (index, observer) {
            // TODO -- We should do a setTimeout for each callback, to keep the UI from hanging up.
            if ($.isFunction(observer[eventType])) {
                observer[eventType].apply(null, eventArguments);
            }
        });
    },

    _submitChanges: function (editedEntities, deferredEvents) {
        var edits = $.map(editedEntities, function (editedEntity) {
            return editedEntity.entitySet.__getEntityEdit(editedEntity.entity);
        });

        $.each(edits, function (index, edit) { edit.updateEntityState(deferredEvents); });

        var operations = $.map(edits, function (edit, index) {
            return $.extend({ Id: index.toString() }, edit.operation);
        });

        var self = this;
        $.ajax({
            url: this._serviceUrl + "/JSON/SubmitChanges",
            contentType: "application/json",
            type: "POST",
            data: JSON.stringify({ changeSet: operations }),
            success: function (data) {
                var hasErrors = self._executeWithDeferredEvents(function (deferredEvents) {
                    var submitResult = data.SubmitChangesResult;
                    for (var i = 0; i < submitResult.length; i++) {
                        var edit = edits[i];
                        if (!edit.succeeded(submitResult[i], deferredEvents)) {
                            return true;
                        }
                    }
                    return false;
                });
                if (!hasErrors) {
                    self._raiseCommitSucceededEvent();
                }
            },
            error: function (response) {
                self._executeWithDeferredEvents(function (deferredEvents) {
                    $.each(edits, function (index, edit) {
                        edit.failed(response, deferredEvents);
                    });
                });
            }
        });
    },

    _executeWithDeferredEvents: function (toExecute) {
        this._assertNotFlushingDeferredEvents();

        var deferredEvents = [],
            changedEntitySets = {},
            deferredEventsCollector = {
                deferEvent: function (eventToDefer) {
                    deferredEvents.push(eventToDefer);
                },
                entitySetChanged: function (entityType) {
                    changedEntitySets[entityType] = true;
                }
            },
            result = toExecute(deferredEventsCollector);
        for (var changedEntitySetType in changedEntitySets) {
            this._distributeEntitySetChanged(changedEntitySetType, deferredEventsCollector);
        }

        try {
            this._flushingDeferredEvents = true;
            $.each(deferredEvents, function (index, deferredEvent) {
                deferredEvent();
            });
        } finally {
            this._flushingDeferredEvents = false;
        }

        return result;
    },

    _distributeEntitySetChanged: function (changedEntitySetType, deferredEvents) {
        // Give entity sets an opportunity to invalidate associated entity collections for
        // their entities.
        for (var entityType in this._entitySets) {
            var entitySetToNotify = this._entitySets[entityType];
            entitySetToNotify.__handleEntitySetChanged(changedEntitySetType, deferredEvents);
        }
    },

    _assertNotFlushingDeferredEvents: function () {
        if (this._flushingDeferredEvents) {
            // This catches bugs in our implementation where we try to do event deferral
            // in the process of issuing event callbacks.
            // It would also be triggered when a client tries to make a side-effecting API
            // call while we're flushing our deferred events.
            // TODO -- We should strengthen this further to catch more cases where the client
            // makes side-effecting API calls during event callbacks.  This only catches the
            // offenses during deferred event callbacks.
            throw "Issuing side-effecting operations during event callbacks is not supported.";
        }
    },

    _addMetadata: function (metadata) {
        var self = this;
        $.each(metadata, function (unused, metadataForType) {
            var type = metadataForType.type;
            if (!self._metadata[type]) {
                self._metadata[type] = metadataForType;
            }
        });
    }
};
