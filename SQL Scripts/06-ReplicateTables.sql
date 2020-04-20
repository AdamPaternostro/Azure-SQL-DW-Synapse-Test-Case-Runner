/****** Object:  StoredProcedure [dbo].[ReplicateTables]    Script Date: 3/18/2020 2:11:18 PM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROC [dbo].[ReplicateTables] AS
BEGIN
    -- SET NOCOUNT ON added to prevent extra result sets from
    -- interfering with SELECT statements.
    SET NOCOUNT ON

	CREATE TABLE #tbl
	WITH
	( DISTRIBUTION = ROUND_ROBIN
	)
	AS
	SELECT ROW_NUMBER() OVER(ORDER BY (SELECT NULL)) AS Sequence,
	       t.[name],
		   'SELECT TOP 1 * FROM [' + sch.[name] + '].[' + t.[name] + '];' AS sql_code
	  FROM sys.tables t  
	  JOIN sys.pdw_replicated_table_cache_state c  
		ON c.object_id = t.object_id 
	  JOIN sys.pdw_table_distribution_properties p 
		ON p.object_id = t.object_id 
	  JOIN sys.schemas sch
		ON t.schema_id = sch.schema_id
	 WHERE c.[state] = 'NotReady'
  	   AND p.[distribution_policy_desc] = 'REPLICATE'


	DECLARE @nbr_statements INT = (SELECT COUNT(*) FROM #tbl)
	,       @i INT = 1
	;

	WHILE   @i <= @nbr_statements
	BEGIN
		DECLARE @sql_code NVARCHAR(4000) = (SELECT sql_code FROM #tbl WHERE Sequence = @i);
		EXEC    sp_executesql @sql_code;
		SET     @i +=1;
		PRINT	@sql_code;
	END

	DROP TABLE #tbl;


END
GO

