using Cloud;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Interop;
using System.Windows.Threading;

namespace CallingAllPublicMethods.Static
{
    public static class Helpers
    {
        public static bool IsValidRegex(string pattern)
        {
            if (string.IsNullOrEmpty(pattern))
            {
                return false;
            }

            try
            {
                Regex.Match(string.Empty, pattern);
            }
            catch (ArgumentException)
            {
                return false;
            }

            return true;
        }

        public static bool CheckForValidCredentials(CLCredentials credentials, bool disallowSessionCredentials = false)
        {
            if (credentials == null)
            {
                MessageBox.Show("Unable to process AllocateSyncboxAction because credentials is null");
                return false;
            }

            if (credentials.IsSessionCredentials())
            {
                if (disallowSessionCredentials)
                {
                    MessageBox.Show("This action is not valid for session credentials");
                    return false;
                }
                else
                {
                    bool checkValid;
                    DateTime expirationDate;
                    ReadOnlyCollection<long> syncboxIds;
                    CLError checkValidError = credentials.IsValid(out checkValid, out expirationDate, out syncboxIds);

                    if (checkValidError != null)
                    {
                        MessageBox.Show(string.Format("Unable to check whether credentials is valid. Exception code: {0}. Error message: {1}", checkValidError.PrimaryException.Code, checkValidError.PrimaryException.Message));
                        return false;
                    }

                    if (!checkValid)
                    {
                        MessageBox.Show("credentials is expired");
                    }

                    return checkValid;
                }
            }

            return true;
        }

        public static object GetItemFromIEnumerator(System.Collections.IEnumerator toIterate, int itemIdx = 0)
        {
            return Cloud.Static.Helpers.ToIEnumerable(toIterate).Cast<object>().Skip(itemIdx).First();
        }

        public static bool TryAllocCLCredentials(string copyKey, string copySecret, string copyToken, out CLCredentials credentials)
        {
            CLError testCredentialsError = CLCredentials.AllocAndInit(
                copyKey,
                copySecret,
                out credentials,
                copyToken);

            if (testCredentialsError != null)
            {
                MessageBox.Show(
                    string.Format(
                        "Key, secret, and/or token are invalid for CLCredentials. ExceptionCode: {0}. Error message: {1}.",
                        testCredentialsError.PrimaryException.Code,
                        testCredentialsError.PrimaryException.Message));

                return false;
            }
            return true;
        }

        #region DialogResultAttachedProperty

        private const string DialogResultPropertyName = "DialogResult";
        public static readonly DependencyProperty DialogResultProperty =
            DependencyProperty.RegisterAttached(
                DialogResultPropertyName,
                propertyType: typeof(Nullable<bool>),
                ownerType: typeof(Helpers),
                defaultMetadata: new FrameworkPropertyMetadata(DialogResultChanged));

        private static void DialogResultChanged(
            DependencyObject d,
            DependencyPropertyChangedEventArgs e)
        {
            Window castD = d as Window;
            if (castD != null)
            {
                Nullable<bool> tempDialogResult = e.NewValue as Nullable<bool>;

                // can only set DialogResult if Window is being shown as modal via ShowDialog();
                // the most accurate way to determine if a Window is being shown this way is private field: [Window]._showingAsDialog,
                // but if this thread's Dispatcher is the same as the Window's then we can check ComponentDispatcher.IsThreadModal which is public;
                // fallback via a try/catch for the InvalidOperationException from setting the DialogResult property
                if (castD.DialogResult != tempDialogResult)
                {
                    if (Dispatcher.CurrentDispatcher == castD.Dispatcher)
                    {
                        if (ComponentDispatcher.IsThreadModal)
                        {
                            castD.DialogResult = e.NewValue as Nullable<bool>;
                        }
                    }
                    else
                    {
                        try
                        {
                            castD.DialogResult = e.NewValue as Nullable<bool>;
                        }
                        catch (InvalidOperationException ex)
                        {
                            MessageBox.Show(string.Format(
                                "Unable to determine whether Window was being shown a modal dialog and setting its DialogResult resulted in an error. Error message: {0}.",
                                ex.Message));
                        }
                    }
                }

                if (tempDialogResult != null)
                {
                    // set value back to itself (won't cause a changed event) which will also remove the Binding (prevents a change event after Window is disposed)
                    castD.SetValue(DialogResultProperty, castD.DialogResult);
                    castD.Close();
                }
            }
        }

        // required for WPF to recognize DialogResultProperty as a proper, attachable DependencyProperty
        public static void SetDialogResult(Window target, Nullable<bool> value)
        {
            target.SetValue(DialogResultProperty, value);
        }

        #endregion
    }
}