USE [master]
GO 
-- Server database
if (exists (select * from sys.databases where name = 'ServerDatabase'))
Begin
	ALTER DATABASE [ServerDatabase] SET  SINGLE_USER WITH ROLLBACK IMMEDIATE;
	DROP DATABASE [ServerDatabase]
End
Create database [ServerDatabase]
Go
-- Client database. No need to create the schema, Dotmim.Sync will do
if (exists (select * from sys.databases where name = 'ClientDatabase'))
Begin
	ALTER DATABASE [ClientDatabase] SET  SINGLE_USER WITH ROLLBACK IMMEDIATE;
	DROP DATABASE [ClientDatabase]
End
Create database [ClientDatabase]
GO
USE [ServerDatabase]
GO
CREATE SCHEMA [js]
GO
CREATE TABLE [js].[OnDemandJobs](
	[Id] [uniqueidentifier] NOT NULL PRIMARY KEY  DEFAULT (newsequentialid()),
	[AssetAlias] [nvarchar](max) NULL,
	[Name] [nvarchar](max) NULL,
	[Server] [nvarchar](max) NULL,
	[User] [nvarchar](max) NULL,
	[Parameters] [nvarchar](max) NULL,
	[Executed] [bit] NOT NULL,
	[ExecutionDurationInSeconds] [int] NULL,
	[ExecutionTimeUtc] [datetime2](7) NULL,
	[ExecutionMessage] [nvarchar](max) NULL
)
GO

USE [ClientDatabase]
GO
CREATE SCHEMA [js]
GO
CREATE TABLE [js].[OnDemandJobs](
	[Id] [uniqueidentifier] NOT NULL PRIMARY KEY DEFAULT (newsequentialid()),
	[AssetAlias] [nvarchar](max) NULL,
	[Name] [nvarchar](max) NULL,
	[Server] [nvarchar](max) NULL,
	[User] [nvarchar](max) NULL,
	[Parameters] [nvarchar](max) NULL,
	[Executed] [bit] NOT NULL,
	[ExecutionDurationInSeconds] [int] NULL,
	[ExecutionTimeUtc] [datetime2](7) NULL,
	[ExecutionMessage] [nvarchar](max) NULL
) 
GO
ALTER DATABASE [ServerDatabase] SET ALLOW_SNAPSHOT_ISOLATION ON  
ALTER DATABASE [ServerDatabase] SET READ_COMMITTED_SNAPSHOT ON  
ALTER DATABASE [ServerDatabase] SET CHANGE_TRACKING = ON (CHANGE_RETENTION = 7 DAYS, AUTO_CLEANUP = ON)

ALTER DATABASE [ClientDatabase] SET ALLOW_SNAPSHOT_ISOLATION ON  
ALTER DATABASE [ClientDatabase] SET READ_COMMITTED_SNAPSHOT ON  
ALTER DATABASE [ClientDatabase] SET CHANGE_TRACKING = ON (CHANGE_RETENTION = 7 DAYS, AUTO_CLEANUP = ON)