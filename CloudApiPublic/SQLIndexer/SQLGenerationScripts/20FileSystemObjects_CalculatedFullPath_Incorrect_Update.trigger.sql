/*
 20FileSystemObjects_CalculatedFullPath_Incorrect_Update.trigger.sql
 Cloud Windows

 Created By DavidBruck.
 Copyright (c) Cloud.com. All rights reserved.
*/

CREATE TRIGGER TGR_FileSystemObjects_CalculatedFullPath_Incorrect_update
BEFORE UPDATE OF CalculatedFullPath
ON FileSystemObjects
FOR EACH ROW
WHEN
  NEW.CalculatedFullPath IS NULL
  OR (OLD.CalculatedFullPath <> NEW.CalculatedFullPath
    AND NEW.CalculatedFullPath <>
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
    ) || NEW.Name))
BEGIN
  --INSERT INTO Trace (String, IsError)
  --VALUES (
  --  'trigger: CalculatedFullPath Update Incorrect.'
  --    || ' Previous CalculatedFullPath = '
  --    || (CASE WHEN OLD.CalculatedFullPath IS NULL THEN 'NULL' ELSE OLD.CalculatedFullPath END)
  --    || ', New CalculatedFullPath = '
  --    || (CASE WHEN NEW.CalculatedFullPath IS NULL THEN 'NULL' ELSE NEW.CalculatedFullPath END),
  --  1 -- IsError
  --);

  SELECT RAISE(FAIL, 'Cannot set calculated field CalculatedFullPath to incorrect value. Remove calculated fields from update colums (Mark with SQLAccess.FieldType.Readonly).');
END