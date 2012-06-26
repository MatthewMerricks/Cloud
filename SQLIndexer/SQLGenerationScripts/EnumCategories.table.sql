CREATE TABLE [EnumCategories]
(
	[EnumCategoryId] int NOT NULL,
	[Name] nvarchar(max) COLLATE Latin1_General_CS_AS NOT NULL CHECK([Name] <> ''),
	CONSTRAINT [PK_EnumCategories] PRIMARY KEY CLUSTERED ([EnumCategoryId] ASC)
)