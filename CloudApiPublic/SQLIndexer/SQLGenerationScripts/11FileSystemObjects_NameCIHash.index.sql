/*
 11FileSystemObjects_NameCIHash.index.sql
 Cloud Windows

 Created By DavidBruck.
 Copyright (c) Cloud.com. All rights reserved.
*/

CREATE INDEX IDX_FileSystemObjects_NameCIHash ON FileSystemObjects
(
    NameCIHash ASC
)