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
#ifdef _DEBUG
	//#define CLTRACE(intPriority, szFormat, ...) 
	#define CLTRACE(intPriority, szFormat, ...) Trace::getInstance()->write(intPriority, szFormat, __VA_ARGS__)
#else	
#define CLTRACE(intPriority, szFormat, ...)
//#define CLTRACE(intPriority, szFormat, ...) Trace::getInstance()->write(intPriority, szFormat, __VA_ARGS__)
#endif // _DEBUG

// Constants
static const OLECHAR * _kwsSharedMemoryName = L"CloudPubSubSharedMemory";	// the wide string name of the shared memory segment
static const char * _ksSharedMemoryName = "CloudPubSubSharedMemory";		// the name of the shared memory segment
static const char * _ksBaseSharedMemoryObjectName = "Base";				// the name of the single shared memory object that contains our shared memory data.
static const int _knMaxEventsInEventQueue = 1000;							// maximum number of events allowed in a subscription's event queue
static const int _knShortRetries = 5;										// number of retries when giving up short amounts of CPU
static const int _knShortRetrySleepMs = 50;								// time to wait when giving up short amounts of CPU

// Static constant initializers
managed_windows_shared_memory *CPubSubServer::_pSegment = NULL;						// pointer to the shared memory segment

/// <summary>
/// Open or create the shared memory segment.
/// </summary>
STDMETHODIMP CPubSubServer::Initialize()
{
    HRESULT result = S_OK;

	CLTRACE(9, "PubSubServer: Initialize: Entry");
    try
    {
		if (_pSegment = NULL)
		{
	        _pSegment = new managed_windows_shared_memory(open_or_create, _ksSharedMemoryName, 1024000);
		}
    }
    catch (std::exception &ex)
    {
        result = E_FAIL;
		CLTRACE(1, "PubSubServer: Initialize: ERROR: Exception.  Message: %s.", ex.what());
    }

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
/// <returns>(int via returnValue: See RC_PUBLISH_*.</returns>
STDMETHODIMP CPubSubServer::Publish(EnumEventType EventType, EnumEventSubType EventSubType, EnumCloudAppIconBadgeType BadgeType, BSTR *FullPath, EnumPubSubServerPublishReturnCodes *returnValue)
{
	if (_pSegment == NULL || returnValue == NULL || FullPath == NULL)
	{
		CLTRACE(1, "PubSubServer: Publish: ERROR. _pSegment, FullPath and returnValue must all be non-NULL.");
		return E_POINTER;
	}

	CLTRACE(9, "PubSubServer: Publish: Entry. EventType: %d. EventSubType: %d. BadgeType: %d. FullPath: %ls.", EventType, EventSubType, BadgeType, *FullPath);
    EnumPubSubServerPublishReturnCodes nResult = RC_PUBLISH_OK;                // assume no error
    Base *base = NULL;

    try
    {
        ULONG processId = GetCurrentProcessId();
        ULONG threadId = GetCurrentThreadId();
		std::vector<GUID> subscribers;

        // Open the shared memory segment, or create it if it is not there.  This is atomic.
        // An allocator convertible to any allocator<T, segment_manager_t> type.
        void_allocator alloc_inst(_pSegment->get_segment_manager());

        // Construct the shared memory Base image and initiliaze it.  This is atomic.
        base = _pSegment->find_or_construct<Base>(_ksBaseSharedMemoryObjectName)(alloc_inst);

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

		base->mutexSharedMemory_.lock();
		try
		{
			// Loop through the subscriptions vector looking for this EventType.  We will deliver this event to those subscribers.
			for (subscription_vector::iterator itSubscription = base->subscriptions_.begin(); itSubscription != base->subscriptions_.end(); ++itSubscription)
			{
				// This is a subscriber if the event types match.
				EnumEventType eventType = itSubscription->nEventType_;
				if (eventType == EventType)
				{
					// This is a subscriber.  Remember its ID.
					CLTRACE(9, "PubSubServer: Publish: Subscriber with GUID<%ls> is subscribed to this event.", CComBSTR(itSubscription->guidSubscriber_));
					subscribers.push_back(itSubscription->guidSubscriber_);
				}
			}

			base->mutexSharedMemory_.unlock();
		}
		catch (std::exception &ex)
		{
			CLTRACE(1, "PubSubServer: Publish: ERROR: Exception(lock).  Message: %s.", ex.what());
			base->mutexSharedMemory_.unlock();
		}

		// Now iterate through the subscribers attempting to deliver the event to each.
		for (std::vector<GUID>::iterator itGuid = subscribers.begin(); itGuid != subscribers.end(); ++itGuid)
		{
			for (int nRetry = 0; nRetry < _knShortRetries; ++nRetry)
			{
				bool fEventDelivered = false;
				bool fSubscriptionRemoved = false;

				base->mutexSharedMemory_.lock();
				try
				{
					// Find the subscription with this ID and try to deliver the event.
					// Loop through the subscriptions looking for this particular subscriber
					for (subscription_vector::iterator itSubscription = base->subscriptions_.begin(); itSubscription != base->subscriptions_.end(); ++itSubscription)
					{
						if ((EventType == itSubscription->nEventType_) && (*itGuid == itSubscription->guidSubscriber_))
						{
							// This is a subscriber of this event.  Is its event queue full?
							if (itSubscription->events_.size() >= _knMaxEventsInEventQueue)
							{
								// No more room.  Retry or delete the subscription in error.
								if (nRetry >= (_knShortRetries - 1))
								{
									// Delete this entire subscription and log an error.
									CLTRACE(9, "PubSubServer: Publish: ERROR: Event queue full.  Delete subscription. EventType: %d. GUID<%ls>.", itSubscription->nEventType_, CComBSTR(itSubscription->guidSubscriber_));
									RemoveTrackedSubscriptionId(itSubscription->nEventType_, itSubscription->guidSubscriber_);         // remove from subscription IDs being tracked
									itSubscription = base->subscriptions_.erase(itSubscription);
									nResult = RC_PUBLISH_AT_LEAST_ONE_EVENT_QUEUE_FULL;
									fSubscriptionRemoved = true;
								}
							}
							else
							{
								// The event queue has room.  Construct this event in shared memory and add it at the back of the event queue for this subscription.
								CLTRACE(9, "PubSubServer: Publish: Post this event to subscription with GUID<%ls>.", CComBSTR(itSubscription->guidSubscriber_));
								itSubscription->events_.emplace_back(EventType, EventSubType, processId, threadId, BadgeType, FullPath, alloc_inst);

								// Post the subscription's semaphore.
								itSubscription->pSemaphoreSubscription_->post();
								fEventDelivered = true;
							}
							break;              // break out of itSubscription loop
						}
					}

					base->mutexSharedMemory_.unlock();
				}
				catch (std::exception &ex)
				{
					CLTRACE(1, "PubSubServer: Publish: ERROR: Exception(lock).  Message: %s.", ex.what());
					base->mutexSharedMemory_.unlock();
				}

				if (fEventDelivered || fSubscriptionRemoved)
				{
					break;                          // No more retries.  Break to next subscriber.
				}

				// Wait a short time to retry publishing to this subscriber
				CLTRACE(9, "PubSubServer: Publish: Sleep %d ms.", _knShortRetrySleepMs);
				Sleep(_knShortRetrySleepMs);
			}					// end retry loop
		}						// end subscribers loop
    }
    catch(std::exception &ex)
    {
		CLTRACE(1, "PubSubServer: Publish: ERROR: Exception.  Message: %s.", ex.what());
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
/// <returns>(int via returnValue): See RC_SUBSCRIBE_*.</returns>
STDMETHODIMP CPubSubServer::Subscribe(
            EnumEventType EventType,
            GUID guidSubscriber,
            LONG TimeoutMilliseconds,
            EnumEventSubType *outEventSubType,
            EnumCloudAppIconBadgeType *outBadgeType,
            BSTR *outFullPath,
            EnumPubSubServerSubscribeReturnCodes *returnValue)
{
	if (_pSegment == NULL || returnValue == NULL || outEventSubType == NULL || outBadgeType == NULL || outFullPath == NULL)
	{
		CLTRACE(1, "PubSubServer: Subscribe: ERROR. One or more required parameters are NULL.");
		return E_POINTER;
	}

	CLTRACE(9, "PubSubServer: Subscribe: Entry. EventType: %d. GUID: %ls. TimeoutMilliseconds: %d.", EventType, CComBSTR(guidSubscriber), TimeoutMilliseconds);
    EnumPubSubServerSubscribeReturnCodes nResult;
    Base *base = NULL;
	bool fSubscriptionFound;
	subscription_vector::iterator itFoundSubscription;

    try
    {
        bool fWaitRequired = false;
		offset_ptr<interprocess_semaphore> optrSemaphore;

        ULONG processId = GetCurrentProcessId();
        ULONG threadId = GetCurrentThreadId();
        ULONGLONG subscriptionFoundIndex = 0;

        // An allocator convertible to any allocator<T, segment_manager_t> type.
        void_allocator alloc_inst(_pSegment->get_segment_manager());

        // Construct the shared memory Base image and initiliaze it.  This is atomic.
        base = _pSegment->find_or_construct<Base>(_ksBaseSharedMemoryObjectName)(alloc_inst);

	    base->mutexSharedMemory_.lock();
		try
		{
			// Look for this subscription.
			fSubscriptionFound = FindSubscription(EventType, guidSubscriber, base, &itFoundSubscription);
			if (fSubscriptionFound)
			{
				// Found our subscription.  Make sure that it has not been previously cancelled.  If so, remove the subscription.
				CLTRACE(9, "PubSubServer: Subscribe: Found our subscription.");
				if (itFoundSubscription->fCancelled_)
				{
					CLTRACE(9, "PubSubServer: Subscribe: Warning: Already cancelled.  Erase the subscription.");
					RemoveTrackedSubscriptionId(itFoundSubscription->nEventType_, itFoundSubscription->guidSubscriber_);         // remove from subscription IDs being tracked
					base->subscriptions_.erase(itFoundSubscription);
					fSubscriptionFound = false;             // itFoundSubscription not valid now
					nResult = RC_SUBSCRIBE_CANCELLED;
				}
				else if (itFoundSubscription->events_.size() > 0)      // Check to see if we have a pending event.
				{
					// An event is waiting.  Return the information.
					EventMessage_vector::iterator itEvent = itFoundSubscription->events_.begin();  // first event waiting
					*outEventSubType = itEvent->EventSubType_;
					*outBadgeType = itEvent->BadgeType_;
					*outFullPath = SysAllocString(itEvent->FullPath_.c_str());             // this is freed explicitly by the subscriber.  On the .Net side, the interop wrapper frees it.
					CLTRACE(9, "PubSubServer: Subscribe: Returned event info: EventSubType: %d. BadgeType: %d. FullPath: %ls.", *outEventSubType, *outBadgeType, *outFullPath);

					// Remove the event from the vector.
					CLTRACE(9, "PubSubServer: Subscribe: Erase the event.");
					itFoundSubscription->events_.erase(itEvent);
					nResult = RC_SUBSCRIBE_GOT_EVENT;
				}
				else 
				{
					// No events are waiting.  We will wait for one.
					CLTRACE(9, "PubSubServer: Subscribe: No events.  A wait will be required.");
					fWaitRequired = true;
					itFoundSubscription->fWaiting_ = true;
				}
			}
			else
			{
				// Our subscription was not found.  Construct a new subscription at the back of the vector.  We will wait for an event.
				CLTRACE(9, "PubSubServer: Subscribe: Our subscription was not found.  Construct a new subscription.  Wait required.");
				base->subscriptions_.emplace_back(guidSubscriber, processId, threadId, EventType, alloc_inst);

				// Point our iterator at the Subscription just placed at the end of the subscription_vector.
				itFoundSubscription = base->subscriptions_.end();
				--itFoundSubscription;
				fWaitRequired = true;
				itFoundSubscription->fWaiting_ = true;
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
				subscriptionFoundIndex = std::distance(base->subscriptions_.begin(), itFoundSubscription);
				optrSemaphore = itFoundSubscription->pSemaphoreSubscription_;
			}

			base->mutexSharedMemory_.unlock();
		}
		catch (std::exception &ex)
		{
			CLTRACE(1, "PubSubServer: Subscribe: ERROR: Exception(lock).  Message: %s.", ex.what());
			base->mutexSharedMemory_.unlock();
		}


        // Wait if we should.
        if (fWaitRequired)
        {
            if (TimeoutMilliseconds != 0)
            {
                // Wait for a matching event to arrive.  Use a timed wait.
				CLTRACE(9, "PubSubServer: Subscribe: Wait with timeout. Waiting ProcessId: %lx. ThreadId: %lx. SubscriptionIndex: %llu. Guid: %ls. Semaphore addr: %x.", processId, threadId, subscriptionFoundIndex, CComBSTR(guidSubscriber), optrSemaphore.get());
                boost::posix_time::ptime tNow(boost::posix_time::microsec_clock::universal_time());
                bool fDidNotTimeOut = optrSemaphore->timed_wait(tNow + boost::posix_time::milliseconds(TimeoutMilliseconds));
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
				CLTRACE(9, "PubSubServer: Subscribe: Wait forever for an event to arrive. Waiting ProcessId: %lx. ThreadId: %lx. SubscriptionIndex: %llu. Guid: %ls. Semaphore addr: %x.", processId, threadId, subscriptionFoundIndex, CComBSTR(guidSubscriber), optrSemaphore.get());
                optrSemaphore->wait();
				CLTRACE(9, "PubSubServer: Subscribe: Got an event or posted by a cancel(2).  Return code 'try again'.");
                nResult = RC_SUBSCRIBE_TRY_AGAIN;
            }

            // Dropped out of the wait.  Lock again.
      	    base->mutexSharedMemory_.lock();
			try
			{
				// The subscriptions may have moved.  Locate our subscription again.
				fSubscriptionFound = FindSubscription(EventType, guidSubscriber, base, &itFoundSubscription);
				if (fSubscriptionFound)
				{
					// We found the subscription again.
					itFoundSubscription->fWaiting_ = false;        // no longer waiting
					CLTRACE(9, "PubSubServer: Subscribe: Waiting flag cleared.");

					// Check for a cancellation.
					if (itFoundSubscription->fCancelled_)
					{
						CLTRACE(9, "PubSubServer: Subscribe: Return code 'cancelled'.");
						RemoveTrackedSubscriptionId(itFoundSubscription->nEventType_, itFoundSubscription->guidSubscriber_);         // remove from subscription IDs being tracked
						base->subscriptions_.erase(itFoundSubscription);
						nResult = RC_SUBSCRIBE_CANCELLED;
					}
				}
				else
				{
					// The subscription is gone!  Another thread must have cancelled it and erased it.
					CLTRACE(1, "PubSubServer: Subscribe: Subscription missing after wait..");
					nResult = RC_SUBSCRIBE_CANCELLED;
				}

				base->mutexSharedMemory_.unlock();
			}
			catch (std::exception &ex)
			{
				CLTRACE(1, "PubSubServer: Subscribe: ERROR: Exception(lock(2)).  Message: %s.", ex.what());
				base->mutexSharedMemory_.unlock();
			}
        }
    }
    catch (std::exception &ex)
    {
		CLTRACE(1, "PubSubServer: Subscribe: ERROR: Exception.  Message: %s.", ex.what());
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
	if (_pSegment == NULL || returnValue == NULL)
	{
		CLTRACE(1, "PubSubServer: CancelSubscriptionsForProcessId: ERROR. _pSegment, and returnValue must all be non-NULL.");
		return E_POINTER;
	}

	CLTRACE(9, "PubSubServer: CancelSubscriptionsForProcessId: Entry. ProcessId: %lx.", ProcessId);
    EnumPubSubServerCancelSubscriptionsByProcessIdReturnCodes nResult = RC_CANCELBYPROCESSID_NOT_FOUND;
    Base *base = NULL;
	std::vector<UniqueSubscription> subscriptionIdsForProcess;		// list of subscriptions belonging to this process

    try
    {
        // An allocator convertible to any allocator<T, segment_manager_t> type.
        void_allocator alloc_inst(_pSegment->get_segment_manager());

        // Construct the shared memory Base image and initiliaze it.  This is atomic.
        base = _pSegment->find_or_construct<Base>(_ksBaseSharedMemoryObjectName)(alloc_inst);

		// Loop until we don't find any subscriptions for this process
		while (true)
		{
			base->mutexSharedMemory_.lock();
			try
			{
				subscriptionIdsForProcess.clear();

				// Look for any subscriptions belonging to this process.  Build up a local list of the subscriptions that we will cancel.
				for (subscription_vector::iterator itSubscription = base->subscriptions_.begin(); itSubscription != base->subscriptions_.end(); ++itSubscription)
				{
					ULONG thisProcessId = itSubscription->uSubscribingProcessId_;
					if (thisProcessId == ProcessId)
					{
						// This is one of our subscriptions.  Add it to a list to cancel.
						CLTRACE(9, "PubSubServer: CancelSubscriptionsForProcessId: Found subscription. EventType: %d. Guid: %ls.", itSubscription->nEventType_, CComBSTR(itSubscription->guidSubscriber_));
						UniqueSubscription thisSubscriptionId;
						thisSubscriptionId.eventType = itSubscription->nEventType_;
						thisSubscriptionId.guid = itSubscription->guidSubscriber_;
						subscriptionIdsForProcess.push_back(thisSubscriptionId);
					}
				}

				base->mutexSharedMemory_.unlock();
			}
			catch (std::exception &ex)
			{
				CLTRACE(1, "PubSubServer: CancelSubscriptionsForProcessId: ERROR: Exception(lock).  Message: %s.", ex.what());
				base->mutexSharedMemory_.unlock();
			}

			// We are done if there are no subscriptions to cancel
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
    catch (std::exception &ex)
    {
		CLTRACE(1, "PubSubServer: CancelSubscriptionsForProcessId: ERROR: Exception.  Message: %s.", ex.what());
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
	if (_pSegment == NULL || returnValue == NULL)
	{
		CLTRACE(1, "PubSubServer: CancelWaitingSubscription: ERROR. _pSegment, and returnValue must all be non-NULL.");
		return E_POINTER;
	}

	CLTRACE(9, "PubSubServer: CancelWaitingSubscription: Entry. EventType: %d. GUID: %ls.", EventType, CComBSTR(guidSubscriber));
    EnumPubSubServerCancelWaitingSubscriptionReturnCodes nResult;
    Base *base = NULL;

    try
    {
        // An allocator convertible to any allocator<T, segment_manager_t> type.
        void_allocator alloc_inst(_pSegment->get_segment_manager());

        // Construct the shared memory Base image and initiliaze it.  This is atomic.
        base = _pSegment->find_or_construct<Base>(_ksBaseSharedMemoryObjectName)(alloc_inst);

	    base->mutexSharedMemory_.lock();
		try
		{
			// Look for this subscription.
			subscription_vector::iterator itFoundSubscription;
			bool fSubscriptionFound = FindSubscription(EventType, guidSubscriber, base, &itFoundSubscription);
			if (fSubscriptionFound)
			{
				// Found our subscription.  Post the semaphore to allow the waiting thread to fall through the wait.
				// Set a flag in the subscription to indicate that it is cancelled.  This will prevent the subscribing thread
				// from waiting again.
				CLTRACE(9, "PubSubServer: CancelWaitingSubscription: Found our subscription. Mark cancelled and post it. Thread Waiting: %d. ProcessId: %lx. ThreadId: %lx. Semaphore addr: %x. Guid: %ls.", 
										itFoundSubscription->fWaiting_, itFoundSubscription->uSubscribingProcessId_, itFoundSubscription->uSubscribingThreadId_, itFoundSubscription->pSemaphoreSubscription_.get(), CComBSTR(itFoundSubscription->guidSubscriber_));
				itFoundSubscription->fCancelled_ = true;
				itFoundSubscription->pSemaphoreSubscription_->post();

				// Give the thread a chance to exit the wait.
				bool fCancelOk = false;
				for (int i = 0; i < _knShortRetries; i++)
				{
					// Give up some cycles.  Free the lock momentarily.
					base->mutexSharedMemory_.unlock();
					Sleep(_knShortRetrySleepMs);
					base->mutexSharedMemory_.lock();

					// We released the lock.  The subscription might have moved.  Locate it again.
					fSubscriptionFound = FindSubscription(EventType, guidSubscriber, base, &itFoundSubscription);
					if (fSubscriptionFound)
					{
						// We found it again, but due to a race condition, it might be a new subscription.  If so, we need to cancel it again.
						if (!itFoundSubscription->fCancelled_)
						{
							// The subscriber placed a new subscription.  Cancel it again.
            				CLTRACE(9, "PubSubServer: CancelWaitingSubscription: New subscription placed.  Cancel it and post it again. Thread Waiting: %d. ProcessId: %lx. ThreadId: %lx. Semaphore addr: %x. Guid: %ls.", 
										itFoundSubscription->fWaiting_, itFoundSubscription->uSubscribingProcessId_, itFoundSubscription->uSubscribingThreadId_, itFoundSubscription->pSemaphoreSubscription_.get(), CComBSTR(itFoundSubscription->guidSubscriber_));
							itFoundSubscription->fCancelled_ = true;
							itFoundSubscription->pSemaphoreSubscription_->post();
							continue;
						}

						// If not waiting now, remove the subscription
						if (!itFoundSubscription->fWaiting_)
						{
							// Remove this subscription ID from the list of subscriptions created by this instance.
        					CLTRACE(9, "PubSubServer: CancelWaitingSubscription: Erase the subscription.");
							RemoveTrackedSubscriptionId(itFoundSubscription->nEventType_, itFoundSubscription->guidSubscriber_);

							// Delete the subscription itself
							base->subscriptions_.erase(itFoundSubscription);
							fCancelOk = true;
							break;
						}
					}
					else
					{
						// Subscription not found. Someone else must have deleted it.
						break;
					}

				}

				if (fCancelOk)
				{
					CLTRACE(1, "PubSubServer: CancelWaitingSubscription: Return result 'Cancelled'.");
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

			base->mutexSharedMemory_.unlock();
		}
		catch (std::exception &ex)
		{
			CLTRACE(1, "PubSubServer: CancelWaitingSubscription: ERROR: Exception(lock).  Message: %s.", ex.what());
			base->mutexSharedMemory_.unlock();
		}


    }
    catch (std::exception &ex)
    {
		CLTRACE(1, "PubSubServer: CancelWaitingSubscription: ERROR: Exception.  Message: %s.", ex.what());
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
	if (_pSegment == NULL || pVal == NULL)
	{
		CLTRACE(1, "PubSubServer: get_SharedMemoryName: ERROR. _pSegment or pVal is NULL.");
		return E_POINTER;
	}

    HRESULT nResult = S_OK;

    try
    {
        *pVal = SysAllocString(_kwsSharedMemoryName);                    // the caller must free this memory.
    }
    catch(std::exception &ex)
    {
        CLTRACE(1, "PubSubServer: get_SharedMemoryName: Exception: %s.", ex.what());
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
    		CLTRACE(1, "PubSubServer: RemoveTrackedSubscriptionId: Erase subscription ID. EventType: %d. GUID: %ls.", eventType, CComBSTR(guid));
            _trackedSubscriptionIds.erase(it);
            break;
        }
    }
}


/// <summary>
/// Delete a subscription from shared memory by GUID ID.
/// NOTE: Assumes the shared memory lock is held.
/// </summary>
void CPubSubServer::DeleteSubscriptionById(Base * base, CPubSubServer::UniqueSubscription subscriptionId)
{
	try
	{
		// Look for the subscription
		for (subscription_vector::iterator itSubscription = base->subscriptions_.begin(); itSubscription != base->subscriptions_.end(); /* iterator bump handled in body */)
		{
			if (itSubscription->guidSubscriber_ == subscriptionId.guid && itSubscription->nEventType_ == subscriptionId.eventType)
			{
				// Remove this subscription ID from the list of subscriptions created by this instance, and erase the subscription itself.
				CLTRACE(9, "PubSubServer: DeleteSubscriptionById: Delete subscription.  EventType: %d. GUID: %ls.", subscriptionId.eventType, CComBSTR(subscriptionId.guid));
                RemoveTrackedSubscriptionId(itSubscription->nEventType_, itSubscription->guidSubscriber_);

				itSubscription = base->subscriptions_.erase(itSubscription);
			}
			else
			{
				++itSubscription;
			}
		}
	}
	catch (std::exception &ex)
	{
        CLTRACE(1, "PubSubServer: DeleteSubscriptionById: Exception: %s.", ex.what());
	}
}

/// <summary>
/// Find a subscription by its event type and GUID.
/// NOTE: Assumes the shared memory lock is held.
/// </summary>
/// <returns>bool: true: found the subscription.</returns>
bool CPubSubServer::FindSubscription(EnumEventType EventType, GUID guidSubscriber, Base *base, CPubSubServer::subscription_vector::iterator *outItFoundSubscription)
{
    bool result = false;
 	try
	{
        // Look for this subscription.
		for (subscription_vector::iterator itSubscription = base->subscriptions_.begin(); itSubscription != base->subscriptions_.end(); ++itSubscription)
        {
            EnumEventType thisEventType = itSubscription->nEventType_;
            GUID thisGuid = itSubscription->guidSubscriber_;
            if (thisEventType == EventType && thisGuid == guidSubscriber)
            {
                *outItFoundSubscription = itSubscription;
                result = true;
				CLTRACE(9, "PubSubServer: FindSubscription: Found subscription index %d.", std::distance(base->subscriptions_.begin(), itSubscription));
                break;
            }
        }
	}
	catch (std::exception &ex)
	{
        CLTRACE(1, "PubSubServer: FindSubscription: Exception: %s.", ex.what());
        result = false;
	}

    return result;
}

/// <summary>
/// Free resources.
/// </summary>
STDMETHODIMP CPubSubServer::Terminate()
{
	// Remove any subscriptions owned by this instance
	CLTRACE(9, "PubSubServer: Terminate: Entry.");
    Base *base = NULL;
	try
	{
		if (_pSegment != NULL)
		{
			// An allocator convertible to any allocator<T, segment_manager_t> type.
			void_allocator alloc_inst(_pSegment->get_segment_manager());

			// Construct the shared memory Base image and initiliaze it.  This is atomic.
			base = _pSegment->find_or_construct<Base>(_ksBaseSharedMemoryObjectName)(alloc_inst);

			base->mutexSharedMemory_.lock();
			try
			{
				// Delete all of the subscriptions made by this instance.
				for (std::vector<UniqueSubscription>::iterator it = _trackedSubscriptionIds.begin(); it != _trackedSubscriptionIds.end(); ++it)
				{
					CLTRACE(9, "PubSubServer: Terminate: Call DeleteSubscriptionById.");
					DeleteSubscriptionById(base, *it);
				}
				_trackedSubscriptionIds.clear();

				ULONG processId = GetCurrentProcessId();
				ULONG threadId = GetCurrentThreadId();

				CLTRACE(9, "PubSubServer: Terminate: Print subscriptions left.  This process ID: %lx. This thread ID: %lx", processId, threadId);
    			for (subscription_vector::iterator itSubscription = base->subscriptions_.begin(); itSubscription != base->subscriptions_.end(); ++itSubscription)
				{
					CLTRACE(9, "PubSubServer: Terminate: Remaining subscription: EventType: %d. ProcessId: %lx. ThreadId: %lx. Guid: %ls.", itSubscription->nEventType_, itSubscription->uSubscribingProcessId_, itSubscription->uSubscribingThreadId_, CComBSTR(itSubscription->guidSubscriber_));
				}
				CLTRACE(9, "PubSubServer: Terminate: End of remaining subscriptions.");

				base->mutexSharedMemory_.unlock();
			}
			catch (std::exception &ex)
			{
				CLTRACE(1, "PubSubServer: Terminate: ERROR: Exception(lock).  Message: %s.", ex.what());
			}

		}
	}
	catch (std::exception &ex)
	{
		CLTRACE(1, "PubSubServer: Terminate: ERROR: Exception.  Message: %s.", ex.what());
	}

	// Release the shared memory segment if all instances are finished with it.
    try
    {
		if (_pSegment != NULL)
		{
	        _pSegment->~basic_managed_windows_shared_memory();
			_pSegment = NULL;
		}
    }
    catch (std::exception &ex)
    {
		CLTRACE(1, "PubSubServer: Terminate: ERROR: Exception(2).  Message: %s.", ex.what());
    }

    return S_OK;
}

