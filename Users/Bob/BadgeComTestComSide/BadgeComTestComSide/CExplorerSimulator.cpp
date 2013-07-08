//
// CExplorerSimulator.cpp
// Cloud Windows COM
//
// Created By BobS.
// Copyright (c) Cloud.com. All rights reserved.

#include "StdAfx.h"
#include "CExplorerSimulator.h"


// Debug trace
#define CLTRACE(intPriority, szFormat, ...) Trace::getInstance()->write(intPriority, szFormat, __VA_ARGS__)

/// <summary>
/// Constructor
/// </summary>
CExplorerSimulator::CExplorerSimulator(void)
{
    try
    {
        // Initialize private fields
		CLTRACE(9, "CExplorerSimulator: CExplorerSimulator: Entry.");
		_nExplorerIndex = -1;											// simulated Explorer index
		_handleIconFailed = NULL;
		_handleIconSynced = NULL;
		_handleIconSyncing = NULL;
		_handleIconSelective = NULL;
		_fRequestExit = false;
		_hr = NULL;
		_pathIconFileFailed[0] = NULL;
		_pathIconFileSynced[0] = NULL;
		_pathIconFileSyncing[0] = NULL;
		_pathIconFileSelective[0] = NULL;

        // Initialize the COM system
        _hr = CoInitializeEx(NULL, COINIT_MULTITHREADED);
		if (_hr != S_OK)
		{
			CLTRACE(9, "CExplorerSimulator: CExplorerSimulator: ERROR: CoInitialize returned %d.", _hr);
			return;
		}

        // Create a class factory to instantiate an instance of CPubSubServer.
        IClassFactory *pIClassFactory = NULL;
		CLTRACE(9, "CExplorerSimulator: CExplorerSimulator: Call CoGetClassObject.");
        hr = CoGetClassObject(CLSID_PubSubServer, CLSCTX_ALL, NULL, IID_IClassFactory, (LPVOID *)&pIClassFactory);
        if (SUCCEEDED(hr) && pIClassFactory != NULL)
        {
            // Instantiate an instance of CPubSubServer
		    CLTRACE(9, "CExplorerSimulator: CExplorerSimulator: Call CreateInstance."); 
            hr = pIClassFactory->CreateInstance(NULL, IID_IPubSubServer, (LPVOID *)&_pPubSubServer);
            if (!SUCCEEDED(hr) || _pPubSubServer == NULL)
            {
    		    CLTRACE(1, "CExplorerSimulator: CExplorerSimulator: ERROR.  PubSubServer not instantiated. Throw."); 
                throw new std::exception("Error creating an instance of CPubSubServer");
            }

            // Release the class factory
  		    CLTRACE(9, "CExplorerSimulator: CExplorerSimulator: Allocated _pPubSubServer: %p.", _pPubSubServer); 
            pIClassFactory->Release();
            pIClassFactory = NULL;
        }
        else
        {
    		CLTRACE(1, "CExplorerSimulator: CExplorerSimulator: ERROR.  Creating class factory. Throw."); 
            throw new std::exception("Error creating a class factory for CPubSubServer.  hr: %d.", hr);
        }
    }
    catch (const std::exception &ex)
    {
		CLTRACE(1, "CExplorerSimulator: CExplorerSimulator: ERROR: Exception.  Message: %s.", ex.what());
    }
    catch (...)
    {
		CLTRACE(1, "CExplorerSimulator: CExplorerSimulator: ERROR: C++ exception.");
    }
	CLTRACE(9, "CExplorerSimulator: CExplorerSimulator: Exit."); 
}


/// <summary>
/// Destructor
/// </summary>
CExplorerSimulator::~CExplorerSimulator(void)
{
    // Kill both threads
    try
    {
		CLTRACE(9, "CExplorerSimulator: ~CExplorerSimulator: Entry.");
		_fRequestSubscribingThreadExit = true;              // preemptive strike
		_fRequestWorkerThreadExit = true;
        _fTerminating = true;
        KillSubscribingThread();
        KillWatchingThread();
    }
    catch (const std::exception &ex)
    {
		CLTRACE(1, "CExplorerSimulator: ~CExplorerSimulator: ERROR: Exception. Killing threads. Message: %s.", ex.what());
    }
    catch (...)
    {
		CLTRACE(1, "CExplorerSimulator: ~CExplorerSimulator: ERROR: C++ exception.");
    }

    try
    {
        // Free the CPubSubServer COM object
        if (_pPubSubServer != NULL)
        {
			_pPubSubServer->Terminate();
            _pPubSubServer->Release();
            CoUninitialize();
            _pPubSubServer = NULL;
        }
    }
    catch (const std::exception &ex)
    {
		CLTRACE(1, "CExplorerSimulator: ~CExplorerSimulator: ERROR: Exception(2).  Message: %s.", ex.what());
    }
    catch (...)
    {
		CLTRACE(1, "CExplorerSimulator: ~CExplorerSimulator: ERROR: C++ exception(2).");
    }
}

/// <summary>
/// Initialize.  Initialize the PubSubEventsServer.
/// </summary>
void CExplorerSimulator::Initialize(int nSimulatedExplorerIndex)
{
    try
    {
		CLTRACE(9, "CExplorerSimulator: Initialize: Entry. Index: %d.", nSimulatedExplorerIndex);
    }
    catch (const std::exception &ex)
    {
		CLTRACE(1, "CExplorerSimulator: Initialize: ERROR: Exception.  Message: %s.", ex.what());
    }
    catch (...)
    {
		CLTRACE(1, "CExplorerSimulator: Initialize: ERROR: C++ exception.");
    }
}

/// <summary>
/// Subscribe and pull events from any of the BadgeCom instance threads.  
/// </summary>
void CExplorerSimulator::SubscribingThreadProc(LPVOID pUserState)
{
    // Cast the user state to an object instance
	bool fLockHeld = false;
    CExplorerSimulator *pThis = (CExplorerSimulator *)pUserState;

    try
    {
        BOOL fSemaphoreReleased = false;

		CLTRACE(9, "CExplorerSimulator: SubscribingThreadProc: Entry.");
		if (pUserState == NULL)
		{
    		CLTRACE(1, "CExplorerSimulator: SubscribingThreadProc: ERROR: User state is NULL.");
            throw new std::exception("pUserState must not be NULL");
		}

        // Loop waiting for events.
        while (true)
        {
            // Exit if we have been requested.
            if (pThis->_fRequestSubscribingThreadExit || pThis->_fTerminating)
            {
				CLTRACE(9, "CExplorerSimulator: SubscribingThreadProc: Requested to exit.  Break out of loop.");
                break;
            }

            // Create or open this subcription.  Since the GUID is unique, this will create the subscription on the first call.
            EnumPubSubServerSubscribeReturnCodes result;
            EnumEventSubType eventSubType;
            EnumCloudAppIconBadgeType badgeType;
            BSTR bsFullPath;                                // allocated by Subscribe.  Must be freed eventually (SysFreeString()).
            ULONG processIdPublisher;
            GUID guidPublisher;
            HRESULT hr = pThis->_pPubSubServer->Subscribe(BadgeNet_To_BadgeCom, pThis->_guidSubscriber, _knSubscriptionTimeoutMs, &eventSubType, &badgeType, &bsFullPath, &processIdPublisher, &guidPublisher, &result);
            if (!SUCCEEDED(hr))
            {
        		CLTRACE(1, "CExplorerSimulator: SubscribingThreadProc: ERROR: From Subscribe.  hr: %d.", hr);
                throw new std::exception("Error from Subscribe");
            }
            if (result == RC_SUBSCRIBE_GOT_EVENT)
            {
                switch (eventSubType)
                {
                    case BadgeNet_AddSyncboxFolderPath:
						try
						{
                            CLTRACE(9, "CExplorerSimulator: SubscribingThreadProc: Event: BadgeNet_AddSyncboxFolderPath. Path: <%ls>. BadgeType: %d.", bsFullPath, badgeType);
		                    pThis->FireEventAddSyncboxFolderPath(bsFullPath, badgeType, processIdPublisher, guidPublisher);
						}
						catch (const std::exception &ex)
						{
							CLTRACE(1, "CExplorerSimulator: SubscribingThreadProc: ERROR: Exception(10).  Message: %s.", ex.what());
						}
                        catch (...)
                        {
		                    CLTRACE(1, "CExplorerSimulator: SubscribingThreadProc: ERROR: C++ exception(10).");
                        }
                        break;
                    case BadgeNet_RemoveSyncboxFolderPath:
						try
						{
	        		        CLTRACE(9, "CExplorerSimulator: SubscribingThreadProc: Event: BadgeNet_RemoveSyncboxFolderPath. Path: <%ls>. BadgeType: %d.", bsFullPath, badgeType);
		                    pThis->FireEventRemoveSyncboxFolderPath(bsFullPath, badgeType, processIdPublisher, guidPublisher);
						}
						catch (const std::exception &ex)
						{
							CLTRACE(1, "CExplorerSimulator: SubscribingThreadProc: ERROR: Exception(11).  Message: %s.", ex.what());
						}
                        catch (...)
                        {
		                    CLTRACE(1, "CExplorerSimulator: SubscribingThreadProc: ERROR: C++ exception(11).");
                        }
                        break;
                    case BadgeNet_AddBadgePath:
						try
						{
	        		        CLTRACE(9, "CExplorerSimulator: SubscribingThreadProc: Event: BadgeNet_AddBadgePath. Path: <%ls>. BadgeType: %d.", bsFullPath, badgeType);
		                    pThis->FireEventAddBadgePath(bsFullPath, badgeType, processIdPublisher, guidPublisher);
						}
						catch (const std::exception &ex)
						{
							CLTRACE(1, "CExplorerSimulator: SubscribingThreadProc: ERROR: Exception(12).  Message: %s.", ex.what());
						}
                        catch (...)
                        {
		                    CLTRACE(1, "CExplorerSimulator: SubscribingThreadProc: ERROR: C++ exception(12).");
                        }
                        break;
                    case BadgeNet_RemoveBadgePath:
						try
						{
	        		        CLTRACE(9, "CExplorerSimulator: SubscribingThreadProc: Event: BadgeNet_RemoveBadgePath. Path: <%ls>.", bsFullPath);
		                    pThis->FireEventRemoveBadgePath(bsFullPath, processIdPublisher, guidPublisher);
						}
						catch (const std::exception &ex)
						{
							CLTRACE(1, "CExplorerSimulator: SubscribingThreadProc: ERROR: Exception(13).  Message: %s.", ex.what());
						}
                        catch (...)
                        {
		                    CLTRACE(1, "CExplorerSimulator: SubscribingThreadProc: ERROR: C++ exception(13).");
                        }
                        break;
                }

				// Free the out parameter BSTR allocated by the COM server.
				SysFreeString(bsFullPath);

                // Exit if we were requested to kill ourselves.
                if (pThis->_fRequestSubscribingThreadExit || pThis->_fTerminating)
                {
				    CLTRACE(1, "CExplorerSimulator: SubscribingThreadProc: Requested to exit (3).  Break out of loop.");
                    break;
                }
            }
            else if (result == RC_SUBSCRIBE_ERROR)
            {
        		CLTRACE(1, "CExplorerSimulator: SubscribingThreadProc: ERROR: From Subscribe.");
                break;
            }
            else if (result == RC_SUBSCRIBE_CANCELLED)
            {
        		CLTRACE(1, "CExplorerSimulator: SubscribingThreadProc: This subscription was cancelled.");
                break;
            }
            else
            {
                // result is RC_SUBSCRIBE_TRY_AGAIN (an event is ready) or RC_SUBSCRIBE_TIMED_OUT (normal cycling).
                // Exit if we were requested to kill ourselves.
                if (pThis->_fRequestSubscribingThreadExit || pThis->_fTerminating)
                {
				    CLTRACE(1, "CExplorerSimulator: SubscribingThreadProc: Requested to exit (2).  Break out of loop.");
                    break;
                }
            }

            // We're alive
            pThis->_locker.lock();
            {
				fLockHeld = true;
                pThis->_isSubscriberThreadAlive = true;

                if (!fSemaphoreReleased)
                {
				    CLTRACE(9, "CExplorerSimulator: SubscribingThreadProc: Subscribed. Post starting thread.");
                    fSemaphoreReleased = true;
                    pThis->_semWaitForSubscriptionThreadStart.signal();
                }
            }
            pThis->_locker.unlock();
			fLockHeld = false;
        }
    }
    catch (const std::exception &ex)
    {
		if (fLockHeld)
		{
	        pThis->_locker.unlock();
			fLockHeld = false;
		}
		CLTRACE(1, "CExplorerSimulator: SubscribingThreadProc: ERROR: Exception.  Message: %s.", ex.what());
    }
    catch (...)
    {
		if (fLockHeld)
		{
	        pThis->_locker.unlock();
			fLockHeld = false;
		}
		CLTRACE(1, "CExplorerSimulator: SubscribingThreadProc: ERROR: C++ exception.");
    }
	CLTRACE(1, "CExplorerSimulator: SubscribingThreadProc: Exit the thread.");
}

/// <summary>
/// The subscribing thread may get stuck waiting on an event if the BadgeCom process is killed.  Monitor the subscribing thread for activity.
/// If no activity is detected, kill the subscribing thread and attempt to restart it.
/// </summary>
void CExplorerSimulator::WorkerThreadProc(LPVOID pUserState)
{
    // Cast the user state to an object instance
	CLTRACE(9, "CExplorerSimulator: WorkerThreadProc: Entry.");
    CExplorerSimulator *pThis = (CExplorerSimulator *)pUserState;
	bool fLockHeld = false;

    try
    {
		if (pUserState == NULL)
		{
    		CLTRACE(1, "CExplorerSimulator: WorkerThreadProc: ERROR: User state is NULL.");
			throw new std::exception("pUserState must not be NULL");
		}

        BOOL fRestartSubscribingThread;
        while (true)
        {
            // Wait letting the subscribing thread work.
			CLTRACE(9, "CExplorerSimulator: WorkerThreadProc: Wait until the next look.");

        }
    }
    catch (const std::exception &ex)
    {
		if (fLockHeld)
		{
	        pThis->_locker.unlock();
			fLockHeld = false;
		}
		CLTRACE(1, "CExplorerSimulator: WorkerThreadProc: ERROR: Exception.  Message: %s.", ex.what());
    }
    catch (...)
    {
		if (fLockHeld)
		{
	        pThis->_locker.unlock();
			fLockHeld = false;
		}
		CLTRACE(1, "CExplorerSimulator: WorkerThreadProc: ERROR: C++ exception.");
    }
	CLTRACE(9, "CExplorerSimulator: WorkerThreadProc: Watchin thread exit.");
}

/// <summary>
/// Handle watching thread exception.
/// </summary>
void CExplorerSimulator::HandleWatchingThreadException(CExplorerSimulator *pThis)
{
    try
    {
		CLTRACE(1, "CExplorerSimulator: HandleWatchingThreadException: Entry.");
		if (pThis != NULL  && !pThis->_fRequestWorkerThreadExit && !pThis->_fTerminating)
		{
			CLTRACE(9, "CExplorerSimulator: HandleWatchingThreadException: Fire event FireEventSubscriptionWatcherFailed.");
		    pThis->FireEventSubscriptionWatcherFailed();                // notify the delegates
			CLTRACE(9, "CExplorerSimulator: HandleWatchingThreadException: Back from firing event FireEventSubscriptionWatcherFailed.");

			pThis->KillSubscribingThread();
		}
    }
    catch (const std::exception &ex)
    {
		CLTRACE(1, "CExplorerSimulator: HandleWatchingThreadException: ERROR: Exception.  Message: %s.", ex.what());
    }
    catch (...)
    {
		CLTRACE(1, "CExplorerSimulator: HandleWatchingThreadException: ERROR: C++ exception.");
    }
}


/// <summary>
/// Start the worker thread.
/// </summary>
/// <returns>BOOL: true: Thread start OK.</returns>
void CExplorerSimulator::StartWorkerThread()
{
    BOOL result = false;
	bool fLockHeld = false;

    try
    {
		CLTRACE(9, "CExplorerSimulator: StartSubscribingThread: Entry.");

        _locker.lock();
        {
            // Clear any status this thread might have.
			fLockHeld = true;
            _fRequestWorkerThreadExit = false;

            // Start a thread to simulate Explorer working with BadgeCom for this particular badge type.
            DWORD dwThreadId;
            HANDLE handle = CreateThread(NULL,                              // default security
                        0,                                                  // default stack size
                        (LPTHREAD_START_ROUTINE)&WorkerThreadProc,          // function to run
                        (LPVOID) this,                                      // thread parameter
                        0,                                                  // imediately run the thread
                        &dwThreadId                                         // output thread ID
                        );
            if (handle == NULL)
            {
                throw new std::exception("Error creating thread");
            }

            _threadWorkerHandle = handle;
        }
        _locker.unlock();
		fLockHeld = false;

    }
    catch (const std::exception &ex)
    {
		if (fLockHeld)
		{
	        _locker.unlock();
			fLockHeld = false;
		}
		CLTRACE(1, "CExplorerSimulator: StartWorkerThread: ERROR: Exception.  Message: %s.", ex.what());
    }
    catch (...)
    {
		if (fLockHeld)
		{
	        _locker.unlock();
			fLockHeld = false;
		}
		CLTRACE(1, "CExplorerSimulator: StartWorkerThread: ERROR: C++ exception.");
    }
}


