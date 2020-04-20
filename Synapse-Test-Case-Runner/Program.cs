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
    /// The program will stop if it hits an error.  It does not continue.  
    /// You can run this locally, but it would be best to create a VM in Azure in the same Region as your SQL Server and run from there (to reduce latency and avoid any internet blips)
    /// </summary>
    public class Program
    {
        // Prerequisites: 
        // Create a scheam called "telemetry". 
        // Run the SQL scripts in the SQL Scripts folder in your database.  
        // You can build a PowerBI dashboard off these tables.

        // Before you run a test:
        // Scale the database if necessary.
        // Run: The stored procedure [ReplicateTables]
        // Run: The stored procedure [ReplicateTablesStatus] and make sure your replicated tables are in a "Ready" state.

        // Turn on resultset caching (depending on your test case)
        // In the Master Database: ALTER DATABASE [REPLACE_ME_DATABASE_NAME] SET RESULT_SET_CACHING ON;
        // To turn off: ALTER DATABASE [REPLACE_ME_DATABASE_NAME] SET RESULT_SET_CACHING OFF;

        // Turn on query store so we capture the actual execution plan
        // ALTER DATABASE [REPLACE_ME_DATABASE_NAME] SET QUERY_STORE ON;

        // A connection string for each resource class you are testing
        const string SQL_CONNECTION_SMALL = @"Server=tcp:REPLACE_ME_SERVER_NAME.database.windows.net,1433;Initial Catalog=REPLACE_ME_DATABASE_NAME;Persist Security Info=False;User ID=resource_class_small;Password=REPLACE_ME;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=600;";

        const string SQL_CONNECTION_MEDIUM = @"Server=tcp:REPLACE_ME_SERVER_NAME.database.windows.net,1433;Initial Catalog=REPLACE_ME_DATABASE_NAME;Persist Security Info=False;User ID=resource_class_medium;Password=REPLACE_ME;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=600;";

        const string SQL_CONNECTION_LARGE = @"Server=tcp:REPLACE_ME_SERVER_NAME.database.windows.net,1433;Initial Catalog=REPLACE_ME_DATABASE_NAME;Persist Security Info=False;User ID=resource_class_large;Password=REPLACE_ME;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=600;";

        const string SQL_CONNECTION_XLARGE = @"Server=tcp:REPLACE_ME_SERVER_NAME.database.windows.net,1433;Initial Catalog=REPLACE_ME_DATABASE_NAME;Persist Security Info=False;User ID=resource_class_xlarge;Password=REPLACE_ME;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=600;";


        // **** CHANGE ME ***  What service level are we at?
        const string DWUs = "10000";


        /// <summary>
        /// Runs the SQL Statements
        /// </summary>
        static async Task Main(string[] args)
        {
            // Configuration Runs (these will all run in a loop)
            List<ExecutionRun> executionRuns = new List<ExecutionRun>();

            // **** CHANGE ME ***  
            // Create your execution runs
            #region Execution Runs

            //////////////////////////////////////////////////////////////////
            // Serial
            //////////////////////////////////////////////////////////////////
            // Run Serial V1 on medium and large resource classes
            executionRuns.Add(new ExecutionRun()
            {
                CacheState = "Replicated Tables",
                ConnectionString = SQL_CONNECTION_MEDIUM,
                DWU = DWUs,
                Enabled = true,
                Interations = 5,
                Mode = SerialOrConcurrentEnum.Serial,
                OptLevel = "Standard",
                ResourceClass = "mediumrc",
                ScriptPath = @"C:\Azure-SQL-DW-Synapse-Test-Case-Runner\Sample-Serial-SQL-v1"
            }); ;

            executionRuns.Add(new ExecutionRun()
            {
                CacheState = "Replicated Tables",
                ConnectionString = SQL_CONNECTION_LARGE,
                DWU = DWUs,
                Enabled = true,
                Interations = 5,
                Mode = SerialOrConcurrentEnum.Serial,
                OptLevel = "Standard",
                ResourceClass = "largerc",
                ScriptPath = @"C:\Azure-SQL-DW-Synapse-Test-Case-Runner\Sample-Serial-SQL-v1"
            }); ;


            // Run Serial V2 on medium and large resource classes
            executionRuns.Add(new ExecutionRun()
            {
                CacheState = "Replicated Tables",
                ConnectionString = SQL_CONNECTION_MEDIUM,
                DWU = DWUs,
                Enabled = true,
                Interations = 5,
                Mode = SerialOrConcurrentEnum.Serial,
                OptLevel = "Standard",
                ResourceClass = "mediumrc",
                ScriptPath = @"C:\Azure-SQL-DW-Synapse-Test-Case-Runner\Sample-Serial-SQL-v2"
            }); ;

            executionRuns.Add(new ExecutionRun()
            {
                CacheState = "Replicated Tables",
                ConnectionString = SQL_CONNECTION_LARGE,
                DWU = DWUs,
                Enabled = true,
                Interations = 5,
                Mode = SerialOrConcurrentEnum.Serial,
                OptLevel = "Standard",
                ResourceClass = "largerc",
                ScriptPath = @"C:\Azure-SQL-DW-Synapse-Test-Case-Runner\Sample-Serial-SQL-v2"
            }); ;


            //////////////////////////////////////////////////////////////////
            // Concurrency
            //////////////////////////////////////////////////////////////////

            // Run Concurrency V1 on medium and large resource classes
            executionRuns.Add(new ExecutionRun()
            {
                CacheState = "Replicated Tables",
                ConnectionString = SQL_CONNECTION_MEDIUM,
                DWU = DWUs,
                Enabled = true,
                Interations = 2,
                Mode = SerialOrConcurrentEnum.Concurrent,
                OptLevel = "Standard",
                ResourceClass = "mediumrc",
                ScriptPath = @"C:\Azure-SQL-DW-Synapse-Test-Case-Runner\Sample-Concurrency-SQL-v1"
            }); ;

            executionRuns.Add(new ExecutionRun()
            {
                CacheState = "Replicated Tables",
                ConnectionString = SQL_CONNECTION_LARGE,
                DWU = DWUs,
                Enabled = true,
                Interations = 2,
                Mode = SerialOrConcurrentEnum.Concurrent,
                OptLevel = "Standard",
                ResourceClass = "largerc",
                ScriptPath = @"C:\Azure-SQL-DW-Synapse-Test-Case-Runner\Sample-Concurrency-SQL-v1"
            }); ;


            // Run Concurrency V2 on medium and large resource classes
            executionRuns.Add(new ExecutionRun()
            {
                CacheState = "Replicated Tables",
                ConnectionString = SQL_CONNECTION_MEDIUM,
                DWU = DWUs,
                Enabled = true,
                Interations = 2,
                Mode = SerialOrConcurrentEnum.Concurrent,
                OptLevel = "Standard",
                ResourceClass = "mediumrc",
                ScriptPath = @"C:\Azure-SQL-DW-Synapse-Test-Case-Runner\Sample-Concurrency-SQL-v2"
            }); ;

            executionRuns.Add(new ExecutionRun()
            {
                CacheState = "Replicated Tables",
                ConnectionString = SQL_CONNECTION_LARGE,
                DWU = DWUs,
                Enabled = true,
                Interations = 2,
                Mode = SerialOrConcurrentEnum.Concurrent,
                OptLevel = "Standard",
                ResourceClass = "largerc",
                ScriptPath = @"C:\Azure-SQL-DW-Synapse-Test-Case-Runner\Sample-Concurrency-SQL-v2"
            }); ;

            #endregion // Execution Runs


            // Read all the SQL Statements and do any necessary string replacements on the SQL
            foreach (ExecutionRun executionRun in executionRuns)
            {
                Console.WriteLine("Starting Execution Run: " + executionRun.ScriptPath);

                if (executionRun.Enabled == false)
                {
                    Console.WriteLine("Skipping Execution Run: " + executionRun.ScriptPath);
                    continue;
                }

                // local variables
                List<string> sessionIds = new List<string>();
                List<SQLTask> sqlScripts = new List<SQLTask>();
                string rawSQL = null;
                string fileName = "";
                int counter = 1;
                DateTime startDate = DateTime.UtcNow;
                DateTime endDate = DateTime.UtcNow;
                sessionIds.Clear();
                sqlScripts.Clear();
                startDate = DateTime.UtcNow;

                // Prepare the scripts for execution
                // We want to do a search and replace on certain items since we might need to change table names or resource classes.
                for (int i = 1; i <= executionRun.Interations; i++)
                {
                    foreach (var item in System.IO.Directory.GetFiles(executionRun.ScriptPath))
                    {
                        SQLTask sqlTask = new SQLTask();

                        rawSQL = System.IO.File.ReadAllText(item);

                        fileName = item.Substring(item.LastIndexOf("\\") + 1).Replace(".sql", string.Empty);

                        // **** CHANGE ME (OVERRIDES PER CUSTOMER) ***
                        // OVERRIDES: To handle multiple CTASs (append the loop variable)
                        // What this will do is find "my_ctas_table" and replace it with "my_ctas_table_1" so if we have 2 loops we will get a _1 and _2 table so we do not have collections
                        rawSQL = rawSQL.Replace("my_ctas_table", "my_ctas_table_" + i.ToString());

                        sqlTask.ConnectionString = executionRun.ConnectionString;
                        sqlTask.ScriptName = fileName;
                        sqlTask.Label = fileName + " :: " + i.ToString() + " :: " + counter.ToString();
                        sqlTask.SQL = rawSQL;

                        // **** CHANGE ME (OVERRIDES PER CUSTOMER) ***
                        // OVERRIDES: Change certain scripts to run in different classes
                        if (sqlTask.ScriptName.ToUpper() == "????")
                        {
                            // Do whatever you want in here
                            sqlTask.ConnectionString = SQL_CONNECTION_LARGE;
                        }

                        sqlScripts.Add(sqlTask);
                        counter++;
                    } // foreach
                } // i


                if (executionRun.Mode == SerialOrConcurrentEnum.Concurrent)
                {
                    // CONCURRENT 
                    List<Task<string>> executeSQLTask = new List<Task<string>>();
                    startDate = DateTime.UtcNow;
                    foreach (SQLTask sqlTask in sqlScripts)
                    {
                        executeSQLTask.Add(Task<string>.Run(() => ExecuteSQL(sqlTask.Label, sqlTask.SQL, sqlTask.ConnectionString)));
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
                        Task<string> serialSQL = Task<string>.Run(() => ExecuteSQL(sqlTask.Label, sqlTask.SQL, sqlTask.ConnectionString));
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



                // Save the data to the Automated Test Tables
                try
                {
                    using (SqlConnection connection = new SqlConnection(SQL_CONNECTION_SMALL))
                    {
                        connection.Open();

                        using (SqlCommand command = new SqlCommand("SELECT 1 + ISNULL(MAX(AutomatedTestId), 0) FROM telemetry.AutomatedTest", connection))
                        {
                            string AutomatedTestId = command.ExecuteScalar().ToString();

                            command.CommandText = "INSERT telemetry.AutomatedTest(AutomatedTestId, Description, Mode, StartTime, EndTime, DWU, CacheState, OptLevel, ScriptMods, ResourceClass)" +
                                                  "VALUES(" + AutomatedTestId + ", " +
                                                  "'" + executionRun.Mode.ToString() + " Test - Iterations (" + executionRun.Interations.ToString() + ")', " +
                                                  "'" + executionRun.Mode.ToString() + "', " +
                                                  "'" + startDate.ToString("yyyy-MM-dd HH:mm:ss.fff") + "'," +
                                                  "'" + endDate.ToString("yyyy-MM-dd HH:mm:ss.fff") + "'," +
                                                  "'" + DWUs + "', " + // we could make this dynamic
                                                  "'" + executionRun.CacheState + "'," +
                                                  "'" + executionRun.OptLevel + "'," +
                                                  "'" + executionRun.ScriptPath.Substring(executionRun.ScriptPath.LastIndexOf("\\") + 1) + "','" +
                                                  executionRun.ResourceClass + "');";

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

                Console.WriteLine("Completed Execution Run: " + executionRun.ScriptPath);

            } // foreach Execution run


            Console.WriteLine("Program Complete!");
        } // Main



        /// <summary>
        /// Runs a dynamic SQL Statement and returned the Synapse session id for tracking the timings.
        /// </summary>
        private static string ExecuteSQL(string label, string sql, string connectionString)
        {
            Console.WriteLine("BEGIN: " + label);
            string sessionId = null;
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    using (SqlCommand command = new SqlCommand(sql, connection))
                    {
                        command.CommandTimeout = 0;
                        command.ExecuteNonQuery();
                        command.CommandText = "SELECT session_id()";
                        sessionId = command.ExecuteScalar().ToString();
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
                Console.WriteLine("END:   " + label + " SESSION: " + label + " - " + (sessionId ?? "NULL"));
            }

            return sessionId;
        } // Execute SQL


        // Do you want to run in parallel or serial
        public enum SerialOrConcurrentEnum { Serial, Concurrent }

        public struct SQLTask
        {
            public string ScriptName { get; set; }
            public string ConnectionString { get; set; }
            public string Label { get; set; }
            public string SQL { get; set; }
        } // SQLTask


        public struct ExecutionRun
        {
            // How many times to run the scripts (in serial mode this is looped one by one and in concurrency mode, this is ALL scripts at once)
            public int Interations { get; set; }

            // How to run the scripts
            public SerialOrConcurrentEnum Mode { get; set; }

            // Constant from server
            public string DWU { get; set; }

            // Did we warm anything up or turn on result set caching
            public string CacheState { get; set; }

            // What type of optomizations to the scripts or servers (materizied views or such) - this is user defined
            public string OptLevel { get; set; }

            // Paths to the scripts (if you want the scripts to run in a certain order then name them 01-script1.sql, 02-script2.sql, etc...)
            public string ScriptPath { get; set; }

            // What resource class are we running the database connection
            public string ResourceClass { get; set; }

            // The Synapse connection string
            public string ConnectionString { get; set; }

            // Ship the execution
            public bool Enabled { get; set; }
        } // ExecutionRun


    } // class
} //namespace
