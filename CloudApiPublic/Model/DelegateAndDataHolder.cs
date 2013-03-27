//
// DelegateAndDataHolder.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using Cloud.Static;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Cloud.Model
{
    internal sealed class DelegateAndDataHolder<TData, TReturn> : DelegateAndDataHolder
    {
        public override TData Data
        {
            get
            {
                return _data;
            }
        }
        private readonly TData _data;

        private readonly DelegateForHolder<TData, TReturn> delegateToWrap;
        private readonly GenericHolder<CLError> errorToAccumulate;

        public override TReturn Process()
        {
            if (delegateToWrap == null)
            {
                throw new NullReferenceException("delegateToWrap cannot be processed when null");
            }

            return delegateToWrap(_data, errorToAccumulate);
        }

        public DelegateAndDataHolder(TData data, DelegateForHolder<TData, TReturn> delegateToWrap, GenericHolder<CLError> errorToAccumulate)
        {
            this._data = data;
            this.delegateToWrap = delegateToWrap;
            this.errorToAccumulate = errorToAccumulate;
        }
    }

    internal sealed class DelegateAndDataHolderVoid<TData> : DelegateAndDataHolder
    {
        public override TData Data
        {
            get
            {
                return _data;
            }
        }
        private readonly TData _data;

        private readonly DelegateForHolderVoid<TData> delegateToWrap;
        private readonly GenericHolder<CLError> errorToAccumulate;

        public override object Process()
        {
            if (delegateToWrap == null)
            {
                throw new NullReferenceException("delegateToWrap cannot be processed when null");
            }

            delegateToWrap(_data, errorToAccumulate);
            return null;
        }

        public DelegateAndDataHolderVoid(TData data, DelegateForHolderVoid<TData> delegateToWrap, GenericHolder<CLError> errorToAccumulate)
        {
            this._data = data;
            this.delegateToWrap = delegateToWrap;
            this.errorToAccumulate = errorToAccumulate;
        }
    }

    internal abstract class DelegateAndDataHolder
    {
        public abstract object Data { get; }
        public abstract object Process();

        public static DelegateAndDataHolder<TData, TReturn> Create<TData, TReturn>(TData data, DelegateForHolder<TData, TReturn> delegateToWrap, GenericHolder<CLError> errorToAccumulate)
        {
            return new DelegateAndDataHolder<TData, TReturn>(data, delegateToWrap, errorToAccumulate);
        }

        public static DelegateAndDataHolderVoid<TData> Create<TData>(TData data, DelegateForHolderVoid<TData> delegateToWrap, GenericHolder<CLError> errorToAccumulate)
        {
            return new DelegateAndDataHolderVoid<TData>(data, delegateToWrap, errorToAccumulate);
        }
    }
    internal delegate TReturn DelegateForHolder<TData, TReturn>(TData data, GenericHolder<CLError> errorToAccumulate);
    internal delegate void DelegateForHolderVoid<TData>(TData data, GenericHolder<CLError> errorToAccumulate);
}