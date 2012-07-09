//
// ContextMenuExt.h
// Cloud Windows COM
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

// ContextMenuExt.h : Declaration of the CContextMenuExt

#pragma once
#include "resource.h"       // main symbols



#include "BadgeCOM_i.h"



#if defined(_WIN32_WCE) && !defined(_CE_DCOM) && !defined(_CE_ALLOW_SINGLE_THREADED_OBJECTS_IN_MTA)
#error "Single-threaded COM objects are not properly supported on Windows CE platform, such as the Windows Mobile platforms that do not include full DCOM support. Define _CE_ALLOW_SINGLE_THREADED_OBJECTS_IN_MTA to force ATL to support creating single-thread COM object's and allow use of it's single-threaded COM object implementations. The threading model in your rgs file was set to 'Free' as that is the only threading model supported in non DCOM Windows CE platforms."
#endif

using namespace ATL;


// CContextMenuExt

class ATL_NO_VTABLE CContextMenuExt :
	public CComObjectRootEx<CComSingleThreadModel>,
	public CComCoClass<CContextMenuExt, &CLSID_ContextMenuExt>,
	public IShellExtInit,
	public IContextMenu
{
public:
	CContextMenuExt()
	{
	}

// IShellExtInit
IFACEMETHODIMP Initialize(LPCITEMIDLIST, LPDATAOBJECT, HKEY);

// IContextMenu

#ifdef X86
IFACEMETHODIMP GetCommandString(UINT, UINT, UINT*, LPSTR, UINT);
#else
IFACEMETHODIMP GetCommandString(UINT_PTR, UINT, UINT*, LPSTR, UINT);
#endif
IFACEMETHODIMP InvokeCommand(LPCMINVOKECOMMANDINFO);
IFACEMETHODIMP QueryContextMenu(HMENU, UINT, UINT, UINT, UINT);

DECLARE_REGISTRY_RESOURCEID(IDR_CONTEXTMENUEXT)

DECLARE_NOT_AGGREGATABLE(CContextMenuExt)

BEGIN_COM_MAP(CContextMenuExt)
	COM_INTERFACE_ENTRY(IShellExtInit)
	COM_INTERFACE_ENTRY(IContextMenu)
END_COM_MAP()

	DECLARE_PROTECT_FINAL_CONSTRUCT()

	HRESULT FinalConstruct()
	{
		return S_OK;
	}

	void FinalRelease()
	{
	}
	
	static const char *m_pszVerb;
	static const wchar_t *m_pwszVerb;

private:
	//// IDList of the folder for extensions invoked on the folder, such as
	//// background context menu handlers or nondefault drag-and-drop handlers.
	//PIDLIST_ABSOLUTE m_pidlFolder;

	//// The data object contains an expression of the items that the handler is
	//// being initialized for. Use SHCreateShellItemArrayFromDataObject to
	//// convert this object to an array of items. Use SHGetItemFromObject if you
	//// are only interested in a single Shell item. If you need a file system
	//// path, use IShellItem::GetDisplayName(SIGDN_FILESYSPATH, ...).
	//IDataObject *m_pdtobj;

	//// For context menu handlers, the registry key provides access to verb
	//// instance data that might be stored there. This is a rare feature to use
	//// so most extensions do not need this variable.
	//HKEY m_hRegKey;
};

OBJECT_ENTRY_AUTO(__uuidof(ContextMenuExt), CContextMenuExt)
