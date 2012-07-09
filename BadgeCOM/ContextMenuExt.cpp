//
// ContextMenuExt.cpp
// Cloud Windows COM
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

// ContextMenuExt.cpp : Implementation of CContextMenuExt

#include "stdafx.h"
#include "ContextMenuExt.h"
#include <strsafe.h>


// CContextMenuExt

const char *CContextMenuExt::m_pszVerb = "TestVerb";
const wchar_t *CContextMenuExt::m_pwszVerb = L"TestVerb";

IFACEMETHODIMP CContextMenuExt::Initialize(__in_opt PCIDLIST_ABSOLUTE pidlFolder,
										   __in_opt IDataObject *pDataObject,
										   __in_opt HKEY hRegKey)
{
	return S_OK;

	//// In some cases, handlers are initialized multiple times. Therefore,
	//// clear any previous state here.
	//CoTaskMemFree(m_pidlFolder);
	//m_pidlFolder = NULL;

	//if (m_pdtobj)
	//{
	//	m_pdtobj->Release();
	//}

	//if (m_hRegKey)
	//{
	//	RegCloseKey(m_hRegKey);
	//	m_hRegKey = NULL;
	//}

	//// Capture the inputs for use later.
	//HRESULT hr = S_OK;

	//if (pidlFolder)
	//{
	//	m_pidlFolder = ILClone(pidlFolder);   // Make a copy to use later.
	//	hr = m_pidlFolder ? S_OK : E_OUTOFMEMORY;
	//}

	//if (SUCCEEDED(hr))
	//{
	//	// If a data object pointer was passed into the method, save it and
	//	// extract the file name.
	//	if (pDataObject)
	//	{
	//		m_pdtobj = pDataObject; 
 //           m_pdtobj->AddRef(); 
 //       }

	//	// It is uncommon to use the registry handle, but if you need it,
	//	// duplicate it now.
	//	if (hRegKey)
	//	{
	//		LSTATUS const result = RegOpenKeyEx(hRegKey, NULL, 0, KEY_READ, &m_hRegKey);
	//		hr = HRESULT_FROM_WIN32(result);
	//	}
	//}

	//return hr;
}

#define IDM_DISPLAY 0

STDMETHODIMP CContextMenuExt::QueryContextMenu(HMENU hMenu,
											   UINT indexMenu,
											   UINT idCmdFirst,
											   UINT idCmdLast,
											   UINT uFlags)
{
	HRESULT hr;

	if(!(CMF_DEFAULTONLY & uFlags))
	{
		InsertMenu(hMenu,
			indexMenu,
			MF_STRING | MF_BYPOSITION,
			idCmdFirst + IDM_DISPLAY,
			L"&Display File Name");

		// TODO: Add error handling to verify HRESULT return values.

		// need to make m_pszVerb non-constant
		const size_t m_pszVerbLen = strlen(CContextMenuExt::m_pszVerb);
		char * m_pszVerbCopy = new char[m_pszVerbLen + 1];
		strncpy(m_pszVerbCopy, CContextMenuExt::m_pszVerb, m_pszVerbLen);
		m_pszVerbCopy[m_pszVerbLen] = '\0';

		// need to make m_pwszVerb non-constant
		const size_t m_pwszVerbLen = lstrlen(CContextMenuExt::m_pwszVerb);
		wchar_t * m_pwszVerbCopy = new wchar_t[m_pwszVerbLen + 1];
		wcsncpy(m_pwszVerbCopy, CContextMenuExt::m_pwszVerb, m_pwszVerbLen);
		m_pwszVerbCopy[m_pwszVerbLen] = '\0';

		hr = StringCbCopyA(m_pszVerbCopy, sizeof(m_pszVerbCopy), "display");
		hr = StringCbCopyW(m_pwszVerbCopy, sizeof(m_pwszVerbCopy), L"display");

		free(m_pszVerbCopy);
		free(m_pwszVerbCopy);

		return MAKE_HRESULT(SEVERITY_SUCCESS, 0, USHORT(IDM_DISPLAY + 1));
	}

	return MAKE_HRESULT(SEVERITY_SUCCESS, 0, USHORT(0));
}

STDMETHODIMP CContextMenuExt::GetCommandString(
#ifdef X86;
	UINT idCommand,
#else
	UINT_PTR idCommand,
#endif
	UINT uFlags,
	LPUINT lpReserved,
	LPSTR pszName,
	UINT uMaxNameLen)
{
	HRESULT hr = E_INVALIDARG;

	if(idCommand != IDM_DISPLAY)
	{
		return hr;
	}

	switch(uFlags)
	{
		case GCS_HELPTEXTA:
			hr = StringCchCopyNA(pszName,
				lstrlen(m_pwszVerb)/sizeof(wchar_t),
				"Display File Name",
				uMaxNameLen);
			break;

		case GCS_HELPTEXTW:
			hr = StringCchCopyNW((LPWSTR)pszName,
				lstrlen(m_pwszVerb)/sizeof(wchar_t),
				L"Display File Name",
				uMaxNameLen);
			break;

		case GCS_VERBA:
			hr = StringCchCopyNA(pszName,
				lstrlen(m_pwszVerb)/sizeof(wchar_t),
				m_pszVerb,
				uMaxNameLen);
			break;

		case GCS_VERBW:
			hr = StringCchCopyNW((LPWSTR)pszName,
				lstrlen(m_pwszVerb)/sizeof(wchar_t),
				m_pwszVerb,
				uMaxNameLen);
			break;

		default:
			hr = S_OK;
			break;
	}
	return hr;
}

STDMETHODIMP CContextMenuExt::InvokeCommand(LPCMINVOKECOMMANDINFO lpcmi)
{
	BOOL fEx = FALSE;
	BOOL fUnicode = FALSE;

	if(lpcmi->cbSize == sizeof(CMINVOKECOMMANDINFOEX))
	{
		fEx = TRUE;
		if((lpcmi->fMask & CMIC_MASK_UNICODE))
		{
			fUnicode = TRUE;
		}
	}

	if(!fUnicode && HIWORD(lpcmi->lpVerb))
	{
		if(StrCmpIA(lpcmi->lpVerb, m_pszVerb))
		{
			return E_FAIL;
		}
	}

	else if(fUnicode && HIWORD(((CMINVOKECOMMANDINFOEX *) lpcmi)->lpVerbW))
	{
		if(StrCmpIW(((CMINVOKECOMMANDINFOEX *)lpcmi)->lpVerbW, m_pwszVerb))
		{
			return E_FAIL;
		}
	}

	else if(LOWORD(lpcmi->lpVerb) != IDM_DISPLAY)
	{
		return E_FAIL;
	}

	else
	{
		MessageBox(lpcmi->hwnd,
			L"The File Name",
			L"File Name",
			MB_OK|MB_ICONINFORMATION);
	}

	return S_OK;
}