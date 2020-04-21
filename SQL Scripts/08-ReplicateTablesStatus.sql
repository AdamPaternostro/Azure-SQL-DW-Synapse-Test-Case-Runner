/****** Object:  StoredProcedure [dbo].[ReplicateTablesStatus]    Script Date: 3/18/2020 2:11:30 PM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROC [dbo].[ReplicateTablesStatus] AS
BEGIN
    -- SET NOCOUNT ON added to prevent extra result sets from
    -- interfering with SELECT statements.
    SET NOCOUNT ON

    -- Insert statements for procedure here
	SELECT '[' + sch.[name] + '].[' + t.[name] + '];' AS table_name, c.[state]
	  FROM sys.tables t  
	  JOIN sys.pdw_replicated_table_cache_state c  
		ON c.object_id = t.object_id 
	  JOIN sys.pdw_table_distribution_properties p 
		ON p.object_id = t.object_id 
	  JOIN sys.schemas sch
		ON t.schema_id = sch.schema_id
	 WHERE p.[distribution_policy_desc] = 'REPLICATE'
	ORDER BY c.[state], table_name

END
GO

