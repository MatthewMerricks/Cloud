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
    HANDLE _handleIconFailed;
    HANDLE _handleIconSynced;
    HANDLE _handleIconSyncing;
    HANDLE _handleIconSelective;
	HANDLE _threadWorkerHandle;
    boost::mutex _locker;
    BOOL _fRequestExit;
	HRESULT _hr;
	WCHAR _pathIconFileFailed[255];
	WCHAR _pathIconFileSynced[255];
	WCHAR _pathIconFileSyncing[255];
	WCHAR _pathIconFileSelective[255];


public:
    // Life cycle
    CExplorerSimulator(void);
    ~CExplorerSimulator(void);

    // Methods
	void Initialize(int nSimulatedExplorerIndex);
	void Terminate();

	void WorkerThreadProc(LPVOID pUserState);



};

