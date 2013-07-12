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
#include <WinVer.h>
#include <boost\interprocess\managed_windows_shared_memory.hpp>
#include <boost\interprocess\allocators\allocator.hpp>
#include <boost\interprocess\containers\map.hpp>
#include <boost\interprocess\containers\vector.hpp>
#include <boost\interprocess\containers\string.hpp>
#include <boost\interprocess\containers\flat_set.hpp>
#include <boost\interprocess\containers\flat_map.hpp>
#include <boost\interprocess\sync\interprocess_mutex.hpp>
#include <boost\interprocess\sync\interprocess_semaphore.hpp>
#include <boost\interprocess\sync\scoped_lock.hpp>
#include <boost\interprocess\detail\move.hpp>
#include <boost\thread.hpp>
#include <boost\foreach.hpp>
#include <boost\format.hpp>
#include <functional>
#include <boost/functional/hash.hpp>
#include "Trace.h"
#include "BadgeCOM_i.h"
#include "dllmain.h"

#if defined(_WIN32_WCE) && !defined(_CE_DCOM) && !defined(_CE_ALLOW_SINGLE_THREADED_OBJECTS_IN_MTA)
#error "Single-threaded COM objects are not properly supported on Windows CE platform, such as the Windows Mobile platforms that do not include full DCOM support. Define _CE_ALLOW_SINGLE_THREADED_OBJECTS_IN_MTA to force ATL to support creating single-thread COM object's and allow use of it's single-threaded COM object implementations. The threading model in your rgs file was set to 'Free' as that is the only threading model supported in non DCOM Windows CE platforms."
#endif

// Debug trace
#define CLTRACE(intPriority, szFormat, ...) Trace::getInstance()->write(intPriority, szFormat, __VA_ARGS__)
#define CLTRACE_DUMP(pData, usDataLength, intPriority, szFormat, ...) Trace::getInstance()->writeDumpData(pData, usDataLength, intPriority, szFormat, __VA_ARGS__)

using namespace ATL;
using namespace boost::interprocess;

// Forward function definitions
static std::size_t FNV_hash(const void* dataToHash, std::size_t length);
static std::size_t hash_value(GUID const& guid);

// Constants
static const uint64_t _kuEventSignature = 0x1212121212121212;				// event object signature
static const uint64_t _kuSubscriptionSignature = 0xCACACACACACACACA;		// subscription object signature
static const uint64_t _kuBaseSignature = 0xACACACACACACACAC;				// base object signature

// CPubSubServer

class ATL_NO_VTABLE CPubSubServer :
	public CComObjectRootEx<CComSingleThreadModel>,
	public CComCoClass<CPubSubServer, &CLSID_PubSubServer>,
	public IPubSubServer
{
public:
	CPubSubServer()
	{
		_fIsTerminating = false;
	}

	// Default implementation does not pass _ATL_REGMAP_ENTRY array used in:
	//DECLARE_REGISTRY_RESOURCEID(IDR_PUBSUBSERVER)
	// Instead, call UpdateRegistry and pass in some substitutable parameters
	static HRESULT WINAPI UpdateRegistry(BOOL bRegister)
	{
		return _AtlModule.UpdateRegistryFromResource(IDR_PUBSUBSERVER, bRegister, RegEntries);
	}





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

	// A GUID comparator for std::less<GUID>.
	struct GUIDPairsComparer
	{
	    bool operator()(const GUID &left, const GUID &right) const
	    {
			if (left.Data1 < right.Data1)
				return true; 
			else if (left.Data1 > right.Data1)
				return false;
			if (left.Data2 < right.Data2)
				return true; 
			else if (left.Data2 > right.Data2)
				return false;    
			if (left.Data3 < right.Data3)
				return true; 
			else if (left.Data3 > right.Data3)
				return false;
			return memcmp(left.Data4, right.Data4, 8) < 0;
		}
	};

	// Typedefs of allocators and containers
	typedef managed_windows_shared_memory::segment_manager          segment_manager_t;      // this is the segment_manager

	// Define the allocators.
	typedef boost::interprocess::allocator<void, segment_manager_t>              void_allocator;         // void_allocator is convertible to any other allocator<T>.
	typedef boost::interprocess::allocator<int, segment_manager_t>               int_allocator;          // allocator for allocating ints.
	typedef boost::interprocess::vector<int, int_allocator>                      int_vector;             // an int_vector is a vector of ints.
	typedef boost::interprocess::allocator<int_vector, segment_manager_t>        int_vector_allocator;   // an allocator for allocating vectors of ints.
	typedef boost::interprocess::vector<int_vector, int_vector_allocator>        int_vector_vector;      // an int_vector_vector is a vecctor of (vectors of ints)
	typedef boost::interprocess::allocator<interprocess_semaphore, segment_manager_t>  semaphore_allocator;   // an allocator for interprocess_semaphore
    typedef boost::interprocess::allocator<WCHAR, segment_manager_t>             wchar_allocator;        // an allocator for wide chars.
    typedef boost::interprocess::basic_string<WCHAR, std::char_traits<WCHAR>, wchar_allocator>  wchar_string;  // a basic_string (which supports formatting).  This is built on a collection of wide chars, allocated by wchar_alloctor.

	// Event base class
	class EventMessage
	{
	public:
		uint64_t					Signature1_;				// signature 0x1212121212121212
		EnumEventType				EventType_;                 // this event's event type
        EnumEventSubType            EventSubType_;              // this event's event subtype
		uint64_t					ProcessId_;                 // for logging only
		uint64_t					ThreadId_;                  // for logging only
		EnumCloudAppIconBadgeType	BadgeType_;                 // the badge type associated with this event
		wchar_string				FullPath_;                  // the full path associated with this event
        GUID                        GuidPublisher_;             // the identity of the publisher
		uint64_t					Signature2_;				// signature 0x1212121212121212

		// Constructor
		EventMessage(EnumEventType EventType, EnumEventSubType EventSubType, ULONG ProcessId, ULONG ThreadId, 
				EnumCloudAppIconBadgeType BadgeType, BSTR *FullPath, GUID GuidPublisher, const wchar_string::allocator_type &allocator) :
				Signature1_(_kuEventSignature), EventType_(EventType), EventSubType_(EventSubType), ProcessId_(ProcessId), ThreadId_(ThreadId), 
					BadgeType_(BadgeType), FullPath_(*FullPath, allocator), GuidPublisher_(GuidPublisher), Signature2_(_kuEventSignature) {}
	};

	// Event allocators
	typedef boost::interprocess::allocator<EventMessage, segment_manager_t>		EventMessage_allocator;			// allocator for allocating EventMessage
	typedef boost::interprocess::vector<EventMessage, EventMessage_allocator>	EventMessage_vector;			// vector of EventMessage objects.

	// Subscription class
	class Subscription
	{
	public:
        uint64_t                    uSignature1_;               // 0xCACACACACACACACA
		uint64_t                    uSubscribingProcessId_;     // the subscribing process ID (logging only)
		uint64_t                    uSubscribingThreadId_;      // the subscribing thread ID (logging only)
		EnumEventType               nEventType_;                // the event type being subscribed to
		offset_ptr<interprocess_semaphore, int64_t, uint64_t>	pSemaphoreSubscription_;    // allows a subscribing thread to wait for events to arrive.
        GUID                        guidSubscriber_;                    // the unique identifier of the subscriber
		uint64_t                    fDestructed_;               // true: this object has been destructed
        uint64_t                    fWaiting_;                  // true: the subscribing thread is waiting
        uint64_t                    fCancelled_;                // true: this subscription has been cancelled.
		EventMessage_vector		    events_;					// a vector of events,
        uint64_t                    uSignature2_;               // 0xCACACACACACACACA

		// Constructor
		Subscription(GUID guidSubscriber, ULONG uSubscribingProcessId, ULONG uSubscribingThreadId, EnumEventType nEventType, 
									const void_allocator &allocator) :
                            uSignature1_(_kuSubscriptionSignature),
							uSubscribingProcessId_(uSubscribingProcessId), 
							uSubscribingThreadId_(uSubscribingThreadId),
							nEventType_(nEventType),
                            guidSubscriber_(guidSubscriber),
							fDestructed_(false),
							fWaiting_(false),
							fCancelled_(false),
							events_(allocator),
							uSignature2_(_kuSubscriptionSignature) {}

		// Destructor
		~Subscription() {}
	};

 	// Define the types related to Subscription
	typedef boost::interprocess::allocator<Subscription, segment_manager_t>                      subscription_allocator;     // allocator for allocating Subscription
	typedef boost::interprocess::allocator<GUID, segment_manager_t>								guid_allocator;			// allocator for allocating GUID
	typedef std::pair<const GUID, Subscription>													pair_guid_subscription;	// a pair of GUID, Subscription
	typedef boost::interprocess::allocator<EnumEventType, segment_manager_t>					eventtype_allocator;	// allocator for allocating EnumEventType
	typedef boost::interprocess::allocator<pair_guid_subscription, segment_manager_t>			pair_guid_subscription_allocator;  // allocator for pair_guid_subscription
	typedef boost::interprocess::flat_map<GUID, Subscription, GUIDPairsComparer, pair_guid_subscription_allocator>  guid_subscription_map;  // a map of GUID => Subscription
	typedef std::pair<const EnumEventType, guid_subscription_map>								pair_eventtype_pair_guid_subscription;  // a pair(EnumEventType, pair(GUID, Subscription))
	typedef boost::interprocess::allocator<pair_eventtype_pair_guid_subscription, segment_manager_t>  pair_eventtype_pair_guid_subscription_allocator;  // allocator for pair(EnumEventType, pair(GUID, Subscription))
	typedef boost::interprocess::flat_map<EnumEventType, guid_subscription_map, std::less<int>, pair_eventtype_pair_guid_subscription_allocator> eventtype_map_guid_subscription_map;  // a map<EnumEventType, map<GUID, Subscription>>
	typedef boost::interprocess::vector<Subscription, subscription_allocator>                    subscription_vector;    // a vector of Subscriptions
	typedef boost::interprocess::allocator<subscription_vector, segment_manager_t>               subscription_vector_allocator;  // allocator for allocating a vector of Subscription.
	typedef boost::interprocess::allocator<guid_subscription_map, segment_manager_t>			guid_subscription_map_allocator;  // allocator for allocating a map of GUID => Subscription
	typedef boost::interprocess::allocator<eventtype_map_guid_subscription_map, segment_manager_t> eventtype_map_guid_subscription_map_allocator;  // allocator for allocating map<EnumEventType, map<GUID, Subscription>>

	// Base class to hold all of the data in shared memory.
	class Base
	{
	public:
		uint64_t reserved1_, reserved2_;
		// Put any required global data here.
        uint64_t            uSignature1_;                 // 0xACACACACACACACAC
		interprocess_mutex  mutexSharedMemory_;  // lock to protect the whole Base object
		eventtype_map_guid_subscription_map subscriptions_;      // map<EnumEventType, map<GUID, Subscription>> (the active subscriptions by event type)
        uint64_t            uSignature2_;                 // 0xACACACACACACACAC

		Base(const std::less<int> &comparator, const void_allocator &allocator)
				: 
			reserved1_(0),
			reserved2_(0),
			uSignature1_(_kuBaseSignature),
			subscriptions_(comparator, allocator),
			uSignature2_(_kuBaseSignature) {}
	};

	// Definition of the map holding all of the data.  There will just be a single map element with key "base".  The value
	// will be a complex container containing any required global data, plus the map<EventType, map<GUID, Subscription>>.
	typedef boost::interprocess::allocator<Base, segment_manager_t>		base_allocator;       // allocator for allocating Base
	typedef boost::interprocess::vector<Base, base_allocator>			base_vector;          // a vector of Base

	// Public OLE accessible methods
    STDMETHOD(Initialize)();
    STDMETHOD(Subscribe)(
            EnumEventType EventType,
            GUID guidSubscriber,
            ULONG TimeoutMilliseconds,
            EnumEventSubType *outEventSubType,
            EnumCloudAppIconBadgeType *outBadgeType,
            BSTR *outFullPath,
            ULONG *outProcessId,
            GUID *outGuidPublisher,
            EnumPubSubServerSubscribeReturnCodes *returnValue);
    STDMETHOD(Publish)(EnumEventType EventType,
            EnumEventSubType EventSubType, 
            EnumCloudAppIconBadgeType BadgeType, 
            BSTR *FullPath, 
            GUID GuidPublisher,
            EnumPubSubServerPublishReturnCodes *returnValue);
	STDMETHOD(CancelWaitingSubscription)(EnumEventType EventType, GUID guidSubscriber, EnumPubSubServerCancelWaitingSubscriptionReturnCodes *returnValue);
	STDMETHOD(CancelSubscriptionsForProcessId)(ULONG ProcessId, EnumPubSubServerCancelSubscriptionsByProcessIdReturnCodes *returnValue);
	STDMETHOD(CleanUpUnusedResources)(EnumPubSubServerCleanUpUnusedResourcesReturnCodes *returnValue);
    STDMETHOD(Terminate)();

    // Public OLE accessible properties
    STDMETHOD(get_SharedMemoryName)(BSTR* pVal);
public:
    // Public methods
    static BOOL IsProcessRunning(DWORD pid);
private:
    // Private structs
    typedef struct UniqueSubscription_tag
    {
         EnumEventType eventType;
         GUID guid;
    } UniqueSubscription, *P_UniqueSubscription;

	// Private methods
    void RemoveTrackedSubscriptionId(EnumEventType eventType, GUID guid);
	void DeleteSubscriptionById(Base * base, UniqueSubscription subscriptionId);
	BOOL FindSubscription(EnumEventType EventType, GUID guidSubscriber, Base *base, 
				eventtype_map_guid_subscription_map::iterator *outItEventType,
				guid_subscription_map::iterator *outItSubscription,
				offset_ptr<Subscription> *outOptrFoundSubscription);
    void TraceCurrentStateOfSharedMemory(Base *pBase);
    std::string GetSharedMemoryNameWithVersion();
    std::wstring GetSharedMemoryNameWithVersionWide();
	void Debug32BitProcess();  //@@@@@@@@@@@ DEBUG ONLY.  REMOVE.  @@@@@@@@@@@
	BOOL _fIsTerminating;

	// Private instance fields
	std::vector<UniqueSubscription> _trackedSubscriptionIds;		// list of subscriptions created by this instance

	// Private static fields
    static managed_windows_shared_memory *_pSegment;		// Pointer to the managed native windows shared memory segment.
};

// FNV hash
// Modified from: http://programmers.stackexchange.com/questions/49550/which-hashing-algorithm-is-best-for-uniqueness-and-speed
static std::size_t FNV_hash(const void* dataToHash, std::size_t length)
{
  unsigned char* p = (unsigned char *) dataToHash;
  std::size_t h = 2166136261UL;
  std::size_t i;

  for(i = 0; i < length; i++)
    h = (h * 16777619) ^ p[i] ;

  return h;
}

// GUID hash
static std::size_t hash_value(GUID const& guid) 
{
	return FNV_hash(&guid, sizeof(GUID));
}

// GUID Comparator


OBJECT_ENTRY_AUTO(__uuidof(PubSubServer), CPubSubServer)
