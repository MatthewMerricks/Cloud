(function ($) {

    // TODO Is it interesting to have WCF somehow provide help in writing AJAX widgets? Perhaps we can 
    // layer something on top of a basic wiget and code-gen that as a start.

    $.widget("ui.weather", {
        options: {
            // Service address 
            address: "./WeatherService.svc/GetForecast",
            // WOEID of location - to find out what it is for a given location, go to http://weather.yahoo.com, look up 
            // the forecast for the location and then look at the URI for the WOEID
            woeid: 2490383
        },
        _create: function () {
            var el = this.element;
            var o = this.options;
            $.getScript("http://ajax.microsoft.com/ajax/jquery.templates/beta1/jquery.tmpl.js",
                function () {
                    $.template("table", '<table><thead><tr><th>${city}</th><th colspan="2">Conditions</th><th>Temperature</th></tr></thead>' +
                    '<tbody>{{each forecast}}<tr><td>${time}</td><td><img src="${icon}" /></td><td><strong>${descr}</strong>{{if sun}}<br/>Sunrise/sunset:<br/>${sun}{{/if}}</td><td>${temp}</td></tr>{{/each}}</tbody></table>');
                    $.ajax({
                        url: o.address,
                        data: { woeid: o.woeid },
                        dataType: "jsonp",
                        success: function (result) {
                            this.valueTable = $.tmpl("table", result).addClass("ui-widget")
                                        .find("thead").addClass("ui-widget-header").end()
                                        .find("tbody tr").addClass("ui-widget-content").end()
                                        .appendTo(el);
                        }
                    });
                    
                });
        },
        destroy: function () {
            this.valueTable.remove();
            $.widget.prototype.destroy.apply(this, arguments); // default
        }
    });
})(jQuery);
