/*
 18FileSystemObjects_Name_Update.trigger.sql
 Cloud Windows

 Created By DavidBruck.
 Copyright (c) Cloud.com. All rights reserved.
*/

CREATE TRIGGER TGR_FileSystemObjects_Name_Update
AFTER UPDATE OF Name
ON FileSystemObjects
FOR EACH ROW
WHEN
  OLD.Name <> NEW.Name
BEGIN
  --INSERT INTO Trace (String, IsError)
  --VALUES (
  --  'trigger: Name Update begin body.'
  --    || ' Fields that won''t change: '
  --    || 'ParentFolderId = '
  --    || (CASE WHEN NEW.ParentFolderId IS NULL THEN 'NULL' ELSE NEW.ParentFolderId END)
  --    || ', Name = '
  --    || NEW.Name -- should never be null
  --    || '. Before update fields: CalculatedFullPath = '
  --    || (CASE WHEN NEW.CalculatedFullPath IS NULL THEN '' ELSE NEW.CalculatedFullPath END),
  --  0 -- IsError
  --);

  UPDATE FileSystemObjects
  SET CalculatedFullPath =
  (
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
  ) || Name
  WHERE FileSystemObjectId = NEW.FileSystemObjectId;

  --INSERT INTO Trace (String, IsError)
  --VALUES (
  --  'trigger: Name Update end body. After update fields: CalculatedFullPath = '
  --    || (
  --        SELECT UpdatedRowPath
  --        FROM
  --        (
  --          SELECT UpdatedRow.CalculatedFullPath AS UpdatedRowPath
  --          FROM FileSystemObjects UpdatedRow
  --          WHERE UpdatedRow.FileSystemObjectId = NEW.FileSystemObjectId
  --            AND UpdatedRow.CalculatedFullPath IS NOT NULL
  --          UNION SELECT 'NULL'
  --        )
  --        ORDER BY UpdatedRowPath = 'NULL'
  --        LIMIT 1
  --      ),
  --  0 -- IsError
  --);
END