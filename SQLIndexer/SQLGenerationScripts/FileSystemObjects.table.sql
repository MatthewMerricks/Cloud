CREATE TABLE [FileSystemObjects]
(
	[FileSystemObjectId] bigint IDENTITY(1, 1) NOT NULL,
	[Path] nvarchar(max) COLLATE Latin1_General_CS_AS NOT NULL CHECK ([Path] <> ''),
	[LastTime] datetime2 NULL, --the greater of the LastAccessedTimeUtc and LastModifiedTimeUtc
	[CreationTime] datetime2 NULL, --CreationTimeUtc
	[IsFolder] bit NOT NULL, --1 for folders (empty or not empty), 0 for files
	[Size] bigint NULL, --set only for files
	[TargetPath] nvarchar(max) COLLATE Latin1_General_CS_AS NULL CHECK ([TargetPath] <> ''),
	[PathChecksum] AS CHECKSUM([Path]),
	[Revision] nvarchar(max) COLLATE Latin1_General_CS_AS NULL CHECK ([Revision] <> ''),
	[StorageKey] nvarchar(max) COLLATE Latin1_General_CS_AS NULL CHECK ([StorageKey] <> ''),
	/* does this belong? */ /*[MD5] binary(15) NULL,*/
	CONSTRAINT [PK_FileSystemObjects] PRIMARY KEY CLUSTERED ([FileSystemObjectId] ASC),
	CONSTRAINT [CHK_FileSystemObjects_SizeSet] CHECK (([IsFolder] = 1 AND [Size] IS NULL) OR ([IsFolder] = 0 AND [Size] IS NOT NULL)),
	CONSTRAINT [CHK_FileSystemObjects_TargetPathForFilesOnly] CHECK ([IsFolder] = 0 OR ([IsFolder] = 1 AND [TargetPath] IS NULL))
)