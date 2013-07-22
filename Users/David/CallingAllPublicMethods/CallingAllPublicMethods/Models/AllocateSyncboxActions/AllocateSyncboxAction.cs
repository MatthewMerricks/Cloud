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

        // CheckProcessParameters is split into two overloads since it calls Helpers.CheckForValidCredentials which has an optional parameter: we do not wish to accidentally apply a different default value

        protected static bool CheckProcessParameters(AllocateSyncboxViewModel viewModel, CLCredentials credentials, bool disallowSessionCredentials)
        {
            if (!CheckViewModel(viewModel))
            {
                return false;
            }

            return Helpers.CheckForValidCredentials(credentials, disallowSessionCredentials);
        }

        protected static bool CheckProcessParameters(AllocateSyncboxViewModel viewModel, CLCredentials credentials)
        {
            if (!CheckViewModel(viewModel))
            {
                return false;
            }

            return Helpers.CheckForValidCredentials(credentials);
        }

        private static bool CheckViewModel(AllocateSyncboxViewModel viewModel)
        {
            if (viewModel == null)
            {
                MessageBox.Show("Unable to process AllocateSyncboxAction because viewModel is null");
                return false;
            }
            return true;
        }

        public abstract void Process(AllocateSyncboxViewModel viewModel, CLCredentials credentials);

        protected AllocateSyncboxAction(string Name)
        {
            this._name = Name;
        }
    }
}