//
// BadgeIconSupport.cpp
// Cloud Windows COM
//
// Created By BobS.
// Copyright (c) Cloud.com. All rights reserved.

#include "stdafx.h"
#include "BadgeIconSupport.h"

// Debug trace
#define CLTRACE(intPriority, szFormat, ...) Trace::getInstance()->write(intPriority, szFormat, __VA_ARGS__)

// CBadgeIconSupport

/// <summary>
/// Add a badge to the badging dictionary under a lock.  This function maintains the badge type of the badge, and the list of processes that have added this badge.
/// Note: Multiple processes may have added a badge
/// </summary>
/// <param name="pLocker *">A pointer to the lock to synchronize on.</param>
/// <param name="pBadgeDictionaryguidSubscriber">A pointer to the badge dictionary.param>
/// <param name="pathToAdd">The full path representing the file or folder to badge.</param>
/// <param name="badgeType">The type of the badge.</param>
/// <param name="processId">The process ID of the process that added the badge.</param>
void AddBadgeToDictionary(boost::mutex *pLocker, boost::unordered_map<std::wstring, DATAFORBADGEPATH> *pBadgeDictionary, std::wstring pathToAdd, EnumCloudAppIconBadgeType badgeType, ULONG processId)
{

}

/// <summary>
/// Remove a badge from the badging dictionary under a lock.
/// </summary>
/// <param name="pLocker *">A pointer to the lock to synchronize on.</param>
/// <param name="pBadgeDictionaryguidSubscriber">A pointer to the badge dictionary.param>
/// <param name="pathToAdd">The full path representing the file or folder to badge.</param>
/// <param name="badgeType">The type of the badge.</param>
/// <param name="processId">The process ID of the process that added the badge.</param>
void RemoveBadgeFromDictionary(boost::mutex *pLocker, boost::unordered_map<std::wstring, DATAFORBADGEPATH> *pBadgeDictionary, std::wstring pathToAdd, EnumCloudAppIconBadgeType badgeType, ULONG processId)
{

}


 //       o RemoveBadgeFromDictionary(&mutex, &boost::unordered_map<std::wstring fullPath, DataForBadgePath>, std::wstring pathToRemove, EnumCloudAppIconBadgeType badgeType, ULONG processID)
        //o ShouldPathBeBadged(&mutex, &boost::unordered_map<std::wstring fullPath, DataForBadgePath>, std::wstring pathToCheck, EnumCloudAppIconBadgeType badgeType)
        //o CheckAndRemoveDeadProcesses(&mutex, &boost::unordered_set<ULONG processId>)
