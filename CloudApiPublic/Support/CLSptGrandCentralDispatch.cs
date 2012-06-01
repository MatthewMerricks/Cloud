//
//  CLSptGrandCentralDispatch.cs
//  Cloud SDK Windows
//
//  Created by BobS.
//  Copyright (c) Cloud.com. All rights reserved.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace CloudApiPublic.Support
{
    public struct DispatchAction 
    {  
        public Action action;  
        public DispatchActionType type;  
        public DispatchAction(Action a, DispatchActionType t) 
        {    
            action = a;
            type = t;
        }
    }

    public enum DispatchActionType 
    {  
        Async,  
        Sync
    };
        
    public class DispatchQueue 
    {    
        private Thread _thread;  
        private Queue<DispatchAction> _queue;  
        private Mutex mut;  
        public DispatchQueue() 
        {    
            _queue = new Queue<DispatchAction>();    
            _thread = new Thread(new ThreadStart(RunLoop));    
            mut = new Mutex();  
        }  
        private void RunLoop() 
        {    
            while (_queue.Count > 0) 
            {      
                mut.WaitOne();
                DispatchAction a = _queue.Dequeue();
                mut.ReleaseMutex();
                a.action();
            }
        }
            
        public void AddAction(Action a, DispatchActionType t) 
        {    
            mut.WaitOne();    
            _queue.Enqueue(new DispatchAction(a, t));
            if (!_thread.IsAlive)
            {
                _thread.Start();
            }
            mut.ReleaseMutex();
        }
    }
        
    public class Dispatch 
    {  
        public static DispatchQueue Queue_Create() 
        {    
            return new DispatchQueue();  
        }  
            
        public static void Async(DispatchQueue q, Action a) 
        {    
            q.AddAction(a, DispatchActionType.Async);  
        }
    }
}
