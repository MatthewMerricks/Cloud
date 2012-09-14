/*
 * FileSystemObjects_EventId.index.sql
 * Cloud Windows
 *
 * Created By DavidBruck.
 * Copyright (c) Cloud.com. All rights reserved.
 */
CREATE NONCLUSTERED INDEX [Idx_FileSystemObjects_EventId] ON [FileSystemObjects]
(
	[EventId] ASC
)