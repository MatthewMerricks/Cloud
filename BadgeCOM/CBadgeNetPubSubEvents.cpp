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
	#define CLTRACE(intPriority, szFormat, ...) //#define CLTRACE(intPriority, szFormat, ...) Trace::getInstance()->write(intPriority, szFormat, __VA_ARGS__)
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
            throw new std::exception("Error creating a class factory for CPubSubServer");
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
/// Initialize and load the BadgeCom PubSubServer.
/// </summary>
void CBadgeNetPubSubEvents::Initialize()
{
    try
    {
        //TODO: Nothing to do here?
    }
    catch (std::exception &ex)
    {
		CLTRACE(1, "CBadgeNetPubSubEvents: Initialize: ERROR: Exception.  Message: %s.", ex.what());
    }
}

/// <summary>
/// Publish an event to BadgeCom
/// </summary>
void CBadgeNetPubSubEvents::PublishEventToBadgeCom(EnumEventType eventType, EnumEventSubType eventSubType, EnumCloudAppIconBadgeType badgeType, BSTR *fullPath)
{
    try
    {
        if (_pPubSubServer == NULL)
        {
            throw new std::exception("Call Initialize() first");
        }

        // Publish the event
        EnumPubSubServerPublishReturnCodes result;
        HRESULT hr = _pPubSubServer->Publish(eventType, eventSubType, badgeType, fullPath, &result);
        if (!SUCCEEDED(hr) || result != RC_PUBLISH_OK)
        {
    		CLTRACE(1, "CBadgeNetPubSubEvents: PublishEventToBadgeCom: ERROR: From Publish.  Result: %d.", result);
        }
    }
    catch (std::exception &ex)
    {
		CLTRACE(1, "CBadgeNetPubSubEvents: PublishEventToBadgeCom: ERROR: Exception.  Message: %s.", ex.what());
    }
}

/// <summary>
/// Start a thread which will subscribe to BadgeCom_Initialization events.  
/// </summary>
void CBadgeNetPubSubEvents::SubscribeToBadgeComInitializationEvents()
{
    // Start the threads.
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
        // Cast the user state to an object instance
        CBadgeNetPubSubEvents *pThis = (CBadgeNetPubSubEvents *)pUserState;

        // Generate a GUID to represent this subscription
        HRESULT hr = CoCreateGuid(&pThis->_guidSubscription);
        if (!SUCCEEDED(hr))
        {
    		CLTRACE(1, "CBadgeNetPubSubEvents: SubscribingThreadProc: ERROR: Creating GUID.  Result: %d.", result);
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
            hr = pThis->_pPubSubServer->Subscribe(BadgeCom_To_BadgeNet, pThis->_guidSubscription, _kMillisecondsTimeoutSubscribingThread, &eventSubType, &badgeType, &bsFullPath, &result);
            if (!SUCCEEDED(hr))
            {
        		CLTRACE(1, "CBadgeNetPubSubEvents: SubscribingThreadProc: ERROR: From Subscribe.  Result: %d.", result);
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
                        pThis->FireEventRemoveBadgePath(bsFullPath, badgeType);
                        break;
                }
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
    CBadgeNetPubSubEvents *pThis = (CBadgeNetPubSubEvents *)pUserState;

    try
    {
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
                    fRestartSubscribingThread = true;
                }

                pThis->_isSubscriberThreadAlive = false;               // reset it for the next look
            }
            pThis->_locker.unlock();

            // Restart the subscribing thread if we need to
            if (fRestartSubscribingThread)
            {
                pThis->RestartSubcribingThread();
            }
        }
    }
    catch (std::exception &ex)
    {
		CLTRACE(1, "CBadgeNetPubSubEvents: WatchingThreadProc: ERROR: Exception.  Message: %s.", ex.what());
        pThis->FireSubscriptionWatcherFailed();                // notify the delegates
        pThis->KillSubscribingThread();
    }
}

/// <summary>
/// Kill the subscribing thread.
/// </summary>
void CBadgeNetPubSubEvents::KillSubscribingThread()
{
    try
    {
        _locker.lock();
        {
            if (_pPubSubServer != NULL)
            {
                // Ask the subscribing thread to exit gracefully.
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
            for (int i = 0; i < 3; i++)
            {
                if (IsThreadAlive(_threadSubscribingHandle))
                {
                    Sleep(50);
                }
                else
                {
                    fThreadDead = true;
                    break;
                }
            }

            if (!fThreadDead)
            {
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
        _locker.lock();
        {
            if (_threadWatchingHandle != NULL)
            {
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

