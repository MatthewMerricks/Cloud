/*
 22FileSystemObjects_Before_Delete.trigger.sql
 Cloud Windows

 Created By DavidBruck.
 Copyright (c) Cloud.com. All rights reserved.
*/

CREATE TRIGGER TGR_FileSystemObjects_Before_Delete
BEFORE DELETE
ON FileSystemObjects
FOR EACH ROW
BEGIN
  DELETE
  FROM FileSystemObjects
  WHERE ParentFolderId = OLD.FileSystemObjectId;
END