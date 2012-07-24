// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Http
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Microsoft.ApplicationServer.Common;

    /// <summary>
    /// Extension methods to allow strongly typed objects to be read from <see cref="HttpContent"/> instances.
    /// </summary>
    public static class HttpContentExtensionMethods
    {
        /// <summary>
        /// Returns an object of the specified type from the <paramref name="content"/> instance.
        /// </summary>
        /// <param name="content">The <see cref="HttpContent"/> instance from which to read.</param>
        /// <param name="type">The type of the object to read.</param>
        /// <returns>An object instance of the specified type.</returns>
        public static object ReadAs(this HttpContent content, Type type)
        {
            if (content == null)
            {
                throw Fx.Exception.ArgumentNull("content");
            }

            using (ObjectContent objectContent = new ObjectContent(type, content))
            {
                return objectContent.ReadAs();
            }
        }

        /// <summary>
        /// Returns an object of the specified type from the <paramref name="content"/> instance.
        /// </summary>
        /// <param name="content">The <see cref="HttpContent"/> instance from which to read.</param>
        /// <param name="type">The type of the object to read.</param>
        /// <param name="formatters">The collection of <see cref="MediaTypeFormatter"/> instances to use.</param>
        /// <returns>An object instance of the specified type.</returns>
        public static object ReadAs(this HttpContent content, Type type, IEnumerable<MediaTypeFormatter> formatters)
        {
            if (content == null)
            {
                throw Fx.Exception.ArgumentNull("content");
            }

            using (ObjectContent objectContent = new ObjectContent(type, content, formatters))
            {
                return objectContent.ReadAs();
            }
        }

        /// <summary>
        /// Returns a <see cref="Task"/> that will yield an object of the specified type
        /// from the <paramref name="content"/> instance.
        /// </summary>
        /// <param name="content">The <see cref="HttpContent"/> instance from which to read.</param>
        /// <param name="type">The type of the object to read.</param>
        /// <returns>A <see cref="Task"/> that will yield an object instance of the specified type.</returns>
        public static Task<object> ReadAsAsync(this HttpContent content, Type type)
        {
            if (content == null)
            {
                throw Fx.Exception.ArgumentNull("content");
            }

            ObjectContent objectContent = new ObjectContent(type, content);
            return objectContent.ReadAsAsync().ContinueWith<object>((t) => { objectContent.Dispose(); return t.Result; });
        }

        /// <summary>
        /// Returns a <see cref="Task"/> that will yield an object of the specified type
        /// from the <paramref name="content"/> instance.
        /// </summary>
        /// <param name="content">The <see cref="HttpContent"/> instance from which to read.</param>
        /// <param name="type">The type of the object to read.</param>
        /// <param name="formatters">The collection of <see cref="MediaTypeFormatter"/> instances to use.</param>
        /// <returns>A <see cref="Task"/> that will return an object instance of the specified type.</returns>
        public static Task<object> ReadAsAsync(this HttpContent content, Type type, IEnumerable<MediaTypeFormatter> formatters)
        {
            if (content == null)
            {
                throw Fx.Exception.ArgumentNull("content");
            }

            ObjectContent objectContent = new ObjectContent(type, content, formatters);
            return objectContent.ReadAsAsync().ContinueWith<object>((t) => { objectContent.Dispose(); return t.Result; });
        }

        /// <summary>
        /// Returns an object or default value of the specified type from the <paramref name="content"/> instance.
        /// </summary>
        /// <param name="content">The <see cref="HttpContent"/> instance from which to read.</param>
        /// <param name="type">The type of the object to read.</param>
        /// <returns>An object instance of the specified type or the default value for that type 
        /// if it was not possible to read the object from the <paramref name="content"/>.</returns>
        public static object ReadAsOrDefault(this HttpContent content, Type type)
        {
            if (content == null)
            {
                throw Fx.Exception.ArgumentNull("content");
            }

            using (ObjectContent objectContent = new ObjectContent(type, content))
            {
                return objectContent.ReadAsOrDefault();
            }
        }

        /// <summary>
        /// Returns an object or default value of the specified type from the <paramref name="content"/> instance.
        /// </summary>
        /// <param name="content">The <see cref="HttpContent"/> instance from which to read.</param>
        /// <param name="type">The type of the object to read.</param>
        /// <param name="formatters">The collection of <see cref="MediaTypeFormatter"/> instances to use.</param>
        /// <returns>An object instance of the specified type or the default value for that type 
        /// if it was not possible to read the object from the <paramref name="content"/>.</returns>
        public static object ReadAsOrDefault(this HttpContent content, Type type, IEnumerable<MediaTypeFormatter> formatters)
        {
            if (content == null)
            {
                throw Fx.Exception.ArgumentNull("content");
            }

            using (ObjectContent objectContent = new ObjectContent(type, content, formatters))
            {
                return objectContent.ReadAsOrDefault();
            }
        }

        /// <summary>
        /// Returns a <see cref="Task"/> that will yield an object or default value
        /// of the specified type from the <paramref name="content"/> instance.
        /// </summary>
        /// <param name="content">The <see cref="HttpContent"/> instance from which to read.</param>
        /// <param name="type">The type of the object to read.</param>
        /// <returns>A <see cref="Task"/> that will yield an object instance of the specified type or the
        /// default value for that type if it was not possible to read the object from the <paramref name="content"/>.
        /// </returns>
        public static Task<object> ReadAsOrDefaultAsync(this HttpContent content, Type type)
        {
            if (content == null)
            {
                throw Fx.Exception.ArgumentNull("content");
            }

            ObjectContent objectContent = new ObjectContent(type, content);
            return objectContent.ReadAsOrDefaultAsync().ContinueWith<object>((t) => { objectContent.Dispose(); return t.Result; });
        }

        /// <summary>
        /// Returns a <see cref="Task"/> that will yield an object or default value
        /// of the specified type from the <paramref name="content"/> instance.
        /// </summary>
        /// <param name="content">The <see cref="HttpContent"/> instance from which to read.</param>
        /// <param name="type">The type of the object to read.</param>
        /// <param name="formatters">The collection of <see cref="MediaTypeFormatter"/> instances to use.</param>
        /// <returns>A <see cref="Task"/> that will yield an object instance of the specified type or the
        /// default value for that type if it was not possible to read the object from the <paramref name="content"/>.
        /// </returns>
        public static Task<object> ReadAsOrDefaultAsync(this HttpContent content, Type type, IEnumerable<MediaTypeFormatter> formatters)
        {
            if (content == null)
            {
                throw Fx.Exception.ArgumentNull("content");
            }

            ObjectContent objectContent = new ObjectContent(type, content, formatters);
            return objectContent.ReadAsOrDefaultAsync().ContinueWith<object>((t) => { objectContent.Dispose(); return t.Result; });
        }

        /// <summary>
        /// Returns an object of the specified type <typeparamref name="T"/> 
        /// from the <paramref name="content"/> instance.
        /// </summary>
        /// <typeparam name="T">The type of the object to read.</typeparam>
        /// <param name="content">The <see cref="HttpContent"/> instance from which to read.</param>
        /// <returns>An object instance of the specified type.</returns>
        public static T ReadAs<T>(this HttpContent content)
        {
            if (content == null)
            {
                throw Fx.Exception.ArgumentNull("content");
            }

            using (ObjectContent<T> objectContent = new ObjectContent<T>(content))
            {
                return objectContent.ReadAs();
            }
        }

        /// <summary>
        /// Returns an object of the specified type <typeparamref name="T"/> 
        /// from the <paramref name="content"/> instance.
        /// </summary>
        /// <typeparam name="T">The type of the object to read.</typeparam>
        /// <param name="content">The <see cref="HttpContent"/> instance from which to read.</param>
        /// <param name="formatters">The collection of <see cref="MediaTypeFormatter"/> instances to use.</param>
        /// <returns>An object instance of the specified type.</returns>
        public static T ReadAs<T>(this HttpContent content, IEnumerable<MediaTypeFormatter> formatters)
        {
            if (content == null)
            {
                throw Fx.Exception.ArgumentNull("content");
            }

            using (ObjectContent<T> objectContent = new ObjectContent<T>(content, formatters))
            {
                return objectContent.ReadAs();
            }
        }

        /// <summary>
        /// Returns a <see cref="Task"/> that will yield an object of the specified 
        /// type <typeparamref name="T"/> from the <paramref name="content"/> instance.
        /// </summary>
        /// <typeparam name="T">The type of the object to read.</typeparam>
        /// <param name="content">The <see cref="HttpContent"/> instance from which to read.</param>
        /// <returns>An object instance of the specified type.</returns>
        public static Task<T> ReadAsAsync<T>(this HttpContent content)
        {
            if (content == null)
            {
                throw Fx.Exception.ArgumentNull("content");
            }

            ObjectContent<T> objectContent = new ObjectContent<T>(content);
            return objectContent.ReadAsAsync().ContinueWith<T>((t) => { objectContent.Dispose(); return t.Result; });
        }

        /// <summary>
        /// Returns a <see cref="Task"/> that will yield an object of the specified 
        /// type <typeparamref name="T"/> from the <paramref name="content"/> instance.
        /// </summary>
        /// <typeparam name="T">The type of the object to read.</typeparam>
        /// <param name="content">The <see cref="HttpContent"/> instance from which to read.</param>
        /// <param name="formatters">The collection of <see cref="MediaTypeFormatter"/> instances to use.</param>
        /// <returns>An object instance of the specified type.</returns>
        public static Task<T> ReadAsAsync<T>(this HttpContent content, IEnumerable<MediaTypeFormatter> formatters)
        {
            if (content == null)
            {
                throw Fx.Exception.ArgumentNull("content");
            }

            ObjectContent<T> objectContent = new ObjectContent<T>(content, formatters);
            return objectContent.ReadAsAsync().ContinueWith<T>((t) => { objectContent.Dispose(); return t.Result; });
        }

        /// <summary>
        /// Returns an object or default value of the specified type <typeparamref name="T"/> 
        /// from the <paramref name="content"/> instance.
        /// </summary>
        /// <typeparam name="T">The type of the object to read.</typeparam>
        /// <param name="content">The <see cref="HttpContent"/> instance from which to read.</param>
        /// <returns>An object instance of the specified type or the default value of that type
        /// if it was not possible to read the object from the <paramref name="content"/>.
        /// </returns>
        public static T ReadAsOrDefault<T>(this HttpContent content)
        {
            if (content == null)
            {
                throw Fx.Exception.ArgumentNull("content");
            }

            using (ObjectContent<T> objectContent = new ObjectContent<T>(content))
            {
                return objectContent.ReadAsOrDefault();
            }
        }

        /// <summary>
        /// Returns an object or default value of the specified type <typeparamref name="T"/> 
        /// from the <paramref name="content"/> instance.
        /// </summary>
        /// <typeparam name="T">The type of the object to read.</typeparam>
        /// <param name="content">The <see cref="HttpContent"/> instance from which to read.</param>
        /// <param name="formatters">The collection of <see cref="MediaTypeFormatter"/> instances to use.</param>
        /// <returns>An object instance of the specified type or the default value of that type
        /// if it was not possible to read the object from the <paramref name="content"/>.
        /// </returns>
        public static T ReadAsOrDefault<T>(this HttpContent content, IEnumerable<MediaTypeFormatter> formatters)
        {
            if (content == null)
            {
                throw Fx.Exception.ArgumentNull("content");
            }

            using (ObjectContent<T> objectContent = new ObjectContent<T>(content, formatters))
            {
                return objectContent.ReadAsOrDefault();
            }
        }

        /// <summary>
        /// Returns a <see cref="Task"/> that will yield an object or default value
        /// of the specified type <typeparamref name="T"/> 
        /// from the <paramref name="content"/> instance.
        /// </summary>
        /// <typeparam name="T">The type of the object to read.</typeparam>
        /// <param name="content">The <see cref="HttpContent"/> instance from which to read.</param>
        /// <returns>A <see cref="Task"/> that will yield object instance of the specified type or
        /// the default value of that type if it was not possible to read from the <paramref name="content"/>.
        /// </returns>
        public static Task<T> ReadAsOrDefaultAsync<T>(this HttpContent content)
        {
            if (content == null)
            {
                throw Fx.Exception.ArgumentNull("content");
            }

            ObjectContent<T> objectContent = new ObjectContent<T>(content);
            return objectContent.ReadAsOrDefaultAsync().ContinueWith<T>((t) => { objectContent.Dispose(); return t.Result; });
        }

        /// <summary>
        /// Returns a <see cref="Task"/> that will yield an object or default value
        /// of the specified type <typeparamref name="T"/> 
        /// from the <paramref name="content"/> instance.
        /// </summary>
        /// <typeparam name="T">The type of the object to read.</typeparam>
        /// <param name="content">The <see cref="HttpContent"/> instance from which to read.</param>
        /// <param name="formatters">The collection of <see cref="MediaTypeFormatter"/> instances to use.</param>
        /// <returns>A <see cref="Task"/> that will yield object instance of the specified type or
        /// the default value of that type if it was not possible to read from the <paramref name="content"/>.
        /// </returns>
        public static Task<T> ReadAsOrDefaultAsync<T>(this HttpContent content, IEnumerable<MediaTypeFormatter> formatters)
        {
            if (content == null)
            {
                throw Fx.Exception.ArgumentNull("content");
            }

            ObjectContent<T> objectContent = new ObjectContent<T>(content, formatters);
            return objectContent.ReadAsOrDefaultAsync().ContinueWith<T>((t) => { objectContent.Dispose(); return t.Result; });
        }
    }
}
