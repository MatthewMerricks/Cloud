CREATE TABLE [Events]
(
	[EventId] bigint IDENTITY(1, 1) NOT NULL,
	[SyncCounter] bigint NULL,
	[FileChangeTypeCategoryId] int NOT NULL CHECK ([FileChangeTypeCategoryId] = 0 /* category FileChangeTypes must equal 0 */),
	[FileChangeTypeEnumId] int NOT NULL,
	[PreviousPath] nvarchar(max) COLLATE Latin1_General_CS_AS NULL CHECK([PreviousPath] <> ''),
	[FileSystemObjectId] bigint NOT NULL,
	[SyncFrom] bit NOT NULL,/* as opposed to SyncTo */
	CONSTRAINT [PK_Events] PRIMARY KEY NONCLUSTERED ([EventId] ASC),
	CONSTRAINT [FK_SyncCounter_SyncCounter] FOREIGN KEY ([SyncCounter]) REFERENCES [Syncs] ([SyncCounter]),
	CONSTRAINT [FK_FileChangeTypeEnumId_FileChangeTypeCategoryId_EnumId_EnumCategoryId] FOREIGN KEY ([FileChangeTypeEnumId], [FileChangeTypeCategoryId]) REFERENCES [Enums] ([EnumId], [EnumCategoryId]),
	CONSTRAINT [FK_FileSystemObjectId_FileSystemObjectId] FOREIGN KEY ([FileSystemObjectId]) REFERENCES [FileSystemObjects] ([FileSystemObjectId]),
	CONSTRAINT [CHK_PreviousPathSet] CHECK (([FileChangeTypeEnumId] = 3 AND [PreviousPath] IS NOT NULL) OR ([FileChangeTypeEnumId] <> 3 AND [PreviousPath] IS NULL)) -- category FileChangeTypes enum Renamed must equal 1
)