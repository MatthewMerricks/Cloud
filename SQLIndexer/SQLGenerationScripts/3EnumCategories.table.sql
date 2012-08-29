/*
 * EnumCategories.table.sql
 * Cloud Windows
 *
 * Created By DavidBruck.
 * Copyright (c) Cloud.com. All rights reserved.
 */
CREATE TABLE [EnumCategories]
(
	[EnumCategoryId] int /*Begin CE Only*/PRIMARY KEY/*End CE Only*/ NOT NULL,
	[Name] ntext /*CE Limitation: COLLATE Latin1_General_CS_AS*/ NOT NULL /*CE Limitation: CHECK ([Name] <> '')*//*CE Limitation: ,
	CONSTRAINT [PK_EnumCategories] PRIMARY KEY CLUSTERED ([EnumCategoryId] ASC)*/
)