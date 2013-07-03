// BadgeComTestComSide.cpp : Defines the entry point for the console application.
//

#include "stdafx.h"
#import "C:\Cloud\CloudSDK-Windows\3rdParty\bin\Release64\BadgeCom.tlb" 

// {46991ec7-7e83-4e3a-8e21-757792bba5c4	}
DEFINE_GUID(CLSID_IBadgeIconSyncing, 0x46991ec7, 0x7e83, 0x4e3a, 0x8e, 0x21, 0x75, 0x77, 0x92, 0xbb, 0xa5, 0xc4);


typedef HRESULT STDMETHODCALLTYPE QueryInterfacePtr(void *, REFIID, void **);
typedef ULONG STDMETHODCALLTYPE AddRefPtr(void *);
typedef ULONG STDMETHODCALLTYPE ReleasePtr(void *);
typedef HRESULT STDMETHODCALLTYPE GetOverlayInfoPtr(void *vTbl, BSTR *pwszIconFile, int cchMax, int *pIndex, DWORD* pdwFlags);
typedef HRESULT STDMETHODCALLTYPE GetPriorityPtr(void *vTbl, int* pPriority);
typedef HRESULT STDMETHODCALLTYPE IsMemberOfPtr(void *vTbl, BSTR *pwszPath, DWORD dwAttrib);

typedef struct {
   // First 3 members must be called QueryInterface, AddRef, and Release
   QueryInterfacePtr  *QueryInterface;
   AddRefPtr          *AddRef;
   ReleasePtr         *Release;
   GetOverlayInfoPtr  *GetOverlayInfo;
   GetPriorityPtr     *GetPriority;
   IsMemberOfPtr      *IsMemberOf;
} MyVtbl, *LP_MyVtbl;

int _tmain(int argc, _TCHAR* argv[])
{
	int nToReturn = 0;
	IDispatch *pDisp;
	IUnknown *pUnknown;
	HRESULT hResult;
	LPWSTR nameSyncing; 
	DISPID dispIdSyncing;
	UINT uiTypeInfoCount = 8888;
	ITypeInfo *ppTInfo;
	LP_MyVtbl *lpVtbl;
	int nOverlayIndex = 2;
	unsigned long dwFlags = 0;
		
	CComBSTR comBstr(L"GetPriority");

    hResult = CoInitializeEx(NULL, COINIT_MULTITHREADED);
    BadgeCOMLib::IBadgeIconSyncingPtr pSyncing;	
	hResult = pSyncing.CreateInstance("BadgeCom.BadgeIconSyncing");
	hResult = pSyncing->GetTypeInfoCount(&uiTypeInfoCount);
	hResult = pSyncing->GetTypeInfo(0, NULL, &ppTInfo);
	//hResult = pSyncing->QueryInterface(IID_IDispatch, (void **)&pDisp);
	hResult = pSyncing->QueryInterface(CLSID_IBadgeIconSyncing, (void **)&lpVtbl);

	// Call the BadgeCom initialization function (GetOverlayInfo()) via the VTable.
	hResult = pSyncing->raw_GetOverlayInfo(nameSyncing, 0, &nOverlayIndex, &dwFlags);
	hResult = pSyncing->raw_GetPriority(&nOverlayIndex);
	hResult = pSyncing->raw_IsMemberOf(nameSyncing, 0);
	//hResult = (*lpVtbl)->GetOverlayInfo(pSyncing, &comBstr, 0, &nOverlayIndex, &dwFlags);
	//hResult = (*lpVtbl)->GetPriority(pSyncing, &nOverlayIndex);
	//hResult = (*lpVtbl)->IsMemberOf(pSyncing, &comBstr, 0);

	//hResult = pSyncing->GetIDsOfNames(IID_NULL, &nameSyncing, 1, GetUserDefaultLCID(), &dispIdSyncing);
		
	//hResult = CoCreateInstance(CLSID_IBadgeIconSyncing, NULL, CLSCTX_SERVER, IID_IUnknown, (void **)&pUnknown);
	//hResult = pUnknown->QueryInterface(IID_IDispatch, (void **)&pDisp);

	//// Load the 64-bit BadgeCom dll.
	//HMODULE dllHandle;
	//dllHandle = LoadLibrary(TEXT("C:\\Cloud\\CloudSDK-Windows\\CloudSdkSyncSample\\bin\\Release\\Amd64\\badgecom.dll"));
	//if (dllHandle == NULL)
	//{
	//	DWORD dwError = GetLastError();
	//	return -1;
	//}


	return nToReturn;
}

