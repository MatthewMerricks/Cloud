//
// dllmain.h
// Cloud Windows COM
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

// dllmain.h : Declaration of module class.

class CContextMenuCOMModule : public ATL::CAtlDllModuleT< CContextMenuCOMModule >
{
public :
	DECLARE_LIBID(LIBID_ContextMenuCOMLib)
};

extern class CContextMenuCOMModule _AtlModule;
