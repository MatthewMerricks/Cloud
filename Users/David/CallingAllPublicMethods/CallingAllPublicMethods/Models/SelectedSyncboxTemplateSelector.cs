using CallingAllPublicMethods.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace CallingAllPublicMethods.Models
{
    public sealed class SelectedSyncboxTemplateSelector : DataTemplateSelector
    {
        public DataTemplate NullSyncboxTemplate { get; set; }
        public DataTemplate ValidSyncboxTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            SyncboxViewModel castItem = item as SyncboxViewModel;
            if (castItem == null
                || castItem.SelectedSyncbox == null)
            {
                return NullSyncboxTemplate;
            }
            return ValidSyncboxTemplate;
        }
    }
}