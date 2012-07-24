// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.ServiceModel.Channels
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.ServiceModel;
    using System.ServiceModel.Channels;
    using Microsoft.ApplicationServer.Common;

    internal abstract class LayeredChannelListener<TChannel> : ChannelListenerBase<TChannel>
        where TChannel : class, IChannel
    {
        private IChannelListener innerChannelListener;
        private bool sharedInnerListener;
        private EventHandler onInnerListenerFaulted;

        protected LayeredChannelListener(bool sharedInnerListener, IDefaultCommunicationTimeouts timeouts, IChannelListener innerChannelListener)
            : base(timeouts)
        {
            this.sharedInnerListener = sharedInnerListener;
            this.innerChannelListener = innerChannelListener;
            this.onInnerListenerFaulted = new EventHandler(this.OnInnerListenerFaulted);
            if (this.innerChannelListener != null)
            {
                this.innerChannelListener.Faulted += this.onInnerListenerFaulted;
            }
        }

        protected LayeredChannelListener(bool sharedInnerListener, IDefaultCommunicationTimeouts timeouts)
            : this(sharedInnerListener, timeouts, null)
        {
        }

        protected LayeredChannelListener(bool sharedInnerListener)
            : this(sharedInnerListener, null, null)
        {
        }

        protected LayeredChannelListener(IDefaultCommunicationTimeouts timeouts, IChannelListener innerChannelListener)
            : this(false, timeouts, innerChannelListener)
        {
        }

        public override Uri Uri
        {
            get { return this.GetInnerListenerSnapshot().Uri; }
        }

        internal virtual IChannelListener InnerChannelListener
        {
            get
            {
                return this.innerChannelListener;
            }

            set
            {
                lock (ThisLock)
                {
                    ThrowIfDisposedOrImmutable();
                    if (this.innerChannelListener != null)
                    {
                        this.innerChannelListener.Faulted -= this.onInnerListenerFaulted;
                    }

                    this.innerChannelListener = value;
                    if (this.innerChannelListener != null)
                    {
                        this.innerChannelListener.Faulted += this.onInnerListenerFaulted;
                    }
                }
            }
        }

        internal bool SharedInnerListener
        {
            get { return this.sharedInnerListener; }
        }

        [SuppressMessage(FxCop.Category.Design, FxCop.Rule.GenericMethodsShouldProvideTypeParameter, Justification = "Existing public API")]
        public override T GetProperty<T>()
        {
            T baseProperty = base.GetProperty<T>();
            if (baseProperty != null)
            {
                return baseProperty;
            }

            IChannelListener channelListener = this.InnerChannelListener;
            if (channelListener != null)
            {
                return channelListener.GetProperty<T>();
            }
            else
            {
                return default(T);
            }
        }

        internal void ThrowIfInnerListenerNotSet()
        {
            if (this.InnerChannelListener == null)
            {
                throw Fx.Exception.AsError(
                    new InvalidOperationException(
                        SR.InnerListenerFactoryNotSet(this.GetType().ToString())));
            }
        }

        internal IChannelListener GetInnerListenerSnapshot()
        {
            IChannelListener innerListener = this.InnerChannelListener;

            if (innerListener == null)
            {
                throw Fx.Exception.AsError(
                    new InvalidOperationException(
                        SR.InnerListenerFactoryNotSet(this.GetType().ToString())));
            }

            return innerListener;
        }

        protected override void OnOpening()
        {
            base.OnOpening();
            this.ThrowIfInnerListenerNotSet();
        }

        protected override void OnOpen(TimeSpan timeout)
        {
            if (this.InnerChannelListener != null && !this.sharedInnerListener)
            {
                this.InnerChannelListener.Open(timeout);
            }
        }

        protected override void OnEndOpen(IAsyncResult result)
        {
            OpenAsyncResult.End(result);
        }

        protected override IAsyncResult OnBeginOpen(TimeSpan timeout, AsyncCallback callback, object state)
        {
            return new OpenAsyncResult(this.InnerChannelListener, this.sharedInnerListener, timeout, callback, state);
        }

        protected override void OnClose(TimeSpan timeout)
        {
            this.OnCloseOrAbort();
            if (this.InnerChannelListener != null && !this.sharedInnerListener)
            {
                this.InnerChannelListener.Close(timeout);
            }
        }

        protected override void OnEndClose(IAsyncResult result)
        {
            CloseAsyncResult.End(result);
        }

        protected override IAsyncResult OnBeginClose(TimeSpan timeout, AsyncCallback callback, object state)
        {
            this.OnCloseOrAbort();
            return new CloseAsyncResult(this.InnerChannelListener, this.sharedInnerListener, timeout, callback, state);
        }

        protected override void OnAbort()
        {
            lock (ThisLock)
            {
                this.OnCloseOrAbort();
            }

            IChannelListener channelListener = this.InnerChannelListener;
            if (channelListener != null && !this.sharedInnerListener)
            {
                channelListener.Abort();
            }
        }

        private void OnInnerListenerFaulted(object sender, EventArgs e)
        {
            // if our inner listener faulted, we should fault as well
            this.Fault();
        }

        private void OnCloseOrAbort()
        {
            IChannelListener channelListener = this.InnerChannelListener;
            if (channelListener != null)
            {
                channelListener.Faulted -= this.onInnerListenerFaulted;
            }
        }

        private class OpenAsyncResult : AsyncResult
        {
            private static AsyncCallback onOpenComplete = Fx.ThunkCallback(new AsyncCallback(OnOpenComplete));
            
            private ICommunicationObject communicationObject;
            
            public OpenAsyncResult(ICommunicationObject communicationObject, bool sharedInnerListener, TimeSpan timeout, AsyncCallback callback, object state)
                : base(callback, state)
            {
                this.communicationObject = communicationObject;

                if (this.communicationObject == null || sharedInnerListener)
                {
                    this.Complete(true);
                    return;
                }

                IAsyncResult result = this.communicationObject.BeginOpen(timeout, onOpenComplete, this);
                if (result.CompletedSynchronously)
                {
                    this.communicationObject.EndOpen(result);
                    this.Complete(true);
                }
            }

            public static void End(IAsyncResult result)
            {
                AsyncResult.End<OpenAsyncResult>(result);
            }

            private static void OnOpenComplete(IAsyncResult result)
            {
                if (result.CompletedSynchronously)
                {
                    return;
                }

                OpenAsyncResult thisPtr = (OpenAsyncResult)result.AsyncState;
                Exception exception = null;

                try
                {
                    thisPtr.communicationObject.EndOpen(result);
                }
                catch (Exception e)
                {
                    if (Fx.IsFatal(e))
                    {
                        throw;
                    }

                    exception = e;
                }

                thisPtr.Complete(false, exception);
            }
        }

        private class CloseAsyncResult : AsyncResult
        {
            private static AsyncCallback onCloseComplete = Fx.ThunkCallback(new AsyncCallback(OnCloseComplete));

            private ICommunicationObject communicationObject;
            
            public CloseAsyncResult(ICommunicationObject communicationObject, bool sharedInnerListener, TimeSpan timeout, AsyncCallback callback, object state)
                : base(callback, state)
            {
                this.communicationObject = communicationObject;

                if (this.communicationObject == null || sharedInnerListener)
                {
                    this.Complete(true);
                    return;
                }

                IAsyncResult result = this.communicationObject.BeginClose(timeout, onCloseComplete, this);

                if (result.CompletedSynchronously)
                {
                    this.communicationObject.EndClose(result);
                    this.Complete(true);
                }
            }

            public static void End(IAsyncResult result)
            {
                AsyncResult.End<CloseAsyncResult>(result);
            }

            private static void OnCloseComplete(IAsyncResult result)
            {
                if (result.CompletedSynchronously)
                {
                    return;
                }

                CloseAsyncResult thisPtr = (CloseAsyncResult)result.AsyncState;
                Exception exception = null;

                try
                {
                    thisPtr.communicationObject.EndClose(result);
                }
                catch (Exception e)
                {
                    if (Fx.IsFatal(e))
                    {
                        throw;
                    }

                    exception = e;
                }

                thisPtr.Complete(false, exception);
            }
        }
    }
}
