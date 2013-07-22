using CallingAllPublicMethods.Popups;
using CallingAllPublicMethods.ViewModels;
using Cloud;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CallingAllPublicMethods.Models.AllocateSyncboxActions
{
    public sealed class AllocAndInitAction : AllocateSyncboxAction
    {
        public static readonly AllocAndInitAction Instance = new AllocAndInitAction();

        public override void Process(AllocateSyncboxViewModel viewModel, CLCredentials credentials)
        {
            if (AllocateSyncboxAction.CheckProcessParameters(viewModel, credentials))
            {
                // must reset last dialog result otherwise popup will only ever be openable once
                viewModel.AllocPopupDialogResult = null;

                AllocSyncboxByIdPopup allocPopup = new AllocSyncboxByIdPopup()
                {
                    DataContext = viewModel
                };

                //// no need to do anything based on allocResult since the alloc command first updated the view model
                //
                //Nullable<bool> allocResult =
                    allocPopup.ShowDialog();
            }
        }

        private AllocAndInitAction() : base("AllocAndInit") { }
    }
}