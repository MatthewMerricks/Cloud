// PubSubServer.cpp : Implementation of CPubSubServer

#include "stdafx.h"
#include "PubSubServer.h"

// Static constant initializers
const OLECHAR * CPubSubServer::_ksSharedMemoryName = L"CloudPubSubSharedMemory";   // the name of the shared memory segment

const int CPubSubServer::_knMaxEventsInEventQueue = 500;                            // maximum number of events allowed in a subscription's event queue

managed_windows_shared_memory CPubSubServer::_segment(open_or_create, "CloudPubSubSharedMemory", 1024000);


//TODO: Remove or rework _tmain and dependencies.
// Forward function definitions.
DWORD ThreadProc(LPVOID lpdwThreadParam);

int _tmain(int argc, _TCHAR* argv[])
{
	//&&&&&&&&&&& end


    // Start multiple threads
    int nThreads = 5;
    int i;

    for (i = 0; i < nThreads; i++)
    {
        DWORD dwThreadId;
        if (CreateThread(NULL,  // default security
                    0,  // default stack size
                    (LPTHREAD_START_ROUTINE) &ThreadProc,  // function to run
                    (LPVOID) &i,        // thread parameter
                    0,                  // imediately run the thread
                    &dwThreadId         // output thread ID
                    ) == NULL)
        {
            printf("Error creating thread#: %d.\n", i);
        }

    }

    std::cout << std::endl << "Press any key to continue...\n";
    getchar();


	return(0);
}

// Thread function
DWORD ThreadProc(LPVOID lpdwThreadParam)
{
    // print thread number
    printf("Thread #: %d started.\n", ((int *)lpdwThreadParam));

    // Loop getting and processing events from this process/thread's queue in shared memory.
    while (true)
    {

    }


    return(0);
}

//STDMETHODIMP CPubSubServer::Publish(int nTestParm)
//{
//	bool isSubscriber = true;
//
//    // Open the shared memory segment, or create it if it is not there.
//    managed_windows_shared_memory segment(open_or_create, "CloudPubSubSharedMemory", 1024000);
//
//    // An allocator convertible to any allocator<T, segment_manager_t> type.
//    void_allocator alloc_inst(segment.get_segment_manager());
//
//    // Construct the shared memory Base image and initiliaze it.
//    Base *base = segment.find_or_construct<Base>("Base")(42, alloc_inst);
//
//    // Lock the rest under the global shared memory lock.
//	base->mutexSharedMemory_.lock();
//
//	if (isSubscriber)
//	{
//		// This is the subscriber. Add a Subscription to the end of the subscriptions vector.
//		base->subscriptions_.emplace_back(&segment, 1, 2, 3, 4, alloc_inst);
//
//		// Are there events available?
//		if (base->subscriptions_[0].events_.size() > 0)
//		{
//			// Loop retrieving the events from the events_ vector.
//			int rc = base->subscriptions_[0].events_.size();
//			while (base->subscriptions_[0].events_.size() > 0)
//			{
//				// Read the event at the front of the vector.
//				void *eventAddr = &(base->subscriptions_[0].events_[0]);
//				int eventType = base->subscriptions_[0].events_[0].EventType_;
//				void *eventTypeAddr = &(base->subscriptions_[0].events_[0].EventType_);
//				EnumCloudAppIconBadgeType badgeType = base->subscriptions_[0].events_[0].BadgeType_;
//				void *badgeTypeAddr = &(base->subscriptions_[0].events_[0].BadgeType_);
//				char_string fullPath = base->subscriptions_[0].events_[0].FullPath_;
//				void *fullPathAddr = &(base->subscriptions_[0].events_[0].FullPath_);
//				std::string fullPathString(fullPath.c_str());
//				int processId = base->subscriptions_[0].events_[0].ProcessId_;
//				void *processIdAddr = &(base->subscriptions_[0].events_[0].ProcessId_);
//				int threadId = base->subscriptions_[0].events_[0].ThreadId_;
//				void *threadIdAddr = &(base->subscriptions_[0].events_[0].ThreadId_);
//
//			    // Delete the Subscription from the shared memory shared memory.
//				base->subscriptions_.erase(base->subscriptions_.begin());
//			}
//
//			base->mutexSharedMemory_.unlock();
//			return(rc);
//		}
//		else
//		{
//			// Wait for an event to be posted.
//			base->mutexSharedMemory_.unlock();
//			base->subscriptions_[0].pSemaphoreSubscription_->wait();
//
//			// One or more events have been added to the vector.  Return zero events to the caller.  The caller
//			// will loop back and retrieve the events from the queue (above).
//			return(0);
//		}
//
//	}
//	else
//	{
//		// This is the publisher.  Add an event to the first Subscription.
//		base->subscriptions_[0].events_.emplace_back(BadgeNet_AddBadgePath, 88, 99, cloudAppBadgeSyncing, "Badge full path", alloc_inst);
//
//		// Read the event at the front of the vector.
//		void *eventAddr = &(base->subscriptions_[0].events_[0]);
//		int eventType = base->subscriptions_[0].events_[0].EventType_;
//		void *eventTypeAddr = &(base->subscriptions_[0].events_[0].EventType_);
//		EnumCloudAppIconBadgeType badgeType = base->subscriptions_[0].events_[0].BadgeType_;
//		void *badgeTypeAddr = &(base->subscriptions_[0].events_[0].BadgeType_);
//		char_string fullPath = base->subscriptions_[0].events_[0].FullPath_;
//		void *fullPathAddr = &(base->subscriptions_[0].events_[0].FullPath_);
//		std::string fullPathString(fullPath.c_str());
//		int processId = base->subscriptions_[0].events_[0].ProcessId_;
//		void *processIdAddr = &(base->subscriptions_[0].events_[0].ProcessId_);
//		int threadId = base->subscriptions_[0].events_[0].ThreadId_;
//		void *threadIdAddr = &(base->subscriptions_[0].events_[0].ThreadId_);
//
//		// Post the subscriber to look for this event just added.
//		base->subscriptions_[0].pSemaphoreSubscription_->post();
//
//		base->mutexSharedMemory_.unlock();
//		return(0);
//	}
//	return(0);
//}

/// <summary>
/// This thread is publishing an event to multiple subscribers who may be waiting to be notified.  
/// All subscribers to this event type and badge type will be notified.
/// </summary>
/// <param name="EventType">This is the event type being published.</param>
/// <param name="BadgeType">This is the badge type being published.</param>
/// <param name="FullPath">This is the full path of the file system item being badged with the badge type.</param>
/// <returns>(int via returnValue: See RC_PUBLISH_*.</returns>
STDMETHODIMP CPubSubServer::Publish(EnumEventType EventType, EnumCloudAppIconBadgeType BadgeType, BSTR *FullPath, int *returnValue)
{
    int nResult = RC_PUBLISH_OK;                // assume no error

    try
    {
        ULONG processId = GetCurrentProcessId();
        ULONG threadId = GetCurrentThreadId();

        // Open the shared memory segment, or create it if it is not there.  This is atomic.
        // An allocator convertible to any allocator<T, segment_manager_t> type.
        void_allocator alloc_inst(_segment.get_segment_manager());

        // Construct the shared memory Base image and initiliaze it.  This is atomic.
        Base *base = _segment.find_or_construct<Base>("Base")(42, alloc_inst);

        // Lock the rest under the global shared memory lock in shared memory Base.
	    base->mutexSharedMemory_.lock();

        // Loop through the subscriptions vector looking for this EventType and BadgeType.  We will process all such subscriptions found.
        subscription_vector::iterator itSubscription = base->subscriptions_.begin();
        while (itSubscription != base->subscriptions_.end())
        {
            // Process this subscription if the event types match.
            bool fBumpUpIterator = true;
            EnumEventType eventType = itSubscription->nEventType_;
            if (eventType == EventType)
            {
                // Is the event queue full?
                if (itSubscription->events_.size() >= _knMaxEventsInEventQueue)
                {
                    // No more room.  Delete this entire subscription and log an error.
                    fBumpUpIterator = false;
                    itSubscription = base->subscriptions_.erase(itSubscription);
                    nResult = RC_PUBLISH_AT_LEAST_ONE_EVENT_QUEUE_FULL;
                }
                else
                {
                    // Construct this event in shared memory and add it at the back of the event queue for this subscription.
                    itSubscription->events_.emplace_back(EventType, processId, threadId, BadgeType, FullPath, alloc_inst);

                    // Post the subscription's semaphore.
                    base->subscriptions_[0].pSemaphoreSubscription_->post();
                }
            }

            // Bump up the iterator.  Note that the iterator may have been adjusted if we erased the subscription.
            if (fBumpUpIterator)
            {
                itSubscription++;
            }
        }

        // Unlock
        base->mutexSharedMemory_.unlock();

    }
    catch(std::exception &ex)
    {
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
STDMETHODIMP CPubSubServer::Subscribe(EnumEventType EventType, LONG TimeoutMilliseconds, int *returnValue)
{
    int nResult;

    try
    {
        bool fWaitRequired = false;

        ULONG processId = GetCurrentProcessId();
        ULONG threadId = GetCurrentThreadId();

        // An allocator convertible to any allocator<T, segment_manager_t> type.
        void_allocator alloc_inst(_segment.get_segment_manager());

        // Construct the shared memory Base image and initiliaze it.  This is atomic.
        Base *base = _segment.find_or_construct<Base>("Base")(42, alloc_inst);

        // Lock the rest under the global shared memory lock in shared memory Base.
	    base->mutexSharedMemory_.lock();

        // Look for this subscription.
        ULONGLONG nFoundSubscriptionIndex = UINT64_MAX;
        for (int i = 0; i < base->subscriptions_.size(); i++)
        {
            EnumEventType thisEventType = base->subscriptions_[i].nEventType_;
            ULONG thisProcessId = base->subscriptions_[i].uSubscribingProcessId_;
            ULONG thisThreadId = base->subscriptions_[i].uSubscribingThreadId_;
            if (thisEventType == EventType && thisProcessId == processId && thisThreadId == threadId)
            {
                nFoundSubscriptionIndex = i;
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
            // Found our subscription.  Check to see if we have a pending event.
            if (base->subscriptions_[nFoundSubscriptionIndex].events_.size() > 0)
            {
                // An event is waiting.  Remove the front event from the vector.
				base->subscriptions_[nFoundSubscriptionIndex].events_.erase(base->subscriptions_[nFoundSubscriptionIndex].events_.begin());
                nResult = RC_SUBSCRIBE_GOT_EVENT;
            }
            else 
            {
                // No events are waiting.  We will wait for one.
                fWaitRequired = true;
            }
        }
        else
        {
            // Our subscription was not found.  Construct a new subscription at the back of the vector.  We will wait for an event.
            base->subscriptions_.emplace_back(&_segment, processId, threadId, EventType, alloc_inst);
            nFoundSubscriptionIndex = base->subscriptions_.size() - 1;      // the index is the index of the last subscription just added.
            fWaitRequired = true;
        }

        // Unlock
        base->mutexSharedMemory_.unlock();

        // Wait if we should.
        if (fWaitRequired)
        {
            if (TimeoutMilliseconds != 0)
            {
                // Wait for a matching event to arrive.  Use a timed wait.
                boost::posix_time::ptime tNow(boost::posix_time::microsec_clock::universal_time());
                bool fDidNotTimeOut = base->subscriptions_[nFoundSubscriptionIndex].pSemaphoreSubscription_->timed_wait(tNow + boost::posix_time::milliseconds(TimeoutMilliseconds));
                if (fDidNotTimeOut)
                {
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
                base->subscriptions_[nFoundSubscriptionIndex].pSemaphoreSubscription_->wait();
                nResult = RC_SUBSCRIBE_TRY_AGAIN;
            }
        }
    }
    catch (std::exception &ex)
    {
        nResult = RC_SUBSCRIBE_ERROR;
    }

    *returnValue = nResult;
    return S_OK;
}

STDMETHODIMP CPubSubServer::get_SharedMemoryName(BSTR* pVal)
{
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
