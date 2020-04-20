/****** Object:  Table [telemetry].[AutomatedTest]    Script Date: 3/18/2020 2:10:01 PM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [telemetry].[AutomatedTest]
(
	[AutomatedTestId] [int] NOT NULL,
	[Description] [varchar](300) NULL,
	[Mode] [varchar](100) NULL,
	[StartTime] [datetime2](7) NOT NULL,
	[EndTime] [datetime2](7) NULL,
	[DWU] [int] NOT NULL,
	[CacheState] [varchar](50) NOT NULL,
	[OptLevel] [varchar](50) NOT NULL,
	[ScriptMods] [varchar](50) NOT NULL,
	[ResourceClass] [nvarchar](20) NULL
)
WITH
(
	DISTRIBUTION = HASH ( [AutomatedTestId] ),
	CLUSTERED COLUMNSTORE INDEX
)
GO

