/*
 24FileSystemObjects_After_Delete.trigger.sql
 Cloud Windows

 Created By DavidBruck.
 Copyright (c) Cloud.com. All rights reserved.
*/

CREATE TRIGGER TGR_FileSystemObjects_After_Delete
BEFORE DELETE
ON FileSystemObjects
FOR EACH ROW
BEGIN
  DELETE
  FROM Events
  WHERE EventId = OLD.EventId;
END