//
// CBadgeNetPubSubEvents.cpp
// Cloud Windows COM
//
// Created By BobS.
// Copyright (c) Cloud.com. All rights reserved.

#include "StdAfx.h"
#include "CBadgeNetPubSubEvents.h"


// Debug trace
#define CLTRACE(intPriority, szFormat, ...) Trace::getInstance()->write(intPriority, szFormat, __VA_ARGS__)

// Initialize static fields.
bool CBadgeNetPubSubEvents::_fDebugging = false;

/// <summary>
/// Constructor
/// </summary>
CBadgeNetPubSubEvents::CBadgeNetPubSubEvents(void) : _semWaitForSubscriptionThreadStart(0), _semWatcher(0)
{
    try
    {
        // Initialize private fields
		CLTRACE(9, "CBadgeNetPubSubEvents: CBadgeNetPubSubEvents: Entry.");
        _pPubSubServer = NULL;
        _threadSubscribingHandle = NULL;
        _threadWatchingHandle = NULL;
        _isSubscriberThreadAlive = false;
        _fRequestSubscribingThreadExit = false;
        _fRequestWatchingThreadExit = false;
        _fTerminating = false;

        // Generate a GUID to represent this subscription
        HRESULT hr = CoCreateGuid(&_guidSubscriber);
        if (!SUCCEEDED(hr))
        {
    		CLTRACE(1, "CBadgeNetPubSubEvents: CBadgeNetPubSubEvents: ERROR: Creating GUID. hr: %d.", hr);
            throw new std::exception("Error creating GUID");
        }

        // Initialize the COM system
        hr = CoInitializeEx(NULL, COINIT_MULTITHREADED);
		CLTRACE(9, "CBadgeNetPubSubEvents: CBadgeNetPubSubEvents: CoInitialize returned %d.", hr);

        // Create a class factory to instantiate an instance of CPubSubServer.
        IClassFactory *pIClassFactory = NULL;
		CLTRACE(9, "CBadgeNetPubSubEvents: CBadgeNetPubSubEvents: Call CoGetClassObject.");
        hr = CoGetClassObject(CLSID_PubSubServer, CLSCTX_ALL, NULL, IID_IClassFactory, (LPVOID *)&pIClassFactory);
        if (SUCCEEDED(hr) && pIClassFactory != NULL)
        {
            // Instantiate an instance of CPubSubServer
		    CLTRACE(9, "CBadgeNetPubSubEvents: CBadgeNetPubSubEvents: Call CreateInstance."); 
            hr = pIClassFactory->CreateInstance(NULL, IID_IPubSubServer, (LPVOID *)&_pPubSubServer);
            if (!SUCCEEDED(hr) || _pPubSubServer == NULL)
            {
    		    CLTRACE(1, "CBadgeNetPubSubEvents: CBadgeNetPubSubEvents: ERROR.  PubSubServer not instantiated. Throw."); 
                throw new std::exception("Error creating an instance of CPubSubServer");
            }

            // Release the class factory
  		    CLTRACE(9, "CBadgeNetPubSubEvents: CBadgeNetPubSubEvents: Allocated _pPubSubServer: %x.", _pPubSubServer); 
            pIClassFactory->Release();
            pIClassFactory = NULL;
        }
        else
        {
    		CLTRACE(1, "CBadgeNetPubSubEvents: CBadgeNetPubSubEvents: ERROR.  Creating class factory. Throw."); 
            throw new std::exception("Error creating a class factory for CPubSubServer.  hr: %d.", hr);
        }
    }
    catch (const std::exception &ex)
    {
		CLTRACE(1, "CBadgeNetPubSubEvents: CBadgeNetPubSubEvents: ERROR: Exception.  Message: %s.", ex.what());
    }
    catch (...)
    {
		CLTRACE(1, "CBadgeNetPubSubEvents: CBadgeNetPubSubEvents: ERROR: C++ exception.");
    }
	CLTRACE(9, "CBadgeNetPubSubEvents: CBadgeNetPubSubEvents: Exit."); 
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
		_fRequestSubscribingThreadExit = true;              // preemptive strike
		_fRequestWatchingThreadExit = true;
        _fTerminating = true;
        KillSubscribingThread();
        KillWatchingThread();
    }
    catch (const std::exception &ex)
    {
		CLTRACE(1, "CBadgeNetPubSubEvents: ~CBadgeNetPubSubEvents: ERROR: Exception. Killing threads. Message: %s.", ex.what());
    }
    catch (...)
    {
		CLTRACE(1, "CBadgeNetPubSubEvents: ~CBadgeNetPubSubEvents: ERROR: C++ exception.");
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
		CLTRACE(1, "CBadgeNetPubSubEvents: ~CBadgeNetPubSubEvents: ERROR: Exception(2).  Message: %s.", ex.what());
    }
    catch (...)
    {
		CLTRACE(1, "CBadgeNetPubSubEvents: ~CBadgeNetPubSubEvents: ERROR: C++ exception(2).");
    }
}

/// <summary>
/// Initialize.  Initialize the PubSubEventsServer.
/// </summary>
void CBadgeNetPubSubEvents::Initialize()
{
    try
    {
		CLTRACE(9, "CBadgeNetPubSubEvents: Initialize: Entry.");
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
    catch (const std::exception &ex)
    {
		CLTRACE(1, "CBadgeNetPubSubEvents: Initialize: ERROR: Exception.  Message: %s.", ex.what());
    }
    catch (...)
    {
		CLTRACE(1, "CBadgeNetPubSubEvents: Initialize: ERROR: C++ exception.");
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
    catch (const std::exception &ex)
    {
		CLTRACE(1, "CBadgeNetPubSubEvents: PublishEventToBadgeNet: ERROR: Exception.  Message: %s.", ex.what());
    }
    catch (...)
    {
		CLTRACE(1, "CBadgeNetPubSubEvents: PublishEventToBadgeNet: ERROR: C++ exception.");
    }
}

/// <summary>
/// Start a thread which will subscribe to events from BadgeNet.  
/// </summary>
/// <returns>bool: true is success</returns>
bool CBadgeNetPubSubEvents::SubscribeToBadgeNetEvents()
{
    bool fIsStartedOk = false;
	try
	{
		// Start the threads.
		CLTRACE(9, "CBadgeNetPubSubEvents: SubscribeToBadgeNetEvents: Entry.");
		bool startedOk = StartSubscribingThread();
		if (startedOk)
		{
			StartWatchingThread();
            fIsStartedOk = true;
		}
		else
		{
			CLTRACE(1, "CBadgeNetPubSubEvents: SubscribeToBadgeNetEvents: ERROR. Subscribing thread did not start.");
		}
	}
	catch (const std::exception &ex)
	{
		CLTRACE(1, "CBadgeNetPubSubEvents: SubscribeToBadgeNetEvents: ERROR: Exception.  Message: %s.", ex.what());
	}
    catch (...)
    {
		CLTRACE(1, "CBadgeNetPubSubEvents: SubscribeToBadgeNetEvents: ERROR: C++ exception.");
    }

    return fIsStartedOk;
}

/// <summary>
/// Subscribe and pull events from any of the BadgeCom instance threads.  
/// </summary>
void CBadgeNetPubSubEvents::SubscribingThreadProc(LPVOID pUserState)
{
    try
    {
        bool fSemaphoreReleased = false;

		CLTRACE(9, "CBadgeNetPubSubEvents: SubscribingThreadProc: Entry.");
		if (pUserState == NULL)
		{
    		CLTRACE(1, "CBadgeNetPubSubEvents: SubscribingThreadProc: ERROR: User state is NULL.");
            throw new std::exception("pUserState must not be NULL");
		}

        // Cast the user state to an object instance
        CBadgeNetPubSubEvents *pThis = (CBadgeNetPubSubEvents *)pUserState;

        // Loop waiting for events.
        while (true)
        {
            // Exit if we have been requested.
            if (pThis->_fRequestSubscribingThreadExit || pThis->_fTerminating)
            {
				CLTRACE(9, "CBadgeNetPubSubEvents: SubscribingThreadProc: Requested to exit.  Break out of loop.");
                break;
            }

            // Create or open this subcription.  Since the GUID is unique, this will create the subscription on the first call.
            EnumPubSubServerSubscribeReturnCodes result;
            EnumEventSubType eventSubType;
            EnumCloudAppIconBadgeType badgeType;
            BSTR bsFullPath;                                // allocated by Subscribe.  Must be freed eventually (SysFreeString()).
            HRESULT hr = pThis->_pPubSubServer->Subscribe(BadgeNet_To_BadgeCom, pThis->_guidSubscriber, _knSubscriptionTimeoutMs, &eventSubType, &badgeType, &bsFullPath, &result);
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
						try
						{
	        		        CLTRACE(9, "CBadgeNetPubSubEvents: SubscribingThreadProc: Fire event: BadgeNet_AddSyncBoxFolderPath.");
		                    pThis->FireEventAddSyncBoxFolderPath(bsFullPath);
						}
						catch (const std::exception &ex)
						{
							CLTRACE(1, "CBadgeNetPubSubEvents: SubscribingThreadProc: ERROR: Exception(10).  Message: %s.", ex.what());
						}
                        catch (...)
                        {
		                    CLTRACE(1, "CBadgeNetPubSubEvents: SubscribingThreadProc: ERROR: C++ exception(10).");
                        }
                        break;
                    case BadgeNet_RemoveSyncBoxFolderPath:
						try
						{
	        		        CLTRACE(9, "CBadgeNetPubSubEvents: SubscribingThreadProc: Event: BadgeNet_RemoveSyncBoxFolderPath.");
		                    pThis->FireEventRemoveSyncBoxFolderPath(bsFullPath);
						}
						catch (const std::exception &ex)
						{
							CLTRACE(1, "CBadgeNetPubSubEvents: SubscribingThreadProc: ERROR: Exception(11).  Message: %s.", ex.what());
						}
                        catch (...)
                        {
		                    CLTRACE(1, "CBadgeNetPubSubEvents: SubscribingThreadProc: ERROR: C++ exception(11).");
                        }
                        break;
                    case BadgeNet_AddBadgePath:
						try
						{
	        		        CLTRACE(9, "CBadgeNetPubSubEvents: SubscribingThreadProc: Event: BadgeNet_AddBadgePath.");
		                    pThis->FireEventAddBadgePath(bsFullPath, badgeType);
						}
						catch (const std::exception &ex)
						{
							CLTRACE(1, "CBadgeNetPubSubEvents: SubscribingThreadProc: ERROR: Exception(12).  Message: %s.", ex.what());
						}
                        catch (...)
                        {
		                    CLTRACE(1, "CBadgeNetPubSubEvents: SubscribingThreadProc: ERROR: C++ exception(12).");
                        }
                        break;
                    case BadgeNet_RemoveBadgePath:
						try
						{
	        		        CLTRACE(9, "CBadgeNetPubSubEvents: SubscribingThreadProc: Event: BadgeNet_RemoveBadgePath.");
		                    pThis->FireEventRemoveBadgePath(bsFullPath);
						}
						catch (const std::exception &ex)
						{
							CLTRACE(1, "CBadgeNetPubSubEvents: SubscribingThreadProc: ERROR: Exception(13).  Message: %s.", ex.what());
						}
                        catch (...)
                        {
		                    CLTRACE(1, "CBadgeNetPubSubEvents: SubscribingThreadProc: ERROR: C++ exception(13).");
                        }
                        break;
                }

				// Free the out parameter BSTR allocated by the COM server.
				SysFreeString(bsFullPath);

                // Exit if we were requested to kill ourselves.
                if (pThis->_fRequestSubscribingThreadExit || pThis->_fTerminating)
                {
				    CLTRACE(1, "CBadgeNetPubSubEvents: SubscribingThreadProc: Requested to exit (3).  Break out of loop.");
                    break;
                }
            }
            else if (result == RC_SUBSCRIBE_ERROR)
            {
        		CLTRACE(1, "CBadgeNetPubSubEvents: SubscribingThreadProc: ERROR: From Subscribe.");
                break;
            }
            else if (result == RC_SUBSCRIBE_CANCELLED)
            {
        		CLTRACE(1, "CBadgeNetPubSubEvents: SubscribingThreadProc: This subscription was cancelled.");
                break;
            }
            else
            {
                // result is RC_SUBSCRIBE_TRY_AGAIN (an event is ready) or RC_SUBSCRIBE_TIMED_OUT (normal cycling).
                // Exit if we were requested to kill ourselves.
                if (pThis->_fRequestSubscribingThreadExit || pThis->_fTerminating)
                {
				    CLTRACE(1, "CBadgeNetPubSubEvents: SubscribingThreadProc: Requested to exit (2).  Break out of loop.");
                    break;
                }
            }

            // We're alive
            pThis->_locker.lock();
            {
                pThis->_isSubscriberThreadAlive = true;

                if (!fSemaphoreReleased)
                {
				    CLTRACE(9, "CBadgeNetPubSubEvents: SubscribingThreadProc: Subscribed. Post starting thread.");
                    fSemaphoreReleased = true;
                    pThis->_semWaitForSubscriptionThreadStart.signal();
                }
            }
            pThis->_locker.unlock();
        }
    }
    catch (const std::exception &ex)
    {
		CLTRACE(1, "CBadgeNetPubSubEvents: SubscribingThreadProc: ERROR: Exception.  Message: %s.", ex.what());
    }
    catch (...)
    {
		CLTRACE(1, "CBadgeNetPubSubEvents: SubscribingThreadProc: ERROR: C++ exception.");
    }
	CLTRACE(1, "CBadgeNetPubSubEvents: SubscribingThreadProc: Exit the thread.");
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
			CLTRACE(9, "CBadgeNetPubSubEvents: WatchingThreadProc: Wait until the next look.");
            boost::system_time const timeout = boost::get_system_time() + boost::posix_time::milliseconds(pThis->_knTimeBetweenWatchingThreadChecksMs);
			CLTRACE(9, "CBadgeNetPubSubEvents: WatchingThreadProc: Out of wait.  Check on subscribing thread.");
            pThis->_semWatcher.wait(timeout);

            // Exit if we should
            if (pThis->_fRequestWatchingThreadExit || pThis->_fTerminating)
            {
				CLTRACE(9, "CBadgeNetPubSubEvents: WatchingThreadProc: Requested to exit.  Break out of loop.");
                break;
            }

            // Did the subscribing thread do any work?
            pThis->_locker.lock();
            {
                if (!pThis->_isSubscriberThreadAlive)
                {
                    if (!_fDebugging && !pThis->_fRequestWatchingThreadExit && !pThis->_fTerminating)
                    {
                        fRestartSubscribingThread = true;
                    }
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

            // Clean any unused shared memory resources.
            EnumPubSubServerCleanUpUnusedResourcesReturnCodes result;
            HRESULT hr = pThis->_pPubSubServer->CleanUpUnusedResources(&result);
            if (!SUCCEEDED(hr))
            {
        		CLTRACE(1, "CBadgeNetPubSubEvents: WatchingThreadProc: ERROR: Exception from CleanUpUnusedResources.  hr: %d.", hr);
            }
            else if (result != RC_CLEANUPUNUSEDRESOURCES_OK)
            {
        		CLTRACE(1, "CBadgeNetPubSubEvents: WatchingThreadProc: ERROR: From CleanUpUnusedResources. result: %d.", result);
            }
        }
    }
    catch (const std::exception &ex)
    {
		CLTRACE(1, "CBadgeNetPubSubEvents: WatchingThreadProc: ERROR: Exception.  Message: %s.", ex.what());
        HandleWatchingThreadException(pThis);
    }
    catch (...)
    {
		CLTRACE(1, "CBadgeNetPubSubEvents: WatchingThreadProc: ERROR: C++ exception.");
        HandleWatchingThreadException(pThis);
    }
	CLTRACE(9, "CBadgeNetPubSubEvents: WatchingThreadProc: Watchin thread exit.");
}

/// <summary>
/// Handle watching thread exception.
/// </summary>
void CBadgeNetPubSubEvents::HandleWatchingThreadException(CBadgeNetPubSubEvents *pThis)
{
    try
    {
		CLTRACE(1, "CBadgeNetPubSubEvents: HandleWatchingThreadException: Entry.");
		if (pThis != NULL  && !pThis->_fRequestWatchingThreadExit && !pThis->_fTerminating)
		{
			CLTRACE(9, "CBadgeNetPubSubEvents: HandleWatchingThreadException: Fire event FireEventSubscriptionWatcherFailed.");
		    pThis->FireEventSubscriptionWatcherFailed();                // notify the delegates
			CLTRACE(9, "CBadgeNetPubSubEvents: HandleWatchingThreadException: Back from firing event FireEventSubscriptionWatcherFailed.");

			pThis->KillSubscribingThread();
		}
    }
    catch (const std::exception &ex)
    {
		CLTRACE(1, "CBadgeNetPubSubEvents: HandleWatchingThreadException: ERROR: Exception.  Message: %s.", ex.what());
    }
    catch (...)
    {
		CLTRACE(1, "CBadgeNetPubSubEvents: HandleWatchingThreadException: ERROR: C++ exception.");
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
        bool fThreadSubscribingInstantiated = false;
        _locker.lock();
        {
            if (_pPubSubServer != NULL)
            {
                // Cancel the subscription the thread may be waiting on.
				CLTRACE(9, "CBadgeNetPubSubEvents: KillSubscribingThread: Call CancelWaitingSubscription.");
                EnumPubSubServerCancelWaitingSubscriptionReturnCodes result;
                HRESULT hr = _pPubSubServer->CancelWaitingSubscription(BadgeNet_To_BadgeCom, _guidSubscriber, &result);
                if (!SUCCEEDED(hr))
                {
        		    CLTRACE(1, "CBadgeNetPubSubEvents: KillSubscribingThread: ERROR: Cancelling. hr: %d.", hr);
                }
                if (result != RC_CANCEL_OK)
                {
        		    CLTRACE(1, "CBadgeNetPubSubEvents: KillSubscribingThread: ERROR: Cancelling. Result: %d.", result);
                }
            }

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

			// Request the thread to exit and wait for it.  Kill it if it takes too long.
			CLTRACE(9, "CBadgeNetPubSubEvents: KillSubscribingThread: Request subscribing thread to exit.");
            _fRequestSubscribingThreadExit = true;
            for (int i = 0; i < _knShortRetries; ++i)
            {
                if (IsThreadAlive(_threadSubscribingHandle))
                {
					CLTRACE(9, "CBadgeNetPubSubEvents: KillSubscribingThread: Wait for the subscribing thread to exit.");
                    Sleep(_knShortRetrySleepMs);
                }
                else
                {
					CLTRACE(9, "CBadgeNetPubSubEvents: KillSubscribingThread: Subscribing thread is dead.");
                    fThreadDead = true;
                    break;
                }
            }

            if (!fThreadDead)
            {
				CLTRACE(9, "CBadgeNetPubSubEvents: KillSubscribingThread: Call TerminateThread.");
                TerminateThread(_threadSubscribingHandle, 9999);
            }

            CloseHandle(_threadSubscribingHandle);
        }

        _threadSubscribingHandle = NULL;
    }
    catch (const std::exception &ex)
    {
		CLTRACE(1, "CBadgeNetPubSubEvents: KillSubscribingThread: ERROR: Exception.  Message: %s.", ex.what());
    }
    catch (...)
    {
		CLTRACE(1, "CBadgeNetPubSubEvents: KillSubscribingThread: ERROR: C++ exception.");
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
        // Request the thread to exit and wait for the thread to be gone.  If it takes too long, kill it.
        bool fThreadWatchingInstantiated = false;
        _locker.lock();
        {
            if (_threadWatchingHandle != NULL)
            {
				CLTRACE(9, "CBadgeNetPubSubEvents: KillWatchingThread: Request watching thread to exit.");
                _fRequestWatchingThreadExit = true;             // request the thread to exit
                _semWatcher.signal();                           // knock it out of its wait

                fThreadWatchingInstantiated = true;
            }
        }
        _locker.unlock();

        // Try to kill the thread if it is instantiated.
        if (fThreadWatchingInstantiated)
        {
			CLTRACE(9, "CBadgeNetPubSubEvents: KillWatchingThread: Wait for watching thread to exit.");
            bool fThreadDead = false;
            for (int i = 0; i < _knShortRetries; i++)
            {
                if (IsThreadAlive(_threadWatchingHandle))
                {
        			CLTRACE(9, "CBadgeNetPubSubEvents: KillWatchingThread: Let the watching thread work.");
                    Sleep(_knShortRetrySleepMs);
                }
                else
                {
        			CLTRACE(9, "CBadgeNetPubSubEvents: KillWatchingThread: The watching thread is dead.");
                    fThreadDead = true;
                    break;
                }
            }

            if (!fThreadDead)
            {
    			CLTRACE(9, "CBadgeNetPubSubEvents: KillWatchingThread: Abort the watching thread.");
                TerminateThread(_threadWatchingHandle, 9999);
            }

            CloseHandle(_threadWatchingHandle);
        }

        _threadWatchingHandle = NULL;
    }
    catch (const std::exception &ex)
    {
		CLTRACE(1, "CBadgeNetPubSubEvents: KillWatchingThread: ERROR: Exception.  Message: %s.", ex.what());
    }
    catch (...)
    {
		CLTRACE(1, "CBadgeNetPubSubEvents: KillWatchingThread: ERROR: C++ exception.");
    }
}

/// <summary>
/// Start the subscribing thread.
/// </summary>
/// <returns>bool: true: Thread subscription OK.</returns>
bool CBadgeNetPubSubEvents::StartSubscribingThread()
{
    bool result = false;

    try
    {
		CLTRACE(9, "CBadgeNetPubSubEvents: StartSubscribingThread: Entry.");

        _locker.lock();
        {
            // Clear any status this thread might have.
            _fRequestSubscribingThreadExit = false;

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

        // Wait for the thread to be started and subscribed.
        boost::system_time const timeout = boost::get_system_time() + boost::posix_time::milliseconds(_knWaitForSubscriberThreadToStartSleepMs);
        result = _semWaitForSubscriptionThreadStart.wait(timeout);
		if (!result)
		{
			CLTRACE(1, "BadgeComPubSubEvents: StartSubscribingThread: ERROR: Subscribing thread did not start.");
		}
    }
    catch (const std::exception &ex)
    {
		CLTRACE(1, "CBadgeNetPubSubEvents: StartSubscribingThread: ERROR: Exception.  Message: %s.", ex.what());
    }
    catch (...)
    {
		CLTRACE(1, "CBadgeNetPubSubEvents: StartSubscribingThread: ERROR: C++ exception.");
    }

    return result;
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
            // Clear any status this thread might have.
            _fRequestWatchingThreadExit = false;

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
    catch (const std::exception &ex)
    {
		CLTRACE(1, "CBadgeNetPubSubEvents: StartWatchingThread: ERROR: Exception.  Message: %s.", ex.what());
    }
    catch (...)
    {
		CLTRACE(1, "CBadgeNetPubSubEvents: StartWatchingThread: ERROR: C++ exception.");
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

