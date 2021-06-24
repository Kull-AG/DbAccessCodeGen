CREATE TABLE dbo.Pets (PetId int PRIMARY KEY , PetName varchar(100) not null, PetHeight decimal, IsNice bit, ts timestamp)
GO
CREATE TABLE dbo.TestDbVersion(VersionNr int)
GO
INSERT INTO dbo.TestDbVersion(VersionNr) VALUES('$(DbVersion)')
GO
INSERT INTO dbo.Pets(PetId, PetName, PetHeight, IsNice)
SELECT 1, 'Dog', 12.2445, 0
UNION ALL 
SELECT 2, 'Dog 2', 0.2345, 1
GO
CREATE PROCEDURE spGetPets
	@OnlyNice bit=0,
	@SearchString varchar(100)='',
	@IpAddress varchar(100),
	@ProgramVersion varchar(100),
	@ParameterNoOneLikes bit = 1
AS
BEGIN
	SELECT PetId, PetName, IsNice, ts, 1.0 AS [IsARealPet%], '' AS TestCol FROM dbo.Pets
		WHERE IsNice=1 OR @OnlyNice=0
		ORDER BY PetId;
END
GO
CREATE PROCEDURE spGetPet
	@Petid int
AS
BEGIN
	SELECT * FROM dbo.Pets WHERE PetId=@PetId
END
GO
CREATE PROCEDURE spAddPet
	@PetName nvarchar(1000),
	@IsNice bit
AS
BEGIN
	-- Just pretending
	SELECT CONVERT(BIT,1) AS Success, 3 AS NewPetId
END
GO
CREATE PROCEDURE spDeletePet
	@Petid int
AS
BEGIN
	-- Just pretending
	SELECT CONVERT(BIT,1) AS Success
END
GO
CREATE PROCEDURE spUpdatePet
	@Petid int,
	@Ts timestamp
AS
BEGIN
	-- Just pretending
	SELECT CONVERT(BIT,1) AS Success
		FROM dbo.Pets WHERE PetId=@PetId AND ts=@Ts
END
GO
CREATE PROCEDURE spUpdateDog
	@Dogid int,
	@Ts timestamp out
AS
BEGIN
	SET @Ts = 0x01;
END
GO
CREATE PROCEDURE spSearchPets
	@SearchString nvarchar(MAX), 
	@DateOfTool datetime
AS
BEGIN 	
	SELECT* FROM Pets WHERE PetName LIKE '%' + @SearchString + '%'
END
GO
CREATE TYPE dbo.IdNameType AS TABLE 
(
	Id bigint, 
	Name nvarchar(1000), 
    PRIMARY KEY (Id)
)
GO
CREATE PROCEDURE dbo.spTestBackend
	@SomeId int,
	@Ids dbo.IdNameType readonly
AS
BEGIN
	SELECT * FROM @Ids
END
GO
CREATE PROCEDURE dbo.spTestNoColumnName
AS
BEGIN
	SELECT GETDATE(), 'TestResult'
END
GO
CREATE PROCEDURE dbo.spTestExecuteParams
	@TestId int
AS
BEGIN
	SELECT @TestId as tid  INTO #tempo
	SELECT @TEstId AS TestId FROM #tempo
END
GO
CREATE PROCEDURE dbo.spTestDate
	 @DateParam datetime2
AS
BEGIN
	SELECT @DateParam as [Date]
END
GO
CREATE PROCEDURE dbo.spBuggyProc
AS
BEGIN
	SELECT 1/CONVERT(INT, 0) AS ZeroException
END
GO
CREATE PROCEDURE dbo.spUserNotPermitted
AS
BEGIN
	RAISERROR('You are not permitted', 16,1,1);
	RETURN;
	SELECT 'hallo' AS Test
END
GO
CREATE PROCEDURE dbo.spFile
	@Image_Content varbinary(MAX),
	@Image_ContentType varchar(1000),
	@Image_FileName varchar(1000),
	@FileDesc varchar(1000)
AS
BEGIN
	SELECT @Image_Content AS Content, @Image_ContentType as ContentType, @Image_FileName AS [FileName]
END
GO
CREATE SCHEMA imp
GO
CREATE TABLE imp.FileImport(
	FileId BIGINT PRIMARY KEY IDENTITY(1,1),
	Content varbinary(MAX),
	[FileName] varchaR(100))
GO
CREATE PROCEDURE imp.spFile_Add
	@Content varbinary(MAX),
	@FileName varchar(100)
AS
BEGIN
	INSERT INTO imp.FileImport([Content], [FileName])
	SELECT @Content, @FileName 
	SELECT SCOPE_IDENTITY() AS NewFileId
END
GO
CREATE PROCEDURE imp.spFile_Get
	@FileId bigint
AS
BEGIN
	SELECT * FROM imp.FileImport WHERE FileId=@FileId;
END
GO
CREATE PROCEDURE imp.spReturnsNothing
	@FileId bigint
AS
BEGIN
	RETURN 1;
END