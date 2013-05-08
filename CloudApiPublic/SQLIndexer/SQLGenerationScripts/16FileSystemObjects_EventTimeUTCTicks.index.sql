/*
 16FileSystemObjects_EventTimeUTCTicks.index.sql
 Cloud Windows

 Created By DavidBruck.
 Copyright (c) Cloud.com. All rights reserved.
*/

CREATE INDEX IDX_FileSystemObjects_EventTimeUTCTicks ON FileSystemObjects
(
    EventTimeUTCTicks DESC
)