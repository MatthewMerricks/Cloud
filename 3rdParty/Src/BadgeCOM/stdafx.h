//
// stdafx.h
// Cloud Windows COM
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

// stdafx.h : include file for standard system include files,
// or project specific include files that are used frequently,
// but are changed infrequently

#pragma once

//#define WAIT_FOR_DEBUG 1				// define to wait at initialization for debug attach in BadgeIconFailed, etc.
//#define DEBUG_ENABLE_ONLY_SYNCED_BADGING 1 // define to just exit at all calls except for synced badging (to simplify debugging)

#ifndef STRICT
#define STRICT
#endif

#include "targetver.h"

//#define _ATL_APARTMENT_THREADED           //@@@@@@@@@@@@@&&&&&&&&&&&&&
#define _ATL_FREE_THREADED

#define _ATL_NO_AUTOMATIC_NAMESPACE

#define _ATL_CSTRING_EXPLICIT_CONSTRUCTORS	// some CString constructors will be explicit


#define ATL_NO_ASSERT_ON_DESTROY_NONEXISTENT_WINDOW

#include "resource.h"
#include <atlbase.h>
#include <atlcom.h>
#include <atlctl.h>
#include <comdef.h>
#include <ShlObj.h>
#include <windows.h>