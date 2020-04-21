# Azure-SQL-DW-Synapse-Test-Case-Runner
The below will help you perform a data warehouse test on SQL DW / Synapse and help you understand how to compare the results to other vendors. There is also a cross platform tool for running test cases in a serial or concurrently and log the results to a set of tables to see the results.

Just want to run the code? Jump down to the code: https://github.com/AdamPaternostro/Azure-SQL-DW-Synapse-Test-Case-Runner#how-to-run-the-code
The sample code works against a new data warehouse when you include the AdventureWorks sample database. 

## How to run a SQL DW / Synapse test

### Step 01: Decide what you are going to test?
- Are you testing query performance?
- Are you testing load performance?
- Are you testing concurrency?
- Do you have a baseline set of data?  
   - If you are replacing an existing system do you have a sample set of queries that represent your workload (10 to 30 of them)?
   - Can you run the queries on this system and obtain their timings?
   - What are the hardware specs for the current system?
   - Can the data be extracted from the existing system?  
       - Sometimes the existing system does not have much free capacity to do the extract.
   - How are you going to get this data to the cloud if the existing system is on-prem?  Bulk move using an appliance or upload?  Upload is usually the easiest if you have the bandwidth.
- How are you measuring the results?  
   - Are you measure row counts?
   - Are you checking the actual data?


#### Notes about testing
- If you are testing performance make sure you understand your goal of the query performance.  You should optomize your Synapse to run as many queries in the best times possible.  You should try to run as many queries in the smallest resource class on the smallest sized server as possible.  Your goal should be how to run the queries for as fast and cheap as possible.
- If you are testing load performance, why?  A lot of customers test this and a one time load is just a one time load.  If vendor A takes 40 hours and vendor B takes 35 hours, does this really impact you and your business?  You probably would end up spending more than 5 hours trying to optomize vendors A load process to gain the 5 hours.  You should test a real life load scenerio, like you load once a night with 100 GB of data.
- If you are testing concurrency, this is usually the least understood test.  Lots of people think that if you only support 30 concurrency that you can only have 30 people query the database at a time.  If you have 300+ users, 30 concurrent queries is usually more than enough.  There is not a direct relation like people think.  Users have to create a query, execute it and spend time reviewing the results.
- If you are measuring the accuracy of your results, then how much time does it take to stream the results somewhere?  The query might take 10 seconds, but streaming 2 billion rows to a client is a problem.  Ideally, run your queries and check their accuracy.  Once you are sure of their accuracy then run your timing tests with a COUNT(*) or TOP 1.


### Step 02: Running the test
- Have your data to be loaded to Synapse in an Azure storage account in the same region
  - Please note: You should put in a request to raise Azure quota for Synapse. If you are on a short timeline and you might need to do your PoC in a different region than your normal region.  Please be flexible since this is a PoC and not production.  Production customers take precedence over PoCs.
- Have your data in CSV or Parquet files and have a flat struture (not nested data types).  If you are using CSV (or delimited), make sure they are valid. 
    - For delimited files you will need to define:
        - Your seperator (e.g. ',' comma)
        - Your quote (e.g. '"' doublequote)
        - Your escape character (e.g. '\' backslash).  Note: some vendors only process the escape character if the character is quoted (e.g. "Hi \\"user\\"")
        - Your null value (e.g. 'NULL').  Note: some vendors are case senstive on this.
        - Avoid mulitple lines (line feeds and/or carriage returns in your files).
- If you are concerned about testing the load loads:
     - Split the data: https://techcommunity.microsoft.com/t5/azure-synapse-analytics/how-to-maximize-copy-load-throughput-with-file-splits/ba-p/1314474
- Load the data using the COPY (or Polybase) command: https://docs.microsoft.com/en-us/azure/data-factory/connector-azure-sql-data-warehouse#use-copy-statement
   - You can also use ADF to load your tables for a more repeatable process
- Load into Heap tables
   - See best practices: https://docs.microsoft.com/en-us/azure/synapse-analytics/sql-data-warehouse/guidance-for-loading-data
- Load from staging tables to your final tables 
  - Figure out the data should be distributed: round robin, ordered cluster columnstore, etc.
  - Ensure dimension tables are replicated.  Anything less than 2GB should be replicated.
- Turn on resultset caching for some of your tests (or at least ensure each vendor has it off...)
- Create materialized views


### Step 03: Comparing results from other vendors
- Please be aware of each vendors default configurations. Some vendors have resultset caching on by default and running against a vendor that has it off by default is not an accurate test.
- Please be aware when vendors want a cold start.  Not that many database are running from a cold start.  While it might be a valid test for some vendors it typically giving vendor A a talking point over vendor B.  
- Be aware of ETL being done in the database.  Some vendors want ETL done in their database and want this measure as part of the loading process.  With data lake architectures being the dominate architecture of today (see https://github.com/AdamPaternostro/Azure-Big-Data-and-Machine-Learning-Architecture), your ETL should be done by a distribute compute engine like Spark.  Doing ETL in your warehouse is not a good idea to use one of the most expensive resources that you can deploy for parsing data.  Customeres typically will lift and shift with the ETL in their warehouse, but quickly start refactoring their ETL out of their warehouse. Doing ETL in your warehouse risks locking your into a vendor by using vendor specific non-standard SQL.
- It is okay to have some slight tweaks done to the SQL statements.  If you can spend 10 minutes adjusting a query that take an hour so that it takes minutes, then consider it.  Each vendor should do some minor mondifications to show how their product can potentially perform. In most tests you should have:
   1. Version 1 is just the queries updated to compile and run on the system
   2. Version 2 is slight modifictions to take advantage of the particular platform
   3. Version 3 using materialized views and other low effort techiques for improving the speed of the queries
   4. Version 4 is using any modification, reguardless of the effort to implement.
   - You shoud track the effort in the changes from V1 to V4 since you need to know how much effort you will put into optomizing your system. 
- When comparing vendors there are a few approaches:
   1. Cost.  You can compare vendor A with a $200 an hour system to vendor B with a $200 an hour system.  Vendor A might have more hardware for this amount and might run faster (or slower).  Please make sure, when you aquire the costs, you know the actual SKU you will be running.  Vendor A might have feature A as part of their standard SKU and Vendor B might require a higher edition for the feature.
      - Please make sure you are comparing list price to list price without discounts.  Vendors have a habit of giving you the best price to get you on their platform and then you fail to get those same discounts upon your renewal.  Comparing list price or list reserved instance price (since they are published) keeps you from getting a getting a surprise when your renewel occurs.
   2. Hardware:  You can compare vendor A with a 10 node system with vendor B with a {x} node system.  You might want to compare number of CPUs, RAM, etc.  You will never get an exact alignment (x cores with y RAM).
   - I perfer to go on cost since I am typically trying to run queries at the cheapest cost. The hardware will vary over time and you never get an exact alignment accross vendors and/or clouds.
- Your data warehouse is just a part of your architecture.  You should treat is like a black box that you send SQL statements and get results.  Surround your database with cheaper components for ETL, reporting, etc.  You should be able to switch out your warehouse just like any other component of your architecture.  Avoid tying into vendor specific features.


### Step 04: Interpreting the results
- You should run your queires on different size warehouses (DWUs) and with different resource classes (the amount of power a query can use for its connection)
- Once you have the executions complete you should analyze the results
   - Which DWU was the fasest?
   - Which resource class was the fastest?
   - Which resource class was the smallest and close to the fastest?  If a small resource class is 10% slower than the medium and the medium 5% slower then a large, then you need to understand the trade off.  Should I run more queries at x% slower, but be able to run more of them?  Review this link on resource classes https://docs.microsoft.com/en-us/azure/synapse-analytics/sql-data-warehouse/memory-concurrency-limits#concurrency-maximums-for-resource-classes and understand how many slots they each take based upon the size of your warehouse.  If you have a DWU 10,000 with 400 slots and run a smallrc (dynamic) you can run 400/12 = 33 concurrent queries.  If you run a mediumrc (dynamic) you can run 400/40 = 10 concurreny queries. So is it more important to run 10 queries faster or 33 queries simultaneously?
- Is there a slow running query?  Check out the Query Store to see what is going on with the actual execution plan.  Moving lots of data between worker nodes is typically a bottleneck to look for.


### Step 05: Other Criteria
- Security:
   - How will the service integrate into your Azure subscription, virtual networks and overall compliance?
   - How will row and column level security be applied?
   - How are users setup and configured?
- Dev Ops
   - How the system work with your DevOps processes?


### Common Questions
1. How do you choose the appropriate Azure Database?  Azure has lots of choices from standard database to hyerscale database to data warehouses.  Here is how I see them:
   - Standard database are what you use for your OLTP applications.  You have transactions and daily reporting taking place in these databasees.
   - Hyerscale databases allow your OLTP transaction database to store more data, like a warehouse and have many readers for more reporting.  These can grow to 100's of TBs.  
   - Data Warehouse has many worker nodes designed to distribute your query to multiple nodes that all work together on the __same__ query.  These can grow to 100's of TBs to Petabytes.
   - My rule of thumb for a determining if you need a Hyerscale versus a Warehouse is this:
       1. Does your query actually run or does it run out of memory?  If your query runs out of memory on Hyperscale (and it is large instance) then you need to look at a warehouse.  You cannot scale up any more.
       2. Are you joining 2+ billion rows or 100's of billions of rows?  You are looking at a data warehouse.  Having billion row tables is SQL Server does __not__ necessary mean you need a data warehouse.
       3. If you have lots of data, but issue small requests against the data, then you are probably fine with Hyperscale.
   - The best part of Azure is that you can change services with realative ease.  Dumping your data out of your Hyerscale and importing to Synapse is not that difficult and then you can easily test both.
2. How does SQL On-Demand fit into the picture?
   - SQL OD lets you run your queries directly against your data lake.  This is a great feature and is great for items like populating Azure Analysis Service or Power BI.  If you are not in a hurry for the query to execute it gives you choices for other strategies.  See this architecture: https://github.com/AdamPaternostro/Azure-Big-Data-and-Machine-Learning-Architecture


## Running against AdventureWorks sample database
- Create the data warehouse in Azure with the AdventureWorks sample data installed
  - Tip: Create the SQL Admin account name with password "REPLACE_me_01" to make things a little simpiler
- Whitelist your IP address on the SQL Sever
- Run the scripts in the SQL Script folder in order 01, 02, etc.
- Set the following values in the C#
```
        const string DATEBASE_NAME = "REPLACE-ME";
        const string SERVER_NAME = "REPLACE-ME";
        const string SQL_ADMIN_NAME = "sqlAdmin";        
        const string PASSWORD = "REPLACE_me_01"; // assumming all the accounts have the same password (if not change the connection string below)
```
- Run the C# code
- Run the stored procedure [telemetry].[AutomatedTestStatistics_ALL] to see the telemetry of your runs
- Open the Power BI report (refresh the data) and explore the data (PowerBI coming soon)


## How to run the Code
- Create your database in Azure
- Execute the scripts in the SQL Scripts folder to setup the telemetry capture tables
- Load your database with data
- Update statistics on your tables
- Write your queries and ensure they are working
- Place your SQL statements in the Sample-Serial-SQL-v1 or Sample-Concurrency-SQL-v1 (you can create as many folders are you like)
   - NOTE: You need to name your queries files {xx}-{yourname}.sql.  
        - {xx} should be 01, 02, etc.  This will determine the order in which the queries will be executed durign a serial test.
        - {your-name} - a meaningful name of the file or test case
- Add Label statements to your queries: OPTION(LABEL = '01-myquery.1') - you can use .2 for SQL scripts that have multiple SQL statements
    - Note: Your labels names should follow the naming standard of {xx}-{your-name}.{query-number}
        - {xx} - the name number as the file name
        - {your-name} - a meaningful name of the file or test case
        - {query-number} - the first query in the file should start with 1, then 2, etc.
- Open the solution file Synapse-Test-Case-Runner in Visual Studio (you can also use VS Code for this) and update these values
```
        const string DATEBASE_NAME = "REPLACE-ME";
        const string SERVER_NAME = "REPLACE-ME";
        const string SQL_ADMIN_NAME = "sqlAdmin";        
        const string PASSWORD = "REPLACE_me_01"; // assumming all the accounts have the same password (if not change the connection string below)
```
- Create your Execution Scenarios in the C# code
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
- Run the stored procedure [telemetry].[AutomatedTestStatistics_ALL] to see the telemetry of your runs
- Open the Power BI report (refresh the data) and explore the data (PowerBI coming soon)



## Enhancements to the .NET Core code
- Add test case to verify if query ran correctly
- Scale the database automatically
- Add a PowerBI report
- Call the replication tables programatically and wait until their status is Ready.
- Turn on result set caching programmatically based upon the run