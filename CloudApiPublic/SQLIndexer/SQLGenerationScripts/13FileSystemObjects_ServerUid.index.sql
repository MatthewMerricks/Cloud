/*
 13FileSystemObjects_ServerUid.index.sql
 Cloud Windows

 Created By DavidBruck.
 Copyright (c) Cloud.com. All rights reserved.
*/

CREATE INDEX IDX_FileSystemObjects_ServerUid ON FileSystemObjects
(
    ServerUid ASC
)