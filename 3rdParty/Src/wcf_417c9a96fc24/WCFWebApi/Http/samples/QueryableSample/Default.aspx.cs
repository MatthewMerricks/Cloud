// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

using Microsoft.ApplicationServer.Http;

namespace QueryableSample
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Xml.Serialization;

    public partial class Default : System.Web.UI.Page
    {
        private bool useJson;
        private string body;

        protected void Page_Load(object sender, EventArgs e)
        {
            this.Buffer = true;
            this.ResponseBody.PreRender += new EventHandler(this.ResponseBody_PreRender);

            if (this.Format.Text == "Json")
            {
                this.useJson = true;
            }
        }

        // Get all the contacts
        protected void GetAllContacts_Click(object sender, EventArgs e)
        {
            string uri = "http://localhost:8300/contacts";
            HttpClient client = this.GetClient(uri);
            var response = client.Get(uri);

            string result = "Received contacts:";
            foreach (Contact contact in response.Content.ReadAs<List<Contact>>())
            {
                result += string.Format(CultureInfo.InvariantCulture, "\r\n Contact Name: {0}, Contact Id: {1}", contact.Name, contact.Id);
            }
 
            this.TextBox1.Text = uri;
            this.Result.Text = result;
        }

        protected void GetTop3_Click(object sender, EventArgs e)
        {
            string uri = "http://localhost:8300/contacts?$Top=3";
            HttpClient client = this.GetClient(uri);
            var response = client.Get(uri);

            string result = "Received top 3 contacts:";
            foreach (Contact contact in response.Content.ReadAs<List<Contact>>())
            {
                result += string.Format(CultureInfo.InvariantCulture, "\r\n Contact Name: {0}, Contact Id: {1}", contact.Name, contact.Id);
            }

            this.TextBox2.Text = uri;
            this.Result.Text = result;
        }

        protected void PostNewContact_Click(object sender, EventArgs e)
        {
            string uri = "http://localhost:8300/contacts";
            HttpClient client = this.GetClient(uri);
            var contact = new Contact { Name = this.TextBox3.Text, Id = 5 };
            var request = new HttpRequestMessage(HttpMethod.Post, new Uri(uri));
            request.Content = new ObjectContent<Contact>(contact, "application/xml");
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml"));
            var response = client.Send(request);
            var receivedContact = response.Content.ReadAs<Contact>();
            this.Result.Text = string.Format(CultureInfo.InvariantCulture, "\r\n Contact Name: {0}, Contact Id: {1}", receivedContact.Name, receivedContact.Id);
        }

        protected void GetId5_Click(object sender, EventArgs e)
        {
            string uri = "http://localhost:8300/contacts?$Skip=4";
            HttpClient client = this.GetClient(uri);
            var response = client.Get(uri);

            string result = string.Empty;
            List<Contact> finalList = response.Content.ReadAs<List<Contact>>();

            if (finalList.Count == 0)
            {
                result = "There are less than 5 contacts";
            }
            else
            {
                result = "Get the 5th contact: ";
                result += string.Format(CultureInfo.InvariantCulture, "\r\n Contact Name: {0}, Contact Id: {1}", finalList[0].Name, finalList[0].Id);
            }

            this.TextBox4.Text = uri;
            this.Result.Text = result;
        }

        protected void GetId6_Click(object sender, EventArgs e)
        {
            int input = 0;
            try
            {
                input = Convert.ToInt32(this.TextBox5.Text, CultureInfo.InvariantCulture);
            }
            catch (Exception)
            {
                this.TextBox5.Text = "Error: please enter an integer";
                return;
            }
            string uri = string.Format("http://localhost:8300/contacts?$filter=Id eq {0}", TextBox5.Text);
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml"));
            var response = client.Get(uri);

            var resultList = response.Content.ReadAs<List<Contact>>();

            string result = string.Empty;

            if (resultList.Count == 0)
            {
                result = "There is no contact with ID = " + input;
            }
            else
            {
                result = "Found a matched contact: ";
                result += string.Format(CultureInfo.InvariantCulture, "\r\n Contact Name: {0}, Contact Id: {1}", resultList[0].Name, resultList[0].Id);
            }

            this.TextBox4.Text = uri;

            this.Result.Text = result;
        }

        private void TraceResponse(HttpResponseMessage response)
        {
            this.body = response.Content.ReadAsString();
        }

        private void ResponseBody_PreRender(object sender, EventArgs e)
        {
            Response.Write("<B>Response Body</B><br>" + Server.HtmlEncode(this.body));
        }

        private HttpClient GetClient(string uri)
        {
            var client = new HttpClient(new Uri(uri));
            client.Channel = new TracingResponseChannel(this.TraceResponse);

            if (this.useJson)
            {
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            }

            return client;
        }
    }
}
