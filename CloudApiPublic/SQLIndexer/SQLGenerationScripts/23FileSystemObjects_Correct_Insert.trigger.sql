/*
 23FileSystemObjects_Correct_Insert.trigger.sql
 Cloud Windows

 Created By DavidBruck.
 Copyright (c) Cloud.com. All rights reserved.
*/

CREATE TRIGGER TGR_FileSystemObjects_Correct_Insert
AFTER INSERT
ON FileSystemObjects
FOR EACH ROW
WHEN
  NEW.CalculatedFullPath IS NOT NULL
  AND NEW.CalculatedFullPath =
    ((
      SELECT ParentName
      FROM
      (
        SELECT Parent.CalculatedFullPath || '\' AS ParentName
        FROM FileSystemObjects Parent
        WHERE Parent.FileSystemObjectId = NEW.ParentFolderId
        UNION SELECT ''
      )
      ORDER BY ParentName = ''
      LIMIT 1
    ) || NEW.Name)
BEGIN
  --INSERT INTO Trace (String, IsError)
  --VALUES (
  --  'trigger: CalculatedFullPath Correct on Insert.'
  --    || ' Inserted CalculatedFullPath = '
  --    || NEW.CalculatedFullPath -- not null because it was checked in WHEN clause above
  --    || '. About to update children''s CalculatedFullPaths where their ParentFolderId equals '
  --    || NEW.FileSystemObjectId,
  --  0 -- IsError
  --);

  UPDATE FileSystemObjects
  SET CalculatedFullPath = NEW.CalculatedFullPath || '\' || Name
  WHERE ParentFolderId = NEW.FileSystemObjectId;

  UPDATE FileSystemObjects
  SET EventOrder = EventId
  WHERE FileSystemObjects.FileSystemObjectId = NEW.FileSystemObjectId;
END