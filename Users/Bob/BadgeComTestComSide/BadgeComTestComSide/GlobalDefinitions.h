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

	unsigned long g_ulIsMemberOfQuerySyncedTotalCount;
	unsigned long g_ulIsMemberOfQuerySyncedTotalCountFalse;
	unsigned long g_ulIsMemberOfQuerySyncedTotalCountTrue;

	unsigned long g_ulIsMemberOfQuerySyncingTotalCount;
	unsigned long g_ulIsMemberOfQuerySyncingTotalCountFalse;
	unsigned long g_ulIsMemberOfQuerySyncingTotalCountTrue;

	unsigned long g_ulIsMemberOfQueryFailedTotalCount;
	unsigned long g_ulIsMemberOfQueryFailedTotalCountFalse;
	unsigned long g_ulIsMemberOfQueryFailedTotalCountTrue;

	unsigned long g_ulIsMemberOfQuerySelectiveTotalCount;
	unsigned long g_ulIsMemberOfQuerySelectiveTotalCountFalse;
	unsigned long g_ulIsMemberOfQuerySelectiveTotalCountTrue;
#else
	extern std::wstring pathsInSyncbox[nMaxItemsAtLevel][nMaxItemsAtLevel][nMaxItemsAtLevel];
	extern std::wstring pathsOutOfSyncbox[nMaxItemsAtLevel][nMaxItemsAtLevel][nMaxItemsAtLevel];
	extern unsigned long g_ulIsMemberOfQueryTotalCount;
	extern unsigned long g_ulIsMemberOfQueryTotalCountFalse;
	extern unsigned long g_ulIsMemberOfQueryTotalCountTrue;

	extern unsigned long g_ulIsMemberOfQuerySyncedTotalCount;
	extern unsigned long g_ulIsMemberOfQuerySyncedTotalCountFalse;
	extern unsigned long g_ulIsMemberOfQuerySyncedTotalCountTrue;

	extern unsigned long g_ulIsMemberOfQuerySyncingTotalCount;
	extern unsigned long g_ulIsMemberOfQuerySyncingTotalCountFalse;
	extern unsigned long g_ulIsMemberOfQuerySyncingTotalCountTrue;

	extern unsigned long g_ulIsMemberOfQueryFailedTotalCount;
	extern unsigned long g_ulIsMemberOfQueryFailedTotalCountFalse;
	extern unsigned long g_ulIsMemberOfQueryFailedTotalCountTrue;

	extern unsigned long g_ulIsMemberOfQuerySelectiveTotalCount;
	extern unsigned long g_ulIsMemberOfQuerySelectiveTotalCountFalse;
	extern unsigned long g_ulIsMemberOfQuerySelectiveTotalCountTrue;
#endif





