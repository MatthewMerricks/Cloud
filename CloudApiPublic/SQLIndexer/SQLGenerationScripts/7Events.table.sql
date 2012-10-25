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
	[FileChangeTypeCategoryId] int NOT NULL /*CE Limitation: CHECK ([FileChangeTypeCategoryId] = 0 /-* category FileChangeTypes must equal 0 *-/)*/,
	[FileChangeTypeEnumId] int NOT NULL,
	[PreviousPath] ntext /*CE Limitation: COLLATE Latin1_General_CS_AS*/ NULL /*CE Limitation: CHECK([PreviousPath] <> '')*/,
	[SyncFrom] bit NOT NULL, -- as opposed to SyncTo
	[GroupId] uniqueidentifier NULL,
	[GroupOrder] int NULL/*CE Limitation: ,
	CONSTRAINT [PK_Events] PRIMARY KEY NONCLUSTERED ([EventId] ASC)*/,
	CONSTRAINT [FK_FileChangeTypeEnumId_FileChangeTypeCategoryId_EnumId_EnumCategoryId] FOREIGN KEY ([FileChangeTypeEnumId], [FileChangeTypeCategoryId]) REFERENCES [Enums] ([EnumId], [EnumCategoryId])/*CE Limitation: ,
	CONSTRAINT [CHK_PreviousPathSet] CHECK (([FileChangeTypeEnumId] = 3 AND [PreviousPath] IS NOT NULL) OR ([FileChangeTypeEnumId] <> 3 AND [PreviousPath] IS NULL)) -- category FileChangeTypes enum Renamed must equal 1*/
)