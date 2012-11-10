//
// BadgeIconFailed.cpp
// Cloud Windows COM
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

// BadgeIconFailed.cpp : Implementation of CBadgeIconFailed

#include "stdafx.h"
#include "BadgeIconFailed.h"
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

// CBadgeIconFailed

// IShellIconOverlayIdentifier::GetOverlayInfo
// returns The Overlay Icon Location to the system
// By debugging I found it only runs once when the com object is loaded to the system, thus it cannot return a dynamic icon
STDMETHODIMP CBadgeIconFailed::GetOverlayInfo(
	LPWSTR pwszIconFile,
	int cchMax,
	int* pIndex,
	DWORD* pdwFlags)
{
	try
	{
		//&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&  DEBUG REMOVE &&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&
		while (true)
		{
			Sleep(100);
		}
		//&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&  DEBUG REMOVE &&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&
		// Get our module's full path
		CLTRACE(9, "CBadgeIconFailed: GetOverlayInfo: Entry");
		GetModuleFileNameW(_AtlBaseModule.GetModuleInstance(), pwszIconFile, cchMax);

        // Use fourth icon in the resource (Failed.ico)
        *pIndex = 3;

		*pdwFlags = ISIOI_ICONFILE | ISIOI_ICONINDEX;

		// Allocate the PubSubEvents system, subscribe to events, and send an initialization event to BadgeNet.
		InitializeBadgeNetPubSubEvents();
	}
	catch(std::exception &ex)
	{
		CLTRACE(1, "CBadgeIconFailed: GetOverlayInfo: ERROR: Exception.  Message: %s.", ex.what());
	}
	return S_OK;
}

// IShellIconOverlayIdentifier::GetPriority
// returns the priority of this overlay 0 being the highest.
STDMETHODIMP CBadgeIconFailed::GetPriority(int* pPriority)
{
	CLTRACE(9, "CBadgeIconFailed: GetPriority: Entry");
	// change the following to set priority between multiple overlays
	*pPriority = 0;
	return S_OK;
}

typedef HRESULT (WINAPI*pfnGetDispName)(LPCWSTR, LPWSTR, DWORD);

// IShellIconOverlayIdentifier::IsMemberOf
// Returns whether the object should have this overlay or not 
STDMETHODIMP CBadgeIconFailed::IsMemberOf(LPCWSTR pwszPath, DWORD dwAttrib)
{
	//default return value is false (no icon overlay)
	HRESULT result = S_FALSE;   // or S_OK for icon overlay

	// Should this path be badged?  It will be badged this exact path is found in the the badging dictionary,
	// and if the current badgeType value matches this icon handler.
	boost::unordered_map<std::wstring, EnumCloudAppIconBadgeType>::iterator it = _mapBadges.find(pwszPath);
	if (it != _mapBadges.end() && it->second == cloudAppBadgeFailed)
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
void CBadgeIconFailed::OnEventAddBadgePath(BSTR fullPath, EnumCloudAppIconBadgeType badgeType)
{
	try
	{
		// Add or update the <path,badgeType>
		CLTRACE(9, "CBadgeIconFailed: OnEventAddBadgePath: Entry. Path: <%ls>.", fullPath);
		_mapBadges[fullPath] = badgeType;
	}
	catch(std::exception &ex)
	{
		CLTRACE(1, "CBadgeIconFailed: OnEventAddBadgePath: ERROR: Exception.  Message: %s.", ex.what());
	}
}

/// <summary>
/// We received a request to remove a badging path from BadgeNet.  There will be no error if it doesn't exist.
/// </summary>
/// <param name="fullPath">The full path of the item being removed.</param>
void CBadgeIconFailed::OnEventRemoveBadgePath(BSTR fullPath)
{
	try
	{
		// Remove the item with key fullPath.
		CLTRACE(9, "CBadgeIconFailed: OnEventRemoveBadgePath: Entry. Path: <%ls>.", fullPath);
		_mapBadges.erase(fullPath);
	}
	catch(std::exception &ex)
	{
		CLTRACE(1, "CBadgeIconFailed: OnEventRemoveBadgePath: ERROR: Exception.  Message: %s.", ex.what());
	}
}

/// <summary>
/// We received a request from BadgeNet to start tracking a new SyncBox folder path.
/// There will be no error if we are already tracking that path.
/// </summary>
/// <param name="fullPath">The full path of the folder being added.</param>
void CBadgeIconFailed::OnEventAddSyncBoxFolderPath(BSTR fullPath)
{
	try
	{
		// Add or update the fullPath.  The value is not used.
		CLTRACE(9, "CBadgeIconFailed: OnEventAddSyncBoxFolderPath: Entry. Path: <%ls>.", fullPath);
		_mapBadges[fullPath] = cloudAppBadgeNone;
	}
	catch(std::exception &ex)
	{
		CLTRACE(1, "CBadgeIconFailed: OnEventAddSyncBoxFolderPath: ERROR: Exception.  Message: %s.", ex.what());
	}
}

/// <summary>
/// We received a request from BadgeNet to stop tracking a SyncBox folder path.
/// There will be no error if we are already not tracking that path.
/// </summary>
/// <param name="fullPath">The full path of the folder being removed.</param>
void CBadgeIconFailed::OnEventRemoveSyncBoxFolderPath(BSTR fullPath)
{
	try
	{
		// Remove the item with key fullPath.
		CLTRACE(9, "CBadgeIconFailed: OnEventRemoveSyncBoxFolderPath: Entry. Path: <%ls>.", fullPath);
		_mapBadges.erase(fullPath);

		// Delete all of the keys in the badging dictionary that have this folder path as a root.
		for (boost::unordered_map<std::wstring, EnumCloudAppIconBadgeType>::iterator it = _mapBadges.begin(); it != _mapBadges.end();  /* bump in body of code */)
		{
			if (IsPathInRootPath(it->first, fullPath))
			{
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
		CLTRACE(1, "CBadgeIconFailed: OnEventRemoveSyncBoxFolderPath: ERROR: Exception.  Message: %s.", ex.what());
	}
}

/// <summary>
/// We received an error from the PubSubServer watcher.  We are no longer subscribed to badging events.
/// </summary>
/// <param name="fullPath">The full path of the folder being removed.</param>
void CBadgeIconFailed::OnEventSubscriptionWatcherFailed()
{
	try
	{
		// Restart the CBadgeNetPubSubEvents class, but not here because this event was fired by that
		// class.  Start a single-fire timer and do it in the timer event.
		CLTRACE(9, "CBadgeIconFailed: OnEventSubscriptionWatcherFailed: Entry.  ERROR: Badging failed.");
		_delayedMethodTimer.SetTimedEvent(this, &CBadgeIconFailed::OnDelayedEvent);
		_delayedMethodTimer.Start(100 /* start after ms delay */, false /* don't start immediately */, true /* run once */);
	}
	catch (std::exception &ex)
	{
		CLTRACE(1, "CBadgeIconFailed: OnEventSubscriptionWatcherFailed: ERROR: Exception.  Message: %s.", ex.what());
	}
}

void CBadgeIconFailed::OnDelayedEvent()
{
	try
	{
		// We lost the badging connection.  Empty the dictionaries.  They will be rebuilt if we can get another connection.
		_mapBadges.clear();
		_mapSyncBoxPaths.clear();

		// Restart the CBadgeNetPubSubEvents class.
		CLTRACE(9, "CBadgeIconFailed: OnDelayedEvent: Entry.");
		if (_pBadgeNetPubSubEvents != NULL)
		{
			// Kill the BadgeNetPubSubEvents threads and free resources.
			_pBadgeNetPubSubEvents->~CBadgeNetPubSubEvents();
			_pBadgeNetPubSubEvents = NULL;

			// Restart the BadgeNetPubSubEvents object
			InitializeBadgeNetPubSubEvents();
		}
	}
	catch (std::exception &ex)
	{
		CLTRACE(1, "CBadgeIconFailed: OnDelayedEvent: ERROR: Exception.  Message: %s.", ex.what());
	}
}

/// <summary>
/// Determines whether a path contains a root path.
/// </summary>
bool CBadgeIconFailed::IsPathInRootPath(std::wstring testPath, std::wstring rootPath)
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

void CBadgeIconFailed::InitializeBadgeNetPubSubEvents()
{
	try
	{
		CLTRACE(9, "CBadgeIconFailed: InitializeBadgeNetPubSubEvents: Entry.");
		_pBadgeNetPubSubEvents = new CBadgeNetPubSubEvents();
		_pBadgeNetPubSubEvents->Initialize();

		// Hook up events.  The "_1" and "_2" are placeholders required by bind (placeholders for the parameters).
		_pBadgeNetPubSubEvents->FireEventAddBadgePath.connect(boost::bind(&CBadgeIconFailed::OnEventAddBadgePath, this, _1, _2));
		_pBadgeNetPubSubEvents->FireEventRemoveBadgePath.connect(boost::bind(&CBadgeIconFailed::OnEventRemoveBadgePath, this, _1));
		_pBadgeNetPubSubEvents->FireEventAddSyncBoxFolderPath.connect(boost::bind(&CBadgeIconFailed::OnEventAddSyncBoxFolderPath, this, _1));
		_pBadgeNetPubSubEvents->FireEventRemoveSyncBoxFolderPath.connect(boost::bind(&CBadgeIconFailed::OnEventRemoveSyncBoxFolderPath, this, _1));
		_pBadgeNetPubSubEvents->FireEventSubscriptionWatcherFailed.connect(boost::bind(&CBadgeIconFailed::OnEventSubscriptionWatcherFailed, this));

		// Subscribe to the events from BadgeNet
		_pBadgeNetPubSubEvents->SubscribeToBadgeNetEvents();

		// Tell BadgeNet we just initialized.
		BSTR dummy;
		_pBadgeNetPubSubEvents->PublishEventToBadgeNet(BadgeCom_To_BadgeNet, BadgeCom_Initialization, cloudAppBadgeNone /* not used */, &dummy /* not used */);
	}
	catch(std::exception &ex)
	{
		CLTRACE(1, "CBadgeIconFailed: InitializeBadgeNetPubSubEvents: ERROR: Exception.  Message: %s.", ex.what());
	}
}


