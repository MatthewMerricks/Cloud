//
// dllmain.h
// Cloud Windows COM
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

// dllmain.h : Declaration of module class.

class CBadgeCOMModule : public ATL::CAtlDllModuleT< CBadgeCOMModule >
{
public :
	DECLARE_LIBID(LIBID_BadgeCOMLib)
	DECLARE_REGISTRY_APPID_RESOURCEID(IDR_BADGECOM, "{2CB206B7-817E-4EF2-8DF4-BF10440DCD3E}")
};

extern class CBadgeCOMModule _AtlModule;
