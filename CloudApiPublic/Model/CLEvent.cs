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

        public static Dictionary<string, object> FsmDictionaryForCLEvents(Array events)
        {
            //NSMutableArray dictArray = NSMutableArray.Array();
            //events.EnumerateObjectsUsingBlock(^ (object obj, NSUInteger idx, bool stop) {
            //    CLEvent Myevent = obj;
            //    if (Myevent.IsMDSEvent == false) {
            //        NSDictionary metadata = CLMetadata.DictionaryFromMetadataItem(Myevent.Metadata);
            //        NSDictionary dicEvent = NSDictionary.DictionaryWithObjectsAndKeys(metadata, CLSyncEventMetadata, Myevent.Action, CLSyncEvent, null);
            //        dictArray.AddObject(dicEvent);
            //    }

            //});
            //return NSDictionary.DictionaryWithObjectForKey(dictArray, CLSyncEvents);
            return new Dictionary<string, object>();
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
