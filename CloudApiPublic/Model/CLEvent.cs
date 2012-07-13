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
        public CLMetadata Metadata
        {
            get
            {
                if (_metadata == null)
                {
                    if (this.GetCloudPath != null
                        && this.GetLastSyncId != null)
                    {
                        _metadata = new CLMetadata(this.GetLastSyncId, this.GetCloudPath);
                        _metadata.ChangeReference = this.ChangeReference;
                    }
                }
                return _metadata;
            }
        }
        private CLMetadata _metadata = null;
        public CLSyncHeader SyncHeader { get; set; }
        public string Action { get; set; }
        public bool IsMDSEvent { get; set; }
        public int RetryAttempts { get; set; }
        public FileChange ChangeReference
        {
            get
            {
                return _changeReference;
            }
            set
            {
                if (_metadata != null)
                {
                    _metadata.ChangeReference = value;
                }
                this.ChangeReference = value;
            }
        }
        private FileChange _changeReference = null;
        private Func<string> GetCloudPath = null;
        private Func<string> GetLastSyncId = null;

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

            List<Dictionary<string, object>> dictArray = new List<Dictionary<string, object>>();
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
