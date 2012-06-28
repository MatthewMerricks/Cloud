CREATE TABLE [Syncs]
(
	[SyncId] nvarchar(450) COLLATE Latin1_General_CS_AS NOT NULL,
	[SyncCounter] int IDENTITY(1, 1) NOT NULL,
	[RootPath] nvarchar(max) COLLATE Latin1_General_CS_AS NOT NULL CHECK ([RootPath] <> '')
	CONSTRAINT [PK_Syncs] PRIMARY KEY CLUSTERED ([SyncCounter] ASC)
)