/*
 20FileSystemObjects_Incorrect_Insert.trigger.sql
 Cloud Windows

 Created By DavidBruck.
 Copyright (c) Cloud.com. All rights reserved.
*/

CREATE TRIGGER TGR_FileSystemObjects_Incorrect_Insert
AFTER INSERT
ON FileSystemObjects
FOR EACH ROW
WHEN
  NEW.CalculatedFullPath IS NULL
  OR NEW.CalculatedFullPath <>
    ((
      SELECT ParentName
      FROM
      (
        SELECT Parent.CalculatedFullPath || '\' AS ParentName
        FROM FileSystemObjects Parent
        WHERE Parent.FileSystemObjectId = NEW.ParentFolderId
        UNION SELECT ''
      )
      ORDER BY ParentName == ''
      LIMIT 1
    ) || NEW.Name)
BEGIN
  --INSERT INTO Trace (String, IsError)
  --VALUES (
  --  'trigger: CalculatedFullPath Incorrect on Insert.'
  --    || ' Inserted CalculatedFullPath = '
  --    || (CASE WHEN NEW.CalculatedFullPath IS NULL THEN 'NULL' ELSE NEW.CalculatedFullPath END)
  --    || '. About to update current row''s CalculatedFullPath by FileSystemObjectId = '
  --    || NEW.FileSystemObjectId,
  --  0 -- IsError
  --);

  UPDATE FileSystemObjects
  SET CalculatedFullPath =
    ((
      SELECT ParentName
      FROM
      (
        SELECT Parent.CalculatedFullPath || '\' AS ParentName
        FROM FileSystemObjects Parent
        WHERE Parent.FileSystemObjectId = NEW.ParentFolderId
        UNION SELECT ''
      )
      ORDER BY ParentName == ''
      LIMIT 1
    ) || Name)
  WHERE FileSystemObjects.FileSystemObjectId = NEW.FileSystemObjectId;
END