// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace JsonValueEvents
{
    using System;
    using System.Json;
    using System.Windows;

    public partial class MainWindow : Window
    {
        private JsonArray items = JsonValue.Parse(
            @"
            [
                {   ""Name"" : ""Yavor"",
                    ""DOB"" : ""1984-01-17"",
                    ""OrderAmount"" : 1e+4
                },
                {   ""Name"" : ""Joe"",
                    ""DOB"" : ""1983-02-18"",
                    ""OrderAmount"" : 50000
                },
                {   ""Name"" : ""Miguel"",
                    ""DOB"" : ""1982-03-19"",
                    ""OrderAmount"" : 25.3e3
                }
            ]") as JsonArray;

        private Random rand = new Random();

        public MainWindow()
        {
            this.InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // TODO This would be a lot easier if we implemented INotifyPropertyChanged
            // and INotifyCollectionChanged, which we are considering
            this.items.Changed += new EventHandler<JsonValueChangeEventArgs>(this.Items_Changed);
            foreach (JsonValue child in this.items)
            {
                child.Changed += new EventHandler<JsonValueChangeEventArgs>(this.Child_Changed);
                this.grid.Items.Add(child);
            }
        }

        private void Items_Changed(object sender, JsonValueChangeEventArgs e)
        {
            switch (e.Change)
            {
                case JsonValueChange.Add:
                    this.grid.Items.Insert(e.Index, e.Child);
                    break;
                case JsonValueChange.Remove:
                    this.grid.Items.RemoveAt(e.Index);
                    break;
            }
        }

        private void Child_Changed(object sender, JsonValueChangeEventArgs e)
        {
            for (int i = 0; i < this.grid.Items.Count; i++)
            {
                if (this.grid.Items[i] == sender)
                {
                    int index = this.grid.Items.IndexOf(this.grid.Items[i]);
                    this.grid.Items.RemoveAt(index);
                    this.grid.Items.Insert(index, sender);
                }
            }
        }

        private void Button_Add(object sender, RoutedEventArgs e)
        {
            int count = this.items.Count;
            if (count != 0)
            {
                // This performs a shallow copy
                JsonObject newObject = new JsonObject(this.items[this.rand.Next(0, count)]);
                
                newObject["Name"] = newObject["Name"].ReadAs<string>(String.Empty) + " Foo";
                newObject["DOB"] = newObject["DOB"].ReadAs<DateTime>(DateTime.Now) + new TimeSpan(10, 0, 0, 0);
                newObject["OrderAmount"] = newObject["OrderAmount"].ReadAs<int>(0) + 10;
                newObject.Changed += new EventHandler<JsonValueChangeEventArgs>(this.Child_Changed);
                this.items.Add(newObject);
            }
        }

        private void Button_Remove(object sender, RoutedEventArgs e)
        {
            int count = this.items.Count;
            if (count != 0)
            {
                JsonObject selected = this.items[this.rand.Next(0, count)] as JsonObject;
                selected.Changed -= new EventHandler<JsonValueChangeEventArgs>(this.Child_Changed);
                this.items.Remove(selected);
            }
        }

        private void Button_Update(object sender, RoutedEventArgs e)
        {
            int count = this.items.Count;
            if (this.items.Count != 0)
            {
                JsonObject current = this.items[this.rand.Next(0, count)] as JsonObject;
                current["OrderAmount"] = current["OrderAmount"].ReadAs<int>(0) + 10;
            }
        }
    }
}
