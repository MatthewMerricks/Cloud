

/* this ALWAYS GENERATED file contains the definitions for the interfaces */


 /* File created by MIDL compiler version 8.00.0594 */
/* at Tue Jun 05 18:06:28 2012
 */
/* Compiler settings for BadgeCOM.idl:
    Oicf, W1, Zp8, env=Win32 (32b run), target_arch=X86 8.00.0594 
    protocol : dce , ms_ext, c_ext, robust
    error checks: allocation ref bounds_check enum stub_data 
    VC __declspec() decoration level: 
         __declspec(uuid()), __declspec(selectany), __declspec(novtable)
         DECLSPEC_UUID(), MIDL_INTERFACE()
*/
/* @@MIDL_FILE_HEADING(  ) */

#pragma warning( disable: 4049 )  /* more than 64k source lines */


/* verify that the <rpcndr.h> version is high enough to compile this file*/
#ifndef __REQUIRED_RPCNDR_H_VERSION__
#define __REQUIRED_RPCNDR_H_VERSION__ 475
#endif

#include "rpc.h"
#include "rpcndr.h"

#ifndef __RPCNDR_H_VERSION__
#error this stub requires an updated version of <rpcndr.h>
#endif // __RPCNDR_H_VERSION__

#ifndef COM_NO_WINDOWS_H
#include "windows.h"
#include "ole2.h"
#endif /*COM_NO_WINDOWS_H*/

#ifndef __BadgeCOM_i_h__
#define __BadgeCOM_i_h__

#if defined(_MSC_VER) && (_MSC_VER >= 1020)
#pragma once
#endif

/* Forward Declarations */ 

#ifndef __IBadgeIconSyncing_FWD_DEFINED__
#define __IBadgeIconSyncing_FWD_DEFINED__
typedef interface IBadgeIconSyncing IBadgeIconSyncing;

#endif 	/* __IBadgeIconSyncing_FWD_DEFINED__ */


#ifndef __IBadgeIconSynced_FWD_DEFINED__
#define __IBadgeIconSynced_FWD_DEFINED__
typedef interface IBadgeIconSynced IBadgeIconSynced;

#endif 	/* __IBadgeIconSynced_FWD_DEFINED__ */


#ifndef __IBadgeIconSelective_FWD_DEFINED__
#define __IBadgeIconSelective_FWD_DEFINED__
typedef interface IBadgeIconSelective IBadgeIconSelective;

#endif 	/* __IBadgeIconSelective_FWD_DEFINED__ */


#ifndef __IBadgeIconFailed_FWD_DEFINED__
#define __IBadgeIconFailed_FWD_DEFINED__
typedef interface IBadgeIconFailed IBadgeIconFailed;

#endif 	/* __IBadgeIconFailed_FWD_DEFINED__ */


#ifndef __BadgeIconSyncing_FWD_DEFINED__
#define __BadgeIconSyncing_FWD_DEFINED__

#ifdef __cplusplus
typedef class BadgeIconSyncing BadgeIconSyncing;
#else
typedef struct BadgeIconSyncing BadgeIconSyncing;
#endif /* __cplusplus */

#endif 	/* __BadgeIconSyncing_FWD_DEFINED__ */


#ifndef __BadgeIconSynced_FWD_DEFINED__
#define __BadgeIconSynced_FWD_DEFINED__

#ifdef __cplusplus
typedef class BadgeIconSynced BadgeIconSynced;
#else
typedef struct BadgeIconSynced BadgeIconSynced;
#endif /* __cplusplus */

#endif 	/* __BadgeIconSynced_FWD_DEFINED__ */


#ifndef __BadgeIconSelective_FWD_DEFINED__
#define __BadgeIconSelective_FWD_DEFINED__

#ifdef __cplusplus
typedef class BadgeIconSelective BadgeIconSelective;
#else
typedef struct BadgeIconSelective BadgeIconSelective;
#endif /* __cplusplus */

#endif 	/* __BadgeIconSelective_FWD_DEFINED__ */


#ifndef __BadgeIconFailed_FWD_DEFINED__
#define __BadgeIconFailed_FWD_DEFINED__

#ifdef __cplusplus
typedef class BadgeIconFailed BadgeIconFailed;
#else
typedef struct BadgeIconFailed BadgeIconFailed;
#endif /* __cplusplus */

#endif 	/* __BadgeIconFailed_FWD_DEFINED__ */


/* header files for imported files */
#include "oaidl.h"
#include "ocidl.h"
#include "shobjidl.h"

#ifdef __cplusplus
extern "C"{
#endif 


#ifndef __IBadgeIconSyncing_INTERFACE_DEFINED__
#define __IBadgeIconSyncing_INTERFACE_DEFINED__

/* interface IBadgeIconSyncing */
/* [unique][nonextensible][dual][uuid][object] */ 


EXTERN_C const IID IID_IBadgeIconSyncing;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("46991EC7-7E83-4E3A-8E21-757792BBA5C4")
    IBadgeIconSyncing : public IDispatch
    {
    public:
    };
    
    
#else 	/* C style interface */

    typedef struct IBadgeIconSyncingVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IBadgeIconSyncing * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IBadgeIconSyncing * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IBadgeIconSyncing * This);
        
        HRESULT ( STDMETHODCALLTYPE *GetTypeInfoCount )( 
            IBadgeIconSyncing * This,
            /* [out] */ UINT *pctinfo);
        
        HRESULT ( STDMETHODCALLTYPE *GetTypeInfo )( 
            IBadgeIconSyncing * This,
            /* [in] */ UINT iTInfo,
            /* [in] */ LCID lcid,
            /* [out] */ ITypeInfo **ppTInfo);
        
        HRESULT ( STDMETHODCALLTYPE *GetIDsOfNames )( 
            IBadgeIconSyncing * This,
            /* [in] */ REFIID riid,
            /* [size_is][in] */ LPOLESTR *rgszNames,
            /* [range][in] */ UINT cNames,
            /* [in] */ LCID lcid,
            /* [size_is][out] */ DISPID *rgDispId);
        
        /* [local] */ HRESULT ( STDMETHODCALLTYPE *Invoke )( 
            IBadgeIconSyncing * This,
            /* [annotation][in] */ 
            _In_  DISPID dispIdMember,
            /* [annotation][in] */ 
            _In_  REFIID riid,
            /* [annotation][in] */ 
            _In_  LCID lcid,
            /* [annotation][in] */ 
            _In_  WORD wFlags,
            /* [annotation][out][in] */ 
            _In_  DISPPARAMS *pDispParams,
            /* [annotation][out] */ 
            _Out_opt_  VARIANT *pVarResult,
            /* [annotation][out] */ 
            _Out_opt_  EXCEPINFO *pExcepInfo,
            /* [annotation][out] */ 
            _Out_opt_  UINT *puArgErr);
        
        END_INTERFACE
    } IBadgeIconSyncingVtbl;

    interface IBadgeIconSyncing
    {
        CONST_VTBL struct IBadgeIconSyncingVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IBadgeIconSyncing_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define IBadgeIconSyncing_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define IBadgeIconSyncing_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define IBadgeIconSyncing_GetTypeInfoCount(This,pctinfo)	\
    ( (This)->lpVtbl -> GetTypeInfoCount(This,pctinfo) ) 

#define IBadgeIconSyncing_GetTypeInfo(This,iTInfo,lcid,ppTInfo)	\
    ( (This)->lpVtbl -> GetTypeInfo(This,iTInfo,lcid,ppTInfo) ) 

#define IBadgeIconSyncing_GetIDsOfNames(This,riid,rgszNames,cNames,lcid,rgDispId)	\
    ( (This)->lpVtbl -> GetIDsOfNames(This,riid,rgszNames,cNames,lcid,rgDispId) ) 

#define IBadgeIconSyncing_Invoke(This,dispIdMember,riid,lcid,wFlags,pDispParams,pVarResult,pExcepInfo,puArgErr)	\
    ( (This)->lpVtbl -> Invoke(This,dispIdMember,riid,lcid,wFlags,pDispParams,pVarResult,pExcepInfo,puArgErr) ) 


#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __IBadgeIconSyncing_INTERFACE_DEFINED__ */


#ifndef __IBadgeIconSynced_INTERFACE_DEFINED__
#define __IBadgeIconSynced_INTERFACE_DEFINED__

/* interface IBadgeIconSynced */
/* [unique][nonextensible][dual][uuid][object] */ 


EXTERN_C const IID IID_IBadgeIconSynced;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("0CC774F1-545A-4129-8B09-8655C5370F54")
    IBadgeIconSynced : public IDispatch
    {
    public:
    };
    
    
#else 	/* C style interface */

    typedef struct IBadgeIconSyncedVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IBadgeIconSynced * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IBadgeIconSynced * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IBadgeIconSynced * This);
        
        HRESULT ( STDMETHODCALLTYPE *GetTypeInfoCount )( 
            IBadgeIconSynced * This,
            /* [out] */ UINT *pctinfo);
        
        HRESULT ( STDMETHODCALLTYPE *GetTypeInfo )( 
            IBadgeIconSynced * This,
            /* [in] */ UINT iTInfo,
            /* [in] */ LCID lcid,
            /* [out] */ ITypeInfo **ppTInfo);
        
        HRESULT ( STDMETHODCALLTYPE *GetIDsOfNames )( 
            IBadgeIconSynced * This,
            /* [in] */ REFIID riid,
            /* [size_is][in] */ LPOLESTR *rgszNames,
            /* [range][in] */ UINT cNames,
            /* [in] */ LCID lcid,
            /* [size_is][out] */ DISPID *rgDispId);
        
        /* [local] */ HRESULT ( STDMETHODCALLTYPE *Invoke )( 
            IBadgeIconSynced * This,
            /* [annotation][in] */ 
            _In_  DISPID dispIdMember,
            /* [annotation][in] */ 
            _In_  REFIID riid,
            /* [annotation][in] */ 
            _In_  LCID lcid,
            /* [annotation][in] */ 
            _In_  WORD wFlags,
            /* [annotation][out][in] */ 
            _In_  DISPPARAMS *pDispParams,
            /* [annotation][out] */ 
            _Out_opt_  VARIANT *pVarResult,
            /* [annotation][out] */ 
            _Out_opt_  EXCEPINFO *pExcepInfo,
            /* [annotation][out] */ 
            _Out_opt_  UINT *puArgErr);
        
        END_INTERFACE
    } IBadgeIconSyncedVtbl;

    interface IBadgeIconSynced
    {
        CONST_VTBL struct IBadgeIconSyncedVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IBadgeIconSynced_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define IBadgeIconSynced_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define IBadgeIconSynced_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define IBadgeIconSynced_GetTypeInfoCount(This,pctinfo)	\
    ( (This)->lpVtbl -> GetTypeInfoCount(This,pctinfo) ) 

#define IBadgeIconSynced_GetTypeInfo(This,iTInfo,lcid,ppTInfo)	\
    ( (This)->lpVtbl -> GetTypeInfo(This,iTInfo,lcid,ppTInfo) ) 

#define IBadgeIconSynced_GetIDsOfNames(This,riid,rgszNames,cNames,lcid,rgDispId)	\
    ( (This)->lpVtbl -> GetIDsOfNames(This,riid,rgszNames,cNames,lcid,rgDispId) ) 

#define IBadgeIconSynced_Invoke(This,dispIdMember,riid,lcid,wFlags,pDispParams,pVarResult,pExcepInfo,puArgErr)	\
    ( (This)->lpVtbl -> Invoke(This,dispIdMember,riid,lcid,wFlags,pDispParams,pVarResult,pExcepInfo,puArgErr) ) 


#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __IBadgeIconSynced_INTERFACE_DEFINED__ */


#ifndef __IBadgeIconSelective_INTERFACE_DEFINED__
#define __IBadgeIconSelective_INTERFACE_DEFINED__

/* interface IBadgeIconSelective */
/* [unique][nonextensible][dual][uuid][object] */ 


EXTERN_C const IID IID_IBadgeIconSelective;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("77D3D99A-1EF6-41F7-A247-229939ADC1D9")
    IBadgeIconSelective : public IDispatch
    {
    public:
    };
    
    
#else 	/* C style interface */

    typedef struct IBadgeIconSelectiveVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IBadgeIconSelective * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IBadgeIconSelective * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IBadgeIconSelective * This);
        
        HRESULT ( STDMETHODCALLTYPE *GetTypeInfoCount )( 
            IBadgeIconSelective * This,
            /* [out] */ UINT *pctinfo);
        
        HRESULT ( STDMETHODCALLTYPE *GetTypeInfo )( 
            IBadgeIconSelective * This,
            /* [in] */ UINT iTInfo,
            /* [in] */ LCID lcid,
            /* [out] */ ITypeInfo **ppTInfo);
        
        HRESULT ( STDMETHODCALLTYPE *GetIDsOfNames )( 
            IBadgeIconSelective * This,
            /* [in] */ REFIID riid,
            /* [size_is][in] */ LPOLESTR *rgszNames,
            /* [range][in] */ UINT cNames,
            /* [in] */ LCID lcid,
            /* [size_is][out] */ DISPID *rgDispId);
        
        /* [local] */ HRESULT ( STDMETHODCALLTYPE *Invoke )( 
            IBadgeIconSelective * This,
            /* [annotation][in] */ 
            _In_  DISPID dispIdMember,
            /* [annotation][in] */ 
            _In_  REFIID riid,
            /* [annotation][in] */ 
            _In_  LCID lcid,
            /* [annotation][in] */ 
            _In_  WORD wFlags,
            /* [annotation][out][in] */ 
            _In_  DISPPARAMS *pDispParams,
            /* [annotation][out] */ 
            _Out_opt_  VARIANT *pVarResult,
            /* [annotation][out] */ 
            _Out_opt_  EXCEPINFO *pExcepInfo,
            /* [annotation][out] */ 
            _Out_opt_  UINT *puArgErr);
        
        END_INTERFACE
    } IBadgeIconSelectiveVtbl;

    interface IBadgeIconSelective
    {
        CONST_VTBL struct IBadgeIconSelectiveVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IBadgeIconSelective_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define IBadgeIconSelective_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define IBadgeIconSelective_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define IBadgeIconSelective_GetTypeInfoCount(This,pctinfo)	\
    ( (This)->lpVtbl -> GetTypeInfoCount(This,pctinfo) ) 

#define IBadgeIconSelective_GetTypeInfo(This,iTInfo,lcid,ppTInfo)	\
    ( (This)->lpVtbl -> GetTypeInfo(This,iTInfo,lcid,ppTInfo) ) 

#define IBadgeIconSelective_GetIDsOfNames(This,riid,rgszNames,cNames,lcid,rgDispId)	\
    ( (This)->lpVtbl -> GetIDsOfNames(This,riid,rgszNames,cNames,lcid,rgDispId) ) 

#define IBadgeIconSelective_Invoke(This,dispIdMember,riid,lcid,wFlags,pDispParams,pVarResult,pExcepInfo,puArgErr)	\
    ( (This)->lpVtbl -> Invoke(This,dispIdMember,riid,lcid,wFlags,pDispParams,pVarResult,pExcepInfo,puArgErr) ) 


#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __IBadgeIconSelective_INTERFACE_DEFINED__ */


#ifndef __IBadgeIconFailed_INTERFACE_DEFINED__
#define __IBadgeIconFailed_INTERFACE_DEFINED__

/* interface IBadgeIconFailed */
/* [unique][nonextensible][dual][uuid][object] */ 


EXTERN_C const IID IID_IBadgeIconFailed;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("2A204983-6DE7-42D2-BBB1-FCD5FCBD8026")
    IBadgeIconFailed : public IDispatch
    {
    public:
    };
    
    
#else 	/* C style interface */

    typedef struct IBadgeIconFailedVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IBadgeIconFailed * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IBadgeIconFailed * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IBadgeIconFailed * This);
        
        HRESULT ( STDMETHODCALLTYPE *GetTypeInfoCount )( 
            IBadgeIconFailed * This,
            /* [out] */ UINT *pctinfo);
        
        HRESULT ( STDMETHODCALLTYPE *GetTypeInfo )( 
            IBadgeIconFailed * This,
            /* [in] */ UINT iTInfo,
            /* [in] */ LCID lcid,
            /* [out] */ ITypeInfo **ppTInfo);
        
        HRESULT ( STDMETHODCALLTYPE *GetIDsOfNames )( 
            IBadgeIconFailed * This,
            /* [in] */ REFIID riid,
            /* [size_is][in] */ LPOLESTR *rgszNames,
            /* [range][in] */ UINT cNames,
            /* [in] */ LCID lcid,
            /* [size_is][out] */ DISPID *rgDispId);
        
        /* [local] */ HRESULT ( STDMETHODCALLTYPE *Invoke )( 
            IBadgeIconFailed * This,
            /* [annotation][in] */ 
            _In_  DISPID dispIdMember,
            /* [annotation][in] */ 
            _In_  REFIID riid,
            /* [annotation][in] */ 
            _In_  LCID lcid,
            /* [annotation][in] */ 
            _In_  WORD wFlags,
            /* [annotation][out][in] */ 
            _In_  DISPPARAMS *pDispParams,
            /* [annotation][out] */ 
            _Out_opt_  VARIANT *pVarResult,
            /* [annotation][out] */ 
            _Out_opt_  EXCEPINFO *pExcepInfo,
            /* [annotation][out] */ 
            _Out_opt_  UINT *puArgErr);
        
        END_INTERFACE
    } IBadgeIconFailedVtbl;

    interface IBadgeIconFailed
    {
        CONST_VTBL struct IBadgeIconFailedVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IBadgeIconFailed_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define IBadgeIconFailed_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define IBadgeIconFailed_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define IBadgeIconFailed_GetTypeInfoCount(This,pctinfo)	\
    ( (This)->lpVtbl -> GetTypeInfoCount(This,pctinfo) ) 

#define IBadgeIconFailed_GetTypeInfo(This,iTInfo,lcid,ppTInfo)	\
    ( (This)->lpVtbl -> GetTypeInfo(This,iTInfo,lcid,ppTInfo) ) 

#define IBadgeIconFailed_GetIDsOfNames(This,riid,rgszNames,cNames,lcid,rgDispId)	\
    ( (This)->lpVtbl -> GetIDsOfNames(This,riid,rgszNames,cNames,lcid,rgDispId) ) 

#define IBadgeIconFailed_Invoke(This,dispIdMember,riid,lcid,wFlags,pDispParams,pVarResult,pExcepInfo,puArgErr)	\
    ( (This)->lpVtbl -> Invoke(This,dispIdMember,riid,lcid,wFlags,pDispParams,pVarResult,pExcepInfo,puArgErr) ) 


#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __IBadgeIconFailed_INTERFACE_DEFINED__ */



#ifndef __BadgeCOMLib_LIBRARY_DEFINED__
#define __BadgeCOMLib_LIBRARY_DEFINED__

/* library BadgeCOMLib */
/* [version][uuid] */ 


EXTERN_C const IID LIBID_BadgeCOMLib;

EXTERN_C const CLSID CLSID_BadgeIconSyncing;

#ifdef __cplusplus

class DECLSPEC_UUID("37E0ECAE-BEA1-4096-9D84-440A7C9AC9E4")
BadgeIconSyncing;
#endif

EXTERN_C const CLSID CLSID_BadgeIconSynced;

#ifdef __cplusplus

class DECLSPEC_UUID("16FDC851-D9B0-4591-B54A-9A50D58226CC")
BadgeIconSynced;
#endif

EXTERN_C const CLSID CLSID_BadgeIconSelective;

#ifdef __cplusplus

class DECLSPEC_UUID("A46EB4D6-B7F7-4255-912F-C151D78E26F8")
BadgeIconSelective;
#endif

EXTERN_C const CLSID CLSID_BadgeIconFailed;

#ifdef __cplusplus

class DECLSPEC_UUID("49C685C9-CD72-4588-8800-2B30C49723A1")
BadgeIconFailed;
#endif
#endif /* __BadgeCOMLib_LIBRARY_DEFINED__ */

/* Additional Prototypes for ALL interfaces */

/* end of Additional Prototypes */

#ifdef __cplusplus
}
#endif

#endif


