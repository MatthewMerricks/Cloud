using CallingAllPublicMethods.Popups;
using CallingAllPublicMethods.ViewModels;
using Cloud;
using Cloud.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace CallingAllPublicMethods.Models.AllocateSyncboxActions
{
    public sealed class CreateSyncboxAction : AllocateSyncboxAction
    {
        public static readonly CreateSyncboxAction Instance = new CreateSyncboxAction();

        public override void Process(AllocateSyncboxViewModel viewModel, CLCredentials credentials)
        {
            if (AllocateSyncboxAction.CheckProcessParameters(viewModel, credentials, disallowSessionCredentials: true))
            {
                // must reset last dialog result otherwise popup will only ever be openable once
                viewModel.CreatePopupDialogResult = null;

                CreateSyncboxPlusStoragePlanListPopup creationPopup = new CreateSyncboxPlusStoragePlanListPopup()
                {
                    DataContext = viewModel
                };

                //// no need to do anything based on creationResult since the create command first updated the view model
                //
                //Nullable<bool> creationResult =
                    creationPopup.ShowDialog();
            }
        }

        public static void RefreshStoragePlans(AllocateSyncboxViewModel viewModel, CLCredentials credentials, bool defaultPlanOnly = false)
        {
            if (AllocateSyncboxAction.CheckProcessParameters(viewModel, credentials, disallowSessionCredentials: true))
            {
                CLStoragePlan[] listedStoragePlans;
                CLError listStoragePlansError;
                if (defaultPlanOnly)
                {
                    CLStoragePlan defaultPlan;
                    listStoragePlansError = CLStoragePlan.DefaultStoragePlanWithCredentials(
                        credentials,
                        out defaultPlan);
                    if (defaultPlan == null)
                    {
                        listedStoragePlans = null;
                    }
                    else
                    {
                        listedStoragePlans = new[] { defaultPlan };
                    }
                }
                else
                {
                    listStoragePlansError = CLStoragePlan.ListStoragePlansWithCredentials(
                        credentials,
                        out listedStoragePlans);
                }

                if (listStoragePlansError != null)
                {
                    MessageBox.Show(string.Format("An error occurred listing storage plans. Exception code: {0}. Error message: {1}.", listStoragePlansError.PrimaryException.Code, listStoragePlansError.PrimaryException.Message));
                }
                else
                {
                    foreach (CLStoragePlan listedStoragePlan in listedStoragePlans.OrderBy(listedStoragePlan => listedStoragePlan.PlanId))
                    {
                        CLStoragePlanProxy listedStoragePlanProxy = new CLStoragePlanProxy(listedStoragePlan);

                        CLStoragePlanProxy matchedStoragePlanProxy;
                        if (viewModel.KnownCLStoragePlansDictionary.TryGetValue(listedStoragePlan.PlanId, out matchedStoragePlanProxy))
                        {
                            for (int matchedStoragePlanIndex = 0; matchedStoragePlanIndex < viewModel.KnownCLStoragePlans.Count; matchedStoragePlanIndex++)
                            {
                                if (viewModel.KnownCLStoragePlans[matchedStoragePlanIndex] == matchedStoragePlanProxy)
                                {
                                    viewModel.KnownCLStoragePlans[matchedStoragePlanIndex] = listedStoragePlanProxy;
                                }
                            }
                        }
                        else
                        {
                            viewModel.KnownCLStoragePlans.Add(listedStoragePlanProxy);
                        }
                    }
                }
            }
        }

        private CreateSyncboxAction() : base("CreateSyncbox") { }
    }
}