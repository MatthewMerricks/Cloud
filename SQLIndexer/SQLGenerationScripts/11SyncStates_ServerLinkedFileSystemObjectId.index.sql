/*
 * SyncStates_ServerLinkedFileSystemObjectId.index.sql
 * Cloud Windows
 *
 * Created By DavidBruck.
 * Copyright (c) Cloud.com. All rights reserved.
 */
CREATE NONCLUSTERED INDEX [Idx_SyncStates_ServerLinkedFileSystemObjectId] ON [SyncStates]
(
	[ServerLinkedFileSystemObjectId] ASC
)