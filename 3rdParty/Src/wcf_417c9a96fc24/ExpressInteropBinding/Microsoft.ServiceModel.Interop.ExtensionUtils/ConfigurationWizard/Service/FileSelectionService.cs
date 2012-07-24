// <copyright file="FileSelectionService.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ServiceModel.Interop.ConfigurationWizard.Services
{
    using Microsoft.Win32;

    /// <summary>
    /// File selection service
    /// </summary>
    public interface IFileSelectionService
    {
        /// <summary>
        /// Selects a file from the file system using the given file filter
        /// </summary>
        /// <param name="filter">File filter</param>
        /// <returns>Path to the selected file</returns>
        string SelectFile(string filter);
    }

    /// <summary>
    /// File selection service implementation
    /// </summary>
    public class FileSelectionService : IFileSelectionService
    {
        /// <summary>
        /// Selects a file from the file system using the given file filter
        /// </summary>
        /// <param name="filter">File filter</param>
        /// <returns>Path to the selected file</returns>
        public string SelectFile(string filter)
        {
            var dialog = new OpenFileDialog();
            dialog.Multiselect = false;
            dialog.Filter = filter;

            if (dialog.ShowDialog().GetValueOrDefault())
            {
                return dialog.FileName;
            }

            return null;
        }
    }
}

