// <copyright file="T4Invoker.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ServiceModel.Interop.ConfigurationWizard.Services
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Text;
    using Microsoft.VisualStudio.TextTemplating;
    using Microsoft.VisualStudio.TextTemplating.VSHost;

    /// <summary>
    /// T4Invoker provides a simple mechanism to invoke the T4 engine given
    /// a template file, a string/string Dictionary containing substitutions and
    /// returns the results and/or error messages
    /// </summary>
    public class T4Invoker : ITextTemplatingCallback
    {
        /// <summary>
        /// List of error messages (if any) resulting from the T4 transformation
        /// </summary>
        private List<string> errorMessages = new List<string>();

        /// <summary>
        /// Errors are collected into a single string and returned in the result value
        /// </summary>
        private string errors = string.Empty;

        /// <summary>
        /// Initializes a new instance of the T4Invoker class
        /// </summary>
        /// <param name="serviceProvider">IServiceProvider for Visual Studio.  You can use MEF to import this into your FeatureCommand or ValueProvider.</param>
        public T4Invoker(IServiceProvider serviceProvider)
        {
            this.ServiceProvider = serviceProvider;
        }

        /// <summary>
        /// Gets and sets the VS Service Provider from which we find the ITextTemplating service
        /// </summary>
        internal IServiceProvider ServiceProvider { get; private set; }

        /// <summary>
        /// Invokes T4 engine on provided file passing Dictionary values for substitution
        /// </summary>
        /// <param name="templateName">T4 template name</param>
        /// <param name="templateFileContent">T4 template content</param>
        /// <param name="fields">String/String Dictionary which T4 contents can use for substitutions</param>
        /// <returns>TextTransformationResult object with HasErrors, Content and Filename properties</returns>
        public TextTransformationResult TransformText(string templateName, string templateFileContent, IDictionary<string, string> fields)
        {
            if (fields == null)
            {
                throw new ArgumentNullException("fields");
            }

            // Get the T4 engine from VS
            ITextTemplating textTemplating = this.ServiceProvider.GetService(typeof(STextTemplating)) as ITextTemplating;

            this.errorMessages.Clear();
            textTemplating.BeginErrorSession();

            // Initialize the T4 host so we can transfer the Dictionary contents
            // into the new app domain in which the host runs
            ITextTemplatingSessionHost host = textTemplating as ITextTemplatingSessionHost;

            host.Session = host.CreateSession();

            foreach (string s in fields.Keys)
            {
                host.Session[s] = fields[s];
            }

            var content = textTemplating.ProcessTemplate(templateName, templateFileContent, this, null);
            textTemplating.EndErrorSession();

            return new TextTransformationResult(content, content.StartsWith("ErrorGeneratingOutput", true, CultureInfo.InvariantCulture), String.Empty);
        }

        /// <summary>
        /// Callback provided for T4 to report errors
        /// </summary>
        /// <param name="warning">True if error is a warning</param>
        /// <param name="message">Error text</param>
        /// <param name="line">Line number within the T4 that generated the error</param>
        /// <param name="column">Column number within the line that generated the error</param>
        public void ErrorCallback(bool warning, string message, int line, int column)
        {
            if (string.IsNullOrEmpty(message))
            {
                throw new ArgumentNullException("message");
            }

            if (!warning && message.Contains(typeof(TextTransformationException).FullName))
            {
                throw new TextTransformationException(message);
            }

            string s = string.Format(CultureInfo.InvariantCulture, "{0}: {1}. Line: {2} Column: {3}", warning ? "warning" : "error", message, line, column);
            this.errors += "\r\n" + s;
            this.errorMessages.Add(s);
        }

        /// <summary>
        /// Required callback for T4.  Since we do not output a file, we do nothing here
        /// </summary>
        /// <param name="extension">Extension for output file</param>
        public void SetFileExtension(string extension)
        {
        }

        /// <summary>
        /// Required callback for T4.  We do nothing with this.
        /// </summary>
        /// <param name="encoding">Encoding for output file</param>
        /// <param name="fromOutputDirective">True if this is from an Output directive</param>
        public void SetOutputEncoding(Encoding encoding, bool fromOutputDirective)
        {
        }
    }
}

