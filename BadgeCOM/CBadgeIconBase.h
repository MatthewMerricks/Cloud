//
// CBadgeIconBase.h
// Cloud Windows COM
//
// Created By BobS.
// Copyright (c) Cloud.com. All rights reserved.

#pragma once
#include "resource.h"       // main symbols
#include "BadgeCOM_i.h"
#include "CBadgeNetPubSubEvents.h"
#include "PubSubServer.h"
#include <boost\unordered_map.hpp>
#include <boost\unordered\unordered_set.hpp>
#include <Windows.h>
#include <ShlObj.h>
#include <stdio.h>
#include <sstream>
#include "lmcons.h"
#include "Trace.h"
#include <tchar.h>
#include <psapi.h>

using namespace ATL;

// CBadgeIconBase
class CBadgeIconBase
{
public:
    // Constructor/destructor
	CBadgeIconBase();
    CBadgeIconBase(int iconIndex, EnumCloudAppIconBadgeType badgeType);
    ~CBadgeIconBase();
	
    // Public methods
	// IShellIconOverlayIdentifier Base Methods.  These are called from the four COM interface modules (e.g.: BadgeIconSynced.cpp).
    HRESULT GetOverlayInfo(LPWSTR pwszIconFile, int cchMax,int *pIndex,DWORD* pdwFlags);
    HRESULT GetPriority(int* pPriority);
    HRESULT IsMemberOf(LPCWSTR pwszPath,DWORD dwAttrib);

private:
    // Private definitions
    // The value member of the badge dictionary.
    typedef struct _DATAFORBADGEPATH
    {
        EnumCloudAppIconBadgeType badgeType;                            // the type of this badge  (cloudAppBadgeNone for a root folder, otherwise one of the four other types)
        boost::unordered_map<uint64_t, boost::unordered_set<GUID>> processesThatAddedThisBadge;        // dictionary of process IDs that have badged this path with this badge type.  Each process ID is associated with multiple Syncbox GUIDs (in that process) that badged this path with this badge type.
    } DATA_FOR_BADGE_PATH, *P_DATA_FOR_BADGE_PATH;

    // Private fields
    int _iconIndex;                             // designates the index of the icon to use for this instance.
    EnumCloudAppIconBadgeType _badgeType;       // designates the badge type to use for this instance.

    CBadgeNetPubSubEvents *_pBadgeNetPubSubEvents;
    boost::unordered_map<std::wstring, DATA_FOR_BADGE_PATH> _mapBadges;             // the dictionary of fullPath->(badgeType, unordered_map<PublisherProcessId, unordered_set<PublisherSyncboxId>>) for the badges.
    boost::unordered_map<std::wstring, DATA_FOR_BADGE_PATH> _mapRootFolders;        // the dictionary of fullPath->(badgeType, unordered_map<PublisherProcessId, unordered_set<PublisherSyncboxId>>) for the Syncbox root folders.
    boost::unordered_set<uint64_t> _setActiveProcessIds;                               // a set of active process ids.
    HANDLE _threadSubscriptionRestart;
    BOOL _fIsInitialized;
    GUID _guidPublisher;
    std::string _strBaseBadgeType;
    boost::recursive_mutex _mutexBadgeDatabase;

    // Private methods
    void OnEventAddBadgePath(BSTR fullPath, EnumCloudAppIconBadgeType badgeType, ULONG processId, GUID guidPublisher);
    BOOL OnEventRemoveBadgePath(BSTR fullPath, ULONG processId, GUID guidPublisher);
    void OnEventAddSyncboxFolderPath(BSTR fullPath, EnumCloudAppIconBadgeType badgeType, ULONG processId, GUID guidPublisher);
    void OnEventRemoveSyncboxFolderPath(BSTR fullPath, EnumCloudAppIconBadgeType badgeType, ULONG processId, GUID guidPublisher);
    void OnEventSubscriptionWatcherFailed();
    void OnEventTimerTick();
    std::wstring NormalizePath(std::wstring inPath);
    BOOL IsPathInRootPath(std::wstring testPath, std::wstring rootPath);
    static void SubscriptionRestartThreadProc(LPVOID pUserState);
    void InitializeBadgeNetPubSubEvents();
    void InitializeBadgeNetPubSubEventsViaThread();
    static void InitializeBadgeNetPubSubEventsThreadProc(LPVOID pUserState);
    static char *BadgeTypeToString(EnumCloudAppIconBadgeType badgeType);
    void CleanBadgingDatabaseForProcessId(ULONG processIdPublisher);
	int GetCurrentProcessNameAndProcessId(CHAR *szOutProcessName, int nOutProcessNameBufferSize, DWORD *dwOutProcessId);
};
