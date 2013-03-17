/*
 2EnumCategories.table.sql
 Cloud Windows

 Created By DavidBruck.
 Copyright (c) Cloud.com. All rights reserved.
*/

CREATE TABLE EnumCategories
(
    EnumCategoryId INTEGER NOT NULL,
    Name TEXT NOT NULL UNIQUE CONSTRAINT CHK_EnumCategories_Name_TEXT
      CHECK (TYPEOF(Name) = 'text' AND Name <> ''),
    PRIMARY KEY (EnumCategoryId ASC)
);