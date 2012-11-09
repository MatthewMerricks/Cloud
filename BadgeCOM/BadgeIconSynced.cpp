//
// BadgeIconSynced.cpp
// Cloud Windows COM
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

// BadgeIconSynced.cpp : Implementation of CBadgeIconSynced

#include "stdafx.h"
#include "BadgeIconSynced.h"
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
	//#define CLTRACE(intPriority, szFormat, ...) Trace::getInstance()->write(intPriority, szFormat, __VA_ARGS__)
#endif // _DEBUG

// CBadgeIconSynced

// IShellIconOverlayIdentifier::GetOverlayInfo
// returns The Overlay Icon Location to the system
// By debugging I found it only runs once when the com object is loaded to the system, thus it cannot return a dynamic icon
STDMETHODIMP CBadgeIconSynced::GetOverlayInfo(
	LPWSTR pwszIconFile,
	int cchMax,
	int* pIndex,
	DWORD* pdwFlags)
{
    try
    {
	    // Get our module's full path
	    CLTRACE(9, "CBadgeIconSynced: GetOverlayInfo: Entry");
	    GetModuleFileNameW(_AtlBaseModule.GetModuleInstance(), pwszIconFile, cchMax);

	    // Use second icon in the resource (Synced.ico)
	    *pIndex = 1;

	    *pdwFlags = ISIOI_ICONFILE | ISIOI_ICONINDEX;

        // Allocate the PubSubEvents system, subscribe to events, and send an initialization event to BadgeNet.
        InitializeBadgeNetPubSubEvents();
    }
    catch(std::exception &ex)
    {
		CLTRACE(1, "CBadgeIconSynced: GetOverlayInfo: ERROR: Exception.  Message: %s.", ex.what());
    }
	return S_OK;
}

// IShellIconOverlayIdentifier::GetPriority
// returns the priority of this overlay 0 being the highest.
STDMETHODIMP CBadgeIconSynced::GetPriority(int* pPriority)
{
	CLTRACE(9, "CBadgeIconSynced: GetPriority: Entry");
	// change the following to set priority between multiple overlays
	*pPriority = 0;
	return S_OK;
}

typedef HRESULT (WINAPI*pfnGetDispName)(LPCWSTR, LPWSTR, DWORD);

// IShellIconOverlayIdentifier::IsMemberOf
// Returns whether the object should have this overlay or not 
STDMETHODIMP CBadgeIconSynced::IsMemberOf(LPCWSTR pwszPath, DWORD dwAttrib)
{
	//default return value is false (no icon overlay)
	HRESULT result = S_FALSE;   // or S_OK for icon overlay

	//copy input path to local unicode char
	wchar_t *s = _wcsdup(pwszPath);

    // Should this path be badged?  It will be badged if the root of the parameter path is found in
    // the SyncBox folder path dictionary, and if the actual path is found in the badging dictionary,
    // and if the current badgeType value matches this icon handler.

    return result;
}

//// IShellIconOverlayIdentifier::IsMemberOf
//// Returns whether the object should have this overlay or not 
//STDMETHODIMP CBadgeIconSynced::IsMemberOf(LPCWSTR pwszPath, DWORD dwAttrib)
//{
//	//default return value is false (no icon overlay)
//	HRESULT r = S_FALSE;
//	HANDLE PipeHandle = INVALID_HANDLE_VALUE;
//	int createRetryCount = 3;
//
//	//copy input path to local unicode char
//	wchar_t *s = _wcsdup(pwszPath);
//	try
//	{
//		CLTRACE(9, "CBadgeIconSynced: IsMemberOf: Entry. Path: %ls, Attrib: %lx.", pwszPath, dwAttrib);
//		//store length of copied path
//		int sLength = wcslen(s);
//
//		//Declare some variables
//		DWORD BytesWritten;
//		DWORD BytesRead;
//		bool pipeConnectionFailed = false;
//
//		// Get the user name of the logged-in user.
//		wchar_t lpszUsername[UNLEN];
//		DWORD dUsername = sizeof(lpszUsername);
//		if(!GetUserName(lpszUsername, &dUsername))
//		{
//			CLTRACE(9, "CBadgeIconSynced: IsMemberOf: ERROR: From GetUserName.");
//			return r;
//		}
//
//		// Build the pipe name.  This will be (no escapes): "\\.\Pipe\<UserName>/BadgeCOMcloudAppBadgeSynced"
//		std::wstring pipeForCurrentBadgeType = L"\\\\.\\Pipe\\";
//		pipeForCurrentBadgeType.append(lpszUsername);
//		pipeForCurrentBadgeType.append(L"/BadgeCOMcloudAppBadgeSynced");
//
//		// Loop until we get a connection that can be used.
//		while (true)
//		{
//		    CLTRACE(9, "CBadgeIconSynced: IsMemberOf: Open the pipe for writing and reading.");
//		    PipeHandle = CreateFile(
//		    	pipeForCurrentBadgeType.c_str(), // Pipe name
//		    	GENERIC_WRITE | GENERIC_READ, // bidirectional
//		    	0, // No sharing
//		    	NULL, // Default security attributes
//		    	OPEN_EXISTING, // Opens existing pipe
//		    	0, // Default attributes
//		    	NULL // No template file
//		    	);
//
//		    // If the pipe handle is opened successfully then break out to continue
//		    if (PipeHandle != INVALID_HANDLE_VALUE)
//		    {
//		    	CLTRACE(9, "CBadgeIconSynced: IsMemberOf: Created the write pipe.");
//				break;
//		    }
//		    // Pipe not successful, find out if it should try again
//		    else
//		    {
//		    	// store not successful reason
//		    	DWORD dwError = GetLastError();
//		    	CLTRACE(9, "CBadgeIconSynced: IsMemberOf: ERROR: Write pipe not opened.  Code: %lx.", dwError);
//
//		    	// Exit if an error other than ERROR_PIPE_BUSY occurs (by setting pipeConnectionFailed to true)
//		    	// This is the normal path when the application is not running (dwError will equal ERROR_FILE_NOT_FOUND)
//		    	if (ERROR_PIPE_BUSY != dwError)
//		    	{
//		    		CLTRACE(9, "CBadgeIconSynced: IsMemberOf: Error is not pipe_busy.");
//		    		pipeConnectionFailed = true;
//					break;
//		    	}
//		    	// pipe is busy
//		    	else
//		    	{
//		    		CLTRACE(9, "CBadgeIconSynced: IsMemberOf: Error is pipe_busy. Wait for it for 2 seconds.");
//		    		// if waiting for a pipe does not complete in 2 seconds, exit  (by setting pipeConnectionFailed to true)
//		    		if (!WaitNamedPipe(pipeForCurrentBadgeType.c_str(), 2000))
//		    		{
//		    			dwError = GetLastError();
//		    			CLTRACE(9, "CBadgeIconSynced: IsMemberOf: ERROR: after wait.  Code: %lx.  Maybe we should retry.", dwError);
//						if (createRetryCount-- > 0)
//						{
//							// We should retry
//							CLTRACE(9, "CBadgeIconSynced: IsMemberOf: Loop to retry the CreateFile.");
//							CloseHandle(PipeHandle);
//							PipeHandle = INVALID_HANDLE_VALUE;
//						}
//						else
//						{
//							CLTRACE(9, "CBadgeIconSynced: IsMemberOf: ERROR: Out of retries.  CreateFile failed.");
//			    			pipeConnectionFailed = true;
//							break;
//						}
//
//		    		}
//					else
//					{
//						// The wait succeeded.  We should retry the CreateFile
//						CLTRACE(9, "CBadgeIconSynced: IsMemberOf: Wait successful.  Loop to retry the CreateFile.");
//						CloseHandle(PipeHandle);
//						PipeHandle = INVALID_HANDLE_VALUE;
//					}
//		    	}
//		    }
//		}
//		
//		// if pipe connection did not fail begin pipe transfer logic
//		CLTRACE(9, "CBadgeIconSynced: IsMemberOf: Continue.  pipeConnectionFailed: %d.", pipeConnectionFailed);
//		if (!pipeConnectionFailed)
//		{
//			CLTRACE(9, "CBadgeIconSynced: IsMemberOf: Write pipe is open.");
//			// calculate bytes required to send filepath
//			unsigned int pathLength = sizeof(wchar_t)*sLength;
//			// unique id for each connection attempt (used for unique return pipe)
//			volatile static unsigned long pipeCounter = 0;
//			long localPipeCounter = InterlockedIncrement(&pipeCounter);
//
//			// store pipeCounter as string
//			stringstream packetIdStream;
//			packetIdStream << localPipeCounter;
//			CLTRACE(9, "CBadgeIconSynced: IsMemberOf: Pipe counter is %lu.", localPipeCounter);
//
//			//need to zero-pad packetId to ensure constant length of 10 chars
//			std::string packetId;
//			packetIdStream >> packetId;
//			int startLength = packetId.length();
//			for (int currentPaddedChar = 0; currentPaddedChar < 10 - startLength; currentPaddedChar++)
//				packetId = "0" + packetId;
//
//			// store filepath byte length as string
//			stringstream currentPathLength;
//			currentPathLength << pathLength;
//
//			//need to zero-pad pathLength to ensure constant length of 10 chars
//			std::string paddedLength;
//			currentPathLength >> paddedLength;
//			startLength = paddedLength.length();
//			for (int currentPaddedChar = 0; currentPaddedChar < 10 - startLength; currentPaddedChar++)
//			{
//				paddedLength = "0" + paddedLength;
//			}
//		
//			// write packetId + filepath byte length to pipe (must be 20 bytes)
//			if (WriteFile(PipeHandle,
//				(packetId + paddedLength).c_str(),
//				20,//needs to be 20 since that's what's being read from the other end
//				&BytesWritten,
//				NULL) != 0)
//			{
//				CLTRACE(9, "CBadgeIconSynced: IsMemberOf: Wrote the pipe counter.");
//				// write filepath to pipe (variable bytes)
//				if (WriteFile(PipeHandle,
//					s, //filepath
//					pathLength, //length of filepath
//					&BytesWritten,
//					NULL) != 0)
//				{
//					// Done writing.  Now read the result (badge or don't).
//					CLTRACE(9, "CBadgeIconSynced: IsMemberOf: Read the result.");
//					byte byBuf;
//					bool fSuccess = ReadFile( 
//								PipeHandle,		// pipe handle 
//								&byBuf,			// buffer to receive reply 
//								1,				// size of buffer 
//								&BytesRead,		// number of bytes read 
//								NULL);			// not overlapped 
//					if (fSuccess)
//					{
//						// We read a byte from the channel.  What was it?
//						CLTRACE(9, "CBadgeIconSynced: IsMemberOf: Read success.  BytesRead: %ld.", BytesRead);
//						if (byBuf == 1)
//						{
//							CLTRACE(9, "CBadgeIconSynced: IsMemberOf: Set return code to badge the icon.");
//							r = S_OK;
//						}
//					}
//					else
//					{
//						// Error reading.
//						DWORD dwError = GetLastError();
//						CLTRACE(9, "CBadgeIconSynced: IsMemberOf: Error reading. Error: %ld.", dwError);
//					}
//				}
//				else
//				{
//					DWORD dwError = GetLastError();
//					CLTRACE(9, "CBadgeIconSynced: IsMemberOf: ERROR: Writing path.  Error: %ld. BytesWritten: %ld.", dwError, BytesWritten);
//				}
//			}
//			else
//			{
//				DWORD dwError = GetLastError();
//				CLTRACE(9, "CBadgeIconSynced: IsMemberOf: ERROR: Writing packet ID.  Error: %ld. BytesWritten: %ld.", dwError, BytesWritten);
//			}
//		}
//	}
//	catch (exception ex)
//	{
//		CLTRACE(1, "CBadgeIconSynced: IsMemberOf: ERROR: Exception.  Message: %s.", ex.what());
//	}
//
//	// Close the pipe handle
//    if (PipeHandle != INVALID_HANDLE_VALUE)
//	{
//		CLTRACE(1, "CBadgeIconFailed: IsMemberOf: Close the pipe handle.");
//		CloseHandle(PipeHandle);
//		PipeHandle = INVALID_HANDLE_VALUE;
//	}
//
//	// clear memory for copied path string
//	free(s);
//
//	// return S_FALSE or S_OK for no icon overlay and icon overlay, respectively
//	CLTRACE(9, "CBadgeIconSynced: IsMemberOf: Return code: %d.", r);
//	return r;
//}



/// <summary>
/// We received a badging event from BadgeNet.  This may be a new path, or it may change the badge type
/// for an existing path.
/// </summary>
/// <param name="fullPath">The full path of the item being added.</param>
/// <param name="badgeType">The type of the badge.</param>
void CBadgeIconSynced::OnEventAddBadgePath(BSTR fullPath, EnumCloudAppIconBadgeType badgeType)
{
    try
    {
        // Add or update the <path,badgeType>
	    CLTRACE(9, "CBadgeIconSynced: OnEventAddBadgePath: Entry. Path: <%ls>.", fullPath);
        _mapBadges[fullPath] = badgeType;
    }
    catch(std::exception &ex)
    {
		CLTRACE(1, "CBadgeIconSynced: OnEventAddBadgePath: ERROR: Exception.  Message: %s.", ex.what());
    }
}

/// <summary>
/// We received a request to remove a badging path from BadgeNet.  There will be no error if it doesn't exist.
/// </summary>
/// <param name="fullPath">The full path of the item being removed.</param>
void CBadgeIconSynced::OnEventRemoveBadgePath(BSTR fullPath)
{
    try
    {
        // Remove the item with key fullPath.
	    CLTRACE(9, "CBadgeIconSynced: OnEventRemoveBadgePath: Entry. Path: <%ls>.", fullPath);
        _mapBadges.erase(fullPath);
    }
    catch(std::exception &ex)
    {
		CLTRACE(1, "CBadgeIconSynced: OnEventRemoveBadgePath: ERROR: Exception.  Message: %s.", ex.what());
    }
}

/// <summary>
/// We received a request from BadgeNet to start tracking a new SyncBox folder path.
/// There will be no error if we are already tracking that path.
/// </summary>
/// <param name="fullPath">The full path of the folder being added.</param>
void CBadgeIconSynced::OnEventAddSyncBoxFolderPath(BSTR fullPath)
{
    try
    {
        // Add or update the fullPath.  The value is not used.
	    CLTRACE(9, "CBadgeIconSynced: OnEventAddSyncBoxFolderPath: Entry. Path: <%ls>.", fullPath);
        _mapBadges[fullPath] = cloudAppBadgeNone;
    }
    catch(std::exception &ex)
    {
		CLTRACE(1, "CBadgeIconSynced: OnEventAddSyncBoxFolderPath: ERROR: Exception.  Message: %s.", ex.what());
    }
}

/// <summary>
/// We received a request from BadgeNet to stop tracking a SyncBox folder path.
/// There will be no error if we are already not tracking that path.
/// </summary>
/// <param name="fullPath">The full path of the folder being removed.</param>
void CBadgeIconSynced::OnEventRemoveSyncBoxFolderPath(BSTR fullPath)
{
    try
    {
        // Remove the item with key fullPath.
	    CLTRACE(9, "CBadgeIconSynced: OnEventRemoveSyncBoxFolderPath: Entry. Path: <%ls>.", fullPath);
        _mapBadges.erase(fullPath);

        // Delete all of the keys in the badging dictionary that have this folder path as a root.
        for (boost::unordered_map<std::wstring, EnumCloudAppIconBadgeType>::iterator it = _mapBadges.begin(); it != _mapBadges.end();  /* bump in body of code */)
        {
            if (IsPathInRootPath(it->first, fullPath))
            {
                it = _mapBadges.erase(it);
            }
            else
            {
                it++;
            }
        }
    }
    catch(std::exception &ex)
    {
		CLTRACE(1, "CBadgeIconSynced: OnEventRemoveSyncBoxFolderPath: ERROR: Exception.  Message: %s.", ex.what());
    }
}

/// <summary>
/// We received an error from the PubSubServer watcher.  We are no longer subscribed to badging events.
/// </summary>
/// <param name="fullPath">The full path of the folder being removed.</param>
void CBadgeIconSynced::OnEventSubscriptionWatcherFailed()
{
    try
    {
        // Restart the CBadgeNetPubSubEvents class, but not here because this event was fired by that
        // class.  Start a single-fire timer and do it in the timer event.
	    CLTRACE(9, "CBadgeIconSynced: OnEventSubscriptionWatcherFailed: Entry.  ERROR: Badging failed.");
        _delayedMethodTimer.SetTimedEvent(this, &CBadgeIconSynced::OnDelayedEvent);
        _delayedMethodTimer.Start(100 /* start after ms delay */, false /* don't start immediately */, true /* run once */);
    }
    catch (std::exception &ex)
    {
		CLTRACE(1, "CBadgeIconSynced: OnEventSubscriptionWatcherFailed: ERROR: Exception.  Message: %s.", ex.what());
    }
}

void CBadgeIconSynced::OnDelayedEvent()
{
    try
    {
        // We lost the badging connection.  Empty the dictionaries.  They will be rebuilt if we can get another connection.
        _mapBadges.clear();
        _mapSyncBoxPaths.clear();

        // Restart the CBadgeNetPubSubEvents class.
	    CLTRACE(9, "CBadgeIconSynced: OnDelayedEvent: Entry.");
        if (_pBadgeNetPubSubEvents != NULL)
        {
            // Kill the BadgeNetPubSubEvents threads and free resources.
            _pBadgeNetPubSubEvents->~CBadgeNetPubSubEvents();
            _pBadgeNetPubSubEvents = NULL;

            // Restart the BadgeNetPubSubEvents object
            InitializeBadgeNetPubSubEvents();
        }
    }
    catch (std::exception &ex)
    {
		CLTRACE(1, "CBadgeIconSynced: OnDelayedEvent: ERROR: Exception.  Message: %s.", ex.what());
    }
}

/// <summary>
/// Determines whether a path contains a root path.
/// </summary>
bool CBadgeIconSynced::IsPathInRootPath(std::wstring testPath, std::wstring rootPath)
{
    if (NormalizePath(testPath).compare(0, rootPath.size(), NormalizePath(rootPath)) == 0)
    {
        return true;
    }
    else
    {
        return false;
    }
}

/// <summary>
/// Normalize a path string to the Windows standard format.
/// </summary>
std::wstring CBadgeIconSynced::NormalizePath(std::wstring inPath)
{
    boost::filesystem::path p(inPath);
    return p.make_preferred().wstring();
}

void CBadgeIconSynced::InitializeBadgeNetPubSubEvents()
{
    try
    {
	    CLTRACE(9, "CBadgeIconSynced: InitializeBadgeNetPubSubEvents: Entry.");
        _pBadgeNetPubSubEvents = new CBadgeNetPubSubEvents();

        // Hook up events.
        _pBadgeNetPubSubEvents->FireEventAddBadgePath.connect(boost::bind(&CBadgeIconSynced::OnEventAddBadgePath, this));
        _pBadgeNetPubSubEvents->FireEventRemoveBadgePath.connect(boost::bind(&CBadgeIconSynced::OnEventRemoveBadgePath, this));
        _pBadgeNetPubSubEvents->FireEventAddSyncBoxFolderPath.connect(boost::bind(&CBadgeIconSynced::OnEventAddSyncBoxFolderPath, this));
        _pBadgeNetPubSubEvents->FireEventRemoveSyncBoxFolderPath.connect(boost::bind(&CBadgeIconSynced::OnEventRemoveSyncBoxFolderPath, this));
        _pBadgeNetPubSubEvents->FireEventSubscriptionWatcherFailed.connect(boost::bind(&CBadgeIconSynced::OnEventSubscriptionWatcherFailed, this));

        // Tell BadgeNet we just initialized.
        _pBadgeNetPubSubEvents->PublishEventToBadgeCom(BadgeCom_To_BadgeNet, BadgeCom_Initialization, cloudAppBadgeNone /* not used */, NULL /* not used */);
    }
    catch(std::exception &ex)
    {
		CLTRACE(1, "CBadgeIconSynced: InitializeBadgeNetPubSubEvents: ERROR: Exception.  Message: %s.", ex.what());
    }
}

