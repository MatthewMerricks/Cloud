/*
 * CompletedVersion.data.sql
 * Cloud Windows
 *
 * Created By DavidBruck.
 * Copyright (c) Cloud.com. All rights reserved.
 */
UPDATE [Version]
SET [Version].[Version] = 2 /* <-- Incremement this version number everytime one of the SQLGenerationScripts change.
 * Make sure to update the corresponding scripts in IndexDBScripts.
 * Also, for every delta between versions, create new version delta scripts for database migration (never delete old migration scripts!)
 */