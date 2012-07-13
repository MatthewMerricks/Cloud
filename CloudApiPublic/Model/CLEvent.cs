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
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using CloudApiPublic.Static;

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
            set
            {
                this.ChangeReference = (value == null
                    ? null
                    : value.ChangeReference);
                _metadata = value;
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

        public static CLEvent EventFromMDSEvent(Func<string> getLastSyncId, Func<string> getCloudPath, Dictionary<string, object> mdsEvent, SyncDirection direction)
        {
            // Merged 7/12/12
            // CLEvent *event = [[CLEvent alloc] init];
            // event.isMDSEvent = YES;
    
            // CLMetadata *mdsEventMetadata = [[CLMetadata alloc] initWithDictionary:[mdsEvent objectForKey:@"metadata"]];
            // event.metadata = mdsEventMetadata;
    
            // CLSyncHeader *syncHeader = [[CLSyncHeader alloc] init];
            // event.syncHeader = syncHeader;
            // event.syncHeader.action = [[mdsEvent objectForKey:@"sync_header"] objectForKey:@"event"];
            // event.syncHeader.eventID = [[mdsEvent objectForKey:@"sync_header"] objectForKey:@"event_id"];
            // event.syncHeader.sid = [[mdsEvent objectForKey:@"sync_header"] objectForKey:@"sid"];
            // event.syncHeader.status = [[mdsEvent objectForKey:@"sync_header"] objectForKey:@"status"];
    
            // if ([event.syncHeader.action rangeOfString:CLEventTypeFolderRange].location != NSNotFound) {
            //     event.metadata.isDirectory = YES;
            // }else {
            //     event.metadata.isDirectory = NO;
            // }
 
            // return event;
            //&&&&

            // CLEvent *event = [[CLEvent alloc] init];
            // event.isMDSEvent = YES;
            CLEvent evt = new CLEvent();
            evt.IsMDSEvent = true;
            evt.GetLastSyncId = getLastSyncId;
            evt.GetCloudPath = getCloudPath;
            
            // CLSyncHeader *syncHeader = [[CLSyncHeader alloc] init];
            // event.syncHeader = syncHeader;
            // event.syncHeader.action = [[mdsEvent objectForKey:@"sync_header"] objectForKey:@"event"];
            // event.syncHeader.eventID = [[mdsEvent objectForKey:@"sync_header"] objectForKey:@"event_id"];
            // event.syncHeader.sid = [[mdsEvent objectForKey:@"sync_header"] objectForKey:@"sid"];
            // event.syncHeader.status = [[mdsEvent objectForKey:@"sync_header"] objectForKey:@"status"];
            Dictionary<string, object> syncHeaderDictionary = ((JToken)mdsEvent[CLDefinitions.CLSyncEventHeader]).ToObject<Dictionary<string, object>>();
            CLSyncHeader syncHeader = new CLSyncHeader();
            evt.SyncHeader = syncHeader;
            evt.SyncHeader.Action = (string)syncHeaderDictionary.GetValueOrDefault(CLDefinitions.CLSyncEvent, String.Empty);
            evt.SyncHeader.EventID = (string)syncHeaderDictionary.GetValueOrDefault(CLDefinitions.CLSyncEventID, String.Empty);
            evt.SyncHeader.Sid = (string)syncHeaderDictionary.GetValueOrDefault(CLDefinitions.CLSyncID, String.Empty);
            evt.SyncHeader.Status = (string)syncHeaderDictionary.GetValueOrDefault(CLDefinitions.CLSyncEventStatus, String.Empty);

            // CLMetadata *mdsEventMetadata = [[CLMetadata alloc] initWithDictionary:[mdsEvent objectForKey:@"metadata"]];
            // event.metadata = mdsEventMetadata;
            CLMetadata mdsEventMetadata = new CLMetadata(evt.GetLastSyncId,
                evt.GetCloudPath,
                ((JToken)mdsEvent[CLDefinitions.CLSyncEventMetadata]).ToObject<Dictionary<string, object>>(),
                syncHeader,
                direction);
            evt.Metadata = mdsEventMetadata;

            // if ([event.syncHeader.action rangeOfString:CLEventTypeFolderRange].location != NSNotFound) {
            if (evt.SyncHeader.Action.Contains(CLDefinitions.CLEventTypeFolderRange))
            {
                // event.metadata.isDirectory = YES;
                evt.ChangeReference.Metadata.HashableProperties = new FileMetadataHashableProperties(true,//only property changed, is folder is now true
                    evt.ChangeReference.Metadata.HashableProperties.LastTime,// copied
                    evt.ChangeReference.Metadata.HashableProperties.CreationTime,// copied
                    evt.ChangeReference.Metadata.HashableProperties.Size);// copied
            }
            else
            {
                // event.metadata.isDirectory = NO;
                evt.ChangeReference.Metadata.HashableProperties = new FileMetadataHashableProperties(false,//only property changed, is folder is now false
                    evt.ChangeReference.Metadata.HashableProperties.LastTime,// copied
                    evt.ChangeReference.Metadata.HashableProperties.CreationTime,// copied
                    evt.ChangeReference.Metadata.HashableProperties.Size);// copied
            }

            // return event;
            return evt;
        }

    }
}
