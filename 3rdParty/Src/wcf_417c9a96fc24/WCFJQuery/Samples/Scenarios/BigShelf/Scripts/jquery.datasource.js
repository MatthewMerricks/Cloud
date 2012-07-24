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

/// <dictionary target='comment'>serializable</dictionary>
(function ($) {

    $.fn.extend({
        dataSource: function (options) {
            // TODO -- I pick off the first element here, following a pattern in jquery.validate.js.  Confirm that this is ok.
            return makeDataSource(this[0], options);
        }
    });

    $.dataSource = function (options) {
        if (!options) {
            throw "Provide contruction options to $.dataSource";
        } else {
            var entityCollection = options.entityCollection || [];
            delete options.entityCollection;
            return makeDataSource(entityCollection, options);
        }
    };

    function makeDataSource (entityCollection, options) {
        if (options) {
            var currentDataSource = entityCollection.__dataSource__;
            if (currentDataSource) {
                currentDataSource.destroy();
            }

            entityCollection.__dataSource__ = createDataSource(entityCollection, options);
        }

        return entityCollection.__dataSource__;
    };

    // TODO -- For associated entities collections, we need to be able to ascribe a data source
    // to a collection from within our data source code itself.  Need to refactor to fix this
    // break in layering.
    $.dataSource.wrapHack = function (entityCollection, dataSource) {
        entityCollection.__dataSource__ = wrap(dataSource);
    };
    $.dataSource.unwrapHack = function (entityCollection) {
        return entityCollection.__dataSource__._dataSource;
    };

    function createDataSource(entityCollection, options) {
        var MSD = Microsoft.ServiceModel.DomainServices;
        var dataSource;
        if (options.serviceUrl) {
            dataSource = new MSD.RemoteDataSource(options.serviceUrl, options.queryName, options.entityType, {
                queryParameters: options.queryParameters,
                bufferChanges: options.bufferChanges,
                dataContext: options.dataContext,
                entityCollection: entityCollection
            });
        } else if (options.inputData) {
            if (!options.inputData.__dataSource__) {
                throw "NYI -- Currenty, input array must be bound to a data source.";
            }
            dataSource = new MSD.LocalDataSource($.dataSource.unwrapHack(options.inputData), {
                entityCollection: entityCollection
            });
        } else {
            throw "Must provide either server parameters or an input array.";
        }

        return wrap(dataSource).options(options);
    };

    function wrap(dataSource) {
        var refreshingHandler, refreshHandler, committingHandler, commitHandler, queryResultsStaleHandler, entityStateChangeHandler;
        var observer = {
            refreshStart: function () {
                $(result).trigger("datasourcerefreshing", arguments);
                if (refreshingHandler) {
                    refreshingHandler.apply(null, arguments);
                }
            },
            refresh: function () {
                $(result).trigger("datasourcerefresh", arguments);
                if (refreshHandler) {
                    refreshHandler.apply(null, arguments);
                }
            },
            commitStart: function () {
                $(result).trigger("datasourcecommitting", arguments);
                if (committingHandler) {
                    committingHandler.apply(null, arguments);
                }
            },
            commit: function () {
                $(result).trigger("datasourcecommit", arguments);
                if (commitHandler) {
                    commitHandler.apply(null, arguments);
                }
            },
            resultsStale: function () {
                $(result).trigger("datasourcequeryresultsstale", arguments);
                if (queryResultsStaleHandler) {
                    queryResultsStaleHandler.apply(null, arguments);
                }
            },
            entityStateChanged: function () {
                $(result).trigger("datasourceentitiestatechange", arguments);
                if (entityStateChangeHandler) {
                    entityStateChangeHandler.apply(null, arguments);
                }
            }
        };
        dataSource.addObserver(observer);

        // TODO -- Make this a function and add the methods, so the result isn't serializable.
        var result = {
            applyLocalQuery: function (options) {
                var newDataSource = new MSD.LocalDataSource(dataSource);

                var entityCollection = newDataSource.getEntities();
                entityCollection.__dataSource__ = wrap(newDataSource);

                entityCollection.__dataSource__.options(options);

                return entityCollection;
                // TODO -- Consider returning the data source here, so this can be fluent.
                // We'd need a getEntities() as well, as typically the app wants to chain data source
                // operators and then assign the eventual array to a var.
            },

            options: function (options) {
                applyOptions(options);
                return this;
            },

            option: function (option, value) {
                applyOption(option, value);
                return this;
            },

            refresh: function (options) {
                if (options) {
                    var hasDataSourceOptions;
                    $.each(options, function (key, value) {
                        if (key !== "all" && key !== "completed") {
                            hasDataSourceOptions = true;
                        }
                    });
                    if (hasDataSourceOptions) {
                        // "applyOptions" trounces all the old query options, so only call if we really have options here.
                        applyOptions(options);
                    }
                }
                dataSource.refresh(options);
                return this;
            },

            commitChanges: function () {
                dataSource.commitChanges();
                return this;
            },

            revertChanges: function (entity, propertyName) {
                if (typeof entity === "object") {
                    dataSource.revertChange(entity, propertyName);
                } else {
                    // Assume "entity" is boolean-typed "all".
                    dataSource.revertChanges(!!entity);
                }
                return this;
            },

            destroy: function () {
                if (dataSource) {
                    delete dataSource.getEntities().__dataSource__;

                    dataSource.removeObserver(observer);
                    dataSource.dispose();

                    dataSource = null;
                }

                return this;
            },

            getEntities: function () {
                return dataSource.getEntities();
            },

            getEntityState: function (entity) {
                return dataSource.getEntityState(entity);
            },

            getEntityValidationRules: function () {
                return dataSource.getEntityValidationRules();
            },

            getErrors: function () {
                return dataSource.getErrors();
            },

            isPropertyChanged: function (entity, propertyName) {
                return dataSource.isPropertyChanged(entity, propertyName);
            },

            dataContext: function () {
                return dataSource.getDataContext();
            },

            getTotalCount: function () {
                return dataSource.getTotalEntityCount();
            },

            // TODO -- Merely supports unwrapHack above.  Remove.
            _dataSource: dataSource
        }

        return result;

        // N.B.  Null/undefined option values will unset the given option.
        function applyOptions(options) {
            options = options || {};

            var self = this;
            $.each([ "filter", "sort", "paging", "refreshing", "refresh", "commit", "queryResultsStale", "entityStateChange" ], function (index, eventName) {
                applyOption(eventName, options[eventName]);
            });
        };

        function applyOption(option, value) {
            switch (option) {
                case "filter":
                    dataSource.setFilter(value);
                    break;

                case "sort":
                    dataSource.setSort(value);
                    break;

                case "paging":
                    dataSource.setPaging(value);
                    break;

                case "refreshing":
                    observer.refreshStart = value;
                    break;

                case "refresh":
                    refreshHandler = value;
                    break;

                case "committing":
                    committingHandler = value;
                    break;

                case "commit":
                    commitHandler = value;
                    break;

                case "queryResultsStale":
                    queryResultsStaleHandler = value;
                    break;

                case "entityStateChange":
                    entityStateChangeHandler = value;
                    break;

                default:
                    throw "Unrecognized option '" + option + "'";
            }
        };
    };
})(jQuery);
