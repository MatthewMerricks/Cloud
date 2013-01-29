//
// BadgeIconSupport.h
// Cloud Windows COM
//
// Created By BobS.
// Copyright (c) Cloud.com. All rights reserved.


#include "BadgeCOM_i.h"
#include <boost\unordered_map.hpp>
#include <boost\unordered\unordered_set.hpp>
#include <boost\signal.hpp>
#include "BoostSemaphore.h"
#include <Windows.h>
#include <stdio.h>
#include <sstream>
#include "lmcons.h"
#include "Trace.h"

using namespace std;

// The value member of the badge dictionary.
typedef struct _DATAFORBADGEPATH
{
    EnumCloudAppIconBadgeType badgeType;                            // the type of this badge  (cloudAppBadgeNone for a root folder, otherwise one of the four other types)
    boost::unordered_set<ULONG> processesThatAddedThisBadge;        // set of process IDs that have added this badge.
} DATAFORBADGEPATH, *P_DATAFORBADGEPATH;