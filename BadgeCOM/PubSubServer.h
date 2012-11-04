// PubSubServer.h : Declaration of the CPubSubServer

#pragma once
#include "resource.h"       // main symbols
//#include <Windows.h>
#include <stdio.h>
#include <stdlib.h>
#include <boost\interprocess\managed_windows_shared_memory.hpp>
#include <boost\interprocess\containers\map.hpp>
#include <boost\interprocess\allocators\allocator.hpp>
#include <boost\interprocess\containers\vector.hpp>
#include <boost\interprocess\containers\string.hpp>
#include <boost\interprocess\sync\interprocess_mutex.hpp>
#include <boost\interprocess\sync\interprocess_semaphore.hpp>
#include <boost\interprocess\sync\scoped_lock.hpp>
#include <boost\interprocess\detail\move.hpp>
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

//	int nTest;

	STDMETHOD(Publish)(int nTestParm);

	// Typedefs of allocators and containers
	typedef managed_windows_shared_memory::segment_manager          segment_manager_t;      // this is the segment_manager

	// Define the allocators.
	typedef allocator<void, segment_manager_t>              void_allocator;         // void_allocator is convertible to any other allocator<T>.
	typedef allocator<int, segment_manager_t>               int_allocator;          // allocator for allocating ints.
	typedef vector<int, int_allocator>                      int_vector;             // an int_vector is a vector of ints.
	typedef allocator<int_vector, segment_manager_t>        int_vector_allocator;   // an allocator for allocating vectors of ints.
	typedef vector<int_vector, int_vector_allocator>        int_vector_vector;      // an int_vector_vector is a vecctor of (vectors of ints)
	typedef allocator<char, segment_manager_t>              char_allocator;         // an allocator for chars.
	typedef basic_string<char, std::char_traits<char>, char_allocator>      char_string;    // a basic_string (which supports formatting).  This is built on a collection of chars, allocated by char_allocator.
	typedef allocator<interprocess_semaphore, segment_manager_t>  semaphore_allocator;   // an allocator for interprocess_semaphore

	// Event types:
	enum EnumEventType
	{
		BadgeCom_Initilization,
		BadgeNet_AddSyncBoxFolderPath,
		BadgeNet_RemoveSyncBoxFolderPath,
		BadgeNet_AddBadgePath,
		BadgeNet_RemoveBadgePath,
	};

	// Badge types
	enum EnumCloudAppIconBadgeType
	{
		cloudAppBadgeNone = 0,				// clears a badge overlay, if any
		cloudAppBadgeSynced = 1,			// sets a badge with a checkmark or similar metaphor.
		cloudAppBadgeSyncing = 2,			// sets a badge indicating circular motion, active sync.
		cloudAppBadgeFailed = 3,			// sets a badge with an x indicating failure to sync.
		cloudAppBadgeSyncSelective = 4,		// sets a badge with an - indicating file/folder is selected not to sync.
	};

	// Event base class
	class EventMessage
	{
	public:
		EnumEventType				EventType_;
		int							ProcessId_;
		int							ThreadId_;
		EnumCloudAppIconBadgeType	BadgeType_;
		char_string					FullPath_;

		// Constructor
		EventMessage(EnumEventType EventType, int ProcessId, int ThreadId, EnumCloudAppIconBadgeType BadgeType, const char *FullPath, const void_allocator &void_alloc) :
				EventType_(EventType), ProcessId_(ProcessId), ThreadId_(ThreadId), BadgeType_(BadgeType), FullPath_(FullPath, void_alloc) {}
	};

	// Event allocators
	typedef allocator<EventMessage, segment_manager_t>		EventMessage_allocator;			// allocator for allocating EventMessage
	typedef vector<EventMessage, EventMessage_allocator>	EventMessage_vector;			// vector of EventMessage objects.

	// Subscription class
	class Subscription
	{
	public:
		offset_ptr<managed_windows_shared_memory> pSegment_;                 // pointer to the shared memory segment
		int                     nSubscribingProcessId_;     // the subscribing process ID
		int                     nSubscribingThreadId_;      // the subscribing thread ID
		int                     nBadgeTypeHandled_;         // for logging only.  The badge type  handled by this process/thread.
		int                     nEventType_;                // the event type being subscribed to
		offset_ptr<interprocess_semaphore>	pSemaphoreSubscription_;    // allows a subscribing thread to wait for events to arrive.
		bool                    fDestructed_;               // true: this object has been destructed
		EventMessage_vector		events_;					// a vector of events

		// Constructor
		Subscription(managed_windows_shared_memory *pSegment, int nSubscribingProcessId, int nSubscribingThreadId, int nBadgeTypeHandled, int nEventType, const void_allocator &void_alloc) :
							pSegment_(pSegment),
							nSubscribingProcessId_(nSubscribingProcessId), 
							nSubscribingThreadId_(nSubscribingThreadId),
							nBadgeTypeHandled_(nBadgeTypeHandled),
							nEventType_(nEventType),
							fDestructed_(false),
							events_(void_alloc)
		{
			// The interprocess_semaphore object is marked not copyable, and this prevented compilation.  Change it to a pointer
			// reference to allow the object to be copied to get past the compiler error.  The actual semaphore should be allocated in
			// shared memory by this constructor, and it should be deallocated when this subscription is destructed.
			 pSemaphoreSubscription_ = pSegment_->construct<interprocess_semaphore>(anonymous_instance)(0);
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
			pSegment_->destroy_ptr(pSemaphoreSubscription_.get());
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

	//&&&&&&&&&&&&&&&&&&&&&

	int nTest;
	STDMETHOD(get_nTestProperty)(LONG* pVal);
	STDMETHOD(put_nTestProperty)(LONG newVal);
};

OBJECT_ENTRY_AUTO(__uuidof(PubSubServer), CPubSubServer)
