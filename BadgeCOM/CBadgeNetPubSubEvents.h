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

class CBadgeNetPubSubEvents
{
private:
    // Constants
    static const int _kMillisecondsTimeoutSubscribingThread = 1000;
    static const int _kMillisecondsTimeoutWatchingThread = 5000;

    // Private fields
    CPubSubServer *_pPubSubServer;
    GUID _guidSubscription;
    HANDLE _threadSubscribingHandle;
    HANDLE _threadWatchingHandle;
    bool _isSubscriberThreadAlive;
    boost::mutex _locker;

public:
    // Life cycle
    CBadgeNetPubSubEvents(void);
    ~CBadgeNetPubSubEvents(void);

    // Events
    boost::signal<void (BSTR)> FireEventAddSyncBoxFolderPath;
    boost::signal<void (BSTR)> FireEventRemoveSyncBoxFolderPath;
    boost::signal<void (BSTR, EnumCloudAppIconBadgeType)> FireEventAddBadgePath;
    boost::signal<void (BSTR)> FireEventRemoveBadgePath;
    boost::signal<void ()> FireEventSubscriptionWatcherFailed;

    // Methods
	void Initialize();
    void PublishEventToBadgeNet(EnumEventType eventType, EnumEventSubType eventSubType, EnumCloudAppIconBadgeType badgeType, BSTR *fullPath);
    void SubscribeToBadgeNetEvents();
    static void SubscribingThreadProc(LPVOID pUserState);
    static void WatchingThreadProc(LPVOID pUserState);
    void KillSubscribingThread();
    void KillWatchingThread();
    void StartSubscribingThread();
    void StartWatchingThread();
    void RestartSubcribingThread();
    bool IsThreadAlive(const HANDLE hThread);
};

