CREATE TABLE [SyncStates]
(
	[SyncStateId] int IDENTITY(1, 1) NOT NULL,
	[SyncId] uniqueidentifier NOT NULL,
	[FileSystemObjectId] int NOT NULL,
	[ServerLinkedFileSystemObjectId] int NULL,
	CONSTRAINT [PK_SyncStates] PRIMARY KEY NONCLUSTERED ([SyncStateId] ASC),
	CONSTRAINT [FK_SyncStates_SyncId_SyncId] FOREIGN KEY ([SyncId]) REFERENCES [Syncs] ([SyncId]),
	CONSTRAINT [FK_SyncStates_FileSystemObjectId_FileSystemObjectId] FOREIGN KEY ([FileSystemObjectId]) REFERENCES [FileSystemObjects] ([FileSystemObjectId]),
	CONSTRAINT [FK_SyncStates_ServerLinkedFileSystemObjectId_FileSystemObjectId] FOREIGN KEY ([ServerLinkedFileSystemObjectId]) REFERENCES [FileSystemObjects] ([FileSystemObjectId])
)