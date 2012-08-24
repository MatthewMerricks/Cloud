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
	if (priority > maxPriorityToTrace)
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
		va_end(vl);
		LeaveCriticalSection(&Trace::cs);
		return;
	}

	// Format the header of this line.
	time_t rawtime;
	struct tm *timeinfo;

	time(&rawtime);
	timeinfo = localtime(&rawtime);

	char time[200];
	strftime(time, sizeof(time), "%Y-%m-%d_%H-%M-%S-", timeinfo);

    // Trace the line prefix
	fprintf(traceStream, "CPP_%s_", time);

	// Trace the message
	vfprintf(traceStream, szFormat, vl);

    // Add a newline
	fprintf(traceStream, "\n");

	// Close the stream to flush the data
	fclose(traceStream);

	va_end(vl);
	LeaveCriticalSection(&Trace::cs);

}


