// BadgeComTestComSide.cpp : Defines the entry point for the console application.
//

#include "stdafx.h"

int _tmain(int argc, _TCHAR* argv[])
{
	#pragma region Constants
	const int nExplorersToSimulate = 8;
	#pragma endregion


	#pragma region Automatic Variables

	CExplorerSimulator simulators[nExplorersToSimulate];


	#pragma endregion

	#pragma region Main Entry Point

	// Start the workers for the test.
	for (int i = 0; i < nExplorersToSimulate; i++)
	{
		simulators[i].Initialize(i);
	}


	// Wait for user input to stop the test.
	std::cout << "Press any key to end the test.";
	std::cin.ignore();

	// Stop the test workers.
	for (int i = 0; i < nExplorersToSimulate; i++)
	{
		simulators[i].Terminate();
	}

	return 0;

	#pragma endregion


	// Automatic variables

	//&&&&&&&&&&&
	//int nToReturn = 0;
	//IDispatch *pDisp;
	//IUnknown *pUnknown;
	HRESULT hResult;
	WCHAR nameSyncing[](L"sdlkfja;sldkjf a;sldkfj a;sldkfj a;sldkfj as;ldkfj asd;lfkj asdl;fkaj sdfl;aksdj fa;sldkjfda"); 
	//DISPID dispIdSyncing;
	//UINT uiTypeInfoCount = 8;
	//ITypeInfo *ppTInfo;
	//LP_MyVtbl *lpVtbl;
	int nOverlayIndex = 2;
	DWORD dwFlags = 0;
	//LPOLESTR oleString = L"THis is an OLESTRING";
	//	
	//CComBSTR comBstr(L"GetPriority");

    hResult = CoInitializeEx(NULL, COINIT_MULTITHREADED);
	BadgeCOMLib::IBadgeIconFailedPtr pSyncing(__uuidof(BadgeCOMLib::BadgeIconFailed));

	// Call the BadgeCom initialization function (GetOverlayInfo()) via the VTable.
	hResult = pSyncing->raw_GetOverlayInfo(nameSyncing, 0, &nOverlayIndex, &dwFlags);
	hResult = pSyncing->raw_GetPriority(&nOverlayIndex);
	hResult = pSyncing->raw_IsMemberOf(nameSyncing, 0);

	return 0;
}

