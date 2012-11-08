//
// BadgeIconSynced.h
// Cloud Windows COM
//
// Created by DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

// BadgeIconSynced.h : Declaration of the CBadgeIconSynced

#pragma once
#include "resource.h"       // main symbols


#include "BadgeCOM_i.h"
#include "CBadgeNetPubSubEvents.h"

#include <ShlObj.h>
#include <comdef.h>



#if defined(_WIN32_WCE) && !defined(_CE_DCOM) && !defined(_CE_ALLOW_SINGLE_THREADED_OBJECTS_IN_MTA)
#error "Single-threaded COM objects are not properly supported on Windows CE platform, such as the Windows Mobile platforms that do not include full DCOM support. Define _CE_ALLOW_SINGLE_THREADED_OBJECTS_IN_MTA to force ATL to support creating single-thread COM object's and allow use of it's single-threaded COM object implementations. The threading model in your rgs file was set to 'Free' as that is the only threading model supported in non DCOM Windows CE platforms."
#endif

using namespace ATL;


// CBadgeIconSynced

class ATL_NO_VTABLE CBadgeIconSynced :
	public CComObjectRootEx<CComSingleThreadModel>,
	public CComCoClass<CBadgeIconSynced, &CLSID_BadgeIconSynced>,
	public IShellIconOverlayIdentifier,
	public IDispatchImpl<IBadgeIconSynced, &IID_IBadgeIconSynced, &LIBID_BadgeCOMLib, /*wMajor =*/ 1, /*wMinor =*/ 0>
{
public:
	CBadgeIconSynced()
	{
	}
	
	// IShellIconOverlayIdentifier Methods
  STDMETHOD(GetOverlayInfo)(LPWSTR pwszIconFile, 
           int cchMax,int *pIndex,DWORD* pdwFlags);
  STDMETHOD(GetPriority)(int* pPriority);
  STDMETHOD(IsMemberOf)(LPCWSTR pwszPath,DWORD dwAttrib);

DECLARE_REGISTRY_RESOURCEID(IDR_BADGEICONSYNCED)


BEGIN_COM_MAP(CBadgeIconSynced)
	COM_INTERFACE_ENTRY(IBadgeIconSynced)
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

public:



};

OBJECT_ENTRY_AUTO(__uuidof(BadgeIconSynced), CBadgeIconSynced)
