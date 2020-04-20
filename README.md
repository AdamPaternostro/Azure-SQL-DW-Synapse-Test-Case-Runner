# Azure-SQL-DW-Synapse-Test-Case-Runner
The below will help you perform a data warehouse test on SQL DW / Synapse and help you understand how to compare the results to other vendors. There is also a cross platform tool for running test cases in a serial or concurrently and log the results to a set of tables to see the results.


## How you use
- Create your database in Azure
- Load your database with data
- Write your queries and ensure they are working
- Add Label statements to your queries: OPTION(LABEL = 'my-query.1') - you can use .2 for SQL scripts that have multiple SQL statements
- Exeucte the scripts in the SQL Scripts folder to setup the telemetry capture tables
- Create user ids in SQL Server
    ```
    -- Run in Master Database
    CREATE LOGIN resource_class_small WITH PASSWORD = 'REPLACE_ME' 
    GO
    CREATE USER resource_class_small FOR LOGIN resource_class_small WITH DEFAULT_SCHEMA = dbo
    GO

    -- Run in Your Database
    CREATE USER resource_class_small FOR LOGIN resource_class_small WITH DEFAULT_SCHEMA = dbo
    GO
    EXEC sp_addrolemember N'db_owner', N'resource_class_small'
    GO
    EXEC sp_addrolemember 'smallrc', 'resource_class_small';
    GO
    ```
- Place your SQL statements in the Sample-Serial-SQL-v1 or Sample-Concurrency-SQL-v1 (you can create as many folders are you like)
- Open the solution file Synapse-Test-Case-Runner in Visual Studio (you can also use VS Code for this)
- Update the connection string SQL_CONNECTION_SMALL
- Create your Execution Scenarios
```
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
```
- Run the stored procedure ReplicateTables on the server (this will replicate ALL tables set with Replicate distribution, you can add exceptions if you like)
- Run the stored procedure ReplicateTablesStatus (wait until they are all in a "Ready" state)
- Run the test!  (Consider your first run a test so don't go too big)
- Review the results in the tables in the Telemetry schema.  Run the stored procedure: AutomatedTestStatistics and use this as a bases for creating additional results.


## How to run a SQL DW / Synapse test

### First decide what you are going to test
- Are you testing query performance?
- Are you testing load performance?
- Are you testing concurrency?
- Do you have a baseline set of data?  
   - If you are replacing an existing system do you have a sample set of queries that represent your workload (10 to 30 of them)?
   - Can you run the queries on this system and obtain their timings?
   - Can the data be extracted from the existing system?  Sometimes the existing system does not have much free capacity to do the extract?
   - How are you going to get this data to the cloud if the existing system is on-prem?  Bulk move using an appliance or upload?  Upload is usually the easiest if you have the bandwidth.
- How are you measuring the results?  
   - Are you measure row counts?
   - Are you checking the actual data?


#### Notes about testing
- If you are testing performance make sure you understand your goal of the query performance.  You should optomize your Synapse to run as many queries in the best times possible.  You should try to run as many queries in the smallest resource class on the smallest sized server as possible.  Your goal should be how to run the queries for as fast and cheap as possible.
- If you are testing load performance, why?  A lot of customers test this and a one time load is just a one time load.  If vendor A takes 40 hours and vendor B takes 35 hours, does this really impact you and your business?  You probably would end up spending more than 5 hours trying to optomize vendors A load process to gain the 5 hours.  You should test a real life load scenerio, like you load once a night with 100 GB of data.
- If you are testing concurrency, this is usually the least understood test.  Lots of people think that if you only support 30 concurrency that you can only have 30 people query the database at a time.  If you have 300+ usrs, 30 concurrent queries is usually more than enough.  There is not a direct relation like people think.
- If you are measuring results, then how much time does it take to stream the results somewhere?  The query might take 10 seconds, but streaming 2 billion rows to a client is a problem.  Ideally, run your queries and check their accuracy.  Then run your timing tests with a COUNT(*).


### Running the test
- Have your data to be loaded to Synapse in an Azure storage account in the same region
  - Please note: You should put in a request to raise Azure quota for Synapse. If you are on a short timeline and you might need to do your PoC in a different region than your normal region.  Please be flexible since this is a PoC and not production.  Production customers take precedence over PoCs.
- Have your data in CSV or Parquet files and have a flat struture (not nested data types).  If you are using CSV (or delimited), make sure they are valid. 
    - For delimited files you will need to define:
        - Your seperator (e.g. ',' comma)
        - Your quote (e.g. '"' doublequote)
        - Your escape character (e.g. '\' backslash).  Note: some vendors only process the escape character if the character is quoted (e.g. "Hi \"user\"")
        - Your null values (e.g. 'NULL').  Note: some vendors are case senstive on this.
        - Avoid mulitple lines (line feeds and/or carriage returns in your files).
- If you are concerned about testing the load loads:
     - Split the data: https://techcommunity.microsoft.com/t5/azure-synapse-analytics/how-to-maximize-copy-load-throughput-with-file-splits/ba-p/1314474
- Load the data using the COPY (or Polybase) command: https://docs.microsoft.com/en-us/azure/data-factory/connector-azure-sql-data-warehouse#use-copy-statement
   - You can also use ADF to load your tables for a more repeatable process
- Load into Heap tables
- Load from staging tables to your final tables 
  - Figure out the data should be distributed: round robin, ordered cluster columnstore, etc.
  - Ensure dimension tables are replicated.  Anything less than 2GB should be replicated.
- Turn on resultset caching for some of your tests (or at least ensure each vendor has it off...)
- Create materialized views


### Comparing results from other vendors
- Please be aware of each vendors default configurations. Some vendors have resultset caching on by default and running against a vendor that has it off by default is not an accurate test.
- Please be aware when vendors want a cold start.  Not that many database are running from a cold start.  While it might be a valid test for some vendors, for most is is not doing anything except giving vendor A a talking point over vendor B.  
- Be aware of ETL being done in the database.  Some vendors want ETL done in their database and want this measure as part of the loading process.  With data lake architectures being the dominate architecture of today, your ETL should be done by a distribute compute engine like Spark.  Doing ETL in one of the most expensive resources that you can deploy in the cloud is not a good idea.  Plus this also gets you caught up in vendor lock in when you use specialize non-standard SQL.
- It is okay to have some slight tweaks done to the SQL statements.  If you can spend 10 minutes adjusting a query that take an hour so that it takes minutes, then consider it.  Each vendor should do some minor mondifications to show how their product can potentially perform.
- When comparing vendors there are a few approaches:
   1. Cost.  You can compare vendor A with a $200 an hour system to vendor B with a $200 an hours system.  Vendor A might have more hardware for this amount and might run faster (or slower).  Please make sure when you do cost you know the actual SKU you will be running.  Vendor A might have feature A as part of their standard SKI and Vendor B might require a higher edition for the feature.
   2. Hardware:  You can compare vendor A with a 10 node system with vendor B with a {x} node system.  You might want to compare number of CPUs, RAM, etc.  You will never get an exact alignment (x core with y RAM).
   - I perfer to go on cost since I am typically trying to run queries at the cheapest cost. The hardware will vary over time and you never get an exact alignment accross vendors and/or clouds.


### Interpreting the results
- Once you have the executions complete you should analyze the results
   - Which DWU was the fasest?
   - Which resource class was the fastest?
   - Which resource class was the smallest and close to the fastest?  If a small resource class is 10% slower than the medium and the medium 5% slower then a large, then you need to understand the trade off.  Should I run more queries at x% slower, but be able to run more of them?  Review this link on resource classes https://docs.microsoft.com/en-us/azure/synapse-analytics/sql-data-warehouse/memory-concurrency-limits#concurrency-maximums-for-resource-classes and understand how many slots they each take based upon the size of your warehouse.  If you have a DWU 10,000 with 400 slots and run a smallrc (dynamic) you can run 400/12 = 33 concurrent queries.  If you run a mediumrc (dynamic) you can run 400/40 = 10 concurreny queries. So is it more important to run 10 queries faster or 33 queries simultaneously?
- Is there a slow running query?  Check out the Query Store to see what is going on with the actual execution plan.  Moving lots of data between worker nodes is typically a bottleneck to look for.


## Enhancements to the .NET Core code
- Set the DWU variable automatically
- Scale the database automatically
- Add a PowerBI report
- Call the replication tables programatically and wait until their status is Ready.
- Turn on result set caching programmatically based upon the run


