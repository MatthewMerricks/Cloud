/*
 10FileSystemObjects.table.sql
 Cloud Windows

 Created By DavidBruck.
 Copyright (c) Cloud.com. All rights reserved.
*/

CREATE TABLE FileSystemObjects
(
    FileSystemObjectId INTEGER NOT NULL,
    Name TEXT NOT NULL CONSTRAINT CHK_FileSystemObjects_Name_TEXT
      CHECK (TYPEOF(Name) = 'text' AND Name <> ''),
    ParentFolderId INTEGER,
    LastTimeUTCTicks INTEGER CONSTRAINT CHK_FileSystemObjects_LastTimeUTCTicks_INTEGER
      CHECK (LastTimeUTCTicks IS NULL OR TYPEOF(LastTimeUTCTicks) = 'integer'),
    CreationTimeUTCTicks INTEGER CONSTRAINT CHK_FileSystemObjects_CreationTimeUTCTicks_INTEGER
      CHECK (CreationTimeUTCTicks IS NULL OR TYPEOF(CreationTimeUTCTicks) = 'integer'),
    IsFolder INTEGER NOT NULL CONSTRAINT CHK_FileSystemObjects_IsFolder_Boolean
      CHECK (IsFolder = 0 OR IsFolder = 1),
    Size INTEGER CONSTRAINT CHK_FileSystemObjects_Size_INTEGER
      CHECK (Size IS NULL OR TYPEOF(Size) = 'integer'),
    StorageKey TEXT CONSTRAINT CHK_FileSystemObjects_StorageKey_TEXT
      CHECK (StorageKey IS NULL OR TYPEOF(StorageKey) = 'text'),
    ServerName TEXT CONSTRAINT CHK_FileSystemObjects_ServerName_TEXT
      CHECK (ServerName IS NULL OR (TYPEOF(ServerName) = 'text' AND ServerName <> '')),
    EventId INTEGER,
	EventOrder INTEGER,
    IsShare INTEGER CONSTRAINT CHK_FileSystemObjects_IsShare_Boolean
      CHECK (IsShare IS NULL OR IsShare = 0 OR IsShare = 1),
    MD5 BLOB CONSTRAINT CHK_FileSystemObjects_MD5_Hash
      CHECK (MD5 IS NULL OR (TYPEOF(MD5) = 'blob' AND LENGTH(MD5) = 16)),
    Version INTEGER CONSTRAINT CHK_FileSystemObjects_Version_INTEGER
      CHECK (Version IS NULL OR TYPEOF(Version) = 'integer'),
    ServerUidId INTEGER NOT NULL,
    Pending INTEGER NOT NULL CONSTRAINT CHK_FileSystemObjects_Pending_Boolean
      CHECK (Pending = 0 OR Pending = 1),
    SyncCounter INTEGER,
    MimeType TEXT CONSTRAINT CHK_FileSystemObjects_MimeType_TEXT
      CHECK (MimeType IS NULL OR TYPEOF(MimeType) = 'text'),
    Permissions INTEGER CONSTRAINT CHK_FileSystemObjects_Permissions_INTEGER
      CHECK (Permissions IS NULL OR TYPEOF(Permissions) = 'integer'),
    EventTimeUTCTicks INTEGER NOT NULL CONSTRAINT CHK_FileSystemObjects_EventTimeUTCTicks_INTEGER
      CHECK (TYPEOF(EventTimeUTCTicks) = 'integer'),
    CalculatedFullPath TEXT CONSTRAINT CHK_FileSystemObjects_CalculatedFullPath_TEXT
      CHECK (CalculatedFullPath IS NULL OR TYPEOF(CalculatedFullPath) = 'text'),
    PRIMARY KEY (FileSystemObjectId ASC),
    CONSTRAINT FK_FileSystemObjects_ParentFolderId__FileSystemObjects_FileSystemObjectId
      FOREIGN KEY (ParentFolderId)
      REFERENCES FileSystemObjects (FileSystemObjectId),
    CONSTRAINT FK_FileSystemObjects_EventId__Events_EventId
      FOREIGN KEY (EventId)
      REFERENCES Events (EventId),
    CONSTRAINT FK_FileSystemObjects_SyncCounter__Syncs_SyncCounter
      FOREIGN KEY (SyncCounter)
      REFERENCES Syncs (SyncCounter),
    CONSTRAINT FK_FileSystemObjects_ServerUidId__ServerUids_ServerUidId
      FOREIGN KEY (ServerUidId)
      REFERENCES ServerUids (ServerUidId)
);