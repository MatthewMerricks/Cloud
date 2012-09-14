/*
 * Events_GroupId.index.sql
 * Cloud Windows
 *
 * Created By DavidBruck.
 * Copyright (c) Cloud.com. All rights reserved.
 */
CREATE NONCLUSTERED INDEX [Idx_Events_GroupId] ON [Events]
(
    [GroupId] ASC
)