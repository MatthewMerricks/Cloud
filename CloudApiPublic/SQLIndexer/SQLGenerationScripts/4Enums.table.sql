/*
 4Enums.table.sql
 Cloud Windows

 Created By DavidBruck.
 Copyright (c) Cloud.com. All rights reserved.
*/

CREATE TABLE Enums
(
    EnumId INTEGER NOT NULL,
    EnumCategoryId INTEGER NOT NULL,
    Name TEXT NOT NULL
      CONSTRAINT CHK_Enums_Name_TEXT
      CHECK (TYPEOF(Name) = 'text' AND Name <> ''),
    PRIMARY KEY (EnumId ASC, EnumCategoryId ASC),
    CONSTRAINT IDX_Enums_EnumCategoryId_Name
      UNIQUE (EnumCategoryId ASC, Name ASC),
    CONSTRAINT FK_Enums_EnumCategoryId__EnumCategories_EnumCategoryId
      FOREIGN KEY (EnumCategoryId)
      REFERENCES EnumCategories (EnumCategoryId)
);