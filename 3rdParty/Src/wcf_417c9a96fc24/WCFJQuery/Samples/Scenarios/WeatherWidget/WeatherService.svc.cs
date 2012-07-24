// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace WeatherWidget
{
    using System;
    using System.Globalization;
    using System.Json;
    using System.ServiceModel;
    using System.ServiceModel.Web;
    using System.Xml.Linq;

    [ServiceContract]
    public class WeatherService
    {
        private const string ServiceBaseUri = "http://weather.yahooapis.com/forecastrss?u=c&w={0}";
        private const string ImageBaseUri = "http://l.yimg.com/a/i/us/we/52/{0}.gif";
        private XNamespace yweather = "http://xml.weather.yahoo.com/ns/rss/1.0";

        [WebGet(UriTemplate = "/GetForecast?woeid={woeid}")]
        public JsonValue GetWeather(string woeid)
        {
            // Get the relevant elements out of the Yahoo RSS feed
            XElement channel = XElement.Load(String.Format(CultureInfo.InvariantCulture, ServiceBaseUri, woeid)).Element("channel");
            XElement location = channel.Element(this.yweather + "location");
            XElement astronomy = channel.Element(this.yweather + "astronomy");
            XElement item = channel.Element("item");
            XElement condition = item.Element(this.yweather + "condition");

            string tempUnit = channel.Element(this.yweather + "units").Attribute("temperature").Value;

            dynamic result = new JsonObject();
            result.city = String.Format(CultureInfo.InvariantCulture, "{0}, {1}", location.Attribute("city").Value, location.Attribute("country").Value);

            // Add today
            dynamic today = new JsonObject();
            today.time = "Today";
            today.temp = String.Format(CultureInfo.InvariantCulture, "{0}{1}", condition.Attribute("temp").Value, tempUnit);
            today.descr = condition.Attribute("text").Value;
            today.icon = String.Format(CultureInfo.InvariantCulture, ImageBaseUri, condition.Attribute("code").Value);
            today.sun = String.Format(CultureInfo.InvariantCulture, "{0} / {1}", astronomy.Attribute("sunrise").Value, astronomy.Attribute("sunset").Value);

            result.forecast = new JsonArray() { today };

            // Add tomorrow and day after
            foreach (XElement forecast in item.Elements(this.yweather + "forecast"))
            {
                dynamic day = new JsonObject();
                day.time = String.Format(CultureInfo.InvariantCulture, "{0} {1}", forecast.Attribute("day").Value, forecast.Attribute("date").Value);
                day.temp = String.Format(CultureInfo.InvariantCulture, "{0}{2} / {1}{2}", forecast.Attribute("high").Value, forecast.Attribute("low").Value, tempUnit);
                day.descr = forecast.Attribute("text").Value;
                day.icon = String.Format(CultureInfo.InvariantCulture, ImageBaseUri, forecast.Attribute("code").Value);

                result.forecast.Add(day);
            }

            return result;
        }
    }
}
