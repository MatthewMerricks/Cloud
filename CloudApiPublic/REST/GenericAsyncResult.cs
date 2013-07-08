//
// GenericAsyncResult.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using System;
using System.Threading;

internal class GenericAsyncResult<T> : IAsyncResult, IDisposable
{
    private readonly AsyncCallback _aCallback;
    private bool _completed;
    private bool _sCompleted;
    private readonly object _aState;
    private readonly object _internalState;
    private readonly ManualResetEvent _aWait = new ManualResetEvent(false);
    private T _aResult;
    private Exception _aError;
    private readonly object _syncRoot = new object();

    internal GenericAsyncResult(
        AsyncCallback aCallback,
        object aState,
        object internalState = null)
    {
        this._aCallback = aCallback;
        this._aState = aState;
        this._internalState = internalState;
    }

    #region IAsyncResult Members

    public object AsyncState
    {
        get
        {
            return this._aState;
        }
    }

    public WaitHandle AsyncWaitHandle
    {
        get
        {
            return this._aWait;
        }
    }

    public bool CompletedSynchronously
    {
        get
        {
            lock (this._syncRoot)
            {
                return this._sCompleted;
            }
        }
    }

    public bool IsCompleted
    {
        get
        {
            lock (this._syncRoot)
            {
                return this._completed;
            }
        }
    }

    #endregion

    #region IDisposable Members

    public void Dispose()
    {
        this.Dispose(true);
        GC.SuppressFinalize(this);
    }

    #endregion

    internal object InternalState
    {
        get
        {
            return _internalState;
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            lock (this._syncRoot)
            {
                if (this._aWait != null)
                {
                    ((IDisposable)this._aWait).Dispose();
                }
            }
        }
    }

    internal Exception Exception
    {
        get
        {
            lock (this._syncRoot)
            {
                return this._aError;
            }
        }
    }

    internal T Result
    {
        get
        {
            lock (this._syncRoot)
            {
                return this._aResult;
            }
        }
    }

    internal void Complete(
        T aResult,
        bool sCompleted)
    {
        lock (this._syncRoot)
        {
            this._completed = true;
            this._sCompleted = sCompleted;
            this._aResult = aResult;
        }

        this.SignalCompletion();
    }

    internal void HandleException(Exception aError,
        bool sCompleted)
    {
        lock (this._syncRoot)
        {
            this._completed = true;
            this._sCompleted = sCompleted;
            this._aError = aError;
        }

        this.SignalCompletion();
    }

    private void SignalCompletion()
    {
        this._aWait.Set();

        ThreadPool.QueueUserWorkItem(new WaitCallback(this.InvokeACallback));
    }

    private void InvokeACallback(object state)
    {
        if (this._aCallback != null)
        {
            this._aCallback(this);
        }
    }
}