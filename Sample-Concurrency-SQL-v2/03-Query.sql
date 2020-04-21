-- This uses the AdventureWorks pre-populated sample database
SELECT COUNT(*)
  FROM [dbo].[FactInternetSales]
       INNER JOIN [dbo].[FactInternetSalesReason]
	           ON [dbo].[FactInternetSales].[SalesOrderNumber] = [dbo].[FactInternetSalesReason].[SalesOrderNumber]
			  AND [dbo].[FactInternetSales].[SalesOrderLineNumber] = [dbo].[FactInternetSalesReason].[SalesOrderLineNumber]
			  AND [UnitPrice] BETWEEN 0 AND 100
OPTION(LABEL = '03-Query.1')
