# LeavePortal — Database Schema

## Overview
- **Database:** Azure SQL Database (Basic tier — free)
- **Approach:** Database First — tables designed here, EF Core scaffolds C# entities from the live DB
- **Tables:** 6 total
- Schema changes are made directly in Azure SQL, then entities are re-scaffolded

---

## Scaffold Command (run after tables are created or changed)
Run from Visual Studio → Tools → NuGet Package Manager → Package Manager Console.
Replace YOUR_PASSWORD with the real password (do NOT commit it).

```bash
dotnet ef dbcontext scaffold "Server=leaveportal-sqlserver.database.windows.net;Database=leaveportal-db;User Id=leaveportaladmin;Password=YOUR_PASSWORD;TrustServerCertificate=True;" Microsoft.EntityFrameworkCore.SqlServer --output-dir Entities --context-dir Data --context LeavePortalDbContext --force --no-onconfiguring --project LeavePortal.Infrastructure\LeavePortal.Infrastructure.csproj
```

**Critical flags learned in Session 7:**
- `--no-onconfiguring` — STOPS scaffold from hardcoding the connection string (with password) into the DbContext. Without this, every re-scaffold leaks the password back into source code.
- `--force` — overwrites existing generated files when re-scaffolding after schema changes.
- `--project ...csproj` — required because `cd` does not change directory inside Package Manager Console.

The connection string is NOT in the DbContext. It lives in appsettings.json (placeholder) /
appsettings.Development.json (real, gitignored) and is wired up in Program.cs via DI.

---

## Tables

---

### 1. Departments
Stores company departments. Used to group employees.

```sql
CREATE TABLE Departments (
    Id          INT IDENTITY(1,1) PRIMARY KEY,
    Name        NVARCHAR(100)   NOT NULL,
    CreatedAt   DATETIME2       NOT NULL DEFAULT GETUTCDATE()
);
```

| Column | Type | Notes |
|---|---|---|
| Id | INT, PK, Identity | Auto-increment primary key |
| Name | NVARCHAR(100), NOT NULL | Department name e.g. Engineering, HR |
| CreatedAt | DATETIME2, NOT NULL | When record was created — always store UTC |

---

### 2. Users
Stores all system users — both Employees and Managers.

```sql
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
```

| Column | Type | Notes |
|---|---|---|
| Id | INT, PK, Identity | Auto-increment primary key |
| FullName | NVARCHAR(150), NOT NULL | Display name |
| Email | NVARCHAR(256), NOT NULL, UNIQUE | Login identifier — unique constraint prevents duplicate accounts |
| PasswordHash | NVARCHAR(512), NOT NULL | Never store plain text — BCrypt hash stored here |
| Role | NVARCHAR(20), NOT NULL | 'Employee' or 'Manager' — CHECK constraint enforces only these two values |
| DepartmentId | INT, NOT NULL, FK | Which department this user belongs to |
| IsActive | BIT, NOT NULL, Default 1 | Soft delete — deactivate users instead of hard deleting |
| CreatedAt | DATETIME2, NOT NULL | When user registered |

**Why UNIQUE on Email:** Prevents two accounts with same email. Database enforces this even if application code has a bug.

**Why CHECK on Role:** Database-level enforcement — only valid role values can be inserted. Application cannot accidentally insert 'Admin' or anything else.

**Why IsActive instead of DELETE:** In enterprise you never hard delete users — it breaks audit trails. Old leave applications would lose their user reference. Deactivate instead.

---

### 3. LeaveTypes
Defines types of leave available in the company.

```sql
CREATE TABLE LeaveTypes (
    Id              INT IDENTITY(1,1) PRIMARY KEY,
    Name            NVARCHAR(100)   NOT NULL,
    DefaultDays     INT             NOT NULL,
    Description     NVARCHAR(500)   NULL,
    IsActive        BIT             NOT NULL DEFAULT 1,

    CONSTRAINT UQ_LeaveTypes_Name UNIQUE (Name)
);
```

| Column | Type | Notes |
|---|---|---|
| Id | INT, PK, Identity | Auto-increment primary key |
| Name | NVARCHAR(100), NOT NULL, UNIQUE | e.g. 'Sick Leave', 'Casual Leave', 'Earned Leave' |
| DefaultDays | INT, NOT NULL | How many days of this type are given per year |
| Description | NVARCHAR(500), NULL | Optional explanation of the leave type |
| IsActive | BIT, NOT NULL | Deactivate leave types without deleting historical records |

---

### 4. LeaveBalances
Tracks each employee's leave balance per leave type per year.

```sql
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
```

| Column | Type | Notes |
|---|---|---|
| Id | INT, PK, Identity | Auto-increment primary key |
| UserId | INT, NOT NULL, FK | Which employee this balance belongs to |
| LeaveTypeId | INT, NOT NULL, FK | Which leave type (Sick, Casual, Earned) |
| Year | INT, NOT NULL | Which year e.g. 2025 — balances reset yearly |
| TotalDays | INT, NOT NULL | Total days allocated for this year |
| UsedDays | INT, NOT NULL, Default 0 | Days consumed so far |
| RemainingDays | Computed Column | TotalDays - UsedDays — automatically calculated by DB, never manually set |

**Why UNIQUE on (UserId, LeaveTypeId, Year) — Composite Unique Constraint:**
The combination of all three columns must be unique together.
- John + Sick Leave + 2025 → allowed once, blocked on second insert
- John + Sick Leave + 2026 → allowed (different year)
- John + Casual Leave + 2025 → allowed (different leave type)
- Sarah + Sick Leave + 2025 → allowed (different user)

Without this: a bug or race condition could insert two rows for same user/type/year.
Balance calculations would be wrong and you may never notice until production.
Database enforces this as a hard rule regardless of what the application does.

**Why Computed Column for RemainingDays:**
Never store data that can be derived from other columns. If stored as a regular column,
it would need updating every time TotalDays or UsedDays changes — and they could go out
of sync. Computed column is always mathematically correct automatically.

---

### 5. LeaveApplications
The core table. Every leave request submitted by an employee.

```sql
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
```

| Column | Type | Notes |
|---|---|---|
| Id | INT, PK, Identity | Auto-increment primary key |
| UserId | INT, NOT NULL, FK | Employee who applied |
| LeaveTypeId | INT, NOT NULL, FK | Type of leave requested |
| StartDate | DATE, NOT NULL | Leave start |
| EndDate | DATE, NOT NULL | Leave end |
| TotalDays | INT, NOT NULL | Number of days (EndDate - StartDate + 1) |
| Reason | NVARCHAR(1000), NOT NULL | Employee's reason |
| DocumentUrl | NVARCHAR(2000), NULL | Azure Blob URL of uploaded document — NULL if no document |
| Status | NVARCHAR(20), NOT NULL | 'Pending', 'Approved', 'Rejected', 'Cancelled' |
| ReviewedBy | INT, NULL, FK | Manager UserId who took action — NULL until reviewed |
| ReviewComment | NVARCHAR(1000), NULL | Manager's comment — NULL until reviewed |
| ReviewedAt | DATETIME2, NULL | When manager took action — NULL until reviewed |
| CreatedAt | DATETIME2, NOT NULL | When application was submitted |
| UpdatedAt | DATETIME2, NOT NULL | Last update timestamp — update on every change |

**Why CHECK on Status:** Prevents invalid status values. Database rejects anything not in the allowed list.

**Why CHECK on Dates:** EndDate must be >= StartDate. Prevents invalid date ranges at database level.

**Why store DocumentUrl and not the file:** Never store binary files in a relational database. Files go to Azure Blob Storage. Only the URL is stored here — cheap, fast, scalable.

**Why UpdatedAt:** Audit trail — always know when a record last changed. Useful for debugging and syncing data to other systems.

---

### 6. NotificationLogs
Audit log of every notification sent by the Azure Function.

```sql
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
```

| Column | Type | Notes |
|---|---|---|
| Id | INT, PK, Identity | Auto-increment primary key |
| LeaveApplicationId | INT, NOT NULL, FK | Which leave application this notification is for |
| RecipientEmail | NVARCHAR(256), NOT NULL | Who received the email |
| Subject | NVARCHAR(500), NOT NULL | Email subject line |
| Status | NVARCHAR(20), NOT NULL | 'Sent' or 'Failed' |
| ErrorMessage | NVARCHAR(2000), NULL | If failed, what the error was — essential for debugging |
| SentAt | DATETIME2, NOT NULL | When the notification attempt was made |

**Why this table exists:** Azure Function is a separate service. If it fails silently you would never know emails weren't sent. This log gives full visibility — query it to see if notifications are working or failing and why.

---

## Relationships Diagram

```
Departments
    └── Users (many employees per department)
            ├── LeaveBalances (one row per user per leave type per year)
            ├── LeaveApplications as applicant (one employee, many applications)
            │       └── NotificationLogs (one application, multiple notifications)
            └── LeaveApplications.ReviewedBy (manager who reviewed)

LeaveTypes
    ├── LeaveBalances (what type of leave is this balance for)
    └── LeaveApplications (what type of leave was requested)
```

---

## Indexes to Create (after table creation)

```sql
-- Speed up leave application queries by employee
CREATE INDEX IX_LeaveApplications_UserId ON LeaveApplications(UserId);

-- Speed up manager's view of pending applications
CREATE INDEX IX_LeaveApplications_Status ON LeaveApplications(Status);

-- Speed up balance lookup (most common query)
CREATE INDEX IX_LeaveBalances_UserId_Year ON LeaveBalances(UserId, Year);

-- Speed up notification log queries by application
CREATE INDEX IX_NotificationLogs_LeaveApplicationId ON NotificationLogs(LeaveApplicationId);
```

---

## Seed Data (insert after tables created — for development)

```sql
-- Departments
INSERT INTO Departments (Name) VALUES
('Engineering'),
('Human Resources'),
('Finance'),
('Operations');

-- Leave Types
INSERT INTO LeaveTypes (Name, DefaultDays, Description) VALUES
('Sick Leave',   12, 'For medical illness or health-related absence'),
('Casual Leave', 12, 'For personal or casual reasons'),
('Earned Leave', 15, 'Accrued based on service — planned vacations');

-- Manager user (replace hash with real BCrypt hash of 'Manager@123')
INSERT INTO Users (FullName, Email, PasswordHash, Role, DepartmentId) VALUES
('Rajesh Sharma', 'manager@leaveportal.com',
 '$2a$11$examplehashformanager', 'Manager', 1);

-- Employee user (replace hash with real BCrypt hash of 'Employee@123')
INSERT INTO Users (FullName, Email, PasswordHash, Role, DepartmentId) VALUES
('Sameer Mansuri', 'employee@leaveportal.com',
 '$2a$11$examplehashforemployee', 'Employee', 1);

-- Leave Balances for 2025 (UserId 2 = Employee — adjust after actual insert)
INSERT INTO LeaveBalances (UserId, LeaveTypeId, Year, TotalDays) VALUES
(2, 1, 2025, 12),
(2, 2, 2025, 12),
(2, 3, 2025, 15);
```

**Note:** Replace PasswordHash values with actual BCrypt hashes generated in code. Never insert plain text passwords.

---

## C# Enums (create in Core project after scaffolding)

After scaffolding, EF generates string properties for Status and Role.
Create these enums in `LeavePortal.Core/Enums/` and use them in service logic:

```csharp
// LeavePortal.Core/Enums/UserRole.cs
public enum UserRole
{
    Employee,
    Manager
}

// LeavePortal.Core/Enums/LeaveStatus.cs
public enum LeaveStatus
{
    Pending,
    Approved,
    Rejected,
    Cancelled
}

// LeavePortal.Core/Enums/NotificationStatus.cs
public enum NotificationStatus
{
    Sent,
    Failed
}
```

---

## Schema Change Process (Database First workflow)

When you need to add or change something in the database:

1. Write the ALTER TABLE or new CREATE TABLE SQL
2. Run it directly in Azure SQL via SSMS
3. Run the scaffold command again with `--force` to regenerate entities
4. Update service logic to use the new properties
5. Commit everything to GitHub

No migration files. Schema is the source of truth.

---

## Current Schema Status

| Table | Created in Azure SQL | Entities Scaffolded |
|---|---|---|
| Departments | ✅ Created | ✅ Scaffolded |
| Users | ✅ Created | ✅ Scaffolded |
| LeaveTypes | ✅ Created | ✅ Scaffolded |
| LeaveBalances | ✅ Created | ✅ Scaffolded |
| LeaveApplications | ✅ Created | ✅ Scaffolded |
| NotificationLogs | ✅ Created | ✅ Scaffolded |
