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

MSD.DataSource = function (inputData) {
    // All logic for base class constructor is in _init.
}

MSD.DataSource.prototype = {

    // Private fields

    // Init-time values
    _inputData: null,
    _entityCollectionEventHandler: null,

    // Optional query options
    _skip: null,
    _take: null,
    _includeTotalCount: false,

    // State
    _observers: null,
    _entities: null,
    _clientEntities: null,
    _lastRefreshTotalEntityCount: 0,


    // Public methods

    dispose: function () {
        if (this._entityCollectionEventHandler) {
            $([ this._clientEntities ]).unbind("arrayChange", this._entityCollectionEventHandler);
            this._entityCollectionEventHandler = null;
        }
        this._observers = [];
    },

    addObserver: function (observer) {
        if ($.inArray(observer, this._observers) < 0) {
            this._observers.push(observer);
        }
    },

    removeObserver: function (observer) {
        this._observers = $.grep(this._observers, function (element, index) {
            return element !== observer;
        });
    },

    getEntities: function () {
        return this._clientEntities;
    },

    getTotalEntityCount: function () {
        var addedEntityCount = $.grep(this._entities, function (cachedEntity) {
            return cachedEntity.added;
        }).length;
        return this._lastRefreshTotalEntityCount + addedEntityCount;
    },

    getEntityState: function (entity) {
        if (this._getCachedEntityByEntity(entity)) {
            return this._inputData.getEntityState(entity);
        } else {
            return null;
        }
    },

    isPropertyChanged: function (entity, propertyName) {
        if (this._getCachedEntityByEntity(entity)) {
            return this._inputData.isPropertyChanged(entity, propertyName);
        } else {
            throw "Entity no longer cached in data source.";
        }
    },

    getErrors: function () {
        var self = this;
        return $.grep(this._inputData.getErrors(), function (error) {
            return !!self._getCachedEntityByEntity(error.entity);
        });
    },

    getDataContext: function () {
        throw "Unreachable";  // Abstract/pure virtual method.
    },

    getEntityValidationRules: function () {
        return this._inputData.getEntityValidationRules();
    },

    // TODO -- These query set-* methods should be consolidated, passing a "settings" parameter.
    // That way, we can issue a single "needs refresh" event when the client updates the query settings.
    // TODO -- Changing query options should trigger "results stale".
    setSort: function (options) {
        throw "Unreachable";  // Abstract/pure virtual method.
    },

    setFilter: function (filter) {
        throw "Unreachable";  // Abstract/pure virtual method.
    },

    setPaging: function (options) {
        options = options || {};
        this._skip = options.skip;
        this._take = options.take;
        this._includeTotalCount = !!options.includeTotalCount;
    },

    refresh: function (options) {
        throw "Unreachable";  // Abstract/pure virtual method.
    },

    revertChange: function (entity, propertyName) {
        var cachedEntity = this._getCachedEntityByEntity(entity);
        if (cachedEntity) {
            // Revert first, so client receives non-"arrayChange" events ahead of "arrayChange" (due to _purgeEntity)
            // that might signal removal of entities from our result set.
            this._inputData.revertChange(entity, propertyName);
            if (!propertyName && cachedEntity.added) {
                this._purgeEntity(cachedEntity.entity);
            }
        } else {
            throw "Entity no longer cached in data source.";
        }
    },

    revertChanges: function (all) {
        this._inputData.revertChanges(all);

        var uncommittedAddedEntities = $.grep(this._entities, function (cachedEntity) {
            // TODO -- Can we trust added here?  How will we sync this bit with the underlying SDS?
            return cachedEntity.added;
        }),
            self = this;
        $.each(uncommittedAddedEntities, function (index, cachedEntity) {
            self._purgeEntity(cachedEntity.entity);
        });
    },


    // Private methods

    _init: function (inputData, options) {
        this._inputData = inputData;

        var clientEntityCollection;
        if (options && options.entityCollection) {
            if (Object.prototype.toString.call(options.entityCollection) !== "[object Array]") {
                throw "Entity collection must be an array";
            }
            // This is checked in RDS and LDS, since long as AssociatedEntitiesDataSource supplies
            // its own client entities.
            //// else if (options.entityCollection.length !== 0) {
            ////     throw "NYI -- Currently, entity collection must be empty to bind to a data source.";
            //// }

            clientEntityCollection = options.entityCollection;
        }

        this._observers = [];
        this._entities = [];
        this._clientEntities = clientEntityCollection || [];
        var self = this;
        this._clientEntities.deleteEntity = function (entity) {
            // Non-destructive delete.
            self._deleteEntity(entity);
        };

        var eventData = {
            // "first" ensures that our handler is notified ahead of non-"first" so the DataSource can bring
            // itself into a consistent state _before_ any non-DataSource clients are notified.
            first: true,

            // "eventDefault" ensures that we issue our events _after_ all clients have been notified of internal adds.
            eventDefault: function () {
                self._flushDeferredEvents();
            }
        };
        this._entityCollectionEventHandler = function (changeEvent, change) {
            self._handleCollectionChange(changeEvent, change);
        };
        $([ this._clientEntities ]).bind("arrayChange", eventData, this._entityCollectionEventHandler);
    },

    _onPropertyChanged: function (entity, property, newValue) {
        var cachedEntity = this._getCachedEntityByEntity(entity);
        if (cachedEntity) {
            this._raiseChangeEvent(entity, property, newValue);
        }
    },

    _onEntityStateChanged: function (entity, state) {
        // We keep track of added entities here, so they can be removed from this._entities on revertChanges.
        var self = this;
        $.each(this._entities, function (index, cachedEntityToClearAdded) {
            if (cachedEntityToClearAdded.added && self._inputData.getEntityState(cachedEntityToClearAdded.entity) === "Unmodified") {
                cachedEntityToClearAdded.added = false;
            }
        });

        var cachedEntity = this._getCachedEntityByEntity(entity);

        var purgeEntity = state === "Deleted" && cachedEntity &&
            (typeof this !== MSD.LocalDataSource || cachedEntity.added);
        // LocalDataSource shouldn't remove entities w/o an explicit refresh.
        // For RemoteDataSource, this "Deleted" will be the result of a refresh or internal delete.
        // In both cases, it's ok to purge.
        if (purgeEntity) {
            // Deleting a entity that is uncommitted and only on the client.
            // TODO -- Reconsider whether we shouldn't throw here, force clients to revert instead.
            this._purgeEntity(cachedEntity.entity);
        }

        if (cachedEntity) {
            this._raiseEntityStateChangedEvent(entity, state);
        }
    },

    _raiseRefreshStartEvent: function (entities, totalCount) {
        this._raiseEvent("refreshStart");
    },

    _raiseRefreshEvent: function (entities, totalCount) {
        this._raiseEvent("refresh", entities, totalCount);
    },

    _raiseChangeEvent: function (entity, property, newValue) {
        this._raiseEvent("propertyChanged", entity, property, newValue);
    },

    _raiseEntityStateChangedEvent: function (entity, state) {
        this._raiseEvent("entityStateChanged", entity, state);
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

    _getCachedEntityByEntity: function (entity) {
        return $.grep(this._entities, function (cachedEntity) {
            return cachedEntity.entity === entity;
        })[0];
    },

    _handleCollectionChange: function (changeEvent, change) {
        if (changeEvent.isInternalChange) {
            // Self-inflicted change.
            return;
        }

        switch (change.change) {
            case "add":
                // We don't account for change.newIndex here.  Our cache of this._entities is not ordered,
                // as it just track internally added entities.  Likewise, there isn't an intuitive, predictable
                // mapping in terms of position onto our input entity collection.

                var entitiesToAdd = change.newItems;
                if (entitiesToAdd.length > 1) {
                    throw "NYI -- Can only add a single entity to/from an array in one operation.";
                }

                // In EntitySet, we defer entity state events around the addition of this entity.
                // (We do this so that all clients of this._clientEntities are informed of the addition
                // _before_ we issue related RIA-specific events re: the added entity.)
                // We don't have to defer here, as we don't _directly_ issue events from this DataSource
                // with respect to this add.
                var entityToAdd = entitiesToAdd[0];
                this._addEntity(entityToAdd);  // Assumes add to query result and not to collection-typed relationship property.
                break;

            case "remove":
                var index = change.oldIndex,
                    entitiesToDelete = change.oldItems;
                if (entitiesToDelete.length > 1) {
                    throw "NYI -- Can only remove a single entity to/from an array in one operation.";
                }

                // TODO -- Destructive deletes are currently NYI
                //// this._deleteEntity(this.getEntityId(entitiesToDelete[0]));
                throw "NYI -- Cannot apply destructive deletes to a entity collection.  Use 'deleteEntity' for non-destructive delete.";
                break;

            default:
                throw "NYI -- Array operation '" + change.change + "' is not supported.";
        }
    },

    _addEntity: function (entity) {
        this._entities.push({ entity: entity, added: true });
        $.push(this._inputData.getEntities(), entity);
    },

    _deleteEntity: function (entity) {
        if (this._getCachedEntityByEntity(entity)) {
            this._inputData.getEntities().deleteEntity(entity);
        } else {
            throw "Entity no longer cached in data source.";
        }
    },

    _purgeEntity: function (entityToPurge) {
        this._entities = $.grep(this._entities, function (cachedEntity) {
            return cachedEntity.entity !== entityToPurge;
        });

        // Manually trigger the "arrayChanging/Change" events so we can add "isInternalChange" and detect self-inflicted changes.
        var index = $.inArray(entityToPurge, this._clientEntities),
            eventArguments = [ { change: "remove", oldIndex: index, oldItems: [ entityToPurge ] } ];  // TODO -- Can we have reuse jQuery code to develop event arguments for us?
        $([ this._clientEntities ]).trigger("arrayChanging", eventArguments);
        this._clientEntities.splice(index, 1);
        $([ this._clientEntities ]).trigger({ type: "arrayChange", isInternalChange: true }, eventArguments);
    },

    _processFilter: function (filter) {
        var filterProperty = filter.property,
            filterValue = filter.value,
            filterOperator;
        if (!filter.operator) {
            filterOperator = "==";
        } else {
            var operatorStrings = {
                "<": ["<", "islessthan", "lessthan", "less", "lt"],
                "<=": ["<=", "islessthanorequalto", "lessthanequal", "lte"],
                "==": ["==", "isequalto", "equals", "equalto", "equal", "eq"],
                "!=": ["!=", "isnotequalto", "notequals", "notequalto", "notequal", "neq", "not"],
                ">=": [">=", "isgreaterthanorequalto", "greaterthanequal", "gte"],
                ">": [">", "isgreaterthan", "greaterthan", "greater", "gt"]
            },
                lowerOperator = filter.operator.toLowerCase();
            for (var op in operatorStrings) {
                if ($.inArray(lowerOperator, operatorStrings[op]) > -1) {
                    filterOperator = op;
                    break;
                }
            }

            if (!filterOperator) {
                // Assume that the filter operator is a function that we'll translate directly
                // into the GET URL.
                filterOperator = filter.operator;
                // throw "Unrecognized filter operator '" + filter.operator + "'.";
            }
        }

        return {
            filterProperty: filterProperty,
            filterOperator: filterOperator,
            filterValue: filterValue
        };
    },

    _flushDeferredEvents: function () {
        // Will be overridden by derived classes.
    },

    _completeRefresh: function (entities, totalCount, options) {
        // Update our total entity count.  We use this cache to track internally added entities.
        this._lastRefreshTotalEntityCount = totalCount;

        // Update our client entities.
        var changed;
        var oldEntities = this.getEntities();
        if (oldEntities.length !== entities.length) {
            changed = true;
        } else {
            $.each(oldEntities, function (index, entity) {
                if (entity !== entities[index]) {
                    changed = true;
                    return false;
                }
            });
        }

        if (changed) {
            var self = this;
            this._entities = $.map(entities, function (entity) {
                var added = $.grep(self._entities, function (cachedEntity) {
                    return cachedEntity.entity === entity && cachedEntity.added;
                }).length > 0;
                return { entity: entity, added: added };
            });

            var eventArguments = [ { change: "reset" } ];
            $([ this._clientEntities ]).trigger("arrayChanging", eventArguments);
            Array.prototype.splice.apply(this._clientEntities, [0, this._clientEntities.length].concat(entities));
            $([ this._clientEntities ]).trigger({ type: "arrayChange", isInternalChange: true }, eventArguments);
        }

        var newClientEntities = this.getEntities(),
            newTotalCount = this.getTotalEntityCount();
        if (options && options.completed && $.isFunction(options.completed)) {
            options.completed(newClientEntities, newTotalCount);
        }
        this._raiseRefreshEvent(newClientEntities, newTotalCount);
    }
};
