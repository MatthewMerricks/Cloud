//
// BadgeIconSelective.cpp
// Cloud Windows COM
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

// BadgeIconSelective.cpp : Implementation of CBadgeIconSelective

#include "stdafx.h"
#include "BadgeIconSelective.h"
#include <Windows.h>
#include <stdio.h>
#include <sstream>
using namespace std;

//// includes for debugging only
//#include <iostream>
//#include <fstream>

// CBadgeIconSelective

// IShellIconOverlayIdentifier::GetOverlayInfo
// returns The Overlay Icon Location to the system
// By debugging I found it only runs once when the com object is loaded to the system, thus it cannot return a dynamic icon
STDMETHODIMP CBadgeIconSelective::GetOverlayInfo(
	LPWSTR pwszIconFile,
	int cchMax,
	int* pIndex,
	DWORD* pdwFlags)
{
	//////Commented out logging:
	//ofstream myfile;
	//myfile.open("C:\\Users\\Public\\Documents\\BadgeCOM.log", ofstream::app);
	//myfile << "Entered GetOverlayInfo\r\n";
	//myfile.close();

	// Get our module's full path
	GetModuleFileNameW(_AtlBaseModule.GetModuleInstance(), pwszIconFile, cchMax);

	// Use third icon in the resource (Selective.ico)
	*pIndex = 3;

	*pdwFlags = ISIOI_ICONFILE | ISIOI_ICONINDEX;
	return S_OK;
}

// IShellIconOverlayIdentifier::GetPriority
// returns the priority of this overlay 0 being the highest.
STDMETHODIMP CBadgeIconSelective::GetPriority(int* pPriority)
{
	//////Commented out logging:
	//ofstream myfile;
	//myfile.open("C:\\Users\\Public\\Documents\\BadgeCOM.log", ofstream::app);
	//myfile << "Entered GetPriority\r\n";
	//myfile.close();

	// change the following to set priority between multiple overlays
	*pPriority = 0;
	return S_OK;
}

typedef HRESULT (WINAPI*pfnGetDispName)(LPCWSTR, LPWSTR, DWORD);

// IShellIconOverlayIdentifier::IsMemberOf
// Returns whether the object should have this overlay or not 
STDMETHODIMP CBadgeIconSelective::IsMemberOf(LPCWSTR pwszPath, DWORD dwAttrib)
{
	//default return value is false (no icon overlay)
	HRESULT r = S_FALSE;
	// identify which COM object this is
	wchar_t const* pipeForCurrentBadgeType = L"\\\\.\\Pipe\\BadgeCOMcloudAppBadgeSyncSelective";
	//copy input path to local unicode char
	wchar_t *s = _wcsdup(pwszPath);
	try
	{
		//store length of copied path
		int sLength = wcslen(s);

		////Commented out logging:
		//ofstream myfile;
		//myfile.open("C:\\Users\\Public\\Documents\\BadgeCOM.log", ofstream::app);
		//myfile << "Entered IsMemberOf: ";
		//myfile << s;//displays number like 00000000237FEA90
		//char *smallS = new char[sLength];
		//////append name in non-unicode
		//wcstombs(smallS, s, sLength);
		//string memberOfTermination = "(";
		//memberOfTermination.append(smallS);
		//memberOfTermination.append(")\r\n");
		//myfile << memberOfTermination;
		//myfile.close();

		//Declare some variables
		HANDLE PipeHandle;
		DWORD BytesWritten;
		DWORD BytesRead;
		BYTE returnBuffer[1];
		bool pipeConnectionFailed = false;

		// Try to open the named pipe identified by the pipe name.
		while (!pipeConnectionFailed)
		{
			// Opens the pipe for writing
			PipeHandle = CreateFile(
				pipeForCurrentBadgeType, // Pipe name
				GENERIC_WRITE, // Write access
				0, // No sharing
				NULL, // Default security attributes
				OPEN_EXISTING, // Opens existing pipe
				0, // Default attributes
				NULL // No template file
				);

			// If the pipe handle is opened successfully then break out to continue
			if (PipeHandle != INVALID_HANDLE_VALUE)
			{
				//////Commented out logging:
				//myfile.open("C:\\Users\\Public\\Documents\\BadgeCOM.log", ofstream::app);
				//myfile << "BadgeCOM pipe connected for write\r\n";
				//myfile.close();
				break;
			}
			// Pipe not successful, find out if it should try again
			else
			{
				// store not successful reason
				DWORD dwError = GetLastError();

				// Exit if an error other than ERROR_PIPE_BUSY occurs (by setting pipeConnectionFailed to true)
				// This is the normal path when the application is not running (dwError will equal ERROR_FILE_NOT_FOUND)
				if (ERROR_PIPE_BUSY != dwError)
				{
					////////Commented out logging:
					//if (ERROR_FILE_NOT_FOUND == dwError)
					//{
					//	myfile.open("C:\\Users\\Public\\Documents\\BadgeCOM.log", ofstream::app);
					//	myfile << "Pipe not found (app probably not running)\r\n";
					//	myfile.close();
					//}
					//else
					//{
					//	stringstream errorStream;
					//	errorStream << dwError;
			
					//	myfile.open("C:\\Users\\Public\\Documents\\BadgeCOM.log", ofstream::app);
					//	myfile << "Error openning BadgeCOM pipe:";
					//	myfile << errorStream.str();
					//	myfile << "\r\n";
					//	myfile.close();
					//}
					pipeConnectionFailed = true;
				}
				// pipe is busy
				else
				{
					// if waiting for a pipe does not complete in 2 seconds, exit  (by setting pipeConnectionFailed to true)
					if (!WaitNamedPipe(pipeForCurrentBadgeType, 2000))
					{
						//////Commented out logging:
						//myfile.open("C:\\Users\\Public\\Documents\\BadgeCOM.log", ofstream::app);
						//myfile << "BadgeCOM pipe failed to open after 2 seconds\r\n";
						//myfile.close();

						dwError = GetLastError();
						pipeConnectionFailed = true;
					}
				}
			}
		}
		// if pipe connection did not fail begin pipe transfer logic
		if (!pipeConnectionFailed)
		{
			// calculate bytes required to send filepath
			unsigned int pathLength = sizeof(wchar_t)*sLength;
			// unique id for each connection attempt (used for unique return pipe)
			static long pipeCounter = 0;

			// store pipeCounter as string
			stringstream packetIdStream;
			packetIdStream << pipeCounter++;

			//need to zero-pad packetId to ensure constant length of 10 chars
			string packetId;
			packetIdStream >> packetId;
			int startLength = packetId.length();
			for (int currentPaddedChar = 0; currentPaddedChar < 10 - startLength; currentPaddedChar++)
				packetId = "0" + packetId;

			// store filepath byte length as string
			stringstream currentPathLength;
			currentPathLength << pathLength;

			//need to zero-pad pathLength to ensure constant length of 10 chars
			string paddedLength;
			currentPathLength >> paddedLength;
			startLength = paddedLength.length();
			for (int currentPaddedChar = 0; currentPaddedChar < 10 - startLength; currentPaddedChar++)
				paddedLength = "0" + paddedLength;
		
			//////Commented out logging:
			//myfile.open("C:\\Users\\Public\\Documents\\BadgeCOM.log", ofstream::app);
			//myfile << "BadgeCOM pipe became writable\r\n";
			//myfile.close();

			// write packetId + filepath byte length to pipe (must be 20 bytes)
			if (WriteFile(PipeHandle,
				(packetId + paddedLength).c_str(),
				20,//needs to be 20 since that's what's being read from the other end
				&BytesWritten,
				NULL) != 0)
			{
				//////Commented out logging:
				//myfile.open("C:\\Users\\Public\\Documents\\BadgeCOM.log", ofstream::app);
				//myfile << "Wrote ";
				//myfile << packetId;
				//myfile << paddedLength;
				//myfile << " to BadgeCOM pipe\r\n";
				//myfile.close();

				// write filepath to pipe (variable bytes)
				if (WriteFile(PipeHandle,
					s, //filepath
					pathLength, //length of filepath
					&BytesWritten,
					NULL) != 0)
				{
					//////Commented out logging:
					//myfile.open("C:\\Users\\Public\\Documents\\BadgeCOM.log", ofstream::app);
					//myfile << "Wrote filePath to BadgeCOM pipe\r\n";
					//myfile.close();

					// convert packetId to string to start building unique name for return pipe
					stringstream returnPipeNameStream;
					returnPipeNameStream << packetId;
					string returnPipeName = returnPipeNameStream.str();

					// widen return pipe name to unicode
					int returnPipeNameWideWidth;
					int returnPipeNameWidth = (int)returnPipeName.length() + 1;
					returnPipeNameWideWidth = MultiByteToWideChar(CP_ACP, 0, returnPipeName.c_str(), returnPipeNameWidth, 0, 0);
					wchar_t* returnPipeNameWideBuffer = new wchar_t[returnPipeNameWideWidth];
					MultiByteToWideChar(CP_ACP, 0, returnPipeName.c_str(), returnPipeNameWidth, returnPipeNameWideBuffer, returnPipeNameWideWidth);
					std::wstring returnPipeNameWide(returnPipeNameWideBuffer);
					delete[] returnPipeNameWideBuffer;

					// add base pipe name before the packetId to complete the return pipe name
					returnPipeNameWide = pipeForCurrentBadgeType + returnPipeNameWide;
				
					//////Commented out logging:
					//myfile.open("C:\\Users\\Public\\Documents\\BadgeCOM.log", ofstream::app);
					//myfile << "About to connect to BadgeCOM pipe: ";
					//myfile << returnPipeName; //this line won't work anymore, would need to write returnPipeNameWide which is wide
					//myfile << " (length: ";
					//stringstream checkLengthOfName;
					//checkLengthOfName << returnPipeNameWide.length();
					//myfile << checkLengthOfName.str();
					//myfile << ")\r\n";
					//myfile.close();

					// variables for return connection
					int returnRetries = 0;
					bool keepTryingForReturn = true;

					// loop for retrying return connection until timeout of 2 seconds (40 tries, 50 millisecond delay each)
					while (keepTryingForReturn)
					{
						HANDLE returnHandle;
						// attempt to establish return connection on unique return pipename (widened to unicode)
						if ((returnHandle = CreateFile(
							returnPipeNameWide.c_str(), // Pipe name
							GENERIC_READ, // Read access
							0, // No sharing
							NULL, // Default security attributes
							OPEN_EXISTING, // Opens existing pipe
							0, // Default attributes
							NULL // No template file
							)) != INVALID_HANDLE_VALUE)
						{
							// connection successful, don't need to keep retrying
							keepTryingForReturn = false;
						
							//////Commented out logging:
							//myfile.open("C:\\Users\\Public\\Documents\\BadgeCOM.log", ofstream::app);
							//myfile << "Return pipe connected: ";
							//myfile << returnPipeName;
							//myfile << "\r\n";
							//myfile.close();

							// read the return byte from the connection
							if (ReadFile(returnHandle,
								returnBuffer,
								1,
								&BytesRead,
								NULL))
							{
								//////Commented out logging:
								//myfile.open("C:\\Users\\Public\\Documents\\BadgeCOM.log", ofstream::app);
								//myfile << "Return pipe read ";
								//if (returnBuffer[0] == 0)
								//{
								//	myfile << "false";
								//}
								//else if (returnBuffer[0] == 1)
								//{
								//	myfile << "true";
								//}
								//else
								//{
								//	myfile << "unknown";
								//}
								//myfile << "\r\n";
								//myfile.close();

								// check if return byte is '1' (true) to enable icon overlay
								if (returnBuffer[0] == 1)
								{
									// enable icon overlay
									r = S_OK;
								}
							}

							// clean up return pipe
							CloseHandle(returnHandle);
						}
						else
						{
							// store error from return pipe connection attempt
							DWORD storeLastError = GetLastError();

							// check for normal errors seen when pipe server is currently being created or has not been created yet
							// if a normal error is found, will check number of retries up to 20
							if (storeLastError == ERROR_ALREADY_EXISTS
								|| storeLastError == ERROR_FILE_NOT_FOUND)
							{
								// increment retry counter
								returnRetries++;
								// if retries hit 40 stop retrying
								if (returnRetries > 39)
								{
									//////Commented out logging:
									//myfile.open("C:\\Users\\Public\\Documents\\BadgeCOM.log", ofstream::app);
									//myfile << "Return pipe connection timed out after 1 second\r\n";
									//myfile.close();

									// stop retrying
									keepTryingForReturn = false;
								}
								// retries less than 40, wait 50 milliseconds and retry
								else
								{
									// wait 50 milliseconds
									Sleep(50);//40 tries at 50 milliseconds is 2 seconds to wait for return pipe to open
								}
							}
							// unknown error connecting to return pipe, stop retrying
							else
							{
								// stop retrying
								keepTryingForReturn = false;
							
								//////Commented out logging:
								//myfile.open("C:\\Users\\Public\\Documents\\BadgeCOM.log", ofstream::app);
								//myfile << "Return pipe connection error: ";
								//stringstream lastErrorStream;
								//lastErrorStream << storeLastError;
								//myfile << lastErrorStream.str();
								//myfile << "\r\n";
								//myfile.close();
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

	// clear memory for copied path string
	free(s);

	// return S_FALSE or S_OK for no icon overlay and icon overlay, respectively
	return r;
}