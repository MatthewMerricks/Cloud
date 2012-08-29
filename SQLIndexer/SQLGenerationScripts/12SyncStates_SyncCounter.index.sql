/*
 * SyncStates_SyncCounter.index.sql
 * Cloud Windows
 *
 * Created By DavidBruck.
 * Copyright (c) Cloud.com. All rights reserved.
 */
CREATE /*Begin CE Only*/NONCLUSTERED/*End CE Only*//*CE Limitation: CLUSTERED*/ INDEX [Idx_SyncStates_SyncCounter] ON [SyncStates]
(
	[SyncCounter] ASC
)