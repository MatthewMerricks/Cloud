//
// dllmain.h
// Cloud Windows COM
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

// dllmain.h : Declaration of module class.

#pragma once

class CBadgeCOMModule : public ATL::CAtlDllModuleT< CBadgeCOMModule >
{
public :
	DECLARE_LIBID(LIBID_BadgeCOMLib)
};

extern CBadgeCOMModule _AtlModule;
extern ATL::_ATL_REGMAP_ENTRY RegEntries[];
