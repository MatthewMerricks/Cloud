/*
 15FileSystemObjects_Pending.index.sql
 Cloud Windows

 Created By DavidBruck.
 Copyright (c) Cloud.com. All rights reserved.
*/

CREATE INDEX IDX_FileSystemObjects_Pending ON FileSystemObjects
(
    Pending ASC
)