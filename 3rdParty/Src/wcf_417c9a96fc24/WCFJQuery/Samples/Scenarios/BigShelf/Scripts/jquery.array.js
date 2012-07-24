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

(function ($) {

    function raiseEvents(arr, eventArgs, setValue) {
        var triggerEvents = !!eventArgs;
        if (triggerEvents) {
            var ret,
            event = $.Event("arrayChanging");
            $.event.trigger(event, [eventArgs], arr);
            if (!event.isDefaultPrevented()) {
                ret = setValue();
                $.event.trigger("arrayChange", [eventArgs], arr);
            }
            return ret;
        } else {
            return setValue();
        }
    }

    $.each("pop push reverse shift sort splice unshift".split(" "), function (i, name) {
        $[name] = function (arr) {
            var args = $.makeArray(arguments);
            args.splice(0, 1);
            return raiseEvents(arr, createEventArguments(name, arr, args), function () {
                arr[name].apply(arr, args);
            });
        }
    });

    var eventArgCreators = {
        pop: function (arr, args) {
            if (arr.length > 0) {
                return { change: "remove", oldIndex: arr.length - 1, oldItems: [arr[arr.length - 1]] };
            }
        },
        push: function (arr, args) {
            return { change: "add", newIndex: arr.length, newItems: [args[0]] };
        },
        reverse: function (arr, args) {
            if (arr.length > 0) {
                return { change: "reset" };
            }
        },
        shift: function (arr, args) {
            if (arr.length > 0) {
                return { change: "remove", oldIndex: 0, oldItems: [arr[0]] };
            }
        },
        sort: function (arr, args) {
            if (arr.length > 0) {
                return { change: "reset" };
            }
        },
        splice: function (arr, args) {
            var index = args[0];
            var numToRemove = args[1];
            var elementsToAdd = args.slice(2);
            if (numToRemove <= 0) {
                if (elementsToAdd.length > 0) {
                    return { change: "add", newIndex: index, newItems: elementsToAdd };
                }
            } else {
                var elementsToRemove = arr.slice(index, index + numToRemove);
                if (elementsToAdd.length > 0) {
                    var move = elementsToAdd.length === elementsToRemove.length &&
                        $.grep(elementsToAdd, function (elementToAdd, index) {
                            return elementToAdd !== elementsToRemove[index];
                        }).length === 0;
                    if (move) {
                        return { change: "move", oldIndex: index, oldItems: elementsToRemove, newIndex: index, newItems: elementsToAdd };
                    } else {
                        return { change: "replace", oldIndex: index, oldItems: elementsToRemove, newIndex: index, newItems: elementsToAdd };
                    }
                } else {
                    return { change: "remove", oldIndex: index, oldItems: elementsToRemove };
                }
            }
        },
        unshift: function (arr, args) {
            return { change: "add", newIndex: 0, newItems: [args[0]] };
        },
        move: function (arr, args) {
            var numToMove = arguments[1];
            if (numToMove > 0) {
                var fromIndex = arguments[0];
                var toIndex = arguments[2];
                var elementsToMove = arr.splice(fromIndex, numToMove);
                return { change: "move", oldIndex: fromIndex, oldItems: elementsToMove, newIndex: toIndex, newItems: elementsToMove };
            }
        }
    };

    function createEventArguments(operation, arr, args) {
        return eventArgCreators[operation](arr, args);
    }

    $.move = function (arr) {
        var args = $.makeArray(arguments);
        args.splice(0, 1);
        return raiseEvents(arr, createEventArguments("move", arr, args), function () {
            var fromIndex = args[0];
            var numToMove = args[1];
            var toIndex = args[2];
            var removed = arr.splice(fromIndex, numToMove);
            arr.splice(toIndex, 0, removed);
        });
    };

    var special = $.event.special;

    $.fn.triggerHandler = function (type, data) {
        if (this[0]) {
            var event = jQuery.Event(type);
            if (this.nodeType) {
                // TODO -- I want default processing for data/setField in order to defer
                // event handling in our DataSources.
                event.preventDefault();
            }
            event.stopPropagation();
            jQuery.event.trigger(event, data, this[0]);
            return event.result;
        }
    };

    $.each(["setField", "changeField"], function (i, name) {
        special[name] = {
            setup: function () {
                if (!this.nodeType) {
                    getEvents(this, name).push = function () {
                        Array.prototype.push.apply(this, arguments);
                        this.sort(function (handler1, handler2) {
                            return getHandlerWeight(handler1) > getHandlerWeight(handler2);
                        });
                    }
                    return false;
                }
            },
            _default: function (event) {
                var events = getEvents(event.target, name);
                if (events) {
                    var eventsToCallback = events.slice();
                    // slice() since handlers might be unbound during callbacks below.
                    $.each(eventsToCallback, function (index, handler) {
                        if ($.inArray(handler, events) >= 0 && handler.data && handler.data.eventDefault) {
                            handler.data.eventDefault(event);
                        }
                    });
                }
                return false;
            }
        }
    });

    $.each(["arrayChanging", "arrayChange"], function (i, name) {
        $.fn[name] = function (filter, fn) {
            if (arguments.length === 1) {
                fn = filter;
                filter = null;
            }
            return fn ? this.bind(name, filter, fn) : this.trigger(name);
        }

        special[name] = {
            setup: function () {
                getEvents(this, name).push = function () {
                    Array.prototype.push.apply(this, arguments);
                    this.sort(function (handler1, handler2) {
                        return getHandlerWeight(handler1) > getHandlerWeight(handler2);
                    });
                }
                return false;
            },
            _default: function (event) {
                var events = getEvents(event.target, name);
                if (events) {
                    var eventsToCallback = events.slice();
                    // slice() since handlers might be unbound during callbacks below.
                    $.each(eventsToCallback, function (index, handler) {
                        if ($.inArray(handler, events) >= 0 && handler.data && handler.data.eventDefault) {
                            handler.data.eventDefault(event);
                        }
                    });
                }
                return false;
            }
        }
    });

    // Returns the array of event handlers for "name" or a false-y value if no handlers have been registered
    // for that event.
    function getEvents(target, name) {
        // TODO: Re: "true" argument below, we're relying on implementation details of jQuery here.
        // We need to switch to a different scheme of ensuring that RIA/JS gets events first/last.
        var events = $.data(target, "events", undefined, true);
        return !events ? null : events[name];
    };

    function getHandlerWeight(handler) {
        return handler.data && !!handler.data.first ? -1 : 0;
    };

})(jQuery);