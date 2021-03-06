/****** Object:  Table [telemetry].[AutomatedTest]    Script Date: 3/18/2020 2:10:01 PM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [telemetry].[AutomatedTest]
(
	[AutomatedTestId] [int] NOT NULL,
	[ScriptMods] [varchar](500) NOT NULL,
	[Mode] [varchar](500) NULL,
	[DWU] [varchar](500) NULL,
	[ReplicatedTables] [bit] NULL,
	[Interations] [int] NULL,
	[MinStatisticDate] [datetime2](7) NOT NULL,
	[ResultSetCaching] [bit] NOT NULL,
	[ResourceClass] [nvarchar](500) NULL,
	[OptLevel] [varchar](500) NOT NULL,
	[StartTime] [datetime2](7) NOT NULL,
	[EndTime] [datetime2](7) NULL
)
WITH
(
	DISTRIBUTION = HASH ( [AutomatedTestId] ),
	CLUSTERED COLUMNSTORE INDEX
)
GO

