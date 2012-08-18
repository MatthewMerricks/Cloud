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
//// for debugging only:
//#include <fstream>

using namespace std;

// Forward function definitions
size_t ExecuteProcess(std::wstring FullPathToExe, std::wstring Parameters);
std::wstring StringToWString(const std::string& s);
std::string WStringToString(const std::wstring& s);

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
		DWORD bytesWritten;
		BYTE pathPointerBytes[8];
		
		bool cloudProcessStarted = false;
		int cloudStartTries = 0;

		HANDLE pipeHandle;
		bool pipeConnectionFailed = false;

		wchar_t lpszUsername[UNLEN];
		DWORD dUsername = sizeof(lpszUsername);
 
		// Get the user name of the logged-in user.
		if(!GetUserName(lpszUsername, &dUsername))
		{
			return E_FAIL;
		}

		// Build the pipe name.  This will be (no escapes): "\\.\Pipe\<UserName>/BadgeCOM/ContextMenu"
		std::wstring pipeName = L"\\\\.\\Pipe\\";
		pipeName.append(lpszUsername);
		pipeName.append(L"/BadgeCOM/ContextMenu");

		// Try to open the named pipe identified by the pipe name.
		while (!pipeConnectionFailed)
		{
			pipeHandle = CreateFile(
				pipeName.c_str(), // Pipe name
				GENERIC_WRITE, // Write access
				0, // No sharing
				NULL, // Default security attributes
				OPEN_EXISTING, // Opens existing pipe
				0, // Default attributes
				NULL // No template file
				);
			
			// If the pipe handle is opened successfully then break out to continue
			if (pipeHandle != INVALID_HANDLE_VALUE)
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
						SHGetSpecialFolderPathW(0, programFilesDirectory, CSIDL_PROGRAM_FILESX86, FALSE);
						std::wstring cloudExeLocation(L"\"");
						cloudExeLocation.append(programFilesDirectory);
						cloudExeLocation.append(L"\\Cloud.com\\Cloud\\Cloud.exe\"");
						
						size_t rc = ExecuteProcess(cloudExeLocation, L"");
						if (rc == 0)
						{
							cloudProcessStarted = true;
						}
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
					if (!WaitNamedPipe(pipeName.c_str(), 2000))
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

		// Get the coordinates of the current Explorer window
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
		unsigned int index = 0;
		while (!m_szFile.empty())
		{
			std::wstring currentPop = m_szFile.back();
			m_szFile.pop_back();

			 root["asSelectedPaths"][index++] = WStringToString(currentPop);
		}

		// Send the information to BadgeNet in Cloud.exe.
		if (!pipeConnectionFailed)
		{
			try
			{
				// Format to a standard JSON string.
				Json::StyledWriter writer;
				std::string outputJson = writer.write( root );
				outputJson.append("\n");			// add a newline to force end of line on the server side.

				// Write it to Cloud.exe BadgeNet.
				if (WriteFile(pipeHandle,
							outputJson.c_str(),
							outputJson.length(),
							&bytesWritten,
							NULL) != 0)
				{
					// Successful
					int i = 0;
					i++;
				}
				else
				{
					// Error writing to the pipe
					DWORD err = GetLastError();
					int i = 0;
					i++;
				}
			}
			catch (exception &ex)
			{
				// Exception
				//cout << "Standard exception: " << ex.what() << endl;
				int i = 0;
				i++;
			}
		}
	}

	return S_OK;
}

// Start a new process.
size_t ExecuteProcess(std::wstring FullPathToExe, std::wstring Parameters) 
{ 
    size_t iMyCounter = 0, iReturnVal = 0, iPos = 0; 
    DWORD dwExitCode = 0; 
    std::wstring sTempStr = L""; 

    // Check to see if the file exists
	LPCWSTR fullPathToExe = FullPathToExe.c_str();
	if(INVALID_FILE_ATTRIBUTES == GetFileAttributesW(fullPathToExe) && GetLastError()==ERROR_FILE_NOT_FOUND) 
	{ 
	    // File not found 
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

    //if (CreateProcessW(const_cast<LPCWSTR>(FullPathToExe.c_str()), 
    //                        pwszParam, 0, 0, false, 
    //                        CREATE_DEFAULT_ERROR_MODE, 0, 0, 
    //                        &siStartupInfo, &piProcessInfo) != false) 
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
    } 

    // Free memory
    delete[]pwszParam; 
    pwszParam = 0; 

    // Release handles
    CloseHandle(piProcessInfo.hProcess); 
    CloseHandle(piProcessInfo.hThread); 

    return iReturnVal; 
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
