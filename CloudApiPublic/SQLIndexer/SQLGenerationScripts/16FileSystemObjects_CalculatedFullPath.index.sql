/*
 16FileSystemObjects_CalculatedFullPath.index.sql
 Cloud Windows

 Created By DavidBruck.
 Copyright (c) Cloud.com. All rights reserved.
*/

CREATE INDEX IDX_FileSystemObjects_CalculatedFullPath ON FileSystemObjects
(
    CalculatedFullPath ASC
)