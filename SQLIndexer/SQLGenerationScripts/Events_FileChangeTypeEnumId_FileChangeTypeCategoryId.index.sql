CREATE NONCLUSTERED INDEX [Idx_Events_FileChangeTypeEnumId_FileChangeTypeCategoryId] ON [Events]
(
	[FileChangeTypeEnumId] ASC,
	[FileChangeTypeCategoryId] ASC
)