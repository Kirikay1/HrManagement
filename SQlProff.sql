CREATE DATABASE [HrManagement]
GO

USE [HrManagement]
GO

CREATE TABLE [typeEvent]
(
	Id INT NOT NULL PRIMARY KEY IDENTITY,
	NameEvent NVARCHAR(100) NOT NULL
)
GO

CREATE TABLE [HrEvent]
(
	Id INT NOT NULL PRIMARY KEY IDENTITY,
	typeEventId INT NOT NULL REFERENCES [typeEvent] (Id),
	EventStatus NVARCHAR(50) NOT NULL,
	DateEvent DATETIME NOT NULL,
	ResponsiblePerson NVARCHAR(200) NOT NULL,
	DescriptionEvent NVARCHAR(1000) NOT NULL
)
GO

CREATE TABLE [Material]
(
	Id INT NOT NULL PRIMARY KEY IDENTITY,
	NameMaterial NVARCHAR(200) NOT NULL,
	ApprovalDate DATETIME NOT NULL,
	ModifiedDate DATETIME NOT NULL,
	StatusMaterial NVARCHAR(50) NOT NULL,
	TypeMaterial NVARCHAR(100) NOT NULL,
	AreaMaterial NVARCHAR(100) NOT NULL,
	AuthorMaterial NVARCHAR(200) NOT NULL
)
GO
CREATE TABLE [LearningCalendar]
(
	Id INT NOT NULL PRIMARY KEY IDENTITY,
	IdEvent INT NULL REFERENCES [HrEvent] (Id),
	IdMaterial INT NULL REFERENCES [Material] (Id),
	BeginLearning DATE NOT NULL,
	EndLearningn DATE NOT NULL,
	reasonLearning NVARCHAR(1000) NULL
)
GO
CREATE TABLE [VacationCalendar]
(
	Id INT NOT NULL PRIMARY KEY IDENTITY,
	BeginVacation DATE NOT NULL,
	EndVacation DATE NOT NULL,
	reasonVacation NVARCHAR(1000) NULL

)
GO
CREATE TABLE [WorkingCalendar]
(
    Id BIGINT NOT NULL PRIMARY KEY IDENTITY,
    ExceptionDate DATE NOT NULL,
	EndExceptionDate DATE NOT NULL,
    IsWorkingDay  BIT NULL,
	reasonWorking NVARCHAR(1000) NULL
)
GO
CREATE TABLE [Calendar]
(
	Id INT NOT NULL PRIMARY KEY IDENTITY,
	IdWorkingCalendar BIGINT NULL REFERENCES [WorkingCalendar] (Id),
	IdLearningCalendar INT NULL REFERENCES [LearningCalendar] (Id),
	IdVacationCalendar INT NULL REFERENCES [VacationCalendar] (Id)
)
GO
CREATE TABLE [Position]
(
	Id INT NOT NULL PRIMARY KEY IDENTITY,
	NamePosition NVARCHAR(200) NOT NULL
)
GO
CREATE TABLE [Department]
(
	Id INT NOT NULL PRIMARY KEY IDENTITY,
	NameDepartment NVARCHAR(200) NOT NULL,
	ParentId INT NULL REFERENCES [Department] (Id),
	descriptionDepartment NVARCHAR(200) NULL
)
GO
CREATE TABLE [Employee]
(
	Id INT NOT NULL PRIMARY KEY IDENTITY,
	FullName NVARCHAR(200) NOT NULL,
	PersonalPhone NVARCHAR(20) NULL,
	BirthDate DATE NULL, 
	IdEmployeeDepartment INT NOT NULL REFERENCES [Department] (Id),
	IdPosition INT NOT NULL REFERENCES [Position] (Id),
	DirectSupervisor INT NULL REFERENCES [employee] (Id),
	AssistantEmployee INT NULL REFERENCES [employee] (Id),
	WorkPhone NVARCHAR(20) NOT NULL,
	Email NVARCHAR(100) NOT NULL,
	EmployeeOffice NVARCHAR(10) NOT NULL,
	Other NVARCHAR(1000) NULL,
	CalendarEmployee INT NULL REFERENCES [Calendar] (Id)
)
GO

