//
//  CLSptGrandCentralDispatch.cs
//  Cloud SDK Windows
//
//  Created by BobS.
//  Copyright (c) Cloud.com. All rights reserved.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;
using CloudApiPublic.Model;
using System.Threading.Tasks;

/// <summary>
/// This is a set of classes meant to emulate a subset of Cocoa's Grand Central Dispatch (GCD).  We support multiple dispatch queues.
/// Tasks must take a single "in" argument and return void.  The single argument may be an arbitrary collection object, so the task's
/// function will need to unpack the collection in case multiple parameters are required.
/// </summary>
namespace CloudApiPublic.Support
{

    /// <summary>
    /// This is a non-generic wrapper required because ConcurrentQueue may contain many different types of actions.
    /// </summary>
    public abstract class DispatchActionGeneric
    {
        public abstract void Run();
    }

    /// <summary>
    /// The generic action of type T.
    /// </summary>
    public class DispatchActionGeneric<T> : DispatchActionGeneric
    {
        public Action<T> Action;
        public T UserState;
        public DispatchActionType Type;
        public TaskCompletionSource<bool> CompletionSource;
        public DispatchActionGeneric(Action<T> action, T userstate, DispatchActionType type, TaskCompletionSource<bool> completionSource = null)
        {
            this.Action = action;
            this.UserState = userstate;
            this.Type = type;
            this.CompletionSource = completionSource;
        }

        /// <summary>
        /// Run the action, which may be of any type.
        /// </summary>
        public override void Run()
        {
 	        this.Action(this.UserState);
            if (this.CompletionSource != null)
            {
                this.CompletionSource.SetResult(true);
            }
        } 
    }

    /// <summary>
    /// The dispatch action type definitions.  
    /// Async: The caller enqueues the action with the user state and continues executing.
    /// Sync:  The caller enqueues the action with the user state, and waits for that action to complete.  Note that
    /// the queue is FIFO, so many other Async or Sync actions may run before this action runs and completes.  The
    /// caller blocks until its own action completes.
    /// </summary>
    public enum DispatchActionType
    {
        Async,
        Sync
    };

    /// <summary>
    /// The FIFO action queue.
    /// </summary>
    public class DispatchQueueGeneric
    {
        private LinkedList<DispatchActionGeneric> _queue;
        private Task _task;
        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        /// <summary>
        /// The public constructor.
        /// </summary>
        public DispatchQueueGeneric() 
        {
            _queue = new LinkedList<DispatchActionGeneric>();
        }

        /// <summary>
        /// The dispatcher run loop.
        /// </summary>
        private void RunLoop() 
        {    
            LinkedListNode<DispatchActionGeneric> node;
            while (true) 
            {
                lock (_queue)
                {
                    node = null;
                    if (_task.IsCanceled)
                    {
                        _queue.Clear();
                        _task = null;
                    }
                    else
                    {
                        // Run an action if there is one
                        node = _queue.First;
                        if (node != null)
                        {
                            _queue.RemoveFirst();
                        }
                        else
                        {
                            _task = null;
                        }
                    }
                }

                if (node != null)
                {
                    // Run the action on this _task
                    node.Value.Run();
                }
                else
                {
                    return;
                }
            }
        }

        /// <summary>
        /// Cancel the dispatcher's long-running thread.  The thread will stop following the exit of any action. 
        /// Note that the thread exit may be delayed by 100 ms.
        /// </summary>
        public void Cancel()
        {
            lock (_queue)
            {
                if (_task != null)
                {
                    _cancellationTokenSource.Cancel();
                }
            }
        }

        /// <summary>
        /// Add an action to the queue.
        /// </summary>
        public void AddAction<T>(Action<T> action, T userstate, DispatchActionType type, TaskCompletionSource<bool> completionSource = null)
        {
            DispatchActionGeneric<T> dispatchAction = new DispatchActionGeneric<T>(action, userstate, type, completionSource);
            lock (_queue)
            {
                _queue.AddLast(dispatchAction);
                if (_task == null)
                {
                    _task = new Task(() => RunLoop(), _cancellationTokenSource.Token, TaskCreationOptions.LongRunning);
                    _task.Start();
                }
            }
        }
    }

    /// <summary>
    /// A static class to make it easier to add actions to the queue.
    /// </summary>
    public class Dispatch
    {
        public static DispatchQueueGeneric Queue_Create() 
        {
            return new DispatchQueueGeneric();  
        }

        /// <summary>
        /// Add an Async task.  Call as: Dispatch.Async(queue, action, userstate);
        /// </summary>
        public static void Async<T>(DispatchQueueGeneric queue, Action<T> action, T userstate)
        {
            queue.AddAction(action, userstate, DispatchActionType.Async);
        }

        /// <summary>
        /// Add a Sync task.  Call as: Dispatch.Sync(queue, action, userstate);
        /// Note that this function blocks until the action is complete.
        /// </summary>
        public static void Sync<T>(DispatchQueueGeneric queue, Action<T> action, T userstate)
        {
            TaskCompletionSource<bool> completionSource = new TaskCompletionSource<bool>();
            queue.AddAction(action, userstate, DispatchActionType.Sync, completionSource);
            Task<bool> task = completionSource.Task;
            bool rc = task.Result;                          // wait here for the queued task to complete
            return;
        }
    }
}
