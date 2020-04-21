-- This uses the AdventureWorks pre-populated sample database
SELECT COUNT(*) 
  FROM [dbo].[FactCurrencyRate]
 OPTION(LABEL = '01-Query.1')

SELECT COUNT(*) 
  FROM [dbo].[FactInternetSalesReason]
 OPTION(LABEL = '01-Query.2')