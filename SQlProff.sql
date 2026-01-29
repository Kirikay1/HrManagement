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
	IdEvent INT NOT NULL REFERENCES [HrEvent] (Id),
	IdMaterial INT NOT NULL REFERENCES [Material] (Id)
)
GO
CREATE TABLE [VacationCalendar]
(
	Id INT NOT NULL PRIMARY KEY IDENTITY,
	BeginVacation DATE NOT NULL,
	EndVacation DATE NOT NULL
)
GO
CREATE TABLE [WorkingCalendar]
(
    Id BIGINT NOT NULL PRIMARY KEY IDENTITY,
    ExceptionDate DATE NOT NULL,
    IsWorkingDay  BIT NOT NULL
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
	ParentId INT NULL REFERENCES [Department] (Id)
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
	EmployeeOffice NVARCHAR(50) NOT NULL,
	Other NVARCHAR(1000) NULL,
	CalendarEmployee INT NULL REFERENCES [Calendar] (Id)
)
GO

exec sp_addextendedproperty 'MS_Description', N'Список дней исключений в производственном календаре', 'SCHEMA', 'dbo',
     'TABLE', 'WorkingCalendar'
go

exec sp_addextendedproperty 'MS_Description', N'Идентификатор строки', 'SCHEMA', 'dbo', 'TABLE', 'WorkingCalendar',
     'COLUMN', 'Id'
go

exec sp_addextendedproperty 'MS_Description', N'День-исключение', 'SCHEMA', 'dbo', 'TABLE', 'WorkingCalendar', 'COLUMN',
     'ExceptionDate'
go

exec sp_addextendedproperty 'MS_Description', N'0 - будний день, но законодательно принят выходным', 'SCHEMA', 'dbo',
     'TABLE', 'WorkingCalendar', 'COLUMN', 'IsWorkingDay'
go

SET IDENTITY_INSERT WorkingCalendar ON
GO

INSERT INTO WorkingCalendar (Id, ExceptionDate, IsWorkingDay) VALUES (1, N'2024-01-01', 0);
INSERT INTO WorkingCalendar (Id, ExceptionDate, IsWorkingDay) VALUES (2, N'2024-01-02', 0);
INSERT INTO WorkingCalendar (Id, ExceptionDate, IsWorkingDay) VALUES (3, N'2024-01-03', 0);
INSERT INTO WorkingCalendar (Id, ExceptionDate, IsWorkingDay) VALUES (4, N'2024-01-04', 0);
INSERT INTO WorkingCalendar (Id, ExceptionDate, IsWorkingDay) VALUES (5, N'2024-01-05', 0);
INSERT INTO WorkingCalendar (Id, ExceptionDate, IsWorkingDay) VALUES (6, N'2024-01-08', 0);
INSERT INTO WorkingCalendar (Id, ExceptionDate, IsWorkingDay) VALUES (7, N'2024-02-23', 0);
INSERT INTO WorkingCalendar (Id, ExceptionDate, IsWorkingDay) VALUES (8, N'2024-03-08', 0);
INSERT INTO WorkingCalendar (Id, ExceptionDate, IsWorkingDay) VALUES (9, N'2024-04-27', 1);
INSERT INTO WorkingCalendar (Id, ExceptionDate, IsWorkingDay) VALUES (10, N'2024-04-29', 0);
INSERT INTO WorkingCalendar (Id, ExceptionDate, IsWorkingDay) VALUES (11, N'2024-04-30', 0);
INSERT INTO WorkingCalendar (Id, ExceptionDate, IsWorkingDay) VALUES (12, N'2024-05-01', 0);
INSERT INTO WorkingCalendar (Id, ExceptionDate, IsWorkingDay) VALUES (13, N'2024-05-09', 0);
INSERT INTO WorkingCalendar (Id, ExceptionDate, IsWorkingDay) VALUES (14, N'2024-05-10', 0);
INSERT INTO WorkingCalendar (Id, ExceptionDate, IsWorkingDay) VALUES (15, N'2024-06-12', 0);
INSERT INTO WorkingCalendar (Id, ExceptionDate, IsWorkingDay) VALUES (16, N'2024-11-02', 1);
INSERT INTO WorkingCalendar (Id, ExceptionDate, IsWorkingDay) VALUES (17, N'2024-11-04', 0);
INSERT INTO WorkingCalendar (Id, ExceptionDate, IsWorkingDay) VALUES (18, N'2024-12-28', 1);
INSERT INTO WorkingCalendar (Id, ExceptionDate, IsWorkingDay) VALUES (19, N'2024-12-30', 0);
INSERT INTO WorkingCalendar (Id, ExceptionDate, IsWorkingDay) VALUES (20, N'2024-12-31', 0);

SET IDENTITY_INSERT WorkingCalendar OFF
GO

CREATE TABLE StagingOrg (
    Col1 NVARCHAR(500), -- Тут будет либо Департамент (1.1...), либо Должность
    Col2 NVARCHAR(500), -- Тут будет ФИО сотрудника (или NULL для строк отделов)
    BirthDate NVARCHAR(50),
    Phone NVARCHAR(50),
    Cabinet NVARCHAR(50),
    Email NVARCHAR(100)
)