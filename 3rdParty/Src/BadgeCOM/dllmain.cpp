//
// dllmain.cpp
// Cloud Windows COM
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

// dllmain.cpp : Implementation of DllMain.

#include "stdafx.h"
#include "resource.h"
#include "BadgeCOM_i.h"
#include "dllmain.h"
#include "GuidDefinitions.h"

CBadgeCOMModule _AtlModule;

HINSTANCE g_hBadgeComInstance = NULL;

// This is the list of substituteable parameters to pass to the .rgs files.  There are five GUIDs that we
// should change each time the version of BadgeCom.dll is changed.  That is to prevent removing and replacing
// old BadgeCom.dlls that might be in use by apps based on older versions of the SDK.
ATL::_ATL_REGMAP_ENTRY RegEntries[] = 
{ 
	{Def_TypeLib_Name, Def_TypeLib_Guid},
	{Def_InterfaceIconSynced_Name, Def_InterfaceIconSynced_Guid},
	{Def_InterfaceIconSyncing_Name, Def_InterfaceIconSyncing_Guid},
	{Def_InterfaceIconFailed_Name, Def_InterfaceIconFailed_Guid},
	{Def_InterfaceIconSelective_Name, Def_InterfaceIconSelective_Guid},
	{Def_InterfacePubSubServer_Name, Def_InterfacePubSubServer_Guid},
	{NULL, NULL} 
};

// DLL Entry Point
extern "C" BOOL WINAPI DllMain(HINSTANCE hInstance, DWORD dwReason, LPVOID lpReserved)
{
	switch (dwReason)
    {
        case DLL_PROCESS_ATTACH:
            g_hBadgeComInstance = hInstance;
            break;
    }

	return _AtlModule.DllMain(dwReason, lpReserved); 
}
