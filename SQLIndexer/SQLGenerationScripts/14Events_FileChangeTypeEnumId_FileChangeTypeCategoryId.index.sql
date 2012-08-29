/*
 * Events_FileChangeTypeEnumId_FileChangeTypeCategoryId.index.sql
 * Cloud Windows
 *
 * Created By DavidBruck.
 * Copyright (c) Cloud.com. All rights reserved.
 */
CREATE NONCLUSTERED INDEX [Idx_Events_FileChangeTypeEnumId_FileChangeTypeCategoryId] ON [Events]
(
	[FileChangeTypeEnumId] ASC,
	[FileChangeTypeCategoryId] ASC
)