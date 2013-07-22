using CallingAllPublicMethods.Popups;
using CallingAllPublicMethods.ViewModels;
using Cloud;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
            // no longer need to disallow this action for session credentials:
            // able to allow listing syncboxes for session via combination: [CLCredentials].IsValid to output SyncboxIds
            // and [CLSyncbox].AllocAndInit once for each SyncboxId
            if (AllocateSyncboxAction.CheckProcessParameters(viewModel, credentials)) // , disallowSessionCredentials: true
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
            // no longer need to disallow this action for session credentials:
            // able to allow listing syncboxes for session via combination: [CLCredentials].IsValid to output SyncboxIds
            // and [CLSyncbox].AllocAndInit once for each SyncboxId
            if (AllocateSyncboxAction.CheckProcessParameters(viewModel, credentials)) // , disallowSessionCredentials: true
            {
                ReadOnlyCollection<long> alternateSyncboxIdsForSessionCredentials = null;

                if (credentials.IsSessionCredentials())
                {
                    bool isValid;
                    DateTime expirationDate;
                    credentials.IsValid(out isValid, out expirationDate, out alternateSyncboxIdsForSessionCredentials);
                }

                if (alternateSyncboxIdsForSessionCredentials == null)
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
                        AddSyncboxesToViewModelList(viewModel, listedSyncboxes);
                    }
                }
                // for session credentials, ListAllSyncboxes it not authorized, so instead use all SyncboxIds to AllocAndInit seperate syncboxes
                else if (alternateSyncboxIdsForSessionCredentials.Count == 1
                    ||  MessageBox.Show(
                        string.Format(
                            string.Join(" ",
                                new[]
                                {
                                    "ListAllSyncboxes is not authorized for session credentials.",
                                    "A seperate query has to be performed to list each known syncbox by its id.",
                                    "This will take {0} seperate queries to the server.",
                                    "Do you still wish to continue?"
                                }),
                            alternateSyncboxIdsForSessionCredentials.Count),
                        string.Format("Perform {0} seperate queries?", alternateSyncboxIdsForSessionCredentials.Count),
                        MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    List<CLSyncbox> allocAndInitSyncboxes = new List<CLSyncbox>();
                    using (IEnumerator<long> currentSyncboxIdEnumerator = alternateSyncboxIdsForSessionCredentials.GetEnumerator())
                    {
                        while (currentSyncboxIdEnumerator.MoveNext())
                        {
                            CLSyncbox currentAllocAndInitSyncbox;
                            CLError allocAndInitSyncboxError = CLSyncbox.AllocAndInit(
                                currentSyncboxIdEnumerator.Current,
                                credentials,
                                out currentAllocAndInitSyncbox,
                                settings: new CLSyncSettings(viewModel.ModifyableDeviceId));

                            if (allocAndInitSyncboxError == null)
                            {
                                allocAndInitSyncboxes.Add(currentAllocAndInitSyncbox);
                            }
                            else
                            {
                                allocAndInitSyncboxes = null; // marks to not add syncboxes to refresh list since at least one failed
                                MessageBox.Show(string.Format(
                                    "An error occurred on AllocAndInit of a syncbox to list for session credentials. SyncboxId: {0}. Exception code: {1}. Error Message: {2}.",
                                    currentSyncboxIdEnumerator.Current,
                                    allocAndInitSyncboxError.PrimaryException.Code,
                                    allocAndInitSyncboxError.PrimaryException.Message));
                                break; // once a failure has occurred, stop AllocAndInit iterations
                            }
                        }
                    }

                    if (allocAndInitSyncboxes != null)
                    {
                        AddSyncboxesToViewModelList(viewModel, allocAndInitSyncboxes);
                    }
                }
            }
        }

        private static void AddSyncboxesToViewModelList(AllocateSyncboxViewModel viewModel, IEnumerable<CLSyncbox> listedSyncboxes)
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

        private ListSyncboxesAction() : base("ListAllSyncboxes") { }
    }
}