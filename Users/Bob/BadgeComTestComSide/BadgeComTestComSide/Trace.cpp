#include "stdafx.h"
#include "Trace.h"

using namespace std;

BOOL Trace::_fInstanceFlag = false;
Trace* Trace::_single = NULL;
volatile long Trace::instanceInitialized = 0;
CRITICAL_SECTION Trace::_cs;

/// <Summary>
/// Return the trace singleton instance.  Create it the first time this is called.
/// </Summary>
Trace* Trace::getInstance()
{
    try
    {
        // First time initialization
        if (InterlockedIncrement(&instanceInitialized) == 1)
        {
            _single = new Trace();
            _fInstanceFlag = true;

            // Determine the trace directory
            wchar_t wszBuffer[MAX_PATH];
            BOOL result = SHGetSpecialFolderPath(NULL, wszBuffer, CSIDL_LOCAL_APPDATA, false /* don't create the folder */ );
            if(!result)
            {
                // Error.  Leave trace disabled.
                return _single;
            }
            _single->_wsTraceDirectory.clear();
            _single->_wsTraceDirectory.append(wszBuffer);

            // Build the full path to the trace directory and create it if it doesn't exist.
            _single->_wsTraceDirectory.append(L"\\Cloud");
		    int rc = SHCreateDirectoryExW(NULL, _single->_wsTraceDirectory.c_str(), NULL);
            if (rc != ERROR_SUCCESS && rc != ERROR_ALREADY_EXISTS)
            {
                // Couldn't create the trace directory
                return _single;
            }

            // Get the maximum trace level we will trace.
            _single->DetermineMaximumTraceLevel();

            // Enable trace if the maximum trace level is set.
            if (_single->_nMaxPriorityToTrace != 0)
            {
                _single->_fTraceEnabled = true;
            }

            // Add a "Starting..." record to the trace.  Get the name of this process.
			char buffer[MAX_PATH];
			GetModuleFileNameA(NULL, buffer, sizeof(buffer));
            _single->write(0, "Starting... TraceEnabled: %d. MaxPriority: %d.  Process name: %s.", _single->_fTraceEnabled, _single->_nMaxPriorityToTrace, buffer);
        }
    }
    catch (...)
    {
    }

    return _single;
}

/// <Summary>
/// Read the maximum trace level from a configuration filee, or default it.
/// </Summary>
void Trace::DetermineMaximumTraceLevel()
{
    try
    {
        // Read the configuration file 
        std::wstring wsConfigFile(_wsTraceDirectory);
        wsConfigFile.append(L"\\CloudTraceLevel.ini");

        std::wifstream ifs(wsConfigFile.c_str());
        std::wstring wsTracePriority((std::istreambuf_iterator<WCHAR>(ifs)), std::istreambuf_iterator<WCHAR>());

        int nTracePriority;
        STR2INT_ERROR rc;
        rc = wstr2int(nTracePriority, wsTracePriority.c_str());
        if (rc == STR2INT_SUCCESS)
        {
            _nMaxPriorityToTrace = nTracePriority;
        }
        else
        {
            _nMaxPriorityToTrace = 0;               // default the maximum trace priority if the file is not found.
        }
    }
    catch (...)
    {
        _nMaxPriorityToTrace = 0;               // default the maximum trace priority if the file is not found.
    }
}


/// <Summary>
/// Write a line of trace.
/// </Summary>
void Trace::write(int priority, char *szFormat, ...)
{
    try
    {
	    EnterCriticalSection(&Trace::_cs);
	    if (!_fTraceEnabled || priority > _nMaxPriorityToTrace)
	    {
		    LeaveCriticalSection(&Trace::_cs);
		    return;
	    }

        // Change the trace file name if the date has changed, and manage the number of trace files in the trace directory.
        PerhapsChangeTraceFilename();

        // Start the variable arg list.
	    va_list vl;
	    va_start(vl, szFormat);

	    // Open the trace file for output.  Allow other processes to append to the file also to intermix the entries.
	    _streamTrace = _wfsopen(_wsTraceFileFullPath.c_str(), L"a", _SH_DENYNO);
	    if (_streamTrace == NULL)
	    {
		    _fTraceEnabled = false;
		    va_end(vl);
		    LeaveCriticalSection(&Trace::_cs);
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
	    fprintf(_streamTrace, "CPP_%04d-%02d-%02d_%02d:%02d:%02d-%03d_P%lx-T%lx_", lt.wYear, lt.wMonth, lt.wDay, lt.wHour, lt.wMinute, lt.wSecond, lt.wMilliseconds, processId, threadId);

	    // Trace the message
	    vfprintf(_streamTrace, szFormat, vl);

        // Add a newline
	    fprintf(_streamTrace, "\n");

	    // Close the stream to flush the data
	    fclose(_streamTrace);

	    va_end(vl);
    }
    catch (...)
    {
    }

    try
    {
        LeaveCriticalSection(&Trace::_cs);
    }
    catch (...)
    {
    }
}

/// <Summary>
/// Convert a string to an integer.
/// </Summary>
Trace::STR2INT_ERROR Trace::wstr2int(int &i, WCHAR const *s, int base)
{
    WCHAR *end;
    long  l;
    errno = 0;
    l = wcstol(s, &end, base);
    if ((errno == ERANGE && l == LONG_MAX) || l > INT_MAX) 
    {
        return STR2INT_OVERFLOW;
    }
    if ((errno == ERANGE && l == LONG_MIN) || l < INT_MIN) 
    {
        return STR2INT_UNDERFLOW;
    }
    if (*s == L'\0' || (*end != L'\0' && *end != L'\n'))
    {
        return STR2INT_INCONVERTIBLE;
    }
    i = l;
    return STR2INT_SUCCESS;
}

/// <Summary>
/// Change the trace file name if the date has changed.  Also manage the number of trace files in the trace directory.
/// </Summary>
void Trace::PerhapsChangeTraceFilename()
{
    try
    {
        // Just return if it isn't time (for performance)
        --_nTraceLineCountdown;
        if (_nTraceLineCountdown >= 0)
        {
            return;
        }
        _nTraceLineCountdown = _knTraceLinesBeforeCheckDateChange;       // reset the count

        // Create the new trace file full path if the date has changed.  Get the current time.
	    time_t rawtime;
	    struct tm timeinfo;

	    time(&rawtime);
	    localtime_s(&timeinfo, &rawtime);

        // Check the date.  Note: tm_mon is 0-11, and tm_year is year - 1900.
        if (timeinfo.tm_year != _nYear
            || timeinfo.tm_mon != _nMonth
            || timeinfo.tm_mday != _nDay)
        {
            // The date has changed
            _nYear = timeinfo.tm_year;
            _nMonth = timeinfo.tm_mon;
            _nDay = timeinfo.tm_mday;

            // Generate the full path of the new trace file.
    	    WCHAR wsTime[200];
		    WCHAR wsBuff[200];
    	    wcsftime(wsTime, sizeof(wsTime) / sizeof(WCHAR), L"%Y-%m-%d", &timeinfo);
		    wsprintf(wsBuff, L"\\Trace-%ls-CloudShellExt.log", wsTime);
		    _wsTraceFileFullPath = _wsTraceDirectory + wsBuff;

            // Start another thread to delete old trace files.
            DWORD dwThreadId;
            HANDLE handle = CreateThread(NULL,                              // default security
                        0,                                                  // default stack size
                        (LPTHREAD_START_ROUTINE)&DeleteOldTraceFilesThreadProc,     // function to run
                        (LPVOID) this,                                      // thread parameter
                        0,                                                  // imediately run the thread
                        &dwThreadId                                         // output thread ID
                        );
            if (handle != NULL)
            {
                _threadDeleteOldTraceFiles = handle;
            }
        }
    }
    catch (...)
    {
    }
}

/// <summary>
/// Check the trace directory for old trace files.  Delete them as necessary.
/// </summary>
void Trace::DeleteOldTraceFilesThreadProc(LPVOID pUserState)
{
    // Cast the user state to an object instance
    Trace *pThis = (Trace *)pUserState;

	try
	{
		// Enumerate the files by wild card "<TraceDirectory>\TraceCloudShellExt*.log".
        if (pThis != NULL)
        {
            // Enumerate files by wild card TraceDirectory\TraceCloudShellExt*.log
            std::wstring searchKey = pThis->_wsTraceDirectory + L"\\TraceCloudShellExt-????-??-??.log";
            std::vector<std::wstring> fileList;
            int nFiles = pThis->GetFileList(searchKey, fileList);
            if (nFiles > 0)
            {
                // Sort the files increasing
                std::sort(fileList.begin(), fileList.end());

                // Determine the cutoff date.
                date dateCutoff(pThis->_nYear + 1900, pThis->_nMonth + 1, pThis->_nDay);
                days daysCutoffInterval(_knDeleteTraceFilesOlderThanDays);
                dateCutoff = dateCutoff - daysCutoffInterval;

                // Loop through the trace files deleting any that are older than the cutoff date.
                for (std::vector<std::wstring>::iterator itFile = fileList.begin(); itFile != fileList.end(); ++itFile)
                {
                    std::wstring thisFileFullPath;
                    thisFileFullPath = pThis->_wsTraceDirectory + L"\\" + *itFile;
                    if (thisFileFullPath.compare(pThis->_wsTraceFileFullPath) != 0)
                    {
                        std::wstring wsParse(itFile->substr(19, 4));
                        int nYear;
                        int nMonth;
                        int nDay;
                        int nRc = pThis->wstr2int(nYear, wsParse.c_str());
                        if (nRc == STR2INT_SUCCESS)
                        {
                            wsParse = itFile->substr(24, 2);
                            nRc = pThis->wstr2int(nMonth, wsParse.c_str());
                            if (nRc == STR2INT_SUCCESS)
                            {
                                wsParse = itFile->substr(27, 2);
                                nRc = pThis->wstr2int(nDay, wsParse.c_str());
                                if (nRc == STR2INT_SUCCESS)
                                {
                                    date dateFile(nYear, nMonth, nDay);
                                    if (dateFile <= dateCutoff)
                                    {
                                        // Delete this file
                                        DeleteFile(thisFileFullPath.c_str());
                                    }
                                }
                            }
                        }
                    }
                }
            }

        }

	}
    catch (...)
    {
    }

    // Clean up the thread handle.
    if (pThis != NULL)
    {
        CloseHandle(pThis->_threadDeleteOldTraceFiles);
        pThis->_threadDeleteOldTraceFiles = NULL;
    }
}

/// <summary>
/// Check the trace directory for old trace files.  Delete them as necessary.
/// Usage:  Call with searchkey like L"c:\\abc\\*.log";
/// </summary>
int Trace::GetFileList(std::wstring &wsSearchKey, std::vector<std::wstring> &outList)
{
    WIN32_FIND_DATA fd;
    HANDLE h = FindFirstFile(wsSearchKey.c_str(), &fd);

    if(h == INVALID_HANDLE_VALUE)
    {
        return 0; // no files found
    }

    while(true)
    {
        outList.push_back(fd.cFileName);
        if(FindNextFile(h, &fd) == FALSE)
        {
            break;
        }
    }

    return (int)outList.size();
}

/// <summary>
/// Trace bytes stored at an arbitrary memory address.  Also trace interpreted ASCII data to the right in each line.
/// </summary>
void Trace::writeDumpData(void *pvData, USHORT usLenData, int priority, char *szFormat, ...)
{
	CHAR	szDump[_knDbgMaxBufferSize];
	CHAR*	pByte;
	DWORD	nLines;
	DWORD	x;
	DWORD	y;
	CHAR*	pbaData = (CHAR *)pvData;

	try
	{
	    EnterCriticalSection(&Trace::_cs);
	    if (!_fTraceEnabled || priority > _nMaxPriorityToTrace)
	    {
		    LeaveCriticalSection(&Trace::_cs);
		    return;
	    }

        // Change the trace file name if the date has changed, and manage the number of trace files in the trace directory.
        PerhapsChangeTraceFilename();

        // Start the variable arg list.
	    va_list vl;
	    va_start(vl, szFormat);

	    // Open the trace file for output.  Allow other processes to append to the file also to intermix the entries.
	    _streamTrace = _wfsopen(_wsTraceFileFullPath.c_str(), L"a", _SH_DENYNO);
	    if (_streamTrace == NULL)
	    {
		    _fTraceEnabled = false;
		    va_end(vl);
		    LeaveCriticalSection(&Trace::_cs);
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
	    fprintf(_streamTrace, "CPP_%04d-%02d-%02d_%02d:%02d:%02d-%03d_P%lx-T%lx_", lt.wYear, lt.wMonth, lt.wDay, lt.wHour, lt.wMinute, lt.wSecond, lt.wMilliseconds, processId, threadId);

	    // Trace the message
	    vfprintf(_streamTrace, szFormat, vl);

        // Add a newline
	    fprintf(_streamTrace, "\n");

		// Start putting out the data lines.
		nLines = usLenData / _knDbgBytesInDumpLine;
		if( nLines * _knDbgBytesInDumpLine != usLenData)
		{
			nLines++;
		}

		for( x=0;x<nLines;x++)
		{
			UCHAR	ucChar;
			size_t	stBytesToDump;
			size_t	stDumpUsed;

			stBytesToDump = min( _knDbgBytesInDumpLine, usLenData - (x*_knDbgBytesInDumpLine));
			sprintf_s( szDump, sizeof( szDump), "%04X:  ", x * _knDbgBytesInDumpLine);
			pByte = &pbaData[ x * _knDbgBytesInDumpLine];
			for( y=0; y<_knDbgBytesInDumpLine; y++, pByte++)
			{
				if( y!= 0 && y % 8 == 0)
				{
					strcat_s( szDump, sizeof( szDump), "- ");
				}
				else if (y != 0 && y % 4 == 0)
				{
					strcat_s( szDump, sizeof( szDump), ". ");
				}

				if( y < stBytesToDump)
				{
					stDumpUsed = strlen( szDump);
					sprintf_s( &szDump[stDumpUsed], sizeof( szDump) - stDumpUsed, "%02x ", *pByte & 0xff);
				}
				else
				{
					strcat_s( &szDump[stDumpUsed], sizeof( szDump) - stDumpUsed, "   ");
				}
			}

			strcat_s( szDump, sizeof( szDump), "  ");
			pByte = &pbaData[ x * _knDbgBytesInDumpLine];
			for (y = 0; y < stBytesToDump; y++, pByte++)
			{
				if (y != 0 && y % 4 == 0)
				{
					strcat_s( szDump, sizeof( szDump), " ");
				}

				stDumpUsed = strlen( szDump);
				ucChar = *pByte & 0xff;
				if(isgraph(ucChar) || isspace(ucChar) && ucChar > 14)
				{
					ucChar = *pByte & 0xff;
				}
				else
				{
					ucChar = '.';
				}

				sprintf_s( &szDump[stDumpUsed], sizeof( szDump) - stDumpUsed, "%c", ucChar);
			}

			// Output this dump line indented.
			fprintf(_streamTrace, "               %s\n", szDump);
		}

	    // Close the stream to flush the data
	    fclose(_streamTrace);

	    va_end(vl);
    }
    catch (...)
    {
    }

    try
    {
        LeaveCriticalSection(&Trace::_cs);
    }
    catch (...)
    {
    }
}


