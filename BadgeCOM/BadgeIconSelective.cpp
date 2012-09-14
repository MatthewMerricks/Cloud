//
// BadgeIconSelective.cpp
// Cloud Windows COM
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

// BadgeIconSelective.cpp : Implementation of CBadgeIconSelective

#include "stdafx.h"
#include "BadgeIconSelective.h"
#include <Windows.h>
#include <stdio.h>
#include <sstream>
#include "lmcons.h"
#include "Trace.h"
using namespace std;

// Debug trace
#ifdef _DEBUG
	#define CLTRACE(intPriority, szFormat, ...) //#define CLTRACE(intPriority, szFormat, ...) Trace::getInstance()->write(intPriority, szFormat, __VA_ARGS__)
#else	
	#define CLTRACE(intPriority, szFormat, ...)
#endif // _DEBUG

// CBadgeIconSelective

// IShellIconOverlayIdentifier::GetOverlayInfo
// returns The Overlay Icon Location to the system
// By debugging I found it only runs once when the com object is loaded to the system, thus it cannot return a dynamic icon
STDMETHODIMP CBadgeIconSelective::GetOverlayInfo(
	LPWSTR pwszIconFile,
	int cchMax,
	int* pIndex,
	DWORD* pdwFlags)
{
	CLTRACE(9, "CBadgeIconSelective: GetOverlayInfo: Entry");

	// Get our module's full path
	GetModuleFileNameW(_AtlBaseModule.GetModuleInstance(), pwszIconFile, cchMax);

	// Use third icon in the resource (Selective.ico)
	*pIndex = 2;

	*pdwFlags = ISIOI_ICONFILE | ISIOI_ICONINDEX;
	return S_OK;
}

// IShellIconOverlayIdentifier::GetPriority
// returns the priority of this overlay 0 being the highest.
STDMETHODIMP CBadgeIconSelective::GetPriority(int* pPriority)
{
	CLTRACE(9, "CBadgeIconSelective: GetPriority: Entry");
	// change the following to set priority between multiple overlays
	*pPriority = 0;
	return S_OK;
}

typedef HRESULT (WINAPI*pfnGetDispName)(LPCWSTR, LPWSTR, DWORD);

// IShellIconOverlayIdentifier::IsMemberOf
// Returns whether the object should have this overlay or not 
STDMETHODIMP CBadgeIconSelective::IsMemberOf(LPCWSTR pwszPath, DWORD dwAttrib)
{
	//default return value is false (no icon overlay)
	HRESULT r = S_FALSE;
	HANDLE PipeHandle = INVALID_HANDLE_VALUE;
	int createRetryCount = 3;

	//copy input path to local unicode char
	wchar_t *s = _wcsdup(pwszPath);
	try
	{
		CLTRACE(9, "CBadgeIconSelective: IsMemberOf: Entry. Path: %ls, Attrib: %lx.", pwszPath, dwAttrib);
		//store length of copied path
		int sLength = wcslen(s);

		//Declare some variables
		DWORD BytesWritten;
		DWORD BytesRead;
		bool pipeConnectionFailed = false;

		// Get the user name of the logged-in user.
		wchar_t lpszUsername[UNLEN];
		DWORD dUsername = sizeof(lpszUsername);
		if(!GetUserName(lpszUsername, &dUsername))
		{
			CLTRACE(9, "CBadgeIconSelective: IsMemberOf: ERROR: From GetUserName.");
			return r;
		}

		// Build the pipe name.  This will be (no escapes): "\\.\Pipe\<UserName>/BadgeCOMcloudAppBadgeSyncSelective"
		std::wstring pipeForCurrentBadgeType = L"\\\\.\\Pipe\\";
		pipeForCurrentBadgeType.append(lpszUsername);
		pipeForCurrentBadgeType.append(L"/BadgeCOMcloudAppBadgeSyncSelective");

		// Loop until we get a connection that can be used.
		while (true)
		{
			CLTRACE(9, "CBadgeIconSelective: IsMemberOf: Open the pipe for writing and reading.");
			PipeHandle = CreateFile(
				pipeForCurrentBadgeType.c_str(), // Pipe name
				GENERIC_WRITE | GENERIC_READ, // bidirectional
				0, // No sharing
				NULL, // Default security attributes
				OPEN_EXISTING, // Opens existing pipe
				0, // Default attributes
				NULL // No template file
				);

			// If the pipe handle is opened successfully then break out to continue
			if (PipeHandle != INVALID_HANDLE_VALUE)
			{
				CLTRACE(9, "CBadgeIconSelective: IsMemberOf: Created the write pipe.");
				break;
			}
			// Pipe not successful, find out if it should try again
			else
			{
				// store not successful reason
				DWORD dwError = GetLastError();
				CLTRACE(9, "CBadgeIconSelective: IsMemberOf: ERROR: Write pipe not opened.  Code: %lx.", dwError);

				// Exit if an error other than ERROR_PIPE_BUSY occurs (by setting pipeConnectionFailed to true)
				// This is the normal path when the application is not running (dwError will equal ERROR_FILE_NOT_FOUND)
				if (ERROR_PIPE_BUSY != dwError)
				{
					CLTRACE(9, "CBadgeIconSelective: IsMemberOf: Error is not pipe_busy.");
					pipeConnectionFailed = true;
					break;
				}
				// pipe is busy
				else
				{
		    		CLTRACE(9, "CBadgeIconSelective: IsMemberOf: Error is pipe_busy. Wait for it for 2 seconds.");
		    		// if waiting for a pipe does not complete in 2 seconds, exit  (by setting pipeConnectionFailed to true)
		    		if (!WaitNamedPipe(pipeForCurrentBadgeType.c_str(), 2000))
		    		{
		    			dwError = GetLastError();
		    			CLTRACE(9, "CBadgeIconSelective: IsMemberOf: ERROR: after wait.  Code: %lx.  Maybe we should retry.", dwError);
						if (createRetryCount-- > 0)
						{
							// We should retry
							CLTRACE(9, "CBadgeIconSelective: IsMemberOf: Loop to retry the CreateFile.");
							CloseHandle(PipeHandle);
							PipeHandle = INVALID_HANDLE_VALUE;
						}
						else
						{
							CLTRACE(9, "CBadgeIconSelective: IsMemberOf: ERROR: Out of retries.  CreateFile failed.");
			    			pipeConnectionFailed = true;
							break;
						}

		    		}
					else
					{
						// The wait succeeded.  We should retry the CreateFile
						CLTRACE(9, "CBadgeIconSelective: IsMemberOf: Wait successful.  Loop to retry the CreateFile.");
						CloseHandle(PipeHandle);
						PipeHandle = INVALID_HANDLE_VALUE;
					}
				}
			}
		}

		// if pipe connection did not fail begin pipe transfer logic
		CLTRACE(9, "CBadgeIconSelective: IsMemberOf: Continue.  pipeConnectionFailed: %d.", pipeConnectionFailed);
		if (!pipeConnectionFailed)
		{
			CLTRACE(9, "CBadgeIconSelective: IsMemberOf: Write pipe is open.");
			// calculate bytes required to send filepath
			unsigned int pathLength = sizeof(wchar_t)*sLength;
			// unique id for each connection attempt (used for unique return pipe)
			volatile static unsigned long pipeCounter = 0;
			long localPipeCounter = InterlockedIncrement(&pipeCounter);

			// store pipeCounter as string
			stringstream packetIdStream;
			packetIdStream << localPipeCounter;
			CLTRACE(9, "CBadgeIconSelective: IsMemberOf: Pipe counter is %lu.", localPipeCounter);

			//need to zero-pad packetId to ensure constant length of 10 chars
			string packetId;
			packetIdStream >> packetId;
			int startLength = packetId.length();
			for (int currentPaddedChar = 0; currentPaddedChar < 10 - startLength; currentPaddedChar++)
				packetId = "0" + packetId;

			// store filepath byte length as string
			stringstream currentPathLength;
			currentPathLength << pathLength;

			//need to zero-pad pathLength to ensure constant length of 10 chars
			string paddedLength;
			currentPathLength >> paddedLength;
			startLength = paddedLength.length();
			for (int currentPaddedChar = 0; currentPaddedChar < 10 - startLength; currentPaddedChar++)
			{
				paddedLength = "0" + paddedLength;
			}
		
			// write packetId + filepath byte length to pipe (must be 20 bytes)
			if (WriteFile(PipeHandle,
				(packetId + paddedLength).c_str(),
				20,//needs to be 20 since that's what's being read from the other end
				&BytesWritten,
				NULL) != 0)
			{
				CLTRACE(9, "CBadgeIconSelective: IsMemberOf: Wrote the pipe counter.");
				// write filepath to pipe (variable bytes)
				if (WriteFile(PipeHandle,
					s, //filepath
					pathLength, //length of filepath
					&BytesWritten,
					NULL) != 0)
				{
					// Done writing.  Now read the result (badge or don't).
					CLTRACE(9, "CBadgeIconSelective: IsMemberOf: Read the result.");
					byte byBuf;
					bool fSuccess = ReadFile( 
								PipeHandle,		// pipe handle 
								&byBuf,			// buffer to receive reply 
								1,				// size of buffer 
								&BytesRead,		// number of bytes read 
								NULL);			// not overlapped 
					if (fSuccess)
					{
						// We read a byte from the channel.  What was it?
						CLTRACE(9, "CBadgeIconSelective: IsMemberOf: Read success.  BytesRead: %ld.", BytesRead);
						if (byBuf == 1)
						{
							CLTRACE(9, "CBadgeIconSelective: IsMemberOf: Set return code to badge the icon.");
							r = S_OK;
						}
					}
					else
					{
						// Error reading.
						DWORD dwError = GetLastError();
						CLTRACE(9, "CBadgeIconSelective: IsMemberOf: Error reading. Error: %ld.", dwError);
					}
				}
				else
				{
					DWORD dwError = GetLastError();
					CLTRACE(9, "CBadgeIconSelective: IsMemberOf: ERROR: Writing path.  Error: %ld. BytesWritten: %ld.", dwError, BytesWritten);
				}
			}
			else
			{
				DWORD dwError = GetLastError();
				CLTRACE(9, "CBadgeIconSelective: IsMemberOf: ERROR: Writing packet ID.  Error: %ld. BytesWritten: %ld.", dwError, BytesWritten);
			}
		}
	}
	catch (exception ex)
	{
		CLTRACE(1, "CBadgeIconSelective: IsMemberOf: ERROR: Exception.  Message: %s.", ex.what());
	}

	// Close the pipe handle
    if (PipeHandle != INVALID_HANDLE_VALUE)
	{
		CLTRACE(1, "CBadgeIconFailed: IsMemberOf: Close the pipe handle.");
		CloseHandle(PipeHandle);
		PipeHandle = INVALID_HANDLE_VALUE;
	}

	// clear memory for copied path string
	free(s);

	// return S_FALSE or S_OK for no icon overlay and icon overlay, respectively
	CLTRACE(9, "CBadgeIconSelective: IsMemberOf: Return code: %d.", r);
	return r;
}