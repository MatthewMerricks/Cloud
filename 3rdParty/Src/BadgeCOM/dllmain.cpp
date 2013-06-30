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

CBadgeCOMModule _AtlModule;

HINSTANCE g_hBadgeComInstance = NULL;

// This is the list of substituteable parameters to pass to the .rgs files.  There are five GUIDs that we
// should change each time the version of BadgeCom.dll is changed.  That is to prevent removing and replacing
// old BadgeCom.dlls that might be in use by apps based on older versions of the SDK.
ATL::_ATL_REGMAP_ENTRY RegEntries[] = 
{ 
	{ OLESTR("TypeLibGuid"), L"{312A39BF-EBC2-479E-A953-68FFBF9FB949}"},
	{ OLESTR("IconSyncedGuid"), L"{88060121-879A-451C-A1C1-A1FD6DDFC00A}"},
	{ OLESTR("IconSyncingGuid"), L"{C7254266-AFF7-4E10-9C1C-ED5F529A89D7}"},
	{ OLESTR("IconFailedGuid"), L"{87174DC8-6847-429F-B92B-155870D7A41D}"},
	{ OLESTR("IconSelectiveGuid"), L"{BF95811A-D7E7-4BA6-98AA-E1ECBBE1F71A}"},
	{ OLESTR("PubSubServerGuid"), L"{746113DC-BED4-4A7E-A285-2E64C32864D6}"},
	{ NULL, NULL} 
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
