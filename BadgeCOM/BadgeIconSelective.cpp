//
// BadgeIconSelective.cpp
// Cloud Windows COM
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

// BadgeIconSelective.cpp : Implementation of CBadgeIconSelective

#include "stdafx.h"
#include "BadgeIconSelective.h"
#include <Windows.h>
#include <stdio.h>
#include <sstream>
#include "lmcons.h"
#include "Trace.h"

using namespace std;

// Debug trace
#ifdef _DEBUG
	//#define CLTRACE(intPriority, szFormat, ...) 
	#define CLTRACE(intPriority, szFormat, ...) Trace::getInstance()->write(intPriority, szFormat, __VA_ARGS__)
#else	
	#define CLTRACE(intPriority, szFormat, ...)
	//#define CLTRACE(intPriority, szFormat, ...) Trace::getInstance()->write(intPriority, szFormat, __VA_ARGS__)
#endif // _DEBUG

// CBadgeIconSelective

// IShellIconOverlayIdentifier::GetOverlayInfo
// returns The Overlay Icon Location to the system
// By debugging I found it only runs once when the com object is loaded to the system, thus it cannot return a dynamic icon
STDMETHODIMP CBadgeIconSelective::GetOverlayInfo(
	LPWSTR pwszIconFile,
	int cchMax,
	int* pIndex,
	DWORD* pdwFlags)
{
	try
	{
		//&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&  DEBUG REMOVE &&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&
		//while (true)
		//{
		//	Sleep(100);
		//}
		//&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&  DEBUG REMOVE &&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&
		// Get our module's full path
		CLTRACE(9, "CBadgeIconSelective: GetOverlayInfo: Entry");
		GetModuleFileNameW(_AtlBaseModule.GetModuleInstance(), pwszIconFile, cchMax);

        // Use third icon in the resource (Selective.ico)
        *pIndex = 2;

		*pdwFlags = ISIOI_ICONFILE | ISIOI_ICONINDEX;

		// Allocate the PubSubEvents system, subscribe to events, and send an initialization event to BadgeNet.
		InitializeBadgeNetPubSubEventsViaThread();
	}
	catch(std::exception &ex)
	{
		CLTRACE(1, "CBadgeIconSelective: GetOverlayInfo: ERROR: Exception.  Message: %s.", ex.what());
	}
	return S_OK;
}

//@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@  DEBUG ONLY  @@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@
void CBadgeIconSelective::InitializeBadgeNetPubSubEventsViaThread()
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
//@@@@@@@@@@@@@@@@@@@@@@@@@@@@@

void CBadgeIconSelective::InitializeBadgeNetPubSubEventsThreadProc(LPVOID pUserState)
{
    // Cast the user state to an object instance
    CBadgeIconSelective *pThis = (CBadgeIconSelective *)pUserState;

    pThis->InitializeBadgeNetPubSubEvents();
}

// IShellIconOverlayIdentifier::GetPriority
// returns the priority of this overlay 0 being the highest.
STDMETHODIMP CBadgeIconSelective::GetPriority(int* pPriority)
{
	CLTRACE(9, "CBadgeIconSelective: GetPriority: Entry");
	// change the following to set priority between multiple overlays
	*pPriority = 0;
	return S_OK;
}

typedef HRESULT (WINAPI*pfnGetDispName)(LPCWSTR, LPWSTR, DWORD);

// IShellIconOverlayIdentifier::IsMemberOf
// Returns whether the object should have this overlay or not 
STDMETHODIMP CBadgeIconSelective::IsMemberOf(LPCWSTR pwszPath, DWORD dwAttrib)
{
	//default return value is false (no icon overlay)
	HRESULT result = S_FALSE;   // or S_OK for icon overlay

	// Should this path be badged?  It will be badged this exact path is found in the the badging dictionary,
	// and if the current badgeType value matches this icon handler.
	boost::unordered_map<std::wstring, EnumCloudAppIconBadgeType>::iterator it = _mapBadges.find(pwszPath);
	if (it != _mapBadges.end() && it->second == cloudAppBadgeSyncSelective)
	{
		result = S_OK;			// badge this icon
	}

	return result;
}

/// <summary>
/// We received a badging event from BadgeNet.  This may be a new path, or it may change the badge type
/// for an existing path.
/// </summary>
/// <param name="fullPath">The full path of the item being added.</param>
/// <param name="badgeType">The type of the badge.</param>
void CBadgeIconSelective::OnEventAddBadgePath(BSTR fullPath, EnumCloudAppIconBadgeType badgeType)
{
	try
	{
		// Add or update the <path,badgeType>
		CLTRACE(9, "CBadgeIconSelective: OnEventAddBadgePath: Entry. Path: <%ls>.", fullPath);
		_mapBadges[fullPath] = badgeType;
        SHChangeNotify(SHCNE_ATTRIBUTES, SHCNF_PATH, COLE2T(fullPath), NULL);
	}
	catch(std::exception &ex)
	{
		CLTRACE(1, "CBadgeIconSelective: OnEventAddBadgePath: ERROR: Exception.  Message: %s.", ex.what());
	}
}

/// <summary>
/// We received a request to remove a badging path from BadgeNet.  There will be no error if it doesn't exist.
/// </summary>
/// <param name="fullPath">The full path of the item being removed.</param>
void CBadgeIconSelective::OnEventRemoveBadgePath(BSTR fullPath)
{
	try
	{
		// Remove the item with key fullPath.
		CLTRACE(9, "CBadgeIconSelective: OnEventRemoveBadgePath: Entry. Path: <%ls>.", fullPath);
		_mapBadges.erase(fullPath);
        SHChangeNotify(SHCNE_ATTRIBUTES, SHCNF_PATH, COLE2T(fullPath), NULL);
	}
	catch(std::exception &ex)
	{
		CLTRACE(1, "CBadgeIconSelective: OnEventRemoveBadgePath: ERROR: Exception.  Message: %s.", ex.what());
	}
}

/// <summary>
/// We received a request from BadgeNet to start tracking a new SyncBox folder path.
/// There will be no error if we are already tracking that path.
/// </summary>
/// <param name="fullPath">The full path of the folder being added.</param>
void CBadgeIconSelective::OnEventAddSyncBoxFolderPath(BSTR fullPath)
{
	try
	{
		// Add or update the fullPath.  The value is not used.
		CLTRACE(9, "CBadgeIconSelective: OnEventAddSyncBoxFolderPath: Entry. Path: <%ls>.", fullPath);
		_mapBadges[fullPath] = cloudAppBadgeNone;
        SHChangeNotify(SHCNE_ATTRIBUTES, SHCNF_PATH, COLE2T(fullPath), NULL);
	}
	catch(std::exception &ex)
	{
		CLTRACE(1, "CBadgeIconSelective: OnEventAddSyncBoxFolderPath: ERROR: Exception.  Message: %s.", ex.what());
	}
}

/// <summary>
/// We received a request from BadgeNet to stop tracking a SyncBox folder path.
/// There will be no error if we are already not tracking that path.
/// </summary>
/// <param name="fullPath">The full path of the folder being removed.</param>
void CBadgeIconSelective::OnEventRemoveSyncBoxFolderPath(BSTR fullPath)
{
	try
	{
		// Remove the item with key fullPath.
		CLTRACE(9, "CBadgeIconSelective: OnEventRemoveSyncBoxFolderPath: Entry. Path: <%ls>.", fullPath);
		_mapBadges.erase(fullPath);
        SHChangeNotify(SHCNE_ATTRIBUTES, SHCNF_PATH, COLE2T(fullPath), NULL);

		// Delete all of the keys in the badging dictionary that have this folder path as a root.
		for (boost::unordered_map<std::wstring, EnumCloudAppIconBadgeType>::iterator it = _mapBadges.begin(); it != _mapBadges.end();  /* bump in body of code */)
		{
			if (IsPathInRootPath(it->first, fullPath))
			{
                SHChangeNotify(SHCNE_ATTRIBUTES, SHCNF_PATH, it->first.c_str(), NULL);
				it = _mapBadges.erase(it);
			}
			else
			{
				++it;
			}
		}
	}
	catch(std::exception &ex)
	{
		CLTRACE(1, "CBadgeIconSelective: OnEventRemoveSyncBoxFolderPath: ERROR: Exception.  Message: %s.", ex.what());
	}
}

/// <summary>
/// We received an error from the PubSubServer watcher.  We are no longer subscribed to badging events.
/// </summary>
/// <param name="fullPath">The full path of the folder being removed.</param>
void CBadgeIconSelective::OnEventSubscriptionWatcherFailed()
{
	try
	{
		// Restart the CBadgeNetPubSubEvents class, but not here because this event was fired by that
		// class.  Start another thread to do it.
		CLTRACE(9, "CBadgeIconSelective: OnEventSubscriptionWatcherFailed: Entry.  ERROR: Badging failed.");
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
	catch (std::exception &ex)
	{
		CLTRACE(1, "CBadgeIconSelective: OnEventSubscriptionWatcherFailed: ERROR: Exception.  Message: %s.", ex.what());
	}
}


/// <summary>
/// The BadgeNetPubSubEvents watcher failed.  We are no longer subscribed to badging events.  Restart
/// the subscription service.
/// </summary>
void CBadgeIconSelective::SubscriptionRestartThreadProc(LPVOID pUserState)
{
    // Cast the user state to an object instance
	CLTRACE(9, "CBadgeIconSelective: SubscriptionRestartThreadProc: Entry.");
    CBadgeIconSelective *pThis = (CBadgeIconSelective *)pUserState;

	try
	{
		// We lost the badging connection.  Empty the dictionaries.  They will be rebuilt if we can get another connection.
		pThis->_mapBadges.clear();
		pThis->_mapSyncBoxPaths.clear();

		// Restart the CBadgeNetPubSubEvents class.
		if (pThis->_pBadgeNetPubSubEvents != NULL)
		{
			// Kill the BadgeNetPubSubEvents threads and free resources.
    		CLTRACE(9, "CBadgeIconSelective: SubscriptionRestartThreadProc: Restart the subscription.");
			pThis->_pBadgeNetPubSubEvents->~CBadgeNetPubSubEvents();
			pThis->_pBadgeNetPubSubEvents = NULL;

			// Restart the BadgeNetPubSubEvents object
			pThis->InitializeBadgeNetPubSubEvents();
		}
	}
	catch (std::exception &ex)
	{
		CLTRACE(1, "CBadgeIconSelective: SubscriptionRestartThreadProc: ERROR: Exception.  Message: %s.", ex.what());
	}

    if (pThis != NULL)
    {
        CloseHandle(pThis->_threadSubscriptionRestart);
        pThis->_threadSubscriptionRestart = NULL;
    }
	CLTRACE(9, "CBadgeIconSelective: SubscriptionRestartThreadProc: Exit.");
}

/// <summary>
/// Determines whether a path contains a root path.
/// </summary>
bool CBadgeIconSelective::IsPathInRootPath(std::wstring testPath, std::wstring rootPath)
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

void CBadgeIconSelective::InitializeBadgeNetPubSubEvents()
{
	try
	{
		CLTRACE(9, "CBadgeIconSelective: InitializeBadgeNetPubSubEvents: Entry.");
		_pBadgeNetPubSubEvents = new CBadgeNetPubSubEvents();
		_pBadgeNetPubSubEvents->Initialize();

		// Hook up events.  The "_1" and "_2" are placeholders required by bind (placeholders for the parameters).
		_pBadgeNetPubSubEvents->FireEventAddBadgePath.connect(boost::bind(&CBadgeIconSelective::OnEventAddBadgePath, this, _1, _2));
		_pBadgeNetPubSubEvents->FireEventRemoveBadgePath.connect(boost::bind(&CBadgeIconSelective::OnEventRemoveBadgePath, this, _1));
		_pBadgeNetPubSubEvents->FireEventAddSyncBoxFolderPath.connect(boost::bind(&CBadgeIconSelective::OnEventAddSyncBoxFolderPath, this, _1));
		_pBadgeNetPubSubEvents->FireEventRemoveSyncBoxFolderPath.connect(boost::bind(&CBadgeIconSelective::OnEventRemoveSyncBoxFolderPath, this, _1));
		_pBadgeNetPubSubEvents->FireEventSubscriptionWatcherFailed.connect(boost::bind(&CBadgeIconSelective::OnEventSubscriptionWatcherFailed, this));

		// Subscribe to the events from BadgeNet
		_pBadgeNetPubSubEvents->SubscribeToBadgeNetEvents();

		// Tell BadgeNet we just initialized.
		BSTR dummy(L"");
		_pBadgeNetPubSubEvents->PublishEventToBadgeNet(BadgeCom_To_BadgeNet, BadgeCom_Initialization, cloudAppBadgeNone /* not used */, &dummy /* not used */);
	}
	catch(std::exception &ex)
	{
		CLTRACE(1, "CBadgeIconSelective: InitializeBadgeNetPubSubEvents: ERROR: Exception.  Message: %s.", ex.what());
	}
}


