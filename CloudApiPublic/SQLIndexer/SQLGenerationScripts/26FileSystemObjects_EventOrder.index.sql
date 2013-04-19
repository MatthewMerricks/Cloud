/*
 26FileSystemObjects_EventOrder.index.sql
 Cloud Windows

 Created By DavidBruck.
 Copyright (c) Cloud.com. All rights reserved.
*/

CREATE INDEX IDX_FileSystemObjects_EventOrder ON FileSystemObjects
(
    EventOrder ASC
)