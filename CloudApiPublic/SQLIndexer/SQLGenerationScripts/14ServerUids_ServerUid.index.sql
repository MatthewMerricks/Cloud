/*
 14ServerUids_ServerUid.index.sql
 Cloud Windows

 Created By DavidBruck.
 Copyright (c) Cloud.com. All rights reserved.
*/

CREATE INDEX IDX_ServerUids_ServerUid ON ServerUids
(
    ServerUid ASC
)