/*
 9ServerUids.table.sql
 Cloud Windows

 Created By DavidBruck.
 Copyright (c) Cloud.com. All rights reserved.
*/

CREATE TABLE ServerUids
(
    ServerUidId INTEGER PRIMARY KEY AUTOINCREMENT,
    ServerUid TEXT CONSTRAINT CHK_ServerUids_ServerUid_TEXT
      CHECK (ServerUid IS NULL OR TYPEOF(ServerUid) = 'text'),
    Revision TEXT CONSTRAINT CHK_ServerUids_Revision_TEXT
      CHECK (Revision IS NULL OR TYPEOF(Revision) = 'text'),
    UNIQUE (ServerUid)
);