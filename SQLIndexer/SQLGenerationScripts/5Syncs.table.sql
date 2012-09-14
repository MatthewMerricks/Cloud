/*
 * Syncs.table.sql
 * Cloud Windows
 *
 * Created By DavidBruck.
 * Copyright (c) Cloud.com. All rights reserved.
 */
CREATE TABLE [Syncs]
(
	[SyncId] nvarchar(450) /*CE Limitation: COLLATE Latin1_General_CS_AS*/ NOT NULL,
	[SyncCounter] bigint IDENTITY(1, 1) /*Begin CE Only*/PRIMARY KEY/*End CE Only*/ NOT NULL,
	[RootPath] ntext /*CE Limitation: COLLATE Latin1_General_CS_AS*/ NOT NULL /*CE Limitation: CHECK ([RootPath] <> '')*//*CE Limitation: ,
	CONSTRAINT [PK_Syncs] PRIMARY KEY CLUSTERED ([SyncCounter] ASC)*/
)