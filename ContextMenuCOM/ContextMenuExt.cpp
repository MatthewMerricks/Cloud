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
#include "JsonSerialization\json.h"
#include "lmcons.h"
#include <stdexcept>
#include "resource.h"
#include "..\BadgeCOM\Trace.h"
#include "psapi.h"

using namespace std;

// Debug trace
#define CLTRACE(intPriority, szFormat, ...) Trace::getInstance()->write(intPriority, szFormat, __VA_ARGS__)

// Forward function definitions
size_t ExecuteProcess(std::wstring FullPathToExe, std::wstring Parameters);
std::wstring StringToWString(const std::string& s);
std::string WStringToString(const std::wstring& s);
bool isProcessRunning(string pName);
bool IsCloudExePresent();

// CContextMenuExt

// define the strings used to identify the command coming back on context menu click??
const char *CContextMenuExt::m_pszVerb = "CloudCOMVerb";
const wchar_t *CContextMenuExt::m_pwszVerb = L"CloudCOMVerb";

/////////////////////////////////////////////////////////////////////////////
// CContextMenuExt construction/destruction

CContextMenuExt::CContextMenuExt()
{
	try
	{
		//&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&  DEBUG REMOVE &&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&
		//static bool fCompletedOnce = false;
		//while (!fCompletedOnce)
		//{
		//	Sleep(100);
		//}
		//fCompletedOnce = true;
		//&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&  DEBUG REMOVE &&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&

		CLTRACE(9, "ContextMenuExt: CContextMenuExt: Entry.");
	    m_hRegBmp = LoadBitmap (_AtlBaseModule.GetModuleInstance(), MAKEINTRESOURCE(IDB_BITMAP1) );
	}
    catch (...)
    {
		CLTRACE(1, "ContextMenuExt: CContextMenuExt: ERROR: Bad exception.");
    }
}

CContextMenuExt::~CContextMenuExt()
{
	try
	{
		if ( NULL != m_hRegBmp )
		{
			CLTRACE(9, "ContextMenuExt: ~CContextMenuExt: Entry.");
			DeleteObject ( m_hRegBmp );
			m_hRegBmp = NULL;
		}
	}
    catch (...)
    {
		CLTRACE(1, "ContextMenuExt: ~CContextMenuExt: ERROR: Bad exception.");
    }
}


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
	HDROP hDrop = NULL;
	bool allocatedStgMedium = false;
	wchar_t *hDropCurrentChar = NULL;

	try
	{
		// Look for CF_HDROP data in the data object. If there
		// is no such data, return an error back to Explorer.
		CLTRACE(9, "ContextMenuExt: Initialize: Entry.");
		if (FAILED(pDataObject->GetData(&fmt, &stg)))
		{
			CLTRACE(9, "ContextMenuExt: Initialize: Return E_INVALIDARG.");
			return E_INVALIDARG;
		}
		allocatedStgMedium = true;

		// Get a pointer to the actual data.
		hDrop = (HDROP)GlobalLock(stg.hGlobal);

		// Make sure it worked.
		if (NULL == hDrop)
		{
			CLTRACE(9, "ContextMenuExt: Initialize: Return E_INVALIDARG (2).");
			ReleaseStgMedium(&stg);
			allocatedStgMedium = false;
			return E_INVALIDARG;
		}
	
		DROPFILES *hDropFiles = (DROPFILES *)hDrop;
	
		if (!m_szFile.empty())
		{
			CLTRACE(9, "ContextMenuExt: Initialize: Clear m_szFile.");
			m_szFile.clear();
		}

		int hDropStartIndex = hDropFiles->pFiles;
		hDropCurrentChar = (wchar_t *)malloc((MAX_PATH + 1) * sizeof(wchar_t));
		if (hDropCurrentChar == NULL)
		{
			CLTRACE(9, "ContextMenuExt: Initialize: Return E_INVALIDARG (3).");
			GlobalUnlock(stg.hGlobal);
			hDrop = NULL;
			ReleaseStgMedium(&stg);
			allocatedStgMedium = false;
			return E_INVALIDARG;
		}

		while (true)
		{
			StrCpyW(hDropCurrentChar, (wchar_t *)hDropFiles + (hDropStartIndex / sizeof(wchar_t)));
			std::wstring hDropCurrentString(hDropCurrentChar);

			if (hDropCurrentString.length() > 0)
			{
				hDropStartIndex += (hDropCurrentString.length() + 1) * sizeof(wchar_t);
				CLTRACE(9, "ContextMenuExt: Initialize: Add selected path: <%s>.", hDropCurrentString.c_str());
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
			CLTRACE(9, "ContextMenuExt: Initialize: ERROR: No files selected.");
			free(hDropCurrentChar);
			hDropCurrentChar = NULL;
			GlobalUnlock(stg.hGlobal);
			hDrop = NULL;
			ReleaseStgMedium(&stg);
			allocatedStgMedium = false;
			return E_INVALIDARG;
		}

		// free locally allocated memory
		CLTRACE(9, "ContextMenuExt: Initialize: Clean up.");
		free(hDropCurrentChar);
		hDropCurrentChar = NULL;
		GlobalUnlock(stg.hGlobal);
		hDrop = NULL;
		ReleaseStgMedium(&stg);
		allocatedStgMedium = false;

		CLTRACE(9, "ContextMenuExt: Initialize: Return %d.", hr);
		return hr;
	}
	catch (exception ex)
	{
		CLTRACE(1, "ContextMenuExt: Initialize: ERROR: Exception: %s.", ex.what());
	}
    catch (...)
    {
		CLTRACE(1, "ContextMenuExt: Initialize: ERROR: Bad exception.");
    }

	// Free resources
	CLTRACE(9, "ContextMenuExt: Initialize: Free resources.");
	if (hDropCurrentChar != NULL)
	{
		free(hDropCurrentChar);
		hDropCurrentChar = NULL;
	}
	if (hDrop != NULL)
	{
		GlobalUnlock(stg.hGlobal);
		hDrop = NULL;
	}

	if (allocatedStgMedium)
	{
		ReleaseStgMedium(&stg);
		allocatedStgMedium = false;
	}


	CLTRACE(9, "ContextMenuExt: Initialize: Return E_INVALIDARG (4).");
	return E_INVALIDARG;
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

	try
	{
		CLTRACE(9, "ContextMenuExt: QueryContextMenu: Entry.");
		if(!(CMF_DEFAULTONLY & uFlags))
		{
			// Check to see if the Cloud.exe program is there.  Don't add the menu item if we can't start Cloud.
			if (!IsCloudExePresent())
			{
				CLTRACE(9, "ContextMenuExt: QueryContextMenu: Cloud.exe is not present.  Return.");
				return MAKE_HRESULT(SEVERITY_SUCCESS, 0, USHORT(0));
			}

			// Adds the custom menu item to the contex menu
			CLTRACE(9, "ContextMenuExt: QueryContextMenu: Insert our context menu entry.");
			InsertMenu(hMenu,
				indexMenu,
				MF_STRING | MF_BYPOSITION,
				idCmdFirst + IDM_DISPLAY,
				L"Share to &Cloud");

			// Set the bitmap for the register item.
			if ( NULL != m_hRegBmp )
			{
				CLTRACE(9, "ContextMenuExt: QueryContextMenu: Set our bitmap icon.");
				SetMenuItemBitmaps(hMenu, indexMenu, MF_BYPOSITION, m_hRegBmp, NULL);
			}

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
			hr = StringCbCopyA(m_pszVerbCopy, sizeof(m_pszVerbCopy), "ShareToCloud");
			hr = StringCbCopyW(m_pwszVerbCopy, sizeof(m_pwszVerbCopy), L"ShareToCloud");

			// free locally allocated memory
			free(m_pszVerbCopy);
			free(m_pwszVerbCopy);

			CLTRACE(9, "ContextMenuExt: QueryContextMenu: Return %u.", IDM_DISPLAY + 1);
			return MAKE_HRESULT(SEVERITY_SUCCESS, 0, USHORT(IDM_DISPLAY + 1));
		}
	}
	catch (exception ex)
	{
		CLTRACE(1, "ContextMenuExt: QueryContextMenu: ERROR: Exception: %s.", ex.what());
	}
    catch (...)
    {
		CLTRACE(1, "ContextMenuExt: QueryContextMenu: ERROR: Bad exception.");
    }
	CLTRACE(9, "ContextMenuExt: QueryContextMenu: Return(2) 0.");
	return MAKE_HRESULT(SEVERITY_SUCCESS, 0, USHORT(0));
}

//////////////////////////////////////////////////////////////////////////
//
// Function:    GetCommandString()
//
// Description:
//  Sets the flyby help string for the Explorer status bar.
//
//////////////////////////////////////////////////////////////////////////

STDMETHODIMP CContextMenuExt::GetCommandString (
// Win32 and X64 platforms had different method signatures
#ifdef X86;
	UINT uCmdID,
#else
	UINT_PTR uCmdID,
#endif
	UINT uFlags,
	LPUINT puReserved,
	LPSTR szName,
	UINT cchMax)
{
	USES_CONVERSION;
	LPCTSTR szPrompt;

	try
	{
		// Check to see if the Cloud.exe program is there.  Don't add the menu item if we can't start Cloud.
		if (!IsCloudExePresent())
		{
			CLTRACE(9, "ContextMenuExt: GetCommandString: Cloud.exe is not present.  Return.");
			return S_OK;
		}

		CLTRACE(9, "ContextMenuExt: GetCommandString: Normal processing.");
		if ( uFlags & GCS_HELPTEXT )
		{
			// Copy the help text into the supplied buffer.  If the shell wants
			// a Unicode string, we need to case szName to an LPCWSTR.
			CLTRACE(9, "ContextMenuExt: GetCommandString: Help texty.");
			szPrompt = _T("Copy to your Cloud folder");
			if ( uFlags & GCS_UNICODE )
			{
				lstrcpynW ( (LPWSTR) szName, T2CW(szPrompt), cchMax );
			}
			else
			{
				lstrcpynA ( szName, T2CA(szPrompt), cchMax );
			}
		}
		else if ( uFlags & GCS_VERB )
		{
			// Copy the verb name into the supplied buffer.  If the shell wants
			// a Unicode string, we need to case szName to an LPCWSTR.
			CLTRACE(9, "ContextMenuExt: GetCommandString: GCS_VERB.");
			if ( uFlags & GCS_UNICODE )
			{
				lstrcpynW ( (LPWSTR) szName, m_pwszVerb, cchMax );
			}
			else
			{
				lstrcpynA ( szName, m_pszVerb, cchMax );
			}
		}
	}
	catch (exception ex)
	{
		CLTRACE(1, "ContextMenuExt: GetCommandString: Exception: %s.", ex.what());
	}
    catch (...)
    {
		CLTRACE(1, "ContextMenuExt: GetCommandString: ERROR: Bad exception.");
    }

	CLTRACE(9, "ContextMenuExt: GetCommandString: Return S_OK.");
    return S_OK;
}

// Describes the action to perform when the custom context menu item is clicked
STDMETHODIMP CContextMenuExt::InvokeCommand(LPCMINVOKECOMMANDINFO lpcmi)
{
	// don't know what most of this does

	CLTRACE(9, "ContextMenuExt: InvokeCommand: Entry.");
	BOOL fEx = FALSE;
	BOOL fUnicode = FALSE;
	HANDLE PipeHandle = INVALID_HANDLE_VALUE;
	int createRetryCount = 3;
	
	try
	{
		// Check to see if the Cloud.exe program is there.  Don't add the menu item if we can't start Cloud.
		if (!IsCloudExePresent())
		{
			CLTRACE(9, "ContextMenuExt: InvokeCommand: Cloud.exe is not present.  Return.");
			return S_OK;
		}

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
				CLTRACE(9, "ContextMenuExt: InvokeCommand: Return E_FAIL.");
				return E_FAIL;
			}
		}

		else if(LOWORD(lpcmi->lpVerb) != IDM_DISPLAY)
		{
			CLTRACE(9, "ContextMenuExt: InvokeCommand: Return E_FAIL(2).");
			return E_FAIL;
		}

		else
		{
			DWORD bytesWritten;
			BYTE pathPointerBytes[8];
		
			bool cloudProcessStarted = false;
			int cloudStartTries = 0;

			bool pipeConnectionFailed = false;

			wchar_t lpszUsername[UNLEN];
			DWORD dUsername = sizeof(lpszUsername);
 
			CLTRACE(9, "ContextMenuExt: InvokeCommand: Process this command.");

			// Get the user name of the logged-in user.
			if(!GetUserName(lpszUsername, &dUsername))
			{
				CLTRACE(9, "ContextMenuExt: InvokeCommand: Return E_FAIL(3).");
				return E_FAIL;
			}

			// Build the pipe name.  This will be (no escapes): "\\.\Pipe\<UserName>/ContextMenuCOM/ContextMenu"
			std::wstring pipeName = L"\\\\.\\Pipe\\";
			pipeName.append(lpszUsername);
			pipeName.append(L"/ContextMenuCOM/ContextMenu");

			// Try to open the named pipe identified by the pipe name.
			while (!pipeConnectionFailed)
			{
				CLTRACE(9, "ContextMenuExt: InvokeCommand: Top of CreateFile loop.  Try to open the pipe.");
				PipeHandle = CreateFile(
					pipeName.c_str(), // Pipe name
					GENERIC_WRITE, // Write access
					0, // No sharing
					NULL, // Default security attributes
					OPEN_EXISTING, // Opens existing pipe
					0, // Default attributes
					NULL // No template file
					);
			
				// If the pipe handle is opened successfully then break out to continue
				if (PipeHandle != INVALID_HANDLE_VALUE)
				{
					CLTRACE(9, "ContextMenuExt: InvokeCommand: Opened successfully.");
					break;
				}
				// Pipe open not successful, find out if it should try again
				else
				{
					// store not successful reason
					DWORD dwError = GetLastError();
					CLTRACE(9, "ContextMenuExt: InvokeCommand: Open failed with code %ld.", dwError);

					// This is the normal path when the application is not running (dwError will equal ERROR_FILE_NOT_FOUND)
					// Start the cloud process on the first attempt or increment a retry counter up to a certain point;
					// after 10 seconds of retrying, display an error message and stop trying
					if (ERROR_FILE_NOT_FOUND == dwError)
					{
						CLTRACE(9, "ContextMenuExt: InvokeCommand: The file was not found.  Have we started Cloud?");
						if (!cloudProcessStarted)
						{
							// We haven't tried to start Cloud yet.  Maybe it is already running??
							CLTRACE(9, "ContextMenuExt: InvokeCommand: See if Cloud is running.");
							if (isProcessRunning("Cloud.exe"))
							{
								CLTRACE(9, "ContextMenuExt: InvokeCommand: Cloud is running.");
								cloudProcessStarted = true;
							}
							else
							{
								// Try to start Cloud.exe so it will open the other end of the pipe.
								CLTRACE(9, "ContextMenuExt: InvokeCommand: Cloud was not running.  Start it.");
								TCHAR programFilesDirectory[MAX_PATH];
								SHGetSpecialFolderPathW(0, programFilesDirectory, CSIDL_PROGRAM_FILESX86, FALSE);
								std::wstring cloudExeLocation(L"\"");
								cloudExeLocation.append(programFilesDirectory);
								cloudExeLocation.append(L"\\Cloud.com\\Cloud\\Cloud.exe\"");
						
								size_t rc = ExecuteProcess(cloudExeLocation, L"");
								if (rc == 0)
								{
									CLTRACE(9, "ContextMenuExt: InvokeCommand: Start was successful.");
									cloudProcessStarted = true;
								}
								else
								{
									// Error from ExecuteProcess
									CLTRACE(9, "ContextMenuExt: InvokeCommand: Error %d from ExecuteProcess.  Tell the user.");
									MessageBox(lpcmi->hwnd,
										L"Cloud could not be started, operation cancelled",
										L"Cloud",
										MB_OK|MB_ICONINFORMATION);

									// Exit now
		    						pipeConnectionFailed = true;
									break;
								}
							}
						}
						else if (cloudStartTries > 99)
						{
							CLTRACE(9, "ContextMenuExt: InvokeCommand: Too many retries, and Cloud should be running.  Tell the user.");
							MessageBox(lpcmi->hwnd,
								L"Cloud did not respond after ten seconds, operation cancelled",
								L"Cloud",
								MB_OK|MB_ICONINFORMATION);

							// Exit now
		    				pipeConnectionFailed = true;
							break;
						}
						else
						{
							CLTRACE(9, "ContextMenuExt: InvokeCommand: Wait 100 ms.");
							cloudStartTries++;
							Sleep(100);
						}

						// Close the pipe handle.  It will be recreated when we loop back up.
						CloseHandle(PipeHandle);
						PipeHandle = INVALID_HANDLE_VALUE;
						CLTRACE(9, "ContextMenuExt: InvokeCommand: Loop back up for a retry.");
					}
					// pipe is busy
					else if (ERROR_PIPE_BUSY == dwError)
					{
						// if waiting for a pipe does not complete in 2 seconds, exit  (by setting pipeConnectionFailed to true)
			    		CLTRACE(9, "ContextMenuExt: InvokeCommand: Error is pipe_busy. Wait for it for 2 seconds.");
						if (!WaitNamedPipe(pipeName.c_str(), 2000))
						{
							dwError = GetLastError();
		    				CLTRACE(9, "ContextMenuExt: InvokeCommand: ERROR: after wait.  Code: %lx.  Maybe we should retry.", dwError);
							if (createRetryCount-- > 0)
							{
								// We should retry
								CLTRACE(9, "ContextMenuExt: InvokeCommand: Loop to retry the CreateFile.");
								CloseHandle(PipeHandle);
								PipeHandle = INVALID_HANDLE_VALUE;
							}
							else
							{
								CLTRACE(9, "ContextMenuExt: InvokeCommand: ERROR: Out of retries.  CreateFile failed.  Tell the user.");

								// Tell the user with an ugly MessageBox!!!
								std::wstring errorMessage(L"Cloud is busy, operation cancelled: ");
								wchar_t *dwErrorChar = new wchar_t[10];
								wsprintf(dwErrorChar, L"%d", dwError);
								errorMessage.append(dwErrorChar);
								free(dwErrorChar);
						
								MessageBox(lpcmi->hwnd,
									errorMessage.c_str(),
									L"Cloud",
									MB_OK|MB_ICONINFORMATION);

								// Exit now
			    				pipeConnectionFailed = true;
								break;
							}
						}
						else
						{
							// The wait succeeded.  We should retry the CreateFile
							CLTRACE(9, "ContextMenuExt: InvokeCommand: Wait successful.  Loop to retry the CreateFile.");
							CloseHandle(PipeHandle);
							PipeHandle = INVALID_HANDLE_VALUE;
						}
					}
					// unknown error
					else
					{
						// Tell the user with an ugly MessageBox!!!
						CLTRACE(9, "ContextMenuExt: InvokeCommand: Unknown error.  Tell the user.");
						std::wstring errorMessage(L"An error occurred while communicating with Cloud, operation cancelled: ");
						wchar_t *dwErrorChar = new wchar_t[10];
						wsprintf(dwErrorChar, L"%d", dwError);
						errorMessage.append(dwErrorChar);
						free(dwErrorChar);

						MessageBox(lpcmi->hwnd,
							errorMessage.c_str(),
							L"Cloud",
							MB_OK|MB_ICONINFORMATION);

						// Exit now
						pipeConnectionFailed = true;
						break;
					}
				}
			}

			// Gather and send the information to ContextMenuNet in Cloud.exe if we got a connection.
			if (!pipeConnectionFailed)
			{
				// Get the coordinates of the current Explorer window
				CLTRACE(9, "ContextMenuExt: InvokeCommand: Pipe connection successful.  Get the window info.");
				HWND hwnd = GetActiveWindow();
				RECT rMyRect;
				GetClientRect(hwnd, (LPRECT)&rMyRect);
				ClientToScreen(hwnd, (LPPOINT)&rMyRect.left);
				ClientToScreen(hwnd, (LPPOINT)&rMyRect.right);

				// Put the information into a JSON object.  The formatted JSON will look like this:
				// {
				//		// Screen coordinates of the Explorer window.
				//		"window_coordinates" : { "left" : 100, "top" : 200, "right" : 300, "bottom" : 400 },
				//
				//		"selected_paths" : [
				//			"path 1",
				//			"path 2",
				//			"path 3"
				//		]
				// }
				Json::Value root;

				// Add the screen coordinates of the Explorer window
				root["rectExplorerWindowCoordinates"]["left"] = rMyRect.left;
				root["rectExplorerWindowCoordinates"]["top"] = rMyRect.top;
				root["rectExplorerWindowCoordinates"]["right"] = rMyRect.right;
				root["rectExplorerWindowCoordinates"]["bottom"] = rMyRect.bottom;

				// Add the selected paths
				CLTRACE(9, "ContextMenuExt: InvokeCommand: Add the selected paths.");
				unsigned int index = 0;
				while (!m_szFile.empty())
				{
					std::wstring currentPop = m_szFile.back();
					m_szFile.pop_back();

					 root["asSelectedPaths"][index++] = WStringToString(currentPop);
				}

				// Format to a standard JSON string.
				Json::FastWriter writer;
				std::string outputJson = writer.write( root );
				outputJson.append("\n");			// add a newline to force end of line on the server side.

				// Write it to Cloud.exe ContextMenuNet.
				CLTRACE(9, "ContextMenuExt: InvokeCommand: Write the JSON to the pipe.");
				if (WriteFile(PipeHandle,
							outputJson.c_str(),
							outputJson.length(),
							&bytesWritten,
							NULL) != 0)
				{
					// Successful
					CLTRACE(9, "ContextMenuExt: InvokeCommand: Write successful.");
				}
				else
				{
					// Error writing to the pipe
					DWORD err = GetLastError();
					CLTRACE(9, "ContextMenuExt: InvokeCommand: ERROR: Writing to pipe.  Code: %ld. Tell the user.", err);
					std::wstring errorMessage(L"An error occurred while communicating with Cloud (2), operation cancelled: ");
					wchar_t *dwErrorChar = new wchar_t[10];
					wsprintf(dwErrorChar, L"%d", err);
					errorMessage.append(dwErrorChar);
					free(dwErrorChar);

					MessageBox(lpcmi->hwnd,
						errorMessage.c_str(),
						L"Cloud",
						MB_OK|MB_ICONINFORMATION);
				}
			}
		}

	}
	catch (exception ex)
	{
		CLTRACE(1, "ContextMenuExt: InvokeCommand: ERROR: Exception: %s.", ex.what());
	}
    catch (...)
    {
		CLTRACE(1, "ContextMenuExt: InvokeCommand: ERROR: Bad exception.");
    }

	// Close the pipe handle
    if (PipeHandle != INVALID_HANDLE_VALUE)
	{
		CLTRACE(1, "ContextMenuExt: InvokeCommand: Close the pipe handle.");
		CloseHandle(PipeHandle);
		PipeHandle = INVALID_HANDLE_VALUE;
	}

	CLTRACE(9, "ContextMenuExt: InvokeCommand: Return OK.");
	return S_OK;
}

// Start a new process.
size_t ExecuteProcess(std::wstring FullPathToExe, std::wstring Parameters) 
{ 
    size_t iMyCounter = 0, iReturnVal = 0, iPos = 0; 
    DWORD dwExitCode = 0; 
    std::wstring sTempStr = L""; 

	try
	{
		// Check to see if the file exists
		CLTRACE(9, "ContextMenuExt: ExecuteProcess: Entry.  Path: %ls, Parms: %ls.", FullPathToExe, Parameters);
		LPCWSTR fullPathToExe = FullPathToExe.c_str();
		if(INVALID_FILE_ATTRIBUTES == GetFileAttributesW(fullPathToExe) && GetLastError()==ERROR_FILE_NOT_FOUND) 
		{ 
			// File not found 
			CLTRACE(9, "ContextMenuExt: ExecuteProcess: ERROR: FIle not found.  Return.");
			return -2; 
		} 

		// Add a space to the beginning of the Parameters
		if (Parameters.size() != 0) 
		{ 
			if (Parameters[0] != L' ') 
			{ 
				Parameters.insert(0,L" "); 
			} 
		} 

		// The first parameter needs to be the exe itself
		sTempStr = FullPathToExe; 
		iPos = sTempStr.find_last_of(L"\\");

		// The last character might be a double quote if the path contains blanks.  Remove the final double quote.
		sTempStr.erase(0, iPos +1); 
		if (sTempStr.length() > 0 && sTempStr.at(sTempStr.length()-1) == L'"')
		{
			sTempStr.erase(sTempStr.length() - 1, sTempStr.length() - 1); 
		}

		Parameters = sTempStr.append(Parameters); 

		// CreateProcessW can modify Parameters thus we allocate needed memory
		wchar_t * pwszParam = new wchar_t[Parameters.size() + 1]; 
		if (pwszParam == 0) 
		{ 
			return -1; 
		} 
		const wchar_t* pchrTemp = Parameters.c_str(); 
		wcscpy_s(pwszParam, Parameters.size() + 1, pchrTemp); 

		// CreateProcess API initialization
		STARTUPINFOW siStartupInfo; 
		PROCESS_INFORMATION piProcessInfo; 
		memset(&siStartupInfo, 0, sizeof(siStartupInfo)); 
		memset(&piProcessInfo, 0, sizeof(piProcessInfo)); 
		siStartupInfo.cb = sizeof(siStartupInfo); 

		sTempStr = FullPathToExe;

		if (CreateProcessW(NULL,
								&sTempStr[0], 0, 0, false, 
								CREATE_DEFAULT_ERROR_MODE, 0, 0, 
								&siStartupInfo, &piProcessInfo) != false) 
		{ 
			 // Watch the process.
			 //dwExitCode = WaitForSingleObject(piProcessInfo.hProcess, (SecondsToWait * 1000)); 
		} 
		else
		{ 
			// CreateProcess failed
			iReturnVal = GetLastError(); 
			CLTRACE(9, "ContextMenuExt: ExecuteProcess: ERROR: CreateProcess failed.  Code: %ld.", iReturnVal);
		} 

		// Free memory
		delete[]pwszParam; 
		pwszParam = 0; 

		// Release handles
		if (piProcessInfo.hProcess != NULL)
		{
			CloseHandle(piProcessInfo.hProcess); 
		}

		if (piProcessInfo.hThread != NULL)
		{
			CloseHandle(piProcessInfo.hThread); 
		}

	    return iReturnVal; 
	}
	catch (exception ex)
	{
		CLTRACE(1, "ContextMenuExt: ExecuteProcess: ERROR: Exception: %s.", ex.what());
	}
    catch (...)
    {
		CLTRACE(1, "ContextMenuExt: ExecuteProcess: ERROR: Bad exception.");
    }

	CLTRACE(9, "ContextMenuExt: ExecuteProcess: Return -3.");
	return -3;
} 

// Convert a std::string to a std::wstring
std::wstring StringToWString(const std::string& s)   
{   
    std::wstring temp(s.length(),L' ');   
    std::copy(s.begin(), s.end(), temp.begin());   
    return temp;   
}   
 
 
// Convert a std::wstring to a std::string
std::string WStringToString(const std::wstring& s)   
{   
    std::string temp(s.length(), ' ');   
    std::copy(s.begin(), s.end(), temp.begin());   
    return temp;   
} 


// Determine whether a process is running.
bool isProcessRunning(string pName)
{    
	unsigned long aProcesses[1024];
	unsigned long cbNeeded;
	unsigned long cProcesses;

	CLTRACE(9, "ContextMenuExt: isProcessRunning: Entry.  Name: %s.", pName);
	if(!EnumProcesses(aProcesses, sizeof(aProcesses), &cbNeeded))
	{
		CLTRACE(9, "ContextMenuExt: isProcessRunning: ERROR: From EnumProcesses.  Return false.");
		return false;
	}
	
	cProcesses = cbNeeded / sizeof(unsigned long);
	for(unsigned int i = 0; i < cProcesses; i++)
	{
		char szProcessName[MAX_PATH] = "<unknown>";

		if(aProcesses[i] == 0)
		{
			continue;
		}
		
		HANDLE hProcess = OpenProcess(PROCESS_QUERY_INFORMATION | PROCESS_VM_READ, 0, aProcesses[i]);
		if (NULL != hProcess)
		{
			// Given a handle to a process, this returns all the modules running within the process.
			// The first module is the executable running the process,
			// and subsequent handles describe DLLs loaded into the process.
			HMODULE hMod;
			DWORD cbNeeded2;
		    if ( EnumProcessModules(hProcess, &hMod, sizeof(hMod), &cbNeeded2))
			{
				// This function returns the short name for a module, typically the file name portion of the EXE or DLL.
				GetModuleBaseNameA( hProcess, hMod, szProcessName, sizeof(szProcessName)/sizeof(char) );
			}

			CloseHandle(hProcess);
		}

		if(pName == szProcessName)
		{
			CLTRACE(9, "ContextMenuExt: isProcessRunning: Found the process.  Return true.");
			return true;
		}
	} 

	CLTRACE(9, "ContextMenuExt: isProcessRunning: Process not found. Return false.");
	return false;
}


// Get the full path\filename.ext of Cloud.exe
std::wstring GetCloudExeFullPath()
{
	CLTRACE(9, "ContextMenuExt: GetCloudExeFullPath: Entry.");
	TCHAR programFilesDirectory[MAX_PATH];
	SHGetSpecialFolderPathW(0, programFilesDirectory, CSIDL_PROGRAM_FILESX86, FALSE);
	std::wstring cloudExeLocation(L"\"");
	cloudExeLocation.append(programFilesDirectory);
	cloudExeLocation.append(L"\\Cloud.com\\Cloud\\Cloud.exe\"");
	CLTRACE(9, "ContextMenuExt: GetCloudExeFullPath: Return Cloud.exe path <%ls>.", cloudExeLocation.c_str());
	return cloudExeLocation;
}

// Determine whether Cloud.exe is present.
bool IsCloudExePresent()
{
	CLTRACE(9, "ContextMenuExt: IsCloudExePresent: Entry");
	std::wstring pathCloudExe = GetCloudExeFullPath();
	CLTRACE(9, "ContextMenuExt: IsCloudExePresent: Cloud.exe is at path %ls.", pathCloudExe.c_str());

	if(INVALID_FILE_ATTRIBUTES == GetFileAttributesW(pathCloudExe.c_str()) && GetLastError() == ERROR_FILE_NOT_FOUND) 
	{ 
		// File not found 
		CLTRACE(9, "ContextMenuExt: IsCloudExePresent: ERROR: FIle not found.  Return.");
		return false;
	} 

	CLTRACE(9, "ContextMenuExt: IsCloudExePresent: Cloud.exe exists.  Return.");
	return true;
}