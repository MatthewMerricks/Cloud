//
// CExplorerSimulator.h
// Cloud Windows COM
//
// Created By BobS.
// Copyright (c) Cloud.com. All rights reserved.

#pragma once
#include <boost\signal.hpp>
#include <boost\thread.hpp>
#include "Trace.h"

class CExplorerSimulator
{
private:
    // Private fields
    int _nExplorerIndex;											// simulated Explorer index
	int _nBadgeType;
	HANDLE _threadWorkerHandle;
    boost::mutex _locker;
    BOOL _fRequestExit;
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

	BadgeCOMLib::IBadgeIconSyncedPtr _pSynced;
	BadgeCOMLib::IBadgeIconSyncingPtr _pSyncing;
	BadgeCOMLib::IBadgeIconFailedPtr _pFailed;
	BadgeCOMLib::IBadgeIconSelectivePtr _pSelective;

public:
	// Definitions
	enum EnumCloudAppIconBadgeType
	{
		cloudAppBadgeNone=0,
	    cloudAppBadgeSynced,
	    cloudAppBadgeSyncing,
	    cloudAppBadgeFailed,
	    cloudAppBadgeSelective
	} ;

    // Life cycle
    CExplorerSimulator(void);
    ~CExplorerSimulator(void);

    // Methods
	void Initialize(int nSimulatedExplorerIndex, int nBadgeType);
	void Terminate();

	void WorkerThreadProc(LPVOID pUserState);



};

