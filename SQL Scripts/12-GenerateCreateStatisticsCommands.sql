-- ======================================================================
-- Create Stored Procedure Template for Azure SQL Data Warehouse Database
-- ======================================================================
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- =============================================
-- Description: Run this command to generate the commands to be used to generate statistics in your database
-- =============================================
CREATE PROCEDURE dbo.GenerateCreateStatisticsCommands
AS
BEGIN
-- SET NOCOUNT ON added to prevent extra result sets from
-- interfering with SELECT statements.
SET NOCOUNT ON

DECLARE @TableName  VARCHAR(128) = '%'
DECLARE @SchemaName VARCHAR(128) = 'dbo'

SELECT sys.tables.name  AS TableName,
       sys.columns.name AS ColumnName,
       'CREATE STATISTICS [stat_' + OBJECT_NAME(sys.columns.object_id) + '_' + sys.columns.name + '] ON [' + 
	   DB_Name() +'].[' + @SchemaName + '].[' + OBJECT_NAME(sys.columns.object_id) + '] ([' + sys.columns.name + ']) WITH FULLSCAN;' AS CommandToExecute
  FROM sys.tables
       INNER JOIN sys.columns 
               ON sys.tables.object_id = sys.columns.object_id
        LEFT JOIN (SELECT sys.stats_columns.object_id,
                          sys.stats.name,
                          sys.stats.auto_created,
                          sys.stats.user_created,
                          sys.stats_columns.stats_id,
                          sys.stats_columns.column_id
                     FROM sys.stats_columns
                          INNER JOIN sys.stats
                                  ON sys.stats_columns.object_id = sys.stats.object_id
                                 AND sys.stats_columns.stats_id  = sys.stats.stats_id
                                 AND sys.stats.user_created      = 'True'
                  ) ExistingTableStats
               ON ExistingTableStats.object_id  = sys.columns.object_id
              AND ExistingTableStats.column_id  = sys.columns.column_id
 WHERE ExistingTableStats.column_id is null
   AND sys.tables.name like (@TableName) 
   AND schema_name(sys.tables.schema_id) = @SchemaName
 --AND sys.tables.name NOT IN ('TABLE-TO-EXCLUDE')
ORDER BY sys.tables.name,
        sys.columns.column_id;

END
GO
