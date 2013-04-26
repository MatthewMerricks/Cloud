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
    internal sealed class DelegateAndDataHolder<TData, TReturn> : DelegateAndDataHolderBase
    {
        public TData TypedData
        {
            get
            {
                return _data;
            }
        }
        public override object Data
        {
            get
            {
                return _data;
            }
        }
        private readonly TData _data;

        private readonly DelegateForHolder<TData, TReturn> delegateToWrap;
        private readonly GenericHolder<CLError> errorToAccumulate;

        public override object Process()
        {
            return TypedProcess();
        }
        public TReturn TypedProcess()
        {
            if (delegateToWrap == null)
            {
                throw new CLNullReferenceException(CLExceptionCode.General_Invalid, Resources.ExceptionDelegateAndDataHolderNullDelegateToWrap);
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

    internal sealed class DelegateAndDataHolderVoid<TData> : DelegateAndDataHolderBase
    {
        public TData TypedData
        {
            get
            {
                return _data;
            }
        }
        public override object Data
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
                throw new CLNullReferenceException(CLExceptionCode.General_Invalid, Resources.ExceptionDelegateAndDataHolderNullDelegateToWrap);
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

    internal sealed class DelegateAndDataHolder<TData, TParam1, TReturn> : DelegateAndDataHolderBase<TParam1>
    {
        public TData TypedData
        {
            get
            {
                return _data;
            }
        }
        public override object Data
        {
            get
            {
                return _data;
            }
        }
        private readonly TData _data;

        private readonly DelegateForHolder<TData, TParam1, TReturn> delegateToWrap;
        private readonly GenericHolder<CLError> errorToAccumulate;

        public override object Process(TParam1 param1)
        {
            return TypedProcess(param1);
        }
        public TReturn TypedProcess(TParam1 param1)
        {
            if (delegateToWrap == null)
            {
                throw new CLNullReferenceException(CLExceptionCode.General_Invalid, Resources.ExceptionDelegateAndDataHolderNullDelegateToWrap);
            }

            return delegateToWrap(_data, param1, errorToAccumulate);
        }

        public DelegateAndDataHolder(TData data, DelegateForHolder<TData, TParam1, TReturn> delegateToWrap, GenericHolder<CLError> errorToAccumulate)
        {
            this._data = data;
            this.delegateToWrap = delegateToWrap;
            this.errorToAccumulate = errorToAccumulate;
        }
    }

    internal sealed class DelegateAndDataHolderVoid<TData, TParam1> : DelegateAndDataHolderBase<TParam1>
    {
        public TData TypedData
        {
            get
            {
                return _data;
            }
        }
        public override object Data
        {
            get
            {
                return _data;
            }
        }
        private readonly TData _data;

        private readonly DelegateForHolderVoid<TData, TParam1> delegateToWrap;
        private readonly GenericHolder<CLError> errorToAccumulate;

        public override object Process(TParam1 param1)
        {
            if (delegateToWrap == null)
            {
                throw new CLNullReferenceException(CLExceptionCode.General_Invalid, Resources.ExceptionDelegateAndDataHolderNullDelegateToWrap);
            }

            delegateToWrap(_data, param1, errorToAccumulate);
            return null;
        }

        public DelegateAndDataHolderVoid(TData data, DelegateForHolderVoid<TData, TParam1> delegateToWrap, GenericHolder<CLError> errorToAccumulate)
        {
            this._data = data;
            this.delegateToWrap = delegateToWrap;
            this.errorToAccumulate = errorToAccumulate;
        }
    }

    internal abstract class DelegateAndDataHolderBase
    {
        public abstract object Data { get; }
        public abstract object Process();
        public void VoidProcess()
        {
            Process();
        }

        public static DelegateAndDataHolder<TData, TReturn> Create<TData, TReturn>(TData data, DelegateForHolder<TData, TReturn> delegateToWrap, GenericHolder<CLError> errorToAccumulate)
        {
            return new DelegateAndDataHolder<TData, TReturn>(data, delegateToWrap, errorToAccumulate);
        }

        public static DelegateAndDataHolderVoid<TData> Create<TData>(TData data, DelegateForHolderVoid<TData> delegateToWrap, GenericHolder<CLError> errorToAccumulate)
        {
            return new DelegateAndDataHolderVoid<TData>(data, delegateToWrap, errorToAccumulate);
        }

        //// commented GetNullOfType methods could be used to correctly anonymously type a field which can be defined later, but I removed all such usages so I commented these out;
        //// if you add these back, add matching ones to the input parameter versions of the delegate holders
        //// -David
        //
        //public static DelegateAndDataHolder<TData, TReturn> GetNullOfType<TData, TReturn>(DelegateAndDataHolder<TData, TReturn> toNullify)
        //{
        //    return null;
        //}

        //public static DelegateAndDataHolderVoid<TData> GetNullOfType<TData>(DelegateAndDataHolderVoid<TData> toNullify)
        //{
        //    return null;
        //}
    }
    internal delegate TReturn DelegateForHolder<TData, TReturn>(TData data, GenericHolder<CLError> errorToAccumulate);
    internal delegate void DelegateForHolderVoid<TData>(TData data, GenericHolder<CLError> errorToAccumulate);

    internal abstract class DelegateAndDataHolderBase<TParam1>
    {
        public abstract object Data { get; }
        public abstract object Process(TParam1 param1);
        public void VoidProcess(TParam1 param1)
        {
            Process(param1);
        }

        public static DelegateAndDataHolder<TData, TParam1, TReturn> Create<TData, TReturn>(TData data, DelegateForHolder<TData, TParam1, TReturn> delegateToWrap, GenericHolder<CLError> errorToAccumulate)
        {
            return new DelegateAndDataHolder<TData, TParam1, TReturn>(data, delegateToWrap, errorToAccumulate);
        }

        public static DelegateAndDataHolderVoid<TData, TParam1> Create<TData>(TData data, DelegateForHolderVoid<TData, TParam1> delegateToWrap, GenericHolder<CLError> errorToAccumulate)
        {
            return new DelegateAndDataHolderVoid<TData, TParam1>(data, delegateToWrap, errorToAccumulate);
        }
    }
    internal delegate TReturn DelegateForHolder<TData, TParam1, TReturn>(TData data, TParam1 param1, GenericHolder<CLError> errorToAccumulate);
    internal delegate void DelegateForHolderVoid<TData, TParam1>(TData data, TParam1 param1, GenericHolder<CLError> errorToAccumulate);
}