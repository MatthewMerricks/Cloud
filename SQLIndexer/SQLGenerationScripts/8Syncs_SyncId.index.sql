/*
 * Syncs_SyncId.index.sql
 * Cloud Windows
 *
 * Created By DavidBruck.
 * Copyright (c) Cloud.com. All rights reserved.
 */
CREATE NONCLUSTERED INDEX [Idx_Syncs_SyncId] ON [Syncs]
(
	[SyncId] ASC
)