/*
 3AddFileChangeTypeEnumCategory.data.sql
 Cloud Windows

 Created By DavidBruck.
 Copyright (c) Cloud.com. All rights reserved.
*/

INSERT INTO EnumCategories (EnumCategoryId, Name)
SELECT 0 as EnumCategoryId, 'FileChangeType' as Name
/*
  How to add additional enumeration categories from here:

  UNION SELECT 1, 'MyEnumType2'
  UNION SELECT 2, 'MyEnumType3'
*/;