CREATE TABLE [dbo].[Settings](
	[Id] [bigint] IDENTITY(1,1) NOT NULL,
	[Key] [nvarchar](128) NOT NULL,
	[Value] [nvarchar](max) NOT NULL,
	CONSTRAINT [PK_Settings] PRIMARY KEY CLUSTERED 
	(
		[Id] ASC
	)
)

CREATE UNIQUE NONCLUSTERED INDEX [IX_Settings_Key] ON [dbo].[Settings]
(
	[Key] ASC
)