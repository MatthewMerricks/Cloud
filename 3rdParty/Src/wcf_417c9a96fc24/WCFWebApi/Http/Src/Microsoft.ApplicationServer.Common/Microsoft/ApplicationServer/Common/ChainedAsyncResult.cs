//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace Microsoft.ApplicationServer.Common
{
    using System;
using System.Diagnostics.CodeAnalysis;

    public delegate IAsyncResult ChainedBeginHandler(TimeSpan timeout, AsyncCallback asyncCallback, object state);
    public delegate void ChainedEndHandler(IAsyncResult result);

    class ChainedAsyncResult : AsyncResult
    {
        ChainedBeginHandler begin2;
        ChainedEndHandler end1;
        ChainedEndHandler end2;
        TimeoutHelper timeoutHelper;
        static AsyncCallback begin1Callback = new AsyncCallback(Begin1Callback);
        static AsyncCallback begin2Callback = new AsyncCallback(Begin2Callback);

        protected ChainedAsyncResult(TimeSpan timeout, AsyncCallback callback, object state)
            : base(callback, state)
        {
            this.timeoutHelper = new TimeoutHelper(timeout);
        }

        public ChainedAsyncResult(TimeSpan timeout, AsyncCallback callback, object state, ChainedBeginHandler begin1, ChainedEndHandler end1, ChainedBeginHandler begin2, ChainedEndHandler end2)
            : base(callback, state)
        {
            this.timeoutHelper = new TimeoutHelper(timeout);
            Begin(begin1, end1, begin2, end2);
        }

        protected void Begin(ChainedBeginHandler beginOne, ChainedEndHandler endOne, ChainedBeginHandler beginTwo, ChainedEndHandler endTwo)
        {
            this.end1 = endOne;
            this.begin2 = beginTwo;
            this.end2 = endTwo;

            IAsyncResult result = beginOne(this.timeoutHelper.RemainingTime(), begin1Callback, this);
            if (!result.CompletedSynchronously)
                return;

            if (Begin1Completed(result))
            {
                this.Complete(true);
            }
        }

        [SuppressMessage(FxCop.Category.Design, FxCop.Rule.DoNotCatchGeneralExceptionTypes, Justification = "Exception is wrapped for later use.", Scope = "Member", Target = "Microsoft.ApplicationServer.Common.ChainedAsyncResult.#Begin1Callback(System.IAsyncResult)")]
        static void Begin1Callback(IAsyncResult result)
        {
            if (result.CompletedSynchronously)
                return;

            ChainedAsyncResult thisPtr = (ChainedAsyncResult)result.AsyncState;

            bool completeSelf = false;
            Exception completeException = null;

            try
            {
                completeSelf = thisPtr.Begin1Completed(result);
            }
            catch (Exception exception)
            {
                completeSelf = true;
                completeException = exception;
            }

            if (completeSelf)
            {
                thisPtr.Complete(false, completeException);
            }
        }

        bool Begin1Completed(IAsyncResult result)
        {
            end1(result);

            result = begin2(this.timeoutHelper.RemainingTime(), begin2Callback, this);
            if (!result.CompletedSynchronously)
            {
                return false;
            }

            end2(result);
            return true;
        }

        [SuppressMessage(FxCop.Category.Design, FxCop.Rule.DoNotCatchGeneralExceptionTypes, Justification = "Exception is wrapped for later use.", Scope = "Member", Target = "Microsoft.ApplicationServer.Common.ChainedAsyncResult.#Begin2Callback(System.IAsyncResult)")]
        static void Begin2Callback(IAsyncResult result)
        {
            if (result.CompletedSynchronously)
                return;

            ChainedAsyncResult thisPtr = (ChainedAsyncResult)result.AsyncState;

            Exception completeException = null;

            try
            {
                thisPtr.end2(result);
            }
            catch (Exception exception)
            {
                completeException = exception;
            }

            thisPtr.Complete(false, completeException);
        }

        public static void End(IAsyncResult result)
        {
            AsyncResult.End<ChainedAsyncResult>(result);
        }
    }
}
