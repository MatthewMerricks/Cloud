// BadgeComTestComSide.cpp : Defines the entry point for the console application.
//

#define MAIN_MODULE
#include "stdafx.h"


std::wstring ConvertInt2String(int nNumber2Convert);
void FillPathArray(std::wstring leadIn, std::wstring paths[nMaxItemsAtLevel][nMaxItemsAtLevel][nMaxItemsAtLevel]);

int _tmain(int argc, _TCHAR* argv[])
{

	#pragma region Automatic Variables

	CExplorerSimulator *pSimulators[nExplorersToSimulate][nMaxBadgeTypeToSimulate];

	#pragma endregion

	#pragma region Main Entry Point

	// Fill the in-syncbox strings
	FillPathArray(L"C:\\Users\\Robertste\\Cloud\\", pathsInSyncbox);
	FillPathArray(L"C:\\Users\\Robertste\\CloudX\\", pathsOutOfSyncbox);

	// Initialize the random generator
	srand(time(0));

	// Start the workers for the test.
	for (int indexExplorer = 0; indexExplorer < nExplorersToSimulate; indexExplorer++)
	{
		for (int indexBadgeType = cloudAppBadgeSynced; indexBadgeType <= nMaxBadgeTypeToSimulate; indexBadgeType++)
		{
			pSimulators[indexExplorer][indexBadgeType - 1] = new CExplorerSimulator();
			pSimulators[indexExplorer][indexBadgeType - 1]->Initialize(indexExplorer, indexBadgeType);
		}
	}

	// Wait for user input to stop the test.
	std::cout << "Press any key to end the test.";
	std::cin.ignore();

	// Stop the test workers.
	for (int indexExplorer = 0; indexExplorer < nExplorersToSimulate; indexExplorer++)
	{
		for (int indexBadgeType = cloudAppBadgeSynced; indexBadgeType <= nMaxBadgeTypeToSimulate; indexBadgeType++)
		{
			pSimulators[indexExplorer][indexBadgeType - 1]->Terminate();
		}
	}

	return 0;

	#pragma endregion
}

void FillPathArray(std::wstring leadIn, std::wstring paths[nMaxItemsAtLevel][nMaxItemsAtLevel][nMaxItemsAtLevel])
{
	for (int index1 = 0; index1 < nMaxItemsAtLevel; index1++)
	{
		for (int index2 = 0; index2 < nMaxItemsAtLevel; index2++)
		{
			for (int index3 = 0; index3 < nMaxItemsAtLevel; index3++)
			{

				paths[index1][index2][index3] = leadIn + L"Level1_LongLongName_" + ConvertInt2String(index1) + L"\\" + L"Level2_LongLongName_" + ConvertInt2String(index2) + L"\\" + L"Level3_LongLongName_" + ConvertInt2String(index3) + L".txt";
			}
		}
	}
}

std::wstring ConvertInt2String(int nNumber2Convert)
{
	std::wstring toReturn = std::to_wstring((_Longlong)nNumber2Convert);
	return toReturn;
}

