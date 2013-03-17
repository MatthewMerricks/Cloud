/*
 11FileSystemObjects_ParentFolderId.index.sql
 Cloud Windows

 Created By DavidBruck.
 Copyright (c) Cloud.com. All rights reserved.
*/

CREATE INDEX IDX_FileSystemObjects_ParentFolderId ON FileSystemObjects
(
    ParentFolderId ASC
)