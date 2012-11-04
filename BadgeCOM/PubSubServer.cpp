// PubSubServer.cpp : Implementation of CPubSubServer

#include "stdafx.h"
#include "PubSubServer.h"

//TODO: Remove or rework _tmain and dependencies.
// Forward function definitions.
DWORD ThreadProc(LPVOID lpdwThreadParam);

int _tmain(int argc, _TCHAR* argv[])
{
	//&&&&&&&&&&& end


    // Start multiple threads
    int nThreads = 5;
    DWORD i;

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

STDMETHODIMP CPubSubServer::Publish(int nTestParm)
{
	bool isSubscriber = true;

    // Open the shared memory segment, or create it if it is not there.
    managed_windows_shared_memory segment(open_or_create, "CloudPubSubSharedMemory", 1024000);

    // An allocator convertible to any allocator<T, segment_manager_t> type.
    void_allocator alloc_inst(segment.get_segment_manager());

    // Construct the shared memory Base image and initiliaze it.
    Base *base = segment.find_or_construct<Base>("Base")(42, alloc_inst);

    // Lock the rest under the global shared memory lock.
	base->mutexSharedMemory_.lock();

	if (isSubscriber)
	{
		// This is the subscriber. Add a Subscription to the end of the subscriptions vector.
		base->subscriptions_.emplace_back(&segment, 1, 2, 3, 4, alloc_inst);

		// Are there events available?
		if (base->subscriptions_[0].events_.size() > 0)
		{
			// Loop retrieving the events from the events_ vector.
			int rc = base->subscriptions_[0].events_.size();
			while (base->subscriptions_[0].events_.size() > 0)
			{
				// Read the event at the front of the vector.
				void *eventAddr = &(base->subscriptions_[0].events_[0]);
				int eventType = base->subscriptions_[0].events_[0].EventType_;
				void *eventTypeAddr = &(base->subscriptions_[0].events_[0].EventType_);
				EnumCloudAppIconBadgeType badgeType = base->subscriptions_[0].events_[0].BadgeType_;
				void *badgeTypeAddr = &(base->subscriptions_[0].events_[0].BadgeType_);
				char_string fullPath = base->subscriptions_[0].events_[0].FullPath_;
				void *fullPathAddr = &(base->subscriptions_[0].events_[0].FullPath_);
				std::string fullPathString(fullPath.c_str());
				int processId = base->subscriptions_[0].events_[0].ProcessId_;
				void *processIdAddr = &(base->subscriptions_[0].events_[0].ProcessId_);
				int threadId = base->subscriptions_[0].events_[0].ThreadId_;
				void *threadIdAddr = &(base->subscriptions_[0].events_[0].ThreadId_);

			    // Delete the Subscription from the shared memory shared memory.
				base->subscriptions_.erase(base->subscriptions_.begin());
			}

			base->mutexSharedMemory_.unlock();
			return(rc);
		}
		else
		{
			// Wait for an event to be posted.
			base->mutexSharedMemory_.unlock();
			base->subscriptions_[0].pSemaphoreSubscription_->wait();

			// One or more events have been added to the vector.  Return zero events to the caller.  The caller
			// will loop back and retrieve the events from the queue (above).
			return(0);
		}

	}
	else
	{
		// This is the publisher.  Add an event to the first Subscription.
		base->subscriptions_[0].events_.emplace_back(BadgeNet_AddBadgePath, 88, 99, cloudAppBadgeSyncing, "Badge full path", alloc_inst);

		// Read the event at the front of the vector.
		void *eventAddr = &(base->subscriptions_[0].events_[0]);
		int eventType = base->subscriptions_[0].events_[0].EventType_;
		void *eventTypeAddr = &(base->subscriptions_[0].events_[0].EventType_);
		EnumCloudAppIconBadgeType badgeType = base->subscriptions_[0].events_[0].BadgeType_;
		void *badgeTypeAddr = &(base->subscriptions_[0].events_[0].BadgeType_);
		char_string fullPath = base->subscriptions_[0].events_[0].FullPath_;
		void *fullPathAddr = &(base->subscriptions_[0].events_[0].FullPath_);
		std::string fullPathString(fullPath.c_str());
		int processId = base->subscriptions_[0].events_[0].ProcessId_;
		void *processIdAddr = &(base->subscriptions_[0].events_[0].ProcessId_);
		int threadId = base->subscriptions_[0].events_[0].ThreadId_;
		void *threadIdAddr = &(base->subscriptions_[0].events_[0].ThreadId_);

		// Post the subscriber to look for this event just added.
		base->subscriptions_[0].pSemaphoreSubscription_->post();

		base->mutexSharedMemory_.unlock();
		return(0);
	}
	return(0);
}



STDMETHODIMP CPubSubServer::get_nTestProperty(LONG* pVal)
{
	// TODO: Add your implementation code here

	return S_OK;
}


STDMETHODIMP CPubSubServer::put_nTestProperty(LONG newVal)
{
	// TODO: Add your implementation code here

	return S_OK;
}
