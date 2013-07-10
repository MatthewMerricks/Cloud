//
// GlobalDefinitions.h
// Cloud Windows COM
//
// Created By BobS.
// Copyright (c) Cloud.com. All rights reserved.

#pragma once

	// Definitions
	enum EnumCloudAppIconBadgeType
	{
		cloudAppBadgeNone=0,
	    cloudAppBadgeSynced,
	    cloudAppBadgeSyncing,
	    cloudAppBadgeFailed,
	    cloudAppBadgeSelective
	} ;

	const int nMaxItemsAtLevel = 10;
	const int nExplorersToSimulate = 4;
	const int nMaxBadgeTypeToSimulate = cloudAppBadgeSelective;


#ifdef MAIN_MODULE
	std::wstring pathsInSyncbox[nMaxItemsAtLevel][nMaxItemsAtLevel][nMaxItemsAtLevel];
	std::wstring pathsOutOfSyncbox[nMaxItemsAtLevel][nMaxItemsAtLevel][nMaxItemsAtLevel];
	unsigned long g_ulIsMemberOfQueryTotalCount;
	unsigned long g_ulIsMemberOfQueryTotalCountFalse;
	unsigned long g_ulIsMemberOfQueryTotalCountTrue;
#else
	extern std::wstring pathsInSyncbox[nMaxItemsAtLevel][nMaxItemsAtLevel][nMaxItemsAtLevel];
	extern std::wstring pathsOutOfSyncbox[nMaxItemsAtLevel][nMaxItemsAtLevel][nMaxItemsAtLevel];
	extern unsigned long g_ulIsMemberOfQueryTotalCount;
	extern unsigned long g_ulIsMemberOfQueryTotalCountFalse;
	extern unsigned long g_ulIsMemberOfQueryTotalCountTrue;
#endif





