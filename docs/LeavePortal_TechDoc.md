# LeavePortal — Technical Document

## How to Use This Document
This is the single source of truth for the LeavePortal project.
At the start of every new chat, upload this file and the DatabaseSchema file and write:
"I am building LeavePortal. Here are my two documents — TechDoc and DatabaseSchema.
Continue as my technical lead from where we left off."
Update the "Current Status" and "Session History" sections at the end of every session.

---

## Project Overview
**Project Name:** LeavePortal
**Type:** Employee Leave Management System
**Purpose:** Learning enterprise architecture, interview prep, portfolio, foundation for future projects
**Status:** Days 1–4 complete — Day 5 (Notification module) next

---

## Goals
- Understand how enterprise-level projects are structured
- Learn how individual Azure services connect and work together in a real system
- See Clean Architecture in practice — not just in theory
- Be able to explain every decision in an interview with business reasoning

---

## Tech Stack

### Backend
| Technology | Version | Reason |
|---|---|---|
| ASP.NET Core Web API | .NET 10 LTS | Latest LTS release, already installed, enterprise standard on Microsoft stack. Even-numbered .NET releases are always LTS. |
| Clean Architecture | — | Separates concerns, supports large teams, industry standard in .NET enterprise |
| Service layer | — | Controllers call plain service classes (`IAuthService`, `ILeaveService`) directly. Chosen over MediatR/CQRS after initially building with it — simpler to read and debug for a project this size |
| DataAnnotations | — | Request-DTO validation via `[Required]`/`[EmailAddress]`/`[Range]` etc., auto-enforced by `[ApiController]`. Replaced FluentValidation — easier to debug, less indirection |
| Entity Framework Core | Latest | ORM for data access — Database First approach |
| JWT Authentication | — | Via HttpOnly Cookies — enterprise browser app standard |
| Role-Based Authorization | — | Two roles: Employee and Manager |

### EF Core Approach — Database First
Database is the source of truth, NOT C# code.
- Design all tables directly in Azure SQL
- Run `dotnet ef dbcontext scaffold` to generate C# entity classes automatically
- No migration files — schema changes happen in the database directly, then re-scaffold
- This matches how enterprise teams with DBAs or existing databases work
- Also aligns with Flyway/Liquibase approach where SQL is the source of truth

### Azure Services
| Service | Tier | Purpose |
|---|---|---|
| Azure SQL Database | Basic (5 DTU) — free | Main relational database |
| Azure Web App | F1 — free | Hosts the .NET API |
| Azure Functions | Consumption Plan — free (1M executions/month) | Email notification engine |
| Azure Service Bus | Basic — ~free at dev scale | Bridge between API and Azure Function |
| Azure Blob Storage | Free tier | Document/file uploads (medical certificates etc.) |

### Email
- **Now:** Gmail SMTP — works in 5 minutes, no approval needed, use App Password
- **Later:** Swap to SendGrid when account gets approved — only 5 minute change inside Azure Function

### Frontend
| Technology | Reason |
|---|---|
| React + Vite | Standard, fast dev server, modern setup |
| React Router | Page navigation |
| React Query (TanStack Query) | All API calls — handles caching, loading, error state. Modern enterprise standard for server state |
| Zustand | Auth state (user info, role, isLoggedIn) — enterprise standard for global client state |
| Bootstrap 5 | Styling — clean, professional UI without a template. Fully custom, fully explainable |

**Frontend build approach:**
- UI is built from scratch using Bootstrap 5 — no paid templates
- Looks professional, every component is understood and explainable in interviews
- After each page is built, a short "explain this to me" review session happens for learning

### Why Zustand for Auth State (not Context API)
Context API re-renders every component that consumes it whenever the value changes.
In a small app it doesn't matter. In enterprise apps with dozens of components consuming
auth state, this causes unnecessary re-renders and performance issues.

Zustand does not have this problem — components only re-render when the specific piece
of state they subscribe to changes. This is why modern enterprise React apps use Zustand.

Learning curve: one day. Extremely simple API — just a store and you read/write directly.
No actions, no reducers, no dispatch, no Provider wrapping.

### State Management — Full Enterprise Picture (2025)
| What | Tool | Why |
|---|---|---|
| Auth state (user info, role, isLoggedIn) | Zustand | Simple, no re-render issues, enterprise standard |
| Server data (API calls, leave list etc.) | React Query | Caching, loading, error handling built in |
| Complex UI state across many components | Zustand | Same store, different slices |
| Old large codebases pre-2022 | Redux Toolkit | Already there, not worth rewriting |

**NOT using:**
- Redux — older codebases use Redux Toolkit, new projects use React Query + Zustand
- Context API — fine for tiny projects, not enterprise grade for auth state

### Source Control
- GitHub only
- No CI/CD pipeline
- Manual deployment

**Branch Strategy:**
| Branch | Purpose |
|---|---|
| `main` | Stable baseline — merge into here when features are complete |
| `backend` | All .NET backend work — API, Core, Infrastructure, Functions |
| `frontend` | All React frontend work |

---

## Architecture Decisions & Why

### 1. HttpOnly Cookies for JWT (not localStorage)
- localStorage is readable by any JavaScript — vulnerable to XSS attacks
- HttpOnly Cookie means JavaScript cannot read the token at all
- Browser sends it automatically with every request
- This is what enterprise browser applications actually use
- Introduces CSRF concern — handled with SameSite=Strict cookie policy
- Note: API-first systems with mobile clients use Bearer tokens instead — cookies don't work cleanly across mobile/cross-domain

### 2. Azure Service Bus between API and Azure Function
- API should NOT directly call "send email"
- If email service is slow or down, your API would hang
- Instead: API puts a message on Service Bus queue and immediately returns 200
- Azure Function independently picks up the message and sends the email
- This is called async decoupled architecture — standard in every serious enterprise system
- Benefit: email provider can be swapped, email service can go down — main API is unaffected

### 3. Service Layer (originally MediatR / CQRS)
- Controller receives request → calls a service method (e.g. `_leaveService.ApplyAsync(...)`) → the service holds the business logic
- **Originally** built with MediatR/CQRS (Commands / Queries / Handlers) + FluentValidation; **deliberately refactored** to a plain service layer + DataAnnotations
- **Why changed:** for a solo project this size, MediatR's indirection made debugging harder (`_mediator.Send()` hides where the logic actually lives) for little payoff. A service layer is equally common in enterprise .NET and far easier to step through.
- **Validation now:** DataAnnotations on request DTOs (auto `400` via `[ApiController]`) + plain business-rule checks inside the services.
- **Interview framing:** "I used MediatR + FluentValidation, then refactored to a service layer + DataAnnotations for debuggability and simplicity" — demonstrates judgment, not just pattern-copying.

### 4. Clean Architecture — 4 Projects
- Separation of concerns — junior dev cannot accidentally write DB code in a controller
- Each layer has one job and one direction of dependency
- Industry standard for teams of any size

### 5. Database First over Code First
- No migration files = no migration conflict headaches
- Schema designed intentionally, not generated from C# assumptions
- Re-scaffold when schema changes — clean and explicit
- In large enterprise: teams use Flyway/Liquibase (plain numbered SQL files) — Database First mindset aligns with this

### 6. Migration Strategy (for interviews)
- EF Core Code First migrations cause conflicts when 100+ developers work on separate branches
- ModelSnapshot file is touched by every migration — massive merge conflict risk
- **What big companies actually use:** Flyway or Liquibase
  - Plain SQL files: V1__initial_schema.sql, V2__add_column.sql
  - Tool tracks which files applied in a schema history table inside the DB
  - No C# magic, readable by any developer or DBA
  - No merge conflicts — each developer writes a new numbered file
- Know this for interviews even though we use Database First scaffolding

---

## Solution Structure

```
LeavePortal/                          ← GitHub repo root
├── backend/
│   ├── LeavePortal.sln               ← Solution file
│   ├── LeavePortal.API/              ← Controllers only, no business logic
│   ├── LeavePortal.Core/             ← Interfaces, domain models, DTOs — no external dependencies
│   ├── LeavePortal.Infrastructure/   ← EF Core DbContext, Service Bus publisher, Blob Storage client
│   └── LeavePortal.Functions/        ← Azure Function — email sender, deploys separately
├── frontend/                         ← React app
└── docs/                             ← Any documentation files
```

### Project Dependencies (who depends on who)
```
LeavePortal.API            → depends on Core + Infrastructure
LeavePortal.Infrastructure → depends on Core
LeavePortal.Core           → depends on NOTHING (pure domain)
LeavePortal.Functions      → depends on Core
```

---

## Modules

| Module | Description | Status |
|---|---|---|
| Auth | Register, Login, JWT issued via HttpOnly Cookie, role assigned | ✅ Done (Day 2) |
| Leave Application | Employee submits leave form, optional document upload to Blob | ✅ Done (Day 3) — apply, view, cancel |
| Leave Approval | Manager approves/rejects, comment added, notification triggered | ✅ Done (Day 4) — approve, reject, pending queue |
| Notification | Azure Function listens to Service Bus, sends email via Gmail SMTP | 🔶 In Progress — Service Bus publisher side built; Azure Function listener pending (Day 5) |
| Leave Balance | Tracks total/used/remaining days per employee per year | ❌ Not Started |
| Document | Upload to Blob Storage, URL saved in DB, retrievable later | ❌ Not Started |

---

## End-to-End Flow

### Employee submits leave:
1. Employee logs in → React sends credentials to .NET API
2. API validates → returns JWT via HttpOnly Cookie → React stores user info in Zustand store
3. Employee fills leave form (leave type, dates, reason, optional document)
4. React calls API — cookie automatically sent by browser, React Query handles the call
5. API validates request via FluentValidation
6. API saves leave application to Azure SQL via EF Core
7. API uploads document to Azure Blob Storage → saves URL in DB
8. API publishes message to Azure Service Bus → returns 200 to React
9. Azure Function wakes up → reads message from Service Bus
10. Function sends email to Manager: "New leave request pending your review"

### Manager approves/rejects:
1. Manager logs in → Zustand stores manager role → React Query fetches pending requests
2. Manager clicks Approve or Reject with comment
3. React calls API → API updates status in Azure SQL
4. API publishes new message to Service Bus
5. Azure Function sends email to Employee: "Your leave has been approved/rejected"

---

## Azure Resources to Create

| Resource | Name Suggestion | Tier | When Needed |
|---|---|---|---|
| Resource Group | leaveportal-rg | Free | Day 1 — before everything |
| Azure SQL Server | leaveportal-sqlserver | — | Before DB design |
| Azure SQL Database | leaveportal-db | Basic (free) | Before scaffolding |
| Azure Web App | leaveportal-api | F1 (free) | Before first deployment |
| Azure Functions App | leaveportal-functions | Consumption (free) | Before function deployment |
| Azure Service Bus Namespace | leaveportal-bus | Basic (~free) | Before notification work |
| Azure Blob Storage Account | leaveportalstorage | Free tier | Before document upload work |

---

## Environment Variables / Config Keys Needed

```
# Azure SQL
ConnectionStrings__DefaultConnection = "Server=...;Database=leaveportal-db;..."

# Azure Service Bus
ServiceBus__ConnectionString = "Endpoint=sb://leaveportal-bus.servicebus.windows.net/..."
ServiceBus__QueueName = "leave-notifications"

# Azure Blob Storage
BlobStorage__ConnectionString = "DefaultEndpointsProtocol=https;AccountName=..."
BlobStorage__ContainerName = "leave-documents"

# JWT / Cookie settings
Jwt__Issuer = "leaveportal"
Jwt__Audience = "leaveportal-users"
Jwt__SecretKey = "your-secret-key-here"

# Gmail SMTP (for Azure Function)
Email__SmtpHost = "smtp.gmail.com"
Email__SmtpPort = "587"
Email__SenderEmail = "your-gmail@gmail.com"
Email__AppPassword = "your-google-app-password"
```

---

## Local Environment — Verified Ready

| Tool | Version | Status |
|---|---|---|
| .NET SDK | 10.0.300 (LTS) | ✅ Installed |
| Visual Studio | 2022 + 2026 | ✅ Installed |
| Node.js | v22.13.1 | ✅ Installed |
| Git | — | ✅ Installed |
| Azure CLI | 2.50.0 | ✅ Installed |

---

## Day 1 Plan — Not Started Yet

Order of operations for Day 1:
1. Create GitHub repo named `LeavePortal`
2. Clone to `D:\LeavesPortal\LeavePortal`
3. Create folder structure: backend, frontend, docs
4. Create .NET solution: `dotnet new sln -n LeavePortal` inside backend folder
5. Create 4 projects (API, Core, Infrastructure, Functions)
6. Add all projects to solution
7. Add project references (dependency direction)
8. Verify build: `dotnet build LeavePortal.sln`
9. Open in Visual Studio
10. Create Azure SQL Database in Azure Portal
11. Design all 6 tables in Azure SQL
12. Scaffold entities: `dotnet ef dbcontext scaffold`

---

## 14-Day Build Plan

| Day | Branch | Focus |
|---|---|---|
| 1 | backend | GitHub repo, solution setup, 4 projects, Azure SQL created, tables designed, entities scaffolded |
| 2 | backend | Auth module — Register, Login, JWT HttpOnly Cookie, roles |
| 3 | backend | Leave Application module — API endpoints, Service Bus publisher |
| 4 | backend | Leave Approval module — Manager endpoints, status updates |
| 5 | backend | Notification module — Azure Function, Service Bus listener, Gmail SMTP |
| 6 | backend | Leave Balance module — deduction logic on approval |
| 7 | backend | Document module — Blob Storage upload, URL stored in DB |
| 8 | frontend | React + Vite setup, React Router, Zustand auth store, React Query config, Login page |
| 9 | frontend | Auth flow — protected routes, role-based routing, Zustand wired to API |
| 10 | frontend | Employee Dashboard — apply for leave form, leave history, React Query calls |
| 11 | frontend | Manager Dashboard — pending requests list, approve/reject with comment |
| 12 | both | Deploy API to Azure Web App, deploy Azure Functions |
| 13 | both | Connect frontend to deployed API, test full end-to-end flow |
| 14 | both | Bug fixes, cleanup, final documentation update |

---

## Current Status
**Phase:** Day 5 in progress — Milestone 1 done (API publishes to Service Bus). Architecture refactored to service layer + DataAnnotations.
**Last Updated:** Session 7
**Done so far:**
- Day 1 — GitHub repo, solution, 4 projects, Azure SQL created, 6 tables designed, entities scaffolded
- Day 2 — Auth module (Register, Login, JWT via HttpOnly Cookie, roles)
- Day 3 — Leave Application module (apply, view, cancel)
- Day 4 — Leave Approval module (manager approve/reject, pending queue)
- Re-pointed to a new Azure SQL server and re-scaffolded the DbContext
- Day 5 Milestone 1 — real Azure Service Bus publisher wired in; apply/cancel fan out to department managers, approve/reject notify the employee; verified messages land in the `leave-notifications` queue
- **Refactor** — removed MediatR/CQRS + FluentValidation; replaced with a service layer (`IAuthService`/`AuthService`, `ILeaveService`/`LeaveService`) + DataAnnotations validation

**Next Step:** Day 5 Milestone 2 — Azure Function listening to Service Bus, sending email via Gmail SMTP, writing `NotificationLogs`
**Deferred (later):** in-app notification feed in the React portal (~Day 9–10) — email-only for now
---

## Session History

### Session 1
- Decided project: Employee Leave Management System — named LeavePortal
- Decided full tech stack: .NET, React, Azure
- Discussed enterprise architecture patterns
- Chose HttpOnly Cookies over localStorage for JWT
- Chose Service Bus + Azure Function for async notification
- Chose MediatR for CQRS pattern
- Discussed Clean Architecture — 4 project structure

### Session 2
- Verified local environment — all tools installed
- Discussed EF Core migration conflicts in large teams
- Discussed Flyway and Liquibase — what big companies use
- Discussed Code First vs Database First
- Discussed Redux vs Zustand vs React Query — modern enterprise picture
- Full Day 1 instructions written and ready

### Session 3
- Changed .NET 8 to .NET 10 LTS (confirmed .NET 10 IS LTS — even-numbered releases are always LTS)
- Changed EF Core Code First to Database First
- Recreated both documents fresh with all corrections

### Session 4
- Replaced Context API with Zustand for auth state
- Reason: Context API causes unnecessary re-renders in enterprise apps — Zustand is the correct enterprise standard
- Full state management picture confirmed: Zustand for client state, React Query for server state
- Both documents recreated with all corrections applied
- Ready to start Day 1

### Session 5

Day 1 started
GitHub repo created: https://github.com/sam777manss/LeavePortal.git
Cloned to D:\LeavesPortal\LeavePortal
Created folder structure: backend, frontend, docs
Created .NET solution: LeavePortal.slnx (new .NET 10 format)
Created 4 projects: API, Core, Infrastructure, Functions all on net10.0
Fixed Functions project — was created loose in backend folder, moved into its own subfolder
Added all 4 projects to solution
Added all project references — dependency direction correct
Build succeeded — 0 errors across all 4 projects
Next: Open Visual Studio → clean up default generated files → create Azure SQL Database in Azure Portal

### Session 6
- Day 1 finished — Azure SQL Database created, all 6 tables designed in Azure SQL, entities scaffolded into LeavePortal.Infrastructure/Entities
- Re-pointed to a new Azure SQL server and re-scaffolded the DbContext
- Database First lesson learned: recreate the DB completely before re-scaffolding, otherwise scaffold drops navigation properties
- Day 2 — Auth module: Register, Login, JWT issued via HttpOnly Cookie, role-based authorization, built on MediatR (CQRS) + FluentValidation, with a ValidationBehavior pipeline
- Day 3 — Leave Application module: apply for leave, view my leaves, view single leave, cancel leave (commands, queries, handlers, validators, LeaveController)
- Day 4 — Leave Approval module: manager approve/reject with comment, pending-requests queue (ApproveLeave, RejectLeave, GetPendingLeaves)
- Service Bus publisher side built and wired (IServiceBusPublisher, ServiceBusPublisher, LeaveNotificationMessage) ready for the Day 5 Azure Function
- Updated both documents (TechDoc + DatabaseSchema) to reflect actual progress through Day 4
- Next: Day 5 — Notification module (Azure Function + Service Bus listener + Gmail SMTP + NotificationLogs)

### Session 7
- Created Azure Service Bus namespace `leaveportal-bus` (Basic) + queue `leave-notifications`; stored connection string in API user-secrets
- Generated a Gmail App Password (for the Day 5 Azure Function — not used yet)
- Day 5 Milestone 1 — swapped the stub publisher for the real `Azure.Messaging.ServiceBus` implementation; `ServiceBusClient` registered as a singleton from user-secrets; apply/cancel fan out one message per department manager, approve/reject notify the employee. Verified 2 messages land in the queue on apply (department has 2 managers)
- Decided notification routing (kept simple/less noisy): apply/cancel → dept managers; approve/reject → the employee. In-app feed deferred to ~Day 9–10
- **Refactor (deliberate):** removed MediatR/CQRS + FluentValidation across Auth + Leave. Replaced with a service layer (`IAuthService`/`AuthService`, `ILeaveService`/`LeaveService`) injected into controllers, and DataAnnotations on request DTOs + business-rule checks in the services. Deleted all Commands/Queries/Handlers/Validators/Behaviors. Reason: easier to debug and read for a solo project; build green, behavior preserved
- Next: Day 5 Milestone 2 — the Azure Function (Service Bus trigger → email via MailKit → write NotificationLogs)