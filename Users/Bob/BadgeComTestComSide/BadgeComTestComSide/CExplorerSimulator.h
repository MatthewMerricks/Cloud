//
// CExplorerSimulator.h
// Cloud Windows COM
//
// Created By BobS.
// Copyright (c) Cloud.com. All rights reserved.

#pragma once

class CExplorerSimulator
{
private:
    // Private fields
    int _nExplorerIndex;											// simulated Explorer index
	int _nBadgeType;
	HANDLE _threadWorkerHandle;
    boost::mutex _locker;
    BOOL _fRequestExit;
	BOOL _fExited;
	BOOL _fInitialized;
	HRESULT _hr;

	WCHAR _pathIconFileFailed[255];
	WCHAR _pathIconFileSynced[255];
	WCHAR _pathIconFileSyncing[255];
	WCHAR _pathIconFileSelective[255];

	int _indexIconFailed;
	int _indexIconSynced;
	int _indexIconSyncing;
	int _indexIconSelective;

	DWORD _flagsIconFailed;
	DWORD _flagsIconSynced;
	DWORD _flagsIconSyncing;
	DWORD _flagsIconSelective;

	int _priorityIconFailed;
	int _priorityIconSynced;
	int _priorityIconSyncing;
	int _priorityIconSelective;

	BadgeCOMLib::IBadgeIconSyncedPtr _pSynced;
	BadgeCOMLib::IBadgeIconSyncingPtr _pSyncing;
	BadgeCOMLib::IBadgeIconFailedPtr _pFailed;
	BadgeCOMLib::IBadgeIconSelectivePtr _pSelective;

private:
	static void WorkerThreadProc(LPVOID pUserState);
	bool randomBool();
	void SendRequestsToIsMemberOf();
	BOOL QueryShouldBadgePath(std::wstring path);

public:
    // Life cycle
    CExplorerSimulator(void);
    ~CExplorerSimulator(void);

    // Methods
	void Initialize(int nSimulatedExplorerIndex, int nBadgeType);
	void Terminate();
};

