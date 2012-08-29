/*
 * Events.table.sql
 * Cloud Windows
 *
 * Created By DavidBruck.
 * Copyright (c) Cloud.com. All rights reserved.
 */
CREATE TABLE [Events]
(
	[EventId] bigint /*Begin CE Only*/PRIMARY KEY/*End CE Only*/ IDENTITY(1, 1) NOT NULL,
	[SyncCounter] bigint NULL,
	[FileChangeTypeCategoryId] int NOT NULL /*CE Limitation: CHECK ([FileChangeTypeCategoryId] = 0 /-* category FileChangeTypes must equal 0 *-/)*/,
	[FileChangeTypeEnumId] int NOT NULL,
	[PreviousPath] ntext /*CE Limitation: COLLATE Latin1_General_CS_AS*/ NULL /*CE Limitation: CHECK([PreviousPath] <> '')*/,
	[FileSystemObjectId] bigint NOT NULL,
	[SyncFrom] bit NOT NULL/*CE Limitation: ,/-* as opposed to SyncTo *-/
	CONSTRAINT [PK_Events] PRIMARY KEY NONCLUSTERED ([EventId] ASC)*/,
	CONSTRAINT [FK_SyncCounter_SyncCounter] FOREIGN KEY ([SyncCounter]) REFERENCES [Syncs] ([SyncCounter]),
	CONSTRAINT [FK_FileChangeTypeEnumId_FileChangeTypeCategoryId_EnumId_EnumCategoryId] FOREIGN KEY ([FileChangeTypeEnumId], [FileChangeTypeCategoryId]) REFERENCES [Enums] ([EnumId], [EnumCategoryId]),
	CONSTRAINT [FK_FileSystemObjectId_FileSystemObjectId] FOREIGN KEY ([FileSystemObjectId]) REFERENCES [FileSystemObjects] ([FileSystemObjectId])/*CE Limitation: ,
	CONSTRAINT [CHK_PreviousPathSet] CHECK (([FileChangeTypeEnumId] = 3 AND [PreviousPath] IS NOT NULL) OR ([FileChangeTypeEnumId] <> 3 AND [PreviousPath] IS NULL)) -- category FileChangeTypes enum Renamed must equal 1*/
)