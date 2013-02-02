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
        _mapRootFolders.clear();

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
			CLTRACE(9, "CBadgeIconBase: OnEventAddBadgePath: Entry. BadgeType: %d. Path: <%ls>. processIdPublisher: %x.  guidPublisher: %ls. Base badge type: %s.", badgeType, fullPath, processIdPublisher, CComBSTR(guidPublisher), _strBaseBadgeType);
            CComBSTR lowerCaseFullPath(fullPath);  // this will free its memory when it goes out of scope.  See http://msdn.microsoft.com/en-us/library/bdyd6xz6(v=vs.80).aspx#programmingwithccombstr_memoryleaks
            lowerCaseFullPath.ToLower();

            this->_mutexBadgeDatabase.lock();
            {
                // Add the publisher process to the list of active processes
                _setActiveProcessIds.insert(processIdPublisher);

                // Find the value in _mapBadges by key: lowerCaseFullPath
                boost::unordered_map<std::wstring, DATA_FOR_BADGE_PATH>::iterator itPathValue = _mapBadges.find(lowerCaseFullPath.m_str);
    	        if (itPathValue == _mapBadges.end())
	            {
        		    CLTRACE(9, "CBadgeIconBase: OnEventAddBadgePath: Did not find the path.  Add it.");
                    // We did not find this item.  Create a new pair <fullPath, DATA_FOR_BADGE_PATH> and add the pair to the badge dictionary.
                    // Create a set of GUIDs with just this one GUID in it.
                    boost::unordered_set<GUID> setOfGuids;
                    setOfGuids.insert(guidPublisher);

                    // Create a dictionary with just one pair <processIdPublisher, setOfGuids>.
                    boost::unordered_map<ULONG, boost::unordered_set<GUID>> mapProcessIdToSetOfGuids;
                    mapProcessIdToSetOfGuids[processIdPublisher] = setOfGuids;

                    // Create a DATA_FOR_BADGE_PATH struct with the badgeType and the dictionary built above.
                    DATA_FOR_BADGE_PATH badgeTypeToDictionary;
                    badgeTypeToDictionary.badgeType = badgeType;
                    badgeTypeToDictionary.processesThatAddedThisBadge = mapProcessIdToSetOfGuids;

                    // Add a new pair to the badge dictionary <fullPath, mapProcessIdToSetOfGuids>
        			_mapBadges[lowerCaseFullPath.m_str] = badgeTypeToDictionary;
	            }
                else
                {
        		    CLTRACE(9, "CBadgeIconBase: OnEventAddBadgePath: Found the path.");
                    // We found this key. The value is a struct DATA_FOR_BADGE_PATH.  That has badgeType and processesThatAddedThisBadge(unordered_map<ULONG, unordered_set<GUID>>).
                    // Check the badgeType we found.
                    if (itPathValue->second.badgeType != _badgeType)
                    {
            		    CLTRACE(9, "CBadgeIconBase: OnEventAddBadgePath: ERROR: Invalid badge type found: %d.", itPathValue->second.badgeType);
                        throw new std::exception("Invalid badgeType: " + itPathValue->second.badgeType);
                    }

                    // Find this processId in the dictionary.
                    boost::unordered_map<ULONG, boost::unordered_set<GUID>>::iterator itProcessItValue = itPathValue->second.processesThatAddedThisBadge.find(processIdPublisher);
        	        if (itProcessItValue == itPathValue->second.processesThatAddedThisBadge.end())
	                {
                        // We didn't find this processId.  Start building a pair to represent it.
                        // Create a set of GUIDs with just this one GUID in it.
            		    CLTRACE(9, "CBadgeIconBase: OnEventAddBadgePath: We didn't find this processId.");
                        boost::unordered_set<GUID> setOfGuids;
                        setOfGuids.insert(guidPublisher);

                        // Create a dictionary with just one pair <processIdPublisher, setOfGuids>.
                        boost::unordered_map<ULONG, boost::unordered_set<GUID>> mapProcessIdToSetOfGuids;
                        mapProcessIdToSetOfGuids[processIdPublisher] = setOfGuids;

                        // Add this pair to the dictionary.
                        itPathValue->second.processesThatAddedThisBadge[processIdPublisher] = setOfGuids;
                    }
                    else
                    {
                        // We found this process ID.  See if this GUID is already in this processId's set.
            		    CLTRACE(9, "CBadgeIconBase: OnEventAddBadgePath: We found this processId.");
                        boost::unordered_set<GUID>::iterator itProcessIdPublisherValue = itProcessItValue->second.find(guidPublisher);
            	        if (itProcessIdPublisherValue == itProcessItValue->second.end())
                        {
                            // We didn't find this guidPublisher.  Add it.
                            itProcessItValue->second.insert(guidPublisher);
                        }
                        else
                        {
                            // This badge has already been stored by this process and SyncBox.
                            // @@@@@@@@@@ DO NOTHING @@@@@@
                        }
                    }
                }
            }
            this->_mutexBadgeDatabase.unlock();

            // Notify Explorer of the changes we made
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
/// <returns>bool: true: This removal removed the entire path from the badging dictionary.</returns>
bool CBadgeIconBase::OnEventRemoveBadgePath(BSTR fullPath, ULONG processIdPublisher, GUID guidPublisher)
{
    bool fToReturnRemovedEntirePath = false;
	try
	{
		// Remove the item with key fullPath.
		CLTRACE(9, "CBadgeIconBase: OnEventRemoveBadgePath: Entry. Path: <%ls>. processIdPublisher: %x. guidPublisher: %ls. Base badge type: %s.", fullPath, processIdPublisher, CComBSTR(guidPublisher), _strBaseBadgeType);
        CComBSTR lowerCaseFullPath(fullPath);  // this will free its memory when it goes out of scope.  See http://msdn.microsoft.com/en-us/library/bdyd6xz6(v=vs.80).aspx#programmingwithccombstr_memoryleaks
        lowerCaseFullPath.ToLower();

        this->_mutexBadgeDatabase.lock();
        {
            // Find the value in _mapBadges by key: lowerCaseFullPath
            boost::unordered_map<std::wstring, DATA_FOR_BADGE_PATH>::iterator itPathValue = _mapBadges.find(lowerCaseFullPath.m_str);
    	    if (itPathValue == _mapBadges.end())
	        {
       		    CLTRACE(9, "CBadgeIconBase: OnEventRemoveBadgePath: Did not find the path.  Nothing to do.");
                // @@@@@@@ DO NOTHING @@@@@@
            }
            else
            {
                // We found the fullPath.  Get the value (DATA_FOR_BADGE_PATH) for this path.
                // Check the badgeType.
                if (itPathValue->second.badgeType != _badgeType)
                {
            		CLTRACE(9, "CBadgeIconBase: OnEventRemoveBadgePath: ERROR: Invalid badge type found: %d.", itPathValue->second.badgeType);
                    throw new std::exception("Invalid badgeType: " + itPathValue->second.badgeType);
                }

                // Find this processId in the dictionary for this path.
                boost::unordered_map<ULONG, boost::unordered_set<GUID>>::iterator itProcessItValue = itPathValue->second.processesThatAddedThisBadge.find(processIdPublisher);
        	    if (itProcessItValue == itPathValue->second.processesThatAddedThisBadge.end())
	            {
       		        CLTRACE(9, "CBadgeIconBase: OnEventRemoveBadgePath: Did not find the process ID.  Nothing to do.");
                    // @@@@@@@ DO NOTHING @@@@@@
                }
                else
                {
                    // Locate the guidPublisher in the set associated with this processId.
                    boost::unordered_set<GUID>::iterator itProcessIdPublisherValue = itProcessItValue->second.find(guidPublisher);
            	    if (itProcessIdPublisherValue == itProcessItValue->second.end())
                    {
                        // We didn't find this guidPublisher.  Do nothing.
       		            CLTRACE(9, "CBadgeIconBase: OnEventRemoveBadgePath: Did not find the guidPublisher.  Nothing to do.");
                        // @@@@@@@ DO NOTHING @@@@@@
                    }
                    else
                    {
                        // We found the path, processIdPublisher and guidPublisher.  Remove the guidPublisher from the set.
       		            CLTRACE(9, "CBadgeIconBase: OnEventRemoveBadgePath: Remove guidPublisher: %ls.", CComBSTR(guidPublisher));
                        itProcessItValue->second.erase(guidPublisher);

                        // If all keys in the guidPublisher set are now gone, remove the processIdPublisher.
                        if (itProcessItValue->second.size() < 1)
                        {
                            // remove this processIdPublisher from the dictionary.
       		                CLTRACE(9, "CBadgeIconBase: OnEventRemoveBadgePath: Remove processIdPublisher: %x.", processIdPublisher);
                            itPathValue->second.processesThatAddedThisBadge.erase(processIdPublisher);

                            // If all keys in the processIdPublisher dictionary are now gone, remove the badge path itself.
                            if (itPathValue->second.processesThatAddedThisBadge.size() < 1)
                            {
                                // Remove this path from the badge dictionary
           		                CLTRACE(9, "CBadgeIconBase: OnEventRemoveBadgePath: Remove path <%ls> from the badging dictionary.", lowerCaseFullPath);
                                _mapBadges.erase(lowerCaseFullPath.m_str);
                                fToReturnRemovedEntirePath = true;
                            }
                        }
                    }
                }
            }
        }
        this->_mutexBadgeDatabase.unlock();

        // Notify Explorer of the changes we made
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

    return fToReturnRemovedEntirePath;
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
		CLTRACE(9, "CBadgeIconBase: OnEventAddSyncBoxFolderPath: Entry. Path: <%ls>. processIdPublisher: %d.  guidPublisher: %ls. Base badge type: %s.", fullPath, processIdPublisher, CComBSTR(guidPublisher), _strBaseBadgeType);
        CComBSTR lowerCaseFullPath(fullPath);  // this will free its memory when it goes out of scope.  See http://msdn.microsoft.com/en-us/library/bdyd6xz6(v=vs.80).aspx#programmingwithccombstr_memoryleaks
        lowerCaseFullPath.ToLower();

        this->_mutexBadgeDatabase.lock();
        {
            // Add the publisher process to the list of active processes
            _setActiveProcessIds.insert(processIdPublisher);

            // Find the value in _mapRootFolders by key: lowerCaseFullPath
            boost::unordered_map<std::wstring, DATA_FOR_BADGE_PATH>::iterator itPathValue = _mapRootFolders.find(lowerCaseFullPath.m_str);
    	    if (itPathValue == _mapRootFolders.end())
	        {
        		CLTRACE(9, "CBadgeIconBase: OnEventAddSyncBoxFolderPath: Did not find the path.  Add it.");
                // We did not find this item.  Create a new pair <fullPath, DATA_FOR_BADGE_PATH> and add the pair to the badge dictionary.
                // Create a set of GUIDs with just this one GUID in it.
                boost::unordered_set<GUID> setOfGuids;
                setOfGuids.insert(guidPublisher);

                // Create a dictionary with just one pair <processIdPublisher, setOfGuids>.
                boost::unordered_map<ULONG, boost::unordered_set<GUID>> mapProcessIdToSetOfGuids;
                mapProcessIdToSetOfGuids[processIdPublisher] = setOfGuids;

                // Create a DATA_FOR_BADGE_PATH struct with the badgeType and the dictionary built above.
                DATA_FOR_BADGE_PATH badgeTypeToDictionary;
                badgeTypeToDictionary.badgeType = cloudAppBadgeNone;
                badgeTypeToDictionary.processesThatAddedThisBadge = mapProcessIdToSetOfGuids;

                // Add a new pair to the badge dictionary <fullPath, mapProcessIdToSetOfGuids>
        		_mapRootFolders[lowerCaseFullPath.m_str] = badgeTypeToDictionary;
	        }
            else
            {
        		CLTRACE(9, "CBadgeIconBase: OnEventAddSyncBoxFolderPath: Found the path.");
                // We found this key. The value is a struct DATA_FOR_BADGE_PATH.  That has badgeType and processesThatAddedThisBadge(unordered_map<ULONG, unordered_set<GUID>>).
                // Check the badgeType we found.
                if (itPathValue->second.badgeType != cloudAppBadgeNone)
                {
            		CLTRACE(9, "CBadgeIconBase: OnEventAddSyncBoxFolderPath: ERROR: Invalid badge type found: %d.", itPathValue->second.badgeType);
                    throw new std::exception("Invalid badgeType: " + itPathValue->second.badgeType);
                }

                // Find this processId in the dictionary.
                boost::unordered_map<ULONG, boost::unordered_set<GUID>>::iterator itProcessItValue = itPathValue->second.processesThatAddedThisBadge.find(processIdPublisher);
        	    if (itProcessItValue == itPathValue->second.processesThatAddedThisBadge.end())
	            {
                    // We didn't find this processId.  Start building a pair to represent it.
                    // Create a set of GUIDs with just this one GUID in it.
            		CLTRACE(9, "CBadgeIconBase: OnEventAddSyncBoxFolderPath: We didn't find this processId.");
                    boost::unordered_set<GUID> setOfGuids;
                    setOfGuids.insert(guidPublisher);

                    // Create a dictionary with just one pair <processIdPublisher, setOfGuids>.
                    boost::unordered_map<ULONG, boost::unordered_set<GUID>> mapProcessIdToSetOfGuids;
                    mapProcessIdToSetOfGuids[processIdPublisher] = setOfGuids;

                    // Add this pair to the dictionary.
                    itPathValue->second.processesThatAddedThisBadge[processIdPublisher] = setOfGuids;
                }
                else
                {
                    // We found this process ID.  See if this GUID is already in this processId's set.
            		CLTRACE(9, "CBadgeIconBase: OnEventAddSyncBoxFolderPath: We found this processId.");
                    boost::unordered_set<GUID>::iterator itProcessIdPublisherValue = itProcessItValue->second.find(guidPublisher);
            	    if (itProcessIdPublisherValue == itProcessItValue->second.end())
                    {
                        // We didn't find this guidPublisher.  Add it.
                        itProcessItValue->second.insert(guidPublisher);
                    }
                    else
                    {
                        // This badge has already been stored by this process and SyncBox.
                        // @@@@@@@@@@ DO NOTHING @@@@@@
                    }
                }
            }
        }
        this->_mutexBadgeDatabase.unlock();

        // Notify Explorer of the changes we made
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
		CLTRACE(9, "CBadgeIconBase: OnEventRemoveSyncBoxFolderPath: Entry. Path: <%ls>. processIdPublisher: %x.  guidPublisher: %ls. Base badge type: %s.", fullPath, processIdPublisher, CComBSTR(guidPublisher), _strBaseBadgeType);
        CComBSTR lowerCaseFullPath(fullPath);  // this will free its memory when it goes out of scope.  See http://msdn.microsoft.com/en-us/library/bdyd6xz6(v=vs.80).aspx#programmingwithccombstr_memoryleaks
        lowerCaseFullPath.ToLower();

        this->_mutexBadgeDatabase.lock();
        {
            // Find the value in _mapRootFolders by key: lowerCaseFullPath
            boost::unordered_map<std::wstring, DATA_FOR_BADGE_PATH>::iterator itPathValue = _mapRootFolders.find(lowerCaseFullPath.m_str);
    	    if (itPathValue == _mapRootFolders.end())
	        {
       		    CLTRACE(9, "CBadgeIconBase: OnEventRemoveSyncBoxFolderPath: Did not find the path.  Nothing to do.");
                // @@@@@@@ DO NOTHING @@@@@@
            }
            else
            {
                // We found the fullPath.  Get the value (DATA_FOR_BADGE_PATH) for this path.
                // Check the badgeType.
                if (itPathValue->second.badgeType != cloudAppBadgeNone)
                {
            		CLTRACE(9, "CBadgeIconBase: OnEventRemoveSyncBoxFolderPath: ERROR: Invalid badge type found: %d.", itPathValue->second.badgeType);
                    throw new std::exception("Invalid badgeType: " + itPathValue->second.badgeType);
                }

                // Find this processId in the dictionary for this path.
                boost::unordered_map<ULONG, boost::unordered_set<GUID>>::iterator itProcessItValue = itPathValue->second.processesThatAddedThisBadge.find(processIdPublisher);
        	    if (itProcessItValue == itPathValue->second.processesThatAddedThisBadge.end())
	            {
       		        CLTRACE(9, "CBadgeIconBase: OnEventRemoveSyncBoxFolderPath: Did not find the process ID.  Nothing to do.");
                    // @@@@@@@ DO NOTHING @@@@@@
                }
                else
                {
                    // Locate the guidPublisher in the set associated with this processId.
                    boost::unordered_set<GUID>::iterator itProcessIdPublisherValue = itProcessItValue->second.find(guidPublisher);
            	    if (itProcessIdPublisherValue == itProcessItValue->second.end())
                    {
                        // We didn't find this guidPublisher.  Do nothing.
       		            CLTRACE(9, "CBadgeIconBase: OnEventRemoveSyncBoxFolderPath: Did not find the guidPublisher.  Nothing to do.");
                        // @@@@@@@ DO NOTHING @@@@@@
                    }
                    else
                    {
                        // We found the path, processIdPublisher and guidPublisher.  Remove the guidPublisher from the set.
       		            CLTRACE(9, "CBadgeIconBase: OnEventRemoveSyncBoxFolderPath: Remove guidPublisher: %ls.", CComBSTR(guidPublisher));
                        itProcessItValue->second.erase(guidPublisher);

                        // If all keys in the guidPublisher set are now gone, remove the processIdPublisher.
                        if (itProcessItValue->second.size() < 1)
                        {
                            // remove this processIdPublisher from the dictionary.
       		                CLTRACE(9, "CBadgeIconBase: OnEventRemoveSyncBoxFolderPath: Remove processIdPublisher: %x.", processIdPublisher);
                            itPathValue->second.processesThatAddedThisBadge.erase(processIdPublisher);

                            // If all keys in the processIdPublisher dictionary are now gone, remove the badge path itself.
                            if (itPathValue->second.processesThatAddedThisBadge.size() < 1)
                            {
                                // Remove this path from the badge dictionary
           		                CLTRACE(9, "CBadgeIconBase: OnEventRemoveSyncBoxFolderPath: Remove path <%ls> from the badging dictionary.", lowerCaseFullPath);
                                _mapRootFolders.erase(lowerCaseFullPath.m_str);

		                        // Delete all of the keys in the badging dictionary that have this folder path as a root.

                                bool fRemovedPathKey;
                                do
                                {
                                    fRemovedPathKey = false;
		                            for (boost::unordered_map<std::wstring, DATA_FOR_BADGE_PATH>::iterator it = _mapBadges.begin(); it != _mapBadges.end();  ++it)
		                            {
                   		                CLTRACE(9, "CBadgeIconBase: OnEventRemoveSyncBoxFolderPath: Found path <%ls>.", lowerCaseFullPath);
			                            if (IsPathInRootPath(it->first, lowerCaseFullPath.m_str))
			                            {
                                            // Remove this path, processIdPublisher, guidPublisher combination.
        		                            CLTRACE(9, "CBadgeIconBase: OnEventRemoveSyncBoxFolderPath: Remove this path only for this processId and guidPublisher: %ls.", it->first.c_str());
                                            fRemovedPathKey = OnEventRemoveBadgePath(CComBSTR(it->first.c_str()), processIdPublisher, guidPublisher);

                                            // If it was removed, notify Explorer to update the icon.
                                            if (fRemovedPathKey)
                                            {
            		                            CLTRACE(9, "CBadgeIconBase: OnEventRemoveSyncBoxFolderPath: Path was removed.");
                                                SHChangeNotify(SHCNE_ATTRIBUTES, SHCNF_PATH, it->first.c_str(), NULL);
                                                break;          // back to loop again so the iterator will be good
                                            }
			                            }
		                            }
                                } while (fRemovedPathKey);
          		                CLTRACE(9, "CBadgeIconBase: OnEventRemoveSyncBoxFolderPath: Finished removing.");
                            }
                        }
                    }
                }
            }
        }
        this->_mutexBadgeDatabase.unlock();

        // Notify Explorer of the changes we made
        SHChangeNotify(SHCNE_ATTRIBUTES, SHCNF_PATH, COLE2T(lowerCaseFullPath.m_str), NULL);
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
/// We received a timer tick from the PubSubServer watcher.  Check for dead processes and clean up.
/// </summary>
void CBadgeIconBase::OnEventTimerTick()
{
	try
	{
        CLTRACE(9, "CBadgeIconBase: OnEventTimerTick: Entry.");
        boost::unordered_set<ULONG> setCopyOfActiveProcesses;
        this->_mutexBadgeDatabase.lock();
        {
            // Copy the active process set into a local set that we will process at this event.
            CLTRACE(9, "CBadgeIconBase: OnEventTimerTick: Copy the active process IDs.");
            setCopyOfActiveProcesses = boost::unordered_set<ULONG>(_setActiveProcessIds);
        }
        this->_mutexBadgeDatabase.unlock();

        // Loop through the copied active process list.  Clean them if the process has died.
        for (boost::unordered_set<ULONG>::iterator itProcess = setCopyOfActiveProcesses.begin(); itProcess != setCopyOfActiveProcesses.end();  ++itProcess)
        {
            if (!CPubSubServer::IsProcessRunning(*itProcess))
            {
                CLTRACE(9, "CBadgeIconBase: OnEventTimerTick: Process ID %x is dead.  Clean the badging database.", *itProcess);
                CleanBadgingDatabaseForProcessId(*itProcess);   // locks on its own
            }
        }
	}
	catch (const std::exception &ex)
	{
		CLTRACE(1, "CBadgeIconBase: OnEventTimerTick: ERROR: Exception.  Message: %s.", ex.what());
	}
    catch (...)
    {
		CLTRACE(1, "CBadgeIconBase: OnEventTimerTick: ERROR: C++ exception.");
    }
}

/// <summary>
/// Clean the badging database of any paths placed by this process.
/// </summary>
void CBadgeIconBase::CleanBadgingDatabaseForProcessId(ULONG processIdPublisher)
{
	try
	{
        this->_mutexBadgeDatabase.lock();
        {
            boost::unordered_set<std::wstring> setPathsToErase;         // the paths that we will need to erase from the badging database

            // Loop through the badging database keys (paths).
			CLTRACE(9, "CBadgeIconBase: CleanBadgingDatabaseForProcessId: Entry.  ProcessId: %x.", processIdPublisher);
            for (boost::unordered_map<std::wstring, DATA_FOR_BADGE_PATH>::iterator itPathValue = _mapBadges.begin(); itPathValue != _mapBadges.end();  ++itPathValue)
		    {
				CLTRACE(9, "CBadgeIconBase: CleanBadgingDatabaseForProcessId: Process path <%ls>. BadgeType: %d.", itPathValue->first.c_str(), itPathValue->second.badgeType);
                boost::unordered_map<ULONG, boost::unordered_set<GUID>>::iterator itProcessItValue = itPathValue->second.processesThatAddedThisBadge.find(processIdPublisher);
        	    if (itProcessItValue != itPathValue->second.processesThatAddedThisBadge.end())
	            {
                    // We found the process here.  Remove it and free its guidPublishers.
					CLTRACE(9, "CBadgeIconBase: CleanBadgingDatabaseForProcessId: Found process here.  Clear the guids.");
                    itProcessItValue->second.clear();           // clear the syncbox guidPublishers
                    itPathValue->second.processesThatAddedThisBadge.erase(processIdPublisher);      // erase the process key from the map

                    // If there are no more processes in the map, remember this path to delete from the badging database
                    if (itPathValue->second.processesThatAddedThisBadge.size() == 0)
                    {
						CLTRACE(9, "CBadgeIconBase: CleanBadgingDatabaseForProcessId: Save this path to erase.");
                        setPathsToErase.insert(itPathValue->first);
                    }
                }
            }

            // Now loop through the pathsToErase and erase them from the badging database.
			CLTRACE(9, "CBadgeIconBase: CleanBadgingDatabaseForProcessId: Now loop erasing the paths for this process.");
            for (boost::unordered_set<std::wstring>::iterator itPathToErase = setPathsToErase.begin(); itPathToErase != setPathsToErase.end(); ++itPathToErase)
            {
				CLTRACE(9, "CBadgeIconBase: CleanBadgingDatabaseForProcessId: Erase path <%ls>.", (*itPathToErase).c_str());
                _mapBadges.erase(*itPathToErase);
            }

            // Remove this process ID from the active process set
			CLTRACE(9, "CBadgeIconBase: CleanBadgingDatabaseForProcessId: Remove process ID %x from the active list.", processIdPublisher);
            _setActiveProcessIds.erase(processIdPublisher);
        }
        this->_mutexBadgeDatabase.unlock();

		// Now do it again for the root folder database.
        this->_mutexBadgeDatabase.lock();
        {
            boost::unordered_set<std::wstring> setPathsToErase;         // the paths that we will need to erase from the badging database

            // Loop through the badging database keys (paths).
			CLTRACE(9, "CBadgeIconBase: CleanBadgingDatabaseForProcessId: Start cleaning the root folder database for processId: %x.", processIdPublisher);
            for (boost::unordered_map<std::wstring, DATA_FOR_BADGE_PATH>::iterator itPathValue = _mapRootFolders.begin(); itPathValue != _mapRootFolders.end();  ++itPathValue)
		    {
				CLTRACE(9, "CBadgeIconBase: CleanBadgingDatabaseForProcessId: Process path <%ls>. BadgeType: %d.", itPathValue->first.c_str(), itPathValue->second.badgeType);
                boost::unordered_map<ULONG, boost::unordered_set<GUID>>::iterator itProcessItValue = itPathValue->second.processesThatAddedThisBadge.find(processIdPublisher);
        	    if (itProcessItValue != itPathValue->second.processesThatAddedThisBadge.end())
	            {
                    // We found the process here.  Remove it and free its guidPublishers.
					CLTRACE(9, "CBadgeIconBase: CleanBadgingDatabaseForProcessId: Found process in root folder database.  Clear the guids.");
                    itProcessItValue->second.clear();           // clear the syncbox guidPublishers
                    itPathValue->second.processesThatAddedThisBadge.erase(processIdPublisher);      // erase the process key from the map

                    // If there are no more processes in the map, remember this path to delete from the badging database
                    if (itPathValue->second.processesThatAddedThisBadge.size() == 0)
                    {
						CLTRACE(9, "CBadgeIconBase: CleanBadgingDatabaseForProcessId: Save this path to erase later.");
                        setPathsToErase.insert(itPathValue->first);
                    }
                }
            }

            // Now loop through the pathsToErase and erase them from the badging database.
			CLTRACE(9, "CBadgeIconBase: CleanBadgingDatabaseForProcessId: Now loop erasing the root paths for this process.");
            for (boost::unordered_set<std::wstring>::iterator itPathToErase = setPathsToErase.begin(); itPathToErase != setPathsToErase.end(); ++itPathToErase)
            {
				CLTRACE(9, "CBadgeIconBase: CleanBadgingDatabaseForProcessId: Erase root path <%ls>.", (*itPathToErase).c_str());
                _mapBadges.erase(*itPathToErase);

				// Notify Explorer about this root path change.
		        SHChangeNotify(SHCNE_ATTRIBUTES, SHCNF_PATH, COLE2T((*itPathToErase).c_str()), NULL);
            }

            // Remove this process ID from the active process set
			CLTRACE(9, "CBadgeIconBase: CleanBadgingDatabaseForProcessId: Remove process ID %x from the active list.", processIdPublisher);
            _setActiveProcessIds.erase(processIdPublisher);
        }
        this->_mutexBadgeDatabase.unlock();
	}
	catch (const std::exception &ex)
	{
		CLTRACE(1, "CBadgeIconBase: CleanBadgingDatabaseForProcessId: ERROR: Exception.  Message: %s.", ex.what());
	}
    catch (...)
    {
		CLTRACE(1, "CBadgeIconBase: CleanBadgingDatabaseForProcessId: ERROR: C++ exception.");
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
        pThis->_mapRootFolders.clear();

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
		_pBadgeNetPubSubEvents->FireEventTimerTick.connect(boost::bind(&CBadgeIconBase::OnEventTimerTick, this));

        // Generate a GUID to represent this publisher
        HRESULT hr = CoCreateGuid(&_guidPublisher);
        if (!SUCCEEDED(hr))
        {
    		CLTRACE(1, "CBadgeIconBase: InitializeBadgeNetPubSubEvents: ERROR: Creating GUID. hr: %d.", hr);
            throw new std::exception("Error creating GUID");
        }
  		CLTRACE(9, "CBadgeIconBase: InitializeBadgeNetPubSubEvents: Publisher's GUID: %ls.", CComBSTR(_guidPublisher));

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

