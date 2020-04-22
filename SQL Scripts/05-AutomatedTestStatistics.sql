/****** Object:  StoredProcedure [telemetry].[AutomatedTestStatistics_ALL]    Script Date: 4/22/2020 10:01:18 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROC [telemetry].[AutomatedTestStatistics_ALL] AS 
BEGIN

SELECT [telemetry].[AutomatedTest].[AutomatedTestId],
       [telemetry].[AutomatedTest].[Mode],
       [telemetry].[AutomatedTest].[Interations],
       [telemetry].[AutomatedTest].[DWU],
       [telemetry].[AutomatedTest].[CacheState],
       [telemetry].[AutomatedTest].[ScriptMods],
       CONVERT(FLOAT,DATEDIFF(millisecond, MIN([telemetry].[AutomatedTest_exec_requests].submit_time),MAX([telemetry].[AutomatedTest_exec_requests].end_time))) / CONVERT(FLOAT,1000) AS Total_TestRun_TimeInSeconds,
       [telemetry].[AutomatedTest_exec_requests].[label] AS QueryName,
       [telemetry].[AutomatedTest_exec_requests].start_time AS QueryStartTime,
       [telemetry].[AutomatedTest_exec_requests].end_time AS QueryEndTime,
       [telemetry].[AutomatedTest_exec_requests].[resource_class] AS ResourceClass,
       [telemetry].[AutomatedTest_exec_requests].[result_cache_hit] AS ResultCacheHit,
       CONVERT(FLOAT,DATEDIFF(millisecond,  [telemetry].[AutomatedTest_exec_requests].start_time, [telemetry].[AutomatedTest_exec_requests].end_time)) / 
          CONVERT(FLOAT,1000) AS QueryExecutionTimeInSeconds,
       CONVERT(FLOAT,DATEDIFF(millisecond,  [telemetry].[AutomatedTest_exec_requests].submit_time,[telemetry].[AutomatedTest_exec_requests].end_compile_time)) / 
          CONVERT(FLOAT,1000)  AS QueryCompileTimeInSeconds,
       (CONVERT(FLOAT,DATEDIFF(millisecond, [telemetry].[AutomatedTest_exec_requests].submit_time,[telemetry].[AutomatedTest_exec_requests].start_time)) - 
          CONVERT(FLOAT,DATEDIFF(millisecond, submit_time,end_compile_time))) / CONVERT(FLOAT,1000) AS QueryQueueTimeInSeconds,
       CONVERT(FLOAT,DATEDIFF(millisecond,  [telemetry].[AutomatedTest_exec_requests].submit_time,[telemetry].[AutomatedTest_exec_requests].end_time)) / 
          CONVERT(FLOAT,1000) AS QueryElaspedTimeInSeconds
 FROM [telemetry].[AutomatedTest]
      INNER JOIN [telemetry].[AutomatedTestSession]
              ON [telemetry].[AutomatedTest].[AutomatedTestId] = [telemetry].[AutomatedTestSession].[AutomatedTestId]
      INNER JOIN [telemetry].[AutomatedTest_exec_requests]
              ON [telemetry].[AutomatedTest_exec_requests].session_id = [telemetry].[AutomatedTestSession].session_id
             AND [telemetry].[AutomatedTest_exec_requests].[label] IS NOT NULL
GROUP BY [telemetry].[AutomatedTest].[AutomatedTestId],
       [telemetry].[AutomatedTest].[Mode],
       [telemetry].[AutomatedTest].[Interations],
       [telemetry].[AutomatedTest].[DWU],
       [telemetry].[AutomatedTest].[CacheState],
       [telemetry].[AutomatedTest].[ScriptMods],
	   [telemetry].[AutomatedTest_exec_requests].[label],
       [telemetry].[AutomatedTest_exec_requests].start_time,
       [telemetry].[AutomatedTest_exec_requests].end_time,
	   [telemetry].[AutomatedTest_exec_requests].[resource_class],
	   [telemetry].[AutomatedTest_exec_requests].[result_cache_hit],
	   [telemetry].[AutomatedTest_exec_requests].submit_time,
	   [telemetry].[AutomatedTest_exec_requests].end_compile_time
ORDER BY [telemetry].[AutomatedTest].[AutomatedTestId], 
         [telemetry].[AutomatedTest_exec_requests].[label]

END
