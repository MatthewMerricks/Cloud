/// <reference path="DataSource.js" />
/// <reference path="DataContext.js" />
/// <reference path="EntitySet.js" />

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

MSD.RemoteDataSource = function (serviceUrl, queryName, entityType, options) {

    var queryParameters, bufferChanges, dataContext;
    if (options) {
        queryParameters = options.queryParameters;
        bufferChanges = options.bufferChanges;
        dataContext = options.dataContext;

        // Should be in DataSource, but AssociatedEntitiesDataSource supplies non-empty entityCollection.
        if (options.entityCollection && options.entityCollection.length !== 0) {
            throw "NYI -- Currently, entity collection must be empty to bind to a data source.";
        }
    }

    this._queryName = queryName;
    this._queryParameters = queryParameters;

    if (!dataContext) {
        dataContext = new MSD.DataContext(serviceUrl, !!bufferChanges);
        // TODO -- If DS exclusively owns the DC, can we make it non-accumulating?
    }

    this._init(dataContext, entityType, options);
}

MSD.RemoteDataSource.prototype = $.extend({}, new MSD.DataSource(), {

    // Private fields

    // Init-time values
    _queryName: null,
    _queryParameters: null,
    _dataContext: null,
    _dataContextObserver: null,
    _entitySet: null,
    _entitySetObserver: null,

    // Optional query options
    _sort: null,
    _filters: null,

    // Public methods

    dispose: function () {
        MSD.DataSource.prototype.dispose.apply(this);
        if (this._dataContextObserver) {
            this._dataContext.removeObserver(this._dataContextObserver);
            this._dataContextObserver = null;
        }
        if (this._entitySetObserver && this._entitySet) {
            this._entitySet.removeObserver(this._entitySetObserver);
            this._entitySetObserver = null;
        }
    },

    getDataContext: function () {
        return this._dataContext;
    },

    setSort: function (sort) {
        // TODO -- Validate sort specification?
        this._sort = sort;
    },

    setFilter: function (filter) {
        if (!filter) {
            this._filters = null;  // Passing null/undefined means clear filter.
        } else if (Object.prototype.toString.call(filter) === "[object Array]") {
            var self = this;
            this._filters = $.map(filter, function (filterPart) {
                return self._processFilter(filterPart);
            });
        } else {
            this._filters = [ this._processFilter(filter) ];
        }
    },

    // TODO -- We should do a single setTimeout here instead, just in case N clients request a refresh
    // in response to callbacks.
    refresh: function (options) {
        this._raiseRefreshStartEvent();

        var self = this;
        this._dataContext.__load({
            // TODO -- Combine these into an object at construction time.
            queryName: this._queryName,
            queryParameters: this._queryParameters,

            filters: this._filters,
            sort: this._sort,
            skip: this._skip,
            take: this._take,
            includeTotalCount: this._includeTotalCount,

            success: function (entitySet, entities, totalCount) {
                self._bindToEntitySet(entitySet);
                // TODO -- This means that we can't do CUD on this data source until we've refreshed it once.
                // Allow client to pass the entityType, which would allow us to _not_ require a refresh to prime.

                self._completeRefresh(entities, totalCount, options);
            }

            // TODO -- Need failure callback here.  How does it affect events/callbacks?
        });
    },

    commitChanges: function () {
        this._dataContext.commitChanges();
    },


    // Private methods

    // Factored this way only for use by AssociatedEntitiesDataSource.
    _init: function (dataContext, entityType, options) {
        this._dataContext = dataContext;
        var self = this;
        this._dataContextObserver = {
            commitSucceeded: function () { self._onCommitSucceeded(); }
        };
        this._dataContext.addObserver(this._dataContextObserver);

        this._entitySetObserver = {
            propertyChanged: function (entity, property, newValue) { self._onPropertyChanged(entity, property, newValue); },
            entityStateChanged: function (entity, state) { self._onEntityStateChanged(entity, state); }
        };
        if (entityType) {
            // If clients supply a entity type, then they'll be able to do entity creation
            // without loading the underlying data source first.
            ////TODO: fix this
            //var entitySet = dataContext.getEntitySet(entityType);
            //this._bindToEntitySet(entitySet);
        }

        var inputData = {
            getEntities: function () { return self._entitySet.getEntities(); },
            getEntityState: function (entity) { return self._entitySet.getEntityState(entity); },
            getEntityValidationRules: function () {
                return {
                    rules: self._dataContext.__getMetadata(self._getEntityType()).rules,
                    messages: self._dataContext.__getMetadata(self._getEntityType()).messages
                };
            },
            isPropertyChanged: function (entity, propertyName) { return self._entitySet.isPropertyChanged(entity, propertyName); },
            getErrors: function () { return self._dataContext.getErrors(); },
            revertChange: function (entity, propertyName) { return self._entitySet.revertChange(entity, propertyName); },
            revertChanges: function (all) {  // TODO -- Not "all" is weird.  Remove.
                if (!!all) {
                    self._dataContext.revertChanges();
                } else {
                    self._entitySet.revertChanges();
                }
            }
        };
        MSD.DataSource.prototype._init.call(this, inputData, options);
    },

    _onCommitSucceeded: function () {
        this._raiseCommitSucceededEvent();
    },

    _raiseCommitSucceededEvent: function () {
        // There is almost zero value to this callback.  Clients can, as easily, react to the "commit succeeded" callback.  
        // Further, notifying the client of pushed notifications requiring a refresh should be handled in a first class way
        // and shouldn't be confused with "results stale" (which we're reserving for the LDS to notify that the local query 
        // would return a different result).
        // Often, clients will want to know the difference between a commit of internal versus external changes anyways.
        //// this._raiseResultsStaleEvent();

        this._raiseEvent("commitSucceeded");
    },

    _bindToEntitySet: function (entitySet) {
        if (entitySet !== this._entitySet) {
            if (this._entitySet) {
                this._entitySet.removeObserver(this._entitySetObserver);
            }
            this._entitySet = entitySet;
            this._entitySet.addObserver(this._entitySetObserver);
        }
    },

    _getEntityType: function () {
        return this._entitySet.__getEntityType();
    }
});
