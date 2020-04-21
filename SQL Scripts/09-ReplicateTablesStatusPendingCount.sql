-- ======================================================================
-- Create Stored Procedure Template for Azure SQL Data Warehouse Database
-- ======================================================================
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE dbo.ReplicateTablesStatusPendingCount
AS
BEGIN
    -- SET NOCOUNT ON added to prevent extra result sets from
    -- interfering with SELECT statements.
    SET NOCOUNT ON

	-- This should return Zero 0 when everything is ready (or if there are not any tables)
	SELECT COUNT(*) - ISNULL(SUM(CASE WHEN c.[state] = 'Ready' THEN 1 ELSE 0 END) ,0)
	  FROM sys.tables t  
		   INNER JOIN sys.pdw_replicated_table_cache_state c  
		           ON c.object_id = t.object_id 
		   INNER JOIN sys.pdw_table_distribution_properties p 
		           ON p.object_id = t.object_id 
		          AND p.[distribution_policy_desc] = 'REPLICATE'
		   INNER JOIN sys.schemas sch
		           ON t.schema_id = sch.schema_id
END
GO
