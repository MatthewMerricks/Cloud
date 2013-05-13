/*
 7Events.table.sql
 Cloud Windows

 Created By DavidBruck.
 Copyright (c) Cloud.com. All rights reserved.
*/

CREATE TABLE Events
(
    EventId INTEGER PRIMARY KEY AUTOINCREMENT,
    FileChangeTypeCategoryId INTEGER NOT NULL DEFAULT 0 CONSTRAINT CHK_Events_FileChangeTypeCategoryId_Zero
      CHECK (FileChangeTypeCategoryId = 0),
    FileChangeTypeEnumId INTEGER NOT NULL,
    SyncFrom INTEGER NOT NULL CONSTRAINT CHK_Events_SyncFrom_Boolean
      CHECK (SyncFrom = 0 OR SyncFrom = 1),
    GroupId BLOB CONSTRAINT CHK_Events_SyncFrom_Guid
	  CHECK (GroupId IS NULL OR (TYPEOF(GroupId) = 'blob' AND LENGTH(GroupId) = 16)),
    GroupOrder INTEGER CONSTRAINT CHK_Events_GroupOrder_INTEGER
      CHECK (GroupOrder IS NULL OR TYPEOF(GroupOrder) = 'integer'),
    PreviousId INTEGER,
    CONSTRAINT IDX_Events_GroupOrder_GroupId
      UNIQUE (GroupOrder ASC, GroupId ASC),
    CONSTRAINT FK_Events_FileChangeTypeEnumId_FileChangeTypeCategoryId__Enums_EnumId_EnumCategoryId
      FOREIGN KEY (FileChangeTypeEnumId, FileChangeTypeCategoryId)
      REFERENCES Enums (EnumId, EnumCategoryId),
    CONSTRAINT CHK_Events_BothOrNeitherGroupFields
      CHECK ((GroupId IS NULL AND GroupOrder IS NULL) OR (GroupId IS NOT NULL AND GroupOrder IS NOT NULL)),
    CONSTRAINT FK_Events_PreviousId__FileSystemObjects_FileSystemObjectId
      FOREIGN KEY (PreviousId)
      REFERENCES FileSystemObjects (FileSystemObjectId)
);