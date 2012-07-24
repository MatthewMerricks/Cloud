// <copyright file="TextTransformationResult.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ServiceModel.Interop.ConfigurationWizard.Services
{
    /// <summary>
    /// Class which holds the result of a T4 transformation as well as any errors
    /// </summary>
    public class TextTransformationResult
    {
        /// <summary>
        /// Initializes a new instance of the TextTransformationResult class with content
        /// </summary>
        /// <param name="content">Result of the T4 transformation</param>
        /// <param name="hasErrors">True if errors were found</param>
        /// <param name="filename">Name of output file (not used)</param>
        internal TextTransformationResult(string content, bool hasErrors, string filename)
        {
            this.HasErrors = hasErrors;
            this.Content = content;
            this.FileName = filename;
        }

        /// <summary>
        /// Gets a value indicating whether or not errors were found
        /// </summary>
        public bool HasErrors { get; private set; }

        /// <summary>
        /// Gets and sets the Content property
        /// </summary>
        public string Content { get; private set; }

        /// <summary>
        /// Gets and sets the Filename property
        /// </summary>
        public string FileName { get; private set; }
    }
}

