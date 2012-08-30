/*
 * FileSystemObjects.table.sql
 * Cloud Windows
 *
 * Created By DavidBruck.
 * Copyright (c) Cloud.com. All rights reserved.
 */
CREATE TABLE [FileSystemObjects]
(
	[FileSystemObjectId] bigint /*Begin CE Only*/PRIMARY KEY/*End CE Only*/ IDENTITY(1, 1) NOT NULL,
	[Path] ntext /*CE Limitation: COLLATE Latin1_General_CS_AS*/ NOT NULL /*CE Limitation: CHECK ([Path] <> '')*/,
	[LastTime] datetime/*CE Limitation: 2*/ NULL, --the greater of the LastAccessedTimeUtc and LastModifiedTimeUtc
	[CreationTime] datetime/*CE Limitation: 2*/ NULL, --CreationTimeUtc
	[IsFolder] bit NOT NULL, --1 for folders (empty or not empty), 0 for files
	[Size] bigint NULL, --set only for files
	[TargetPath] ntext /*CE Limitation: COLLATE Latin1_General_CS_AS*/ NULL /*CE Limitation: CHECK([TargetPath] <> '')*/,
	[PathChecksum] /*CE Limitation: AS CHECKSUM([Path])*//*Begin CE Only*/int NOT NULL/*End CE Only*/,
	[Revision] nvarchar(128) /*CE Limitation: COLLATE Latin1_General_CS_AS*/ NOT NULL /*CE Limitation: CHECK ([Revision] <> '')*//*Begin CE Only*/,
	[RevisionIsNull] bit NOT NULL/*End CE Only*/,
	[StorageKey] ntext /*CE Limitation: COLLATE Latin1_General_CS_AS*/ NULL /*CE Limitation: CHECK ([StorageKey] <> '')*//*CE Limitation: ,
	CONSTRAINT [PK_FileSystemObjects] PRIMARY KEY CLUSTERED ([FileSystemObjectId] ASC)*//*CE Limitation: ,
	CONSTRAINT [CHK_FileSystemObjects_SizeSet] CHECK (([IsFolder] = 1 AND [Size] IS NULL) OR ([IsFolder] = 0 AND [Size] IS NOT NULL))*//*CE Limitation: ,
	CONSTRAINT [CHK_FileSystemObjects_TargetPathForFilesOnly] CHECK ([IsFolder] = 0 OR ([IsFolder] = 1 AND [TargetPath] IS NULL))*/
)