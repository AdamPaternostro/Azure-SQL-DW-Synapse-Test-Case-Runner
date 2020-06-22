using System;
using System.Threading.Tasks;
using System.Data.SqlClient;
using System.Collections.Generic;

namespace SynapseConcurrency
{
    /// <summary>
    /// This program will execute a set of SQL Queries located in a folder
    /// The queries can be run in serial or concurrently
    /// The queries will be run in the order they reside on disk (if you have a specific order then name your files 01-first.sql, 02-second.sql, etc)
    /// Each query session id will be retrieved
    /// All the data will then be saved to the database.  The data from the sys.dm_pdw_exec_requests table will be captured.
    /// You can run the stored procedure [telemetry].[AutomatedTestStatistics] to view the latest run or for ideas around timing values.
    /// The program will stop if it hits an error.  It does not continue.  It does not save the telemetry for the current test case (prior test cases are saved).
    /// You can run this locally, but it would be best to create a VM in Azure in the same Region as your SQL Server and run from there (to reduce latency and avoid any internet blips)
    /// </summary>
    public class Program
    {
        // Prerequisites: You need to have run the SQL scripts in the SQL Scripts folder

        // Manual Step:
        // Turn on resultset caching (depending on your test case)
        // In the Master Database: ALTER DATABASE [REPLACE_ME_DATABASE_NAME] SET RESULT_SET_CACHING ON;
        // To turn off: ALTER DATABASE [REPLACE_ME_DATABASE_NAME] SET RESULT_SET_CACHING OFF;


        // **** CHANGE ME ****
        const string DATABASE_NAME = "ART-DEMO-01";
        const string SERVER_NAME = "artcentralsql";
        const string SQL_ADMIN_NAME = "SmallCaseDriver";
        // Assumes all your passwords for all users are the same (if not change the connection string below)
        const string PASSWORD = "<password>";
        // Which DWUs do you want to run this test at. You can just do one.
        // "DW100c", "DW200c", "DW300c", "DW400c", "DW500c", "DW1000c", "DW1500c", "DW2000c", "DW2500c", 
        // "DW3000c", "DW5000c", "DW6000c", "DW7500c", "DW10000c", "DW15000c", "DW30000c"
        //static List<string> DWUsToTestList = new List<string>() { "DW100c", "DW200c", "DW300c", "DW400c", "DW500c", "DW1000c" };
        static List<string> DWUsToTestList = new List<string>() { "DW400c" };




        // A Global connection string for which has access to the Master Database so we can query the DWUs
        const string SQL_CONNECTION_MASTER_DATABASE = @"Server=tcp:" + SERVER_NAME + ".database.windows.net,1433;Initial Catalog=master;Persist Security Info=False;" +
            "User ID=" + SQL_ADMIN_NAME + "; Password=" + PASSWORD + "; MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=20;";

        // A Global connection string for which has access to the User Database so we can run some SQL
        const string SQL_CONNECTION_USER_DATABASE = @"Server=tcp:" + SERVER_NAME + ".database.windows.net,1433;Initial Catalog=" + DATABASE_NAME + ";Persist Security Info=False;" +
            "User ID=" + SQL_ADMIN_NAME + "; Password=" + PASSWORD + "; MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=20;";

        // A connection string for each resource class you are testing
        const string SQL_CONNECTION_SMALL = @"Server=tcp:" + SERVER_NAME + ".database.windows.net,1433;Initial Catalog=" + DATABASE_NAME + "; Persist Security Info=False;" +
            "User ID=resource_class_small;Password=" + PASSWORD + ";MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=600;";

        const string SQL_CONNECTION_MEDIUM = @"Server=tcp:" + SERVER_NAME + ".database.windows.net,1433;Initial Catalog=" + DATABASE_NAME + ";Persist Security Info=False;" +
            "User ID=resource_class_medium;Password=" + PASSWORD + ";MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=600;";

        const string SQL_CONNECTION_LARGE = @"Server=tcp:" + SERVER_NAME + ".database.windows.net,1433;Initial Catalog=" + DATABASE_NAME + ";Persist Security Info=False;" +
            "User ID=resource_class_large;Password=" + PASSWORD + ";MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=600;";

        const string SQL_CONNECTION_XLARGE = @"Server=tcp:" + SERVER_NAME + ".database.windows.net,1433;Initial Catalog=" + DATABASE_NAME + ";Persist Security Info=False;" +
            "User ID=resource_class_xlarge;Password=" + PASSWORD + ";MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=600;";


        /// <summary>
        /// Runs the SQL Statements
        /// </summary>
        static async Task Main(string[] args)
        {

            //TelemetrySqlHelper.TestMe("Server=tcp:artcentralsql.database.windows.net,1433;Initial Catalog=ART-DEMO-01;Persist Security Info=False;User ID=SmallCaseDriver;Password=<password>;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;");
            //return;
            //This constant is for temporary use
            const int TMP_WORKLOAD_ID = 2; //Desginates a set of test cases in the telemetry tables for which there exists reporting metadata. Would need to come from somewhere above.
            const string TMP_CONN_STRING = "Server=tcp:artcentralsql.database.windows.net,1433;Initial Catalog=ART-DEMO-01;Persist Security Info=False;User ID=SmallCaseDriver;Password=<password>;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;";
            string DWUs = "DW400c";

            try
            {
                //DateTime minStatisticDate = GetMinStatisticDate();
                string optomizationLevel = "(none)";
                foreach (string dwuScale in DWUsToTestList)
                {
                    /*                

                        // Get the number of DWUs SQL DW is running
                        string DWUs = GetDWUs();

                        if (DWUs != dwuScale)
                        {
                            // Is the database at the corect DWUs?  If not then let's scale it
                            ScaleDatabase(DWUs, dwuScale);
                            DWUs = GetDWUs();
                        }


                        // Replicate all the tables marked for replication and wait until they are all replicated.  
                        // If you need to alter the stored procedures to adjust which tables to test (e.g. WHERE table_name IS NOT IN ('skip-me-table')
                        ReplicateTables();
                    */
                    // Configuration Runs (these will all run in a loop)
                    List<ExecutionRun> executionRuns = new List<ExecutionRun>();
                    executionRuns.Clear();
                    

                    // **** CHANGE ME ****  
                    // Create your execution runs
                    #region Execution Runs
                    /*
                    //////////////////////////////////////////////////////////////////
                    // Serial
                    //////////////////////////////////////////////////////////////////
                    // Run Serial V1 on small, medium and large resource classes
                    executionRuns.Add(new ExecutionRun()
                    {
                        ReplicatedTables = true,
                        ConnectionString = SQL_CONNECTION_SMALL,
                        DWU = DWUs,
                        Enabled = true,
                        Interations = 5,
                        Mode = SerialOrConcurrentEnum.Serial,
                        OptLevel = optomizationLevel,
                        ResourceClass = "smallrc",
                        ResultSetCaching = false,
                        ScriptPath = @"..\..\..\..\Sample-Serial-SQL-v1"
                    }); 

                    executionRuns.Add(new ExecutionRun()
                    {
                        ReplicatedTables = true,
                        ConnectionString = SQL_CONNECTION_MEDIUM,
                        DWU = DWUs,
                        Enabled = true,
                        Interations = 5,
                        Mode = SerialOrConcurrentEnum.Serial,
                        OptLevel = optomizationLevel,
                        ResourceClass = "mediumrc",
                        ResultSetCaching = false,
                        ScriptPath = @"..\..\..\..\Sample-Serial-SQL-v1"
                    }); 

                    executionRuns.Add(new ExecutionRun()
                    {
                        ReplicatedTables = true,
                        ConnectionString = SQL_CONNECTION_LARGE,
                        DWU = DWUs,
                        Enabled = true,
                        Interations = 5,
                        Mode = SerialOrConcurrentEnum.Serial,
                        OptLevel = optomizationLevel,
                        ResourceClass = "largerc",
                        ResultSetCaching = false,
                        ScriptPath = @"..\..\..\..\Sample-Serial-SQL-v1"
                    }); 


                    // Run Serial V2 on medium and large resource classes
                    executionRuns.Add(new ExecutionRun()
                    {
                        ReplicatedTables = true,
                        ConnectionString = SQL_CONNECTION_SMALL,
                        DWU = DWUs,
                        Enabled = true,
                        Interations = 5,
                        Mode = SerialOrConcurrentEnum.Serial,
                        OptLevel = optomizationLevel,
                        ResourceClass = "smallrc",
                        ResultSetCaching = false,
                        ScriptPath = @"..\..\..\..\Sample-Serial-SQL-v2"
                    }); 

                    executionRuns.Add(new ExecutionRun()
                    {
                        ReplicatedTables = true,
                        ConnectionString = SQL_CONNECTION_MEDIUM,
                        DWU = DWUs,
                        Enabled = true,
                        Interations = 5,
                        Mode = SerialOrConcurrentEnum.Serial,
                        OptLevel = optomizationLevel,
                        ResourceClass = "mediumrc",
                        ResultSetCaching = false,
                        ScriptPath = @"..\..\..\..\Sample-Serial-SQL-v2"
                    }); 

                    executionRuns.Add(new ExecutionRun()
                    {
                        ReplicatedTables = true,
                        ConnectionString = SQL_CONNECTION_LARGE,
                        DWU = DWUs,
                        Enabled = true,
                        Interations = 5,
                        Mode = SerialOrConcurrentEnum.Serial,
                        OptLevel = optomizationLevel,
                        ResourceClass = "largerc",
                        ResultSetCaching = false,
                        ScriptPath = @"..\..\..\..\Sample-Serial-SQL-v2"
                    }); 


                    //////////////////////////////////////////////////////////////////
                    // Concurrency
                    //////////////////////////////////////////////////////////////////

                    // Run Concurrency V1 on medium and large resource classes
                    executionRuns.Add(new ExecutionRun()
                    {
                        ReplicatedTables = true,
                        ConnectionString = SQL_CONNECTION_MEDIUM,
                        DWU = DWUs,
                        Enabled = true,
                        Interations = 2,
                        Mode = SerialOrConcurrentEnum.Concurrent,
                        OptLevel = optomizationLevel,
                        ResourceClass = "mediumrc",
                        ScriptPath = @"..\..\..\..\Sample-Concurrency-SQL-v1",
                        ResultSetCaching = false,
                    }); 

                    executionRuns.Add(new ExecutionRun()
                    {
                        ReplicatedTables = true,
                        ConnectionString = SQL_CONNECTION_LARGE,
                        DWU = DWUs,
                        Enabled = true,
                        Interations = 2,
                        Mode = SerialOrConcurrentEnum.Concurrent,
                        OptLevel = optomizationLevel,
                        ResourceClass = "largerc",
                        ResultSetCaching = false,
                        ScriptPath = @"..\..\..\..\Sample-Concurrency-SQL-v1"
                    }); 


                    // Run Concurrency V2 on medium and large resource classes
                    executionRuns.Add(new ExecutionRun()
                    {
                        ReplicatedTables = true,
                        ConnectionString = SQL_CONNECTION_MEDIUM,
                        DWU = DWUs,
                        Enabled = true,
                        Interations = 2,
                        Mode = SerialOrConcurrentEnum.Concurrent,
                        OptLevel = optomizationLevel,
                        ResourceClass = "mediumrc",
                        ResultSetCaching = false,
                        ScriptPath = @"..\..\..\..\Sample-Concurrency-SQL-v2"
                    }); 

                    executionRuns.Add(new ExecutionRun()
                    {
                        ReplicatedTables = true,
                        ConnectionString = SQL_CONNECTION_LARGE,
                        DWU = DWUs,
                        Enabled = true,
                        Interations = 2,
                        Mode = SerialOrConcurrentEnum.Concurrent,
                        OptLevel = optomizationLevel,
                        ResourceClass = "largerc",
                        ResultSetCaching = false,
                        ScriptPath = @"..\..\..\..\Sample-Concurrency-SQL-v2"
                    });

                    //////////////////////////////////////////////////////////////////
                    // Concurrency WITH ResultSetCaching
                    //////////////////////////////////////////////////////////////////
                    executionRuns.Add(new ExecutionRun()
                    {
                        ReplicatedTables = true,
                        ConnectionString = SQL_CONNECTION_SMALL,
                        DWU = DWUs,
                        Enabled = true,
                        Interations = 4,
                        Mode = SerialOrConcurrentEnum.Concurrent,
                        OptLevel = optomizationLevel,
                        ResourceClass = "smallrc",
                        ResultSetCaching = true,
                        ScriptPath = @"..\..\..\..\Sample-Concurrency-SQL-v1"
                    });
                    */

                    #endregion // Execution Runs



                    executionRuns.Add(new ExecutionRun()
                    {
                        ReplicatedTables = true,
                        ConnectionString = TMP_CONN_STRING,
                        DWU = DWUs,
                        Enabled = true,
                        Interations = 1,
                        Mode = SerialOrConcurrentEnum.Concurrent,
                        OptLevel = optomizationLevel,
                        ResourceClass = "smallrc",
                        ResultSetCaching = false,
                        ScriptPath = @"C:\faketestcases",
                        StepsFrom = TestPassSource.ReadFromTelemetryDb
                    });


                    // Read all the SQL Statements and do any necessary string replacements on the SQL
                    foreach (ExecutionRun executionRun in executionRuns)
                    {
                        Console.WriteLine("Starting Execution Run: " + executionRun.ScriptPath);

                        if (executionRun.Enabled == false)
                        {
                            Console.WriteLine("Skipping Execution Run: " + executionRun.ScriptPath);
                            continue;
                        }

                        //bool resultSetCachingStatus = GetResultSetCachingStatus();
                        //if (resultSetCachingStatus != executionRun.ResultSetCaching)
                        //{
                        //    SetResultSetCachingStatus(executionRun.ResultSetCaching);
                        //}
                        
                        // local variables
                        List<string> sessionIds = new List<string>();
                        sessionIds.Clear();
                        List<SQLTask> sqlScripts = new List<SQLTask>();
                        sqlScripts.Clear();
                        string rawSQL = null;
                        string fileName = "";
                        int counter = 1;
                        DateTime startDate = DateTime.UtcNow;
                        DateTime endDate = DateTime.UtcNow;
                        startDate = DateTime.UtcNow;

                        // Prepare the scripts for execution
                        // We want to do a search and replace on certain items since we might need to change table names or resource classes.
                        for (int i = 1; i <= executionRun.Interations; i++)
                        {
                            if (executionRun.StepsFrom == TestPassSource.ReadFromFolder)
                            {
                                foreach (var item in System.IO.Directory.GetFiles(executionRun.ScriptPath))
                                {
                                    SQLTask sqlTask = new SQLTask();

                                    rawSQL = System.IO.File.ReadAllText(item);

                                    fileName = item.Substring(item.LastIndexOf("\\") + 1).Replace(".sql", string.Empty);

                                    // **** CHANGE ME (OVERRIDES PER CUSTOMER) ****
                                    // OVERRIDES: To handle multiple CTASs (append the loop variable)
                                    // What this will do is find "my_ctas_table" and replace it with "my_ctas_table_1" so if we have 2 loops we will get a _1 and _2 table so we do not have collections
                                    rawSQL = rawSQL.Replace("my_ctas_table", "my_ctas_table_" + i.ToString());

                                    sqlTask.ConnectionString = executionRun.ConnectionString;
                                    sqlTask.ScriptName = fileName;
                                    sqlTask.Label = fileName + " :: " + i.ToString() + " :: " + counter.ToString();
                                    sqlTask.SQL = rawSQL;

                                    // **** CHANGE ME (OVERRIDES PER CUSTOMER) ****
                                    // OVERRIDES: Change certain scripts to run in different classes
                                    if (sqlTask.ScriptName.ToUpper() == "????")
                                    {
                                        // Do whatever you want in here
                                        sqlTask.ConnectionString = SQL_CONNECTION_LARGE;
                                    }

                                    sqlScripts.Add(sqlTask);
                                    counter++;
                                } // foreach
                            }
                            else
                            {
                                sqlScripts.AddRange(TelemetrySqlHelper.GetTestCases(TMP_CONN_STRING, TMP_WORKLOAD_ID, executionRun.ScriptPath));
                            }
                        } // i


                        int testPassId = TelemetrySqlHelper.LogTestPassStart(TMP_CONN_STRING, TMP_WORKLOAD_ID, executionRun.DWU, "Unknown", executionRun.OptLevel, "Unknown", executionRun.ResourceClass, executionRun.Mode);

                        if (executionRun.Mode == SerialOrConcurrentEnum.Concurrent)
                        {
                            // CONCURRENT 
                            List<Task<string>> executeSQLTask = new List<Task<string>>();
                            executeSQLTask.Clear();

                            startDate = DateTime.UtcNow;
                            foreach (SQLTask sqlTask in sqlScripts)
                            {
                                executeSQLTask.Add(Task<string>.Run(() => ExecuteSQL(sqlTask.Label, sqlTask.SQL, sqlTask.ConnectionString, sqlTask.TestCaseNum)));
                            }

                            // Wait for everything to finish
                            await Task.WhenAll(executeSQLTask);
                            endDate = DateTime.UtcNow;

                            // Copy SessionIds
                            foreach (var item in executeSQLTask)
                            {
                                sessionIds.Add(item.Result);
                                if (item.Result != null)
                                {
                                    Console.WriteLine("DONE: " + (item.Result ?? "NULL"));
                                }
                                else
                                {
                                    Console.WriteLine("*********** ERROR ABORT TEST: SESSION ID IS NULL ***********");
                                    return;
                                }
                            }
                        }
                        else
                        {
                            // SERIAL
                            startDate = DateTime.UtcNow;
                            foreach (SQLTask sqlTask in sqlScripts)
                            {
                                Task<string> serialSQL = Task<string>.Run(() => ExecuteSQL(sqlTask.Label, sqlTask.SQL, sqlTask.ConnectionString, sqlTask.TestCaseNum));
                                serialSQL.Wait();
                                sessionIds.Add(serialSQL.Result);
                                if (serialSQL.Result != null)
                                {
                                    Console.WriteLine("DONE: " + (serialSQL.Result ?? "NULL"));
                                }
                                else
                                {
                                    Console.WriteLine("***********ERROR ABORT TEST: SESSION ID IS NULL -- SQL: " + sqlTask.Label + " ***********");
                                    return;
                                }
                            }
                            endDate = DateTime.UtcNow;
                        } // serialOrConcurrentMode == SerialOrConcurrentEnum.Concurrent

                        TelemetrySqlHelper.LogTestPassEnd(TMP_CONN_STRING);

                        /*
                        // Save the data to the Automated Test Tables
                        try
                        {
                            using (SqlConnection connection = new SqlConnection(SQL_CONNECTION_SMALL))
                            {
                                connection.Open();

                                using (SqlCommand command = new SqlCommand("SELECT 1 + ISNULL(MAX(AutomatedTestId), 0) FROM telemetry.AutomatedTest", connection))
                                {
                                    string AutomatedTestId = command.ExecuteScalar().ToString();

                                    command.CommandText = "INSERT telemetry.AutomatedTest(" +
                                                                "[AutomatedTestId]," +
                                                                "[ScriptMods]," +
                                                                "[Mode]," +
                                                                "[DWU]," +
                                                                "[ReplicatedTables]," +
                                                                "[Interations]," +
                                                                "[MinStatisticDate]," +
                                                                "[ResultSetCaching]," +
                                                                "[ResourceClass]," +
                                                                "[OptLevel]," +
                                                                "[StartTime]," +
                                                                "[EndTime]) VALUES (" +
                                                                AutomatedTestId.ToString() + "," +
                                                                "'" + executionRun.ScriptPath.Substring(executionRun.ScriptPath.LastIndexOf("\\") + 1) + "'," +
                                                                "'" + executionRun.Mode.ToString() + "'," +
                                                                "'" + DWUs + "', " +
                                                                (executionRun.ReplicatedTables ? "1" : "0") + "," +
                                                                executionRun.Interations.ToString() + "," +
                                                                "'" + minStatisticDate.ToString("yyyy-MM-dd HH:mm:ss.fff") + "'," +
                                                                (executionRun.ResultSetCaching ? "1" : "0") + "," +
                                                                "'" + executionRun.ResourceClass + "'," +
                                                                "'" + executionRun.OptLevel + "'," +
                                                                "'" + startDate.ToString("yyyy-MM-dd HH:mm:ss.fff") + "'," +
                                                                "'" + endDate.ToString("yyyy-MM-dd HH:mm:ss.fff") + "');";

                                    command.ExecuteNonQuery();

                                    foreach (var sessionId in sessionIds)
                                    {
                                        command.CommandText = "INSERT INTO telemetry.AutomatedTestSession (AutomatedTestId,session_id) " +
                                                              "VALUES (" + AutomatedTestId.ToString() + ",'" + sessionId + " ');";
                                        command.ExecuteNonQuery();
                                        Console.WriteLine("Saving Data: " + sessionId);
                                    } // sessionId

                                    // Capture the exec_requests since we can loose them when the server is paused
                                    command.CommandText = "INSERT INTO [telemetry].[AutomatedTest_exec_requests] " +
                                                            "([request_id], [session_id], [status], [submit_time], [start_time], [end_compile_time], [end_time], [total_elapsed_time], " +
                                                            "[label], [error_id], [database_id], [command], [resource_class], [importance], [group_name], [classifier_name], " +
                                                            "[resource_allocation_percentage], [result_cache_hit]) " +
                                                          "SELECT [request_id], [session_id], [status], [submit_time], [start_time], [end_compile_time], [end_time], [total_elapsed_time],  " +
                                                                 "[label], [error_id], [database_id], [command], [resource_class], [importance], [group_name], [classifier_name],  " +
                                                                 "[resource_allocation_percentage], [result_cache_hit] " +
                                                           "FROM sys.dm_pdw_exec_requests " +
                                                          "WHERE session_id IN     (SELECT session_id FROM [telemetry].[AutomatedTestSession]) " +
                                                            "AND session_id NOT IN (SELECT session_id FROM [telemetry].[AutomatedTest_exec_requests]);";

                                    command.ExecuteNonQuery();
                                    Console.WriteLine("Captured sys.dm_pdw_exec_requests");
                                } // SqlCommand command
                            }
                        }
                        
                        catch (SqlException e)
                        {
                            Console.WriteLine("*********** ERROR: " + e.ToString());
                            Console.ReadKey();
                        }
                        */

                        Console.WriteLine("Completed Execution Run: " + executionRun.ScriptPath);

                    } // foreach Execution run

                } // (string dwuScale in DWUsToTestList)
            }
            catch (Exception mainException)
            {
                Console.WriteLine("*********** ERROR: " + mainException.ToString());
                Console.ReadKey();
            }

            Console.WriteLine("Program Complete!");
        } // Main



        /// <summary>
        /// Runs a dynamic SQL Statement and returned the Synapse session id for tracking the timings.
        /// </summary>
        private static string ExecuteSQL(string label, string sql, string connectionString, int testCaseNum)
        {
            TelemetrySqlHelper telemetryHelper = new TelemetrySqlHelper();
            string testRunId = null;

            Console.WriteLine("BEGIN: " + label);
            string sessionId = null;
            SqlConnection connection = new SqlConnection(connectionString);
            
            try
            {
                //using (SqlConnection connection = new SqlConnection(connectionString))
                //{
                    connection.Open();
                    testRunId = telemetryHelper.LogTestRunStart(connection, testCaseNum);

                    using (SqlCommand command = new SqlCommand(sql, connection))
                    {

                        command.CommandTimeout = 0;
                        command.ExecuteNonQuery();
                        //command.CommandText = "SELECT session_id()";
                        //sessionId = command.ExecuteScalar().ToString();

                        sessionId = telemetryHelper.LogTestRunEnd(connection, testRunId, null);
                    }
                //}
            }
            catch (SqlException e)
            {
                sessionId = telemetryHelper.LogTestRunEnd(connection, testRunId, e);

                Console.WriteLine("*********** ERROR: " + e.ToString());
                Console.ReadKey();  // <-- is this a good idea during concurrent tests? -ATR
            }
            finally
            {
                connection.Close();
                connection.Dispose();
                Console.WriteLine("END:   " + label + " -> SESSION: " + label + " - " + (sessionId ?? "NULL"));
            }

            return sessionId;
        } // Execute SQL


        public static string GetDWUs()
        {
            string DWUs = null;
            string sql = "SELECT sys.database_service_objectives.service_objective " +
                           "FROM sys.database_service_objectives " +
                                "INNER JOIN sys.databases " +
                                        "ON sys.database_service_objectives.database_id = sys.databases.database_id " +
                                       "AND sys.databases.name = '" + DATABASE_NAME + "'";

            using (SqlConnection connection = new SqlConnection(SQL_CONNECTION_MASTER_DATABASE))
            {
                connection.Open();

                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    command.CommandTimeout = 0;
                    DWUs = command.ExecuteScalar().ToString();
                }
            }


            Console.WriteLine("**** Database is running at " + DWUs.ToString() + " DWUs ***");
            return DWUs;
        } // GetDWUs


        /// <summary>
        /// Gets the min statistics rebuild date.  This does not mean all your tables are out of date, but if you have a old date or null 
        /// you should create or rebuild your statistics.
        /// </summary>
        /// <returns></returns>
        public static DateTime GetMinStatisticDate()
        {
            DateTime minStatisticDate = DateTime.Parse("01/01/1900");
            string sql = "SELECT MIN(STATS_DATE(st.[object_id],st.[stats_id])) AS MinStatisticDate " +
                           "FROM sys.objects ob " +
                                "INNER JOIN sys.stats st " +
                                        "ON ob.[object_id] = st.[object_id] " +
                                "INNER JOIN sys.stats_columns sc " +
                                        "ON  st.[stats_id] = sc.[stats_id] " +
                                       "AND st.[object_id] = sc.[object_id] " +
                                "INNER JOIN sys.columns co " +
                                        "ON sc.[column_id] = co.[column_id] " +
                                       "AND sc.[object_id] = co.[object_id] " +
                                "INNER JOIN sys.types ty " +
                                        "ON co.[user_type_id] = ty.[user_type_id] " +
                                "INNER JOIN sys.tables tb " +
                                        "ON co.[object_id] = tb.[object_id] " +
                                "INNER JOIN sys.schemas sm " +
                                        "ON tb.[schema_id] = sm.[schema_id] " +
                         "WHERE st.[user_created] = 1;";

            using (SqlConnection connection = new SqlConnection(SQL_CONNECTION_USER_DATABASE))
            {
                connection.Open();

                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    command.CommandTimeout = 0;
                    minStatisticDate = DateTime.Parse(command.ExecuteScalar().ToString());
                }
            }


            Console.WriteLine($"**** Database MinStatisticDate = {minStatisticDate} ****");
            return minStatisticDate;
        } // GetMinStatisticDate


        /// <summary>
        /// Returns if resultset caching is on
        /// </summary>
        /// <returns></returns>
        public static bool GetResultSetCachingStatus()
        {
            string isResultSetCachingOn = null;
            string sql = "SELECT is_result_set_caching_on " +
                           "FROM sys.databases " +
                          $"WHERE name = '{DATABASE_NAME}'; ";

            using (SqlConnection connection = new SqlConnection(SQL_CONNECTION_MASTER_DATABASE))
            {
                connection.Open();

                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    command.CommandTimeout = 0;
                    isResultSetCachingOn = command.ExecuteScalar().ToString();
                }
            }


            Console.WriteLine($"**** Database isResultSetCachingOn = {isResultSetCachingOn} ****");
            if (isResultSetCachingOn.ToLower() == "true" || isResultSetCachingOn == "1")
            {
                return true;
            }
            else
            {
                return false;
            }
        } // GetResultSetCachingStatus


        /// <summary>
        /// Returns if resultset caching is on
        /// </summary>
        /// <returns></returns>
        public static void SetResultSetCachingStatus(bool setOn)
        {
            string onOrOff = (setOn == true ? "ON" : "OFF");
            string sql = $"ALTER DATABASE {DATABASE_NAME} SET RESULT_SET_CACHING {onOrOff}";

            Console.WriteLine($"**** Setting RESULT_SET_CACHING {onOrOff} ***");

            using (SqlConnection connection = new SqlConnection(SQL_CONNECTION_MASTER_DATABASE))
            {
                connection.Open();

                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    command.CommandTimeout = 0;
                    command.ExecuteNonQuery(); 
                }
            }
        } // SetResultSetCachingStatus


        public static void ReplicateTables()
        {
            string sqlReplicate = "[dbo].[ReplicateTables]";
            string sqlReplicateTest = "[dbo].[ReplicateTablesStatusPendingCount]";
            int replicateTestCount = 0;

            try
            {
                using (SqlConnection connection = new SqlConnection(SQL_CONNECTION_USER_DATABASE))
                {
                    connection.Open();

                    using (SqlCommand command = new SqlCommand(sqlReplicate, connection))
                    {
                        Console.WriteLine("**** Replicating Tables ***");
                        command.CommandTimeout = 0;
                        command.CommandType = System.Data.CommandType.StoredProcedure;
                        command.ExecuteNonQuery();

                        command.CommandText = sqlReplicateTest;

                        while (true)
                        {
                            Console.WriteLine("**** Testing Replicated Tables Status ***");
                            replicateTestCount = int.Parse(command.ExecuteScalar().ToString());
                            if (replicateTestCount == 0)
                            {
                                Console.WriteLine("**** All Tables Replicated ***");
                                break;
                            }
                            else
                            {
                                Console.WriteLine("**** Tables not Replicated.  Waiting on " + replicateTestCount.ToString() + " table(s) ***");
                                System.Threading.Thread.Sleep(10 * 1000); // wait 10 seconds
                            }
                        }

                    }
                }
            }
            catch (SqlException e)
            {
                Console.WriteLine("*********** ERROR: " + e.ToString());
                Console.ReadKey();
            }
            finally
            {
            }
        } // ReplicateTables


        static DateTime lastScaleDateTime = DateTime.MinValue;

        public static void ScaleDatabase(string originalDWU, string newDWU)
        {
            string sqlScale = $"ALTER DATABASE {DATABASE_NAME} MODIFY(SERVICE_OBJECTIVE = '{newDWU}');";
            string sqlScaleTest = $"SELECT TOP 1 state_desc " +
                                     "FROM sys.dm_operation_status " +
                                    "WHERE resource_type_desc = 'Database' " +
                                     $"AND major_resource_id = '{DATABASE_NAME}' " +
                                      "AND operation = 'ALTER DATABASE' " +
                                 "ORDER BY start_time DESC";

            try
            {
                bool scaledDatabase = false;
                string scaleResult = null;

                // Try 5 times to scale the database
                // Sometimes we scale so fast it can fail since a scale is in progress
                for (int i = 1; i <= 5; i++)
                {
                    // Issue the Scale command
                    using (SqlConnection connection = new SqlConnection(SQL_CONNECTION_MASTER_DATABASE))
                    {
                        connection.Open();

                        using (SqlCommand command = new SqlCommand(sqlScale, connection))
                        {
                            Console.WriteLine($"**** Scaling Database to {newDWU} DWUs ***");
                            command.CommandTimeout = 0;
                            command.ExecuteNonQuery();
                        }
                    }

                    // We need to give Azure some time to start the scaling process
                    System.Threading.Thread.Sleep(1 * 60 * 1000); // wait 1 minute

                    while (true)
                    {
                        using (SqlConnection connection = new SqlConnection(SQL_CONNECTION_MASTER_DATABASE))
                        {
                            connection.Open();

                            using (SqlCommand command = new SqlCommand(sqlScaleTest, connection))
                            {
                                command.CommandTimeout = 0;
                                scaleResult = command.ExecuteScalar().ToString();
                                if (scaleResult == "IN_PROGRESS")
                                {
                                    Console.WriteLine($"**** Database Scaling is In Progress... (waiting 30 seconds and testing again) ***");
                                    System.Threading.Thread.Sleep(30 * 1000); // wait 30 seconds
                                }
                                else if (scaleResult == "FAILED")
                                {
                                    Console.WriteLine($"**** Database Scaling FAILED ***");
                                    Console.WriteLine($"**** The database was recently scaled.  Sleeping for 5 minutes ***");
                                    System.Threading.Thread.Sleep(5 * 60 * 1000); // wait 5 minutes
                                    break;
                                }
                                else if (scaleResult == "COMPLETED")
                                {
                                    Console.WriteLine($"**** Database Scaled Successfully from {originalDWU} to {newDWU} * **");
                                    scaledDatabase = true;
                                    break;
                                }
                                else
                                {
                                    Console.WriteLine($"**** Unknown Scale Status (update code for: {scaleResult}... (waiting 30 seconds and testing again) ***");
                                    System.Threading.Thread.Sleep(30 * 1000); // wait 30 seconds
                                }
                            }
                        }
                    }

                    if (scaledDatabase == true)
                    {
                        break;
                    }
                } // (int i =1; i <= 5; i++)

                if (scaledDatabase == false)
                {
                    Console.WriteLine($"**** FAILED to scale the database {originalDWU} to {newDWU} ***");
                    throw new Exception($"**** FAILED to scale the database {originalDWU} to {newDWU} ***");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("*********** ERROR: " + e.ToString());
                Console.ReadKey();
            }
            finally
            {
            }
        } // ScaleDatabase


        public struct ExecutionRun
        {
            // Paths to the scripts (if you want the scripts to run in a certain order then name them 01-script1.sql, 02-script2.sql, etc...)
            public string ScriptPath { get; set; }

            // Paths to the scripts (if you want the scripts to run in a certain order then name them 01-script1.sql, 02-script2.sql, etc...)
            public TestPassSource StepsFrom { get; set; }

            // How to run the scripts
            public SerialOrConcurrentEnum Mode { get; set; }

            // Constant from server
            public string DWU { get; set; }

            // Did we warm anything up or turn on result set caching
            public bool ReplicatedTables { get; set; }

            // How many times to run the scripts (in serial mode this is looped one by one and in concurrency mode, this is ALL scripts at once)
            public int Interations { get; set; }

            // What type of optomizations to the scripts or servers (materizied views or such) - this is user defined
            public bool ResultSetCaching { get; set; }

            // What resource class are we running the database connection
            public string ResourceClass { get; set; }

            // What type of optomizations to the scripts or servers (materizied views or such) - this is user defined
            public string OptLevel { get; set; }


            // The Synapse connection string
            public string ConnectionString { get; set; }

            // Ship the execution
            public bool Enabled { get; set; }
        } // ExecutionRun


    } // class

    // Do you want to run in parallel or serial
    public enum SerialOrConcurrentEnum { Serial, Concurrent }

    // Do you want to run in parallel or serial
    public enum TestPassSource { ReadFromFolder, ReadFromTelemetryDb }

    public struct SQLTask
    {
        public string ScriptName { get; set; }
        public string ConnectionString { get; set; }
        public string Label { get; set; }
        public string SQL { get; set; }
        public int TestCaseNum { get; set; }
    } // SQLTask

} //namespace
