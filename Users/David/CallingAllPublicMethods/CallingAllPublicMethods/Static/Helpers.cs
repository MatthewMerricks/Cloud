using Cloud;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;

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

        public static bool CheckForValidCredentials(CLCredentials credentials)
        {
            if (credentials == null)
            {
                MessageBox.Show("Unable to process AllocateSyncboxAction because credentials is null");
                return false;
            }

            if (credentials.IsSessionCredentials())
            {
                bool checkValid;
                CLError checkValidError = credentials.IsValid(out checkValid);

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

            return true;
        }

        #region DialogResultAttachedProperty

        private const string DialogResultPropertyName = "DialogResult";
        public static readonly DependencyProperty DialogResultProperty =
            DependencyProperty.RegisterAttached(
                DialogResultPropertyName,
                propertyType: typeof(bool?),
                ownerType: typeof(Helpers),
                defaultMetadata: new FrameworkPropertyMetadata(DialogResultChanged));

        private static void DialogResultChanged(
            DependencyObject d,
            DependencyPropertyChangedEventArgs e)
        {
            Window castD = d as Window;
            if (castD != null)
            {
                Nullable<bool> tempDialogResult;
                castD.DialogResult = tempDialogResult = e.NewValue as Nullable<bool>;

                if (tempDialogResult != null)
                {
                    castD.Close();
                }
            }
        }

        public static void SetDialogResult(Window target, Nullable<bool> value)
        {
            target.SetValue(DialogResultProperty, value);
        }

        #endregion
    }
}