// stdafx.h : include file for standard system include files,
// or project specific include files that are used frequently, but
// are changed infrequently
//

#pragma once

#include "targetver.h"

#include <stdio.h>
#include <tchar.h>
#include <iostream>


#ifndef STRICT
#define STRICT
#endif

#include "targetver.h"

#define _ATL_FREE_THREADED

#define _ATL_NO_AUTOMATIC_NAMESPACE

#define _ATL_CSTRING_EXPLICIT_CONSTRUCTORS	// some CString constructors will be explicit


#define ATL_NO_ASSERT_ON_DESTROY_NONEXISTENT_WINDOW

#include <atlbase.h>
#include <atlstr.h>
#include <atlcom.h>
#include <atlctl.h>
#include <comdef.h>

// Imports
#import "C:\Cloud\CloudSDK-Windows\3rdParty\bin\Release64\BadgeCom.tlb" named_guids 

// TODO: reference additional headers your program requires here
#include <ShlObj.h>
#include "GlobalDefinitions.h"
#include <boost\signal.hpp>
#include <boost\thread.hpp>
#include "Trace.h"
#include "CExplorerSimulator.h"

