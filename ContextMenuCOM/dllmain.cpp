//
// dllmain.cpp
// Cloud Windows COM
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

// dllmain.cpp : Implementation of DllMain.

#include "stdafx.h"
#include "resource.h"
#include "ContextMenuCOM_i.h"
#include "dllmain.h"

CContextMenuCOMModule _AtlModule;

HINSTANCE g_hContextMenuComInstance = NULL;

// DLL Entry Point
extern "C" BOOL WINAPI DllMain(HINSTANCE hInstance, DWORD dwReason, LPVOID lpReserved)
{
	switch (dwReason)
    {
        case DLL_PROCESS_ATTACH:
            g_hContextMenuComInstance = hInstance;
            break;
    }

	return _AtlModule.DllMain(dwReason, lpReserved); 
}
