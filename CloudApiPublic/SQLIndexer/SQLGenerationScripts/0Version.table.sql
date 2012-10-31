/*
 * Version.table.sql
 * Cloud Windows
 *
 * Created By DavidBruck.
 * Copyright (c) Cloud.com. All rights reserved.
 */
CREATE TABLE [Version]
(
	[TrueKey] bit /*Begin CE Only*/PRIMARY KEY/*End CE Only*/ NOT NULL,
	[Version] int NOT NULL/*CE Limitation: ,
	CONSTRAINT [PK_Version] PRIMARY KEY CLUSTERED ([TrueKey] ASC)*/
)