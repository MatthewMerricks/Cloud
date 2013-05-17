/*
 26FileSystemObjects_EventId_Update.trigger.sql
 Cloud Windows

 Created By DavidBruck.
 Copyright (c) Cloud.com. All rights reserved.
*/

CREATE TRIGGER TGR_FileSystemObjects_EventId_Update
AFTER UPDATE OF EventId
ON FileSystemObjects
FOR EACH ROW
WHEN
	(OLD.EventId IS NULL AND NEW.EventId IS NOT NULL)
	OR (OLD.EventId IS NOT NULL AND NEW.EventId IS NULL)
	OR (OLD.EventId IS NOT NULL AND NEW.EventId IS NOT NULL AND OLD.EventId <> NEW.EventId)
BEGIN
  UPDATE FileSystemObjects
  SET EventOrder = NEW.EventId
  WHERE FileSystemObjects.FileSystemObjectId = NEW.FileSystemObjectId;
END