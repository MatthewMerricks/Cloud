/*
 * SyncStates.table.sql
 * Cloud Windows
 *
 * Created By DavidBruck.
 * Copyright (c) Cloud.com. All rights reserved.
 */
CREATE TABLE [SyncStates]
(
	[SyncStateId] bigint /*Begin CE Only*/PRIMARY KEY/*End CE Only*/ IDENTITY(1, 1) NOT NULL,
	[SyncCounter] bigint NOT NULL,
	[FileSystemObjectId] bigint NOT NULL,
	[ServerLinkedFileSystemObjectId] bigint NULL/*CE Limitation: ,
	CONSTRAINT [PK_SyncStates] PRIMARY KEY NONCLUSTERED ([SyncStateId] ASC)*/,
	CONSTRAINT [FK_SyncStates_SyncCounter_SyncCounter] FOREIGN KEY ([SyncCounter]) REFERENCES [Syncs] ([SyncCounter]),
	CONSTRAINT [FK_SyncStates_FileSystemObjectId_FileSystemObjectId] FOREIGN KEY ([FileSystemObjectId]) REFERENCES [FileSystemObjects] ([FileSystemObjectId]),
	CONSTRAINT [FK_SyncStates_ServerLinkedFileSystemObjectId_FileSystemObjectId] FOREIGN KEY ([ServerLinkedFileSystemObjectId]) REFERENCES [FileSystemObjects] ([FileSystemObjectId])
)