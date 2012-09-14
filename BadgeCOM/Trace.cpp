#include "stdafx.h"
#include <Windows.h>
#include <stdio.h>
#include "lmcons.h"
#include "Trace.h"
#include "time.h"
#include "limits.h"
#include <string>
using namespace std;

bool Trace::instanceFlag = false;
Trace* Trace::single = NULL;
CRITICAL_SECTION Trace::cs;

//@@@@@@@@@@@@@@@Remove this for release
//#define _DEBUG 1


Trace* Trace::getInstance()
{
    if(! instanceFlag)
    {
        single = new Trace();
        instanceFlag = true;

#ifdef _DEBUG
		// Initialization
		EnterCriticalSection(&Trace::cs);
		single->traceDirectory = "C:\\Trash\\Trace";
		single->maxPriorityToTrace = LONG_MAX;
		int rc = SHCreateDirectoryExA(NULL, single->traceDirectory.c_str(), NULL);
		single->traceEnabled = (rc == ERROR_SUCCESS || rc == ERROR_ALREADY_EXISTS);

		// Get the current time
		time_t rawtime;
		struct tm *timeinfo;

		time(&rawtime);
		timeinfo = localtime(&rawtime);

		char time[200];
		strftime(time, sizeof(time), "%Y-%m-%d", timeinfo);

		// Build the name of the trace file like string.Format(@"\Trace-{0:yyyy-MM-dd}.txt", DateTime.Now);
		char buff[200];
		sprintf(buff, "\\Trace-%s.txt", time);
		single->traceFile = buff;
		LeaveCriticalSection(&Trace::cs);
#endif

        return single;
    }
    else
    {
        return single;
    }
}


void Trace::setDirectory(std::string traceDirectoryParm)
{
	traceDirectory = traceDirectoryParm;
	int rc = SHCreateDirectoryExA(NULL, single->traceDirectory.c_str(), NULL);
	traceEnabled = (rc == ERROR_SUCCESS || rc == ERROR_ALREADY_EXISTS);
}

void Trace::setMaxPriorityToTrace(int priority)
{
	maxPriorityToTrace = priority;
}



void Trace::write(int priority, char *szFormat, ...)
{
	EnterCriticalSection(&Trace::cs);
#ifndef _DEBUG
	return;
#endif
	if (!traceEnabled || priority > maxPriorityToTrace)
	{
		LeaveCriticalSection(&Trace::cs);
		return;
	}

	va_list vl;
	va_start(vl, szFormat);

	// Build the full path of the trace file and open it.  It is a daily file.
	// Allow other processes to append to the file also to intermix the entries.
	std::string fullPathOfTraceFile = traceDirectory +  traceFile;
	traceStream = _fsopen(fullPathOfTraceFile.c_str(), "a", _SH_DENYNO);
	if (traceStream == NULL)
	{
		traceEnabled = false;
		va_end(vl);
		LeaveCriticalSection(&Trace::cs);
		return;
	}

	// Get the thread ID and process ID
	DWORD threadId = GetCurrentThreadId();
	DWORD processId = GetCurrentProcessId();
	
	// Get the current local time
    SYSTEMTIME st, lt;
    GetSystemTime(&st);
    GetLocalTime(&lt);

    // Trace the line prefix
	fprintf(traceStream, "CPP_%04d-%02d-%02d_%02d:%02d:%02d-%03d_P%lx-T%lx_", lt.wYear, lt.wMonth, lt.wDay, lt.wHour, lt.wMinute, lt.wSecond, lt.wMilliseconds, processId, threadId);

	// Trace the message
	vfprintf(traceStream, szFormat, vl);

    // Add a newline
	fprintf(traceStream, "\n");

	// Close the stream to flush the data
	fclose(traceStream);

	va_end(vl);
	LeaveCriticalSection(&Trace::cs);

}


