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
//// for debugging only:
//#include <fstream>

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

	/*std::fstream logStream;
	logStream.open("C:\\Users\\Public\\Documents\\logFile.txt", std::fstream::app | std::fstream::ate);
	logStream<<"EnteredInitialize"<<std::endl;
	logStream.close();*/

	// Look for CF_HDROP data in the data object. If there
	// is no such data, return an error back to Explorer.
	if (FAILED(pDataObject->GetData(&fmt, &stg)))
		return E_INVALIDARG;

	// Get a pointer to the actual data.
	hDrop = (HDROP)GlobalLock(stg.hGlobal);

	// Make sure it worked.
	if (NULL == hDrop)
		return E_INVALIDARG;
	
	DROPFILES *hDropFiles = (DROPFILES *)hDrop;
	
	/*std::fstream logStream2;
	logStream2.open("C:\\Users\\Public\\Documents\\logFile.txt", std::fstream::app | std::fstream::ate);
	logStream2<<"hDropFiles->pFiles"<<hDropFiles->pFiles<<std::endl;
	logStream2.close();*/
	
	if (!m_szFile.empty())
		m_szFile.clear();

	int hDropStartIndex = hDropFiles->pFiles;
	wchar_t *hDropCurrentChar = (wchar_t *)malloc((MAX_PATH + 1) * sizeof(wchar_t));

	while (true)
	{
		StrCpyW(hDropCurrentChar, (wchar_t *)hDropFiles + (hDropStartIndex / sizeof(wchar_t)));
		std::wstring hDropCurrentString(hDropCurrentChar);

		if (hDropCurrentString.length() > 0)
		{
			hDropStartIndex += (hDropCurrentString.length() + 1) * sizeof(wchar_t);
			m_szFile.push_back(hDropCurrentString);
		}
		else
		{
			break;
		}
	}
	
	// Sanity check – make sure there is at least one filename.
	HRESULT hr = S_OK;
	if (hDropStartIndex == hDropFiles->pFiles)
	{
		GlobalUnlock(stg.hGlobal);
		ReleaseStgMedium(&stg);
		return E_INVALIDARG;
	}

	// free locally allocated memory
	free(hDropCurrentChar);
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

	/*std::fstream logStream;
	logStream.open("C:\\Users\\Public\\Documents\\logFile.txt", std::fstream::app | std::fstream::ate);
	logStream<<"Entered QueryContextMenu"<<std::endl;
	logStream.close();*/

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

	/*std::fstream logStream;
	logStream.open("C:\\Users\\Public\\Documents\\logFile.txt", std::fstream::app | std::fstream::ate);
	logStream<<"Entered GetCommandString"<<std::endl;
	logStream.close();*/

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
	
	/*std::fstream logStream;
	logStream.open("C:\\Users\\Public\\Documents\\logFile.txt", std::fstream::app | std::fstream::ate);
	logStream<<"Entered InvokeCommand"<<std::endl;
	logStream.close();*/

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
		wchar_t const* pipeGetCloudDirectory = L"\\\\.\\Pipe\\BadgeCOMGetCloudPath";

		DWORD BytesRead;
		BYTE pathPointerBytes[8];
		
		bool cloudProcessStarted = false;
		int cloudStartTries = 0;

		HANDLE pathHandle;
		bool pipeConnectionFailed = false;

		// Try to open the named pipe identified by the pipe name.
		while (!pipeConnectionFailed)
		{
			pathHandle = CreateFile(
				pipeGetCloudDirectory, // Pipe name
				GENERIC_READ, // Write access
				0, // No sharing
				NULL, // Default security attributes
				OPEN_EXISTING, // Opens existing pipe
				0, // Default attributes
				NULL // No template file
				);
			
			// If the pipe handle is opened successfully then break out to continue
			if (pathHandle != INVALID_HANDLE_VALUE)
			{
				break;
			}
			// Pipe not successful, find out if it should try again
			else
			{
				// store not successful reason
				DWORD dwError = GetLastError();

				// This is the normal path when the application is not running (dwError will equal ERROR_FILE_NOT_FOUND)
				// Start the cloud process on the first attempt or increment a retry counter up to a certain point;
				// after 10 seconds of retrying, display an error message and stop trying
				if (ERROR_FILE_NOT_FOUND == dwError)
				{
					if (!cloudProcessStarted)
					{

						TCHAR programFilesDirectory[MAX_PATH];
						SHGetSpecialFolderPathW(0, programFilesDirectory, CSIDL_PROGRAM_FILES, FALSE);
						std::wstring cloudExeLocation(L"\"");
						cloudExeLocation.append(programFilesDirectory);
						cloudExeLocation.append(L"\\Cloud\\Cloud.exe\"");
						
						HANDLE cloudProcessHandle;
						CreateProcessAsUserW(cloudProcessHandle,
							NULL,

							//_tcsdup(cloudExeLocation.c_str()),

							_tcsdup(TEXT("\"C:\\Windows\\Notepad.exe\"")),

							NULL,
							NULL,
							NULL,
							NULL,
							NULL,
							NULL,
							NULL,
							NULL);

						_wsystem(cloudExeLocation.c_str());


						//_wsystem(L"C:\\Windows\\Notepad.exe");
						cloudProcessStarted = true;
					}
					else if (cloudStartTries > 99)
					{
						pipeConnectionFailed = true;

						MessageBox(lpcmi->hwnd,
							L"Cloud did not respond after ten seconds, operation cancelled",
							L"Cloud",
							MB_OK|MB_ICONINFORMATION);
					}
					else
					{
						cloudStartTries++;
						Sleep(100);
					}
				}
				// pipe is busy
				else if (ERROR_PIPE_BUSY == dwError)
				{
					// if waiting for a pipe does not complete in 2 seconds, exit  (by setting pipeConnectionFailed to true)
					if (!WaitNamedPipe(pipeGetCloudDirectory, 2000))
					{
						dwError = GetLastError();

						std::wstring errorMessage(L"Cloud is busy, operation cancelled: ");
						wchar_t *dwErrorChar = new wchar_t[10];
						wsprintf(dwErrorChar, L"%d", dwError);
						errorMessage.append(dwErrorChar);
						free(dwErrorChar);
						
						MessageBox(lpcmi->hwnd,
							errorMessage.c_str(),
							L"Cloud",
							MB_OK|MB_ICONINFORMATION);

						pipeConnectionFailed = true;
					}
				}
				// unknown error
				else
				{
					std::wstring errorMessage(L"An error occurred while communicating with Cloud, operation cancelled: ");
					wchar_t *dwErrorChar = new wchar_t[10];
					wsprintf(dwErrorChar, L"%d", dwError);
					errorMessage.append(dwErrorChar);
					free(dwErrorChar);

					MessageBox(lpcmi->hwnd,
						errorMessage.c_str(),
						L"Cloud",
						MB_OK|MB_ICONINFORMATION);

					pipeConnectionFailed = true;
				}
			}
		}

		if (!pipeConnectionFailed)
		{
			// get the size of the cloud path
			if (ReadFile(pathHandle,
				pathPointerBytes,
				8,
				&BytesRead,
				NULL))
			{
				if (BytesRead != 8)
				{
					std::wstring errorMessage(L"Cloud returned invalid data, operation cancelled: length=");
					wchar_t *bytesReadChar = new wchar_t[10];
					wsprintf(bytesReadChar, L"%d", BytesRead);
					errorMessage.append(bytesReadChar);
					free(bytesReadChar);
					errorMessage.append(L" data=");
					wchar_t *pathBytesChar = new wchar_t[5];
					for (int hexIndex = 0; hexIndex < BytesRead; hexIndex++)
					{
						wsprintf(pathBytesChar, L"%02x ", (unsigned char)pathPointerBytes[hexIndex]);
						errorMessage.append(pathBytesChar);
					}
					free(pathBytesChar);
						
					MessageBox(lpcmi->hwnd,
						errorMessage.c_str(),
						L"Cloud",
						MB_OK|MB_ICONINFORMATION);
				}
				else
				{
					wchar_t *retrievedPath = (wchar_t *)&BytesRead;
					
					MessageBox(lpcmi->hwnd,
						retrievedPath,
						L"Cloud",
						MB_OK|MB_ICONINFORMATION);
				}
			}
			else
			{
				std::wstring errorMessage(L"Cloud communication failed to return data, operation cancelled: ");
				wchar_t *dwErrorChar = new wchar_t[10];
				wsprintf(dwErrorChar, L"%d", GetLastError());
				errorMessage.append(dwErrorChar);
				free(dwErrorChar);

				MessageBox(lpcmi->hwnd,
					errorMessage.c_str(),
					L"Cloud",
					MB_OK|MB_ICONINFORMATION);
			}
		}

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