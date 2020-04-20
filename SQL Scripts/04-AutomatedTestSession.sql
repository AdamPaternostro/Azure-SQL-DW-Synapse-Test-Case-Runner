/****** Object:  Table [telemetry].[AutomatedTestSession]    Script Date: 3/18/2020 2:11:03 PM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [telemetry].[AutomatedTestSession]
(
	[AutomatedTestId] [int] NULL,
	[session_id] [varchar](32) NULL
)
WITH
(
	DISTRIBUTION = HASH ( [AutomatedTestId] ),
	CLUSTERED COLUMNSTORE INDEX
)
GO

