//
// CBadgeIconBase.cpp
// Cloud Windows COM
//
// Created By BobS.
// Copyright (c) Cloud.com. All rights reserved.

#include "stdafx.h"
#include "CBadgeIconBase.h"

// Debug trace
#define CLTRACE(intPriority, szFormat, ...) Trace::getInstance()->write(intPriority, szFormat, __VA_ARGS__)

// CBadgeIconBase

/// <Summary>
/// Default Constructor.
/// </Summary>
CBadgeIconBase::CBadgeIconBase()
{
    CLTRACE(1, "CBadgeIconBase: CBadgeIconBase: ERROR: Default constructor not supported.");   
    throw new std::exception("Default contstructor not supported");
}

/// <Summary>
/// Constructor with parameters.
/// </Summary>
CBadgeIconBase::CBadgeIconBase(int iconIndex, EnumCloudAppIconBadgeType badgeType)
{
    try
    {
		//&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&  DEBUG REMOVE &&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&
		//static bool fCompletedOnce = false;
		//while (!fCompletedOnce)
		//{
		//	Sleep(100);
		//}
		//fCompletedOnce = true;
		//&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&  DEBUG REMOVE &&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&

	    // Allocate the PubSubEvents system, subscribe to events, and send an initialization event to BadgeNet.
        _strBaseBadgeType = BadgeTypeToString(badgeType);
		CLTRACE(9, "CBadgeIconBase: CBadgeIconBase: Entry. Start the subscription threads. Badge type: %s.", _strBaseBadgeType);
        _fIsInitialized = false;
        _iconIndex = iconIndex;
        _badgeType = badgeType;
	    InitializeBadgeNetPubSubEventsViaThread();
    }
    catch (const std::exception &ex)
    {
		CLTRACE(1, "CBadgeIconBase: CBadgeIconBase: ERROR: Exception.  Message: %s.", ex.what());
    }
    catch (...)
    {
		CLTRACE(1, "CBadgeIconBase: CBadgeIconBase: ERROR: C++ exception.");
    }
}

/// <Summary>
/// Destructor.
/// </Summary>
CBadgeIconBase::~CBadgeIconBase()
{
    try
    {
		// We lost the badging connection.  Empty the dictionaries.  They will be rebuilt if we can get another connection.
		CLTRACE(9, "CBadgeIconBase: ~CBadgeIconBase: Entry. Shut down this instance.");
		_mapBadges.clear();

		// Restart the CBadgeNetPubSubEvents class.
		if (_pBadgeNetPubSubEvents != NULL)
		{
			// Kill the BadgeNetPubSubEvents threads and free resources.
    		CLTRACE(9, "CBadgeIconBase: ~CBadgeIconBase: Terminate the subscriptions.");
			_pBadgeNetPubSubEvents->~CBadgeNetPubSubEvents();
			_pBadgeNetPubSubEvents = NULL;
        }
    }
    catch (const std::exception &ex)
    {
		CLTRACE(1, "CBadgeIconBase: ~CBadgeIconBase: ERROR: Exception.  Message: %s.", ex.what());
    }
    catch (...)
    {
		CLTRACE(1, "CBadgeIconBase: ~CBadgeIconBase: ERROR: C++ exception.");
    }
}

// IShellIconOverlayIdentifier::GetOverlayInfo
// returns The Overlay Icon Location to the system
// By debugging I found it only runs once when the com object is loaded to the system, thus it cannot return a dynamic icon
HRESULT CBadgeIconBase::GetOverlayInfo(
	LPWSTR pwszIconFile,
	int cchMax,
	int* pIndex,
	DWORD* pdwFlags)
{
	try
	{
		// Get our module's full path
		CLTRACE(9, "CBadgeIconBase: GetOverlayInfo: Entry. Badge type: %s.", _strBaseBadgeType);
		GetModuleFileNameW(_AtlBaseModule.GetModuleInstance(), pwszIconFile, cchMax);

        *pIndex = _iconIndex;                       // this is the index of the icon to use (synced, syncing, ...)

		*pdwFlags = ISIOI_ICONFILE | ISIOI_ICONINDEX;
	}
	catch (const std::exception &ex)
	{
		CLTRACE(1, "CBadgeIconBase: GetOverlayInfo: ERROR: Exception.  Message: %s.", ex.what());
	}
    catch (...)
    {
		CLTRACE(1, "CBadgeIconBase: GetOverlayInfo: ERROR: C++ exception.");
    }

	return S_OK;
}

/// <Summary>
/// Create a new thread so it can establish itself as a Multithreaded Apartment (MTA) thread for PubSub.
/// </Summary>
void CBadgeIconBase::InitializeBadgeNetPubSubEventsViaThread()
{
    try
    {
        // Start a thread to subscribe and process BadgeCom initialization events.  Upon receiving one of these events,
        // we will send the entire badging database for this process.
        DWORD dwThreadId;
        HANDLE handle = CreateThread(NULL,                              // default security
                    0,                                                  // default stack size
                    (LPTHREAD_START_ROUTINE)&InitializeBadgeNetPubSubEventsThreadProc,     // function to run
                    (LPVOID) this,                                      // thread parameter
                    0,                                                  // imediately run the thread
                    &dwThreadId                                         // output thread ID
                    );
        if (handle == NULL)
        {
            throw new std::exception("Error creating thread");
        }
    }
    catch (const std::exception &ex)
    {
		CLTRACE(1, "CBadgeIconBase: InitializeBadgeNetPubSubEventsViaThread: ERROR: Exception.  Message: %s.", ex.what());
    }
    catch (...)
    {
		CLTRACE(1, "CBadgeIconBase: InitializeBadgeNetPubSubEventsViaThread: ERROR: C++ exception.");
    }
}

/// <Summary>
/// Thread procedure to initialize PubSub.
/// </Summary>
void CBadgeIconBase::InitializeBadgeNetPubSubEventsThreadProc(LPVOID pUserState)
{
    try
    {
        // Cast the user state to an object instance
        CBadgeIconBase *pThis = (CBadgeIconBase *)pUserState;

        pThis->InitializeBadgeNetPubSubEvents();
    }
    catch (const std::exception &ex)
    {
		CLTRACE(1, "CBadgeIconBase: InitializeBadgeNetPubSubEventsThreadProc: ERROR: Exception.  Message: %s.", ex.what());
    }
    catch (...)
    {
		CLTRACE(1, "CBadgeIconBase: InitializeBadgeNetPubSubEventsThreadProc: ERROR: C++ exception.");
    }
}

// IShellIconOverlayIdentifier::GetPriority
// returns the priority of this overlay 0 being the highest.
HRESULT CBadgeIconBase::GetPriority(int* pPriority)
{
    try
    {
	    CLTRACE(9, "CBadgeIconBase: GetPriority: Entry. Badge type: %s.", _strBaseBadgeType);
	    // change the following to set priority between multiple overlays
	    *pPriority = 0;
    }
    catch (const std::exception &ex)
    {
		CLTRACE(1, "CBadgeIconBase: GetPriority: ERROR: Exception.  Message: %s.", ex.what());
    }
    catch (...)
    {
		CLTRACE(1, "CBadgeIconBase: GetPriority: ERROR: C++ exception.");
    }

	return S_OK;
}

typedef HRESULT (WINAPI*pfnGetDispName)(LPCWSTR, LPWSTR, DWORD);

// IShellIconOverlayIdentifier::IsMemberOf
// Returns whether the object should have this overlay or not 
HRESULT CBadgeIconBase::IsMemberOf(LPCWSTR pwszPath, DWORD dwAttrib)
{
	// Default return value is false (no icon overlay)
	HRESULT result = S_FALSE;   // or S_OK for icon overlay

    try
    {
        // No badging if not initialized
        if (!_fIsInitialized)
        {
            return result;
        }

	    // Should this path be badged?  It will be badged this exact path is found in the the badging dictionary,
	    // and if the current badgeType value matches this icon handler.
		CLTRACE(9, "CBadgeIconBase: IsMemberOf: Entry. Path: <%ls>. Badge type: %s.", pwszPath, _strBaseBadgeType);
        CComBSTR lowerCaseFullPath(pwszPath);  // this will free its memory when it goes out of scope.  See http://msdn.microsoft.com/en-us/library/bdyd6xz6(v=vs.80).aspx#programmingwithccombstr_memoryleaks
        lowerCaseFullPath.ToLower();
	    boost::unordered_map<std::wstring, DATA_FOR_BADGE_PATH>::iterator it = _mapBadges.find(lowerCaseFullPath.m_str);
	    if (it != _mapBadges.end() && it->second.badgeType == _badgeType)
	    {
    		CLTRACE(9, "CBadgeIconBase: IsMemberOf: Badge it!.");
		    result = S_OK;			// badge this icon
	    }
    }
    catch (const std::exception &ex)
    {
		CLTRACE(1, "CBadgeIconBase: IsMemberOf: ERROR: Exception.  Message: %s.", ex.what());
    }
    catch (...)
    {
		CLTRACE(1, "CBadgeIconBase: IsMemberOf: ERROR: C++ exception.");
    }

	return result;
}

/// <summary>
/// We received a badging event from BadgeNet.  This may be a new path, or it may change the badge type
/// for an existing path.
/// </summary>
/// <param name="fullPath">The full path of the item being added.</param>
/// <param name="badgeType">The type of the badge.</param>
/// <param name="processIdPublisher">The process ID that sent the event.</param>
/// <param name="guidPublisher">The ID of the SyncBox that sent the event.</param>
void CBadgeIconBase::OnEventAddBadgePath(BSTR fullPath, EnumCloudAppIconBadgeType badgeType, ULONG processIdPublisher, GUID guidPublisher)
{
	try
	{
		// Add or update the <path,badgeType>
		if (badgeType == _badgeType)
		{
			CLTRACE(9, "CBadgeIconBase: OnEventAddBadgePath: Entry. BadgeType: %d. Path: <%ls>. processIdPublisher: %d.  guidPublisher: %ls. Base badge type: %s.", badgeType, fullPath, processIdPublisher, CComBSTR(guidPublisher), _strBaseBadgeType);
            CComBSTR lowerCaseFullPath(fullPath);  // this will free its memory when it goes out of scope.  See http://msdn.microsoft.com/en-us/library/bdyd6xz6(v=vs.80).aspx#programmingwithccombstr_memoryleaks
            lowerCaseFullPath.ToLower();

            this->_mutexBadgeDatabase.lock();
            {
                // Find the value in _mapBadges by key: lowerCaseFullPath
                boost::unordered_map<std::wstring, DATA_FOR_BADGE_PATH>::iterator it = _mapBadges.find(lowerCaseFullPath.m_str);
    	        if (it == _mapBadges.end() || it->second.badgeType != _badgeType)
	            {
        		    CLTRACE(9, "CBadgeIconBase: OnEventAddBadgePath: Did not find the path.  Add it.");
                    boost::unordered_set<GUID> setOfGuids;
                    setOfGuids.insert(guidPublisher);
                    boost::unordered_map<ULONG, boost::unordered_set<GUID>> mapProcessIdToSetOfGuids;
                    mapProcessIdToSetOfGuids[processIdPublisher] = setOfGuids;
                    boost::unordered_map<EnumCloudAppIconBadgeType, boost::unordered_map<ULONG, boost::unordered_set<GUID>>> mapBadgeTypeToMapOfProcessIdsToSetOfGuids;
                    mapBadgeTypeToMapOfProcessIdsToSetOfGuids[badgeType] = mapProcessIdToSetOfGuids;
        			_mapBadges[lowerCaseFullPath.m_str] = mapBadgeTypeToMapOfProcessIdsToSetOfGuids;

                    &&&&&&&&&&&
                    create new pair <fullPath, unordered_map<EnumCloudAppIconBadgeType, unordered_map<ULONG, unordered_set<GUID>>>
                    add pair to _mapBadges
	            }
                else
                {
                    get value to itPathValue.
                    find value in itPathValue by key: badgeType
                    if not found
                      create new pair <badgeTypeEnumCloudAppIconBadgeType, unordered_map<ULONG, unordered_set<GUID>
                      add pair to itPathValue
                    else found
                      get value to itBadgeTypeValue
                      find in itBadgeTypeValue by key: processIdPublisher
                      if not found
                        create new pair <ULONG, unordered_set<GUID>>
                        add pair to itBadgeTypeValue
                      else found
                        get value to itProcessIdPublisherValue
                        find in itProcessIdPublisherValue by key: guidIdPublisher
                        if not found
                          add key guidIdPublisher to itProcessIdPublisherValue
                        else found
                          ; this badge is already stored.  Do nothing.
                        endelse found
                      endelse found
                    endelse found
                }
            }
            this->_mutexBadgeDatabase.unlock();


            &&&&&&&&&&&&&&&&&&&&
			_mapBadges[lowerCaseFullPath.m_str].badgeType = badgeType;
			SHChangeNotify(SHCNE_ATTRIBUTES, SHCNF_PATH, COLE2T(lowerCaseFullPath.m_str), NULL);
		}
	}
	catch (const std::exception &ex)
	{
		CLTRACE(1, "CBadgeIconBase: OnEventAddBadgePath: ERROR: Exception.  Message: %s.", ex.what());
	}
    catch (...)
    {
		CLTRACE(1, "CBadgeIconBase: OnEventAddBadgePath: ERROR: C++ exception.");
    }
}

/// <summary>
/// We received a request to remove a badging path from BadgeNet.  There will be no error if it doesn't exist.
/// </summary>
/// <param name="fullPath">The full path of the item being removed.</param>
/// <param name="processIdPublisher">The process ID that sent the event.</param>
/// <param name="guidPublisher">The ID of the SyncBox that sent the event.</param>
void CBadgeIconBase::OnEventRemoveBadgePath(BSTR fullPath, ULONG processIdPublisher, GUID guidPublisher)
{
	try
	{
		// Remove the item with key fullPath.
		CLTRACE(9, "CBadgeIconBase: OnEventRemoveBadgePath: Entry. Path: <%ls>. Base badge type: %s.", fullPath, _strBaseBadgeType);
        CComBSTR lowerCaseFullPath(fullPath);  // this will free its memory when it goes out of scope.  See http://msdn.microsoft.com/en-us/library/bdyd6xz6(v=vs.80).aspx#programmingwithccombstr_memoryleaks
        lowerCaseFullPath.ToLower();
		_mapBadges.erase(lowerCaseFullPath.m_str);
        SHChangeNotify(SHCNE_ATTRIBUTES, SHCNF_PATH, COLE2T(lowerCaseFullPath.m_str), NULL);
	}
	catch (const std::exception &ex)
	{
		CLTRACE(1, "CBadgeIconBase: OnEventRemoveBadgePath: ERROR: Exception.  Message: %s.", ex.what());
	}
    catch (...)
    {
		CLTRACE(1, "CBadgeIconBase: OnEventRemoveBadgePath: ERROR: C++ exception.");
    }
}

/// <summary>
/// We received a request from BadgeNet to start tracking a new SyncBox folder path.
/// There will be no error if we are already tracking that path.
/// </summary>
/// <param name="fullPath">The full path of the folder being added.</param>
/// <param name="processIdPublisher">The process ID that sent the event.</param>
/// <param name="guidPublisher">The ID of the SyncBox that sent the event.</param>
void CBadgeIconBase::OnEventAddSyncBoxFolderPath(BSTR fullPath, ULONG processIdPublisher, GUID guidPublisher)
{
	try
	{
		// Add or update the fullPath.  The value is not used.
		CLTRACE(9, "CBadgeIconBase: OnEventAddSyncBoxFolderPath: Entry. Path: <%ls>. Base badge type: %s.", fullPath, _strBaseBadgeType);
        CComBSTR lowerCaseFullPath(fullPath);  // this will free its memory when it goes out of scope.  See http://msdn.microsoft.com/en-us/library/bdyd6xz6(v=vs.80).aspx#programmingwithccombstr_memoryleaks
        lowerCaseFullPath.ToLower();
		_mapBadges[lowerCaseFullPath.m_str].badgeType = cloudAppBadgeNone;
        SHChangeNotify(SHCNE_ATTRIBUTES, SHCNF_PATH, COLE2T(lowerCaseFullPath.m_str), NULL);
	}
	catch (const std::exception &ex)
	{
		CLTRACE(1, "CBadgeIconBase: OnEventAddSyncBoxFolderPath: ERROR: Exception.  Message: %s.", ex.what());
	}
    catch (...)
    {
		CLTRACE(1, "CBadgeIconBase: OnEventAddSyncBoxFolderPath: ERROR: C++ exception.");
    }
}

/// <summary>
/// We received a request from BadgeNet to stop tracking a SyncBox folder path.
/// There will be no error if we are already not tracking that path.
/// </summary>
/// <param name="fullPath">The full path of the folder being removed.</param>
/// <param name="processIdPublisher">The process ID that sent the event.</param>
/// <param name="guidPublisher">The ID of the SyncBox that sent the event.</param>
void CBadgeIconBase::OnEventRemoveSyncBoxFolderPath(BSTR fullPath, ULONG processIdPublisher, GUID guidPublisher)
{
	try
	{
		// Remove the item with key fullPath.
		CLTRACE(9, "CBadgeIconBase: OnEventRemoveSyncBoxFolderPath: Entry. Path: <%ls>. Base badge type: %s.", fullPath, _strBaseBadgeType);
        CComBSTR lowerCaseFullPath(fullPath);  // this will free its memory when it goes out of scope.  See http://msdn.microsoft.com/en-us/library/bdyd6xz6(v=vs.80).aspx#programmingwithccombstr_memoryleaks
        lowerCaseFullPath.ToLower();
		_mapBadges.erase(lowerCaseFullPath.m_str);
        SHChangeNotify(SHCNE_ATTRIBUTES, SHCNF_PATH, COLE2T(lowerCaseFullPath.m_str), NULL);

		// Delete all of the keys in the badging dictionary that have this folder path as a root.
		for (boost::unordered_map<std::wstring, DATA_FOR_BADGE_PATH>::iterator it = _mapBadges.begin(); it != _mapBadges.end();  /* bump in body of code */)
		{
			if (IsPathInRootPath(it->first, lowerCaseFullPath.m_str))
			{
        		CLTRACE(9, "CBadgeIconBase: OnEventRemoveSyncBoxFolderPath: Erase path from badging dictionary: %ls.", it->first.c_str());
                SHChangeNotify(SHCNE_ATTRIBUTES, SHCNF_PATH, it->first.c_str(), NULL);
				it = _mapBadges.erase(it);
			}
			else
			{
				++it;
			}
		}
	}
	catch (const std::exception &ex)
	{
		CLTRACE(1, "CBadgeIconBase: OnEventRemoveSyncBoxFolderPath: ERROR: Exception.  Message: %s.", ex.what());
	}
    catch (...)
    {
		CLTRACE(1, "CBadgeIconBase: OnEventRemoveSyncBoxFolderPath: ERROR: C++ exception.");
    }
}

/// <summary>
/// We received an error from the PubSubServer watcher.  We are no longer subscribed to badging events.
/// </summary>
void CBadgeIconBase::OnEventSubscriptionWatcherFailed()
{
	try
	{
		// Restart the CBadgeNetPubSubEvents class, but not here because this event was fired by that
		// class.  Start another thread to do it.
		CLTRACE(9, "CBadgeIconBase: OnEventSubscriptionWatcherFailed: Entry.  ERROR: Badging failed. Base badge type: %s.", _strBaseBadgeType);
        DWORD dwThreadId;
        HANDLE handle = CreateThread(NULL,                              // default security
                    0,                                                  // default stack size
                    (LPTHREAD_START_ROUTINE)&SubscriptionRestartThreadProc,     // function to run
                    (LPVOID) this,                                      // thread parameter
                    0,                                                  // imediately run the thread
                    &dwThreadId                                         // output thread ID
                    );
        if (handle == NULL)
        {
            throw new std::exception("Error creating thread");
        }

        _threadSubscriptionRestart = handle;
	}
	catch (const std::exception &ex)
	{
		CLTRACE(1, "CBadgeIconBase: OnEventSubscriptionWatcherFailed: ERROR: Exception.  Message: %s.", ex.what());
	}
    catch (...)
    {
		CLTRACE(1, "CBadgeIconBase: OnEventSubscriptionWatcherFailed: ERROR: C++ exception.");
    }
}


/// <summary>
/// The BadgeNetPubSubEvents watcher failed.  We are no longer subscribed to badging events.  Restart
/// the subscription service.
/// </summary>
void CBadgeIconBase::SubscriptionRestartThreadProc(LPVOID pUserState)
{
    // Cast the user state to an object instance
    CBadgeIconBase *pThis = (CBadgeIconBase *)pUserState;

	try
	{
		// We lost the badging connection.  Empty the dictionaries.  They will be rebuilt if we can get another connection.
    	CLTRACE(9, "CBadgeIconBase: SubscriptionRestartThreadProc: Entry.  Base badge type: %s.", pThis->_strBaseBadgeType);
		pThis->_mapBadges.clear();

		// Restart the CBadgeNetPubSubEvents class.
		if (pThis->_pBadgeNetPubSubEvents != NULL)
		{
			// Kill the BadgeNetPubSubEvents threads and free resources.
    		CLTRACE(9, "CBadgeIconBase: SubscriptionRestartThreadProc: Restart the subscription.");
			pThis->_pBadgeNetPubSubEvents->~CBadgeNetPubSubEvents();
			pThis->_pBadgeNetPubSubEvents = NULL;

			// Restart the BadgeNetPubSubEvents object
			pThis->InitializeBadgeNetPubSubEvents();
		}
	}
	catch (const std::exception &ex)
	{
		CLTRACE(1, "CBadgeIconBase: SubscriptionRestartThreadProc: ERROR: Exception.  Message: %s.", ex.what());
	}
    catch (...)
    {
		CLTRACE(1, "CBadgeIconBase: SubscriptionRestartThreadProc: ERROR: C++ exception.");
    }

    if (pThis != NULL)
    {
        CloseHandle(pThis->_threadSubscriptionRestart);
        pThis->_threadSubscriptionRestart = NULL;
    }
	CLTRACE(9, "CBadgeIconBase: SubscriptionRestartThreadProc: Exit.");
}

/// <summary>
/// Determines whether a path contains a root path.
/// </summary>
bool CBadgeIconBase::IsPathInRootPath(std::wstring testPath, std::wstring rootPath)
{
	if (testPath.compare(0, rootPath.size(), rootPath) == 0)
	{
		return true;
	}
	else
	{
		return false;
	}
}

/// <summary>
/// Instantiate and initialize the PubSub event subscriber and watcher threads.
/// </summary>
void CBadgeIconBase::InitializeBadgeNetPubSubEvents()
{
	try
	{
		CLTRACE(9, "CBadgeIconBase: InitializeBadgeNetPubSubEvents: Entry. Base badge type: %s.", _strBaseBadgeType);
		_pBadgeNetPubSubEvents = new CBadgeNetPubSubEvents();
		_pBadgeNetPubSubEvents->Initialize();

		// Hook up events.  The "_1" and "_2" are placeholders required by bind (placeholders for the parameters).
		_pBadgeNetPubSubEvents->FireEventAddBadgePath.connect(boost::bind(&CBadgeIconBase::OnEventAddBadgePath, this, _1, _2, _3, _4));
		_pBadgeNetPubSubEvents->FireEventRemoveBadgePath.connect(boost::bind(&CBadgeIconBase::OnEventRemoveBadgePath, this, _1, _2, _3));
		_pBadgeNetPubSubEvents->FireEventAddSyncBoxFolderPath.connect(boost::bind(&CBadgeIconBase::OnEventAddSyncBoxFolderPath, this, _1, _2, _3));
		_pBadgeNetPubSubEvents->FireEventRemoveSyncBoxFolderPath.connect(boost::bind(&CBadgeIconBase::OnEventRemoveSyncBoxFolderPath, this, _1, _2, _3));
		_pBadgeNetPubSubEvents->FireEventSubscriptionWatcherFailed.connect(boost::bind(&CBadgeIconBase::OnEventSubscriptionWatcherFailed, this));

        // Generate a GUID to represent this publisher
        HRESULT hr = CoCreateGuid(&_guidPublisher);
        if (!SUCCEEDED(hr))
        {
    		CLTRACE(1, "CBadgeIconBase: InitializeBadgeNetPubSubEvents: ERROR: Creating GUID. hr: %d.", hr);
            throw new std::exception("Error creating GUID");
        }
  		CLTRACE(9, "CBadgeIconBase: InitializeBadgeNetPubSubEvents: Publisher's GUID: %ls.", _guidPublisher);

		// Subscribe to the events from BadgeNet
		_fIsInitialized = _pBadgeNetPubSubEvents->SubscribeToBadgeNetEvents();

		// Tell BadgeNet we just initialized.
        if (_fIsInitialized)
        {
    		BSTR dummy(L"");
    		CLTRACE(9, "CBadgeIconBase: InitializeBadgeNetPubSubEvents: Call PublishEventToBadgeNet.");
    		_pBadgeNetPubSubEvents->PublishEventToBadgeNet(BadgeCom_To_BadgeNet, BadgeCom_Initialization, cloudAppBadgeNone /* not used */, &dummy /* not used */, _guidPublisher);
        }
	}
	catch (const std::exception &ex)
	{
		CLTRACE(1, "CBadgeIconBase: InitializeBadgeNetPubSubEvents: ERROR: Exception.  Message: %s.", ex.what());
        _fIsInitialized = false;
	}
    catch (...)
    {
		CLTRACE(1, "CBadgeIconBase: InitializeBadgeNetPubSubEvents: ERROR: C++ exception.");
        _fIsInitialized = false;
    }
    CLTRACE(9, "CBadgeIconBase: InitializeBadgeNetPubSubEvents: Exit.");
}


/// <summary>
/// Transform a badge type into a printable string.
/// </summary>
std::string CBadgeIconBase::BadgeTypeToString(EnumCloudAppIconBadgeType badgeType)
{
    switch (badgeType)
    {
        case cloudAppBadgeNone:
            return "NoBadgeType";
            break;
        case cloudAppBadgeSynced:
            return "Synced";
            break;
        case cloudAppBadgeSyncing:
            return "Syncing";
            break;
        case cloudAppBadgeFailed:
            return "Failed";
            break;
        case cloudAppBadgeSyncSelective:
            return "Selective";
            break;
        default:
            return "UnknownBadgeType";
            break;
    }
}



//&&&&&&&&&&&&&
// CBadgeIconSupport

/// <summary>
/// Add a badge to the badging dictionary under a lock.  This function maintains the badge type of the badge, and the list of processes that have added this badge.
/// Note: Multiple processes may have added a badge
/// </summary>
/// <param name="pLocker *">A pointer to the lock to synchronize on.</param>
/// <param name="pBadgeDictionaryguidSubscriber">A pointer to the badge dictionary.param>
/// <param name="pathToAdd">The full path representing the file or folder to badge.</param>
/// <param name="badgeType">The type of the badge.</param>
/// <param name="processId">The process ID of the process that added the badge.</param>
//void AddBadgeToDictionary(boost::mutex *pLocker, boost::unordered_map<std::wstring, DATAFORBADGEPATH> *pBadgeDictionary, std::wstring pathToAdd, EnumCloudAppIconBadgeType badgeType, ULONG processId)
//{

//}

/// <summary>
/// Remove a badge from the badging dictionary under a lock.
/// </summary>
/// <param name="pLocker *">A pointer to the lock to synchronize on.</param>
/// <param name="pBadgeDictionaryguidSubscriber">A pointer to the badge dictionary.param>
/// <param name="pathToAdd">The full path representing the file or folder to badge.</param>
/// <param name="badgeType">The type of the badge.</param>
/// <param name="processId">The process ID of the process that added the badge.</param>
//void RemoveBadgeFromDictionary(boost::mutex *pLocker, boost::unordered_map<std::wstring, DATAFORBADGEPATH> *pBadgeDictionary, std::wstring pathToAdd, EnumCloudAppIconBadgeType badgeType, ULONG processId)
//{

//}


 //       o RemoveBadgeFromDictionary(&mutex, &boost::unordered_map<std::wstring fullPath, DataForBadgePath>, std::wstring pathToRemove, EnumCloudAppIconBadgeType badgeType, ULONG processID)
        //o ShouldPathBeBadged(&mutex, &boost::unordered_map<std::wstring fullPath, DataForBadgePath>, std::wstring pathToCheck, EnumCloudAppIconBadgeType badgeType)
        //o CheckAndRemoveDeadProcesses(&mutex, &boost::unordered_set<ULONG processId>)
