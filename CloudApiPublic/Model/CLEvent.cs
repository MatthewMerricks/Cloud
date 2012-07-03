//
//  CLEvent.cs
//  Cloud Windows
//
//  Created by BobS.
//  Copyright (c) Cloud.com. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CloudApiPublic.Model
{
    public class CLEvent
    {
        private CLMetadata _metadata;
        public CLMetadata Metadata
        {
            get
            {
                return _metadata;
            }
            set
            {
                _metadata = value;
            }
        }

        private CLSyncHeader _syncHeader;
        public CLSyncHeader SyncHeader
        {
            get
            {
                return _syncHeader;
            }
            set
            {
                _syncHeader = value;
            }
        }

        private string _action;
        public string Action
        {
            get
            {
                return _action;
            }
            set
            {
                _action = value;
            }
        }

        private bool _isMDSEvent;
        public bool IsMDSEvent
        {
            get
            {
                return _isMDSEvent;
            }
            set
            {
                _isMDSEvent = value;
            }
        }

        private int _retryAttempts;
        public int RetryAttempts
        {
            get { return _retryAttempts; }
            set { _retryAttempts = value; }
        }
        

        public CLEvent()
        {
        }

        public void LogEvent()
        {
            //NSDictionary syncHeader;
            //NSDictionary Myevent;
            //NSDictionary action;
            //NSDictionary metadata = CLMetadata.DictionaryFromMetadataItem(this.Metadata);
            //if (this.IsMDSEvent == false) {
            //    action = NSDictionary.DictionaryWithObjectForKey(this.Action, "event");
            //    Myevent = NSDictionary.DictionaryWithObjectsAndKeys(action, "event", metadata, "metadata", null);
            //}
            //else {
            //    if (this.SyncHeader != null) {
            //        syncHeader = NSDictionary.DictionaryWithObjectsAndKeys(this.SyncHeader.Action, "event", this.SyncHeader.EventID, "event_id", this.
            //          SyncHeader.Sid, "sid", this.SyncHeader.Status, "status", null);
            //        Myevent = NSDictionary.DictionaryWithObjectsAndKeys(metadata, "metadata", syncHeader, "sync_header", null);
            //    }
            //    else {
            //        Myevent = NSDictionary.DictionaryWithObjectsAndKeys(metadata, "metadata", null);
            //    }

            //}

            //Console.WriteLine("%@", Myevent);
        }

        public static void LogEvents(Array events)
        {
            //events.EnumerateObjectsUsingBlock(^ (object obj, NSUInteger idx, bool stop) {
            //    Console.WriteLine("\n\n\n");
            //    CLEvent Myevent = obj;
            //    if (Myevent.IsMDSEvent) {
            //        Console.WriteLine("MDS Events:");
            //    }
            //    else {
            //        Console.WriteLine("FSM Events:");
            //    }

            //    Myevent.LogEvent();
            //    Console.WriteLine("\n\n\n");
            //});
        }

        //+ (NSDictionary *)fsmDictionaryForCLEvents:(NSArray *)events
        public static Dictionary<string, object> FsmDictionaryForCLEvents(List<CLEvent> events)
        {
            //__block NSMutableArray *dictArray = [NSMutableArray array];
    
            //[events enumerateObjectsUsingBlock:^(id obj, NSUInteger idx, BOOL *stop) {

            //CLEvent *event = obj;

            //    if (event.isMDSEvent == NO) {
            
            //        NSDictionary *metadata = [CLMetadata dictionaryFromMetadataItem:event.metadata];
            //        NSDictionary *dicEvent = [NSDictionary dictionaryWithObjectsAndKeys:metadata, CLSyncEventMetadata, event.action, CLSyncEvent, nil];
            //        [dictArray addObject:dicEvent];
            //    }
            //}];

            //return [NSDictionary dictionaryWithObject:dictArray forKey:CLSyncEvents];

            List<object> dictArray = new List<object>();
            events.ForEach(obj =>
            {
                CLEvent evt = obj;

                if (!evt.IsMDSEvent) 
                {
                    Dictionary<string, object> metadata = new Dictionary<string,object>(CLMetadata.DictionaryFromMetadataItem(evt.Metadata));
                    Dictionary<string, object> dicEvent = new Dictionary<string, object>()
                    {
                        {CLDefinitions.CLSyncEventMetadata, metadata},
                        {CLDefinitions.CLSyncEvent, evt.Action},
                    };
                    dictArray.Add(dicEvent);
                }
            });

            return new Dictionary<string, object>()
            {
                {CLDefinitions.CLSyncEvents, dictArray}
            };
        }

        public static CLEvent EventFromMDSEvent(Dictionary<string, object> mdsEvent)
        {
            //CLEvent Myevent = new CLEvent();
            //Myevent.IsMDSEvent = true;
            //CLMetadata mdsEventMetadata = new CLMetadata(mdsEvent.ObjectForKey("metadata"));
            //Myevent.Metadata = mdsEventMetadata;
            //CLSyncHeader syncHeader = new CLSyncHeader();
            //Myevent.SyncHeader = syncHeader;
            //Myevent.SyncHeader.Action = (mdsEvent.ObjectForKey("sync_header")).ObjectForKey("event");
            //Myevent.SyncHeader.EventID = (mdsEvent.ObjectForKey("sync_header")).ObjectForKey("event_id");
            //Myevent.SyncHeader.Sid = (mdsEvent.ObjectForKey("sync_header")).ObjectForKey("sid");
            //Myevent.SyncHeader.Status = (mdsEvent.ObjectForKey("sync_header")).ObjectForKey("status");
            //return Myevent;
            return new CLEvent();
        }

    }
}
