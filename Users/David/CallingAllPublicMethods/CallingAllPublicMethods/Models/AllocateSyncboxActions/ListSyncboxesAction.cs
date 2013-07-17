using CallingAllPublicMethods.Popups;
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
    public sealed class ListSyncboxesAction : AllocateSyncboxAction
    {
        public static readonly ListSyncboxesAction Instance = new ListSyncboxesAction();

        public override void Process(AllocateSyncboxViewModel viewModel, CLCredentials credentials)
        {
            if (AllocateSyncboxAction.CheckProcessParameters(viewModel, credentials))
            {
                // must reset last dialog result otherwise popup will only ever be openable once
                viewModel.ListPopupDialogResult = null;

                SelectSyncboxFromListPopup selectionPopup = new SelectSyncboxFromListPopup()
                {
                    DataContext = viewModel
                };

                //// no need to do anything based on selectionResult since the select command first updated the view model
                //
                //Nullable<bool> selectionResult =
                    selectionPopup.ShowDialog();
            }
        }

        public static void RefreshList(AllocateSyncboxViewModel viewModel, CLCredentials credentials)
        {
            if (AllocateSyncboxAction.CheckProcessParameters(viewModel, credentials))
            {
                CLSyncbox[] listedSyncboxes;
                CLError listSyncboxesError = CLSyncbox.ListAllSyncboxes(
                    credentials,
                    out listedSyncboxes,
                    settings: new CLSyncSettings(viewModel.ModifyableDeviceId));

                if (listSyncboxesError != null)
                {
                    MessageBox.Show(string.Format("An error occurred listing syncboxes. Exception code: {0}. Error message: {1}.", listSyncboxesError.PrimaryException.Code, listSyncboxesError.PrimaryException.Message));
                }
                else
                {
                    foreach (CLSyncbox listedSyncbox in listedSyncboxes.OrderBy(listedSyncbox => listedSyncbox.SyncboxId))
                    {
                        CLSyncboxProxy listedSyncboxProxy = new CLSyncboxProxy(listedSyncbox);

                        CLSyncboxProxy matchedSyncboxProxy;
                        if (viewModel.KnownCLSyncboxesDictionary.TryGetValue(listedSyncbox.SyncboxId, out matchedSyncboxProxy))
                        {
                            for (int matchedSyncboxIndex = 0; matchedSyncboxIndex < viewModel.KnownCLSyncboxes.Count; matchedSyncboxIndex++)
                            {
                                if (viewModel.KnownCLSyncboxes[matchedSyncboxIndex] == matchedSyncboxProxy)
                                {
                                    viewModel.KnownCLSyncboxes[matchedSyncboxIndex] = listedSyncboxProxy;
                                }
                            }
                        }
                        else
                        {
                            viewModel.KnownCLSyncboxes.Add(listedSyncboxProxy);
                        }
                    }
                }
            }
        }

        private ListSyncboxesAction() : base("Pick from existing list") { }
    }
}