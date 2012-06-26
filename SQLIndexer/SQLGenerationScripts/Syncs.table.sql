CREATE TABLE [Syncs]
(
	[SyncId] uniqueidentifier NOT NULL,
	[RootPath] nvarchar(max) COLLATE Latin1_General_CS_AS NOT NULL CHECK ([RootPath] <> '')
	CONSTRAINT [PK_Syncs] PRIMARY KEY CLUSTERED ([SyncId] ASC)
)