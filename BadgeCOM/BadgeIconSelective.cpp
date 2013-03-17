//
// BadgeIconSelective.cpp
// Cloud Windows COM
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

// BadgeIconSelective.cpp : Implementation of CBadgeIconSelective

#include "stdafx.h"
#include "BadgeIconSelective.h"

using namespace std;

// Debug trace
#define CLTRACE(intPriority, szFormat, ...) Trace::getInstance()->write(intPriority, szFormat, __VA_ARGS__)

// CBadgeIconSelective

/// <Summary>
/// Constructor.
/// </Summary>
CBadgeIconSelective::CBadgeIconSelective()
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
		CLTRACE(9, "CBadgeIconSelective: CBadgeIconSelective: Entry.");
        _pBaseShellExtension = new CBadgeIconBase(2 /* Selective icon index */, cloudAppBadgeSyncSelective);
    }
    catch (const std::exception &ex)
    {
		CLTRACE(1, "CBadgeIconSelective: CBadgeIconSelective: ERROR: Exception.  Message: %s.", ex.what());
    }
    catch (...)
    {
		CLTRACE(1, "CBadgeIconSelective: CBadgeIconSelective: ERROR: C++ exception.");
    }
}

/// <Summary>
/// Destructor.
/// </Summary>
CBadgeIconSelective::~CBadgeIconSelective()
{
    try
    {
		//&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&  DEBUG REMOVE &&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&
#if DEBUG_ENABLE_ONLY_SYNCED_BADGING
		return;
#endif // DEBUG_ENABLE_ONLY_SYNCED_BADGING
		//&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&  DEBUG REMOVE &&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&

        if (_pBaseShellExtension != NULL)
        {
            _pBaseShellExtension->~CBadgeIconBase();
            _pBaseShellExtension = NULL;
        }
    }
    catch (const std::exception &ex)
    {
		CLTRACE(1, "CBadgeIconSelective: ~CBadgeIconSelective: ERROR: Exception.  Message: %s.", ex.what());
    }
    catch (...)
    {
		CLTRACE(1, "CBadgeIconSelective: ~CBadgeIconSelective: ERROR: C++ exception.");
    }
}

// IShellIconOverlayIdentifier::GetOverlayInfo
// returns The Overlay Icon Location to the system
// By debugging I found it only runs once when the com object is loaded to the system, thus it cannot return a dynamic icon
STDMETHODIMP CBadgeIconSelective::GetOverlayInfo(
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
		CLTRACE(9, "CBadgeIconSelective: GetOverlayInfo: Entry");
        if (_pBaseShellExtension != NULL)
        {
            _pBaseShellExtension->GetOverlayInfo(pwszIconFile, cchMax, pIndex, pdwFlags);
        }
	}
	catch (const std::exception &ex)
	{
		CLTRACE(1, "CBadgeIconSelective: GetOverlayInfo: ERROR: Exception.  Message: %s.", ex.what());
	}
    catch (...)
    {
		CLTRACE(1, "CBadgeIconSelective: GetOverlayInfo: ERROR: C++ exception.");
    }

	return S_OK;
}

// IShellIconOverlayIdentifier::GetPriority
// returns the priority of this overlay 0 being the highest.
STDMETHODIMP CBadgeIconSelective::GetPriority(int* pPriority)
{
		//&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&  DEBUG REMOVE &&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&
#if DEBUG_ENABLE_ONLY_SYNCED_BADGING
		return S_OK;
#endif // DEBUG_ENABLE_ONLY_SYNCED_BADGING
		//&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&  DEBUG REMOVE &&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&

    try
    {
		// Pass it thru to base
	    CLTRACE(9, "CBadgeIconSelective: GetPriority: Entry");
        if (_pBaseShellExtension != NULL)
        {
            _pBaseShellExtension->GetPriority(pPriority);
        }
    }
    catch (const std::exception &ex)
    {
		CLTRACE(1, "CBadgeIconSelective: GetPriority: ERROR: Exception.  Message: %s.", ex.what());
    }
    catch (...)
    {
		CLTRACE(1, "CBadgeIconSelective: GetPriority: ERROR: C++ exception.");
    }

	return S_OK;
}

// IShellIconOverlayIdentifier::IsMemberOf
// Returns whether the object should have this overlay or not 
STDMETHODIMP CBadgeIconSelective::IsMemberOf(LPCWSTR pwszPath, DWORD dwAttrib)
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
		CLTRACE(1, "CBadgeIconSelective: IsMemberOf: ERROR: Exception.  Message: %s.", ex.what());
    }
    catch (...)
    {
		CLTRACE(1, "CBadgeIconSelective: IsMemberOf: ERROR: C++ exception.");
    }

	return result;
}

