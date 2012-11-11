//
// PubSubServer.h
// Cloud Windows COM
//
// Created By BobS.
// Copyright (c) Cloud.com. All rights reserved.

#pragma once
#include "resource.h"       // main symbols
#include <stdio.h>
#include <stdlib.h>
#include <iostream>
#include <exception>
#include <limits.h>
#include <boost\interprocess\managed_windows_shared_memory.hpp>
#include <boost\interprocess\containers\map.hpp>
#include <boost\interprocess\allocators\allocator.hpp>
#include <boost\interprocess\containers\vector.hpp>
#include <boost\interprocess\containers\string.hpp>
#include <boost\interprocess\sync\interprocess_mutex.hpp>
#include <boost\interprocess\sync\interprocess_semaphore.hpp>
#include <boost\interprocess\sync\scoped_lock.hpp>
#include <boost\interprocess\detail\move.hpp>
#include <boost\thread.hpp>
#include "Trace.h"
#include "BadgeCOM_i.h"

#if defined(_WIN32_WCE) && !defined(_CE_DCOM) && !defined(_CE_ALLOW_SINGLE_THREADED_OBJECTS_IN_MTA)
#error "Single-threaded COM objects are not properly supported on Windows CE platform, such as the Windows Mobile platforms that do not include full DCOM support. Define _CE_ALLOW_SINGLE_THREADED_OBJECTS_IN_MTA to force ATL to support creating single-thread COM object's and allow use of it's single-threaded COM object implementations. The threading model in your rgs file was set to 'Free' as that is the only threading model supported in non DCOM Windows CE platforms."
#endif

using namespace ATL;
using namespace boost::interprocess;


// CPubSubServer

class ATL_NO_VTABLE CPubSubServer :
	public CComObjectRootEx<CComSingleThreadModel>,
	public CComCoClass<CPubSubServer, &CLSID_PubSubServer>,
	public IPubSubServer
{
public:
	CPubSubServer()
	{
	}

DECLARE_REGISTRY_RESOURCEID(IDR_PUBSUBSERVER)


BEGIN_COM_MAP(CPubSubServer)
	COM_INTERFACE_ENTRY(IPubSubServer)
END_COM_MAP()



	DECLARE_PROTECT_FINAL_CONSTRUCT()

	HRESULT FinalConstruct()
	{
		return S_OK;
	}

	void FinalRelease()
	{
	}

public:

	// Typedefs of allocators and containers
	typedef managed_windows_shared_memory::segment_manager          segment_manager_t;      // this is the segment_manager

	// Define the allocators.
	typedef allocator<void, segment_manager_t>              void_allocator;         // void_allocator is convertible to any other allocator<T>.
	typedef allocator<int, segment_manager_t>               int_allocator;          // allocator for allocating ints.
	typedef vector<int, int_allocator>                      int_vector;             // an int_vector is a vector of ints.
	typedef allocator<int_vector, segment_manager_t>        int_vector_allocator;   // an allocator for allocating vectors of ints.
	typedef vector<int_vector, int_vector_allocator>        int_vector_vector;      // an int_vector_vector is a vecctor of (vectors of ints)
	typedef allocator<interprocess_semaphore, segment_manager_t>  semaphore_allocator;   // an allocator for interprocess_semaphore
    typedef allocator<WCHAR, segment_manager_t>             wchar_allocator;        // an allocator for wide chars.
    typedef basic_string<WCHAR, std::char_traits<WCHAR>, wchar_allocator>  wchar_string;  // a basic_string (which supports formatting).  This is built on a collection of wide chars, allocated by wchar_alloctor.

	// Event types:  Note this is defined in the .IDL file.
	//enum EnumEventTypeX
	//{
	//	BadgeCom_Initilization,
	//	BadgeNet_AddSyncBoxFolderPath,
	//	BadgeNet_RemoveSyncBoxFolderPath,
	//	BadgeNet_AddBadgePath,
	//	BadgeNet_RemoveBadgePath,
	//};

	// Badge types:  Note this is defined in the .IDL file.
	//enum EnumCloudAppIconBadgeTypeX
	//{
	//	cloudAppBadgeNone = 0,				// clears a badge overlay, if any
	//	cloudAppBadgeSynced = 1,			// sets a badge with a checkmark or similar metaphor.
	//	cloudAppBadgeSyncing = 2,			// sets a badge indicating circular motion, active sync.
	//	cloudAppBadgeFailed = 3,			// sets a badge with an x indicating failure to sync.
	//	cloudAppBadgeSyncSelective = 4,		// sets a badge with an - indicating file/folder is selected not to sync.
	//};

	// Event base class
	class EventMessage
	{
	public:
		EnumEventType				EventType_;                 // this event's event type
        EnumEventSubType            EventSubType_;              // this event's event subtype
		ULONG						ProcessId_;                 // for logging only
		ULONG						ThreadId_;                  // for logging only
		EnumCloudAppIconBadgeType	BadgeType_;                 // the badge type associated with this event
		wchar_string				FullPath_;                  // the full path associated with this event

		// Constructor
		EventMessage(EnumEventType EventType, EnumEventSubType EventSubType, ULONG ProcessId, ULONG ThreadId, EnumCloudAppIconBadgeType BadgeType, BSTR *FullPath, const void_allocator &void_alloc) :
				EventType_(EventType), EventSubType_(EventSubType), ProcessId_(ProcessId), ThreadId_(ThreadId), BadgeType_(BadgeType), FullPath_(*FullPath, void_alloc) {}
	};

	// Event allocators
	typedef allocator<EventMessage, segment_manager_t>		EventMessage_allocator;			// allocator for allocating EventMessage
	typedef vector<EventMessage, EventMessage_allocator>	EventMessage_vector;			// vector of EventMessage objects.

	// Subscription class
	class Subscription
	{
	public:
		ULONG                       uSubscribingProcessId_;     // the subscribing process ID (logging only)
		ULONG                       uSubscribingThreadId_;      // the subscribing thread ID (logging only)
		EnumEventType               nEventType_;                // the event type being subscribed to
		offset_ptr<interprocess_semaphore>	pSemaphoreSubscription_;    // allows a subscribing thread to wait for events to arrive.
        GUID                        guidId_;                    // the unique identifier of this subscription
		bool                        fDestructed_;               // true: this object has been destructed
        bool                        fWaiting_;                  // true: the subscribing thread is waiting
        bool                        fCancelled_;                // true: this subscription has been cancelled.
		EventMessage_vector		    events_;					// a vector of events

		// Constructor
		Subscription(GUID guidId, ULONG uSubscribingProcessId, ULONG uSubscribingThreadId, EnumEventType nEventType, const void_allocator &void_alloc) :
							uSubscribingProcessId_(uSubscribingProcessId), 
							uSubscribingThreadId_(uSubscribingThreadId),
							nEventType_(nEventType),
                            guidId_(guidId),
							fDestructed_(false),
							fWaiting_(false),
							fCancelled_(false),
							events_(void_alloc)
		{
			// The interprocess_semaphore object is marked not copyable, and this prevented compilation.  Change it to a pointer
			// reference to allow the object to be copied to get past the compiler error.  The actual semaphore should be allocated in
			// shared memory by this constructor, and it should be deallocated when this subscription is destructed.
			 pSemaphoreSubscription_ = _pSegment->construct<interprocess_semaphore>(anonymous_instance)(0);
		}

		// Destructor
		~Subscription()
		{
			// Don't do this twice
			if (fDestructed_)
			{
				return;
			}
			fDestructed_ = true;

			// Deallocate the semaphore
			_pSegment->destroy_ptr(pSemaphoreSubscription_.get());
		}
	};

	// Define the types related to Subscription
	typedef allocator<Subscription, segment_manager_t>                      subscription_allocator;     // allocator for allocating Subscription
	typedef vector<Subscription, subscription_allocator>                    subscription_vector;    // a vector of Subscriptions
	typedef allocator<subscription_vector, segment_manager_t>               subscription_vector_allocator;  // allocator for allocating a vector of Subscription.

	// Base class to hold all of the data in shared memory.
	class Base
	{
	public:
		// Put any required global data here.
		int                 nSampleInt_;         // sample global data
		interprocess_mutex  mutexSharedMemory_;  // lock to protect the whole Base object
		subscription_vector subscriptions_;      // a vector of Subscription (the active subscriptions)

		Base(int nSampleInt, const void_allocator &void_alloc) : nSampleInt_(nSampleInt), subscriptions_(void_alloc) {}
	};

	// Definition of the map holding all of the data.  There will just be a single map element with key "base".  The value
	// will be a complex container containing any required global data, plus the vector<Subscription>.
	typedef allocator<Base, segment_manager_t>                              base_allocator;       // allocator for allocating Base

	// Public OLE accessible methods
    STDMETHOD(Initialize)();
    STDMETHOD(Publish)(EnumEventType EventType,
            EnumEventSubType EventSubType, 
            EnumCloudAppIconBadgeType BadgeType, 
            BSTR *FullPath, 
            EnumPubSubServerPublishReturnCodes *returnValue);
    STDMETHOD(Subscribe)(
            EnumEventType EventType,
            GUID guidId,
            LONG TimeoutMilliseconds,
            EnumEventSubType *outEventSubType,
            EnumCloudAppIconBadgeType *outBadgeType,
            BSTR *outFullPath,
            EnumPubSubServerSubscribeReturnCodes *returnValue);
	STDMETHOD(CancelWaitingSubscription)(EnumEventType EventType, GUID guidId, EnumPubSubServerCancelWaitingSubscriptionReturnCodes *returnValue);
    STDMETHOD(Terminate)();

    // Public OLE accessible properties
    STDMETHOD(get_SharedMemoryName)(BSTR* pVal);
private:
	// Private methods
	void DeleteSubscriptionByGuid(Base * base, GUID guid);

	// Private instance fields
	std::vector<GUID> _subscriptionIds;						// list of subscriptions created by this instance
	boost::mutex _lockerLocal;								// local locker
	

	// Private static fields
    static managed_windows_shared_memory *_pSegment;		// Pointer to the managed native windows shared memory segment.
	static int _nSegmentReferenceCounter;					// reference counter for _pSegment.

    // Private static constants
    static const OLECHAR *_ksSharedMemoryName;              // the name of the shared memory segment
    static const int _knMaxEventsInEventQueue;              // maximum number of events allowed in a subscription's event queue
};

OBJECT_ENTRY_AUTO(__uuidof(PubSubServer), CPubSubServer)
