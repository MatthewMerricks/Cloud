CREATE TABLE [Enums]
(
	[EnumId] int NOT NULL,
	[EnumCategoryId] int NOT NULL,
	[Name] nvarchar(max) COLLATE Latin1_General_CS_AS NOT NULL CHECK([Name] <> ''),
	CONSTRAINT [PK_Enums] PRIMARY KEY CLUSTERED ([EnumId] ASC, [EnumCategoryId] ASC),
	CONSTRAINT [FK_EnumCategoryId_EnumCategoryId] FOREIGN KEY ([EnumCategoryId]) REFERENCES [EnumCategories] ([EnumCategoryId]),
	CONSTRAINT [CHK_Enums_Unique_Name_EnumCategoryId] CHECK ([FUN_CheckUniqueEnumNameAndCategory]([EnumId], [EnumCategoryId], [Name]) = 1)
)