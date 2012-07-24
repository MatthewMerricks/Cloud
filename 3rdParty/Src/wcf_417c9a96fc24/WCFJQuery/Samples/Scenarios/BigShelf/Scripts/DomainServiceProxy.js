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

MSD.DomainServiceProxy = function (serviceBaseUrl) {
    // Ensure the new keyword has been used so that the 'this' keyword is set properly
    if (!(this instanceof MSD.DomainServiceProxy)) {
        return new MSD.DomainServiceProxy(serviceBaseUrl);
    }

    this.serviceBaseUrl = serviceBaseUrl;
    this.query = function (queryName, successCallback, queryPartsFunction, queryParts) {
        /// <summary>
        /// Invoke a query.  queryParts is an optional object seed or array of name/value pairs.
        /// </summary>
        /// <param name="queryName">The name of the query method defined in the DomainService.</param>
        /// <param name="successCallback">The function to call when the query is successful.  The results of the query will be supplied.</param>
        /// <param name="queryPartsFunction" optional="true" mayBeNull="true">
        /// Optional. A function that accepts the base query and will return the query with any where, orderby, skip, or take operations applied.
        /// </param>
        /// <param name="queryParts" optional="true">
        /// Optional. Can be either and Array of name/value pairs or an object whose properties will be transformed into name/value pairs.
        /// </param>

        // TODO: parameter validation - successCallback.
        // TODO: parameter validation - queryParts.
        // TODO: parameter validation - queryPartsFunction.

        // Use the same approach as jQuery for determining if an object is an Array
        var isArray = function (object) { return (Object.prototype.toString.call(object) === "[object Array]"); };

        // "queryParts" can be either an object with properties and values or it
        // can be an array of name/value pairs.  When it's an object, we transform
        // it to an array of name/value pairs.
        if (queryParts && !isArray(queryParts)) {
            // If the queryParts specified is not an array, then create an
            // array of name/value pairs from the properties on that object.
            var queryPartsArray = [];
            $.each(queryParts, function (parameterName, parameterValue) { queryPartsArray.push({ name: parameterName, value: parameterValue }); });
            queryParts = queryPartsArray;
        } else if (!queryParts) {
            queryParts = [];
        }

        // Default the _includeTotalCount property to false
        // Before submitting the query, this property will be turned
        // into a query part if it has been set to true.
        queryParts._includeTotalCount = false;

        // If a query parts function was specified, then
        if ($.isFunction(queryPartsFunction)) {
            // Allow many forms for specifying standard operators
            var operatorStrings = {
                "<": ["<", "islessthan", "lessthan", "less", "lt"],
                "<=": ["<=", "islessthanorequalto", "lessthanequal", "lte"],
                "==": ["==", "isequalto", "equals", "equalto", "equal", "eq"],
                "!=": ["!=", "isnotequalto", "notequals", "notequalto", "notequal", "neq", "not"],
                ">=": [">=", "isgreaterthanorequalto", "greaterthanequal", "gte"],
                ">": [">", "isgreaterthan", "greaterthan", "greater", "gt"]
            };

            // Helper function for applying an operator to a property/value pair
            var applyOperator = function (property, operator, value) {
                var lowerOperator = operator.toString().toLowerCase();

                if (typeof value === "string") {
                    value = '"' + value + '"';
                }

                // See if the operator specified is one of the accepted forms of
                // standard operators.  If so, choose the operator's allowed form.
                for (var op in operatorStrings) {
                    if ($.inArray(lowerOperator, operatorStrings[op]) > -1) {
                        return property + op + value;
                    }
                }

                // A non-standard operator was used.  Assume it's a method on the property
                // such as .Contains() or .StartsWith().
                return property + '.' + operator + '(' + value + ')';
            };

            // Allow orderBy, where, skip, and take methods to be called directly against our queryParts array.
            queryParts.orderby = queryParts.orderBy = queryParts.OrderBy = function (sort) {
                var sortValue;
                if (Object.prototype.toString.call(sort) === "[object Array]") {
                    sortValue = "";
                    $.each(sort, function (index, sortPart) {
                        if (index > 0) {
                            sortValue += ",";
                        }
                        sortValue += makeSortPartValue(sortPart);
                    });
                } else {
                    sortValue = makeSortPartValue(sort);
                }
                this.push({ name: "$orderby", value: sortValue });
                return this;

                function makeSortPartValue (sortPart) {
                    return sortPart.property + (sortPart.direction ? " " + sortPart.direction : "");
                };
            };
            queryParts.where = queryParts.Where = function (clause, value, operator) {
                if (value === undefined) {
                    this.push({ name: "$where", value: clause });
                } else {
                    if (operator === null || operator === undefined) {
                        operator = "eq";
                    }
                    this.push({ name: "$where", value: applyOperator(clause, operator, value) });
                }
                return this;
            };
            queryParts.skip = queryParts.Skip = function (skipAmount) {
                this.push({ name: "$skip", value: skipAmount });
                return this;
            };
            queryParts.take = queryParts.Take = function (takeAmount) {
                this.push({ name: "$take", value: takeAmount });
                return this;
            };

            // Also create an includeTotalCount method that will set the includeTotalCount property to true
            // to allow for a fluent call chain.
            queryParts.includetotalcount = queryParts.includeTotalCount = queryParts.IncludeTotalCount = function () {
                this._includeTotalCount = true;
                return this;
            };

            // Call the function specified by the caller to add query parts
            // This function will use the orderBy, where, skip, take, and includeTotalCount methods
            // to form the query to be executed.
            queryPartsFunction(queryParts);
        }

        // If we need to include the total count, then add that to the query parts
        if (queryParts._includeTotalCount) {
            queryParts.push({ name: "$includeTotalCount", value: true });
        }

        // Handle a service base URL with or without a trailing slash
        var slash = (this.serviceBaseUrl.substring(this.serviceBaseUrl.length - 1) !== "/" ? "/" : "");

        // Invoke the query
        $.ajax({
            url: this.serviceBaseUrl + slash + "json/" + queryName,
            data: queryParts,
            dataType: "json",
            success: function (queryResult) {
                if ($.isFunction(successCallback)) {
                    var resultData = queryResult[queryName + "Result"];
                    successCallback(resultData);
                }
            }
        });
    };
}