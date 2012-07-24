/// <reference path="AssociatedEntitiesDataSource.js" />
/// <dictionary target='comment'>enqueue</dictionary>

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

MSD.EntitySet = function (dataContext, entityType) {

    this._dataContext = dataContext;
    this._entityType = entityType;
    var metadata = dataContext.__getMetadata(entityType);
    this._idProperty = metadata.key[0];  // TODO -- Generalize to N fields.

    this._observers = [];
    this._updatedEntities = [];
    this._originalEntities = [];
    this._entityStates = {};
    this._addedEntities = [];
    this._changeHandlers = {};
    this._errors = [];
    this._clientEntities = [];
    this._deferredEvents = [];
    this._childEntitiesCollections = {};
    var self = this,
        eventData = {
            // "first" ensures that our handler is notified ahead of non-"first" so the DataSource can bring
            // itself into a consistent state _before_ any non-DataSource clients are notified.
            first: true,

            // "eventDefault" ensures that we issue our events _after_ all clients have been notified of internal adds.
            eventDefault: function () {
                self._flushDeferredEventsFromAddOrUpdate();
            }
        };
    $([ this._clientEntities ]).bind("arrayChange", eventData, function (changeEvent, change) {
        self._handleCollectionChange(changeEvent, change);
    });
    // "first" ensures that our handler is notified ahead of non-"first" so the DataSource can bring
    // itself into a consistent state _before_ any non-DataSource clients are notified.
    this._clientEntities.deleteEntity = function (entity) {
        // Non-destructive delete.
        self._deleteEntity(entity);
    };
}

MSD.EntitySet.prototype = {

    // Private fields

    // Init-time values
    _dataContext: null,
    _entityType: null,
    _idProperty: null,

    // State
    _observers: null,
    _updatedEntities: null,
    _originalEntities: null,
    _clientEntities: null,
    _entityStates: null,
    _addedEntities: null,
    _changeHandlers: null,
    _errors: null,
    _deferredEvents: null,
    _flushingDeferredEvents: false,
    _childEntitiesCollections: null,


    // Public methods

    dispose: function () {
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

    getEntities: function () {
        return this._clientEntities;
    },

    getEntityState: function (entity) {
        var id = this._getEntityId(entity);
        if (id) {
            return this._entityStates[id];
        }

        return null;
    },

    isPropertyChanged: function (entity, propertyName) {
        var id = this._getEntityId(entity);
        switch (this._entityStates[id]) {
            case "ClientUpdated":
            case "ServerUpdating":
                var index = this._getEntityIndexFromId(id);
                return this._originalEntities[index][propertyName] !== this._updatedEntities[index][propertyName];
                // TODO -- Only works for scalar-typed property values.
                // TODO -- Check if propertyName should exist on entity according to metadata?
                // TODO -- Extend support to parent entity properties.

            case undefined:
            case "Deleted":
                throw "Entity no longer cached in data source.";

            default:
                return false;
        }
    },

    getErrors: function () {
        return this._errors;
    },

    revertChange: function (entity, propertyName) {
        var id = this._getEntityId(entity);

        var state = this._entityStates[id];
        if (!state || state === "Deleted") {
            throw "Entity no longer cached in data source.";
        }

        var self = this;
        this._executeWithDeferredEvents(function (deferredEvents) {
            if (!propertyName) {
                if (state === "ClientDeleted" || state === "ClientUpdated") {
                    self._revertToOriginalEntity(id, deferredEvents);
                    self._errors = $.grep(self._errors, function (unused, error) { return error.entity !== entity; });
                    self._updateEntityState(id, "Unmodified", deferredEvents);
                    // TODO -- Might we sniff the entity and revert to "ClientUpdated" from "ClientDeleted" for 
                    // a entity where a property has been updated then the entity deleted?  Confusing?
                } else if (state === "ClientAdded") {
                    self._purgeUncommittedAddedEntity(self._getAddedEntityFromId(id), true, deferredEvents);
                } else {
                    throw "Entity changes cannot be reverted for entity in state '" + state + "'.";
                }
            } else {
                if (state !== "ClientUpdated") {
                    throw "Property change cannot be reverted for entity in state '" + state + "'.";
                } else {
                    var index = self._getEntityIndexFromId(id);
                    $(self._updatedEntities[index]).setField(propertyName, self._originalEntities[index][propertyName]);
                    deferredEvents.entitySetChanged(self._entityType);
                    // TODO -- Only works for scalar-typed property values.
                    // TODO -- Check if propertyName should exist on entity according to metadata?
                    // TODO -- Should we consider reverting from "ClientUpdated" to "Unmodified" for the last such change?
                    // TODO -- Should we reason over errors and GC those pertaining to propertyName here?
                    // TODO -- Extend support to parent entity properties.
                }
            }
        });
    },

    revertChanges: function () {
        var self = this;
        this._executeWithDeferredEvents(function (deferredEvents) {
            self.__revertChanges(deferredEvents);
        });
    },


    // Internal methods

    __hasUncommittedEdits: function () {
        var hasUncommittedEdits = false;
        $.each(this._entityStates, function (key, value) {
            if (value !== "Unmodified") {
                hasUncommittedEdits = true;
            }
        });
        return hasUncommittedEdits;
    },

    __loadEntities: function (entities, deferredEvents) {
        // For each entity, either merge it with a cached entity or add it to the cache.
        var self = this,
            mergedLoadedEntities = [],
            entitiesNewToEntitySet = [];
        $.each(entities, function (unused, entity) {
            var updatedEntity,
                serverId = self._getServerEntityId(entity),
                index = self._getEntityIndexFromServerId(serverId);
            if (index >= 0) {
                // We have this entity cached locally.  Update both the updated and original copies of this
                // entity to reflect property values on "entity".

                // From "entity", overwrite unmodified property values on our updated copy of this entity.
                var oldOriginalEntity = self._originalEntities[index];
                updatedEntity = self._updatedEntities[index];
                self._mergeOntoUpdatedEntity(updatedEntity, entity, oldOriginalEntity, deferredEvents);

                // "entity" becomes the original copy of this cached entity.
                self._originalEntities[index] = entity;
            } else {
                updatedEntity = $.extend({}, entity);  // TODO -- Only works for scalar-typed property values.

                self._addAssociationProperties(updatedEntity);

                var id = serverId.toString();  // Ok, since this is a new entity.
                self._wirePropertyChanged(updatedEntity, id);
                self._entityStates[id] = "Unmodified";

                self._updatedEntities.push(updatedEntity);
                self._originalEntities.push(entity);

                entitiesNewToEntitySet.push(updatedEntity);
            }

            mergedLoadedEntities.push(updatedEntity);
        });

        var newClientEntities = function () {
            // Of the entities in addedEntities, return all added entities not already committed to the server.
            // Entities are kept in addedEntities post-commit in order to maintain their synthetic id's as the
            // id known to clients.
            var addedEntities = $.map(self._addedEntities, function (addedEntity) { return addedEntity.entity; });
            addedEntities = $.grep(addedEntities, function (addedEntity) { return $.inArray(addedEntity, self._updatedEntities) < 0; });

            // Added entities will show up at the end of getEntities.  Clients need to commit/refresh to reapply any ordering.
            return self._updatedEntities.concat(addedEntities);
        } ();

        Array.prototype.splice.apply(this._clientEntities, [0, this._clientEntities.length].concat(newClientEntities));

        if (entitiesNewToEntitySet.length > 0) {
            deferredEvents.deferEvent(function () {
                // Don't trigger a 'reset' here.  What would RemoteDataSources do with such an event?
                // They only have a subset of our entities as the entities they show their clients.  They could
                // only reapply their remote query in response to "reset".
                var eventArguments = [{
                    change: "add",
                    newIndex: $.inArray(entitiesNewToEntitySet[0], newClientEntities),
                    newItems: entitiesNewToEntitySet
                }];
                // TODO -- We buffer events in order to establish consistent caches before issuing callbacks.
                // I don't know what to do with "arrayChanging" as long as this is the case.
                //// $([ self._clientEntities ]).trigger("arrayChanging", eventArguments);
                $([self._clientEntities]).trigger({ type: "arrayChange", isInternalChange: true }, eventArguments);
            });
        }

        return mergedLoadedEntities;
    },

    __getEditedEntities: function () {
        var self = this,
            entities = [];
        $.each(this._entityStates, function (id, state) {
            if (state.indexOf("Client") === 0) {
                entities.push(self._getEntityFromId(id));
            }
        });

        return entities;
    },

    __getEntityEdit: function (entity) {
        // TODO -- Throughout here, we should consult schema and strip off fields that aren't 
        // compliant (like jQuery's __events__ field).

        var id = this._getEntityId(entity),
            self = this,
            submittingState,
            operation,
            index = this._getEntityIndexFromId(id),
            addEntityType = function (entityToExtend) {
                return $.extend({ "__type": self._entityType }, entityToExtend);
            };
        switch (this._entityStates[id]) {
            case "ClientUpdated":
                submittingState = "ServerUpdating";
                operation = {
                    Operation: 3,
                    Entity: addEntityType(this._updatedEntities[index]),
                    OriginalEntity: addEntityType(this._originalEntities[index])
                };
                break;

            case "ClientAdded":
                submittingState = "ServerAdding";
                var addedEntity = this._getAddedEntityFromId(id);
                operation = {
                    Operation: 2,
                    Entity: addEntityType(addedEntity.entity)
                };
                break;

            case "ClientDeleted":
                submittingState = "ServerDeleting";
                var addedEntityBeingDeleted = this._getAddedEntityFromId(id),
                    serverId = addedEntityBeingDeleted
                        ? addedEntityBeingDeleted.serverId
                        : this._getServerEntityId(this._originalEntities[index]),
                    key = {};
                key[this._idProperty] = serverId;
                operation = {
                    Operation: 4,
                    Entity: addEntityType(key)
                };
                // TODO -- Do we allow for concurrency guards here?
                break;

            default:
                throw "Unrecognized entity state.";
        }

        var edit = {
            updateEntityState: function (deferredEvents) {
                self._updateEntityState(id, submittingState, deferredEvents);
            },
            operation: operation,
            succeeded: function (result, deferredEvents) {
                return self._handleSubmitSucceeded(id, operation, result, deferredEvents);
            },
            failed: function (response, deferredEvents) {
                self._handleSubmitFailed(id, operation, response, deferredEvents);
            }
        };
        return edit;
    },

    __revertChanges: function (deferredEvents) {
        var synchronizing;
        $.each(this._entityStates, function (unused, state) {
            if (state.indexOf("Server") === 0) {
                synchronizing = true;
                return false;
            }
        });
        if (synchronizing) {
            throw "Can't revert changes while a commit is in progress.";
        }

        var self = this;
        var uncommittedAddedEntities = $.grep(this._addedEntities, function (addedEntity) {
            return addedEntity.entity && $.inArray(addedEntity.entity, self._updatedEntities) < 0;
        });

        // Remove uncommitted added entities.
        if (uncommittedAddedEntities.length > 0) {
            this._addedEntities = $.grep(this._addedEntities, function (addedEntity) {
                return $.inArray(addedEntity, uncommittedAddedEntities) < 0;
            });
            $.each(uncommittedAddedEntities, function (index, addedEntity) {
                self._purgeUncommittedAddedEntity(addedEntity, true, deferredEvents);
            });
        }

        this._errors = [];

        // Revert "Client"-* entity states to "Unmodified" and discard changes to updatedEntities.
        for (var id in this._entityStates) {
            if (this._entityStates[id].indexOf("Client") === 0) {
                this._revertToOriginalEntity(id, deferredEvents);
                this._updateEntityState(id, "Unmodified", deferredEvents);
            }
        }
    },

    __handleEntitySetChanged: function (entityType, deferredEvents) {
        var metadata = this._dataContext.__getMetadata(this._entityType);
        if (!metadata) {
            return;
        }

        // Invalidate our child entities collections, in response to some change
        // to the target entity set.
        for (var id in this._childEntitiesCollections) {
            var childEntitiesCollections = this._childEntitiesCollections[id];
            var entity = this._getEntityFromId(id);
            for (var fieldName in childEntitiesCollections) {
                var associationMetadata = metadata.fields[fieldName];
                if (associationMetadata.type === entityType) {
                    var childEntitiesCollection = childEntitiesCollections[fieldName];
                    var newChildEntities = this._computeAssociatedEntities(entity, associationMetadata);

                    // Perform adds/removes on childEntitiesCollection to have it reflect the same membership
                    // as newChildEntities.  Issue change events for the adds/removes.
                    // Don't try to preserve ordering between childEntitiesCollection and newChildEntities,
                    // so we don't, for instance, issue "move" events while we're in the midst of a client-issued
                    // add to a child entity collection.
                    // TODO -- Assert that we have only a single add or remove here.  My indices for events will
                    // be off for multiple adds/removes.
                    var addedEntities = $.grep(newChildEntities, function (childEntity) {
                        return $.inArray(childEntity, childEntitiesCollection) < 0;
                    });
                    $.each(addedEntities, function (unused, addedEntity) {
                        childEntitiesCollection.push(addedEntity);

                        var indexAdd = childEntitiesCollection.length;
                        var childEntitiesCollectionForEvent = childEntitiesCollection;
                        var addedEntityForEvent = addedEntity;
                        deferredEvents.deferEvent(function () {
                            var eventArguments = [{
                                change: "add",
                                newIndex: indexAdd,
                                newItems: [addedEntityForEvent]
                            }];
                            // TODO -- We buffer events in order to establish consistent caches before issuing callbacks.
                            // I don't know what to do with "arrayChanging" as long as this is the case.
                            //// $([ childEntitiesCollection ]).trigger("arrayChanging", eventArguments);
                            $([childEntitiesCollectionForEvent]).trigger({ type: "arrayChange", isInternalChange: true }, eventArguments);
                        });
                    });

                    var removedEntities = $.grep(childEntitiesCollection, function (childEntity) {
                        return $.inArray(childEntity, newChildEntities) < 0;
                    });
                    $.each(removedEntities, function (unused, removedEntity) {
                        var indexRemove = $.inArray(removedEntity, childEntitiesCollection);
                        Array.prototype.splice.call(childEntitiesCollection, indexRemove, 1);

                        var childEntitiesCollectionForEvent = childEntitiesCollection;
                        var removedEntityForEvent = removedEntity;
                        deferredEvents.deferEvent(function () {
                            var eventArguments = [{
                                change: "remove",
                                oldIndex: indexRemove,
                                oldItems: [removedEntityForEvent]
                            }];
                            // TODO -- We buffer events in order to establish consistent caches before issuing callbacks.
                            // I don't know what to do with "arrayChanging" as long as this is the case.
                            //// $([ childEntitiesCollection ]).trigger("arrayChanging", eventArguments);
                            $([childEntitiesCollectionForEvent]).trigger({ type: "arrayChange", isInternalChange: true }, eventArguments);
                        });
                    });

                    if (addedEntities.length > 0 || removedEntities.length > 0) {
                        // TODO -- Explicitly kick the data source ascribed to childEntitiesCollection to have it
                        // sync its internal cache (which we don't actually need) to childEntitiesCollection.
                        $.dataSource.unwrapHack(childEntitiesCollection).__syncToNewEntities();
                    }
                }
            }
        }
    },

    __getEntityType: function () {
        return this._entityType;
    },

    // TODO -- We have no mechanism to similarly clear data sources.
    //// __clear: function() {
    ////     $([this._clientEntities]).trigger("arrayChanging", [ { change: "reset"  }]);
    ////     while (this._updatedEntities.length > 0) {
    ////         this._purgeEntityAtIndex(0, false);
    ////     }
    ////     while (this._addedEntities.length > 0) {
    ////         this._purgeUncommittedAddedEntity(this._addedEntities[0], false);
    ////     }
    ////     $([ this._clientEntities ]).trigger({ type: "arrayChange", isInternalChange: true }, [ { change: "reset" } ]);
    //// },


    // Private methods

    _handleCollectionChange: function (changeEvent, change) {
        if (changeEvent.isInternalChange) {
            // Self-inflicted change.
            return;
        }

        switch (change.change) {
            case "add":
                // The value of change.newIndex is not interesting at the EntitySet level.
                // Ordering of RDS entities is established by the server.
                // EntitySet collects both internal and external adds by appending them to its cache.

                var entitiesToAdd = change.newItems;
                if (entitiesToAdd.length > 1) {
                    throw "NYI -- Can only add a single entity to/from an array in one operation.";
                }

                var entityToAdd = entitiesToAdd[0],
                    self = this;
                this._executeWithDeferredEvents(function (deferredEvents) {
                    self._addEntity(entityToAdd, deferredEvents);
                    // Assumes add to query result and not to collection-typed relationship property.
                }, /* fromAddOrUpdate: */true);
                // Re: fromAddOrUpdate, this will be triggered due to "arrayChanged" and we want a consistent view of our 
                // cache, regardless of "arrayChanged" ordering b/t us and clients.
                break;

            case "remove":
                var index = change.oldIndex;
                var entitiesToDelete = change.oldItems;
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

    _addEntity: function (entity, deferredEvents) {
        if ($.inArray(entity, this._updatedEntities) >= 0 ||  // ...in entities from last query
            this._getAddedEntityFromEntity(entity)) {  // ...in added entities
            throw "Entity already in data source.";
        }

        var id = "added" + (Math.floor(Math.random() * Math.pow(2, 32) + 1)).toString(),
            addedEntity = { entity: entity, clientId: id };
        this._addedEntities.push(addedEntity);
        // N.B.  Entity will already have been added to this._clientEntities, as clients issue CUD operations
        // against this._clientEntities.
        this._wirePropertyChanged(entity, id);
        this._entityStates[id] = "Unmodified";
        this._addAssociationProperties(entity);

        this._updateEntityState(id, "ClientAdded", deferredEvents);

        this._dataContext.__commitEntityIfImplicit(this, entity, deferredEvents);

        deferredEvents.entitySetChanged(this._entityType);
    },

    _deleteEntity: function (entity) {
        var self = this;
        this._executeWithDeferredEvents(function (deferredEvents) {
            var id = self._getEntityId(entity),
                index = self._getEntityIndexFromId(id),
                addedEntityBeingDeleted = self._getAddedEntityFromId(id),
                deletingAddedEntity = index < 0 && addedEntityBeingDeleted;
            if (deletingAddedEntity) {
                var entityState = self._entityStates[id];
                if (entityState === "ClientAdded") {
                    // Deleting a entity that is uncommitted and only on the client.
                    // TODO -- Reconsider whether we shouldn't throw here, force clients to revert instead.
                    self._purgeUncommittedAddedEntity(addedEntityBeingDeleted, true, deferredEvents);
                } else {
                    // To be in addedEntities but not in updatedEntities, entity should either be in a
                    // pre-commit or committing state.
                    //// Assert(entityState === "ServerAdding");

                    // TODO -- Need to detect and enqueue dependent commits?
                    throw "NYI -- Can't edit a entity while previous edits are being committed.";
                }
            } else if (index < 0) {
                throw "Entity no longer cached in data source.";
            } else {
                if (self._entityStates[id].indexOf("Server") === 0) {
                    // TODO -- Need to detect and enqueue dependent commits?
                    throw "NYI -- Can't edit a entity while previous edits are being committed.";
                }

                self._updateEntityState(id, "ClientDeleted", deferredEvents);

                self._dataContext.__commitEntityIfImplicit(self, entity, deferredEvents);
            }
        });
    },

    _updateEntity: function (entity, deferredEvents) {
        var index = $.inArray(entity, this._updatedEntities),
            updatingAddedEntity = index < 0 && this._getAddedEntityFromEntity(entity);
        if (updatingAddedEntity) {
            var entityState = this._entityStates[this._getEntityId(entity)];
            if (entityState === "ClientAdded") {
                // Updating a entity that is uncommitted and only on the client.
                // Edit state remains "ClientAdded".  We won't event an edit state change (so clients had
                // better be listening on "changeField".
                return;
            } else {
                // To be in addedEntities but not in updatedEntities, entity should either be in a
                // pre-commit or committing state.
                //// Assert(entityState == "ServerAdding");

                // TODO -- What do we do if this entity is in the process of being added to the server?
                // We'll have to enqueue this update and treat it properly with respect to errors on the add, just
                // as we'll enqueue any dependent edits?
                throw "NYI -- Can't update an added entity while it's being committed.";
            }
        } else if (index < 0) {
            throw "Entity no longer cached in data source.";
        }

        var id = this._getEntityId(entity);

        if (this._entityStates[id].indexOf("Server") === 0) {
            // TODO -- Need to detect and enqueue dependent commits?
            throw "NYI -- Can't edit a entity while previous edits are being committed.";
        }

        // TODO -- What if entity is in the "ClientDeleted" state?  Should we discard the delete or throw?
        this._updateEntityState(id, "ClientUpdated", deferredEvents);

        this._dataContext.__commitEntityIfImplicit(this, entity, deferredEvents);
    },

    _updateEntityState: function (id, state, deferredEvents, responseText, entity) {
        /// <param name="responseText" optional="true"></param>
        /// <param name="entity" optional="true"></param>

        var oldState = this._entityStates[id];
        if (this._entityStates[id]) {  // We'll purge the entity before raising "Deleted".
            this._entityStates[id] = state;
        }

        entity = entity || this._getEntityFromId(id);  // Notifying after a purge requires that we pass the entity for id.

        if (responseText) {
            var error = JSON.parse(responseText);  // TODO -- I've seen this in XML format too.
            this._errors.push({ entity: entity, error: error });
        }

        // TODO -- Use "error" to maintain lists of uncommitted and of in-error operations.
        // Allow for resolve/retry on in-error operations.

        if (oldState !== state) {
            var self = this;
            deferredEvents.deferEvent(function () {
                self._raiseEntityStateChangedEvent(entity, state);
            });
        }
    },

    _purgeEntityAtIndex: function (indexToPurge, triggerArrayChange, deferredEvents) {
        var entityToPurge = this._updatedEntities[indexToPurge],
            idToPurge = this._getEntityId(entityToPurge);
    
        $(this._updatedEntities[indexToPurge]).unbind("changeField", this._changeHandlers[idToPurge]);
        delete this._changeHandlers[idToPurge];
        // TODO -- Unbind by namespace and save the changeHandlers cache?

        this._updatedEntities = $.grep(this._updatedEntities, function (unused, index) {
            return index !== indexToPurge;
        });
        this._originalEntities = $.grep(this._originalEntities, function (unused, index) {
            return index !== indexToPurge;
        });
        this._purgeFromClientEntities(entityToPurge, triggerArrayChange, deferredEvents);

        delete this._entityStates[idToPurge];
        this._disposeChildEntitiesCollections(idToPurge);

        if (this._getAddedEntityFromId(idToPurge)) {
            this._addedEntities = $.grep(this._addedEntities, function (entity) { return entity.clientId !== idToPurge; });
        }

        this._errors = $.grep(this._errors, function (index, error) { return error.entity !== entityToPurge; });

        // TODO -- Have a specific event for entities leaving the cache?
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

    _getServerEntityId: function (entity) {
        return entity[this._idProperty];
    },

    _getEntityIndexFromId: function (id) {
        var addedEntity = this._getAddedEntityFromId(id),
            idToFind;
        if (!addedEntity) {
            idToFind = id;
        } else if (addedEntity.serverId === undefined) {
            return -1;
        } else {
            idToFind = addedEntity.serverId.toString();
        }

        var index = -1;
        for (var i = 0; i < this._originalEntities.length; i++) {
            // Compare based on string, since "id" is actually a client id.
            if (this._getServerEntityId(this._originalEntities[i]).toString() === idToFind) {
                index = i;
                break;
            }
        }

        return index;
    },

    // N.B.  Server ids are faithful to the native type of identity properties.  Clients ids are strings.
    _getEntityIndexFromServerId: function (id) {
        var index = -1;
        for (var i = 0; i < this._originalEntities.length; i++) {
            if (this._getServerEntityId(this._originalEntities[i]) === id) {
                index = i;
                break;
            }
        }

        return index;
    },

    _wirePropertyChanged: function (entity, id) {
        var self = this,
            eventData = {
                // "first" ensures that our handler is notified ahead of non-"first" so the DataSource can bring
                // itself into a consistent state _before_ any non-DataSource clients are notified.
                first: true,

                // "eventDefault" ensures that we issue our events _after_ all clients have been notified of internal adds.
                eventDefault: function () {
                    self._flushDeferredEventsFromAddOrUpdate();
                }
            },
            changeHandler = function (changeEvent, changed, newValue) {
                self._handlePropertyChange(changeEvent, changed, newValue);
            };
        $(entity).bind("changeField", eventData, changeHandler);
        this._changeHandlers[id] = changeHandler;
    },

    _handlePropertyChange: function (changeEvent, changed, newValue) {
        if (changeEvent.isInternalChange) {
            // Self-inflicted change.
            return;
        }

        if (changed === "") {
            // Data-linking sends all <input> changes to the linked object.
            return;
        }

        var entity = changeEvent.target,
            id = this._getEntityId(entity);
        if (this._entityStates[id] === "Unmodified" &&
            entity[changed] === this._originalEntities[this._getEntityIndexFromId(id)][changed]) {
            // No-op
            return;
        }

        var self = this;
        this._executeWithDeferredEvents(function (deferredEvents) {
            deferredEvents.deferEvent(function () {
                self._raiseChangeEvent(entity, changed, newValue);
            });  // Issue the "entity change" event prior to any related "entity state changed" event below...
            self._updateEntity(entity, deferredEvents);  // TODO -- We need some property update facility.  This is whole-entity.

            deferredEvents.entitySetChanged(self._entityType);
        }, /* fromAddOrUpdate: */true);
    },

    // Updates "updatedEntity" (the updated version of this cached entity) based on like-named properties on 
    // "sourceEntity".
    // Only locally modified properties on "updatedEntity" are changed, and "originalEntity" is used to determine
    // whether a given property has been modified locally.
    _mergeOntoUpdatedEntity: function (updatedEntity, sourceEntity, originalEntity, deferredEvents) {
        var self = this, changed;
        $.each(sourceEntity, function (key, value) {
            var isModifiedProperty = originalEntity && updatedEntity[key] !== originalEntity[key];
            if (!isModifiedProperty) {  // Only merge unmodified properties.
                if (updatedEntity[key] !== value) {  // TODO -- Only works for scalar-typed property values.
                    changed = true;
                    updatedEntity[key] = value;

                    deferredEvents.deferEvent(function () {
                        // Manually trigger the "setData/changeField" events so we can add "isInternalChange" and detect self-inflicted changes.
                        var eventArguments = [key, value];
                        // TODO -- We buffer events in order to establish consistent caches before issuing callbacks.
                        // I don't know what to do with "setData" as long as this is the case.
                        //// $(updatedEntity).trigger("setData", eventArguments);
                        $(updatedEntity).trigger({ type: "changeField", isInternalChange: true }, eventArguments);

                        self._raiseChangeEvent(updatedEntity, key, value);
                    });
                }
            }
        });

        if (changed) {
            deferredEvents.entitySetChanged(this._entityType);
        }
    },

    _getAddedEntityFromId: function (id) {
        var addedEntities = $.grep(this._addedEntities, function (addedEntity) { return addedEntity.clientId === id; });
        // Assert(addedEntities.length <= 1);
        return addedEntities[0];
    },

    _getAddedEntityFromEntity: function (entity) {
        var addedEntities = $.grep(this._addedEntities, function (addedEntity) { return addedEntity.entity === entity; });
        // Assert(addedEntities.length <= 1);
        return addedEntities[0];
    },

    _handleSubmitSucceeded: function (id, operation, result, deferredEvents) {
        var success = true,
            entity = this._getEntityFromId(id);  // ...before we purge.

        switch (operation.Operation) {
            case 2:
                if (result.ValidationErrors) {
                    success = false;
                    var self = this;
                    $.each(result.ValidationErrors, function (index, error) {
                        self._errors.push({ entity: entity, error: error });
                    });
                    this._updateEntityState(id, "ClientAdded", deferredEvents);
                } else {
                    var newOriginalEntity = result.Entity;
                    delete newOriginalEntity.__type;

                    var addedEntity = this._getAddedEntityFromId(id),
                        newUpdatedEntity = addedEntity.entity;

                    // Keep entity in addedEntities to maintain its synthetic id as the client-known id.
                    addedEntity.serverId = this._getServerEntityId(newOriginalEntity);

                    // Added entities will show up at the end of getEntities.  Clients need to refresh
                    // to reapply any ordering.
                    this._updatedEntities.push(newUpdatedEntity);
                    this._originalEntities.push(newOriginalEntity);

                    // Merge after adding to updated/originalEntities, since we'll issue callbacks during merge.
                    this._revertToOriginalEntity(addedEntity.clientId, deferredEvents);

                    this._updateEntityState(id, "Unmodified", deferredEvents);
                }
                break;

            case 3:
                var updatedOriginalEntity = result.Entity;
                delete updatedOriginalEntity.__type;
                this._originalEntities[this._getEntityIndexFromId(id)] = updatedOriginalEntity;
                this._revertToOriginalEntity(id, deferredEvents);
                this._updateEntityState(id, "Unmodified", deferredEvents);
                break;

            case 4:
                var indexToPurge = this._getEntityIndexFromId(id);
                this._purgeEntityAtIndex(indexToPurge, true, deferredEvents);
                this._updateEntityState(id, "Deleted", deferredEvents, null, entity);
                break;
        }

        if (success) {
            this._errors = $.grep(this._errors, function (index, error) { return error.entity !== entity; });
        }

        return success;
    },

    _handleSubmitFailed: function (id, operation, response, deferredEvents) {
        var state;
        switch (operation.Operation) {
            case 2: state = "ClientAdded"; break;
            case 3: state = "ClientUpdated"; break;
            case 4: state = "ClientDeleted"; break;
        }
        this._updateEntityState(id, state, deferredEvents, response.responseText);
    },

    _revertToOriginalEntity: function (id, deferredEvents) {
        var index = this._getEntityIndexFromId(id);
        this._mergeOntoUpdatedEntity(this._updatedEntities[index], this._originalEntities[index], null, deferredEvents);
    },

    _purgeUncommittedAddedEntity: function (addedEntityBeingPurged, triggerArrayChange, deferredEvents) {
        var id = addedEntityBeingPurged.clientId,
            entity = addedEntityBeingPurged.entity;
        $(entity).unbind("changeField", this._changeHandlers[id]);
        delete this._changeHandlers[id];
        this._addedEntities = $.grep(this._addedEntities, function (addedEntity) { return addedEntity !== addedEntityBeingPurged; });
        delete this._entityStates[id];
        this._disposeChildEntitiesCollections(id);
        this._errors = $.grep(this._errors, function (index, error) { return error.entity !== entity; });
        this._purgeFromClientEntities(entity, triggerArrayChange, deferredEvents);
        this._updateEntityState(id, "Deleted", deferredEvents, null, entity);
    },

    _purgeFromClientEntities: function (entity, triggerArrayChange, deferredEvents) {
        var index = $.inArray(entity, this._clientEntities);
        this._clientEntities.splice(index, 1);

        if (triggerArrayChange) {
            var self = this;
            deferredEvents.deferEvent(function () {
                // Manually trigger the "arrayChanging/Change" events so we can add "isInternalChange" and detect self-inflicted changes.
                var eventArguments = [{
                    change: "remove",
                    oldIndex: index,
                    oldItems: [entity]
                }];  // TODO -- Can we have reuse jQuery code to develop event arguments for us?
                // TODO -- We buffer events in order to establish consistent caches before issuing callbacks.
                // I don't know what to do with "arrayChanging" as long as this is the case.
                //// $([ self._clientEntities ]).trigger("arrayChanging", eventArguments);
                $([self._clientEntities]).trigger({ type: "arrayChange", isInternalChange: true }, eventArguments);
            });
        }

        deferredEvents.entitySetChanged(this._entityType);
    },

    _getEntityFromId: function (id) {
        var self = this;
        return $.grep(this.getEntities(), function (entity) { return self._getEntityId(entity) === id; })[0];
    },

    _getEntityId: function (entity) {
        var addedEntity = this._getAddedEntityFromEntity(entity);
        if (addedEntity) {
            return addedEntity.clientId;
        }

        var index = $.inArray(entity, this._updatedEntities);
        if (index >= 0) {
            // Trust only the property values on the original entity, allowing the client to update id properties.
            return this._getServerEntityId(this._originalEntities[index]).toString();
        }

        return null;
    },

    // TODO -- This code is largely duplicated in DataContext.js.
    _executeWithDeferredEvents: function (toExecute, fromAddOrUpdate) {
        /// <param name="fromAddOrUpdate" optional="true"></param>

        this._assertNotFlushingDeferredEvents();

        var deferredEvents = [],
            entitySetChanged,
            deferredEventsCollector = {
                deferEvent: function (eventToDefer) {
                    deferredEvents.push(eventToDefer);
                },
                entitySetChanged: function (entityType) {
                    // TODO -- Assert entityType === this._entityType.
                    entitySetChanged = true;
                }
            },
            result = toExecute(deferredEventsCollector);
        if (entitySetChanged) {
            this._dataContext.__entitySetChanged(this._entityType, deferredEventsCollector);
        }

        if (fromAddOrUpdate) {
            // We defer events related to internal adds or updates so that all clients receive
            // their arrayChange/changeField callbacks before they receive our callbacks.
            // Delete doesn't get such treatment because for non-destructive deletes there is
            // no "arrayChange" event accompanying the internal delete.
            var self = this;
            $.each(deferredEvents, function (index, deferredEvent) {
                self._deferredEvents.push(deferredEvent);
            });
        } else {
            this._flushDeferredEvents(deferredEvents);
        }

        return result;
    },

    _flushDeferredEventsFromAddOrUpdate: function () {
        if (this._deferredEvents.length !== 0) {
            var deferredEvents = this._deferredEvents;
            this._deferredEvents = [];
            this._flushDeferredEvents(deferredEvents);
        }
    },

    _flushDeferredEvents: function (deferredEvents) {
        try {
            this._flushingDeferredEvents = true;
            $.each(deferredEvents, function (index, deferredEvent) {
                deferredEvent();
            });
        } finally {
            this._flushingDeferredEvents = false;
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

    _addAssociationProperties: function (entity) {
        var metadata = this._dataContext.__getMetadata(this._entityType) || {};
        if (metadata.fields) {
            var self = this;
            $.each(metadata.fields, function (fieldName, fieldMetadata) {
                if (fieldMetadata.association) {
                    if (fieldMetadata.association.isForeignKey) {
                        entity["get_" + fieldName] = function () {
                            return self._getParentEntity(entity, fieldName);
                        };
                        entity["set_" + fieldName] = function (parentEntity) {
                            return self._setParentEntity(entity, fieldName, parentEntity);
                        };
                    } else if (fieldMetadata.array) {
                        entity["get_" + fieldName] = function () {
                            return self._getChildEntities(entity, fieldName);
                        };
                    } else {
                        // TODO -- Singleton child entities?
                    }
                }
            });
        }
    },

    _getParentEntity: function (entity, fieldName) {
        var metadata = this._dataContext.__getMetadata(this._entityType),  // Will be non-null, if we get here.
            associationMetadata = metadata.fields[fieldName];
        var parentEntity = this._computeAssociatedEntities(entity, associationMetadata)[0];
        return parentEntity || null;
    },

    _setParentEntity: function (entity, fieldName, parentEntity) {
        var metadata = this._dataContext.__getMetadata(this._entityType),  // Will be non-null, if we get here.
            associationMetadata = metadata.fields[fieldName];

        var targetEntitySet = this._dataContext.getEntitySet(associationMetadata.type);
        if ($.inArray(parentEntity, targetEntitySet.getEntities()) < 0) {
            // TODO -- Should this implicitly add the parent entity?  I doubt it.
            throw "Parent entity is not in the parent entity set for this association.";
        } else if ((targetEntitySet.getEntityState(parentEntity) || "").indexOf("Add") > 0) {
            // TODO -- Add support for added parent entities without an established key value, fix-up after commit.
            throw "NYI -- Cannot set foreign keys to key values computed from added entities.  Commit your parent entity first.";
        }

        var targetKey = associationMetadata.association.otherKey,
            targetKeyValue = parentEntity ? parentEntity[targetKey[0]] : null;  // TODO -- Generalize to N fields.
        if (targetKeyValue === undefined) {
            throw "Parent entity has no value for its '" + targetKey[0] + "' key property.";
        }

        var sourceKey = associationMetadata.association.thisKey,
            sourceKeyValue = entity[sourceKey[0]],  // TODO -- Generalize to N fields.
            setForeignKeyValue;
        if (!parentEntity) {
            if (sourceKeyValue !== null) {
                setForeignKeyValue = true;
            }
        } else if (sourceKeyValue === undefined || sourceKeyValue !== targetKeyValue) {
            setForeignKeyValue = true;
        }

        if (setForeignKeyValue) {
            $(entity).setField(sourceKey[0], targetKeyValue);
            // TODO -- Should we trigger "changeField" on the child entities parent property here?
        }
    },

    _getChildEntities: function (entity, fieldName) {
        var id = this._getEntityId(entity),
            childEntitiesCollections = this._childEntitiesCollections[id];
        if (!childEntitiesCollections) {
            childEntitiesCollections = this._childEntitiesCollections[id] = {};
        }

        var childEntitiesCollection = childEntitiesCollections[fieldName];
        if (!childEntitiesCollection) {
            var metadata = this._dataContext.__getMetadata(this._entityType),  // Will be non-null, if we get here.
                associationMetadata = metadata.fields[fieldName];

            childEntitiesCollection = this._computeAssociatedEntities(entity, associationMetadata);

            var self = this;
            var handleAddEntity = function (entityToAdd) {
                if ((self.getEntityState(entity) || "").indexOf("Add") > 0) {
                    // TODO -- Add support for added parent entities without an established key value, fix-up after commit.
                    throw "NYI -- Cannot set foreign keys to key values computed from added entities.  Commit your parent entity first.";
                }

                var sourceKey = associationMetadata.association.thisKey,
                    sourceKeyValue = entity[sourceKey[0]];  // TODO -- Generalize to N fields.
                if (sourceKeyValue === undefined) {
                    throw "Parent entity has no value for its '" + sourceKey[0] + "' key property.";
                }

                var targetKey = associationMetadata.association.otherKey,
                    targetKeyValue = entityToAdd[targetKey[0]];  // TODO -- Generalize to N fields.
                // TODO -- Add support for added parent entities without an established key value, fix-up after commit.
                if (targetKeyValue === undefined || targetKeyValue !== sourceKeyValue) {
                    $(entityToAdd).setField(targetKey[0], sourceKeyValue);  // TODO -- Generalize to N fields.
                }

                var targetEntitySet = self._dataContext.getEntitySet(associationMetadata.type);
                if ($.inArray(entityToAdd, targetEntitySet.getEntities()) < 0) {
                    // Do this via an internal protocol and not via $.push so that all clients of the child entity
                    // collection receive "entityStateChanged" _after_ "arrayChange" events.  
                    targetEntitySet._addEntityViaChildEntitiesCollection(entityToAdd);

                    // TODO -- Explicitly kick the data source ascribed to childEntitiesCollection to have it
                    // sync its internal cache (which we don't actually need) to childEntitiesCollection.
                    $.dataSource.unwrapHack(childEntitiesCollection).__syncToNewEntities();
                }
            },
            flushDeferredEventsFromAdd = function () {
                var targetEntitySet = self._dataContext.getEntitySet(associationMetadata.type);
                targetEntitySet._flushDeferredEventsFromAddOrUpdate();
            };

            var dataSource = new MSD.AssociatedEntitiesDataSource(
                this._dataContext, associationMetadata.type, childEntitiesCollection, handleAddEntity, flushDeferredEventsFromAdd);
            $.dataSource.wrapHack(childEntitiesCollection, dataSource);  // TODO -- Rework factoring.

            childEntitiesCollections[fieldName] = childEntitiesCollection;
        }

        return childEntitiesCollection;
    },

    _computeAssociatedEntities: function (entity, associationMetadata) {
        var sourceKeyValue = entity[associationMetadata.association.thisKey[0]];  // TODO -- Generalize to N fields.
        var targetEntitySet = this._dataContext.getEntitySet(associationMetadata.type);
        var targetEntities = targetEntitySet._getTargetEntities(associationMetadata.association.otherKey, sourceKeyValue);
        return targetEntities;
    },

    _getTargetEntities: function (key, keyValue) {
        var targetEntities = $.grep(this._clientEntities, function (entity) {
            var targetKeyValue = entity[key[0]];  // TODO -- Generalize to N fields.
            return targetKeyValue !== undefined && targetKeyValue === keyValue;
            // TODO -- Confirm correct equivalence check here.
        });
        return targetEntities;
    },

    _disposeChildEntitiesCollections: function (id) {
        var childEntitiesCollections = this._childEntitiesCollections[id];
        if (childEntitiesCollections) {
            $.each(childEntitiesCollections, function (unused, childEntitiesCollection) {
                $([childEntitiesCollection]).dataSource().destroy();
            });
        }
        delete this._childEntitiesCollections[id];
    },

    _addEntityViaChildEntitiesCollection: function (entityToAdd) {
        var self = this;
        this._executeWithDeferredEvents(function (deferredEvents) {
            // _addEntity itself doesn't update _clientRecords nor does it trigger "arrayChange", since it's meant
            // to react to "arrayChange" from internal adds by the client.
            self._clientEntities.push(entityToAdd);

            self._addEntity(entityToAdd, deferredEvents);

            deferredEvents.deferEvent(function () {
                var eventArguments = [{
                    change: "add",
                    newIndex: $.inArray(entityToAdd, self.getEntities()),
                    newItems: entityToAdd
                }];
                // TODO -- We buffer events in order to establish consistent caches before issuing callbacks.
                // I don't know what to do with "arrayChanging" as long as this is the case.
                //// $([ self._clientEntities ]).trigger("arrayChanging", eventArguments);
                $([self._clientEntities]).trigger({ type: "arrayChange", isInternalChange: true }, eventArguments);
            });
        }, /* fromAddOrUpdate: */true);
        // Re: fromAddOrUpdate, this will be triggered due to "arrayChanged" on a child entity collection and 
        // we want a consistent view of our cache, regardless of "arrayChanged" ordering b/t the entity set and
        // clients.
    }
};
    
