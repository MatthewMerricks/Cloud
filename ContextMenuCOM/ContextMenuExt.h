//
// ContextMenuExt.h
// Cloud Windows COM
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

// ContextMenuExt.h : Declaration of the CContextMenuExt

#pragma once
#include "resource.h"       // main symbols
#include <vector>
#include "ContextMenuCOM_i.h"

#if defined(_WIN32_WCE) && !defined(_CE_DCOM) && !defined(_CE_ALLOW_SINGLE_THREADED_OBJECTS_IN_MTA)
#error "Single-threaded COM objects are not properly supported on Windows CE platform, such as the Windows Mobile platforms that do not include full DCOM support. Define _CE_ALLOW_SINGLE_THREADED_OBJECTS_IN_MTA to force ATL to support creating single-thread COM object's and allow use of it's single-threaded COM object implementations. The threading model in your rgs file was set to 'Free' as that is the only threading model supported in non DCOM Windows CE platforms."
#endif

using namespace ATL;

// CContextMenuExt

class ATL_NO_VTABLE CContextMenuExt :
	public CComObjectRootEx<CComSingleThreadModel>,
	public CComCoClass<CContextMenuExt, &CLSID_ContextMenuExt>,
	// interface when the shell object is created (after a group of files is already selected)
	public IShellExtInit,
	// interface for the modifications to the context menu
	public IContextMenu
{
public:
    CContextMenuExt();
    ~CContextMenuExt();

// IShellExtInit

// Called when before the context menu is created after a group of items were selected
IFACEMETHODIMP Initialize(LPCITEMIDLIST, LPDATAOBJECT, HKEY);

// IContextMenu

// Win32 and X64 platforms had different method signatures
#ifdef X86
// Gets the name of the item that appears in the menu
IFACEMETHODIMP GetCommandString(UINT, UINT, UINT*, LPSTR, UINT);
#else
// Gets the name of the item that appears in the menu
IFACEMETHODIMP GetCommandString(UINT_PTR, UINT, UINT*, LPSTR, UINT);
#endif
// Describes the action to perform when the custom context menu item is clicked
IFACEMETHODIMP InvokeCommand(LPCMINVOKECOMMANDINFO);
// Modifies the context menu to add the custom item
IFACEMETHODIMP QueryContextMenu(HMENU, UINT, UINT, UINT, UINT);
DECLARE_REGISTRY_RESOURCEID(IDR_CONTEXTMENUEXT)
DECLARE_NOT_AGGREGATABLE(CContextMenuExt)
DECLARE_PROTECT_FINAL_CONSTRUCT()

BEGIN_COM_MAP(CContextMenuExt)
	COM_INTERFACE_ENTRY(IShellExtInit)
	COM_INTERFACE_ENTRY(IContextMenu)
END_COM_MAP()

	HRESULT FinalConstruct()
	{
		return S_OK;
	}

	void FinalRelease()
	{
	}
	
	// some sort of strings that are used to identify the command coming back on context menu click??
	static const char *m_pszVerb;
	static const wchar_t *m_pwszVerb;

protected:
	// holds the file names
	std::vector<std::wstring> m_szFile;
	// holds the number of file names
	int m_szFileLength;

	// The icon bitmap
	HBITMAP     m_hRegBmp;
};

OBJECT_ENTRY_AUTO(__uuidof(ContextMenuExt), CContextMenuExt)
