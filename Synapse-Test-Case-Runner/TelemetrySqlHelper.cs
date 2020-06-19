using System;
using System.Collections.Generic;
using System.Text;
using System.Data.SqlClient;
using System.Data;
using System.IO;
using System.Security.Cryptography;

namespace SynapseConcurrency
{
    class TelemetrySqlHelper
    {

        //Static members are not intended to be thread-safe
        /*
         * testPassId member variable
         * LogTestPassStart, LogTestPassEnd
         * GetTestCases
        */

        private static int _testPassId = -1;

        public static List<SQLTask> GetTestCases(string connString, int workloadId, string scriptPath)
        {
            List<SQLTask> listOfTasks = new List<SQLTask>();
            string commandText = String.Format("SELECT SqlFileName, TestCaseNum, TestCaseName FROM telemetry.TestCase WHERE WorkloadId = {0} ORDER BY ExecutionOrder", workloadId);

            try
            {
                using (SqlConnection connection = new SqlConnection(connString))
                {
                    connection.Open();

                    using (SqlCommand command = new SqlCommand(commandText, connection))
                    {
                        using (SqlDataReader reader = command.ExecuteReader(CommandBehavior.SingleResult))
                        {
                            while (reader.Read())
                            {
                                SQLTask sqlTask = new SQLTask();

                                string fileName = reader.GetString(0);
                                string rawSQL = File.ReadAllText(Path.Combine(scriptPath, fileName));

                                // **** NEED TO RE-LOCATE THIS TO A COMMON LOCATION ****
                                // **** CHANGE ME (OVERRIDES PER CUSTOMER) ****
                                // OVERRIDES: To handle multiple CTASs (append the loop variable)
                                // What this will do is find "my_ctas_table" and replace it with "my_ctas_table_1" so if we have 2 loops we will get a _1 and _2 table so we do not have collections
                                //rawSQL = rawSQL.Replace("my_ctas_table", "my_ctas_table_" + i.ToString());


                                sqlTask.ConnectionString = connString;
                                sqlTask.ScriptName = fileName;
                                sqlTask.Label = reader.GetString(2);
                                sqlTask.SQL = rawSQL;
                                sqlTask.TestCaseNum = reader.GetInt32(1);

                                listOfTasks.Add(sqlTask);
                            }
                        }
                    }
                }
            }
            catch (SqlException e)
            {
                Console.WriteLine("*********** ERROR in TelemetrySqlHelper: " + e.ToString());
                //Console.ReadKey();  // <-- is this a good idea during concurrent tests? -ATR
            }

            return listOfTasks;

        }

        public static int LogTestPassStart(string connString, int workloadId, string DWU, string cacheState, string optLevel, string scriptMods, string resourceClass, SerialOrConcurrentEnum serialOrConcurrent)
        {
            int newTestPassId = -1;
            int integerDWU;

            if (!String.IsNullOrWhiteSpace(DWU))
            {
                if (false == int.TryParse(DWU.TrimStart('D', 'W').TrimEnd('c'), out integerDWU))
                    throw new ArgumentException("Could not parse DWU argument");
            }
            else
                throw new ArgumentException("Missing value in parameter DWU");

            try
            {
                using (SqlConnection connection = new SqlConnection(connString))
                {
                    connection.Open();

                    using (SqlCommand command = new SqlCommand())
                    {
                        command.Connection = connection;
                        command.CommandType = CommandType.StoredProcedure;
                        command.CommandText = "telemetry.usp_logtestpass_start";

                        command.Parameters.Add("@WorkloadId", SqlDbType.Int).Value = workloadId;
                        command.Parameters.Add("@DWU", SqlDbType.Int).Value = integerDWU;
                        command.Parameters.Add("@CacheState", SqlDbType.VarChar, 50).Value = cacheState;
                        command.Parameters.Add("@OptLevel", SqlDbType.VarChar, 50).Value = optLevel;
                        command.Parameters.Add("@ScriptMods", SqlDbType.VarChar, 50).Value = scriptMods;
                        command.Parameters.Add("@ResourceClass", SqlDbType.NVarChar, 20).Value = resourceClass;
                        command.Parameters.Add("@ConcurrentOrSerial", SqlDbType.NVarChar, 20).Value = serialOrConcurrent == SerialOrConcurrentEnum.Concurrent ? "Concurrent" : "Serial";

                        SqlParameter testPassIdParam = command.Parameters.Add("@test_pass_id", SqlDbType.Int);
                        testPassIdParam.Direction = ParameterDirection.Output;

                        command.CommandTimeout = 60;
                        command.ExecuteNonQuery();


                        if (testPassIdParam.Value != null)
                        {
                            if (int.TryParse(testPassIdParam.Value.ToString(), out newTestPassId))
                            {
                                _testPassId = newTestPassId;
                                Console.WriteLine("Started Logging Test Pass ID {0}", newTestPassId);
                            }
                        }

                        if (newTestPassId == -1)
                        {
                            Console.WriteLine("*********** ERROR Failed to get Test Pass ID for WorkloadId {0}", workloadId);
                        }
                    }
                }
            }
            catch (SqlException e)
            {
                Console.WriteLine("*********** ERROR in TelemetrySqlHelper: " + e.ToString());
                //Console.ReadKey();  // <-- is this a good idea during concurrent tests? -ATR
            }

            return newTestPassId;
        }

        public static void LogTestPassEnd(string connString)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connString))
                {
                    connection.Open();

                    using (SqlCommand command = new SqlCommand())
                    {
                        command.Connection = connection;
                        command.CommandType = CommandType.StoredProcedure;
                        command.CommandText = "telemetry.usp_logtestpass_end";

                        command.Parameters.Add("@test_pass_id", SqlDbType.Int).Value = _testPassId;

                        command.CommandTimeout = 60;
                        command.ExecuteNonQuery();

                        Console.WriteLine("Ended Logging Test Pass ID {0}", _testPassId);
                    }
                }
            }
            catch (SqlException e)
            {
                Console.WriteLine("*********** ERROR in TelemetrySqlHelper: " + e.ToString());
                //Console.ReadKey();  // <-- is this a good idea during concurrent tests? -ATR
            }

        }


        //To support concurrent tests, these members need to be thread-safe
        //Therefore they are instance methods
        /*
         * LogTestRunStart
         * LogTestRunEnd
        */
        public string LogTestRunStart(SqlConnection conn, int testCaseNum)
        {
            string newTestRunId = "";

            try
            {

                if (conn.State != ConnectionState.Open)
                    conn.Open();

                using (SqlCommand command = new SqlCommand())
                {
                    command.Connection = conn;
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandText = "telemetry.usp_logtestrun_start";

                    command.Parameters.Add("@TestPassId", SqlDbType.Int).Value = _testPassId;
                    command.Parameters.Add("@TestCaseNum", SqlDbType.Int).Value = testCaseNum;

                    SqlParameter testRunIdParam = command.Parameters.Add("@test_run_id", SqlDbType.VarChar, 100);
                    testRunIdParam.Direction = ParameterDirection.Output;

                    command.CommandTimeout = 60;
                    command.ExecuteNonQuery();


                    if (testRunIdParam.Value != null)
                    {
                        newTestRunId = testRunIdParam.Value.ToString();
                    }
                    else
                    {
                        Console.WriteLine("*********** ERROR Failed to get Test Run ID for TestCaseNum {0}", testCaseNum);
                    }
                }
            }
            catch (SqlException e)
            {
                Console.WriteLine("*********** ERROR in TelemetrySqlHelper: " + e.ToString());
            }

            return newTestRunId;
        }

        public string LogTestRunEnd(SqlConnection conn, string testCaseRunId, SqlException sqlEx)
        {
            string sessionId = "";
            string errText = "NULL";

            if (sqlEx != null)
            {
                errText = String.Format("'ErrorNumber: {0} | ErrorSeverity: {1} | ErrorState: {2} | ErrorProcedure: {3} | ErrorMessage: {4}'", sqlEx.Number, "?", sqlEx.State, sqlEx.Procedure, sqlEx.Message);
            }

            StringBuilder sbCmdText = new StringBuilder();
            sbCmdText.AppendLine(String.Format("exec telemetry.usp_logtestrun_end @TestCaseRunID = '{0}', @ErrorInfo = {1};", testCaseRunId, errText));
            sbCmdText.AppendLine("SELECT session_id();");


            try
            {
                if (conn.State != ConnectionState.Open)
                    conn.Open();

                using (SqlCommand command = new SqlCommand(sbCmdText.ToString(), conn))
                {

                    command.CommandTimeout = 60;
                    sessionId = command.ExecuteScalar().ToString();
                }
            }
            catch (SqlException e)
            {
                Console.WriteLine("*********** ERROR in TelemetrySqlHelper: " + e.ToString());
            }

            return sessionId;
        }

        public static void TestMe(string connString)
        {

            List<SQLTask> tasks = TelemetrySqlHelper.GetTestCases(connString, 2, @"C:\faketestcases");

            int testPassId = TelemetrySqlHelper.LogTestPassStart(connString, 2, "DW400c", "Unknown", "Stage 1", "v1.0", "smallrc", SerialOrConcurrentEnum.Serial);

            using (SqlConnection connection = new SqlConnection(connString))
            {
                connection.Open();

                TelemetrySqlHelper helper = new TelemetrySqlHelper();

                foreach (SQLTask task in tasks)
                {
                    string testRunId = helper.LogTestRunStart(connection, task.TestCaseNum);
                    helper.LogTestRunEnd(connection, testRunId, null);
                }
            }

            TelemetrySqlHelper.LogTestPassEnd(connString);
        }
    }

}


