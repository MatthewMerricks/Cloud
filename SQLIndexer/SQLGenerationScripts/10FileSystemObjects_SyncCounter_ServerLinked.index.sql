/*
 * FileSystemObjects_SyncCounter_ServerLinked.index.sql
 * Cloud Windows
 *
 * Created By DavidBruck.
 * Copyright (c) Cloud.com. All rights reserved.
 */
CREATE NONCLUSTERED INDEX [Idx_FileSystemObjects_SyncCounter_ServerLinked] ON [FileSystemObjects]
(
	[SyncCounter] ASC,
	[ServerLinked] ASC
)