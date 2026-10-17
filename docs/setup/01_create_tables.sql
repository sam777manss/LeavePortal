-- LeavePortal: full database setup (run once on an empty Azure SQL database)
-- Paste this whole file into Azure Portal > SQL database > Query editor > Run

-- 1. Departments
CREATE TABLE Departments (
    Id          INT IDENTITY(1,1) PRIMARY KEY,
    Name        NVARCHAR(100)   NOT NULL,
    CreatedAt   DATETIME2       NOT NULL DEFAULT GETUTCDATE()
);

-- 2. Users (Employees + Managers)
CREATE TABLE Users (
    Id              INT IDENTITY(1,1) PRIMARY KEY,
    FullName        NVARCHAR(150)   NOT NULL,
    Email           NVARCHAR(256)   NOT NULL,
    PasswordHash    NVARCHAR(512)   NOT NULL,
    Role            NVARCHAR(20)    NOT NULL,
    DepartmentId    INT             NOT NULL,
    IsActive        BIT             NOT NULL DEFAULT 1,
    CreatedAt       DATETIME2       NOT NULL DEFAULT GETUTCDATE(),

    CONSTRAINT UQ_Users_Email       UNIQUE (Email),
    CONSTRAINT FK_Users_Departments FOREIGN KEY (DepartmentId) REFERENCES Departments(Id),
    CONSTRAINT CHK_Users_Role       CHECK (Role IN ('Employee', 'Manager'))
);

-- 3. LeaveTypes
CREATE TABLE LeaveTypes (
    Id              INT IDENTITY(1,1) PRIMARY KEY,
    Name            NVARCHAR(100)   NOT NULL,
    DefaultDays     INT             NOT NULL,
    Description     NVARCHAR(500)   NULL,
    IsActive        BIT             NOT NULL DEFAULT 1,

    CONSTRAINT UQ_LeaveTypes_Name UNIQUE (Name)
);

-- 4. LeaveBalances (one row per user + leave type + year)
CREATE TABLE LeaveBalances (
    Id              INT IDENTITY(1,1) PRIMARY KEY,
    UserId          INT             NOT NULL,
    LeaveTypeId     INT             NOT NULL,
    Year            INT             NOT NULL,
    TotalDays       INT             NOT NULL,
    UsedDays        INT             NOT NULL DEFAULT 0,
    RemainingDays   AS (TotalDays - UsedDays),

    CONSTRAINT FK_LeaveBalances_Users      FOREIGN KEY (UserId)      REFERENCES Users(Id),
    CONSTRAINT FK_LeaveBalances_LeaveTypes FOREIGN KEY (LeaveTypeId) REFERENCES LeaveTypes(Id),
    CONSTRAINT UQ_LeaveBalances_User_Type_Year UNIQUE (UserId, LeaveTypeId, Year)
);

-- 5. LeaveApplications (the core table)
CREATE TABLE LeaveApplications (
    Id              INT IDENTITY(1,1) PRIMARY KEY,
    UserId          INT             NOT NULL,
    LeaveTypeId     INT             NOT NULL,
    StartDate       DATE            NOT NULL,
    EndDate         DATE            NOT NULL,
    TotalDays       INT             NOT NULL,
    Reason          NVARCHAR(1000)  NOT NULL,
    DocumentUrl     NVARCHAR(2000)  NULL,
    Status          NVARCHAR(20)    NOT NULL DEFAULT 'Pending',
    ReviewedBy      INT             NULL,
    ReviewComment   NVARCHAR(1000)  NULL,
    ReviewedAt      DATETIME2       NULL,
    CreatedAt       DATETIME2       NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt       DATETIME2       NOT NULL DEFAULT GETUTCDATE(),

    CONSTRAINT FK_LeaveApplications_Users      FOREIGN KEY (UserId)      REFERENCES Users(Id),
    CONSTRAINT FK_LeaveApplications_LeaveTypes FOREIGN KEY (LeaveTypeId) REFERENCES LeaveTypes(Id),
    CONSTRAINT FK_LeaveApplications_ReviewedBy FOREIGN KEY (ReviewedBy)  REFERENCES Users(Id),
    CONSTRAINT CHK_LeaveApplications_Status    CHECK (Status IN ('Pending', 'Approved', 'Rejected', 'Cancelled')),
    CONSTRAINT CHK_LeaveApplications_Dates     CHECK (EndDate >= StartDate)
);

-- 6. NotificationLogs (written by the Azure Function after each email)
CREATE TABLE NotificationLogs (
    Id                  INT IDENTITY(1,1) PRIMARY KEY,
    LeaveApplicationId  INT             NOT NULL,
    RecipientEmail      NVARCHAR(256)   NOT NULL,
    Subject             NVARCHAR(500)   NOT NULL,
    Status              NVARCHAR(20)    NOT NULL,
    ErrorMessage        NVARCHAR(2000)  NULL,
    SentAt              DATETIME2       NOT NULL DEFAULT GETUTCDATE(),

    CONSTRAINT FK_NotificationLogs_LeaveApplications FOREIGN KEY (LeaveApplicationId) REFERENCES LeaveApplications(Id),
    CONSTRAINT CHK_NotificationLogs_Status CHECK (Status IN ('Sent', 'Failed'))
);

-- 7. Messages (SignalR chat history)
CREATE TABLE Messages (
    Id          INT IDENTITY(1,1) PRIMARY KEY,
    SenderId    INT             NOT NULL,
    ReceiverId  INT             NOT NULL,
    Content     NVARCHAR(2000)  NOT NULL,
    SentAt      DATETIME2       NOT NULL DEFAULT GETUTCDATE(),
    IsRead      BIT             NOT NULL DEFAULT 0,

    CONSTRAINT FK_Messages_Sender   FOREIGN KEY (SenderId)   REFERENCES Users(Id),
    CONSTRAINT FK_Messages_Receiver FOREIGN KEY (ReceiverId) REFERENCES Users(Id)
);

-- Indexes (make common queries faster)
CREATE INDEX IX_LeaveApplications_UserId ON LeaveApplications(UserId);
CREATE INDEX IX_LeaveApplications_Status ON LeaveApplications(Status);
CREATE INDEX IX_LeaveBalances_UserId_Year ON LeaveBalances(UserId, Year);
CREATE INDEX IX_NotificationLogs_LeaveApplicationId ON NotificationLogs(LeaveApplicationId);

-- Seed data (users are created later through the API's register endpoint, so passwords get hashed)
INSERT INTO Departments (Name) VALUES
('Engineering'), ('Human Resources'), ('Finance'), ('Operations');

INSERT INTO LeaveTypes (Name, DefaultDays, Description) VALUES
('Sick Leave',   12, 'For medical illness or health-related absence'),
('Casual Leave', 12, 'For personal or casual reasons'),
('Earned Leave', 15, 'Accrued based on service - planned vacations');
