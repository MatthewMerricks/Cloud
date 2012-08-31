/*
 * FileSystemObjects_Revision.index.sql
 * Cloud Windows
 *
 * Created By DavidBruck.
 * Copyright (c) Cloud.com. All rights reserved.
 */
CREATE NONCLUSTERED INDEX [Idx_FileSystemObjects_Revision] ON [FileSystemObjects]
(
	[Revision] ASC,
	[RevisionIsNull] ASC
)