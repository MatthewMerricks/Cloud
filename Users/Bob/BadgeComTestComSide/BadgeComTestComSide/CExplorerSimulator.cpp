//
// CExplorerSimulator.cpp
// Cloud Windows COM
//
// Created By BobS.
// Copyright (c) Cloud.com. All rights reserved.

#include "stdafx.h"

// Debug trace
#define CLTRACE(intPriority, szFormat, ...) Trace::getInstance()->write(intPriority, szFormat, __VA_ARGS__)

/// <summary>
/// Constructor
/// </summary>
CExplorerSimulator::CExplorerSimulator(void)
{
    try
    {
        // Initialize private fields
		CLTRACE(9, "CExplorerSimulator: CExplorerSimulator: Entry.");
		_nExplorerIndex = -1;											// simulated Explorer index
		_fRequestExit = false;
		_fExited = false;
		_fInitialized;
		_hr = NULL;

		_pathIconFileFailed[0] = L'\0';
		_pathIconFileSynced[0] = L'\0';
		_pathIconFileSyncing[0] = L'\0';
		_pathIconFileSelective[0] = L'\0';

		_indexIconFailed = -1;
		_indexIconSynced = -1;
		_indexIconSyncing = -1;
		_indexIconSelective = -1;

		_flagsIconFailed = 0;
		_flagsIconSynced = 0;
		_flagsIconSyncing = 0;
		_flagsIconSelective = 0;

		_priorityIconFailed = -1;
		_priorityIconSynced = -1;
		_priorityIconSyncing = -1;
		_priorityIconSelective = -1;

		_pSynced = NULL;
		_pSyncing = NULL;
		_pFailed = NULL;
		_pSelective = NULL;
    }
    catch (const std::exception &ex)
    {
		CLTRACE(1, "CExplorerSimulator: CExplorerSimulator: ERROR: Exception.  Message: %s.", ex.what());
    }
    catch (...)
    {
		CLTRACE(1, "CExplorerSimulator: CExplorerSimulator: ERROR: C++ exception.");
    }
	CLTRACE(9, "CExplorerSimulator: CExplorerSimulator: Exit."); 
}


/// <summary>
/// Destructor
/// </summary>
CExplorerSimulator::~CExplorerSimulator(void)
{
    // Kill threads
    try
    {
		CLTRACE(9, "CExplorerSimulator: ~CExplorerSimulator: Entry.");
		Terminate();
    }
    catch (const std::exception &ex)
    {
		CLTRACE(1, "CExplorerSimulator: ~CExplorerSimulator: ERROR: Exception. Killing threads. Message: %s.", ex.what());
    }
    catch (...)
    {
		CLTRACE(1, "CExplorerSimulator: ~CExplorerSimulator: ERROR: C++ exception.");
    }
}

/// <summary>
/// Initialize.  Initialize the PubSubEventsServer.
/// </summary>
void CExplorerSimulator::Initialize(int nSimulatedExplorerIndex, int nBadgeType)
{
	BOOL fLockHeld = false;

    try
    {
		CLTRACE(9, "CExplorerSimulator: Initialize: Entry. Index: %d.", nSimulatedExplorerIndex);

		if (nSimulatedExplorerIndex < 0)
		{
			throw std::exception("Invalid explorer index");
		}
		if (nBadgeType < cloudAppBadgeSynced || nBadgeType > cloudAppBadgeSelective)
		{
			throw std::exception("Invalid badge type");
		}


		CLTRACE(9, "CExplorerSimulator: StartSubscribingThread: Entry.");
        _locker.lock();
        {
			// Set the parameters for this instance.
			_nExplorerIndex = nSimulatedExplorerIndex;
			_nBadgeType = nBadgeType;

            // Start a thread to simulate an individual Explorer thread.
			fLockHeld = true;
            _fRequestExit = false;

            // Start a thread to simulate Explorer working with BadgeCom for this particular badge type.
            DWORD dwThreadId;
            HANDLE handle = CreateThread(NULL,                              // default security
                        0,                                                  // default stack size
                        (LPTHREAD_START_ROUTINE)&WorkerThreadProc,          // function to run
                        (LPVOID) this,                                      // thread parameter
                        0,                                                  // imediately run the thread
                        &dwThreadId                                         // output thread ID
                        );
            if (handle == NULL)
            {
                throw std::exception("Error creating thread");
            }

            _threadWorkerHandle = handle;
        }
        _locker.unlock();
		fLockHeld = false;


    }
    catch (const std::exception &ex)
    {
		if (fLockHeld)
		{
			_locker.unlock();
			fLockHeld = false;
		}
		CLTRACE(1, "CExplorerSimulator: Initialize: ERROR: Exception.  Message: %s.", ex.what());
    }
    catch (...)
    {
		if (fLockHeld)
		{
			_locker.unlock();
			fLockHeld = false;
		}
		CLTRACE(1, "CExplorerSimulator: Initialize: ERROR: C++ exception.");
    }
}

/// <summary>
/// The subscribing thread may get stuck waiting on an event if the BadgeCom process is killed.  Monitor the subscribing thread for activity.
/// If no activity is detected, kill the subscribing thread and attempt to restart it.
/// </summary>
void CExplorerSimulator::WorkerThreadProc(LPVOID pUserState)
{
	if (pUserState == NULL)
	{
    	CLTRACE(1, "CExplorerSimulator: WorkerThreadProc: ERROR: User state is NULL.");
		throw std::exception("pUserState must not be NULL");
	}

    // Cast the user state to an object instance
    CExplorerSimulator *pThis = (CExplorerSimulator *)pUserState;

    try
    {
		// Initialize the COM system
        pThis->_hr = CoInitializeEx(NULL, COINIT_MULTITHREADED);
		if (pThis->_hr != S_OK)
		{
			CLTRACE(9, "CExplorerSimulator: CExplorerSimulator: ERROR: CoInitialize returned %d.", pThis->_hr);
			return;
		}

		// Initialize to the interface
		switch (pThis->_nBadgeType)
		{
			case cloudAppBadgeSynced:
				// Get the path to the icon resource assembly, the icon index and the icon flags, and initialize BadgeCom.
				pThis->_pSynced = new BadgeCOMLib::IBadgeIconSyncedPtr(__uuidof(BadgeCOMLib::BadgeIconSynced));
				pThis->_hr = pThis->_pSynced->GetOverlayInfo(pThis->_pathIconFileSynced, 0, &(pThis->_indexIconSynced), &(pThis->_flagsIconSynced));
				if (pThis->_hr != S_OK)
				{
					CLTRACE(9, "CExplorerSimulator: WorkerThreadProc: ERROR: Synced GetOverlayInfo returned %d.", pThis->_hr);
					throw std::exception("Error from Synced GetOverlayInfo");
				}
				if (pThis->_indexIconSynced != 1)
				{
					CLTRACE(9, "CExplorerSimulator: WorkerThreadProc: ERROR: Synced GetOverlayInfo returned icon index %d.", pThis->_indexIconSynced);
					throw std::exception("Bad icon index from Synced GetOverlayInfo");
				}
				if (pThis->_flagsIconSynced != 3)
				{
					CLTRACE(9, "CExplorerSimulator: WorkerThreadProc: ERROR: Synced GetOverlayInfo returned icon flags %d.", pThis->_flagsIconSynced);
					throw std::exception("Bad icon flags from Synced GetOverlayInfo");
				}

				// Get the icon priority.
				pThis->_hr = pThis->_pSynced->GetPriority(&(pThis->_priorityIconSynced));
				if (pThis->_hr != S_OK)
				{
					CLTRACE(9, "CExplorerSimulator: WorkerThreadProc: ERROR: Synced GetPriority returned %d.", pThis->_hr);
					throw std::exception("Error from Synced GetPriority");
				}
				if (pThis->_priorityIconSynced != 0)
				{
					CLTRACE(9, "CExplorerSimulator: WorkerThreadProc: ERROR: Synced GetPriority returned icon priority %d.", pThis->_priorityIconSynced);
					throw std::exception("Bad icon priority from Synced GetPriority");
				}
				break;

			case cloudAppBadgeSyncing:
				// Get the path to the icon resource assembly, the icon index and the icon flags, and initialize BadgeCom.
				pThis->_pSyncing = new BadgeCOMLib::IBadgeIconSyncingPtr(__uuidof(BadgeCOMLib::BadgeIconSyncing));
				pThis->_hr = pThis->_pSyncing->GetOverlayInfo(pThis->_pathIconFileSyncing, 0, &(pThis->_indexIconSyncing), &(pThis->_flagsIconSyncing));
				if (pThis->_hr != S_OK)
				{
					CLTRACE(9, "CExplorerSimulator: WorkerThreadProc: ERROR: Syncing GetOverlayInfo returned %d.", pThis->_hr);
					throw std::exception("Error from Syncing GetOverlayInfo");
				}
				if (pThis->_indexIconSyncing != 0)
				{
					CLTRACE(9, "CExplorerSimulator: WorkerThreadProc: ERROR: Syncing GetOverlayInfo returned icon index %d.", pThis->_indexIconSyncing);
					throw std::exception("Bad icon index from Syncing GetOverlayInfo");
				}
				if (pThis->_flagsIconSyncing != 3)
				{
					CLTRACE(9, "CExplorerSimulator: WorkerThreadProc: ERROR: Syncing GetOverlayInfo returned icon flags %d.", pThis->_flagsIconSyncing);
					throw std::exception("Bad icon flags from Syncing GetOverlayInfo");
				}

				// Get the icon priority.
				pThis->_hr = pThis->_pSyncing->GetPriority(&(pThis->_priorityIconSyncing));
				if (pThis->_hr != S_OK)
				{
					CLTRACE(9, "CExplorerSimulator: WorkerThreadProc: ERROR: Syncing GetPriority returned %d.", pThis->_hr);
					throw std::exception("Error from Syncing GetPriority");
				}
				if (pThis->_priorityIconSyncing != 0)
				{
					CLTRACE(9, "CExplorerSimulator: WorkerThreadProc: ERROR: Syncing GetPriority returned icon priority %d.", pThis->_priorityIconSyncing);
					throw std::exception("Bad icon priority from Syncing GetPriority");
				}
				break;

			case cloudAppBadgeFailed:
				// Get the path to the icon resource assembly, the icon index and the icon flags, and initialize BadgeCom.
				pThis->_pFailed = new BadgeCOMLib::IBadgeIconFailedPtr(__uuidof(BadgeCOMLib::BadgeIconFailed));
				pThis->_hr = pThis->_pFailed->GetOverlayInfo(pThis->_pathIconFileFailed, 0, &(pThis->_indexIconFailed), &(pThis->_flagsIconFailed));
				if (pThis->_hr != S_OK)
				{
					CLTRACE(9, "CExplorerSimulator: WorkerThreadProc: ERROR: Failed GetOverlayInfo returned %d.", pThis->_hr);
					throw std::exception("Error from Failed GetOverlayInfo");
				}
				if (pThis->_indexIconFailed != 3)
				{
					CLTRACE(9, "CExplorerSimulator: WorkerThreadProc: ERROR: Failed GetOverlayInfo returned icon index %d.", pThis->_indexIconFailed);
					throw std::exception("Bad icon index from Failed GetOverlayInfo");
				}
				if (pThis->_flagsIconFailed != 3)
				{
					CLTRACE(9, "CExplorerSimulator: WorkerThreadProc: ERROR: Failed GetOverlayInfo returned icon flags %d.", pThis->_flagsIconFailed);
					throw std::exception("Bad icon flags from Failed GetOverlayInfo");
				}

				// Get the icon priority.
				pThis->_hr = pThis->_pFailed->GetPriority(&(pThis->_priorityIconFailed));
				if (pThis->_hr != S_OK)
				{
					CLTRACE(9, "CExplorerSimulator: WorkerThreadProc: ERROR: Failed GetPriority returned %d.", pThis->_hr);
					throw std::exception("Error from Failed GetPriority");
				}
				if (pThis->_priorityIconFailed != 0)
				{
					CLTRACE(9, "CExplorerSimulator: WorkerThreadProc: ERROR: Failed GetPriority returned icon priority %d.", pThis->_priorityIconFailed);
					throw std::exception("Bad icon priority from Failed GetPriority");
				}
				break;

			case cloudAppBadgeSelective:
				// Get the path to the icon resource assembly, the icon index and the icon flags, and initialize BadgeCom.
				pThis->_pSelective = new BadgeCOMLib::IBadgeIconSelectivePtr(__uuidof(BadgeCOMLib::BadgeIconSelective));
				pThis->_hr = pThis->_pSelective->GetOverlayInfo(pThis->_pathIconFileSelective, 0, &(pThis->_indexIconSelective), &(pThis->_flagsIconSelective));
				if (pThis->_hr != S_OK)
				{
					CLTRACE(9, "CExplorerSimulator: WorkerThreadProc: ERROR: Selective GetOverlayInfo returned %d.", pThis->_hr);
					throw std::exception("Error from Selective GetOverlayInfo");
				}
				if (pThis->_indexIconSelective != 2)
				{
					CLTRACE(9, "CExplorerSimulator: WorkerThreadProc: ERROR: Selective GetOverlayInfo returned icon index %d.", pThis->_indexIconSelective);
					throw std::exception("Bad icon index from Selective GetOverlayInfo");
				}
				if (pThis->_flagsIconSelective != 3)
				{
					CLTRACE(9, "CExplorerSimulator: WorkerThreadProc: ERROR: Selective GetOverlayInfo returned icon flags %d.", pThis->_flagsIconSelective);
					throw std::exception("Bad icon flags from Selective GetOverlayInfo");
				}

				// Get the icon priority.
				pThis->_hr = pThis->_pSelective->GetPriority(&(pThis->_priorityIconSelective));
				if (pThis->_hr != S_OK)
				{
					CLTRACE(9, "CExplorerSimulator: WorkerThreadProc: ERROR: Selective GetPriority returned %d.", pThis->_hr);
					throw std::exception("Error from Selective GetPriority");
				}
				if (pThis->_priorityIconSelective != 0)
				{
					CLTRACE(9, "CExplorerSimulator: WorkerThreadProc: ERROR: Selective GetPriority returned icon priority %d.", pThis->_priorityIconSelective);
					throw std::exception("Bad icon priority from Selective GetPriority");
				}
				break;
		}

		// Initialized now.  Start querying BadgeCom for files that should be badged.
		while (!pThis->_fRequestExit)
		{
			// Send 10 requests for this badge type rapidly, all from the same folder.
			pThis->SendRequestsToIsMemberOf();

			Sleep(200);
		}
    }
    catch (const std::exception &ex)
    {
		CLTRACE(1, "CExplorerSimulator: WorkerThreadProc: ERROR: Exception.  Message: %s.", ex.what());
    }
    catch (...)
    {
		CLTRACE(1, "CExplorerSimulator: WorkerThreadProc: ERROR: C++ exception.");
    }

	CLTRACE(9, "CExplorerSimulator: WorkerThreadProc: Thread exit.");
	pThis->_fExited = true;
}

/// <summary>
/// Initialize.  Initialize the PubSubEventsServer.
/// </summary>
void CExplorerSimulator::Terminate()
{
	_fRequestExit = true;

	// Wait for this thread to exit.
	while (!_fExited)
	{
		Sleep(0);
	}
}

bool CExplorerSimulator::randomBool() {
  return rand() % 2 == 1;
}

void CExplorerSimulator::SendRequestsToIsMemberOf()
{

	// Choose in/out of the syncbox, and each of the levels randomly.
	bool fInSyncbox = randomBool();
	int index1 = rand() % nMaxItemsAtLevel;
	int index2 = rand() % nMaxItemsAtLevel;
	int index3 = rand() % nMaxItemsAtLevel;

	// Send the queries.
	std::wstring path;
	for (int indexPath = 0; indexPath < nMaxItemsAtLevel; indexPath++)
	{
		// Get the path.
		if (fInSyncbox)
		{
			path = pathsInSyncbox[index1][index2][index3];
		}
		else
		{
			path = pathsOutOfSyncbox[index1][index2][index3];
		}

		// Queery this path.
		BOOL fShouldBadgePath = QueryShouldBadgePath(path);

		// bump the lowest level index from the starting point with wrapping.
		index3++;
		if (index3 >= nMaxItemsAtLevel)
		{
			index3 = 0;
		}
	}

}

BOOL CExplorerSimulator::QueryShouldBadgePath(std::wstring path)
{
	// Query the IsMemberOf member of the interface.  It returns S_OK to badge the icon, or S_FALSE for no badge.
	switch (_nBadgeType)
	{
		case cloudAppBadgeSynced:
			_hr = _pSynced->IsMemberOf((LPWSTR)path.c_str(), 0);
			if (_hr != S_OK && _hr != S_FALSE)
			{
				CLTRACE(9, "CExplorerSimulator: QueryShouldBadgePath: ERROR: Synced IsMemberOf returned %d.", _hr);
				throw std::exception("Error from Synced IsMemberOf");
			}
			if (_hr == S_OK)
			{
				CLTRACE(9, "CExplorerSimulator: QueryShouldBadgePath: BadgeIt Synced.  Path: %s.", path.c_str());
			}
			break;

		case cloudAppBadgeSyncing:
			_hr = _pSyncing->IsMemberOf((LPWSTR)path.c_str(), 0);
			if (_hr != S_OK && _hr != S_FALSE)
			{
				CLTRACE(9, "CExplorerSimulator: QueryShouldBadgePath: ERROR: Syncing IsMemberOf returned %d.", _hr);
				throw std::exception("Error from Syncing IsMemberOf");
			}
			if (_hr == S_OK)
			{
				CLTRACE(9, "CExplorerSimulator: QueryShouldBadgePath: BadgeIt Syncing.  Path: %s.", path.c_str());
			}
			break;

		case cloudAppBadgeFailed:
			_hr = _pFailed->IsMemberOf((LPWSTR)path.c_str(), 0);
			if (_hr != S_OK && _hr != S_FALSE)
			{
				CLTRACE(9, "CExplorerSimulator: QueryShouldBadgePath: ERROR: Failed IsMemberOf returned %d.", _hr);
				throw std::exception("Error from Failed IsMemberOf");
			}
			if (_hr == S_OK)
			{
				CLTRACE(9, "CExplorerSimulator: QueryShouldBadgePath: BadgeIt Failed.  Path: %s.", path.c_str());
			}
			break;

		case cloudAppBadgeSelective:
			_hr = _pSelective->IsMemberOf((LPWSTR)path.c_str(), 0);
			if (_hr != S_OK && _hr != S_FALSE)
			{
				CLTRACE(9, "CExplorerSimulator: QueryShouldBadgePath: ERROR: Selective IsMemberOf returned %d.", _hr);
				throw std::exception("Error from Selective IsMemberOf");
			}
			if (_hr == S_OK)
			{
				CLTRACE(9, "CExplorerSimulator: QueryShouldBadgePath: BadgeIt Selective.  Path: %s.", path.c_str());
			}
			break;
	}

	return _hr == S_OK ? true : false;
}
