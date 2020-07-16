using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Text;

namespace SynapseTestTelemetry
{

    class TelemetrySqlHelper
    {

        //Static members are not intended to be thread-safe
        /*
         * testPassId member variable
         * LogTestPassStart, LogTestPassEnd
         * GetTestCases
        */

        private const int _noTestActive = -1;

        private static int _testPassId = _noTestActive;

        public static List<TestCaseInfo> GetTestCases(string connString, int workloadId)
        {
            List<TestCaseInfo> listOfTasks = new List<TestCaseInfo>();
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
                                TestCaseInfo testCase = new TestCaseInfo();

                                string fileName = reader.GetString(0);

                                testCase.SqlFileName = reader.GetString(0);
                                testCase.TestCaseNum = reader.GetInt32(1);
                                testCase.TestCaseName = reader.GetString(2);


                                listOfTasks.Add(testCase);
                            }
                        }
                    }
                }
            }
            catch (SqlException e)
            {
                Console.WriteLine("*********** ERROR in TelemetrySqlHelper: " + e.ToString());
            }

            return listOfTasks;

        }

        public static int LogTestPassStart(string connString, int workloadId, string DWU, string cacheState, string optLevel, string scriptMods, string resourceClass, string serialOrConcurrent)
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
                        //command.Parameters.Add("@ConcurrentOrSerial", SqlDbType.NVarChar, 20).Value = serialOrConcurrent == SerialOrConcurrentEnum.Concurrent ? "Concurrent" : "Serial";
                        command.Parameters.Add("@ConcurrentOrSerial", SqlDbType.NVarChar, 20).Value = serialOrConcurrent;

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

                _testPassId = _noTestActive;
            }
            catch (SqlException e)
            {
                Console.WriteLine("*********** ERROR in TelemetrySqlHelper: " + e.ToString());
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
            if (_testPassId == _noTestActive)
                throw new InvalidOperationException("LogTestRunStart cannot be called if a TestPass has not been started first.");

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
            if (_testPassId == _noTestActive)
                throw new InvalidOperationException("LogTestRunEnd cannot be called if a TestPass has not been started first.");

            string sessionId = "";
            string errText = "NULL";

            if (sqlEx != null)
            {
                errText = String.Format("'ErrorNumber: {0} | ErrorSeverity: {1} | ErrorState: {2} | ErrorProcedure: {3} | ErrorMessage: {4}'", sqlEx.Number, "?", sqlEx.State, sqlEx.Procedure, sqlEx.Message.Replace("'", "''"));
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

            List<TestCaseInfo> tasks = TelemetrySqlHelper.GetTestCases(connString, 1);

            int testPassId = TelemetrySqlHelper.LogTestPassStart(connString, 1, "DW400c", "Unknown", "Stage 1", "v1.0", "smallrc", "Serial");

            using (SqlConnection connection = new SqlConnection(connString))
            {
                connection.Open();

                TelemetrySqlHelper helper = new TelemetrySqlHelper();

                foreach (TestCaseInfo task in tasks)
                {
                    string testRunId = helper.LogTestRunStart(connection, task.TestCaseNum);
                    helper.LogTestRunEnd(connection, testRunId, null);
                }
            }

            TelemetrySqlHelper.LogTestPassEnd(connString);
        }
    }

    public class TestCaseInfo
    {
        public string TestCaseID { get; set; }
        public int TestCaseNum { get; set; }
        public int WorkloadId { get; set; }
        public int ExecutionOrder { get; set; }
        public string TestCaseName { get; set; }
        public string SqlFileName { get; set; }
    }


}


