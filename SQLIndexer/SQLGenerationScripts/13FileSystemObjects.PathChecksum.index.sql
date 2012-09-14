/*
 * FileSystemObjects.PathChecksum.index.sql
 * Cloud Windows
 *
 * Created By DavidBruck.
 * Copyright (c) Cloud.com. All rights reserved.
 */
CREATE NONCLUSTERED INDEX [Idx_FileSystemObjects_PathChecksum] ON [FileSystemObjects]
(
	[PathChecksum] ASC
)