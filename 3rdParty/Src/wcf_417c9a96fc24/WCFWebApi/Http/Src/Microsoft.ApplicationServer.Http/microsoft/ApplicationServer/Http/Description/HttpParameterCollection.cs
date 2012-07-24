// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Http.Description
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.ServiceModel.Description;
    using Microsoft.ApplicationServer.Common;

    /// <summary>
    /// Represents an ordered collection of <see cref="HttpParameter"/> instances.
    /// </summary>
    internal class HttpParameterCollection : IList<HttpParameter>, IEnumerable<HttpParameter>, IEnumerable
    {
        // This class operates in only one of 2 possible modes:
        //  this.operationDescription != null means it is synchronized against an OperationDescription.
        //  this.innerCollection != null means it is a stand-alone instance that is not synchronized.
        private OperationDescription operationDescription;
        private bool isOutputCollection;
        private List<HttpParameter> innerCollection;

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpParameterCollection"/> class.
        /// </summary>
        public HttpParameterCollection()
        {
            this.innerCollection = new List<HttpParameter>();
            Fx.Assert(!this.IsSynchronized, "Default ctor cannot be synchronized");
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpParameterCollection"/> class.
        /// </summary>
        /// <remarks>
        /// This form of the constructor initializes the instance from the given <paramref name="parameters"/>.
        /// </remarks>
        /// <param name="parameters">The collection whose elements are copied to the new list.</param>
        public HttpParameterCollection(IEnumerable<HttpParameter> parameters) 
        {
            if (parameters == null)
            {
                throw Fx.Exception.ArgumentNull("parameters");
            }

            this.innerCollection = new List<HttpParameter>(parameters);
            Fx.Assert(!this.IsSynchronized, "IEnumerable ctor cannot be synchronized");
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpParameterCollection"/> class.
        /// </summary>
        /// <remarks>
        /// This form of the constructor initializes the instance from the respective collection in the given
        /// <see cref="OperationDescription"/> and keeps the two instances synchronized.
        /// </remarks>
        /// <param name="operationDescription">The <see cref="OperationDescription"/>
        /// instance from which to create the collection.</param>
        /// <param name="isOutputCollection">If <c>false</c> use the input parameter collection, 
        /// otherwise use the output parameter collection.</param>
        internal HttpParameterCollection(OperationDescription operationDescription, bool isOutputCollection)
        {
            Fx.Assert(operationDescription != null, "The 'operationDescription' parameter can not be null.");
            this.operationDescription = operationDescription;
            this.isOutputCollection = isOutputCollection;
            Fx.Assert(this.IsSynchronized, "OperationDescription ctor must be synchronized");
        }

        /// <summary>
        /// Gets the number of elements contained in the collection.
        /// </summary>
        public int Count
        {
            get
            {
                if (this.IsSynchronized)
                {
                    MessagePartDescriptionCollection mpdColl = this.MessagePartDescriptionCollection;
                    return (mpdColl == null) ? 0 : mpdColl.Count;                   
                }
                else
                {
                    return this.innerCollection.Count();
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether the collection is read-only.
        /// </summary>
        public bool IsReadOnly
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is synchronized with a corresponding
        /// collection in a <see cref="OperationDescription"/>.
        /// </summary>
        private bool IsSynchronized
        {
            get
            {
                Fx.Assert(
                    (this.operationDescription != null && this.innerCollection == null) ||
                    (this.operationDescription == null && this.innerCollection != null),
                    "Inconsistent state: must be either synchronized or not");

                return this.operationDescription != null;
            }
        }

        private MessageDescription MessageDescription
        {
            get
            {
                if (this.operationDescription != null)
                {
                    int messageIndex = this.isOutputCollection ? 1 : 0;
                    HttpOperationDescription.CreateMessageDescriptionIfNecessary(this.operationDescription, messageIndex);
                    return this.operationDescription.Messages[messageIndex];
                }

                return null;
            }
        }

        /// <summary>
        /// Gets the <see cref="MessagePartDescriptionCollection"/> from the original
        /// <see cref="OperationDescription"/> instance.
        /// It returns <c>null</c> if this instance
        /// is not synchronized or the synchronized <see cref="OperationDescription"/>
        /// is incomplete and does not contain required collection.
        /// </summary>
        private MessagePartDescriptionCollection MessagePartDescriptionCollection
        {
            get
            {
                if (this.IsSynchronized)
                {
                    int messageIndex = this.isOutputCollection ? 1 : 0;
                    return (this.operationDescription.Messages.Count > messageIndex)
                        ? this.operationDescription.Messages[messageIndex].Body.Parts
                        : null;
                }

                return null;
            }
        }

        /// <summary>
        /// Gets or sets the element at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the element to get or set.</param>
        /// <returns>The item at the specified index.</returns>
        public HttpParameter this[int index]
        {
            get
            {
                if (index < 0 || (index >= this.Count))
                {
                    throw Fx.Exception.AsError(new ArgumentOutOfRangeException("index"));
                }

                if (this.IsSynchronized)
                {
                    // The Count check above will have thrown if there is no MessagePartDescriptionCollection,
                    // because Count would have been zero.
                    MessagePartDescriptionCollection messagePartDescriptions = this.MessagePartDescriptionCollection;
                    Fx.Assert(messagePartDescriptions != null, "MessagePartDescriptionCollection cannot be null");
                    return new HttpParameter(messagePartDescriptions[index]);
                }

                return this.innerCollection[index];
            }

            set
            {
                if (index < 0 || (index >= this.Count))
                {
                    throw Fx.Exception.AsError(new ArgumentOutOfRangeException("index"));
                }

                if (value == null)
                {
                    throw Fx.Exception.ArgumentNull("value");
                }

                value.SynchronizeToMessagePartDescription(this.MessageDescription);

                if (this.innerCollection != null)
                {
                    this.innerCollection[index] = value;
                }
                else
                {
                    // The Count check above will have thrown if there is no MessagePartDescriptionCollection,
                    // because Count would have been zero.
                    MessagePartDescriptionCollection mpdColl = this.MessagePartDescriptionCollection;
                    Fx.Assert(mpdColl != null, "MessagePartDescriptionCollection cannot be null");
                    mpdColl[index] = value.MessagePartDescription;
                }
            }
        }

        /// <summary>
        /// Determines the index of a specific item in the collection.
        /// </summary>
        /// <param name="item">The object to locate in the collection.</param>
        /// <returns>The index of item if found in the list; otherwise, -1.</returns>
        public int IndexOf(HttpParameter item)
        {
            if (item == null)
            {
                throw Fx.Exception.ArgumentNull("item");
            }

            if (this.IsSynchronized && item.MessagePartDescription != null)
            {
                MessagePartDescriptionCollection mpdColl = this.MessagePartDescriptionCollection;
                if (mpdColl == null)
                {
                    return -1;
                }

                return mpdColl.IndexOf(item.MessagePartDescription);
            }

            return this.innerCollection.IndexOf(item);
        }

        /// <summary>
        /// Inserts an item at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index at which item should be inserted.</param>
        /// <param name="item">The object to insert at the specified index.</param>
        public void Insert(int index, HttpParameter item)
        {
            if (item == null)
            {
                throw Fx.Exception.ArgumentNull("item");
            }

            if (index < 0 || (index > this.Count))
            {
                throw Fx.Exception.AsError(new ArgumentOutOfRangeException("index"));
            }

            item.SynchronizeToMessagePartDescription(this.MessageDescription);

            if (this.IsSynchronized)
            {
                MessagePartDescriptionCollection mpdColl = this.GetOrCreateMessagePartDescriptionCollection();
                Fx.Assert(mpdColl != null, "MessagePartDescriptionCollection cannot be null");
                mpdColl.Insert(index, item.MessagePartDescription);
            }
            else
            {
                this.innerCollection.Insert(index, item);
            }
        }

        /// <summary>
        /// Removes the item at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the item to remove,</param>
        public void RemoveAt(int index)
        {
            if (index < 0 || (index >= this.Count))
            {
                throw Fx.Exception.AsError(new ArgumentOutOfRangeException("index"));
            }

            if (this.IsSynchronized)
            {
                // The Count check above will have thrown if there is no MessagePartDescriptionCollection,
                // because Count would have been zero.
                MessagePartDescriptionCollection mpdColl = this.MessagePartDescriptionCollection;
                Fx.Assert(mpdColl != null, "MessagePartDescriptionCollection cannot be null");
                mpdColl.RemoveAt(index);
            }
            else
            {
                this.innerCollection.RemoveAt(index);
            }
        }

        /// <summary>
        /// Adds an item to the collection.
        /// </summary>
        /// <param name="item">The object to add to the collection.</param>
        public void Add(HttpParameter item)
        {
            if (item == null)
            {
                throw Fx.Exception.ArgumentNull("item");
            }

            item.SynchronizeToMessagePartDescription(this.MessageDescription);

            if (this.IsSynchronized)
            {
                MessagePartDescriptionCollection mpdColl = this.GetOrCreateMessagePartDescriptionCollection();
                mpdColl.Add(item.MessagePartDescription);
            }
            else
            {
                this.innerCollection.Add(item);
            }
        }

        /// <summary>
        /// Removes all items from the collection.
        /// </summary>
        public void Clear()
        {
            if (this.IsSynchronized)
            {
                MessagePartDescriptionCollection mpdColl = this.MessagePartDescriptionCollection;
                if (mpdColl != null)
                {
                    mpdColl.Clear();
                }
            }
            else
            {
                this.innerCollection.Clear();
            }
        }

        /// <summary>
        /// Determines whether the collection contains a specific value.
        /// </summary>
        /// <param name="item">The object to locate in the collection.</param>
        /// <returns><c>true</c> if item is found in the collection; otherwise, <c>false</c>.</returns>
        public bool Contains(HttpParameter item)
        {
            if (item == null)
            {
                throw Fx.Exception.ArgumentNull("item");
            }

            if (this.IsSynchronized && item.MessagePartDescription != null)
            {
                MessagePartDescriptionCollection mdpColl = this.MessagePartDescriptionCollection;
                if (mdpColl == null)
                {
                    return false;
                }

                // Strategy: use the knowledge we would have wrapped the MessagePartDescription in the
                // past when we released this HttpParameter.
                return mdpColl.Contains(item.MessagePartDescription);
            }

            return this.innerCollection.Contains(item);
        }

        /// <summary>
        /// Copies the elements of the collection to an <see cref="Array"/>, starting at a particular array index.
        /// </summary>
        /// <param name="array">The one-dimensional <see cref="Array"/> that is the destination of the elements copied from collection. The array must have zero-based indexing.</param>
        /// <param name="arrayIndex">The zero-based index in <paramref name="array"/> at which copying begins.</param>
        public void CopyTo(HttpParameter[] array, int arrayIndex)
        {
            if (array == null)
            {
                throw Fx.Exception.ArgumentNull("array");
            }

            if (arrayIndex < 0 || ((arrayIndex + this.Count) > array.Length))
            {
                throw Fx.Exception.AsError(new ArgumentOutOfRangeException("arrayIndex"));
            }

            if (this.IsSynchronized)
            {
                MessagePartDescriptionCollection mdpColl = this.MessagePartDescriptionCollection;
                if (mdpColl != null)
                {
                    HttpParameter[] newArray = ToArray(mdpColl);
                    Array.Copy(newArray, 0, array, arrayIndex, newArray.Length);
                }
                else if (array.Length > 0)
                {
                    // clear out the contents from the arrayIndex till the end of the array
                    Array.Clear(array, arrayIndex, array.Length - arrayIndex);
                }
            }
            else
            {
                this.innerCollection.CopyTo(array, arrayIndex);
            }
        }

        /// <summary>
        /// Removes the first occurrence of a specific object from the collection.
        /// </summary>
        /// <param name="item">The object to remove from the collection.</param>
        /// <returns><c>true</c> if <paramref name="item"/> was successfully removed from the collection; otherwise, <c>false</c>. 
        /// This method also returns <c>false</c> if <paramref name="item"/> is not found in the original collection.</returns>
        public bool Remove(HttpParameter item)
        {
            if (item == null)
            {
                throw Fx.Exception.ArgumentNull("item");
            }

            if (this.IsSynchronized && item.MessagePartDescription != null)
            {
                MessagePartDescriptionCollection mdpColl = this.MessagePartDescriptionCollection;
                if (mdpColl == null)
                {
                    return false;
                }

                return mdpColl.Remove(item.MessagePartDescription);
            }

            return this.innerCollection.Remove(item);
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>An enumerator that can be used to iterate through the collection.</returns>
        public IEnumerator<HttpParameter> GetEnumerator()
        {
            if (this.IsSynchronized)
            {
                MessagePartDescriptionCollection mdpColl = this.MessagePartDescriptionCollection;
                if (mdpColl == null)
                {
                    return Enumerable.Empty<HttpParameter>().GetEnumerator();
                }

                HttpParameter[] newArray = ToArray(mdpColl);
                return newArray.Cast<HttpParameter>().GetEnumerator();
            }

            return this.innerCollection.GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>An enumerator that can be used to iterate through the collection.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            if (this.IsSynchronized)
            {
                MessagePartDescriptionCollection messagePartDescriptions = this.MessagePartDescriptionCollection;
                if (messagePartDescriptions == null)
                {
                    return Enumerable.Empty<HttpParameter>().GetEnumerator();
                }

                HttpParameter[] newArray = ToArray(messagePartDescriptions);
                return newArray.GetEnumerator();
            }

            return this.innerCollection.GetEnumerator();
        }

        /// <summary>
        /// Creates an array of <see cref="HttpParameter"/> elements that are synchronized with their
        /// corresponding <see cref="MessagePartDescription"/> elements.
        /// </summary>
        /// <param name="messagePartDescriptionCollection">The existing collection from which to create the array.</param>
        /// <returns>The new array.</returns>
        private static HttpParameter[] ToArray(MessagePartDescriptionCollection messagePartDescriptionCollection)
        {
            return messagePartDescriptionCollection
                    .Select<MessagePartDescription, HttpParameter>(m => new HttpParameter(m))
                    .ToArray();
        }

        /// <summary>
        /// Retrieves the appropriate <see cref="MessagePartDescriptionCollection"/> for the current instance.
        /// If the synchronized <see cref="OperationDescription"/> does not have the corresponding collection,
        /// this method will create a default <see cref="MessageDescription"/> element so that the collection exists.
        /// </summary>
        /// <returns>The <see cref="MessagePartDescriptionCollection"/>.</returns>
        private MessagePartDescriptionCollection GetOrCreateMessagePartDescriptionCollection()
        {
            Fx.Assert(this.IsSynchronized, "This method cannot be called for unsynchronized collections");
            MessagePartDescriptionCollection messagePartDescriptions = this.MessagePartDescriptionCollection;
            if (messagePartDescriptions == null)
            {
                OperationDescription operationDescription = this.operationDescription;
                int messageIndex = this.isOutputCollection ? 1 : 0;
                if (operationDescription.Messages.Count <= messageIndex)
                {
                    HttpOperationDescription.CreateMessageDescriptionIfNecessary(operationDescription, messageIndex);
                }

                Fx.Assert(operationDescription.Messages.Count > messageIndex, "CreateMessageDescription should have created Message element");
                messagePartDescriptions = operationDescription.Messages[messageIndex].Body.Parts;
            }

            Fx.Assert(messagePartDescriptions != null, "return value can never be null");
            return messagePartDescriptions;
        }
    }
}
