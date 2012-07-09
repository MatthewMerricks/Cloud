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

// define the strings used to identify the command coming back on context menu click??
const char *CContextMenuExt::m_pszVerb = "CloudCOMVerb";
const wchar_t *CContextMenuExt::m_pwszVerb = L"CloudCOMVerb";

// Called when before the context menu is created after a group of items were selected
IFACEMETHODIMP CContextMenuExt::Initialize(__in_opt PCIDLIST_ABSOLUTE pidlFolder,
										   __in_opt IDataObject *pDataObject,
										   __in_opt HKEY hRegKey)
{
	FORMATETC fmt =
	{
		CF_HDROP,
		NULL,
		DVASPECT_CONTENT,
		-1,
		TYMED_HGLOBAL
	};
	STGMEDIUM stg = { TYMED_HGLOBAL };
	HDROP hDrop;

	// Look for CF_HDROP data in the data object. If there
	// is no such data, return an error back to Explorer.
	if (FAILED(pDataObject->GetData(&fmt, &stg)))
		return E_INVALIDARG;

	// Get a pointer to the actual data.
	hDrop = (HDROP)GlobalLock(stg.hGlobal);

	// Make sure it worked.
	if (NULL == hDrop)
		return E_INVALIDARG;

	// Sanity check – make sure there is at least one filename.
	UINT uNumFiles = DragQueryFile(hDrop, 0xFFFFFFFF, NULL, 0);
	HRESULT hr = S_OK;

	if (0 == uNumFiles)
	{
		GlobalUnlock(stg.hGlobal);
		ReleaseStgMedium(&stg);
		return E_INVALIDARG;
	}

	if (!m_szFile.empty())
		m_szFile.clear();
	bool foundError = false;

	// loop through all the files, grab their file names, ensure they're valid, and store them
	for (int fileIndex = 0; fileIndex < uNumFiles; fileIndex++)
	{
		if (!foundError)
		{
			TCHAR currentFileName[MAX_PATH];

			if (0 == DragQueryFile(hDrop, fileIndex, currentFileName, MAX_PATH))
			{
				foundError = true;
				hr = E_INVALIDARG;
				if (!m_szFile.empty())
					m_szFile.clear();
			}
			else
			{
				m_szFile.push_back(std::wstring(currentFileName));
			}
		}
	}

	// free locally allocated memory
	GlobalUnlock(stg.hGlobal);
	ReleaseStgMedium(&stg);

	return hr;
}

// no idea??
#define IDM_DISPLAY 0

// Modifies the context menu to add the custom item
STDMETHODIMP CContextMenuExt::QueryContextMenu(HMENU hMenu,
											   UINT indexMenu,
											   UINT idCmdFirst,
											   UINT idCmdLast,
											   UINT uFlags)
{
	HRESULT hr;

	if(!(CMF_DEFAULTONLY & uFlags))
	{
		// Adds the custom menu item to the contex menu
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

		// writes the verbs to come back when the command fires??
		hr = StringCbCopyA(m_pszVerbCopy, sizeof(m_pszVerbCopy), "display");
		hr = StringCbCopyW(m_pwszVerbCopy, sizeof(m_pwszVerbCopy), L"display");

		// free locally allocated memory
		free(m_pszVerbCopy);
		free(m_pwszVerbCopy);

		return MAKE_HRESULT(SEVERITY_SUCCESS, 0, USHORT(IDM_DISPLAY + 1));
	}

	return MAKE_HRESULT(SEVERITY_SUCCESS, 0, USHORT(0));
}

// Gets the name of the item that appears in the menu
STDMETHODIMP CContextMenuExt::GetCommandString(
// Win32 and X64 platforms had different method signatures
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

	// some kind of switch based on the use of the context menu,
	// the options link back the verb used when the menu item was defined and set the display text that appears
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

// Describes the action to perform when the custom context menu item is clicked
STDMETHODIMP CContextMenuExt::InvokeCommand(LPCMINVOKECOMMANDINFO lpcmi)
{
	// don't know what most of this does

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
		// this part pulls the strings of file paths out
		// from the initialization array and appends them for a message box

		std::wstring allFiles;
		bool firstFile = true;

		while (!m_szFile.empty())
		{
			std::wstring currentPop = m_szFile.back();
			m_szFile.pop_back();

			if (!firstFile)
				allFiles.append(L"\r\n");
			allFiles.append(currentPop);

			firstFile = false;
		}

		MessageBox(lpcmi->hwnd,
			allFiles.c_str(),
			L"File Name",
			MB_OK|MB_ICONINFORMATION);
	}

	return S_OK;
}