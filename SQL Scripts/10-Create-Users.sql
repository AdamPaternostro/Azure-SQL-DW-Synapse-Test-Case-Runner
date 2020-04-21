-- Run in Master Database
CREATE LOGIN resource_class_small WITH PASSWORD = 'REPLACE_me_01' 
GO
CREATE USER resource_class_small FOR LOGIN resource_class_small WITH DEFAULT_SCHEMA = dbo
GO

CREATE LOGIN resource_class_medium WITH PASSWORD = 'REPLACE_me_01' 
GO
CREATE USER resource_class_medium FOR LOGIN resource_class_medium WITH DEFAULT_SCHEMA = dbo
GO

CREATE LOGIN resource_class_large WITH PASSWORD = 'REPLACE_me_01' 
GO
CREATE USER resource_class_large FOR LOGIN resource_class_large WITH DEFAULT_SCHEMA = dbo
GO

CREATE LOGIN resource_class_xlarge WITH PASSWORD = 'REPLACE_me_01' 
GO
CREATE USER resource_class_xlarge FOR LOGIN resource_class_xlarge WITH DEFAULT_SCHEMA = dbo
GO

-- Run in Your Database
CREATE USER resource_class_small FOR LOGIN resource_class_small WITH DEFAULT_SCHEMA = dbo
GO
EXEC sp_addrolemember N'db_owner', N'resource_class_small'
GO
-- Not needed since a user is in the smallrc role by default
-- EXEC sp_addrolemember 'smallrc', 'resource_class_small';
-- GO

CREATE USER resource_class_medium FOR LOGIN resource_class_medium WITH DEFAULT_SCHEMA = dbo
GO
EXEC sp_addrolemember N'db_owner', N'resource_class_medium'
GO
EXEC sp_addrolemember 'mediumrc', 'resource_class_medium';
GO

CREATE USER resource_class_large FOR LOGIN resource_class_large WITH DEFAULT_SCHEMA = dbo
GO
EXEC sp_addrolemember N'db_owner', N'resource_class_large'
GO
EXEC sp_addrolemember 'largerc', 'resource_class_large';
GO

CREATE USER resource_class_xlarge FOR LOGIN resource_class_xlarge WITH DEFAULT_SCHEMA = dbo
GO
EXEC sp_addrolemember N'db_owner', N'resource_class_xlarge'
GO
EXEC sp_addrolemember 'xlargerc', 'resource_class_xlarge';
GO


-- View the results
SELECT database_principals_1.name AS DatabaseRoleName,   ISNULL (database_principals_2.name, '(empty)') AS DatabaseUserName   
 FROM sys.database_role_members AS DRM  
      RIGHT OUTER JOIN sys.database_principals AS database_principals_1  
                    ON DRM.role_principal_id = database_principals_1.principal_id  
      LEFT OUTER JOIN sys.database_principals AS database_principals_2  
                   ON DRM.member_principal_id = database_principals_2.principal_id  
 WHERE database_principals_1.type = 'R'
ORDER BY database_principals_1.name; 