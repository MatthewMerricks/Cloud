INSERT INTO [EnumCategories]
(
	[EnumCategoryId],
	[Name]
)
VALUES
(
	0,
	'FileChangeType'
)

INSERT INTO [Enums]
(
	[EnumId],
	[EnumCategoryId],
	[Name]
)
VALUES
(
	0,
	0,
	'Created'
),
(
	1,
	0,
	'Modified'
),
(
	2,
	0,
	'Deleted'
),
(
	3,
	0,
	'Renamed'
)