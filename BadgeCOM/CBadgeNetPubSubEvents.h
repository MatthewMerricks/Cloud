//
// CBadgeNetPubSubEvents.h
// Cloud Windows COM
//
// Created By BobS.
// Copyright (c) Cloud.com. All rights reserved.

#pragma once
#include <boost\signal.hpp>
#include <boost\thread.hpp>
#include "PubSubServer.h"
#include "Trace.h"
#include "BadgeCOM_i.h"
#include "BoostSemaphore.h"

class CBadgeNetPubSubEvents
{
private:
    // Constants
    static const int _knSubscriptionTimeoutMs = 1000;							// time to wait for an event to arrive before timing out
    static const int _knTimeBetweenWatchingThreadChecksMs = 20000;				// time between checks on the subscribing thread
	static const int _knShortRetries = 5;										// number of retries when giving up short amounts of CPU
	static const int _knShortRetrySleepMs = 50;									// time to wait when giving up short amounts of CPU
	static const int _knWaitForSubscriberThreadToStartSleepMs = 5000;			// time to wait for the Subscriber thread to start.


    // Static private fields
    static bool _fDebugging;

    // Private fields
    CPubSubServer *_pPubSubServer;
    GUID _guidSubscriber;
    HANDLE _threadSubscribingHandle;
    HANDLE _threadWatchingHandle;
    bool _isSubscriberThreadAlive;
    boost::mutex _locker;
    semaphore _semWaitForSubscriptionThreadStart;
    semaphore _semWatcher;
    bool _fRequestSubscribingThreadExit;
    bool _fRequestWatchingThreadExit;
    bool _fTerminating;

public:
    // Life cycle
    CBadgeNetPubSubEvents(void);
    ~CBadgeNetPubSubEvents(void);

    // Events
    boost::signal<void (BSTR, ULONG, GUID)> FireEventAddSyncBoxFolderPath;
    boost::signal<void (BSTR, ULONG, GUID)> FireEventRemoveSyncBoxFolderPath;
    boost::signal<void (BSTR, EnumCloudAppIconBadgeType, ULONG, GUID)> FireEventAddBadgePath;
    boost::signal<void (BSTR, ULONG, GUID)> FireEventRemoveBadgePath;
    boost::signal<void ()> FireEventSubscriptionWatcherFailed;

    // Methods
	void Initialize();
    void PublishEventToBadgeNet(EnumEventType eventType, EnumEventSubType eventSubType, EnumCloudAppIconBadgeType badgeType, BSTR *fullPath, GUID guidPublisher);
    bool SubscribeToBadgeNetEvents();
    static void SubscribingThreadProc(LPVOID pUserState);
    static void WatchingThreadProc(LPVOID pUserState);
    static void CBadgeNetPubSubEvents::HandleWatchingThreadException(CBadgeNetPubSubEvents *pThis);
    void KillSubscribingThread();
    void KillWatchingThread();
    bool StartSubscribingThread();
    void StartWatchingThread();
    void RestartSubcribingThread();
    bool IsThreadAlive(const HANDLE hThread);
};

