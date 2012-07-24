/// <reference path="RemoteDataSource.js" />

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

MSD.AssociatedEntitiesDataSource = function (dataContext, entityType, clientEntities, handleAddEntity, flushDeferredEventsFromAdd) {
    var options = { 
        entityCollection: clientEntities 
    };
    MSD.RemoteDataSource.prototype._init.call(this, dataContext, entityType, options);
    var entitySet = dataContext.getEntitySet(entityType);
    this._bindToEntitySet(entitySet);

    this._handleAddEntity = handleAddEntity;
    this._flushDeferredEventsFromAdd = flushDeferredEventsFromAdd;

    // This can move to DataSource if/when it supports starting with non-empty clientEntities.
    this.__syncToNewEntities();
}

// TODO -- I chose to derive from RemoteDataSource merely out of convenience.  It keeps a dedicated "_entities" cache
// that is separate from "_clientEntities", which isn't needed for AssociatedEntitiesDataSource.
MSD.AssociatedEntitiesDataSource.prototype = $.extend({}, new MSD.RemoteDataSource(), {

    // Private fields

    _handleAddEntity: null,
    _flushDeferredEventsFromAdd: null,


    // Public methods

    getTotalEntityCount: function () {
        return this._entities.length;
    },

    setSort: function (options) {
        throw "Associated entities collections don't support query.";
    },

    setFilter: function (filter) {
        throw "Associated entities collections don't support query.";
    },

    setPaging: function (options) {
        throw "Associated entities collections don't support query.";
    },

    refresh: function (options) {
        throw "Associated entities collections refresh implicitly.";
    },


    // Internal methods

    __syncToNewEntities: function () {
        // TODO -- We sync _entities differently than the other data source types.
        this._entities = $.map(this._clientEntities, function (entity) {
            return { entity: entity };
        });
    },


    // Private methods

    _addEntity: function (entity) {
        // TODO -- We don't care about our base class' tracking of entities added
        // directly to this data source.  We sync _entities via a different mechanism.
        //// this._entities.push({ entity: entity, added: true });

        // Delegate to the appropriate entity set to make the addition and synchronize all affected 
        // associated child entity collections.
        this._handleAddEntity(entity);
    },

    _purgeEntity: function (entityToPurge) {
        // TODO -- We sync _entities via a different mechanism.
    },

    _flushDeferredEvents: function () {
        // Used to flush deferred events from _handleAddEntity and order them _after_ all "arrayChange"
        // events on our _clientEntities.
        this._flushDeferredEventsFromAdd();
    }

    // TODO -- Make array removals from "_clientEntities" null out foreign key values.
});
