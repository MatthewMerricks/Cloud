/*
 * CheckUniqueEnumNameAndCategory.function.sql
 * Cloud Windows
 *
 * Created By DavidBruck.
 * Copyright (c) Cloud.com. All rights reserved.
 */
/*CE Limitation: CREATE FUNCTION [FUN_CheckUniqueEnumNameAndCategory]
(
	@EnumId int,
	@EnumCategoryId int,
	@Name nvarchar(max)
)
RETURNS BIT
AS
BEGIN
	DECLARE @IsUnique BIT

	SET @IsUnique = (SELECT CASE WHEN NOT EXISTS
	(
		SELECT NULL
		FROM [Enums]
		WHERE [Enums].[EnumCategoryId] = @EnumCategoryId
			AND [Enums].[EnumId] <> @EnumId
			AND [Enums].[Name] = @Name
	)
	THEN 1
	ELSE 0
	END)

	RETURN @IsUnique
END*/