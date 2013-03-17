//
// BadgeIconSyncing.cpp
// Cloud Windows COM
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

// BadgeIconSyncing.cpp : Implementation of CBadgeIconSyncing

#include "stdafx.h"
#include "BadgeIconSyncing.h"

using namespace std;

// Debug trace
#define CLTRACE(intPriority, szFormat, ...) Trace::getInstance()->write(intPriority, szFormat, __VA_ARGS__)

// CBadgeIconSyncing

/// <Summary>
/// Constructor.
/// </Summary>
CBadgeIconSyncing::CBadgeIconSyncing()
{
    try
    {
		//&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&  DEBUG REMOVE &&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&
#if WAIT_FOR_DEBUG
		static BOOL fCompletedOnce = false;
		while (!fCompletedOnce)
		{
			Sleep(100);
		}
		fCompletedOnce = true;
#endif // WAIT_FOR_DEBUG

#if DEBUG_ENABLE_ONLY_SYNCED_BADGING
		return;
#endif // DEBUG_ENABLE_ONLY_SYNCED_BADGING
		//&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&  DEBUG REMOVE &&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&

	    // Allocate the base shell extension.
		CLTRACE(9, "CBadgeIconSyncing: CBadgeIconSyncing: Entry.");
        _pBaseShellExtension = new CBadgeIconBase(0 /* Syncing icon index */, cloudAppBadgeSyncing);
    }
    catch (const std::exception &ex)
    {
		CLTRACE(1, "CBadgeIconSyncing: CBadgeIconSyncing: ERROR: Exception.  Message: %s.", ex.what());
    }
    catch (...)
    {
		CLTRACE(1, "CBadgeIconSyncing: CBadgeIconSyncing: ERROR: C++ exception.");
    }
}

/// <Summary>
/// Destructor.
/// </Summary>
CBadgeIconSyncing::~CBadgeIconSyncing()
{
		//&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&  DEBUG REMOVE &&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&
#if DEBUG_ENABLE_ONLY_SYNCED_BADGING
		return;
#endif // DEBUG_ENABLE_ONLY_SYNCED_BADGING
		//&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&  DEBUG REMOVE &&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&

    try
    {
        if (_pBaseShellExtension != NULL)
        {
            _pBaseShellExtension->~CBadgeIconBase();
            _pBaseShellExtension = NULL;
        }
    }
    catch (const std::exception &ex)
    {
		CLTRACE(1, "CBadgeIconSyncing: ~CBadgeIconSyncing: ERROR: Exception.  Message: %s.", ex.what());
    }
    catch (...)
    {
		CLTRACE(1, "CBadgeIconSyncing: ~CBadgeIconSyncing: ERROR: C++ exception.");
    }
}

// IShellIconOverlayIdentifier::GetOverlayInfo
// returns The Overlay Icon Location to the system
// By debugging I found it only runs once when the com object is loaded to the system, thus it cannot return a dynamic icon
STDMETHODIMP CBadgeIconSyncing::GetOverlayInfo(
	LPWSTR pwszIconFile,
	int cchMax,
	int* pIndex,
	DWORD* pdwFlags)
{
		//&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&  DEBUG REMOVE &&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&
#if DEBUG_ENABLE_ONLY_SYNCED_BADGING
		return S_OK;
#endif // DEBUG_ENABLE_ONLY_SYNCED_BADGING
		//&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&  DEBUG REMOVE &&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&

	try
	{
		// Pass it thru to base
		CLTRACE(9, "CBadgeIconSyncing: GetOverlayInfo: Entry");
        if (_pBaseShellExtension != NULL)
        {
            _pBaseShellExtension->GetOverlayInfo(pwszIconFile, cchMax, pIndex, pdwFlags);
        }
	}
	catch (const std::exception &ex)
	{
		CLTRACE(1, "CBadgeIconSyncing: GetOverlayInfo: ERROR: Exception.  Message: %s.", ex.what());
	}
    catch (...)
    {
		CLTRACE(1, "CBadgeIconSyncing: GetOverlayInfo: ERROR: C++ exception.");
    }

	return S_OK;
}

// IShellIconOverlayIdentifier::GetPriority
// returns the priority of this overlay 0 being the highest.
STDMETHODIMP CBadgeIconSyncing::GetPriority(int* pPriority)
{
		//&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&  DEBUG REMOVE &&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&
#if DEBUG_ENABLE_ONLY_SYNCED_BADGING
		return S_OK;
#endif // DEBUG_ENABLE_ONLY_SYNCED_BADGING
		//&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&  DEBUG REMOVE &&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&

    try
    {
		// Pass it thru to base
	    CLTRACE(9, "CBadgeIconSyncing: GetPriority: Entry");
        if (_pBaseShellExtension != NULL)
        {
            _pBaseShellExtension->GetPriority(pPriority);
        }
    }
    catch (const std::exception &ex)
    {
		CLTRACE(1, "CBadgeIconSyncing: GetPriority: ERROR: Exception.  Message: %s.", ex.what());
    }
    catch (...)
    {
		CLTRACE(1, "CBadgeIconSyncing: GetPriority: ERROR: C++ exception.");
    }

	return S_OK;
}

// IShellIconOverlayIdentifier::IsMemberOf
// Returns whether the object should have this overlay or not 
STDMETHODIMP CBadgeIconSyncing::IsMemberOf(LPCWSTR pwszPath, DWORD dwAttrib)
{
		//&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&  DEBUG REMOVE &&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&
#if DEBUG_ENABLE_ONLY_SYNCED_BADGING
		return S_FALSE;
#endif // DEBUG_ENABLE_ONLY_SYNCED_BADGING
		//&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&  DEBUG REMOVE &&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&

	// Default return value is false (no icon overlay)
	HRESULT result = S_FALSE;   // or S_OK for icon overlay

    try
    {
		// Pass it thru to base
        if (_pBaseShellExtension != NULL)
        {
            result = _pBaseShellExtension->IsMemberOf(pwszPath, dwAttrib);
        }
    }
    catch (const std::exception &ex)
    {
		CLTRACE(1, "CBadgeIconSyncing: IsMemberOf: ERROR: Exception.  Message: %s.", ex.what());
    }
    catch (...)
    {
		CLTRACE(1, "CBadgeIconSyncing: IsMemberOf: ERROR: C++ exception.");
    }

	return result;
}

