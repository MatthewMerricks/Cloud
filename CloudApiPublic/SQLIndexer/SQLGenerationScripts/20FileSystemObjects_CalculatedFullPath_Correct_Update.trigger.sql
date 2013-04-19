/*
 20FileSystemObjects_CalculatedFullPath_Correct_Update.trigger.sql
 Cloud Windows

 Created By DavidBruck.
 Copyright (c) Cloud.com. All rights reserved.
*/

CREATE TRIGGER TGR_FileSystemObjects_CalculatedFullPath_Correct_Update
AFTER UPDATE OF CalculatedFullPath
ON FileSystemObjects
FOR EACH ROW
WHEN
  NEW.CalculatedFullPath IS NOT NULL
  AND OLD.CalculatedFullPath <> NEW.CalculatedFullPath
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
  --  'trigger: CalculatedFullPath Update Correct.'
  --    || ' Previous CalculatedFullPath = '
  --    || (CASE WHEN OLD.CalculatedFullPath IS NULL THEN 'NULL' ELSE OLD.CalculatedFullPath END)
  --    || ', New CalculatedFullPath = '
  --    || (CASE WHEN NEW.CalculatedFullPath IS NULL THEN 'NULL' ELSE NEW.CalculatedFullPath END)
  --    || '. About to update children''s CalculatedFullPaths where their ParentFolderId equals '
  --    || NEW.FileSystemObjectId,
  --  0 -- IsError
  --);

  UPDATE FileSystemObjects
  SET CalculatedFullPath = NEW.CalculatedFullPath || '\' || Name
  WHERE ParentFolderId = NEW.FileSystemObjectId;
END