# LeavePortal: Azure Rebuild Guide

How to recreate every Azure service for LeavePortal from zero, in order.
Region for everything: **Central India**. Resource group: **leaveportal-rg**.

| # | Service | Name | Used by |
|---|---|---|---|
| 1 | Resource Group | leaveportal-rg | Folder for all services |
| 2 | Azure SQL Database | leaveportal-db (server: leaveportal-sqlserver3) | API (EF Core) + Function |
| 3 | Storage Account | (next) | API: leave documents (Blob) |
| 4 | Service Bus | (next) | API sends, Function receives |
| 5 | Function App | (next) | Sends emails |
| 6 | App Service | (next) | Hosts API + React |

---

## Step 0: Subscription + budget
1. Portal → **Subscriptions** → expired subscription → **Upgrade** → Pay-As-You-Go.
2. **Cost Management** → **Budgets** → **+ Add** → $6/month, alert at 80% to email.

**Why:** Pay-As-You-Go keeps the free tiers. The budget alert warns you before any real cost.

## Step 1: Resource Group
Portal → **Resource groups** → **+ Create** → name `leaveportal-rg`, region Central India.

**Why:** one folder for all services. Deleting it deletes everything inside.

## Step 2: Azure SQL Database
1. Portal → **SQL databases** → **+ Create** → **Apply offer** (free monthly database).
2. Database `leaveportal-db` (**note:** with the free offer Azure actually named it `free-sql-db-2358031` — check the real name in SSMS/portal and use it as `Initial Catalog`); new server `leaveportal-sqlserver3`, SQL authentication (admin login + password kept private).
3. Free limit behavior: **Auto-pause until next month**.
4. Networking: Public endpoint, **Allow Azure services = Yes**, **Add current client IP = Yes**.

**Why:** the server = login + firewall; the database = the tables. "Allow Azure services" lets App Service and the Function connect.

## Step 2.1: Create the tables
1. Open the database → **Query editor (preview)** → log in with the SQL admin.
2. Paste all of [01_create_tables.sql](01_create_tables.sql) → **Run**.

Creates 7 tables (Departments, Users, LeaveTypes, LeaveBalances, LeaveApplications, NotificationLogs, Messages) + indexes + seed data.

Alternative: run the same script from **SSMS** on your laptop. Server = `leaveportal-sqlserver3.database.windows.net`, SQL Server Authentication, User Name = the **Server admin login** shown on the server's Overview page (here it was accidentally set to `leaveportal-sqlserver2.database.windows.net`), Encrypt = Mandatory + Trust server certificate. Error 18456 = wrong login or password.

**Why Database First:** the tables are designed in SQL first; EF Core then generates C# classes from them (`Infrastructure/Entities`). The tables were created to match the existing classes exactly, so no re-scaffold is needed.

## Step 3: Storage Account (Blob for leave documents)
1. Portal → **Storage accounts** → **+ Create**.
2. Resource group `leaveportal-rg`, name `leaveportalstorage` (lowercase, globally unique; add numbers if taken), region Central India.
3. Primary service: **Azure Blob Storage**; Performance **Standard**; Redundancy **LRS** (cheapest).
4. **Review + create** → **Create**.
5. Open it → **Containers** → **+ Container** → name `leave-documents`, access level **Private**.
6. **Access keys** → copy **Connection string** (key1). Keep it private; it goes into app settings later.

**Why:** files never go into SQL. The file goes into Blob; only its URL is saved in `LeaveApplications.DocumentUrl`. Private container = nobody can open a file by URL; the API checks owner-or-manager and streams it (`GET /api/leave/{id}/document`).

## Step 4: Service Bus (message queue)
1. Portal → **Service Bus** → **+ Create**.
2. Resource group `leaveportal-rg`, namespace `leaveportal-bus` (globally unique; add numbers if taken), region Central India, pricing tier **Basic** (queues only, cheapest).
3. **Review + create** → **Create**.
4. Open it → **Queues** → **+ Queue** → name `leave-notifications` (exact name; it is hardcoded in the code) → defaults → **Create**.
5. **Shared access policies** → **RootManageSharedAccessKey** → copy **Primary connection string**. Keep private.

**Why:** when a leave is applied/approved/rejected, the API drops a small message in the queue and returns immediately. The Function picks it up and sends the email. If email is slow or down, the user never waits and nothing is lost (messages stay in the queue and retry). This is **asynchronous, decoupled** communication.

## Step 5: Function App (email sender)
1. Portal → **Function App** → **+ Create** → hosting plan **Consumption** (pay per run, ~free). If only **Flex Consumption** is offered, pick that.
2. Resource group `leaveportal-rg`, name `leaveportal-functions` (globally unique), runtime **.NET**, version **10 (isolated worker)**, region Central India, OS **Windows**.
3. **Storage** tab: use the existing `leaveportalstorage` (Functions need a storage account for their own bookkeeping).
4. **Monitoring** tab: Application Insights **Yes** → create new `leaveportal-insights`. (Logs + KQL queries; the TCS JD asks for this.)
5. **Review + create** → **Create**.

**Why:** the Function is a separate small program that wakes up only when a message lands in the queue (`ServiceBusTrigger`). It sends the email with MailKit (Gmail SMTP) and writes a row into `NotificationLogs`. Consumption = billed per execution, so idle cost is ~0.

Notes on Flex Consumption create screen: pick existing `leaveportalstorage`; the "no network access restrictions" and "diagnostic settings" messages are info only. Deployment tab: continuous deployment Disabled (publish from Visual Studio).

## Step 6: App Service (hosts API + React)
1. Portal → **App Services** → **+ Create** → **Web App**.
2. Resource group `leaveportal-rg`, name `leaveportal-api` (globally unique), Publish **Code**, Runtime **.NET 10**, OS **Windows**, region Central India.
3. Pricing plan: create new `leaveportal-plan`, tier **Free F1**.
4. **Monitoring** tab: Application Insights **Yes** → select existing `leaveportal-insights`.
5. **Review + create** → **Create**.
6. After create: **Configuration / Settings → General settings** → **Web sockets = On** (SignalR chat needs it) → Save.

**Why:** App Service runs the ASP.NET Core API. The React build is copied into the API's `wwwroot`, so one URL serves both. Same origin = the HttpOnly JWT cookie works without cross-site cookie problems.

## Step 7: Key Vault (store secrets)
1. Portal → **Key vaults** → **+ Create** → RG `leaveportal-rg`, name `leaveportal-kv` (globally unique), region Central India, tier **Standard**.
2. **Access configuration** tab: **Azure role-based access control (RBAC)** (default) → **Review + create** → **Create**.
3. Give yourself permission to add secrets: vault → **Access control (IAM)** → **+ Add role assignment** → role **Key Vault Secrets Officer** → member = your own account → Assign. (With RBAC, even the subscription owner can't add secrets until this is done.)
4. Vault → **Objects → Secrets** → **+ Generate/Import**, one per secret (names: letters, numbers, dashes only):

| Secret name | Value (where to get it) |
|---|---|
| `SqlConnection` | SQL database → **Connection strings** → **ADO.NET (SQL authentication)** tab (NOT the Entra passwordless one — that causes "Login failed for user '<token-identified principal>'"); replace `{your_password}`; `Initial Catalog` must be the real DB name |
| `BlobConnection` | Storage account → **Access keys** → key1 connection string |
| `ServiceBusConnection` | Service Bus → **Shared access policies** → RootManageSharedAccessKey → primary connection string |
| `JwtSecretKey` | Any long random string (32+ characters) |
| `EmailAppPassword` | Google account → Security → 2-Step Verification → **App passwords** |

**Why:** secrets in `appsettings.json` end up in Git (the repo is public), are visible to anyone with the code, and must be changed in many places when rotated. Key Vault keeps them in one secured, audited place.

## Step 8: Managed Identity + Key Vault references
**A. Turn on identity** (App Service `leaveportal-api` AND Function App `leaveportal-functions`):
Settings → **Identity** → **System assigned** tab → Status **On** → Save → Yes.

**B. Allow both identities to read secrets:**
Key vault → **Access control (IAM)** → **+ Add role assignment** → role **Key Vault Secrets User** → Assign access to **Managed identity** → **+ Select members** → pick App Service `leaveportal-api` and Function App `leaveportal-functions` → Review + assign.

**C. App Service settings** (Settings → **Environment variables** → App settings → + Add, then **Apply**):

| Name | Value |
|---|---|
| `ConnectionStrings__DefaultConnection` | `@Microsoft.KeyVault(VaultName=leaveportal-kv;SecretName=SqlConnection)` |
| `BlobStorage__ConnectionString` | `@Microsoft.KeyVault(VaultName=leaveportal-kv;SecretName=BlobConnection)` |
| `BlobStorage__ContainerName` | `leave-documents` |
| `ServiceBus__ConnectionString` | `@Microsoft.KeyVault(VaultName=leaveportal-kv;SecretName=ServiceBusConnection)` |
| `Jwt__SecretKey` | `@Microsoft.KeyVault(VaultName=leaveportal-kv;SecretName=JwtSecretKey)` |
| `Jwt__Issuer` | `leaveportal` |
| `Jwt__Audience` | `leaveportal-users` |
| `ASPNETCORE_ENVIRONMENT` | `Development` (shows Swagger; use `Production` for real use) |

**D. Function App settings** (same place):

| Name | Value |
|---|---|
| `ServiceBusConnection` | `@Microsoft.KeyVault(VaultName=leaveportal-kv;SecretName=ServiceBusConnection)` |
| `SqlConnectionString` | `@Microsoft.KeyVault(VaultName=leaveportal-kv;SecretName=SqlConnection)` |
| `Email__AppPassword` | `@Microsoft.KeyVault(VaultName=leaveportal-kv;SecretName=EmailAppPassword)` |
| `Email__SmtpHost` | `smtp.gmail.com` |
| `Email__SmtpPort` | `587` |
| `Email__SenderEmail` | your Gmail address |
| `Email__SenderName` | `LeavePortal` |

**E. Check:** after Apply, each Key Vault setting shows a **green tick / "Key vault Reference"** in the Source column. Red = role not assigned yet (wait 1–2 min, then refresh).

**How it works:** `__` in a setting name = `:` in code, so `Jwt__SecretKey` is read by `configuration["Jwt:SecretKey"]`. The code never changes. At startup Azure sees `@Microsoft.KeyVault(...)`, uses the app's managed identity to fetch the real value from Key Vault, and hands it to the app as a normal setting.

**Interview answers:**
- *Managed Identity* = an identity Azure creates for your app in Entra ID. The app gets tokens automatically, so there is no password or key to store anywhere.
- *System-assigned* = tied to one resource; created and deleted with it. *User-assigned* = a standalone identity you create once and attach to many resources (survives if the app is deleted).
- *Why not appsettings.json* = secrets end up in Git and in every copy of the code, can't be rotated in one place, and there's no audit of who read them.

## Troubleshooting log (real errors hit during the rebuild)
| Error | Cause | Fix |
|---|---|---|
| Key vault reference red ❌ `SecretNotFound` | Secrets not created yet / name typo | Create secrets with exact names |
| Secrets page: "operation is not allowed by RBAC" | You lack a data role on the vault | Give yourself **Key Vault Secrets Officer** |
| `Login failed for user '<token-identified principal>'` | Copied the Entra **passwordless** connection string | Use ADO.NET **SQL authentication** string |
| SQL login fails though password is right | DB name wrong (`free-sql-db-2358031`, not `leaveportal-db`) | Fix `Initial Catalog` |
| `IDX10720 ... key has '240' bits` | JWT key too short | 64-character `JwtSecretKey` |
| `ServiceBusConnectionStringProperties.Parse` FormatException | Copied the key, not the connection string | Copy **Primary Connection String** (`Endpoint=sb://...`) |
| `StorageConnectionString ... No valid combination of account information` | Copied the key, not the connection string | Copy key1 **Connection string** (`DefaultEndpointsProtocol=https;...`) |
| New secret version not picked up | App Service caches Key Vault values (up to 24h) | Change any app setting (e.g. `KV_REFRESH`) and Apply |

Tip: to test whether Key Vault or the value is the problem, temporarily put the plain value in the app setting, then switch back.
Tip: one bad connection string breaks *every* endpoint of a controller, because DI builds all dependencies (LeaveService → ServiceBusClient, BlobStorageService) when the controller is created.
