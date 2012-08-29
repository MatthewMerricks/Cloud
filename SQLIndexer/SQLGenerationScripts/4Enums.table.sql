/*
 * Enums.table.sql
 * Cloud Windows
 *
 * Created By DavidBruck.
 * Copyright (c) Cloud.com. All rights reserved.
 */
CREATE TABLE [Enums]
(
	[EnumId] int NOT NULL,
	[EnumCategoryId] int NOT NULL,
	[Name] ntext /*CE Limitation: COLLATE Latin1_General_CS_AS*/ NOT NULL /*CE Limitation: CHECK ([Name] <> '')*//*CE Limitation: ,
	CONSTRAINT [PK_Enums] PRIMARY KEY CLUSTERED ([EnumId] ASC, [EnumCategoryId] ASC)*//*Begin CE Only*/,
	PRIMARY KEY ([EnumId], [EnumCategoryId])/*End CE Only*/,
	CONSTRAINT [FK_EnumCategoryId_EnumCategoryId] FOREIGN KEY ([EnumCategoryId]) REFERENCES [EnumCategories] ([EnumCategoryId])/*CE Limitation: ,
	CONSTRAINT [CHK_Enums_Unique_Name_EnumCategoryId] CHECK ([dbo].[FUN_CheckUniqueEnumNameAndCategory]([EnumId], [EnumCategoryId], [Name]) = 1)*/
)