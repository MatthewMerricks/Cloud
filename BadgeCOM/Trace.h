//
// Trace.h
// Cloud Windows COM
//
// Created By BobS
// Copyright (c) Cloud.com. All rights reserved.

#pragma once
#include <Windows.h>
#include <stdio.h>
#include <vector>
#include <iostream>
#include <fstream>
#include <algorithm>
#include <string>
#include <boost\date_time\gregorian\gregorian_types.hpp>
#include "lmcons.h"
#include "time.h"
#include "limits.h"

using namespace boost::gregorian;

class Trace
{
private:
    // Private constants
    static const int _knTraceLinesBeforeCheckDateChange = 100;
    static const int _knDeleteTraceFilesOlderThanDays = 10;

    // Private static fields
    static bool _fInstanceFlag;
    static Trace *_single;
	static CRITICAL_SECTION _cs;
    volatile static long instanceInitialized;

    // Private instance fields
    std::wstring _wsTraceDirectory;
	FILE *_streamTrace;
	std::wstring _wsTraceFileFullPath;
	int _nMaxPriorityToTrace;
	bool _fTraceEnabled;
    int _nYear;
    int _nMonth;
    int _nDay;
    int _nTraceLineCountdown;
    HANDLE _threadDeleteOldTraceFiles;

    enum STR2INT_ERROR { STR2INT_SUCCESS, STR2INT_OVERFLOW, STR2INT_UNDERFLOW, STR2INT_INCONVERTIBLE };

    Trace()
    {
        // Private constructor
        _streamTrace = NULL;
        _nMaxPriorityToTrace = 0;
        _fTraceEnabled = 0;
        _nYear = 0;
        _nMonth = 0;
        _nDay = 0;
        _nTraceLineCountdown = 0;
        _threadDeleteOldTraceFiles = NULL;
		InitializeCriticalSection(&_cs);
    }

public:
    // Public methods.
    static Trace* getInstance();
    void write(int priority, char *szFormat, ...);
    void DetermineMaximumTraceLevel();
    STR2INT_ERROR wstr2int (int &i, WCHAR const *s, int base = 10);
    void PerhapsChangeTraceFilename();
    static void DeleteOldTraceFilesThreadProc(LPVOID pUserState);
    int GetFileList(std::wstring &wsSearchKey, std::vector<std::wstring> &outList);

    // Public destructor.
    ~Trace()
    {
		if (_fInstanceFlag)
		{
	        _fInstanceFlag = false;
			DeleteCriticalSection(&_cs);
		}
    }
};


