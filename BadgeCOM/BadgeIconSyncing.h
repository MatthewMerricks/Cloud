//
// BadgeIconSyncing.h
// Cloud Windows COM
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

// BadgeIconSyncing.h : Declaration of the CBadgeIconSyncing

#pragma once
#include "resource.h"       // main symbols
#include "BadgeCOM_i.h"
#include "CBadgeNetPubSubEvents.h"
#include <boost\unordered_map.hpp>
#include <ShlObj.h>
#include <comdef.h>

#if defined(_WIN32_WCE) && !defined(_CE_DCOM) && !defined(_CE_ALLOW_SINGLE_THREADED_OBJECTS_IN_MTA)
#error "Single-threaded COM objects are not properly supported on Windows CE platform, such as the Windows Mobile platforms that do not include full DCOM support. Define _CE_ALLOW_SINGLE_THREADED_OBJECTS_IN_MTA to force ATL to support creating single-thread COM object's and allow use of it's single-threaded COM object implementations. The threading model in your rgs file was set to 'Free' as that is the only threading model supported in non DCOM Windows CE platforms."
#endif

using namespace ATL;

// CBadgeIconSyncing

class ATL_NO_VTABLE CBadgeIconSyncing :
	public CComObjectRootEx<CComMultiThreadModel>,
	public CComCoClass<CBadgeIconSyncing, &CLSID_BadgeIconSyncing>,
	public IShellIconOverlayIdentifier,
	public IDispatchImpl<IBadgeIconSyncing, &IID_IBadgeIconSyncing, &LIBID_BadgeCOMLib, /*wMajor =*/ 1, /*wMinor =*/ 0>
{
public:
	CBadgeIconSyncing();
    ~CBadgeIconSyncing();
	
	// IShellIconOverlayIdentifier Methods
    STDMETHOD(GetOverlayInfo)(LPWSTR pwszIconFile, 
            int cchMax,int *pIndex,DWORD* pdwFlags);
    STDMETHOD(GetPriority)(int* pPriority);
    STDMETHOD(IsMemberOf)(LPCWSTR pwszPath,DWORD dwAttrib);

    DECLARE_REGISTRY_RESOURCEID(IDR_BADGEICONSYNCING)

    BEGIN_COM_MAP(CBadgeIconSyncing)
        COM_INTERFACE_ENTRY(IBadgeIconSyncing)
        COM_INTERFACE_ENTRY(IDispatch)
        COM_INTERFACE_ENTRY(IShellIconOverlayIdentifier)
    END_COM_MAP()

	DECLARE_PROTECT_FINAL_CONSTRUCT()

	HRESULT FinalConstruct()
	{
		return S_OK;
	}

	void FinalRelease()
	{
	}

private:
    // Private fields
    CBadgeNetPubSubEvents *_pBadgeNetPubSubEvents;
    boost::unordered_map<std::wstring, EnumCloudAppIconBadgeType> _mapBadges;             // the dictionary of fullPath->badgeType
    HANDLE _threadSubscriptionRestart;

    // Private methods
    void OnEventAddBadgePath(BSTR fullPath, EnumCloudAppIconBadgeType badgeType);
    void OnEventRemoveBadgePath(BSTR fullPath);
    void OnEventAddSyncBoxFolderPath(BSTR fullPath);
    void OnEventRemoveSyncBoxFolderPath(BSTR fullPath);
    void OnEventSubscriptionWatcherFailed();
    std::wstring NormalizePath(std::wstring inPath);
    bool IsPathInRootPath(std::wstring testPath, std::wstring rootPath);
    static void SubscriptionRestartThreadProc(LPVOID pUserState);
    void InitializeBadgeNetPubSubEvents();
    void InitializeBadgeNetPubSubEventsViaThread();
    static void InitializeBadgeNetPubSubEventsThreadProc(LPVOID pUserState);

public:

};

OBJECT_ENTRY_AUTO(__uuidof(BadgeIconSyncing), CBadgeIconSyncing)
