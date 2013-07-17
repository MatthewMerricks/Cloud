using CallingAllPublicMethods.Static;
using CallingAllPublicMethods.ViewModels;
using Cloud;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace CallingAllPublicMethods.Models.AllocateSyncboxActions
{
    public abstract class AllocateSyncboxAction
    {
        public string Name
        {
            get
            {
                return _name;
            }
        }
        private readonly string _name;

        protected static bool CheckProcessParameters(AllocateSyncboxViewModel viewModel, CLCredentials credentials)
        {
            if (viewModel == null)
            {
                MessageBox.Show("Unable to process AllocateSyncboxAction because viewModel is null");
                return false;
            }

            return Helpers.CheckForValidCredentials(credentials);
        }

        public abstract void Process(AllocateSyncboxViewModel viewModel, CLCredentials credentials);

        protected AllocateSyncboxAction(string Name)
        {
            this._name = Name;
        }
    }
}