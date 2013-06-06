/*
 17FileSystemObjects_CalculatedFullPathCIHashes.index.sql
 Cloud Windows

 Created By DavidBruck.
 Copyright (c) Cloud.com. All rights reserved.
*/

CREATE INDEX IDX_FileSystemObjects_CalculatedFullPathCIHashes ON FileSystemObjects
(
    CalculatedFullPathCIHashes ASC
)