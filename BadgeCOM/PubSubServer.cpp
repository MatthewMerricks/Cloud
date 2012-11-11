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

// Static constant initializers
const OLECHAR * CPubSubServer::_ksSharedMemoryName = L"CloudPubSubSharedMemory";	// the name of the shared memory segment
const int CPubSubServer::_knMaxEventsInEventQueue = 1000;							// maximum number of events allowed in a subscription's event queue
managed_windows_shared_memory *CPubSubServer::_pSegment = NULL;						// pointer to the shared memory segment
int CPubSubServer::_nSegmentReferenceCounter = 0;													// reference counter for _pSegment.

/// <summary>
/// Open or create the shared memory segment.
/// </summary>
STDMETHODIMP CPubSubServer::Initialize()
{
    HRESULT result = S_OK;

	CLTRACE(9, "PubSubServer: Initialize: Entry");
	_lockerLocal.lock();
    try
    {
		if (_nSegmentReferenceCounter == 0)
		{
	        _pSegment = new managed_windows_shared_memory(open_or_create, "CloudPubSubSharedMemory", 1024000);
		}
		++_nSegmentReferenceCounter;
    }
    catch (std::exception &ex)
    {
        _pSegment = NULL;
        result = E_FAIL;
		CLTRACE(1, "PubSubServer: Initialize: ERROR: Exception.  Message: %s.", ex.what());
    }
	_lockerLocal.unlock();

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
	if (!_pSegment == NULL || returnValue == NULL || FullPath == NULL)
	{
		CLTRACE(1, "PubSubServer: Publish: ERROR. _pSegment, FullPath and returnValue must all be non-NULL.");
		return E_POINTER;
	}

	CLTRACE(9, "PubSubServer: Publish: Entry. EventType: %d. EventSubType: %d. BadgeType: %d. FullPath: %ls.", EventType, EventSubType, BadgeType, FullPath);
    EnumPubSubServerPublishReturnCodes nResult = RC_PUBLISH_OK;                // assume no error
    bool fIsLocked = false;
    Base *base = NULL;

    try
    {
        ULONG processId = GetCurrentProcessId();
        ULONG threadId = GetCurrentThreadId();
		const int knMillisecondsToWaitEachTry = 50;
		const int knRetries = 4;

        // Open the shared memory segment, or create it if it is not there.  This is atomic.
        // An allocator convertible to any allocator<T, segment_manager_t> type.
        void_allocator alloc_inst(_pSegment->get_segment_manager());

        // Construct the shared memory Base image and initiliaze it.  This is atomic.
        base = _pSegment->find_or_construct<Base>("Base")(42, alloc_inst);

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

        // Find the subscriptions that should receive this event.
		std::vector<GUID> subscribers;
	    base->mutexSharedMemory_.lock();
        fIsLocked = true;

        // Loop through the subscriptions vector looking for this EventType.  We will deliver this event to those subscribers.
		for (subscription_vector::iterator itSubscription = base->subscriptions_.begin(); itSubscription != base->subscriptions_.end(); ++itSubscription)
        {
            // This is a subscriber if the event types match.
            EnumEventType eventType = itSubscription->nEventType_;
            if (eventType == EventType)
            {
                // This is a subscriber.  Remember its ID.
				CLTRACE(9, "PubSubServer: Publish: Subscriber with GUID<%ls> is subscribed to this event.", CComBSTR(itSubscription->guidId_));
				subscribers.push_back(itSubscription->guidId_);
            }
        }

        // Unlock
        fIsLocked = false;
        base->mutexSharedMemory_.unlock();

		// Now iterate through the subscribers attempting to deliver the event to each.
		for (std::vector<GUID>::iterator itGuid = subscribers.begin(); itGuid != subscribers.end(); ++itGuid)
		{
			for (int nRetry; nRetry < knRetries; ++nRetry)
			{
				// Find the subscription with this ID and try to deliver the event.  Lock around this action.
				base->mutexSharedMemory_.lock();
				fIsLocked = true;

				bool fEventDelivered = false;
				bool fSubscriptionRemoved = false;
				for (subscription_vector::iterator itSubscription = base->subscriptions_.begin(); itSubscription != base->subscriptions_.end(); ++itSubscription)
				{
					if (*itGuid == itSubscription->guidId_)
					{
						// This is a subscriber.  Is its event queue full?
						if (itSubscription->events_.size() >= _knMaxEventsInEventQueue)
						{
							// No more room.  Retry or delete the subscription in error.
							if (nRetry >= (knRetries - 1))
							{
								// Delete this entire subscription and log an error.
								CLTRACE(9, "PubSubServer: Publish: ERROR: Event queue full.  Delete subscription with GUID<%ls>.", CComBSTR(itSubscription->guidId_));
								base->subscriptions_.erase(itSubscription);
								nResult = RC_PUBLISH_AT_LEAST_ONE_EVENT_QUEUE_FULL;
								fSubscriptionRemoved = true;
							}
						}
						else
						{
							// Construct this event in shared memory and add it at the back of the event queue for this subscription.
							CLTRACE(9, "PubSubServer: Publish: Post this event to subscription with GUID<%ls>.", CComBSTR(itSubscription->guidId_));
							itSubscription->events_.emplace_back(EventType, EventSubType, processId, threadId, BadgeType, FullPath, alloc_inst);

							// Post the subscription's semaphore.
							itSubscription->pSemaphoreSubscription_->post();
							fEventDelivered = true;
						}
						break;
					}
				}

				// Unlock
				fIsLocked = false;
				base->mutexSharedMemory_.unlock();

				if (fEventDelivered || fSubscriptionRemoved)
				{
					break;
				}

				// Wait a short time
				CLTRACE(9, "PubSubServer: Publish: Sleep %d ms.", knMillisecondsToWaitEachTry);
				Sleep(knMillisecondsToWaitEachTry);
			}
		}
    }
    catch(std::exception &ex)
    {
		CLTRACE(1, "PubSubServer: Publish: ERROR: Exception.  Message: %s.", ex.what());
        if (fIsLocked && base != NULL)
        {
            fIsLocked = false;
            base->mutexSharedMemory_.unlock();
        }
        nResult = RC_PUBLISH_ERROR;
    }

    *returnValue = nResult;
    return S_OK;
}

/// <summary>
/// This thread wants to receive events with a particular event type.  Optionally time out if an event does not arrive within the timeout period.
/// </summary>
/// <param name="EventType">This is the event type we want to receive.</param>
/// <param name="TimeoutMilliseconds">Wait for an event for up to this period of time.  If specified as 0, the wait will not time out.</param>
/// <returns>(int via returnValue): See RC_SUBSCRIBE_*.</returns>
STDMETHODIMP CPubSubServer::Subscribe(
            EnumEventType EventType,
            GUID guidId,
            LONG TimeoutMilliseconds,
            EnumEventSubType *outEventSubType,
            EnumCloudAppIconBadgeType *outBadgeType,
            BSTR *outFullPath,
            EnumPubSubServerSubscribeReturnCodes *returnValue)
{
	if (!_pSegment == NULL || returnValue == NULL || outEventSubType == NULL || outBadgeType == NULL || outFullPath == NULL)
	{
		CLTRACE(1, "PubSubServer: Subscribe: ERROR. One or more required parameters are NULL.");
		return E_POINTER;
	}

	CLTRACE(9, "PubSubServer: Subscribe: Entry. EventType: %d. GUID: %ls. TimeoutMilliseconds: %d.", EventType, CComBSTR(guidId), TimeoutMilliseconds);
    EnumPubSubServerSubscribeReturnCodes nResult;
    bool fIsLocked = false;
    Base *base = NULL;

    try
    {
        bool fWaitRequired = false;

        ULONG processId = GetCurrentProcessId();
        ULONG threadId = GetCurrentThreadId();

        // An allocator convertible to any allocator<T, segment_manager_t> type.
        void_allocator alloc_inst(_pSegment->get_segment_manager());

        // Construct the shared memory Base image and initiliaze it.  This is atomic.
        base = _pSegment->find_or_construct<Base>("Base")(42, alloc_inst);

        // Lock the rest under the global shared memory lock in shared memory Base.
	    base->mutexSharedMemory_.lock();
        fIsLocked = true;

        // Look for this subscription.
        ULONGLONG nFoundSubscriptionIndex = UINT64_MAX;
        for (int i = 0; i < base->subscriptions_.size(); i++)
        {
            EnumEventType thisEventType = base->subscriptions_[i].nEventType_;
            GUID thisGuid = base->subscriptions_[i].guidId_;
            if (thisEventType == EventType && thisGuid == guidId)
            {
                nFoundSubscriptionIndex = i;
				CLTRACE(9, "PubSubServer: Subscribe: Found subscription index %d.", nFoundSubscriptionIndex);
                break;
            }
        }

        // Add a subscription if not found.  Otherwise, wait.
        // NOTE: There is a bug in the compiler!!   Comparing two maximum value ULONGLONG numbers is broken when the comparison is against two numbers with value 0xFFFFFFFFFFFFFFFFU.
        // The code below ends up in the "No match" block.
        //     if (nFoundSubscriptionIndex != UINT64_MAX)
        //     {
        //         // No match
        //     }
        //     else
        //     {
        //         // Match
        //     }
        // The code below is substituted to determine whether we have a valid index.
        if (nFoundSubscriptionIndex == 0
            || (nFoundSubscriptionIndex - 1) / 2 != MAXLONGLONG)
        {
            // Found our subscription.  Make sure that it has not been previously cancelled.  If so, remove the subscription.
			CLTRACE(9, "PubSubServer: Subscribe: Found our subscription.");
            if (base->subscriptions_[nFoundSubscriptionIndex].fCancelled_)
            {
				CLTRACE(9, "PubSubServer: Subscribe: Warning: Already cancelled.");
                base->subscriptions_.erase(base->subscriptions_.begin() + nFoundSubscriptionIndex);
                nResult = RC_SUBSCRIBE_ALREADY_CANCELLED;
            }
            else if (base->subscriptions_[nFoundSubscriptionIndex].events_.size() > 0)      // Check to see if we have a pending event.
            {
                // An event is waiting.  Return the information.
                *outEventSubType = base->subscriptions_[nFoundSubscriptionIndex].events_.begin()->EventSubType_;
                *outBadgeType = base->subscriptions_[nFoundSubscriptionIndex].events_.begin()->BadgeType_;
				//TODO: Is this memory freed by the subscriber?
                *outFullPath = SysAllocString(base->subscriptions_[nFoundSubscriptionIndex].events_.begin()->FullPath_.c_str());
				CLTRACE(9, "PubSubServer: Subscribe: Returned event info: EventSubType: %d. BadgeType: %d. FullPath: %ls.", *outEventSubType, *outBadgeType, outFullPath);

                // Remove the event from the vector.
				CLTRACE(9, "PubSubServer: Subscribe: Erase the event.");
				base->subscriptions_[nFoundSubscriptionIndex].events_.erase(base->subscriptions_[nFoundSubscriptionIndex].events_.begin());
                nResult = RC_SUBSCRIBE_GOT_EVENT;
            }
            else 
            {
                // No events are waiting.  We will wait for one.
				CLTRACE(9, "PubSubServer: Subscribe: No events.  A wait will be required.");
                fWaitRequired = true;
                base->subscriptions_[nFoundSubscriptionIndex].fWaiting_ = true;
            }
        }
        else
        {
            // Our subscription was not found.  Construct a new subscription at the back of the vector.  We will wait for an event.
			CLTRACE(9, "PubSubServer: Subscribe: Our subscription was not found.  Construct a new subscription.  Wait required.");
            base->subscriptions_.emplace_back(guidId, processId, threadId, EventType, alloc_inst);
            nFoundSubscriptionIndex = base->subscriptions_.size() - 1;      // the index is the index of the last subscription just added.
            fWaitRequired = true;
            base->subscriptions_[nFoundSubscriptionIndex].fWaiting_ = true;
			_subscriptionIds.push_back(guidId);								// add this subscription ID to the list of subscriptions created by this instance
        }

        // Unlock
        fIsLocked = false;
        base->mutexSharedMemory_.unlock();

        // Wait if we should.
        if (fWaitRequired)
        {
            if (TimeoutMilliseconds != 0)
            {
                // Wait for a matching event to arrive.  Use a timed wait.
				CLTRACE(9, "PubSubServer: Subscribe: Wait with timeout.");
                boost::posix_time::ptime tNow(boost::posix_time::microsec_clock::universal_time());
                bool fDidNotTimeOut = base->subscriptions_[nFoundSubscriptionIndex].pSemaphoreSubscription_->timed_wait(tNow + boost::posix_time::milliseconds(TimeoutMilliseconds));
                if (fDidNotTimeOut)
                {
					CLTRACE(9, "PubSubServer: Subscribe: Got an event. Return code 'try again'.");
                    nResult = RC_SUBSCRIBE_TRY_AGAIN;
                }
                else
                {
                    nResult = RC_SUBSCRIBE_TIMED_OUT;
                }
            }
            else
            {
                // Wait forever for a matching event to arrive.
				CLTRACE(9, "PubSubServer: Subscribe: Wait forever for an event to arrive.");
                base->subscriptions_[nFoundSubscriptionIndex].pSemaphoreSubscription_->wait();
                nResult = RC_SUBSCRIBE_TRY_AGAIN;
            }

            // Dropped out of the wait.  Clear the waiting state under the lock, and indicate an error if we dropped out of the wait due to a cancellation.
            base->mutexSharedMemory_.lock();
            fIsLocked = true;
            base->subscriptions_[nFoundSubscriptionIndex].fWaiting_ = false;

            if (base->subscriptions_[nFoundSubscriptionIndex].fCancelled_)
            {
				CLTRACE(9, "PubSubServer: Subscribe: Return code 'cancelled'.");
                nResult = RC_SUBSCRIBE_CANCELLED;
            }

            fIsLocked = false;
            base->mutexSharedMemory_.unlock();
        }
    }
    catch (std::exception &ex)
    {
		CLTRACE(1, "PubSubServer: Subscribe: ERROR: Exception.  Message: %s.", ex.what());
        if (fIsLocked && base != NULL)
        {
            fIsLocked = false;
            base->mutexSharedMemory_.unlock();
        }
        nResult = RC_SUBSCRIBE_ERROR;
    }

    *returnValue = nResult;
    return S_OK;
}


/// <Summary>
/// Break a waiting subscription out of its wait and delete the subscription.
/// </Summary>
STDMETHODIMP CPubSubServer::CancelWaitingSubscription(EnumEventType EventType, GUID guidId, EnumPubSubServerCancelWaitingSubscriptionReturnCodes *returnValue)
{
	if (!_pSegment == NULL || returnValue == NULL)
	{
		CLTRACE(1, "PubSubServer: CancelWaitingSubscription: ERROR. _pSegment, and returnValue must all be non-NULL.");
		return E_POINTER;
	}

	CLTRACE(9, "PubSubServer: CancelWaitingSubscription: Entry. EventType: %d. GUID: %ls.", EventType, CComBSTR(guidId));
    EnumPubSubServerCancelWaitingSubscriptionReturnCodes nResult;
    bool fIsLocked = false;
    Base *base = NULL;

    try
    {
        // An allocator convertible to any allocator<T, segment_manager_t> type.
        void_allocator alloc_inst(_pSegment->get_segment_manager());

        // Construct the shared memory Base image and initiliaze it.  This is atomic.
        base = _pSegment->find_or_construct<Base>("Base")(42, alloc_inst);

        // Lock the rest under the global shared memory lock in shared memory Base.
	    base->mutexSharedMemory_.lock();
        fIsLocked = true;

        // Look for this subscription.
        ULONGLONG nFoundSubscriptionIndex = UINT64_MAX;
        for (int i = 0; i < base->subscriptions_.size(); i++)
        {
            EnumEventType thisEventType = base->subscriptions_[i].nEventType_;
            GUID thisGuid = base->subscriptions_[i].guidId_;
            if (thisEventType == EventType && thisGuid == guidId)
            {
                nFoundSubscriptionIndex = i;
				CLTRACE(9, "PubSubServer: CancelWaitingSubscription: Found subscription index %d.", nFoundSubscriptionIndex);
                break;
            }
        }

        // Process by whether we found the target subscription.
        // NOTE: There is a bug in the compiler!!   Comparing two maximum value ULONGLONG numbers is broken when the comparison is against two numbers with value 0xFFFFFFFFFFFFFFFFU.
        // The code below ends up in the "No match" block.
        //     if (nFoundSubscriptionIndex != UINT64_MAX)
        //     {
        //         // No match
        //     }
        //     else
        //     {
        //         // Match
        //     }
        // The code below is substituted to determine whether we have a valid index.
        if (nFoundSubscriptionIndex == 0
            || (nFoundSubscriptionIndex - 1) / 2 != MAXLONGLONG)
        {
            // Found our subscription.  Post the semaphore to allow the waiting thread to fall through the wait.
            // Set a flag in the subscription to indicate that it is cancelled.  This will prevent the subscribing thread
            // from waiting again.
			CLTRACE(9, "PubSubServer: CancelWaitingSubscription: Found our subscription. Mark cancelled and post it.");
            base->subscriptions_[nFoundSubscriptionIndex].fCancelled_ = true;
            base->subscriptions_[nFoundSubscriptionIndex].pSemaphoreSubscription_->post();

            // Give the thread a chance to exit the wait.
            bool fCancelOk = false;
            for (int i = 0; i < 5; i++)
            {
                // Give up some cycles.  Free the lock momentarily.
                fIsLocked = false;
                base->mutexSharedMemory_.unlock();
                Sleep(1);
                base->mutexSharedMemory_.lock();
                fIsLocked = true;

                // If not waiting now, remove the subscription
                if (!base->subscriptions_[nFoundSubscriptionIndex].fWaiting_)
                {
					// Remove this subscription ID from the list of subscriptions created by this instance.
					_subscriptionIds.erase(std::find(_subscriptionIds.begin(), _subscriptionIds.end(), base->subscriptions_[nFoundSubscriptionIndex].guidId_));

					// Delete the subscription itself
                    base->subscriptions_.erase(base->subscriptions_.begin() + nFoundSubscriptionIndex);
                    fCancelOk = true;
                    break;
                }
            }

            if (fCancelOk)
            {
				CLTRACE(1, "PubSubServer: CancelWaitingSubscription: Return result 'Cancelled'.");
                nResult = RC_CANCEL_CANCELLED;
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

        fIsLocked = false;
        base->mutexSharedMemory_.unlock();

    }
    catch (std::exception &ex)
    {
		CLTRACE(1, "PubSubServer: CancelWaitingSubscription: ERROR: Exception.  Message: %s.", ex.what());
        if (fIsLocked && base != NULL)
        {
            fIsLocked = false;
            base->mutexSharedMemory_.unlock();
        }
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
	if (!_pSegment == NULL)
	{
		return E_POINTER;
	}

    HRESULT nResult = S_OK;

    try
    {
        *pVal = SysAllocString(_ksSharedMemoryName);
    }
    catch(std::exception &ex)
    {
        nResult = E_OUTOFMEMORY;
    }

    return nResult;
}


/// <summary>
/// Delete a subscription from shared memory by GUID ID.  Assumes that the caller holds the shared memory lock.
/// </summary>
void CPubSubServer::DeleteSubscriptionByGuid(Base * base, GUID guid)
{
	try
	{
		// Look for the subscription
		for (subscription_vector::iterator itSubscription = base->subscriptions_.begin(); itSubscription != base->subscriptions_.end(); /* iterator bump handled in body */)
		{
			if (itSubscription->guidId_ == guid)
			{
				CLTRACE(9, "PubSubServer: DeleteSubscriptionByGuid: Delete subscription with GUID: %ls.", CComBSTR(guid));
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
	}
}

/// <summary>
/// Free resources.
/// </summary>
STDMETHODIMP CPubSubServer::Terminate()
{
	// Remove any subscriptions owned by this instance
	CLTRACE(9, "PubSubServer: Terminate: Entry.");
    bool fIsLocked = false;
    Base *base = NULL;
	try
	{
		if (_pSegment != NULL)
		{
			// An allocator convertible to any allocator<T, segment_manager_t> type.
			void_allocator alloc_inst(_pSegment->get_segment_manager());

			// Construct the shared memory Base image and initiliaze it.  This is atomic.
			base = _pSegment->find_or_construct<Base>("Base")(42, alloc_inst);

			// Lock the rest under the global shared memory lock in shared memory Base.
			base->mutexSharedMemory_.lock();
			fIsLocked = true;

			for (std::vector<GUID>::iterator it = _subscriptionIds.begin(); it != _subscriptionIds.end(); ++it)
			{
				CLTRACE(9, "PubSubServer: Terminate: Call DeleteSubscriptionByGuid for GUID: %ls.", CComBSTR(*it));
				DeleteSubscriptionByGuid(base, *it);
			}

			fIsLocked = false;
			base->mutexSharedMemory_.unlock();
		}
	}
	catch (std::exception &ex)
	{
		CLTRACE(1, "PubSubServer: Terminate: ERROR: Exception.  Message: %s.", ex.what());
        if (fIsLocked && base != NULL)
        {
            fIsLocked = false;
            base->mutexSharedMemory_.unlock();
        }
	}

	// Release the shared memory segment if all instances are finished with it.
	_lockerLocal.lock();
    try
    {
		--_nSegmentReferenceCounter;
		if (_nSegmentReferenceCounter < 0)
		{
			_nSegmentReferenceCounter = 0;
		}

        if (_nSegmentReferenceCounter == 0)
        {
			if (_pSegment != NULL)
			{
	            _pSegment->~basic_managed_windows_shared_memory();
				_pSegment = NULL;
			}
        }
    }
    catch (std::exception &ex)
    {
		CLTRACE(1, "PubSubServer: Terminate: ERROR: Exception(2).  Message: %s.", ex.what());
    }
	_lockerLocal.unlock();

    return S_OK;
}

