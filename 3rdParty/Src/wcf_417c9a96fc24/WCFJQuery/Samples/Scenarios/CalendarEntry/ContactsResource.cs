// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace CalendarEntry
{
    using System;
    using System.Configuration;
    using System.Data.SqlClient;
    using System.Globalization;
    using System.Json;
    using System.ServiceModel;
    using System.ServiceModel.Activation;
    using System.ServiceModel.Web;
    using Microsoft.ServiceModel.Web;

    [AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Allowed)]
    [ServiceContract]
    public class ContactsResource
    {
        private static string connectionString = ConfigurationManager.ConnectionStrings["ContactManagerConnectionString"].ConnectionString;

        [WebGet(UriTemplate = "")]
        public JsonValue GetAll()
        {
            JsonObject parameters = WebOperationContext.Current.IncomingRequest.GetQueryStringAsJsonObject();
            
            JsonValue term; 
            string termValue = parameters.TryGetValue("term", out term) ? term.ReadAs<string>() : String.Empty;

            using (SqlConnection sc = new SqlConnection(connectionString))
            {
                sc.Open();
                using (SqlCommand getAll = new SqlCommand("SELECT Email FROM Contact WHERE (Email LIKE @term)", sc))
                {
                    getAll.Parameters.AddWithValue("@term", termValue + "%");
                    SqlDataReader reader = getAll.ExecuteReader();

                    JsonArray results = new JsonArray();
                    while (reader.Read())
                    {
                        results.Add(Convert.ToString(reader[0], CultureInfo.InvariantCulture));
                    } 

                    return results;
                }
            }
        } 
    }
}
