//
// GuidDefinitions.h
// Cloud Windows COM
//
// Created By BobS.
// Copyright (c) Cloud.com. All rights reserved.

#pragma once

// Change the following definitions each time the BadgeCom version changes.  This will insure that each BadgeCom.dll is registered independently, 
// and we won't over-install a version of BadgeCom that an older app may be using.
#define EncloseInBraces(X) { ## X ## }
#define QQHelper(X) #X
#define QQ(X) QQHelper(X)
#define PrependLHelper(X) L ## X
#define PrependL(X) PrependLHelper(X)

#define Def_TypeLib_Name L"TypeLibGuid"
#define Def_TypeLib_RawGuid D4B831FC-0BA1-435C-B853-4ED074E8AAE1
#define Def_TypeLib_GuidInBraces EncloseInBraces(Def_TypeLib_RawGuid)
#define Def_TypeLib_GuidInBracesWithQuotes QQ(Def_TypeLib_GuidInBraces)
#define Def_TypeLib_Guid PrependL(Def_TypeLib_GuidInBracesWithQuotes)
#define Def_TypeLib_Uuid uuid( ## Def_TypeLib_RawGuid ## )

#define Def_InterfaceIconSynced_Name L"InterfaceIconSyncedGuid"
#define Def_InterfaceIconSynced_RawGuid 3CF80A7B-CA4D-4F64-8709-6DF8D4CD73AA
#define Def_InterfaceIconSynced_GuidInBraces EncloseInBraces(Def_InterfaceIconSynced_RawGuid)
#define Def_InterfaceIconSynced_GuidInBracesWithQuotes QQ(Def_InterfaceIconSynced_GuidInBraces)
#define Def_InterfaceIconSynced_Guid PrependL(Def_InterfaceIconSynced_GuidInBracesWithQuotes)
#define Def_InterfaceIconSynced_Uuid uuid( ## Def_InterfaceIconSynced_RawGuid ## )

#define Def_InterfaceIconSyncing_Name L"InterfaceIconSyncingGuid"
#define Def_InterfaceIconSyncing_RawGuid 4DFF171C-9964-414F-B215-EBA287BC3BED
#define Def_InterfaceIconSyncing_GuidInBraces EncloseInBraces(Def_InterfaceIconSyncing_RawGuid)
#define Def_InterfaceIconSyncing_GuidInBracesWithQuotes QQ(Def_InterfaceIconSyncing_GuidInBraces)
#define Def_InterfaceIconSyncing_Guid PrependL(Def_InterfaceIconSyncing_GuidInBracesWithQuotes)
#define Def_InterfaceIconSyncing_Uuid uuid( ## Def_InterfaceIconSyncing_RawGuid ## )

#define Def_InterfaceIconFailed_Name L"InterfaceIconFailedGuid"
#define Def_InterfaceIconFailed_RawGuid E6A56B59-677D-42F0-8170-61D879D4B017
#define Def_InterfaceIconFailed_GuidInBraces EncloseInBraces(Def_InterfaceIconFailed_RawGuid)
#define Def_InterfaceIconFailed_GuidInBracesWithQuotes QQ(Def_InterfaceIconFailed_GuidInBraces)
#define Def_InterfaceIconFailed_Guid PrependL(Def_InterfaceIconFailed_GuidInBracesWithQuotes)
#define Def_InterfaceIconFailed_Uuid uuid( ## Def_InterfaceIconFailed_RawGuid ## )

#define Def_InterfaceIconSelective_Name L"InterfaceIconSelectiveGuid"
#define Def_InterfaceIconSelective_RawGuid 4ADBD9FB-8F3F-435D-88DF-7FE05BF5152C
#define Def_InterfaceIconSelective_GuidInBraces EncloseInBraces(Def_InterfaceIconSelective_RawGuid)
#define Def_InterfaceIconSelective_GuidInBracesWithQuotes QQ(Def_InterfaceIconSelective_GuidInBraces)
#define Def_InterfaceIconSelective_Guid PrependL(Def_InterfaceIconSelective_GuidInBracesWithQuotes)
#define Def_InterfaceIconSelective_Uuid uuid( ## Def_InterfaceIconSelective_RawGuid ## )

#define Def_InterfacePubSubServer_Name L"InterfacePubSubServerGuid"
#define Def_InterfacePubSubServer_RawGuid 77E63152-9162-4B24-BEEF-3E1585DDA526
#define Def_InterfacePubSubServer_GuidInBraces EncloseInBraces(Def_InterfacePubSubServer_RawGuid)
#define Def_InterfacePubSubServer_GuidInBracesWithQuotes QQ(Def_InterfacePubSubServer_GuidInBraces)
#define Def_InterfacePubSubServer_Guid PrependL(Def_InterfacePubSubServer_GuidInBracesWithQuotes)
#define Def_InterfacePubSubServer_Uuid uuid( ## Def_InterfacePubSubServer_RawGuid ## )

#define Def_ClassIconSynced_Name L"ClassIconSyncedGuid"
#define Def_ClassIconSynced_RawGuid BE3E4C27-80B4-4D2B-AE48-98EF5B3E30B9
#define Def_ClassIconSynced_GuidInBraces EncloseInBraces(Def_ClassIconSynced_RawGuid)
#define Def_ClassIconSynced_GuidInBracesWithQuotes QQ(Def_ClassIconSynced_GuidInBraces)
#define Def_ClassIconSynced_Guid PrependL(Def_ClassIconSynced_GuidInBracesWithQuotes)
#define Def_ClassIconSynced_Uuid uuid( ## Def_ClassIconSynced_RawGuid ## )

#define Def_ClassIconSyncing_Name L"ClassIconSyncingGuid"
#define Def_ClassIconSyncing_RawGuid 16661249-1973-4EB1-A444-86DEA1E47318
#define Def_ClassIconSyncing_GuidInBraces EncloseInBraces(Def_ClassIconSyncing_RawGuid)
#define Def_ClassIconSyncing_GuidInBracesWithQuotes QQ(Def_ClassIconSyncing_GuidInBraces)
#define Def_ClassIconSyncing_Guid PrependL(Def_ClassIconSyncing_GuidInBracesWithQuotes)
#define Def_ClassIconSyncing_Uuid uuid( ## Def_ClassIconSyncing_RawGuid ## )

#define Def_ClassIconFailed_Name L"ClassIconFailedGuid"
#define Def_ClassIconFailed_RawGuid 1691D7B0-617C-45D5-B59E-ED1399FFA0AD
#define Def_ClassIconFailed_GuidInBraces EncloseInBraces(Def_ClassIconFailed_RawGuid)
#define Def_ClassIconFailed_GuidInBracesWithQuotes QQ(Def_ClassIconFailed_GuidInBraces)
#define Def_ClassIconFailed_Guid PrependL(Def_ClassIconFailed_GuidInBracesWithQuotes)
#define Def_ClassIconFailed_Uuid uuid( ## Def_ClassIconFailed_RawGuid ## )

#define Def_ClassIconSelective_Name L"ClassIconSelectiveGuid"
#define Def_ClassIconSelective_RawGuid F5478C98-85CE-4C3F-9D82-293B58B5AFB9
#define Def_ClassIconSelective_GuidInBraces EncloseInBraces(Def_ClassIconSelective_RawGuid)
#define Def_ClassIconSelective_GuidInBracesWithQuotes QQ(Def_ClassIconSelective_GuidInBraces)
#define Def_ClassIconSelective_Guid PrependL(Def_ClassIconSelective_GuidInBracesWithQuotes)
#define Def_ClassIconSelective_Uuid uuid( ## Def_ClassIconSelective_RawGuid ## )

#define Def_ClassPubSubServer_Name L"ClassPubSubServerGuid"
#define Def_ClassPubSubServer_RawGuid AC925C3A-63C7-49F2-9977-FBB4980E2188
#define Def_ClassPubSubServer_GuidInBraces EncloseInBraces(Def_ClassPubSubServer_RawGuid)
#define Def_ClassPubSubServer_GuidInBracesWithQuotes QQ(Def_ClassPubSubServer_GuidInBraces)
#define Def_ClassPubSubServer_Guid PrependL(Def_ClassPubSubServer_GuidInBracesWithQuotes)
#define Def_ClassPubSubServer_Uuid uuid( ## Def_ClassPubSubServer_RawGuid ## )
