//
// CBadgeNetPubSubEvents.cpp
// Cloud Windows COM
//
// Created By BobS.
// Copyright (c) Cloud.com. All rights reserved.

#include "StdAfx.h"
#include "CBadgeNetPubSubEvents.h"


// Debug trace
#ifdef _DEBUG
	//#define CLTRACE(intPriority, szFormat, ...) 
	#define CLTRACE(intPriority, szFormat, ...) Trace::getInstance()->write(intPriority, szFormat, __VA_ARGS__)
#else	
	#define CLTRACE(intPriority, szFormat, ...)
	//#define CLTRACE(intPriority, szFormat, ...) Trace::getInstance()->write(intPriority, szFormat, __VA_ARGS__)
#endif // _DEBUG



/// <summary>
/// Constructor
/// </summary>
CBadgeNetPubSubEvents::CBadgeNetPubSubEvents(void)
{
    try
    {
        // Initialize private fields
		CLTRACE(9, "CBadgeNetPubSubEvents: CBadgeNetPubSubEvents: Entry.");
        _pPubSubServer = NULL;
        _threadSubscribingHandle = NULL;
        _threadWatchingHandle = NULL;
        _isSubscriberThreadAlive = false;

        // Create a class factory to instantiate an instance of CPubSubServer.
        IClassFactory *pIClassFactory = NULL;
        HRESULT hr;
        hr = CoGetClassObject(CLSID_PubSubServer, CLSCTX_ALL, NULL, IID_IClassFactory, (LPVOID *)&pIClassFactory);
        if (SUCCEEDED(hr) && pIClassFactory != NULL)
        {
            // Instantiate an instance of CPubSubServer
            hr = pIClassFactory->CreateInstance(NULL, IID_IPubSubServer, (LPVOID *)&_pPubSubServer);
            if (!SUCCEEDED(hr) || _pPubSubServer == NULL)
            {
                throw new std::exception("Error creating an instance of CPubSubServer");
            }

            // Release the class factory
            pIClassFactory->Release();
            pIClassFactory = NULL;
        }
        else
        {
            throw new std::exception("Error creating a class factory for CPubSubServer.  hr: %d.", hr);
        }
    }
    catch (std::exception &ex)
    {
		CLTRACE(1, "CBadgeNetPubSubEvents: CBadgeNetPubSubEvents: ERROR: Exception.  Message: %s.", ex.what());
    }
}


/// <summary>
/// Destructor
/// </summary>
CBadgeNetPubSubEvents::~CBadgeNetPubSubEvents(void)
{
    // Kill both threads
    try
    {
		CLTRACE(9, "CBadgeNetPubSubEvents: ~CBadgeNetPubSubEvents: Entry.");
        KillSubscribingThread();
        KillWatchingThread();
    }
    catch (std::exception &ex)
    {
		CLTRACE(1, "CBadgeNetPubSubEvents: ~CBadgeNetPubSubEvents: ERROR: Exception. Killing threads. Message: %s.", ex.what());
    }

    try
    {
        // Free the CPubSubServer COM object
        if (_pPubSubServer != NULL)
        {
			_pPubSubServer->Terminate();
            _pPubSubServer->Release();
            _pPubSubServer = NULL;
        }
    }
    catch (std::exception &ex)
    {
		CLTRACE(1, "CBadgeNetPubSubEvents: ~CBadgeNetPubSubEvents: ERROR: Exception.  Message: %s.", ex.what());
    }
}

/// <summary>
/// Initialize.  Initialize the PubSubEventsServer.
/// </summary>
void CBadgeNetPubSubEvents::Initialize()
{
    try
    {
        if (_pPubSubServer == NULL)
        {
            throw new std::exception("Call Initialize() first");
        }

        // Initialize the PubSubServer
        HRESULT hr = _pPubSubServer->Initialize();
        if (!SUCCEEDED(hr))
        {
    		CLTRACE(1, "CBadgeNetPubSubEvents: Initialize: ERROR: From Initialize.  hr: %d.", hr);
        }
    }
    catch (std::exception &ex)
    {
		CLTRACE(1, "CBadgeNetPubSubEvents: Initialize: ERROR: Exception.  Message: %s.", ex.what());
    }
}

/// <summary>
/// Publish an event to BadgeNet
/// </summary>
void CBadgeNetPubSubEvents::PublishEventToBadgeNet(EnumEventType eventType, EnumEventSubType eventSubType, EnumCloudAppIconBadgeType badgeType, BSTR *fullPath)
{
    try
    {
		CLTRACE(9, "CBadgeNetPubSubEvents: PublishEventToBadgeNet: Entry. EventType: %d. EventSubType: %d. BadgeType: %d.", eventType, eventSubType, badgeType);
        if (_pPubSubServer == NULL || fullPath == NULL)
        {
            throw new std::exception("Call Initialize() first");
        }

        // Publish the event
        EnumPubSubServerPublishReturnCodes result;
        HRESULT hr = _pPubSubServer->Publish(eventType, eventSubType, badgeType, fullPath, &result);
        if (!SUCCEEDED(hr) || result != RC_PUBLISH_OK)
        {
    		CLTRACE(1, "CBadgeNetPubSubEvents: PublishEventToBadgeNet: ERROR: From Publish.  hr: %d. Result: %d.", hr, result);
        }
    }
    catch (std::exception &ex)
    {
		CLTRACE(1, "CBadgeNetPubSubEvents: PublishEventToBadgeNet: ERROR: Exception.  Message: %s.", ex.what());
    }
}

/// <summary>
/// Start a thread which will subscribe to events from BadgeNet.  
/// </summary>
void CBadgeNetPubSubEvents::SubscribeToBadgeNetEvents()
{
    // Start the threads.
	CLTRACE(9, "CBadgeNetPubSubEvents: SubscribeToBadgeNetEvents: Entry.");
    StartSubscribingThread();
    StartWatchingThread();
}

/// <summary>
/// Subscribe and pull events from any of the BadgeCom instance threads.  
/// </summary>
void CBadgeNetPubSubEvents::SubscribingThreadProc(LPVOID pUserState)
{
    try
    {
		CLTRACE(9, "CBadgeNetPubSubEvents: SubscribingThreadProc: Entry.");
		if (pUserState == NULL)
		{
    		CLTRACE(1, "CBadgeNetPubSubEvents: SubscribingThreadProc: ERROR: User state is NULL.");
            throw new std::exception("pUserState must not be NULL");
		}

        // Cast the user state to an object instance
        CBadgeNetPubSubEvents *pThis = (CBadgeNetPubSubEvents *)pUserState;

        // Generate a GUID to represent this subscription
        HRESULT hr = CoCreateGuid(&pThis->_guidSubscription);
        if (!SUCCEEDED(hr))
        {
    		CLTRACE(1, "CBadgeNetPubSubEvents: SubscribingThreadProc: ERROR: Creating GUID. hr: %d.", hr);
            throw new std::exception("Error creating GUID");
        }

        // Loop waiting for events.
        while (true)
        {
            // Create or open this subcription.  Since the GUID is unique, this will create the subscription on the first call.
            EnumPubSubServerSubscribeReturnCodes result;
            EnumEventSubType eventSubType;
            EnumCloudAppIconBadgeType badgeType;
            BSTR bsFullPath;                                // allocated by Subscribe.  Must be freed eventually (SysFreeString()).
            hr = pThis->_pPubSubServer->Subscribe(BadgeNet_To_BadgeCom, pThis->_guidSubscription, _kMillisecondsTimeoutSubscribingThread, &eventSubType, &badgeType, &bsFullPath, &result);
            if (!SUCCEEDED(hr))
            {
        		CLTRACE(1, "CBadgeNetPubSubEvents: SubscribingThreadProc: ERROR: From Subscribe.  hr: %d.", hr);
                throw new std::exception("Error from Subscribe");
            }
            if (result == RC_SUBSCRIBE_GOT_EVENT)
            {
                switch (eventSubType)
                {
                    case BadgeNet_AddSyncBoxFolderPath:
                        pThis->FireEventAddSyncBoxFolderPath(bsFullPath);
                        break;
                    case BadgeNet_RemoveSyncBoxFolderPath:
                        pThis->FireEventRemoveSyncBoxFolderPath(bsFullPath);
                        break;
                    case BadgeNet_AddBadgePath:
                        pThis->FireEventAddBadgePath(bsFullPath, badgeType);
                        break;
                    case BadgeNet_RemoveBadgePath:
                        pThis->FireEventRemoveBadgePath(bsFullPath);
                        break;
                }

				// Free the out parameter BSTR allocated by the COM server.
				SysFreeString(bsFullPath);
            }
            else if (result == RC_SUBSCRIBE_ERROR)
            {
        		CLTRACE(1, "CBadgeNetPubSubEvents: SubscribingThreadProc: ERROR: From Subscribe.  Result: %d.", result);
                break;
            }
            else if (result == RC_SUBSCRIBE_ALREADY_CANCELLED)
            {
        		CLTRACE(1, "CBadgeNetPubSubEvents: SubscribingThreadProc: Already Cancelled: From Subscribe.  Result: %d.", result);
                break;
            }

            // We're alive
            pThis->_locker.lock();
            {
                pThis->_isSubscriberThreadAlive = true;
            }
            pThis->_locker.unlock();
        }
    }
    catch (std::exception &ex)
    {
		CLTRACE(1, "CBadgeNetPubSubEvents: SubscribingThreadProc: ERROR: Exception.  Message: %s.", ex.what());
    }
}

/// <summary>
/// The subscribing thread may get stuck waiting on an event if the BadgeCom process is killed.  Monitor the subscribing thread for activity.
/// If no activity is detected, kill the subscribing thread and attempt to restart it.
/// </summary>
void CBadgeNetPubSubEvents::WatchingThreadProc(LPVOID pUserState)
{
    // Cast the user state to an object instance
	CLTRACE(9, "CBadgeNetPubSubEvents: WatchingThreadProc: Entry.");
    CBadgeNetPubSubEvents *pThis = (CBadgeNetPubSubEvents *)pUserState;

    try
    {
		if (pUserState == NULL)
		{
    		CLTRACE(1, "CBadgeNetPubSubEvents: WatchingThreadProc: ERROR: User state is NULL.");
			throw new std::exception("pUserState must not be NULL");
		}

        bool fRestartSubscribingThread;

        while (true)
        {
            // Wait letting the subscribing thread work.
            fRestartSubscribingThread = false;
            Sleep(_kMillisecondsTimeoutWatchingThread);

            // Did it do any work?
            pThis->_locker.lock();
            {
                if (!pThis->_isSubscriberThreadAlive)
                {
					//&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&  DEBUG REPLACE FOLLOWING STATEMEN &&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&
                    //fRestartSubscribingThread = true;
					//&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&  DEBUG REPLACE FOLLOWING STATEMEN &&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&
                }

                pThis->_isSubscriberThreadAlive = false;               // reset it for the next look
            }
            pThis->_locker.unlock();

            // Restart the subscribing thread if we need to
            if (fRestartSubscribingThread)
            {
				CLTRACE(1, "CBadgeNetPubSubEvents: WatchingThreadProc: Call RestartSubscribingThread().");
                pThis->RestartSubcribingThread();
            }
        }
    }
    catch (std::exception &ex)
    {
		CLTRACE(1, "CBadgeNetPubSubEvents: WatchingThreadProc: ERROR: Exception.  Message: %s.", ex.what());
		if (pThis != NULL)
		{
	        pThis->FireEventSubscriptionWatcherFailed();                // notify the delegates
			pThis->KillSubscribingThread();
		}
    }
}

/// <summary>
/// Kill the subscribing thread.
/// </summary>
void CBadgeNetPubSubEvents::KillSubscribingThread()
{
    try
    {
		CLTRACE(9, "CBadgeNetPubSubEvents: KillSubscribingThread: Entry.");
        _locker.lock();
        {
            if (_pPubSubServer != NULL)
            {
                // Ask the subscribing thread to exit gracefully.
				CLTRACE(9, "CBadgeNetPubSubEvents: KillSubscribingThread: Call CancelWaitingSubscription.");
                EnumPubSubServerCancelWaitingSubscriptionReturnCodes result;
                HRESULT hr = _pPubSubServer->CancelWaitingSubscription(BadgeCom_To_BadgeNet, _guidSubscription, &result);
                if (!SUCCEEDED(hr))
                {
        		    CLTRACE(1, "CBadgeNetPubSubEvents: KillSubscribingThread: ERROR: Cancelling. hr: %d.", hr);
                }
                if (result != RC_CANCEL_CANCELLED)
                {
        		    CLTRACE(1, "CBadgeNetPubSubEvents: KillSubscribingThread: ERROR: Cancelling. Result: %d.", result);
                }
            }
        }
        _locker.unlock();

        // Wait for the thread to be gone.  If it takes too long, kill it.
        bool fThreadSubscribingInstantiated = false;
        _locker.lock();
        {
            if (_threadSubscribingHandle != NULL)
            {
                fThreadSubscribingInstantiated = true;
            }
        }
        _locker.unlock();

        // Try to kill the thread if it is instantiated.
        if (fThreadSubscribingInstantiated)
        {
            bool fThreadDead = false;
            for (int i = 0; i < 3; ++i)
            {
                if (IsThreadAlive(_threadSubscribingHandle))
                {
                    Sleep(50);
                }
                else
                {
					CLTRACE(9, "CBadgeNetPubSubEvents: KillSubscribingThread: Thread is dead.");
                    fThreadDead = true;
                    break;
                }
            }

            if (!fThreadDead)
            {
				CLTRACE(9, "CBadgeNetPubSubEvents: KillSubscribingThread: Call TerminateThread.");
                TerminateThread(_threadSubscribingHandle, 9999);
            }
        }

        _threadSubscribingHandle = NULL;
    }
    catch (std::exception &ex)
    {
		CLTRACE(1, "CBadgeNetPubSubEvents: KillSubscribingThread: ERROR: Exception.  Message: %s.", ex.what());
    }
}

/// <summary>
/// Kill the watching thread.
/// </summary>
void CBadgeNetPubSubEvents::KillWatchingThread()
{
    try
    {
		CLTRACE(9, "CBadgeNetPubSubEvents: KillWatchingThread: Entry.");
        _locker.lock();
        {
            if (_threadWatchingHandle != NULL)
            {
				CLTRACE(9, "CBadgeNetPubSubEvents: KillWatchingThread: Call TerminateThread.");
                TerminateThread(_threadWatchingHandle, 9999);
                _threadWatchingHandle = NULL;
            }
        }
        _locker.unlock();
    }
    catch (std::exception &ex)
    {
		CLTRACE(1, "CBadgeNetPubSubEvents: KillWatchingThread: ERROR: Exception.  Message: %s.", ex.what());
    }
}

/// <summary>
/// Start the subscribing thread.
/// </summary>
void CBadgeNetPubSubEvents::StartSubscribingThread()
{
    try
    {
		CLTRACE(9, "CBadgeNetPubSubEvents: StartSubscribingThread: Entry.");
        _locker.lock();
        {
            // Start a thread to subscribe and process BadgeCom initialization events.  Upon receiving one of these events,
            // we will send the entire badging database for this process.
            DWORD dwThreadId;
            HANDLE handle = CreateThread(NULL,                              // default security
                        0,                                                  // default stack size
                        (LPTHREAD_START_ROUTINE)&SubscribingThreadProc,     // function to run
                        (LPVOID) this,                                      // thread parameter
                        0,                                                  // imediately run the thread
                        &dwThreadId                                         // output thread ID
                        );
            if (handle == NULL)
            {
                throw new std::exception("Error creating thread");
            }

            _threadSubscribingHandle = handle;
        }
        _locker.unlock();
    }
    catch (std::exception &ex)
    {
		CLTRACE(1, "CBadgeNetPubSubEvents: StartSubscribingThread: ERROR: Exception.  Message: %s.", ex.what());
    }
}


/// <summary>
/// Start the watching thread.
/// </summary>
void CBadgeNetPubSubEvents::StartWatchingThread()
{
    try
    {
		CLTRACE(9, "CBadgeNetPubSubEvents: StartWatchingThread: Entry.");
        _locker.lock();
        {
            // Start a thread to watch the thread that is watching BadgeCom.  This is necessary because BadgeCom may crash with
            // the threadWatcher thread waiting for an event to arrive.  That might result in a wait forever.  This thread
            // will kill the threadWatcher if it waits too long.  If it kills the thread, it will attempt to restart it.
            DWORD dwThreadId;
            HANDLE handle = CreateThread(NULL,                              // default security
                        0,                                                  // default stack size
                        (LPTHREAD_START_ROUTINE) &WatchingThreadProc,       // function to run
                        (LPVOID) this,                                      // thread parameter
                        0,                                                  // imediately run the thread
                        &dwThreadId                                         // output thread ID
                        );
            if (handle == NULL)
            {
                throw new std::exception("Error creating thread");
            }

            _threadWatchingHandle = handle;
        }
        _locker.unlock();
    }
    catch (std::exception &ex)
    {
		CLTRACE(1, "CBadgeNetPubSubEvents: StartWatchingThread: ERROR: Exception.  Message: %s.", ex.what());
    }
}

/// <summary>
/// Attempt to restart the subscribing thread.
/// </summary>
void CBadgeNetPubSubEvents::RestartSubcribingThread()
{
	CLTRACE(9, "CBadgeNetPubSubEvents: RestartSubcribingThread: Entry.");
    KillSubscribingThread();
    StartSubscribingThread();
}

/// <summary>
/// Checks whether a thread is alive.
/// </summary>
/// <param name="hThread">This thread handle to check.</param>
/// <returns>bool true: The thread is still alive.</returns>
bool CBadgeNetPubSubEvents::IsThreadAlive(const HANDLE hThread)
{
     // Read thread's exit code.
     DWORD dwExitCode = 0;
     if(GetExitCodeThread(hThread, &dwExitCode))
     {
         // if return code is STILL_ACTIVE then thread is live.
         return (dwExitCode == STILL_ACTIVE);
     }

     // Not active.
     return false;
}

