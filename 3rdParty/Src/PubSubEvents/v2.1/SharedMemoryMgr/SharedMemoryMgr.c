#include <windows.h>
#include <stdio.h>

#include "SharedMemoryMgr.h"

#define UNICODE

#ifdef _DEBUG
#define DEBUGCODE( code_fragment) {code_fragment}
#else
#define DEBUGCODE( code_fragment)
#endif

extern INT32 __cdecl InitMemoryMgr(LPCTSTR SharedMemoryName, DWORD SharedMemorySize, PCOMMBUFFER *CommBufferIn);
extern INT32 __cdecl JoinMemoryMgr(LPCTSTR SharedMemoryName, PCOMMBUFFER *CommBufferIn);
extern INT32 __cdecl ReleaseMemoryMgr(PCOMMBUFFER *CommBufferIn);
extern INT32 __cdecl PutBuffer(LPCSTR pEventBuffer, DWORD dwEventLength, DWORD dwTimeOut, PCOMMBUFFER *CommBufferIn);
extern INT32 __cdecl GetBuffer(LPCSTR pEventBuffer, DWORD dwEventBufferLength, DWORD dwTimeOut, DWORD *pBytesRead, PCOMMBUFFER *CommBufferIn);

void InitEvent(PCOMMBUFFER CommBuffer);
void InitNewEvent(PCOMMBUFFER CommBuffer);
INT32 CopyEventToBuffer(DWORD iOffset, PWSPEVENT pWspEvent, LPCSTR pEventBuffer, DWORD dwEventLength, PCOMMBUFFER CommBuffer);
INT32 CopyEventFromBuffer(LPCSTR pEventBuffer, DWORD dwEventBufferLength, DWORD *pBytesRead, UINT64 *pEventNum, PCOMMBUFFER CommBuffer);
LPCSTR GetWspEvent(PWSPEVENT pWspEvent, PCOMMBUFFER CommBuffer, DWORD dwStartOffset);

BOOL bCriticalSectionInitialized = FALSE;
CRITICAL_SECTION critSec;

HANDLE ghMapFile = NULL;
PSHAREDMEMORY gpBuf = NULL;
INT32 iRefCount = 0;

// RKS The following 4 lines were specified as Global\\*
#define MUTEX_NAME "Local\\WSP_MUTEX"
#define EVENT_NAME "Local\\WSP_EVENT"
#define CHILDEVENT_NAME "Local\\WSP_CHILDEVENT"
#define GLOBALPREPEND "Local\\"

#define SUCCESS 0
#define GENERALERRORCODE 5
#define TIMEOUT -1
#define OVERFLOW 9999
#define ENDOFDATA 3

#define READYTOREAD 0xFA
#define PREPARETOREAD 0xFB
#define NOTREADYTOREAD 0xFC
#define ALREADYREAD 0xFD


extern INT32 __cdecl InitMemoryMgr(LPCTSTR SharedMemoryNameIn, DWORD SharedMemorySize, PCOMMBUFFER *CommBufferIn)
{
	DWORD dwError;
	DWORD errco;
	PCOMMBUFFER CommBuffer;
	BOOL bFileExists;
	int iSize;
	LPCTSTR lpSharedMemoryName;
	SECURITY_DESCRIPTOR  sd;
	SECURITY_ATTRIBUTES sa = { sizeof sa, &sd, FALSE };

	InitializeSecurityDescriptor(&sd, SECURITY_DESCRIPTOR_REVISION);
	SetSecurityDescriptorDacl(&sd, TRUE, NULL, FALSE);

	iSize = strlen(GLOBALPREPEND) + strlen(SharedMemoryNameIn) + 1;
	lpSharedMemoryName = (LPCTSTR)malloc(iSize);
	strcpy_s((char*)lpSharedMemoryName, iSize, (char*)GLOBALPREPEND);
	strcat_s((char*)lpSharedMemoryName, iSize, (char*)SharedMemoryNameIn);

	bFileExists = FALSE;

	CommBuffer = (PCOMMBUFFER)malloc(sizeof(COMMBUFFER));
	*CommBufferIn = CommBuffer;

	InitEvent(CommBuffer);

	CommBuffer->iLastEventNumRead = 0;
	CommBuffer->bSharedMemoryOwner = TRUE;
	CommBuffer->dwNextReadOffset = 0;

	CommBuffer->ghMutex = CreateMutex(&sa, FALSE, MUTEX_NAME);

	errco = GetLastError();

	if (CommBuffer->ghMutex == NULL) 
	{
		if(errco == 0)
			errco = GENERALERRORCODE;

		free((char*)lpSharedMemoryName);
		free(CommBuffer);
		*CommBufferIn = NULL;

		return errco;
	}

    CommBuffer->ghEvent = CreateEvent(&sa, TRUE, 0, EVENT_NAME); 

	errco = GetLastError();

    if (CommBuffer->ghEvent == NULL) 
    { 
		if(errco == 0)
			errco = GENERALERRORCODE;

		free((char*)lpSharedMemoryName);
		ReleaseMutex(CommBuffer->ghMutex);
		free(CommBuffer);
		*CommBufferIn = NULL;

		return errco;
    }

	CommBuffer->ghMapFile = CreateFileMapping(
                 (HANDLE) INVALID_HANDLE_VALUE,		// use paging file
                 &sa,								// default security 
                 PAGE_READWRITE,					// read/write access
                 0,									// max. object size 
                 SharedMemorySize,					// buffer size 
                 (LPCTSTR) lpSharedMemoryName);		// name of mapping object

	errco = GetLastError();

	if(errco == ERROR_ALREADY_EXISTS)
	{
		bFileExists = TRUE;
	}
 
	if(CommBuffer->ghMapFile == NULL || CommBuffer->ghMapFile == INVALID_HANDLE_VALUE) 
	{ 
		if(errco == 0)
			errco = GENERALERRORCODE;

		free((char*)lpSharedMemoryName);
		ReleaseMutex(CommBuffer->ghMutex);
		CloseHandle(CommBuffer->ghEvent);
		free(CommBuffer);
		*CommBufferIn = NULL;

		return errco;
	}

	CommBuffer->gpBuf = (PSHAREDMEMORY) MapViewOfFile(
						CommBuffer->ghMapFile,	// handle to map object
						FILE_MAP_ALL_ACCESS,	// read/write permission
						0,                   
						0,                   
						0);

	errco = GetLastError();
 
   if (CommBuffer->gpBuf == NULL) 
   { 
		if(errco == 0)
			errco = GENERALERRORCODE;

		free((char*)lpSharedMemoryName);
		ReleaseMutex(CommBuffer->ghMutex);
		CloseHandle(CommBuffer->ghEvent);
		CloseHandle(CommBuffer->ghMapFile);
		free(CommBuffer);
		*CommBufferIn = NULL;

		return errco;
   }

   if(bFileExists == TRUE)
   {
	CommBuffer->iLastEventNumRead = CommBuffer->gpBuf->iLastEventNumRead;
	CommBuffer->dwNextReadOffset = CommBuffer->gpBuf->dwNextReadOffset;
   }
   else
   {
	   CommBuffer->gpBuf->dwNextReadOffset = 0;
	   CommBuffer->gpBuf->dwNextWriteOffset = 0;
	   CommBuffer->gpBuf->iLastEventNumWritten = 0;
	   CommBuffer->gpBuf->iLastEventNumRead = 0;
	   CommBuffer->gpBuf->iSharedMemSize = SharedMemorySize;
	   CommBuffer->gpBuf->iEventBufferSize = SharedMemorySize - 
		   (DWORD)((BYTE *)&(CommBuffer->gpBuf->bEventBuffer) - (BYTE *)CommBuffer->gpBuf);

	   InitNewEvent(CommBuffer);
   }

   ReleaseMutex(CommBuffer->ghMutex);

   free((char*)lpSharedMemoryName);

   return SUCCESS;
}

extern INT32 __cdecl JoinMemoryMgr(LPCTSTR SharedMemoryNameIn, PCOMMBUFFER *CommBufferIn)
{
	int rc;
	PCOMMBUFFER CommBuffer;
	int iSize;
	LPCTSTR lpSharedMemoryName;
	DWORD errco;

	if(bCriticalSectionInitialized == FALSE)
	{
	   InitializeCriticalSection(&critSec);
	   bCriticalSectionInitialized = TRUE;
	}

	iSize = strlen(GLOBALPREPEND) + strlen(SharedMemoryNameIn) + 1;
	lpSharedMemoryName = (LPCTSTR)malloc(iSize);
	strcpy_s((char*)lpSharedMemoryName, iSize, GLOBALPREPEND);
	strcat_s((char*)lpSharedMemoryName, iSize, SharedMemoryNameIn);

	CommBuffer = (PCOMMBUFFER)malloc(sizeof(COMMBUFFER));

	InitEvent(CommBuffer);

	CommBuffer->iLastEventNumRead = 0;
	CommBuffer->bSharedMemoryOwner = FALSE;

	CommBuffer->ghMutex = OpenMutex(MUTEX_ALL_ACCESS, TRUE, MUTEX_NAME);

	errco = GetLastError();

	if (CommBuffer->ghMutex == NULL) 
	{
		if(errco == 0)
			errco = GENERALERRORCODE;

		free((char*)lpSharedMemoryName);
		free(CommBuffer);
		*CommBufferIn = NULL;

		return errco;
	}

    CommBuffer->ghEvent = OpenEvent(EVENT_ALL_ACCESS, TRUE, EVENT_NAME); 

	errco = GetLastError();

    if (CommBuffer->ghEvent == NULL) 
    { 
		if(errco == 0)
			errco = GENERALERRORCODE;

		free((char*)lpSharedMemoryName);
		ReleaseMutex(CommBuffer->ghMutex);
		free(CommBuffer);
		*CommBufferIn = NULL;

		return errco;
    }

	EnterCriticalSection(&critSec);

	if(ghMapFile == NULL)
	{
		CommBuffer->ghMapFile = OpenFileMapping(
					 FILE_MAP_ALL_ACCESS,				// desired access
					 TRUE,								// inherit handle 
					 lpSharedMemoryName);				// name of mapping object

		errco = GetLastError();

	   if(CommBuffer->ghMapFile == NULL) 
	   { 
			if(errco == 0)
				errco = GENERALERRORCODE;

			free((char*)lpSharedMemoryName);
			ReleaseMutex(CommBuffer->ghMutex);
			CloseHandle(CommBuffer->ghEvent);
			CloseHandle(CommBuffer->ghMapFile);
			free(CommBuffer);
			*CommBufferIn = NULL;

			LeaveCriticalSection(&critSec);

			return errco;
	   }

		ghMapFile = CommBuffer->ghMapFile;
	}
	else
	{
		CommBuffer->ghMapFile = ghMapFile;
		errco = SUCCESS;
	}

	if(gpBuf == NULL)
	{
		CommBuffer->gpBuf = (PSHAREDMEMORY) MapViewOfFile(
							CommBuffer->ghMapFile,				// handle to map object
							FILE_MAP_ALL_ACCESS,	// read/write permission
							0,                   
							0,                   
							0);           

		errco = GetLastError();

		if (CommBuffer->gpBuf == NULL) 
		{ 
			if(errco == 0)
				errco = GENERALERRORCODE;

			free((char*)lpSharedMemoryName);
			ReleaseMutex(CommBuffer->ghMutex);
			CloseHandle(CommBuffer->ghEvent);
			CloseHandle(CommBuffer->ghMapFile);
			free(CommBuffer);
			*CommBufferIn = NULL;

			LeaveCriticalSection(&critSec);

			return errco;
		}

		gpBuf = CommBuffer->gpBuf;
	}
	else
	{
		CommBuffer->gpBuf = gpBuf;
		errco = SUCCESS;
	}

	iRefCount = iRefCount + 1;

	LeaveCriticalSection(&critSec);

	CommBuffer->dwNextReadOffset = CommBuffer->gpBuf->dwNextReadOffset;

	free((char*)lpSharedMemoryName);

	*CommBufferIn = CommBuffer;

	return SUCCESS;
}

extern INT32 __cdecl ReleaseMemoryMgr(PCOMMBUFFER *CommBufferIn)
{
	BOOL rc1 = SUCCESS;
	BOOL rc2 = SUCCESS;
	PCOMMBUFFER CommBuffer = *CommBufferIn;

	if(CommBuffer == NULL)
	{
		return SUCCESS;
	}

	ReleaseMutex(CommBuffer->ghMutex);
	CloseHandle(CommBuffer->ghEvent);

	EnterCriticalSection(&critSec);

	if(gpBuf == CommBuffer->gpBuf)
	{
		iRefCount = iRefCount - 1;

		if(iRefCount <= 0)
		{
			rc1 = UnmapViewOfFile(CommBuffer->gpBuf);

			rc2 = CloseHandle(CommBuffer->ghMapFile);

			gpBuf = NULL;
			ghMapFile = NULL;
		}
	}
	else
	{
		rc1 = UnmapViewOfFile(CommBuffer->gpBuf);

		rc2 = CloseHandle(CommBuffer->ghMapFile);
	}

	LeaveCriticalSection(&critSec);

	free(CommBuffer);
	*CommBufferIn = NULL;

	if(rc1 == FALSE || rc2 == FALSE)
	{
		return GENERALERRORCODE;
	}

	return SUCCESS;
}

extern DWORD GetQueueSize(PCOMMBUFFER *CommBufferIn)
{
	PCOMMBUFFER CommBuffer = *CommBufferIn;

	if(CommBuffer == NULL)
	{
		return 0;
	}

	return CommBuffer->gpBuf->iEventBufferSize;
}

extern INT32 __cdecl PutBuffer(LPCSTR pEventBuffer, DWORD dwEventLength, DWORD dwTimeOut, PCOMMBUFFER *CommBufferIn)
{
	DWORD dwWaitResult;
	WSPEVENT wspEvent;
	INT32 rc;
	PCOMMBUFFER CommBuffer = *CommBufferIn;

    dwWaitResult = WaitForSingleObject(CommBuffer->ghMutex, dwTimeOut);
 
    if(dwWaitResult == WAIT_OBJECT_0 || dwWaitResult == WAIT_ABANDONED) 
    {
		wspEvent.bReadyToRead = PREPARETOREAD;
		wspEvent.iEventNum = CommBuffer->gpBuf->iLastEventNumWritten + 1;
		wspEvent.iEventSize = sizeof(WSPEVENT) - 1 + dwEventLength;

		rc = CopyEventToBuffer(CommBuffer->gpBuf->dwNextWriteOffset, &wspEvent, 
								pEventBuffer, dwEventLength, CommBuffer);

		if(rc == SUCCESS)
		{
			CommBuffer->gpBuf->dwNextWriteOffset = (CommBuffer->gpBuf->dwNextWriteOffset + 
				wspEvent.iEventSize) % CommBuffer->gpBuf->iEventBufferSize;

			CommBuffer->gpBuf->iLastEventNumWritten++;

			ReleaseMutex(CommBuffer->ghMutex);

			PulseEvent(CommBuffer->ghEvent);

			return SUCCESS;
		}

		ReleaseMutex(CommBuffer->ghMutex);

		return GENERALERRORCODE;
	}
	else
	{
		return TIMEOUT;
	}
}

extern INT32 __cdecl GetBuffer(LPCSTR pEventBuffer, DWORD dwEventBufferLength, DWORD dwTimeOut, 
						   DWORD *pBytesRead, PCOMMBUFFER *CommBufferIn)
{
	DWORD dwWaitResult;
	INT32 rc;
	BYTE *pEventStartLocation;
	UINT64 iEventNum;
	PCOMMBUFFER CommBuffer = *CommBufferIn;

	if(CommBuffer->bSharedMemoryOwner == TRUE)
	{
		//SetThreadPriority(GetCurrentThread(), THREAD_PRIORITY_BELOW_NORMAL);
	}
	else
	{
		SetThreadPriority(GetCurrentThread(), THREAD_PRIORITY_TIME_CRITICAL);
	}

	pEventStartLocation = &(CommBuffer->gpBuf->bEventBuffer) + CommBuffer->dwNextReadOffset;

	if(CommBuffer->gpBuf->iLastEventNumWritten > CommBuffer->iLastEventNumRead)
	{
		if(*pEventStartLocation == READYTOREAD || *pEventStartLocation == ALREADYREAD)
		{
			rc = CopyEventFromBuffer(pEventBuffer, dwEventBufferLength, pBytesRead, &iEventNum, CommBuffer);

			if(rc == SUCCESS)
			{
				CommBuffer->iLastEventNumRead = iEventNum;

				if(CommBuffer->bSharedMemoryOwner == TRUE)
				{
					CommBuffer->gpBuf->dwNextReadOffset = CommBuffer->dwNextReadOffset;
					CommBuffer->gpBuf->iLastEventNumRead = CommBuffer->iLastEventNumRead;

					*pEventStartLocation = ALREADYREAD;
				}

				return SUCCESS;
			}
			else
			{
				if(rc != ENDOFDATA)
				{
					return rc;
				}
			}
		}
		else
		{
			CommBuffer->dwNextReadOffset = CommBuffer->gpBuf->dwNextReadOffset;
		}
	}

    dwWaitResult = WaitForSingleObject(CommBuffer->ghEvent, dwTimeOut);
 
    if(dwWaitResult == WAIT_OBJECT_0) 
    {
		if(CommBuffer->gpBuf->iLastEventNumWritten > CommBuffer->iLastEventNumRead &&
			(*pEventStartLocation == READYTOREAD || *pEventStartLocation == ALREADYREAD))
		{
			rc = CopyEventFromBuffer(pEventBuffer, dwEventBufferLength, pBytesRead, &iEventNum, CommBuffer);

			if(rc == SUCCESS)
			{
				CommBuffer->iLastEventNumRead = iEventNum;

				if(CommBuffer->bSharedMemoryOwner == TRUE)
				{
					CommBuffer->gpBuf->dwNextReadOffset = CommBuffer->dwNextReadOffset;
					CommBuffer->gpBuf->iLastEventNumRead = CommBuffer->iLastEventNumRead;

					*pEventStartLocation = ALREADYREAD;
				}

				return SUCCESS;
			}
			else
			{
				if(rc != ENDOFDATA)
				{
					return rc;
				}
			}
		}
	}

	iEventNum = 0;
	*pBytesRead = 0;

	return TIMEOUT;
}

void InitEvent(PCOMMBUFFER CommBuffer)
{
	CommBuffer->gInitEvent.bReadyToRead = NOTREADYTOREAD;
	CommBuffer->gInitEvent.iEventNum = 0;
	CommBuffer->gInitEvent.iEventSize = sizeof(WSPEVENT) - 1;
	CommBuffer->gInitEvent.bEvent = 0;
}

void InitNewEvent(PCOMMBUFFER CommBuffer)
{
	WSPEVENT newEvent;

	memcpy_s(&newEvent, sizeof(WSPEVENT), &(CommBuffer->gInitEvent), sizeof(WSPEVENT));

	newEvent.iEventNum = CommBuffer->gpBuf->iLastEventNumWritten;

	CopyEventToBuffer(0, &newEvent, NULL, sizeof(WSPEVENT), CommBuffer);
}

INT32 CopyEventToBuffer(DWORD iOffset, PWSPEVENT pWspEvent, LPCSTR pEventBuffer, DWORD dwEventLength, 
						PCOMMBUFFER CommBuffer)
{
	WSPEVENT wspEvent;
	BYTE *pStart;
	BYTE *pNext;
	DWORD dwSegmentLength;
	DWORD dwEventHeaderSize;

	dwEventHeaderSize = sizeof(WSPEVENT) - 1;

	// Event is larger than event buffer size
	if((dwEventLength + dwEventHeaderSize) >= CommBuffer->gpBuf->iEventBufferSize)
	{
		return GENERALERRORCODE;
	}

	// Event will wrap on buffer
	if((iOffset + dwEventLength + dwEventHeaderSize) >= CommBuffer->gpBuf->iEventBufferSize)
	{
		// Write will overtake next read
		if(CommBuffer->gpBuf->dwNextReadOffset > iOffset)
		{
			return GENERALERRORCODE;
		}
		else
		{
			if(CommBuffer->gpBuf->dwNextReadOffset == iOffset)
			{
				if(CommBuffer->gpBuf->iLastEventNumWritten > CommBuffer->gpBuf->iLastEventNumRead)
				{
					return GENERALERRORCODE;
				}
			}
			else
			{
				// Write will overtake next read
				if((iOffset + dwEventLength + dwEventHeaderSize - CommBuffer->gpBuf->iEventBufferSize) >= 
					CommBuffer->gpBuf->dwNextReadOffset)
				{
					return GENERALERRORCODE;
				}
			}
		}

		pStart = ((BYTE *) &(CommBuffer->gpBuf->bEventBuffer)) + iOffset;
		dwSegmentLength = CommBuffer->gpBuf->iEventBufferSize - iOffset;

		// Event header will fit in remaining buffer space
		if(dwSegmentLength > dwEventHeaderSize)
		{
			memcpy_s(pStart, dwEventHeaderSize, pWspEvent, dwEventHeaderSize);

			pNext = pStart + dwEventHeaderSize;
			dwSegmentLength = dwSegmentLength - dwEventHeaderSize;

			if(pEventBuffer != NULL)
			{
				// Copy first part of event to end of buffer
				memcpy_s(pNext, dwSegmentLength, pEventBuffer, dwSegmentLength);

				// Copy last part of event to beginning of buffer
				memcpy_s(&(CommBuffer->gpBuf->bEventBuffer), dwEventLength - dwSegmentLength, 
					((BYTE *) pEventBuffer) + dwSegmentLength, dwEventLength - dwSegmentLength);
			}

			if(*pStart == PREPARETOREAD)
			{
				*pStart = READYTOREAD;
			}
		}
		else
		{
			// Event header will NOT fit in remaining buffer space
			if(dwSegmentLength < dwEventHeaderSize)
			{
				// Copy first part of event header to end of buffer
				memcpy_s(pStart, dwSegmentLength, pWspEvent, dwSegmentLength);

				// Copy second part of event header to beginning of buffer
				memcpy_s(&(CommBuffer->gpBuf->bEventBuffer), dwEventHeaderSize - dwSegmentLength,
					((BYTE *) pWspEvent) + dwSegmentLength, dwEventHeaderSize - dwSegmentLength);

				pNext = ((BYTE *) &(CommBuffer->gpBuf->bEventBuffer)) + dwEventHeaderSize - dwSegmentLength;

				if(pEventBuffer != NULL)
				{
					// Copy last part of event
					memcpy_s(pNext, dwEventLength, pEventBuffer, dwEventLength);
				}

				if(*pStart == PREPARETOREAD)
				{
					*pStart = READYTOREAD;
				}
			}
			else
			{
				// Event header fits exactly in remaining buffer space
				memcpy_s(pStart, dwEventHeaderSize, pWspEvent, dwEventHeaderSize);

				if(pEventBuffer != NULL)
				{
					// Copy last part of event at beginning of buffer
					memcpy_s(&(CommBuffer->gpBuf->bEventBuffer), dwEventLength, pEventBuffer, dwEventLength);
				}

				if(*pStart == PREPARETOREAD)
				{
					*pStart = READYTOREAD;
				}
			}
		}
	}
	// Event fits in remainder of buffer
	else
	{
		pStart = &(CommBuffer->gpBuf->bEventBuffer) + iOffset;

		// Write offset is BEFORE next read offset
		if(iOffset < CommBuffer->gpBuf->dwNextReadOffset)
		{
			// Write will overtake next read
			if((iOffset + dwEventLength + dwEventHeaderSize + 1) > CommBuffer->gpBuf->dwNextReadOffset)
			{
				return GENERALERRORCODE;
			}
		}

		// Write offset is EQUAL to next read offset and write will overtake next read
		if(iOffset == CommBuffer->gpBuf->dwNextReadOffset &&
			CommBuffer->gpBuf->iLastEventNumWritten > CommBuffer->gpBuf->iLastEventNumRead)
		{
			return GENERALERRORCODE;
		}

		// Copy event header
		memcpy_s(pStart, dwEventHeaderSize, pWspEvent, dwEventHeaderSize);

		if(pEventBuffer != NULL)
		{
			// Copy event
			memcpy_s(((BYTE *) pStart) + dwEventHeaderSize, dwEventLength, pEventBuffer, dwEventLength);
		}

		if(*pStart == PREPARETOREAD)
		{
			*pStart = READYTOREAD;
		}
	}

   return SUCCESS;
}

INT32 CopyEventFromBuffer(LPCSTR pEventBuffer, DWORD dwEventBufferLength, DWORD *pBytesRead, UINT64 *pEventNum, 
						  PCOMMBUFFER CommBuffer)
{
	WSPEVENT wspEvent;
	DWORD dwNewOffset;
	LPCSTR pStart;
	DWORD dwSegmentLength;
	DWORD dwEventLength;
	DWORD dwEventHeaderSize;

	dwEventHeaderSize = sizeof(WSPEVENT) - 1;

	*pBytesRead = 0;
	*pEventNum = 0;

	if(dwEventHeaderSize > dwEventBufferLength)
	{
		return GENERALERRORCODE;
	}

	pStart = GetWspEvent(&wspEvent, CommBuffer, CommBuffer->dwNextReadOffset);

	dwEventLength = wspEvent.iEventSize - dwEventHeaderSize;

	*pBytesRead = dwEventLength;
	*pEventNum = wspEvent.iEventNum;

	if(dwEventLength > dwEventBufferLength)
	{
		return OVERFLOW;
	}

	// Event wraps buffer
	if( ((BYTE *) pStart - (BYTE *) &(CommBuffer->gpBuf->bEventBuffer)) + dwEventLength > 
		CommBuffer->gpBuf->iEventBufferSize)
	{
		dwSegmentLength = CommBuffer->gpBuf->iEventBufferSize - 
			((BYTE *) pStart - (BYTE *) &(CommBuffer->gpBuf->bEventBuffer));

		// Copy first part of event
		memcpy_s((void *)pEventBuffer, (size_t) dwSegmentLength, pStart, (size_t) dwSegmentLength);

		// Wrap and copy last part of event
		memcpy_s((void *)((BYTE *) pEventBuffer + dwSegmentLength), (size_t) (dwEventLength - dwSegmentLength), 
			&(CommBuffer->gpBuf->bEventBuffer), (size_t) (dwEventLength - dwSegmentLength));

		dwNewOffset = dwEventLength - dwSegmentLength;
	}
	else
	{
		// Copy event
		memcpy_s((void *)pEventBuffer, (size_t) dwEventLength, pStart, (size_t) dwEventLength);

		dwNewOffset = ((BYTE *) pStart - (BYTE *) &(CommBuffer->gpBuf->bEventBuffer)) + dwEventLength;
	}

	CommBuffer->dwNextReadOffset = dwNewOffset;

	if(CommBuffer->dwNextReadOffset == CommBuffer->gpBuf->iEventBufferSize)
	{
		CommBuffer->dwNextReadOffset = 0;
	}

	return SUCCESS;
}

LPCSTR GetWspEvent(PWSPEVENT pWspEvent, PCOMMBUFFER CommBuffer, DWORD dwStartOffset)
{
	LPCSTR pStart;
	DWORD dwSegmentLength;
	DWORD dwEventHeaderSize;

	dwEventHeaderSize = sizeof(WSPEVENT) - 1;

	// Event header wraps the buffer
	if((dwStartOffset + dwEventHeaderSize) >= CommBuffer->gpBuf->iEventBufferSize)
	{
		dwSegmentLength = CommBuffer->gpBuf->iEventBufferSize - dwStartOffset;

		// Copy first part of event header
		memcpy_s(pWspEvent, dwSegmentLength, ((BYTE *) &(CommBuffer->gpBuf->bEventBuffer)) + dwStartOffset, dwSegmentLength);

		// Wrap and copy last part of event header
		memcpy_s(((BYTE *) pWspEvent) + dwSegmentLength, dwEventHeaderSize - dwSegmentLength, 
			&(CommBuffer->gpBuf->bEventBuffer), dwEventHeaderSize - dwSegmentLength);

		// Starting location for event
		pStart = (LPCSTR) (((BYTE *) &(CommBuffer->gpBuf->bEventBuffer)) + dwEventHeaderSize - dwSegmentLength);
	}
	else
	{
		// Copy event header
		memcpy_s(pWspEvent, dwEventHeaderSize, 
			((BYTE *) &(CommBuffer->gpBuf->bEventBuffer)) + dwStartOffset, dwEventHeaderSize);

		pStart = (LPCSTR) (((BYTE *) &(CommBuffer->gpBuf->bEventBuffer)) + dwStartOffset + dwEventHeaderSize);
	}

	return pStart;
}
