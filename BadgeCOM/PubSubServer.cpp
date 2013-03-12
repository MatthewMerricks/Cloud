//
// PubSubServer.cpp
// Cloud Windows COM
//
// Created By BobS.
// Copyright (c) Cloud.com. All rights reserved.

// PubSubServer.cpp : Implementation of CPubSubServer

#include "stdafx.h"
#include "PubSubServer.h"

// Debug trace
#define CLTRACE(intPriority, szFormat, ...) Trace::getInstance()->write(intPriority, szFormat, __VA_ARGS__)

// Constants
static const char * _ksSharedMemoryName = "64BDBAC9-709B-4429-A616-A6D4661C78A2";	// the name of the shared memory segment, GUID so it won't be obvious who owns it.
static const char * _ksBaseSharedMemoryObjectName = "Base";					// the name of the single shared memory object that contains our shared memory data.
static const int _knMaxEventsInEventQueue = 1000;							// maximum number of events allowed in a subscription's event queue
static const int _knShortRetries = 5;										// number of retries when giving up short amounts of CPU
static const int _knShortRetrySleepMs = 50;									// time to wait when giving up short amounts of CPU

// Static constant initializers
managed_windows_shared_memory *CPubSubServer::_pSegment = NULL;						// pointer to the shared memory segment

/// <summary>
/// Open or create the shared memory segment.
/// </summary>
STDMETHODIMP CPubSubServer::Initialize()
{
    HRESULT result = S_OK;
    char *pszExceptionStateTracker = "Start";

    try
    {
    	CLTRACE(9, "PubSubServer: Initialize: Entry");
		if (_pSegment == NULL)
		{
			void *shm_base = 0;
            pszExceptionStateTracker = "Call open_or_create";
            _pSegment = new managed_windows_shared_memory(open_or_create, GetSharedMemoryNameWithVersion().c_str(), 1024000, shm_base);
            pszExceptionStateTracker = "Call get_segment_manager";
			segment_manager_t *segm = _pSegment->get_segment_manager();
            pszExceptionStateTracker = "Call get_address";
			shm_base = _pSegment->get_address();
			CLTRACE(9, "PubSubServer: Initialize: shm_base: Address in this process: %p.", shm_base);
			//BOOL fIsSane = segm->check_sanity();
			//if (!fIsSane)
			//{
			//	pszExceptionStateTracker = "Throw";
			//	throw new std::exception("ERROR: Shared memory segment is corrupted.");
			//}

			// Trace the sizes of the shared memory types.
	    	CLTRACE(9, "PubSubServer: Initialize: sizeof(IntPtr): %d", sizeof(shm_base));
	    	CLTRACE(9, "PubSubServer: Initialize: sizeof(ULONG): %d", sizeof(ULONG));
	    	CLTRACE(9, "PubSubServer: Initialize: sizeof(ULONG32): %d", sizeof(ULONG32));
	    	CLTRACE(9, "PubSubServer: Initialize: sizeof(ULONG64): %d", sizeof(ULONG64));
	    	CLTRACE(9, "PubSubServer: Initialize: sizeof(uint64_t): %d", sizeof(uint64_t));
	    	CLTRACE(9, "PubSubServer: Initialize: sizeof(BOOL): %d", sizeof(BOOL));
	    	CLTRACE(9, "PubSubServer: Initialize: sizeof(EnumEventType): %d", sizeof(EnumEventType));
	    	CLTRACE(9, "PubSubServer: Initialize: sizeof(EnumEventSubType): %d", sizeof(EnumEventSubType));
	    	CLTRACE(9, "PubSubServer: Initialize: sizeof(EnumCloudAppIconBadgeType): %d", sizeof(EnumCloudAppIconBadgeType));
	    	CLTRACE(9, "PubSubServer: Initialize: sizeof(GUID): %d", sizeof(GUID));
	    	CLTRACE(9, "PubSubServer: Initialize: sizeof(EventMessage): %d", sizeof(EventMessage));
	    	CLTRACE(9, "PubSubServer: Initialize: sizeof(Subscription): %d", sizeof(Subscription));
	    	CLTRACE(9, "PubSubServer: Initialize: sizeof(Base): %d", sizeof(Base));
	    	CLTRACE(9, "PubSubServer: Initialize: sizeof(UniqueSubscription_tag): %d", sizeof(UniqueSubscription_tag));
	    	CLTRACE(9, "PubSubServer: Initialize: sizeof(size_t): %d", sizeof(size_t));
		}
       	CLTRACE(9, "PubSubServer: Initialize: Segment: %p.", _pSegment);
    }
    catch (const std::exception &ex)
    {
		CLTRACE(1, "PubSubServer: Initialize: ERROR: Exception.  Message: %s. Tracker: %s.", ex.what(), pszExceptionStateTracker);
        result = E_FAIL;
    }
    catch (...)
    {
		CLTRACE(1, "PubSubServer: Initialize: ERROR: C++ exception. Tracker: %s.", pszExceptionStateTracker);
        result = E_FAIL;
    }

	CLTRACE(9, "PubSubServer: Initialize: Exit");
    return result;
}

/// <summary>
/// This thread is publishing an event to multiple subscribers who may be waiting to be notified.  
/// All subscribers to this event type and badge type will be notified.
/// </summary>
/// <param name="EventType">This is the event type being published.</param>
/// <param name="EventSubType">This is the event subtype being published.</param>
/// <param name="BadgeType">This is the badge type being published.</param>
/// <param name="FullPath">This is the full path of the file system item being badged with the badge type.</param>
/// <param name="GuidPublisher">This is the identity of the publisher</param>
/// <returns>(int via returnValue: See RC_PUBLISH_*.</returns>
STDMETHODIMP CPubSubServer::Publish(EnumEventType EventType, EnumEventSubType EventSubType, EnumCloudAppIconBadgeType BadgeType, BSTR *FullPath, GUID GuidPublisher, EnumPubSubServerPublishReturnCodes *returnValue)
{
    EnumPubSubServerPublishReturnCodes nResult = RC_PUBLISH_OK;                // assume no error
    char *pszExceptionStateTracker = "Start";

    try
    {
	    if (_pSegment == NULL || returnValue == NULL || FullPath == NULL)
	    {
		    CLTRACE(1, "PubSubServer: Publish: ERROR. _pSegment, FullPath and returnValue must all be non-NULL.");
		    return E_POINTER;
	    }

        ULONG processId = GetCurrentProcessId();
        ULONG threadId = GetCurrentThreadId();
		std::vector<GUID> subscribers;

	    CLTRACE(9, "PubSubServer: Publish: Entry. EventType: %d. EventSubType: %d. BadgeType: %d. FullPath: %ls. processId: %lx. GuidPublisher: %ls.", EventType, EventSubType, BadgeType, *FullPath, processId, CComBSTR(GuidPublisher));
        Base *pBase = NULL;


        // Open the shared memory segment, or create it if it is not there.  This is atomic.
        // An allocator convertible to any allocator<T, segment_manager_t> type.
        pszExceptionStateTracker = "Call alloc_inst";
        void_allocator alloc_inst(_pSegment->get_segment_manager());

        // Construct the shared memory Base image and initiliaze it.  This is atomic.
        pszExceptionStateTracker = "Call find_or_construct";
		std::less<int> comparator;
        pBase = _pSegment->find_or_construct<Base>(_ksBaseSharedMemoryObjectName)(comparator, alloc_inst);
	    CLTRACE(9, "PubSubServer: Initialize: sizeof(pBase->reserved1_): %d", sizeof(pBase->reserved1_));
	    CLTRACE(9, "PubSubServer: Initialize: sizeof(pBase->reserved2_): %d", sizeof(pBase->reserved2_));
	    CLTRACE(9, "PubSubServer: Initialize: sizeof(pBase->reserved3_): %d", sizeof(pBase->reserved3_));
	    CLTRACE(9, "PubSubServer: Initialize: sizeof(pBase->mutexSharedMemory_): %d", sizeof(pBase->mutexSharedMemory_));
	    CLTRACE(9, "PubSubServer: Initialize: sizeof(pBase->subscriptions_): %d", sizeof(pBase->subscriptions_));

		// We want to add this event to the queue for each of the scriptions waiting for this event type, but
		// one or more of the subscriber's event queues may be full.  If we find a full event queue, we will
		// sleep for a short while to give the subscriber cycles to remove the events.
		// During the first pass under the lock, we will save a list of the subscriptions that need to receive
		// a copy of this event.  Then we will release the lock.  Then we will iterate through the saved subscriptions
		// attempting to deliver the event (under the lock), checking to see if the subscription is still there.  We
		// will make several attempts to deliver the event to each subscriber (under the lock), sleeping for a short
		// while after each unsuccessful attempt.  If we time out during this process, delete the entire full subscription
		// and return with an error.  If the subscriber is really alive, the subscription will be reestablished the next
		// time the subscriber looks for an event.

		pBase->mutexSharedMemory_.lock();
		try
		{
			// Locate the inner map for this event type.
            pszExceptionStateTracker = "Locate inner map";
			eventtype_map_guid_subscription_map::iterator itMapOuter = pBase->subscriptions_.find(EventType);
			if (itMapOuter != pBase->subscriptions_.end())
			{
				// We found the inner map<GUID, Subscription> for this event type.  Iterate through that map and capture the guids so we can deliver this event to all of those subscribers.
				for (guid_subscription_map::iterator itSubscription = itMapOuter->second.begin(); itSubscription != itMapOuter->second.end(); ++itSubscription)
				{
					CLTRACE(9, "PubSubServer: Publish: Subscriber with GUID<%ls> is subscribed to this event.", CComBSTR(itSubscription->first));
					subscribers.push_back(itSubscription->first);
				}
			}

			pBase->mutexSharedMemory_.unlock();
		}
		catch (const std::exception &ex)
		{
			CLTRACE(1, "PubSubServer: Publish: ERROR: Exception(lock).  Message: %s. Tracker: %s.", ex.what(), pszExceptionStateTracker);
			pBase->mutexSharedMemory_.unlock();
            nResult = RC_PUBLISH_ERROR;
		}
        catch (...)
        {
		    CLTRACE(1, "PubSubServer: Publish: ERROR: C++ exception(lock). Tracker: %s.", pszExceptionStateTracker);
			pBase->mutexSharedMemory_.unlock();
            nResult = RC_PUBLISH_ERROR;
        }

		// Now iterate through the subscribers attempting to deliver the event to each.
        pszExceptionStateTracker = "Iterate thru subscribers";
		for (std::vector<GUID>::iterator itGuid = subscribers.begin(); itGuid != subscribers.end(); ++itGuid)
		{
			for (int nRetry = 0; nRetry < _knShortRetries; ++nRetry)
			{
				BOOL fEventDelivered = false;
				BOOL fSubscriptionRemoved = false;
				BOOL fSubscriptionNotFound = false;

				pBase->mutexSharedMemory_.lock();
				try
				{
					// Deliver this event to the subscription identified by this itGuid and the parameter EventType.
					eventtype_map_guid_subscription_map::iterator outItEventType;
					guid_subscription_map::iterator outItSubscription;
					offset_ptr<Subscription> outOptrFoundSubscription;
                    pszExceptionStateTracker = "Call FindSubscription";
					BOOL fFoundSubscription = FindSubscription(EventType, *itGuid, pBase, &outItEventType, &outItSubscription, &outOptrFoundSubscription);
					if (fFoundSubscription)
					{
						// We found the subscription.  This is a subscriber of this event.  Is its event queue full?
						if (outOptrFoundSubscription->events_.size() >= _knMaxEventsInEventQueue)
						{
							// No more room.  Retry or delete the subscription in error.
							if (nRetry >= (_knShortRetries - 1))
							{
								// Delete this entire subscription and log an error.
								CLTRACE(9, "PubSubServer: Publish: ERROR: Event queue full.  Delete subscription. EventType: %d. GUID<%ls>.", 
												outOptrFoundSubscription->nEventType_, CComBSTR(outOptrFoundSubscription->guidSubscriber_));
								RemoveTrackedSubscriptionId(outOptrFoundSubscription->nEventType_, outOptrFoundSubscription->guidSubscriber_);         // remove from subscription IDs being tracked
								outItEventType->second.erase(outItSubscription);
								nResult = RC_PUBLISH_AT_LEAST_ONE_EVENT_QUEUE_FULL;
								fSubscriptionRemoved = true;
							}
						}
						else
						{
							// The event queue has room.  Construct this event in shared memory and add it at the back of the event queue for this subscription.
							CLTRACE(9, "PubSubServer: Publish: Post this event to subscription with GUID<%ls>. pSemaphoreSubscription local addr: %p.", CComBSTR(*itGuid), outOptrFoundSubscription->pSemaphoreSubscription_.get());
							outOptrFoundSubscription->events_.emplace_back(EventType, EventSubType, processId, threadId, BadgeType, FullPath, GuidPublisher, alloc_inst);

							// Post the subscription's semaphore.
							outOptrFoundSubscription->pSemaphoreSubscription_->post();
							fEventDelivered = true;
						}
					}
					else
					{
						// Did not find the subscription.  Someone must have cancelled it.  This would be a normal race condition.
						CLTRACE(1, "PubSubServer: Publish: Event not found.  Break to next GUID.");
						fSubscriptionNotFound = true;
					}

					pBase->mutexSharedMemory_.unlock();
				}
				catch (const std::exception &ex)
				{
					CLTRACE(1, "PubSubServer: Publish: ERROR: Exception(lock, 2).  Message: %s. Tracker: %s.", ex.what(), pszExceptionStateTracker);
					pBase->mutexSharedMemory_.unlock();
                    nResult = RC_PUBLISH_ERROR;
				}
                catch (...)
                {
		            CLTRACE(1, "PubSubServer: Publish: ERROR: C++ exception(lock, 2). Tracker: %s.", pszExceptionStateTracker);
					pBase->mutexSharedMemory_.unlock();
                    nResult = RC_PUBLISH_ERROR;
                }

				if (fEventDelivered || fSubscriptionRemoved || fSubscriptionNotFound)
				{
					break;                          // No more retries.  Break to next subscriber.
				}

				// Wait a short time to retry publishing to this subscriber
				CLTRACE(9, "PubSubServer: Publish: Sleep %d ms.", _knShortRetrySleepMs);
				Sleep(_knShortRetrySleepMs);
			}					// end retry loop
		}						// end subscribers loop
    }
    catch (const std::exception &ex)
    {
		CLTRACE(1, "PubSubServer: Publish: ERROR: Exception.  Message: %s. Tracker: %s.", ex.what(), pszExceptionStateTracker);
        nResult = RC_PUBLISH_ERROR;
    }
    catch (...)
    {
		CLTRACE(1, "PubSubServer: Publish: ERROR: C++ exception. Tracker: %s.", pszExceptionStateTracker);
        nResult = RC_PUBLISH_ERROR;
    }

    *returnValue = nResult;
    return S_OK;
}

/// <summary>
/// This thread wants to receive events with a particular event type.  Optionally time out if an event does not arrive within the timeout period.
/// </summary>
/// <param name="EventType">This is the event type we want to receive.</param>
/// <param name="guidSubscriber">Represents the subscriber.  A subscriber may subscribe to multiple EventTypes.</param>
/// <param name="TimeoutMilliseconds">Wait for an event for up to this period of time.  If specified as 0, the wait will not time out.</param>
/// <param name="outEventSubType">(out) Pointer to the found event's subtype.</param>
/// <param name="outBadgeType">(out) Pointer to the found event's badge type.</param>
/// <param name="outFullPath">(out) Pointer to the found event's full path.</param>
/// <param name="outProcessIdPublisher">(out) Pointer to the found event's process ID.</param>
/// <param name="outGuidPublisher">(out) Pointer to the found event's publisher ID.</param>
/// <param name="returnValue">(out) Pointer to the return code.</param>
/// <returns>(int via returnValue): See RC_SUBSCRIBE_*.  Any non-zero HRESULT returned causes the .net wrapper code to throw an exception.</returns>
STDMETHODIMP CPubSubServer::Subscribe(
            EnumEventType EventType,
            GUID guidSubscriber,
            ULONG TimeoutMilliseconds,
            EnumEventSubType *outEventSubType,
            EnumCloudAppIconBadgeType *outBadgeType,
            BSTR *outFullPath,
            ULONG *outProcessIdPublisher,
            GUID *outGuidPublisher,
            EnumPubSubServerSubscribeReturnCodes *returnValue)
{
    EnumPubSubServerSubscribeReturnCodes nResult;
    char *pszExceptionStateTracker = "Start";

    try
    {
	    if (_pSegment == NULL || returnValue == NULL || outEventSubType == NULL || outBadgeType == NULL || outFullPath == NULL || outProcessIdPublisher == NULL || outGuidPublisher == NULL)
	    {
		    CLTRACE(1, "PubSubServer: Subscribe: ERROR. One or more required parameters are NULL.");
		    return E_POINTER;
	    }

	    CLTRACE(9, "PubSubServer: Subscribe: Entry. EventType: %d. GUID: %ls. TimeoutMilliseconds: %d.", EventType, CComBSTR(guidSubscriber), TimeoutMilliseconds);
		Base *pBase = NULL;
	    BOOL fSubscriptionFound;
	    subscription_vector::iterator itFoundSubscription;

        BOOL fWaitRequired = false;
		interprocess_semaphore *pFoundSemaphore = NULL;

        ULONG processId = GetCurrentProcessId();
        ULONG threadId = GetCurrentThreadId();

        // An allocator convertible to any allocator<T, segment_manager_t> type.
	    CLTRACE(9, "PubSubServer: Subscribe: Call get_segment_manager. _pSegment: %p.", _pSegment);
        pszExceptionStateTracker = "Call get_segment_manager";
        void_allocator alloc_inst(_pSegment->get_segment_manager());
	    CLTRACE(9, "PubSubServer: Subscribe: After call to get_segment_manager.");

        // Construct the shared memory Base image and initiliaze it.  This is atomic.
	    CLTRACE(9, "PubSubServer: Subscribe: Call find_or_construct.  _pSegment: %p.", _pSegment);
        pszExceptionStateTracker = "Call find_or_construct";
		std::less<int> comparator;
        pBase = _pSegment->find_or_construct<Base>(_ksBaseSharedMemoryObjectName)(comparator, alloc_inst);
	    CLTRACE(9, "PubSubServer: Subscribe: After call to find_or_construct. pBase: %p.", pBase);
	    CLTRACE(9, "PubSubServer: Initialize: sizeof(pBase->reserved1_): %d", sizeof(pBase->reserved1_));
	    CLTRACE(9, "PubSubServer: Initialize: sizeof(pBase->reserved2_): %d", sizeof(pBase->reserved2_));
	    CLTRACE(9, "PubSubServer: Initialize: sizeof(pBase->reserved3_): %d", sizeof(pBase->reserved3_));
	    CLTRACE(9, "PubSubServer: Initialize: sizeof(pBase->mutexSharedMemory_): %d", sizeof(pBase->mutexSharedMemory_));
	    CLTRACE(9, "PubSubServer: Initialize: sizeof(pBase->subscriptions_): %d", sizeof(pBase->subscriptions_));

	    pBase->mutexSharedMemory_.lock();
		try
		{
			// Look for this subscription.
	        CLTRACE(9, "PubSubServer: Subscribe: Inside lock.");
			eventtype_map_guid_subscription_map::iterator outItEventType;
			guid_subscription_map::iterator outItSubscription;
			offset_ptr<Subscription> outOptrFoundSubscription;
            pszExceptionStateTracker = "Call FindSubscription";
			fSubscriptionFound = FindSubscription(EventType, guidSubscriber, pBase, &outItEventType, &outItSubscription, &outOptrFoundSubscription);
			if (fSubscriptionFound)
			{
				// Found our subscription.  Make sure that it has not been previously cancelled.  If so, remove the subscription.
				CLTRACE(9, "PubSubServer: Subscribe: Found our subscription.");
                pszExceptionStateTracker = "Subscription found";
				if (outOptrFoundSubscription->fCancelled_)
				{
					CLTRACE(9, "PubSubServer: Subscribe: Warning: Already cancelled.  Erase the subscription.");
					RemoveTrackedSubscriptionId(outOptrFoundSubscription->nEventType_, outOptrFoundSubscription->guidSubscriber_);         // remove from subscription IDs being tracked
					outItEventType->second.erase(outItSubscription);
					fSubscriptionFound = false;             // itFoundSubscription not valid now
					nResult = RC_SUBSCRIBE_CANCELLED;
				}
				else if (outOptrFoundSubscription->events_.size() > 0)      // Check to see if we have a pending event.
				{
					// An event is waiting.  Return the information.
					EventMessage_vector::iterator itEvent = outOptrFoundSubscription->events_.begin();  // first event waiting
					*outEventSubType = itEvent->EventSubType_;
					*outBadgeType = itEvent->BadgeType_;
					*outFullPath = SysAllocString(itEvent->FullPath_.c_str());             // this is freed explicitly by the subscriber.  On the .Net side, the interop wrapper frees it.
                    *outProcessIdPublisher = (ULONG)itEvent->ProcessId_;
                    *outGuidPublisher = itEvent->GuidPublisher_;
					CLTRACE(9, "PubSubServer: Subscribe: Returned event info: EventSubType: %d. BadgeType: %d. FullPath: %ls. ProcessId: %lx. GuidPublisher: %ls.", *outEventSubType, *outBadgeType, *outFullPath, *outProcessIdPublisher, CComBSTR(*outGuidPublisher));

					// Remove the event from the vector.
					CLTRACE(9, "PubSubServer: Subscribe: Erase the event.");
					outOptrFoundSubscription->events_.erase(itEvent);
					nResult = RC_SUBSCRIBE_GOT_EVENT;
				}
				else 
				{
					// No events are waiting.  We will wait for one.
					CLTRACE(9, "PubSubServer: Subscribe: No events.  A wait will be required.");
					fWaitRequired = true;
					outOptrFoundSubscription->fWaiting_ = true;
				}
			}
			else
			{
				// Our specific subscription was not found.  We will need to add it.
				// First, locate the event type element in the outer map<EventType, map<GUID, Subscription>>.  The record may not be there.
				CLTRACE(9, "PubSubServer: Subscribe: Our specific subscription was not found.");
                pszExceptionStateTracker = "Subscription not found";
				eventtype_map_guid_subscription_map::iterator itMapOuter = pBase->subscriptions_.find(EventType);
				if (itMapOuter != pBase->subscriptions_.end())
				{
					// The event type record was found in the outer map.  Add a pair_guid_subscription record to the inner map.
					CLTRACE(9, "PubSubServer: Subscribe: The EventType record was found in the outer map.  Construct an inner map pair_guid_subscription and add it to the inner map.");
                    pszExceptionStateTracker = "Add an inner map";
					std::pair<guid_subscription_map::iterator, BOOL> retvalEmplace;
					retvalEmplace = itMapOuter->second.emplace(pair_guid_subscription(guidSubscriber, Subscription(guidSubscriber, processId, threadId, EventType, alloc_inst)));
					retvalEmplace.first->second.fWaiting_ = true;
					outOptrFoundSubscription = &retvalEmplace.first->second;
				}
				else
				{
					// The event type was not found in the outer map.  Construct an inner map<GUID, Subscription> and add it to the outer map.
					CLTRACE(9, "PubSubServer: Subscribe: The EventType record was not found in the outer map.  Construct and add it.");
					CLTRACE(9, "PubSubServer: Subscribe: sizeof(subscription_allocator): %d.", sizeof(subscription_allocator));
					CLTRACE(9, "PubSubServer: Subscribe: sizeof(guid_allocator): %d.", sizeof(guid_allocator));
					CLTRACE(9, "PubSubServer: Subscribe: sizeof(pair_guid_subscription): %d.", sizeof(pair_guid_subscription));
					CLTRACE(9, "PubSubServer: Subscribe: sizeof(eventtype_allocator): %d.", sizeof(eventtype_allocator));
					CLTRACE(9, "PubSubServer: Subscribe: sizeof(pair_guid_subscription_allocator): %d.", sizeof(pair_guid_subscription_allocator));
					CLTRACE(9, "PubSubServer: Subscribe: sizeof(guid_subscription_map): %d.", sizeof(guid_subscription_map));
					CLTRACE(9, "PubSubServer: Subscribe: sizeof(pair_eventtype_pair_guid_subscription): %d.", sizeof(pair_eventtype_pair_guid_subscription));
					CLTRACE(9, "PubSubServer: Subscribe: sizeof(pair_eventtype_pair_guid_subscription_allocator): %d.", sizeof(pair_eventtype_pair_guid_subscription_allocator));
					CLTRACE(9, "PubSubServer: Subscribe: sizeof(eventtype_map_guid_subscription_map): %d.", sizeof(eventtype_map_guid_subscription_map));
					CLTRACE(9, "PubSubServer: Subscribe: sizeof(subscription_vector): %d.", sizeof(subscription_vector));
					CLTRACE(9, "PubSubServer: Subscribe: sizeof(subscription_vector_allocator): %d.", sizeof(subscription_vector_allocator));
					CLTRACE(9, "PubSubServer: Subscribe: sizeof(guid_subscription_map_allocator): %d.", sizeof(guid_subscription_map_allocator));
					CLTRACE(9, "PubSubServer: Subscribe: sizeof(eventtype_map_guid_subscription_map_allocator): %d.", sizeof(eventtype_map_guid_subscription_map_allocator));

                    pszExceptionStateTracker = "Construct an inner map";
					std::pair<guid_subscription_map::iterator, BOOL> retvalEmplace;

					GUIDPairsComparer comparator;
					guid_subscription_map *mapGuidSubscription = _pSegment->construct<guid_subscription_map>(anonymous_instance)(comparator, alloc_inst);
					if (mapGuidSubscription == NULL)
					{
						throw new std::exception("ERROR: mapGuidSubscrition is NULL");
					}

					// Add a pair_guid_subscription to the inner map just constructed.
					CLTRACE(9, "PubSubServer: Subscribe: Construct the GUID/Subscription pair to the inner map.");
					retvalEmplace = mapGuidSubscription->emplace(pair_guid_subscription(guidSubscriber, Subscription(guidSubscriber, processId, threadId, EventType, alloc_inst)));
					retvalEmplace.first->second.fWaiting_ = true;
					outOptrFoundSubscription = &retvalEmplace.first->second;

					// Then add the constructed inner map to the outer map.
					CLTRACE(9, "PubSubServer: Subscribe: Add the constructed inner map to the outer map.");
					pBase->subscriptions_.emplace(pair_eventtype_pair_guid_subscription(EventType, *mapGuidSubscription));
				}

				// Point our iterator at the Subscription just inserted or located in the map for this event type.
				CLTRACE(9, "PubSubServer: Subscribe: This is a new subscription.  A wait will be required.");
				fWaitRequired = true;
				fSubscriptionFound = true;                  // itFoundSubscription valid now

				// Remember this subscription
				UniqueSubscription thisSubscriptionId;
				thisSubscriptionId.eventType = EventType;
				thisSubscriptionId.guid = guidSubscriber;
				_trackedSubscriptionIds.push_back(thisSubscriptionId);				// add this subscription ID to the list of subscriptions created by this instance
			}

			// Get a pointer to the semaphore allocated in shared memory.  We must release the lock before waiting on the semaphore.
			// Without the lock, the subscriptions may move around in shared memory, but the semaphore itself will remain fixed.
			if (fSubscriptionFound && fWaitRequired)
			{
				pFoundSemaphore = outOptrFoundSubscription->pSemaphoreSubscription_.get();
				CLTRACE(9, "PubSubServer: Subscribe: optrSemaphore local address under lock: %p.", pFoundSemaphore);
			}

			pBase->mutexSharedMemory_.unlock();
		}
		catch (const std::exception &ex)
		{
			CLTRACE(1, "PubSubServer: Subscribe: ERROR: Exception(lock, 3).  Message: %s. Tracker: %s.", ex.what(), pszExceptionStateTracker);
			pBase->mutexSharedMemory_.unlock();
            nResult = RC_SUBSCRIBE_ERROR;
		}
        catch (...)
        {
		    CLTRACE(1, "PubSubServer: Subscribe: ERROR: C++ exception(lock, 3). Tracker: %s.", pszExceptionStateTracker);
			pBase->mutexSharedMemory_.unlock();
            nResult = RC_SUBSCRIBE_ERROR;
        }

        // Wait if we should.
        pszExceptionStateTracker = "Wait";
        if (fWaitRequired)
        {
            if (TimeoutMilliseconds != 0)
            {
                // Wait for a matching event to arrive.  Use a timed wait.
				CLTRACE(9, "PubSubServer: Subscribe: Wait with timeout. Waiting ProcessId: %lx. ThreadId: %lx. Guid: %ls. Semaphore local addr: %p.", processId, threadId, CComBSTR(guidSubscriber), pFoundSemaphore);
                pszExceptionStateTracker = "Call timed_wait";
                boost::posix_time::ptime tNow(boost::posix_time::microsec_clock::universal_time());
                BOOL fDidNotTimeOut = pFoundSemaphore->timed_wait(tNow + boost::posix_time::milliseconds(TimeoutMilliseconds));
                if (fDidNotTimeOut)
                {
					CLTRACE(9, "PubSubServer: Subscribe: Got an event or posted by a cancel. Return code 'try again'.");
                    nResult = RC_SUBSCRIBE_TRY_AGAIN;
                }
                else
                {
					CLTRACE(9, "PubSubServer: Subscribe: Timed out waiting for an event. Return 'timed out'.");
                    nResult = RC_SUBSCRIBE_TIMED_OUT;
                }
            }
            else
            {
                // Wait forever for a matching event to arrive.
				CLTRACE(9, "PubSubServer: Subscribe: Wait forever for an event to arrive. Waiting ProcessId: %lx. ThreadId: %lx. Guid: %ls. Semaphore addr: %p.", processId, threadId, CComBSTR(guidSubscriber), pFoundSemaphore);
                pszExceptionStateTracker = "Call wait";
                pFoundSemaphore->wait();
				CLTRACE(9, "PubSubServer: Subscribe: Got an event or posted by a cancel(2).  Return code 'try again'.");
                nResult = RC_SUBSCRIBE_TRY_AGAIN;
            }

            // Dropped out of the wait.  Lock again.
            pszExceptionStateTracker = "Lock";
      	    pBase->mutexSharedMemory_.lock();
			try
			{
				// The subscriptions may have moved.  Locate our subscription again.
				eventtype_map_guid_subscription_map::iterator outItEventType;
				guid_subscription_map::iterator outItSubscription;
				offset_ptr<Subscription> outOptrFoundSubscription;
                pszExceptionStateTracker = "Call FindSubscription2";
				fSubscriptionFound = FindSubscription(EventType, guidSubscriber, pBase, &outItEventType, &outItSubscription, &outOptrFoundSubscription);
				if (fSubscriptionFound)
				{
					// We found the subscription again.
					outOptrFoundSubscription->fWaiting_ = false;        // no longer waiting
					CLTRACE(9, "PubSubServer: Subscribe: Waiting flag cleared.");

					// Check for a cancellation.
					if (outOptrFoundSubscription->fCancelled_)
					{
						CLTRACE(9, "PubSubServer: Subscribe: This subscription has been cancelled.  Erase it.  Return code 'cancelled'.");
						RemoveTrackedSubscriptionId(outOptrFoundSubscription->nEventType_, outOptrFoundSubscription->guidSubscriber_);         // remove from subscription IDs being tracked
						outItEventType->second.erase(outItSubscription);
						nResult = RC_SUBSCRIBE_CANCELLED;
					}
				}
				else
				{
					// The subscription is gone!  Another thread must have cancelled it and erased it.
					CLTRACE(1, "PubSubServer: Subscribe: Subscription missing after wait..");
					nResult = RC_SUBSCRIBE_CANCELLED;
				}

				pBase->mutexSharedMemory_.unlock();
			}
			catch (const std::exception &ex)
			{
				CLTRACE(1, "PubSubServer: Subscribe: ERROR: Exception(lock, 4).  Message: %s. Tracker: %s.", ex.what(), pszExceptionStateTracker);
				pBase->mutexSharedMemory_.unlock();
                nResult = RC_SUBSCRIBE_ERROR;
			}
            catch (...)
            {
		        CLTRACE(1, "PubSubServer: Subscribe: ERROR: C++ exception(lock, 4). Tracker: %s.", pszExceptionStateTracker);
			    pBase->mutexSharedMemory_.unlock();
                nResult = RC_SUBSCRIBE_ERROR;
            }
        }
    }
    catch (const std::exception &ex)
    {
		CLTRACE(1, "PubSubServer: Subscribe: ERROR: Exception.  Message: %s. Tracker: %s.", ex.what(), pszExceptionStateTracker);
        nResult = RC_SUBSCRIBE_ERROR;
    }
    catch (...)
    {
		CLTRACE(1, "PubSubServer: Subscribe: ERROR: C++ exception. Tracker: %s.", pszExceptionStateTracker);
        nResult = RC_SUBSCRIBE_ERROR;
    }

    *returnValue = nResult;
    return S_OK;
}

/// <Summary>
/// Cancel all of the subscriptions belonging to a particular process ID.
/// </Summary>
STDMETHODIMP CPubSubServer::CancelSubscriptionsForProcessId(ULONG ProcessId, EnumPubSubServerCancelSubscriptionsByProcessIdReturnCodes *returnValue)
{
    EnumPubSubServerCancelSubscriptionsByProcessIdReturnCodes nResult = RC_CANCELBYPROCESSID_NOT_FOUND;
    char *pszExceptionStateTracker = "Start";

    try
    {
	    if (_pSegment == NULL || returnValue == NULL)
	    {
		    CLTRACE(1, "PubSubServer: CancelSubscriptionsForProcessId: ERROR. _pSegment, and returnValue must all be non-NULL.");
		    return E_POINTER;
	    }

	    CLTRACE(9, "PubSubServer: CancelSubscriptionsForProcessId: Entry. ProcessId: %lx.", ProcessId);
        Base *pBase = NULL;
	    std::vector<UniqueSubscription> subscriptionIdsForProcess;		// list of subscriptions belonging to this process

        // An allocator convertible to any allocator<T, segment_manager_t> type.
        pszExceptionStateTracker = "Call alloc_inst";
        void_allocator alloc_inst(_pSegment->get_segment_manager());

        // Construct the shared memory Base image and initiliaze it.  This is atomic.
        pszExceptionStateTracker = "Call find_or_construct";
		std::less<int> comparator;
        pBase = _pSegment->find_or_construct<Base>(_ksBaseSharedMemoryObjectName)(comparator, alloc_inst);

		// Loop until we don't find any subscriptions for this process
		while (true)
		{
			pBase->mutexSharedMemory_.lock();
			try
			{
                pszExceptionStateTracker = "Call clear";
				subscriptionIdsForProcess.clear();

				// Look for any subscriptions belonging to this process.  Build up a local list of the subscriptions that we will cancel.
				// Iterate through the event types:
				for (eventtype_map_guid_subscription_map::iterator itEventType = pBase->subscriptions_.begin(); itEventType != pBase->subscriptions_.end(); ++itEventType)
				{
					EnumEventType eventType = itEventType->first;
					for (guid_subscription_map::iterator itSubscription = itEventType->second.begin(); itSubscription != itEventType->second.end(); ++itSubscription)
					{
						if (itSubscription->second.uSubscribingProcessId_ == ProcessId)
						{
							// This is one of our subscriptions.  Add it to a list to cancel.
							CLTRACE(9, "PubSubServer: CancelSubscriptionsForProcessId: Found subscription. EventType: %d. Guid: %ls.", 
									eventType, CComBSTR(itSubscription->second.guidSubscriber_));
							UniqueSubscription thisSubscriptionId;
							thisSubscriptionId.eventType = itSubscription->second.nEventType_;
							thisSubscriptionId.guid = itSubscription->second.guidSubscriber_;
							subscriptionIdsForProcess.push_back(thisSubscriptionId);
						}
					}
				}

				pBase->mutexSharedMemory_.unlock();
			}
			catch (const std::exception &ex)
			{
				CLTRACE(1, "PubSubServer: CancelSubscriptionsForProcessId: ERROR: Exception(lock).  Message: %s.", ex.what());
				pBase->mutexSharedMemory_.unlock();
                nResult = RC_CANCELBYPROCESSID_ERROR;
            }
            catch (...)
            {
		        CLTRACE(1, "PubSubServer: CancelSubscriptionsForProcessId: ERROR: C++ exception(lock).");
				pBase->mutexSharedMemory_.unlock();
                nResult = RC_CANCELBYPROCESSID_ERROR;
            }

			// We are done if there are no subscriptions to cancel
            pszExceptionStateTracker = "Subscriptions gathered";
			if (subscriptionIdsForProcess.size() <= 0)
			{
				break;
			}

			// Now loop through the local list cancelling subscriptions without the lock.  CancelWaitingSubscription() will lock for
			// each individual cancellation.
			for (std::vector<UniqueSubscription>::iterator it = subscriptionIdsForProcess.begin(); it != subscriptionIdsForProcess.end(); ++it)
			{
				EnumPubSubServerCancelWaitingSubscriptionReturnCodes cancelResult;
				CancelWaitingSubscription(it->eventType, it->guid, &cancelResult);
				if (cancelResult == RC_CANCEL_OK || cancelResult == RC_CANCEL_NOT_FOUND)
				{
					if (nResult != RC_CANCELBYPROCESSID_ERROR)
					{
						nResult = RC_CANCELBYPROCESSID_OK;
					}
				}
				else
				{
					nResult = RC_CANCELBYPROCESSID_ERROR;
				}
			}
		}
    }
    catch (const std::exception &ex)
    {
		CLTRACE(1, "PubSubServer: CancelSubscriptionsForProcessId: ERROR: Exception.  Message: %s. Tracker: %s.", ex.what(), pszExceptionStateTracker);
        nResult = RC_CANCELBYPROCESSID_ERROR;
    }
    catch (...)
    {
		CLTRACE(1, "PubSubServer: CancelSubscriptionsForProcessId: ERROR: C++ exception. Tracker: %s.", pszExceptionStateTracker);
        nResult = RC_CANCELBYPROCESSID_ERROR;
    }

    *returnValue = nResult;
    return S_OK;
}

/// <Summary>
/// Break a waiting subscription out of its wait and delete the subscription.
/// </Summary>
STDMETHODIMP CPubSubServer::CancelWaitingSubscription(EnumEventType EventType, GUID guidSubscriber, EnumPubSubServerCancelWaitingSubscriptionReturnCodes *returnValue)
{
    EnumPubSubServerCancelWaitingSubscriptionReturnCodes nResult;
    char *pszExceptionStateTracker = "Start";

    try
    {
	    if (_pSegment == NULL || returnValue == NULL)
	    {
		    CLTRACE(1, "PubSubServer: CancelWaitingSubscription: ERROR. _pSegment, and returnValue must all be non-NULL.");
		    return E_POINTER;
	    }

	    CLTRACE(9, "PubSubServer: CancelWaitingSubscription: Entry. EventType: %d. GUID: %ls.", EventType, CComBSTR(guidSubscriber));
        Base *pBase = NULL;

        // An allocator convertible to any allocator<T, segment_manager_t> type.
        pszExceptionStateTracker = "Call alloc_inst";
        void_allocator alloc_inst(_pSegment->get_segment_manager());

        // Construct the shared memory Base image and initiliaze it.  This is atomic.
        pszExceptionStateTracker = "Call find_or_construct";
		std::less<int> comparator;
        pBase = _pSegment->find_or_construct<Base>(_ksBaseSharedMemoryObjectName)(comparator, alloc_inst);

	    pBase->mutexSharedMemory_.lock();
		try
		{
			// Look for this subscription.
			eventtype_map_guid_subscription_map::iterator outItEventType;
			guid_subscription_map::iterator outItSubscription;
			offset_ptr<Subscription> outOptrFoundSubscription;
            pszExceptionStateTracker = "Call FindSubscription";
			BOOL fSubscriptionFound = FindSubscription(EventType, guidSubscriber, pBase, &outItEventType, &outItSubscription, &outOptrFoundSubscription);
			if (fSubscriptionFound)
			{
				// Found our subscription.  Post the semaphore to allow the waiting thread to fall through the wait.
				// Set a flag in the subscription to indicate that it is cancelled.  This will prevent the subscribing thread
				// from waiting again.
				CLTRACE(9, "PubSubServer: CancelWaitingSubscription: Found our subscription. Mark cancelled and post it. Thread Waiting: %d. ProcessId: %lx. ThreadId: %lx. Semaphore local addr: %p. Guid: %ls.", 
										outOptrFoundSubscription->fWaiting_, outOptrFoundSubscription->uSubscribingProcessId_, 
										outOptrFoundSubscription->uSubscribingThreadId_, outOptrFoundSubscription->pSemaphoreSubscription_.get(), 
										CComBSTR(outOptrFoundSubscription->guidSubscriber_));
				outOptrFoundSubscription->fCancelled_ = true;
				outOptrFoundSubscription->pSemaphoreSubscription_->post();

				// Give the thread a chance to exit the wait.
				BOOL fCancelOk = false;
				for (int i = 0; i < _knShortRetries; i++)
				{
					// Give up some cycles.  Free the lock momentarily.
					pBase->mutexSharedMemory_.unlock();
					Sleep(_knShortRetrySleepMs);
					pBase->mutexSharedMemory_.lock();

					// We released the lock.  The subscription might have moved.  Locate it again.
                    pszExceptionStateTracker = "Call FindSubscription2";
					fSubscriptionFound = FindSubscription(EventType, guidSubscriber, pBase, &outItEventType, &outItSubscription, &outOptrFoundSubscription);
					if (fSubscriptionFound)
					{
						// We found it again, but due to a race condition, it might be a new subscription.  If so, we need to cancel it again.
           				CLTRACE(9, "PubSubServer: CancelWaitingSubscription: Subscription found again under lock.");
						if (!outOptrFoundSubscription->fCancelled_)
						{
							// The subscriber placed a new subscription.  Cancel it again.
            				CLTRACE(9, "PubSubServer: CancelWaitingSubscription: New subscription placed.  Cancel it and post it again. Thread Waiting: %d. ProcessId: %lx. ThreadId: %lx. Semaphore addr: %p. Guid: %ls.", 
										outOptrFoundSubscription->fWaiting_, outOptrFoundSubscription->uSubscribingProcessId_, 
										outOptrFoundSubscription->uSubscribingThreadId_, outOptrFoundSubscription->pSemaphoreSubscription_.get(), 
										CComBSTR(outOptrFoundSubscription->guidSubscriber_));
							outOptrFoundSubscription->fCancelled_ = true;
							outOptrFoundSubscription->pSemaphoreSubscription_->post();
							continue;
						}

						// If not waiting now, remove the subscription
						if (!outOptrFoundSubscription->fWaiting_)
						{
							// Remove this subscription ID from the list of subscriptions created by this instance.
        					CLTRACE(9, "PubSubServer: CancelWaitingSubscription: Erase the subscription.");
							RemoveTrackedSubscriptionId(outOptrFoundSubscription->nEventType_, outOptrFoundSubscription->guidSubscriber_);

							// Delete the subscription itself
							outItEventType->second.erase(outItSubscription);
							fCancelOk = true;
							break;
						}
					}
					else
					{
						// Subscription not found. Someone else must have deleted it.
           				CLTRACE(9, "PubSubServer: CancelWaitingSubscription: Someone else erased the subscription.");
						fCancelOk = true;
						break;          // break out of retry loop
					}

				}               // end retry loop

				if (fCancelOk)
				{
					CLTRACE(9, "PubSubServer: CancelWaitingSubscription: Return result 'Cancelled'.");
					nResult = RC_CANCEL_OK;
				}
				else
				{
					CLTRACE(1, "PubSubServer: CancelWaitingSubscription: Return result 'Cancelled, but subscription not removed'.");
					nResult = RC_CANCEL_CANCELLED_BUT_SUBSCRIPTION_NOT_REMOVED;
				}
			}
			else
			{
				// This subscription was not found
				CLTRACE(1, "PubSubServer: CancelWaitingSubscription: Return result 'not found'.");
				nResult = RC_CANCEL_NOT_FOUND;
			}

			pBase->mutexSharedMemory_.unlock();
		}
		catch (const std::exception &ex)
		{
			CLTRACE(1, "PubSubServer: CancelWaitingSubscription: ERROR: Exception(lock).  Message: %s. Tracker: %s.", ex.what(), pszExceptionStateTracker);
			pBase->mutexSharedMemory_.unlock();
			nResult = RC_CANCEL_ERROR;
		}
        catch (...)
        {
		    CLTRACE(1, "PubSubServer: CancelWaitingSubscription: ERROR: C++ exception(lock). Tracker: %s.", pszExceptionStateTracker);
			pBase->mutexSharedMemory_.unlock();
			nResult = RC_CANCEL_ERROR;
        }
    }
    catch (const std::exception &ex)
    {
		CLTRACE(1, "PubSubServer: CancelWaitingSubscription: ERROR: Exception.  Message: %s. Tracker: %s.", ex.what(), pszExceptionStateTracker);
        nResult = RC_CANCEL_ERROR;
    }
    catch (...)
    {
		CLTRACE(1, "PubSubServer: CancelWaitingSubscription: ERROR: C++ exception. Tracker: %s.", pszExceptionStateTracker);
        nResult = RC_CANCEL_ERROR;
    }

    *returnValue = nResult;
    return S_OK;
}

/// <Summary>
/// Get the name of the shared memory segment.
/// </Summary>
STDMETHODIMP CPubSubServer::get_SharedMemoryName(BSTR* pVal)
{
    HRESULT nResult = S_OK;

    try
    {
	    if (_pSegment == NULL || pVal == NULL)
	    {
		    CLTRACE(1, "PubSubServer: get_SharedMemoryName: ERROR. _pSegment or pVal is NULL.");
		    return E_POINTER;
	    }


        *pVal = SysAllocString(GetSharedMemoryNameWithVersionWide().c_str());                    // the caller must free this memory.
    }
    catch (const std::exception &ex)
    {
        CLTRACE(1, "PubSubServer: get_SharedMemoryName: Exception: %s.", ex.what());
        nResult = E_OUTOFMEMORY;
    }
    catch (...)
    {
		CLTRACE(1, "PubSubServer: get_SharedMemoryName: ERROR: C++ exception.");
        nResult = E_OUTOFMEMORY;
    }

    return nResult;
}

/// <summary>
/// Remove a subscription ID from the list of subscriptions created by this instance.
/// NOTE: Assumes that the caller holds the shared memory lock.
/// </summary>
void CPubSubServer::RemoveTrackedSubscriptionId(EnumEventType eventType, GUID guid)
{
    for (std::vector<UniqueSubscription>::iterator it = _trackedSubscriptionIds.begin(); it != _trackedSubscriptionIds.end(); ++it)
    {
        if (it->eventType == eventType && it->guid == guid)
        {
    		CLTRACE(9, "PubSubServer: RemoveTrackedSubscriptionId: Erase subscription ID. EventType: %d. GUID: %ls.", eventType, CComBSTR(guid));
            _trackedSubscriptionIds.erase(it);
            break;
        }
    }
}


/// <summary>
/// Delete a subscription from shared memory by GUID ID.
/// NOTE: Assumes the shared memory lock is held.
/// </summary>
void CPubSubServer::DeleteSubscriptionById(Base * pBase, CPubSubServer::UniqueSubscription subscriptionId)
{
	try
	{
		// Look for the subscription
		eventtype_map_guid_subscription_map::iterator itMapOuter = pBase->subscriptions_.find(subscriptionId.eventType);
		if (itMapOuter != pBase->subscriptions_.end())
		{
			// We found the inner map<GUID, Subscription> for this event type.  Find the subscription by GUID.
			guid_subscription_map::iterator itMapInner = itMapOuter->second.find(subscriptionId.guid);
			if (itMapInner != itMapOuter->second.end())
			{
				CLTRACE(9, "PubSubServer: DeleteSubscriptionById: Delete subscription.  EventType: %d. GUID: %ls.", subscriptionId.eventType, CComBSTR(subscriptionId.guid));
                RemoveTrackedSubscriptionId(subscriptionId.eventType, subscriptionId.guid);

				itMapOuter->second.erase(itMapInner);
			}
		}
	}
	catch (const std::exception &ex)
	{
        CLTRACE(1, "PubSubServer: DeleteSubscriptionById: Exception: %s.", ex.what());
	}
    catch (...)
    {
		CLTRACE(1, "PubSubServer: DeleteSubscriptionById: ERROR: C++ exception.");
    }
}

/// <summary>
/// Find a subscription by its event type and GUID.
/// NOTE: Assumes the shared memory lock is held.
/// </summary>
/// <returns>BOOL: true: found the subscription.</returns>
BOOL CPubSubServer::FindSubscription(EnumEventType EventType, GUID guidSubscriber, Base *pBase, eventtype_map_guid_subscription_map::iterator *outItEventType,
						guid_subscription_map::iterator *outItSubscription,	offset_ptr<Subscription> *outOptrFoundSubscription)
{
    BOOL result = false;
 	try
	{
		eventtype_map_guid_subscription_map::iterator itMapOuter = pBase->subscriptions_.find(EventType);
		if (itMapOuter != pBase->subscriptions_.end())
		{
			// We found the inner map<GUID, Subscription> for this event type.  Find the subscription by GUID.
			*outItEventType = itMapOuter;
			guid_subscription_map::iterator itMapInner = itMapOuter->second.find(guidSubscriber);
			if (itMapInner != itMapOuter->second.end())
			{
				CLTRACE(9, "PubSubServer: FindSubscription: Found subscription.");
				*outItSubscription = itMapInner;
				*outOptrFoundSubscription = &itMapInner->second;
				result = true;
			}
		}
	}
	catch (const std::exception &ex)
	{
        CLTRACE(1, "PubSubServer: FindSubscription: Exception: %s.", ex.what());
	}
    catch (...)
    {
		CLTRACE(1, "PubSubServer: FindSubscription: ERROR: C++ exception.");
    }

    return result;
}
/// <summary>
/// Trace the current state of shared memory.
/// NOTE: This function assumes that the shared memory lock is held by the caller.
/// </summary>
void CPubSubServer::TraceCurrentStateOfSharedMemory(Base *pBase)
{
    try
    {
        // Loop through all of the event types.
    	CLTRACE(9, "PubSubServer: TraceCurrentStateOfSharedMemory: Trace state of shared memory.");
		for (eventtype_map_guid_subscription_map::iterator itEventType = pBase->subscriptions_.begin(); itEventType != pBase->subscriptions_.end(); ++itEventType)
		{
            // Loop through all of the subscriptions for this event type.
			for (guid_subscription_map::iterator itSubscription = itEventType->second.begin(); itSubscription != itEventType->second.end(); ++itSubscription)
			{
				// Log this subscription
				CLTRACE(9, "PubSubServer: TraceCurrentStateOfSharedMemory: Subscription: EventType: %d. ProcessId: %lx. ThreadId: %lx. Guid: %ls. fCancelled: %d. fDestructed: %d. fWaiting: %d. pSemaphoreSubscription: %p.", 
						itSubscription->second.nEventType_,
                        itSubscription->second.uSubscribingProcessId_, 
						itSubscription->second.uSubscribingThreadId_,
                        CComBSTR(itSubscription->second.guidSubscriber_),
                        itSubscription->second.fCancelled_,
                        itSubscription->second.fDestructed_,
                        itSubscription->second.fWaiting_,
                        itSubscription->second.pSemaphoreSubscription_.get()
                       );

				// List all of the events
                for (EventMessage_vector::iterator itEvent = itSubscription->second.events_.begin(); itEvent != itSubscription->second.events_.end(); ++itEvent)
                {
    				CLTRACE(9, "PubSubServer: TraceCurrentStateOfSharedMemory: Event: EventType: %d. ProcessId: %lx. ThreadId: %lx. EventSubType: %d. BadgeType: %d. FullPath: %ls. GuidPublisher: %ls.", 
                        itEvent->EventType_,
                        itEvent->ProcessId_,
                        itEvent->ThreadId_,
                        itEvent->EventSubType_,
                        itEvent->BadgeType_,
                        itEvent->FullPath_.c_str(),
                        CComBSTR(itEvent->GuidPublisher_)
                        );
                }
			}
		}
    }
    catch (const std::exception &ex)
    {
        CLTRACE(1, "PubSubServer: TraceCurrentStateOfSharedMemory: Exception: %s.", ex.what());
    }
    catch (...)
    {
		CLTRACE(1, "PubSubServer: TraceCurrentStateOfSharedMemory: ERROR: C++ exception.");
    }
}

/// <summary>
/// Clean up resources that are left when processes are killed.
/// </summary>
STDMETHODIMP CPubSubServer::CleanUpUnusedResources(EnumPubSubServerCleanUpUnusedResourcesReturnCodes *returnValue)
{
    char *pszExceptionStateTracker = "Start";
	try
	{
	    if (_pSegment == NULL || returnValue == NULL)
	    {
		    CLTRACE(1, "PubSubServer: CleanUpUnusedResources: ERROR. _pSegment, and returnValue must all be non-NULL.");
		    return E_POINTER;
	    }

	    // Remove any subscriptions owned by this instance
	    CLTRACE(9, "PubSubServer: CleanUpUnusedResources: Entry.");
        Base *pBase = NULL;

        *returnValue = RC_CLEANUPUNUSEDRESOURCES_OK;
		if (_pSegment != NULL)
		{
			// An allocator convertible to any allocator<T, segment_manager_t> type.
            pszExceptionStateTracker = "Call alloc_inst";
			void_allocator alloc_inst(_pSegment->get_segment_manager());

			// Construct the shared memory Base image and initiliaze it.  This is atomic.
            pszExceptionStateTracker = "Call find_or_construct";
			std::less<int> comparator;
	        pBase = _pSegment->find_or_construct<Base>(_ksBaseSharedMemoryObjectName)(comparator, alloc_inst);

			pBase->mutexSharedMemory_.lock();
			try
			{
				ULONG processId = GetCurrentProcessId();
				ULONG threadId = GetCurrentThreadId();

				CLTRACE(9, "PubSubServer: CleanUpUnusedResources: Trace current state of shared memory.  This process ID: %lx. This thread ID: %lx", processId, threadId);
                TraceCurrentStateOfSharedMemory(pBase);

                // Loop through all of the event types.
				for (eventtype_map_guid_subscription_map::iterator itEventType = pBase->subscriptions_.begin(); itEventType != pBase->subscriptions_.end(); ++itEventType)
				{
                    // Loop through the subscriptions for this event type.  Check each of the processes.  If the process is not running, erase the subscription.
					for (guid_subscription_map::iterator itSubscription = itEventType->second.begin(); itSubscription != itEventType->second.end(); /* iterator bumped in the body */)
					{
						// Log this subscription
                        BOOL fBumpIterator = true;
                        ULONG thisProcessId = (ULONG)itSubscription->second.uSubscribingProcessId_;
						CLTRACE(9, "PubSubServer: CleanUpUnusedResources: Found subscription: EventType: %d. ProcessId: %lx. ThreadId: %lx. Guid: %ls.", 
								itSubscription->second.nEventType_, thisProcessId, 
								itSubscription->second.uSubscribingThreadId_, CComBSTR(itSubscription->second.guidSubscriber_));

						// Erase the subscription if it doesn't belong to this process, and the subscription's process is not running.
                        if (thisProcessId != processId && !IsProcessRunning(thisProcessId))
                        {
    						CLTRACE(9, "PubSubServer: CleanUpUnusedResources: This process is not running.  Erase the subscription.");
                            itSubscription = itEventType->second.erase(itSubscription);
                            fBumpIterator = false;
                        }

                        if (fBumpIterator)
                        {
                            ++itSubscription;
                        }
					}
				}

                CLTRACE(9, "PubSubServer: CleanUpUnusedResources: Trace current state of shared memory after clean up.  This process ID: %lx. This thread ID: %lx", processId, threadId);
                TraceCurrentStateOfSharedMemory(pBase);

				pBase->mutexSharedMemory_.unlock();
			}
			catch (const std::exception &ex)
			{
				CLTRACE(1, "PubSubServer: CleanUpUnusedResources: ERROR: Exception(lock).  Message: %s. Tracker: %s.", ex.what(), pszExceptionStateTracker);
				pBase->mutexSharedMemory_.unlock();
                *returnValue = RC_CLEANUPUNUSEDRESOURCES_ERROR;
			}
            catch (...)
            {
		        CLTRACE(1, "PubSubServer: CleanUpUnusedResources: ERROR: C++ exception(lock). Tracker: %s.", pszExceptionStateTracker);
				pBase->mutexSharedMemory_.unlock();
                *returnValue = RC_CLEANUPUNUSEDRESOURCES_ERROR;
            }

		}
	}
	catch (const std::exception &ex)
	{
		CLTRACE(1, "PubSubServer: CleanUpUnusedResources: ERROR: Exception.  Message: %s. Tracker: %s.", ex.what(), pszExceptionStateTracker);
        *returnValue = RC_CLEANUPUNUSEDRESOURCES_ERROR;
	}
    catch (...)
    {
		CLTRACE(1, "PubSubServer: CleanUpUnusedResources: ERROR: C++ exception. Tracker: %s.", pszExceptionStateTracker);
        *returnValue = RC_CLEANUPUNUSEDRESOURCES_ERROR;
    }

	CLTRACE(9, "PubSubServer: CleanUpUnusedResources: Exit.");
    return S_OK;
}

/// <summary>
/// Free resources.
/// </summary>
STDMETHODIMP CPubSubServer::Terminate()
{
    char *pszExceptionStateTracker = "Start";
	try
	{
	    // Remove any subscriptions owned by this instance
	    CLTRACE(9, "PubSubServer: Terminate: Entry.");
        Base *pBase = NULL;

		if (_pSegment != NULL)
		{
			// An allocator convertible to any allocator<T, segment_manager_t> type.
            pszExceptionStateTracker = "Call alloc_inst";
			void_allocator alloc_inst(_pSegment->get_segment_manager());

			// Construct the shared memory Base image and initiliaze it.  This is atomic.
            pszExceptionStateTracker = "Call find_or_construct";
			std::less<int> comparator;
	        pBase = _pSegment->find_or_construct<Base>(_ksBaseSharedMemoryObjectName)(comparator, alloc_inst);

			pBase->mutexSharedMemory_.lock();
			try
			{
				// Delete all of the subscriptions made by this instance.
                pszExceptionStateTracker = "Delete subscriptions";
				for (std::vector<UniqueSubscription>::iterator it = _trackedSubscriptionIds.begin(); it != _trackedSubscriptionIds.end(); ++it)
				{
					CLTRACE(9, "PubSubServer: Terminate: Call DeleteSubscriptionById.");
					DeleteSubscriptionById(pBase, *it);
				}
				_trackedSubscriptionIds.clear();

				ULONG processId = GetCurrentProcessId();
				ULONG threadId = GetCurrentThreadId();

				CLTRACE(9, "PubSubServer: Terminate: Print subscriptions left.  This process ID: %lx. This thread ID: %lx", processId, threadId);
				for (eventtype_map_guid_subscription_map::iterator itEventType = pBase->subscriptions_.begin(); itEventType != pBase->subscriptions_.end(); ++itEventType)
				{
					for (guid_subscription_map::iterator itSubscription = itEventType->second.begin(); itSubscription != itEventType->second.end(); ++itSubscription)
					{
						// Log this subscription.
						CLTRACE(9, "PubSubServer: Terminate: Remaining subscription: EventType: %d. ProcessId: %lx. ThreadId: %lx. Guid: %ls.", 
								itSubscription->second.nEventType_, itSubscription->second.uSubscribingProcessId_, 
								itSubscription->second.uSubscribingThreadId_, CComBSTR(itSubscription->second.guidSubscriber_));
					}
				}
				CLTRACE(9, "PubSubServer: Terminate: End of remaining subscriptions.");

				pBase->mutexSharedMemory_.unlock();
			}
			catch (const std::exception &ex)
			{
				CLTRACE(1, "PubSubServer: Terminate: ERROR: Exception(lock).  Message: %s. Tracker: %s.", ex.what(), pszExceptionStateTracker);
				pBase->mutexSharedMemory_.unlock();
			}
            catch (...)
            {
		        CLTRACE(1, "PubSubServer: Terminate: ERROR: C++ exception(lock). Tracker: %s.", pszExceptionStateTracker);
				pBase->mutexSharedMemory_.unlock();
            }

		}
	}
	catch (const std::exception &ex)
	{
		CLTRACE(1, "PubSubServer: Terminate: ERROR: Exception.  Message: %s. Tracker: %s.", ex.what(), pszExceptionStateTracker);
	}
    catch (...)
    {
		CLTRACE(1, "PubSubServer: Terminate: ERROR: C++ exception. Tracker: %s.", pszExceptionStateTracker);
    }

	// Release the shared memory segment if all instances are finished with it.
    pszExceptionStateTracker = "Release segment";
    try
    {
		if (_pSegment != NULL)
		{
	        _pSegment->~basic_managed_windows_shared_memory();
			_pSegment = NULL;
		}
    }
    catch (const std::exception &ex)
    {
		CLTRACE(1, "PubSubServer: Terminate: ERROR: Exception(2).  Message: %s. Tracker: %s.", ex.what(), pszExceptionStateTracker);
    }
    catch (...)
    {
		CLTRACE(1, "PubSubServer: Terminate: ERROR: C++ exception(2). Tracker: %s.", pszExceptionStateTracker);
    }

	CLTRACE(9, "PubSubServer: Terminate: Exit.");
    return S_OK;
}

/// <summary>
/// Checks whether a process is alive.
/// </summary>
/// <param name="pid">Process ID to check.</param>
/// <returns>BOOL true: The process is still alive.</returns>
BOOL CPubSubServer::IsProcessRunning(DWORD pid)
{
    HANDLE process = OpenProcess(SYNCHRONIZE, FALSE, pid);
    DWORD ret = WaitForSingleObject(process, 0);
    CloseHandle(process);
    return ret == WAIT_TIMEOUT;
}

/// <summary>
/// Get the global name of the shared memory segment.  We may have multiple versions of this DLL in play,
/// so the name is formatted like "CloudPubSubSharedMemory_VA_B_C_D".
/// </summary>
/// <returns>std::wstring: The formatted name of the shared memory segment.</returns>
std::wstring CPubSubServer::GetSharedMemoryNameWithVersionWide()
{
    std::string s = GetSharedMemoryNameWithVersion();
    int slength = (int)s.length() + 1;
    int len = MultiByteToWideChar(CP_ACP, 0, s.c_str(), slength, 0, 0); 
    std::wstring r(len, L'\0');
    MultiByteToWideChar(CP_ACP, 0, s.c_str(), slength, &r[0], len);
    CLTRACE(9, "PubSubServer: GetSharedMemoryNameWithVersionWide: Return %ls.", r.c_str());
    return r;
}

/// <summary>
/// Get the global name of the shared memory segment.  We may have multiple versions of this DLL in play,
/// so the name is formatted like "CloudPubSubSharedMemory_VA_B_C_D".
/// </summary>
/// <returns>std::sstring: The formatted name of the shared memory segment.</returns>
extern HINSTANCE g_hBadgeComInstance;
std::string CPubSubServer::GetSharedMemoryNameWithVersion()
{
    BYTE* versionInfo = NULL;
    std::string sOutput;
    try
    {
        // Get the full path to this running DLL assembly.
        WCHAR fileName[_MAX_PATH + 1];
        DWORD size = GetModuleFileName(g_hBadgeComInstance, fileName, _MAX_PATH);
        if (size == 0)
        {
            throw new std::exception("GetModuleFileName failed");
        }
        fileName[size] = NULL;

        // Get the version info size
        DWORD handle = 0;
        size = GetFileVersionInfoSize(fileName, &handle);
        if (size == 0)
        {
            throw new std::exception("No file version information size returned");
        }

        // Get the version info itself
        versionInfo = new BYTE[size + 1];
        if (!GetFileVersionInfo(fileName, handle, size, versionInfo))
        {
            throw new std::exception("No file version information returned");
        }

        // We have version information.  Extract it.
        UINT    			len = 0;
        VS_FIXEDFILEINFO*   vsfi = NULL;
        BOOL fRc = VerQueryValue(versionInfo, L"\\", (void**)&vsfi, &len);
        if (!fRc)
        {
            throw new std::exception("VerQueryValue error.  Name or resource is invalid");
        }

        // Extract the version info.
        USHORT aVersion[4];
        aVersion[0] = HIWORD(vsfi->dwFileVersionMS);
        aVersion[1] = LOWORD(vsfi->dwFileVersionMS);
        aVersion[2] = HIWORD(vsfi->dwFileVersionLS);
        aVersion[3] = LOWORD(vsfi->dwFileVersionLS);

        // Format the version info into a string like "CloudPubSubSharedMemory_VA_B_C_D"
        sOutput = str(boost::format("%s_V%d_%d_%d_%d") % _ksSharedMemoryName % aVersion[0] % aVersion[1] % aVersion[2] % aVersion[3]);

        delete[] versionInfo;
        versionInfo = NULL;
    }
    catch (const std::exception &ex)
    {
		CLTRACE(1, "PubSubServer: GetSharedMemoryNameWithVersion: ERROR: Exception.  Message: %s.", ex.what());
        if (versionInfo != NULL)
        {
            delete[] versionInfo;
            versionInfo = NULL;
        }
    }
    catch (...)
    {
		CLTRACE(1, "PubSubServer: GetSharedMemoryNameWithVersion: ERROR: C++ exception.");
        if (versionInfo != NULL)
        {
            delete[] versionInfo;
            versionInfo = NULL;
        }
    }

    CLTRACE(9, "PubSubServer: GetSharedMemoryNameWithVersion: Return %s.", sOutput.c_str());
    return sOutput;
}

