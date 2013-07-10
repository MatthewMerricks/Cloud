// BadgeComTestComSide.cpp : Defines the entry point for the console application.
//

#define MAIN_MODULE
#include "stdafx.h"

// Debug trace
#define CLTRACE(intPriority, szFormat, ...) Trace::getInstance()->write(intPriority, szFormat, __VA_ARGS__)


std::wstring ConvertInt2String(int nNumber2Convert);
void FillPathArray(std::wstring leadIn, std::wstring paths[nMaxItemsAtLevel][nMaxItemsAtLevel][nMaxItemsAtLevel]);

int _tmain(int argc, _TCHAR* argv[])
{

	#pragma region Automatic Variables

	CExplorerSimulator *pSimulators[nExplorersToSimulate][nMaxBadgeTypeToSimulate];

	#pragma endregion

	#pragma region Main Entry Point

	// Fill the in-syncbox strings
	FillPathArray(L"C:\\Users\\robertste\\Cloud\\", pathsInSyncbox);
	FillPathArray(L"C:\\Users\\robertste\\CloudX\\", pathsOutOfSyncbox);

	// Initialize the random generator
	srand(time(0));

	// Start the workers for the test.
	for (int indexExplorer = 0; indexExplorer < nExplorersToSimulate; indexExplorer++)
	{
		for (int indexBadgeType = cloudAppBadgeSynced; indexBadgeType <= nMaxBadgeTypeToSimulate; indexBadgeType++)
		{
			CLTRACE(9, "BadgeComTestComSide: _tmain: Construct CExplorerSimulator for indexExplorer: %d, indexBadgeType: %d.", indexExplorer, indexBadgeType);
			pSimulators[indexExplorer][indexBadgeType - 1] = new CExplorerSimulator();
			pSimulators[indexExplorer][indexBadgeType - 1]->Initialize(indexExplorer, indexBadgeType);
			CLTRACE(9, "BadgeComTestComSide: _tmain: After Initialize CExplorerSimulator for indexExplorer: %d, indexBadgeType: %d.", indexExplorer, indexBadgeType);
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
			CLTRACE(9, "BadgeComTestComSide: _tmain: Delete CExplorerSimulator for indexExplorer: %d, indexBadgeType: %d.", indexExplorer, indexBadgeType);
			delete pSimulators[indexExplorer][indexBadgeType - 1];
			CLTRACE(9, "BadgeComTestComSide: _tmain: After delete CExplorerSimulator for indexExplorer: %d, indexBadgeType: %d.", indexExplorer, indexBadgeType);
			pSimulators[indexExplorer][indexBadgeType - 1] = NULL;
		}
	}

	// Output the statistics.
	std::cout << "Statistics:" << std::endl;
	std::cout << "    Total queries:" << g_ulIsMemberOfQueryTotalCount << std::endl;
	std::cout << "    Total positive queries:" << g_ulIsMemberOfQueryTotalCountTrue << std::endl;
	std::cout << "    Total negative queries:" << g_ulIsMemberOfQueryTotalCountFalse << std::endl;
	std::cout << std::endl;
	std::cout << "    Total Synced queries:" << g_ulIsMemberOfQuerySyncedTotalCount << std::endl;
	std::cout << "    Total Synced positive queries:" << g_ulIsMemberOfQuerySyncedTotalCountTrue << std::endl;
	std::cout << "    Total Synced negative queries:" << g_ulIsMemberOfQuerySyncedTotalCountFalse << std::endl;
	std::cout << std::endl;
	std::cout << "    Total Syncing queries:" << g_ulIsMemberOfQuerySyncingTotalCount << std::endl;
	std::cout << "    Total Syncing positive queries:" << g_ulIsMemberOfQuerySyncingTotalCountTrue << std::endl;
	std::cout << "    Total Syncing negative queries:" << g_ulIsMemberOfQuerySyncingTotalCountFalse << std::endl;
	std::cout << std::endl;
	std::cout << "    Total Failed queries:" << g_ulIsMemberOfQueryFailedTotalCount << std::endl;
	std::cout << "    Total Failed positive queries:" << g_ulIsMemberOfQueryFailedTotalCountTrue << std::endl;
	std::cout << "    Total Failed negative queries:" << g_ulIsMemberOfQueryFailedTotalCountFalse << std::endl;
	std::cout << std::endl;
	std::cout << "    Total Selective queries:" << g_ulIsMemberOfQuerySelectiveTotalCount << std::endl;
	std::cout << "    Total Selective positive queries:" << g_ulIsMemberOfQuerySelectiveTotalCountTrue << std::endl;
	std::cout << "    Total Selective negative queries:" << g_ulIsMemberOfQuerySelectiveTotalCountFalse << std::endl;
	std::cout << "Press any key to continue....";
	std::cin.ignore();

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

