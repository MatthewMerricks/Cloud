/*
 5AddFileChangeTypeEnumValues.data.sql
 Cloud Windows

 Created By DavidBruck.
 Copyright (c) Cloud.com. All rights reserved.
*/

INSERT INTO Enums (EnumId, EnumCategoryId, Name)
SELECT 0 as EnumId, 0 as EnumCategoryId, 'Created'
  UNION SELECT 1, 0, 'Modified'
  UNION SELECT 2, 0, 'Deleted'
  UNION SELECT 3, 0, 'Renamed'