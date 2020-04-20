/****** Object:  Table [telemetry].[AutomatedTest_exec_requests]    Script Date: 3/18/2020 2:10:48 PM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [telemetry].[AutomatedTest_exec_requests]
(
	[request_id] [nvarchar](32) NULL,
	[session_id] [nvarchar](32) NULL,
	[status] [nvarchar](32) NULL,
	[submit_time] [datetime] NULL,
	[start_time] [datetime] NULL,
	[end_compile_time] [datetime] NULL,
	[end_time] [datetime] NULL,
	[total_elapsed_time] [int] NULL,
	[label] [nvarchar](255) NULL,
	[error_id] [nvarchar](36) NULL,
	[database_id] [int] NULL,
	[command] [nvarchar](4000) NULL,
	[resource_class] [nvarchar](20) NULL,
	[importance] [nvarchar](128) NULL,
	[group_name] [nvarchar](128) NULL,
	[classifier_name] [nvarchar](128) NULL,
	[resource_allocation_percentage] [decimal](5, 2) NULL,
	[result_cache_hit] [bit] NULL
)
WITH
(
	DISTRIBUTION = ROUND_ROBIN,
	HEAP
)
GO

