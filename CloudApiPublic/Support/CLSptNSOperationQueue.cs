//
//  CLSptNSOperationQueue.cs
//  Cloud SDK Windows
//
//  Created by BobS.
//  Copyright (c) Cloud.com. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CloudApiPublic.Model;

namespace CloudApiPublic.Support
{
    public class CLSptNSOperationQueue : IDisposable
    {
        private LinkedList<CLSptNSOperation> _operationQueue = null;
        private int _numberOfActiveOperations = 0;
        private bool _disposed = false;
        private ReaderWriterLockSlim _disposeLocker = new ReaderWriterLockSlim();

        private bool _isSuspended;
        public bool IsSuspended
        {
            get 
            {
                bool rc;
                lock (_operationQueue)
                {
                    rc = _isSuspended;
                }

                return rc;
            }
            set
            {
                lock (_operationQueue)
                {
                    _isSuspended = value;
                    if (!_isSuspended)
                    {
                        // There may be some work to process.
                        Dispatcher();
                    }
                }
            }
        }
        
        private int _maxConcurrentTasks = 0;
        public int MaxConcurrentTasks
        {
            get
            {
                int rc;
                lock (_operationQueue)
                {
                    rc = _maxConcurrentTasks;
                }
                return rc;
            }
            set
            {
                lock (_operationQueue)
                {
                    _maxConcurrentTasks = value;
                }
            }
        }

        public int OperationCount
        {
            get
            {
                int count;
                lock (_operationQueue)
                {
                    count = _operationQueue.Count;
                }
                return count;
            }
        }
        
        public CLSptNSOperationQueue() : this(4)
        {
        }

        public CLSptNSOperationQueue(int maxConcurrentTasks)
        {
            _operationQueue = new LinkedList<CLSptNSOperation>();
            lock (_operationQueue)
            {
                MaxConcurrentTasks = maxConcurrentTasks;
            }
        }

        public void EnqueueOperation(CLSptNSOperation operation)
        {
            _disposeLocker.EnterReadLock();
            try
            {
                if (!_disposed)
                {
                    lock (_operationQueue)
                    {
                        _operationQueue.AddLast(operation);
                    }
                    Dispatcher();
                }
            }
            finally
            {
                _disposeLocker.ExitReadLock();
            }
        }

        public bool CancelOperation(CLSptNSOperation operation)
        {
            CLSptNSOperation foundOperation = null;
            _disposeLocker.EnterReadLock();
            try
            {
                if (!_disposed)
                {
                    lock (_operationQueue)
                    {
                        operation.Cancel();
                    }
                    Dispatcher();
                }
            }
            finally
            {
                _disposeLocker.ExitReadLock();
            }
            return foundOperation != null ? true : false;
        }

        public void CancelAllOperations()
        {
            _disposeLocker.EnterReadLock();
            try
            {
                if (!_disposed)
                {
                    lock (_operationQueue)
                    {
                        foreach (CLSptNSOperation operationIndex in _operationQueue)
                        {
                            if (operationIndex.Executing)
                            {
                                operationIndex.Cancel();
                            }
                        }
                    }
                    Dispatcher();
                }
            }
            finally
            {
                _disposeLocker.ExitReadLock();
            }
        }

        public void WaitUntilFinished()
        {
            while (true)
            {
                bool isFinished = true;

                _disposeLocker.EnterReadLock();
                try
                {
                    if (!_disposed)
                    {
                        lock (_operationQueue)
                        {
                            foreach (CLSptNSOperation operationIndex in _operationQueue)
                            {
                                if (operationIndex.Executing)
                                {
                                    isFinished = false;
                                }
                            }
                        }
                    }
                }
                finally
                {
                    _disposeLocker.ExitReadLock();
                }

                if (isFinished)
                {
                    break;
                }

                //Task.Delay(100);
                Thread.Sleep(100);
            }
        }

        public void AddOperations(List<CLSptNSOperation> operations)
        {
            _disposeLocker.EnterReadLock();
            try
            {
                if (!_disposed)
                {
                    lock (_operationQueue)
                    {
                        foreach (CLSptNSOperation operationIndex in operations)
                        {
                            _operationQueue.AddLast(operationIndex);
                        }
                    }
                    Dispatcher();
                }
            }
            finally
            {
                _disposeLocker.ExitReadLock();
            }
        }

        private void Dispatcher()
        {
            lock (_operationQueue)
            {
                foreach(CLSptNSOperation operationIndex in _operationQueue)
                {
                    if (_numberOfActiveOperations >= _maxConcurrentTasks)
                    {
                        break;
                    }
                    if (!operationIndex.Executing)
                    {
                        RunOperationAsync(operationIndex);
                    }
                }
            }
        }

        private void RunOperationAsync(CLSptNSOperation operation)
        {
            operation.Executing = true;
            _numberOfActiveOperations++;
            //await Task.Run(() =>
            //{
            //});
            (new Thread(() =>
                {
                    CLError mainError = operation.Main();

                    CLHTTPConnectionOperation subClassed = operation as CLHTTPConnectionOperation;
                    if (subClassed != null
                        && subClassed.CompletionBlock != null)
                    {
                        subClassed.CompletionBlock(subClassed, mainError);
                    }
                    //if (operation.CompletionBlock != null)
                    //{
                    //    operation.CompletionBlock();
                    //}
                })).Start();
            

            lock (_operationQueue)
            {
                operation.Executing = false;
                if (_numberOfActiveOperations > 0)
                {
                    _numberOfActiveOperations--;
                }
                _operationQueue.Remove(operation);
            }
            Dispatcher();
        }

        // Leave out the finalizer altogether if this class doesn't own unmanaged
        // resources itself.
        //~CLSptNSOperation() 
        //{ 
        //    Dispose(false); 
        //} 

        public void Dispose()
        {
            _disposeLocker.EnterWriteLock();
            try
            {
                if (!_disposed)
                {
                    // Free managed resources, if any
                    if (_operationQueue != null)
                    {
                        CancelAllOperations();
                        _operationQueue = null;
                    }

                    // Free native (unmanaged) resources, if any
                }
                _disposed = true;
                GC.SuppressFinalize(this);
            }
            finally
            {
                _disposeLocker.ExitWriteLock();
            }
        }
    }
}
