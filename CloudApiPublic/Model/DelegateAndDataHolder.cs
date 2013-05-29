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

    internal sealed class DelegateAndDataHolder<TData, TParam1, TParam2, TReturn> : DelegateAndDataHolderBase<TParam1, TParam2>
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

        private readonly DelegateForHolder<TData, TParam1, TParam2, TReturn> delegateToWrap;
        private readonly GenericHolder<CLError> errorToAccumulate;

        public override object Process(TParam1 param1, TParam2 param2)
        {
            return TypedProcess(param1, param2);
        }
        public TReturn TypedProcess(TParam1 param1, TParam2 param2)
        {
            if (delegateToWrap == null)
            {
                throw new CLNullReferenceException(CLExceptionCode.General_Invalid, Resources.ExceptionDelegateAndDataHolderNullDelegateToWrap);
            }

            return delegateToWrap(_data, param1, param2, errorToAccumulate);
        }

        public DelegateAndDataHolder(TData data, DelegateForHolder<TData, TParam1, TParam2, TReturn> delegateToWrap, GenericHolder<CLError> errorToAccumulate)
        {
            this._data = data;
            this.delegateToWrap = delegateToWrap;
            this.errorToAccumulate = errorToAccumulate;
        }
    }

    internal sealed class DelegateAndDataHolderVoid<TData, TParam1, TParam2> : DelegateAndDataHolderBase<TParam1, TParam2>
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

        private readonly DelegateForHolderVoid<TData, TParam1, TParam2> delegateToWrap;
        private readonly GenericHolder<CLError> errorToAccumulate;

        public override object Process(TParam1 param1, TParam2 param2)
        {
            if (delegateToWrap == null)
            {
                throw new CLNullReferenceException(CLExceptionCode.General_Invalid, Resources.ExceptionDelegateAndDataHolderNullDelegateToWrap);
            }

            delegateToWrap(_data, param1, param2, errorToAccumulate);
            return null;
        }

        public DelegateAndDataHolderVoid(TData data, DelegateForHolderVoid<TData, TParam1, TParam2> delegateToWrap, GenericHolder<CLError> errorToAccumulate)
        {
            this._data = data;
            this.delegateToWrap = delegateToWrap;
            this.errorToAccumulate = errorToAccumulate;
        }
    }

    internal sealed class DelegateAndDataHolder<TData, TParam1, TParam2, TParam3, TReturn> : DelegateAndDataHolderBase<TParam1, TParam2, TParam3>
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

        private readonly DelegateForHolder<TData, TParam1, TParam2, TParam3, TReturn> delegateToWrap;
        private readonly GenericHolder<CLError> errorToAccumulate;

        public override object Process(TParam1 param1, TParam2 param2, TParam3 param3)
        {
            return TypedProcess(param1, param2, param3);
        }
        public TReturn TypedProcess(TParam1 param1, TParam2 param2, TParam3 param3)
        {
            if (delegateToWrap == null)
            {
                throw new CLNullReferenceException(CLExceptionCode.General_Invalid, Resources.ExceptionDelegateAndDataHolderNullDelegateToWrap);
            }

            return delegateToWrap(_data, param1, param2, param3, errorToAccumulate);
        }

        public DelegateAndDataHolder(TData data, DelegateForHolder<TData, TParam1, TParam2, TParam3, TReturn> delegateToWrap, GenericHolder<CLError> errorToAccumulate)
        {
            this._data = data;
            this.delegateToWrap = delegateToWrap;
            this.errorToAccumulate = errorToAccumulate;
        }
    }

    internal sealed class DelegateAndDataHolderVoid<TData, TParam1, TParam2, TParam3> : DelegateAndDataHolderBase<TParam1, TParam2, TParam3>
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

        private readonly DelegateForHolderVoid<TData, TParam1, TParam2, TParam3> delegateToWrap;
        private readonly GenericHolder<CLError> errorToAccumulate;

        public override object Process(TParam1 param1, TParam2 param2, TParam3 param3)
        {
            if (delegateToWrap == null)
            {
                throw new CLNullReferenceException(CLExceptionCode.General_Invalid, Resources.ExceptionDelegateAndDataHolderNullDelegateToWrap);
            }

            delegateToWrap(_data, param1, param2, param3, errorToAccumulate);
            return null;
        }

        public DelegateAndDataHolderVoid(TData data, DelegateForHolderVoid<TData, TParam1, TParam2, TParam3> delegateToWrap, GenericHolder<CLError> errorToAccumulate)
        {
            this._data = data;
            this.delegateToWrap = delegateToWrap;
            this.errorToAccumulate = errorToAccumulate;
        }
    }

    internal sealed class DelegateAndDataHolder<TData, TParam1, TParam2, TParam3, TParam4, TReturn> : DelegateAndDataHolderBase<TParam1, TParam2, TParam3, TParam4>
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

        private readonly DelegateForHolder<TData, TParam1, TParam2, TParam3, TParam4, TReturn> delegateToWrap;
        private readonly GenericHolder<CLError> errorToAccumulate;

        public override object Process(TParam1 param1, TParam2 param2, TParam3 param3, TParam4 param4)
        {
            return TypedProcess(param1, param2, param3, param4);
        }
        public TReturn TypedProcess(TParam1 param1, TParam2 param2, TParam3 param3, TParam4 param4)
        {
            if (delegateToWrap == null)
            {
                throw new CLNullReferenceException(CLExceptionCode.General_Invalid, Resources.ExceptionDelegateAndDataHolderNullDelegateToWrap);
            }

            return delegateToWrap(_data, param1, param2, param3, param4, errorToAccumulate);
        }

        public DelegateAndDataHolder(TData data, DelegateForHolder<TData, TParam1, TParam2, TParam3, TParam4, TReturn> delegateToWrap, GenericHolder<CLError> errorToAccumulate)
        {
            this._data = data;
            this.delegateToWrap = delegateToWrap;
            this.errorToAccumulate = errorToAccumulate;
        }
    }

    internal sealed class DelegateAndDataHolderVoid<TData, TParam1, TParam2, TParam3, TParam4> : DelegateAndDataHolderBase<TParam1, TParam2, TParam3, TParam4>
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

        private readonly DelegateForHolderVoid<TData, TParam1, TParam2, TParam3, TParam4> delegateToWrap;
        private readonly GenericHolder<CLError> errorToAccumulate;

        public override object Process(TParam1 param1, TParam2 param2, TParam3 param3, TParam4 param4)
        {
            if (delegateToWrap == null)
            {
                throw new CLNullReferenceException(CLExceptionCode.General_Invalid, Resources.ExceptionDelegateAndDataHolderNullDelegateToWrap);
            }

            delegateToWrap(_data, param1, param2, param3, param4, errorToAccumulate);
            return null;
        }

        public DelegateAndDataHolderVoid(TData data, DelegateForHolderVoid<TData, TParam1, TParam2, TParam3, TParam4> delegateToWrap, GenericHolder<CLError> errorToAccumulate)
        {
            this._data = data;
            this.delegateToWrap = delegateToWrap;
            this.errorToAccumulate = errorToAccumulate;
        }
    }

    internal sealed class DelegateAndDataHolder<TData, TParam1, TParam2, TParam3, TParam4, TParam5, TReturn> : DelegateAndDataHolderBase<TParam1, TParam2, TParam3, TParam4, TParam5>
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

        private readonly DelegateForHolder<TData, TParam1, TParam2, TParam3, TParam4, TParam5, TReturn> delegateToWrap;
        private readonly GenericHolder<CLError> errorToAccumulate;

        public override object Process(TParam1 param1, TParam2 param2, TParam3 param3, TParam4 param4, TParam5 param5)
        {
            return TypedProcess(param1, param2, param3, param4, param5);
        }
        public TReturn TypedProcess(TParam1 param1, TParam2 param2, TParam3 param3, TParam4 param4, TParam5 param5)
        {
            if (delegateToWrap == null)
            {
                throw new CLNullReferenceException(CLExceptionCode.General_Invalid, Resources.ExceptionDelegateAndDataHolderNullDelegateToWrap);
            }

            return delegateToWrap(_data, param1, param2, param3, param4, param5, errorToAccumulate);
        }

        public DelegateAndDataHolder(TData data, DelegateForHolder<TData, TParam1, TParam2, TParam3, TParam4, TParam5, TReturn> delegateToWrap, GenericHolder<CLError> errorToAccumulate)
        {
            this._data = data;
            this.delegateToWrap = delegateToWrap;
            this.errorToAccumulate = errorToAccumulate;
        }
    }

    internal sealed class DelegateAndDataHolderVoid<TData, TParam1, TParam2, TParam3, TParam4, TParam5> : DelegateAndDataHolderBase<TParam1, TParam2, TParam3, TParam4, TParam5>
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

        private readonly DelegateForHolderVoid<TData, TParam1, TParam2, TParam3, TParam4, TParam5> delegateToWrap;
        private readonly GenericHolder<CLError> errorToAccumulate;

        public override object Process(TParam1 param1, TParam2 param2, TParam3 param3, TParam4 param4, TParam5 param5)
        {
            if (delegateToWrap == null)
            {
                throw new CLNullReferenceException(CLExceptionCode.General_Invalid, Resources.ExceptionDelegateAndDataHolderNullDelegateToWrap);
            }

            delegateToWrap(_data, param1, param2, param3, param4, param5, errorToAccumulate);
            return null;
        }

        public DelegateAndDataHolderVoid(TData data, DelegateForHolderVoid<TData, TParam1, TParam2, TParam3, TParam4, TParam5> delegateToWrap, GenericHolder<CLError> errorToAccumulate)
        {
            this._data = data;
            this.delegateToWrap = delegateToWrap;
            this.errorToAccumulate = errorToAccumulate;
        }
    }

    internal sealed class DelegateAndDataHolder<TData, TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TReturn> : DelegateAndDataHolderBase<TParam1, TParam2, TParam3, TParam4, TParam5, TParam6>
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

        private readonly DelegateForHolder<TData, TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TReturn> delegateToWrap;
        private readonly GenericHolder<CLError> errorToAccumulate;

        public override object Process(TParam1 param1, TParam2 param2, TParam3 param3, TParam4 param4, TParam5 param5, TParam6 param6)
        {
            return TypedProcess(param1, param2, param3, param4, param5, param6);
        }
        public TReturn TypedProcess(TParam1 param1, TParam2 param2, TParam3 param3, TParam4 param4, TParam5 param5, TParam6 param6)
        {
            if (delegateToWrap == null)
            {
                throw new CLNullReferenceException(CLExceptionCode.General_Invalid, Resources.ExceptionDelegateAndDataHolderNullDelegateToWrap);
            }

            return delegateToWrap(_data, param1, param2, param3, param4, param5, param6, errorToAccumulate);
        }

        public DelegateAndDataHolder(TData data, DelegateForHolder<TData, TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TReturn> delegateToWrap, GenericHolder<CLError> errorToAccumulate)
        {
            this._data = data;
            this.delegateToWrap = delegateToWrap;
            this.errorToAccumulate = errorToAccumulate;
        }
    }

    internal sealed class DelegateAndDataHolderVoid<TData, TParam1, TParam2, TParam3, TParam4, TParam5, TParam6> : DelegateAndDataHolderBase<TParam1, TParam2, TParam3, TParam4, TParam5, TParam6>
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

        private readonly DelegateForHolderVoid<TData, TParam1, TParam2, TParam3, TParam4, TParam5, TParam6> delegateToWrap;
        private readonly GenericHolder<CLError> errorToAccumulate;

        public override object Process(TParam1 param1, TParam2 param2, TParam3 param3, TParam4 param4, TParam5 param5, TParam6 param6)
        {
            if (delegateToWrap == null)
            {
                throw new CLNullReferenceException(CLExceptionCode.General_Invalid, Resources.ExceptionDelegateAndDataHolderNullDelegateToWrap);
            }

            delegateToWrap(_data, param1, param2, param3, param4, param5, param6, errorToAccumulate);
            return null;
        }

        public DelegateAndDataHolderVoid(TData data, DelegateForHolderVoid<TData, TParam1, TParam2, TParam3, TParam4, TParam5, TParam6> delegateToWrap, GenericHolder<CLError> errorToAccumulate)
        {
            this._data = data;
            this.delegateToWrap = delegateToWrap;
            this.errorToAccumulate = errorToAccumulate;
        }
    }

    internal sealed class DelegateAndDataHolder<TData, TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TParam7, TReturn> : DelegateAndDataHolderBase<TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TParam7>
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

        private readonly DelegateForHolder<TData, TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TParam7, TReturn> delegateToWrap;
        private readonly GenericHolder<CLError> errorToAccumulate;

        public override object Process(TParam1 param1, TParam2 param2, TParam3 param3, TParam4 param4, TParam5 param5, TParam6 param6, TParam7 param7)
        {
            return TypedProcess(param1, param2, param3, param4, param5, param6, param7);
        }
        public TReturn TypedProcess(TParam1 param1, TParam2 param2, TParam3 param3, TParam4 param4, TParam5 param5, TParam6 param6, TParam7 param7)
        {
            if (delegateToWrap == null)
            {
                throw new CLNullReferenceException(CLExceptionCode.General_Invalid, Resources.ExceptionDelegateAndDataHolderNullDelegateToWrap);
            }

            return delegateToWrap(_data, param1, param2, param3, param4, param5, param6, param7, errorToAccumulate);
        }

        public DelegateAndDataHolder(TData data, DelegateForHolder<TData, TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TParam7, TReturn> delegateToWrap, GenericHolder<CLError> errorToAccumulate)
        {
            this._data = data;
            this.delegateToWrap = delegateToWrap;
            this.errorToAccumulate = errorToAccumulate;
        }
    }

    internal sealed class DelegateAndDataHolderVoid<TData, TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TParam7> : DelegateAndDataHolderBase<TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TParam7>
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

        private readonly DelegateForHolderVoid<TData, TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TParam7> delegateToWrap;
        private readonly GenericHolder<CLError> errorToAccumulate;

        public override object Process(TParam1 param1, TParam2 param2, TParam3 param3, TParam4 param4, TParam5 param5, TParam6 param6, TParam7 param7)
        {
            if (delegateToWrap == null)
            {
                throw new CLNullReferenceException(CLExceptionCode.General_Invalid, Resources.ExceptionDelegateAndDataHolderNullDelegateToWrap);
            }

            delegateToWrap(_data, param1, param2, param3, param4, param5, param6, param7, errorToAccumulate);
            return null;
        }

        public DelegateAndDataHolderVoid(TData data, DelegateForHolderVoid<TData, TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TParam7> delegateToWrap, GenericHolder<CLError> errorToAccumulate)
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

    internal abstract class DelegateAndDataHolderBase<TParam1, TParam2>
    {
        public abstract object Data { get; }
        public abstract object Process(TParam1 param1, TParam2 param2);
        public void VoidProcess(TParam1 param1, TParam2 param2)
        {
            Process(param1, param2);
        }

        public static DelegateAndDataHolder<TData, TParam1, TParam2, TReturn> Create<TData, TReturn>(TData data, DelegateForHolder<TData, TParam1, TParam2, TReturn> delegateToWrap, GenericHolder<CLError> errorToAccumulate)
        {
            return new DelegateAndDataHolder<TData, TParam1, TParam2, TReturn>(data, delegateToWrap, errorToAccumulate);
        }

        public static DelegateAndDataHolderVoid<TData, TParam1, TParam2> Create<TData>(TData data, DelegateForHolderVoid<TData, TParam1, TParam2> delegateToWrap, GenericHolder<CLError> errorToAccumulate)
        {
            return new DelegateAndDataHolderVoid<TData, TParam1, TParam2>(data, delegateToWrap, errorToAccumulate);
        }
    }
    internal delegate TReturn DelegateForHolder<TData, TParam1, TParam2, TReturn>(TData data, TParam1 param1, TParam2 param2, GenericHolder<CLError> errorToAccumulate);
    internal delegate void DelegateForHolderVoid<TData, TParam1, TParam2>(TData data, TParam1 param1, TParam2 param2, GenericHolder<CLError> errorToAccumulate);

    internal abstract class DelegateAndDataHolderBase<TParam1, TParam2, TParam3>
    {
        public abstract object Data { get; }
        public abstract object Process(TParam1 param1, TParam2 param2, TParam3 param3);
        public void VoidProcess(TParam1 param1, TParam2 param2, TParam3 param3)
        {
            Process(param1, param2, param3);
        }

        public static DelegateAndDataHolder<TData, TParam1, TParam2, TParam3, TReturn> Create<TData, TReturn>(TData data, DelegateForHolder<TData, TParam1, TParam2, TParam3, TReturn> delegateToWrap, GenericHolder<CLError> errorToAccumulate)
        {
            return new DelegateAndDataHolder<TData, TParam1, TParam2, TParam3, TReturn>(data, delegateToWrap, errorToAccumulate);
        }

        public static DelegateAndDataHolderVoid<TData, TParam1, TParam2, TParam3> Create<TData>(TData data, DelegateForHolderVoid<TData, TParam1, TParam2, TParam3> delegateToWrap, GenericHolder<CLError> errorToAccumulate)
        {
            return new DelegateAndDataHolderVoid<TData, TParam1, TParam2, TParam3>(data, delegateToWrap, errorToAccumulate);
        }
    }
    internal delegate TReturn DelegateForHolder<TData, TParam1, TParam2, TParam3, TReturn>(TData data, TParam1 param1, TParam2 param2, TParam3 param3, GenericHolder<CLError> errorToAccumulate);
    internal delegate void DelegateForHolderVoid<TData, TParam1, TParam2, TParam3>(TData data, TParam1 param1, TParam2 param2, TParam3 param3, GenericHolder<CLError> errorToAccumulate);

    internal abstract class DelegateAndDataHolderBase<TParam1, TParam2, TParam3, TParam4>
    {
        public abstract object Data { get; }
        public abstract object Process(TParam1 param1, TParam2 param2, TParam3 param3, TParam4 param4);
        public void VoidProcess(TParam1 param1, TParam2 param2, TParam3 param3, TParam4 param4)
        {
            Process(param1, param2, param3, param4);
        }

        public static DelegateAndDataHolder<TData, TParam1, TParam2, TParam3, TParam4, TReturn> Create<TData, TReturn>(TData data, DelegateForHolder<TData, TParam1, TParam2, TParam3, TParam4, TReturn> delegateToWrap, GenericHolder<CLError> errorToAccumulate)
        {
            return new DelegateAndDataHolder<TData, TParam1, TParam2, TParam3, TParam4, TReturn>(data, delegateToWrap, errorToAccumulate);
        }

        public static DelegateAndDataHolderVoid<TData, TParam1, TParam2, TParam3, TParam4> Create<TData>(TData data, DelegateForHolderVoid<TData, TParam1, TParam2, TParam3, TParam4> delegateToWrap, GenericHolder<CLError> errorToAccumulate)
        {
            return new DelegateAndDataHolderVoid<TData, TParam1, TParam2, TParam3, TParam4>(data, delegateToWrap, errorToAccumulate);
        }
    }
    internal delegate TReturn DelegateForHolder<TData, TParam1, TParam2, TParam3, TParam4, TReturn>(TData data, TParam1 param1, TParam2 param2, TParam3 param3, TParam4 param4, GenericHolder<CLError> errorToAccumulate);
    internal delegate void DelegateForHolderVoid<TData, TParam1, TParam2, TParam3, TParam4>(TData data, TParam1 param1, TParam2 param2, TParam3 param3, TParam4 param4, GenericHolder<CLError> errorToAccumulate);

    internal abstract class DelegateAndDataHolderBase<TParam1, TParam2, TParam3, TParam4, TParam5>
    {
        public abstract object Data { get; }
        public abstract object Process(TParam1 param1, TParam2 param2, TParam3 param3, TParam4 param4, TParam5 param5);
        public void VoidProcess(TParam1 param1, TParam2 param2, TParam3 param3, TParam4 param4, TParam5 param5)
        {
            Process(param1, param2, param3, param4, param5);
        }

        public static DelegateAndDataHolder<TData, TParam1, TParam2, TParam3, TParam4, TParam5, TReturn> Create<TData, TReturn>(TData data, DelegateForHolder<TData, TParam1, TParam2, TParam3, TParam4, TParam5, TReturn> delegateToWrap, GenericHolder<CLError> errorToAccumulate)
        {
            return new DelegateAndDataHolder<TData, TParam1, TParam2, TParam3, TParam4, TParam5, TReturn>(data, delegateToWrap, errorToAccumulate);
        }

        public static DelegateAndDataHolderVoid<TData, TParam1, TParam2, TParam3, TParam4, TParam5> Create<TData>(TData data, DelegateForHolderVoid<TData, TParam1, TParam2, TParam3, TParam4, TParam5> delegateToWrap, GenericHolder<CLError> errorToAccumulate)
        {
            return new DelegateAndDataHolderVoid<TData, TParam1, TParam2, TParam3, TParam4, TParam5>(data, delegateToWrap, errorToAccumulate);
        }
    }
    internal delegate TReturn DelegateForHolder<TData, TParam1, TParam2, TParam3, TParam4, TParam5, TReturn>(TData data, TParam1 param1, TParam2 param2, TParam3 param3, TParam4 param4, TParam5 param5, GenericHolder<CLError> errorToAccumulate);
    internal delegate void DelegateForHolderVoid<TData, TParam1, TParam2, TParam3, TParam4, TParam5>(TData data, TParam1 param1, TParam2 param2, TParam3 param3, TParam4 param4, TParam5 param5, GenericHolder<CLError> errorToAccumulate);

    internal abstract class DelegateAndDataHolderBase<TParam1, TParam2, TParam3, TParam4, TParam5, TParam6>
    {
        public abstract object Data { get; }
        public abstract object Process(TParam1 param1, TParam2 param2, TParam3 param3, TParam4 param4, TParam5 param5, TParam6 param6);
        public void VoidProcess(TParam1 param1, TParam2 param2, TParam3 param3, TParam4 param4, TParam5 param5, TParam6 param6)
        {
            Process(param1, param2, param3, param4, param5, param6);
        }

        public static DelegateAndDataHolder<TData, TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TReturn> Create<TData, TReturn>(TData data, DelegateForHolder<TData, TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TReturn> delegateToWrap, GenericHolder<CLError> errorToAccumulate)
        {
            return new DelegateAndDataHolder<TData, TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TReturn>(data, delegateToWrap, errorToAccumulate);
        }

        public static DelegateAndDataHolderVoid<TData, TParam1, TParam2, TParam3, TParam4, TParam5, TParam6> Create<TData>(TData data, DelegateForHolderVoid<TData, TParam1, TParam2, TParam3, TParam4, TParam5, TParam6> delegateToWrap, GenericHolder<CLError> errorToAccumulate)
        {
            return new DelegateAndDataHolderVoid<TData, TParam1, TParam2, TParam3, TParam4, TParam5, TParam6>(data, delegateToWrap, errorToAccumulate);
        }
    }
    internal delegate TReturn DelegateForHolder<TData, TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TReturn>(TData data, TParam1 param1, TParam2 param2, TParam3 param3, TParam4 param4, TParam5 param5, TParam6 param6, GenericHolder<CLError> errorToAccumulate);
    internal delegate void DelegateForHolderVoid<TData, TParam1, TParam2, TParam3, TParam4, TParam5, TParam6>(TData data, TParam1 param1, TParam2 param2, TParam3 param3, TParam4 param4, TParam5 param5, TParam6 param6, GenericHolder<CLError> errorToAccumulate);

    internal abstract class DelegateAndDataHolderBase<TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TParam7>
    {
        public abstract object Data { get; }
        public abstract object Process(TParam1 param1, TParam2 param2, TParam3 param3, TParam4 param4, TParam5 param5, TParam6 param6, TParam7 param7);
        public void VoidProcess(TParam1 param1, TParam2 param2, TParam3 param3, TParam4 param4, TParam5 param5, TParam6 param6, TParam7 param7)
        {
            Process(param1, param2, param3, param4, param5, param6, param7);
        }

        public static DelegateAndDataHolder<TData, TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TParam7, TReturn> Create<TData, TReturn>(TData data, DelegateForHolder<TData, TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TParam7, TReturn> delegateToWrap, GenericHolder<CLError> errorToAccumulate)
        {
            return new DelegateAndDataHolder<TData, TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TParam7, TReturn>(data, delegateToWrap, errorToAccumulate);
        }

        public static DelegateAndDataHolderVoid<TData, TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TParam7> Create<TData>(TData data, DelegateForHolderVoid<TData, TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TParam7> delegateToWrap, GenericHolder<CLError> errorToAccumulate)
        {
            return new DelegateAndDataHolderVoid<TData, TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TParam7>(data, delegateToWrap, errorToAccumulate);
        }
    }
    internal delegate TReturn DelegateForHolder<TData, TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TParam7, TReturn>(TData data, TParam1 param1, TParam2 param2, TParam3 param3, TParam4 param4, TParam5 param5, TParam6 param6, TParam7 param7, GenericHolder<CLError> errorToAccumulate);
    internal delegate void DelegateForHolderVoid<TData, TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TParam7>(TData data, TParam1 param1, TParam2 param2, TParam3 param3, TParam4 param4, TParam5 param5, TParam6 param6, TParam7 param7, GenericHolder<CLError> errorToAccumulate);
}