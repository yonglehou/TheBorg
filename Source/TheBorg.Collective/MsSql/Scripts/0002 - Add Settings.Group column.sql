IF NOT EXISTS(SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Settings' AND COLUMN_NAME = 'GroupKey')
BEGIN
	DROP INDEX [IX_Settings_Key] ON [dbo].[Settings]

	ALTER TABLE [dbo].[Settings]
		ADD [GroupKey] [nvarchar](128) NOT NULL
		CONSTRAINT [DF_GroupKey] DEFAULT ''

	CREATE UNIQUE NONCLUSTERED INDEX [IX_Settings_GroupKey] ON [dbo].[Settings]
	(
		[Key] ASC,
		[GroupKey] ASC
	)

	ALTER TABLE [dbo].[Settings] DROP CONSTRAINT [DF_GroupKey]
END