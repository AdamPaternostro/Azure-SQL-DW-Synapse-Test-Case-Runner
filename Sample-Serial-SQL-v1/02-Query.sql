-- This uses the AdventureWorks pre-populated sample database
SELECT COUNT(*)
  FROM [dbo].[FactFinance]
       INNER JOIN [dbo].[DimDate]
	           ON [dbo].[FactFinance].[DateKey] = [dbo].[DimDate].[DateKey]
       INNER JOIN [dbo].[DimOrganization]
	           ON [dbo].[FactFinance].[OrganizationKey] = [dbo].[DimOrganization].[OrganizationKey]
OPTION(LABEL = '02-Query.1')