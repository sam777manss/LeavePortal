# Day 3 — Leave Application Module

**Branch:** `backend`
**Status:** ✅ Complete — build verified (0 errors, 0 warnings)
**Pattern:** CQRS via MediatR + FluentValidation (same as Day 2 Auth)

---

## 1. What Day 3 Delivers

The Employee side of leave management. An authenticated user can:

| Action | Endpoint | Type |
|---|---|---|
| Apply for leave | `POST /api/leave/apply` | Command (write) |
| View own leave history | `GET /api/leave/my` | Query (read) |
| View one of own applications | `GET /api/leave/{id}` | Query (read) |
| Cancel own pending application | `PUT /api/leave/{id}/cancel` | Command (write) |

> Manager approve/reject is **Day 4** — intentionally not in this module.

---

## 2. Files Added & Why

### Core project (contracts, no logic)

| File | Role |
|---|---|
| `DTOs/Leave/ApplyLeaveRequest.cs` | Shape the client POSTs. **No UserId** — see security note below. |
| `DTOs/Leave/LeaveApplicationDto.cs` | Clean response shape. We never return the raw EF entity. Includes `LeaveTypeName` so the frontend needs no second call. |
| `DTOs/Notifications/LeaveNotificationMessage.cs` | The message contract placed on the bus. The Day 5 Azure Function reads exactly this shape. |
| `Commands/Leave/ApplyLeaveCommand.cs` | MediatR command. Carries UserId (set from claims) + form data. |
| `Commands/Leave/CancelLeaveCommand.cs` | MediatR command. Carries application id + UserId (for ownership). |
| `Queries/Leave/GetMyLeavesQuery.cs` | MediatR query → list of DTOs for the current user. |
| `Queries/Leave/GetLeaveByIdQuery.cs` | MediatR query → single DTO, ownership-enforced, nullable. |
| `Validators/Leave/ApplyLeaveCommandValidator.cs` | FluentValidation rules for apply. |
| `Validators/Leave/CancelLeaveCommandValidator.cs` | FluentValidation rules for cancel. |
| `Interfaces/IServiceBusPublisher.cs` | Abstraction over the message bus (decoupling). |

### Infrastructure project (implementations)

| File | Role |
|---|---|
| `Handlers/Leave/ApplyLeaveCommandHandler.cs` | Validates leave type/user, computes TotalDays, saves Pending app, publishes `LeaveApplied`. |
| `Handlers/Leave/CancelLeaveCommandHandler.cs` | Ownership + "only Pending can cancel" rule, sets Cancelled, publishes `LeaveCancelled`. |
| `Handlers/Leave/GetMyLeavesQueryHandler.cs` | Read-only projection of the user's applications, newest first. |
| `Handlers/Leave/GetLeaveByIdQueryHandler.cs` | Read-only single application, id + UserId match. |
| `Services/ServiceBusPublisher.cs` | **STUB** — logs the message instead of sending. Replaced on Day 5. |

### API project (entry point)

| File | Role |
|---|---|
| `Controllers/LeaveController.cs` | Thin controller. `[Authorize]`. Reads UserId from claims, sends commands/queries via MediatR. |
| `Program.cs` (edited) | Registered `IServiceBusPublisher → ServiceBusPublisher`. |

---

## 3. Key Decisions & Why (interview-ready)

### 3.1 UserId comes from the JWT, never the request body
`ApplyLeaveRequest` deliberately has **no UserId**. If the client could send a UserId, a logged-in user could submit leave on behalf of someone else by changing one number. Instead the controller reads it from the authenticated token:

```csharp
private int CurrentUserId =>
    int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
```

The body carries only `LeaveTypeId, StartDate, EndDate, Reason`. Identity is always server-trusted.

### 3.2 TotalDays is computed on the server
```csharp
var totalDays = (request.EndDate.DayNumber - request.StartDate.DayNumber) + 1;
```
We never trust a client-sent day count. `+1` because both start and end dates are inclusive (a 10th→10th leave = 1 day).

### 3.3 Service Bus is abstracted and stubbed (decoupled architecture)
The TechDoc decision: the API must **not** send email directly — if email is slow/down, the API would hang. Instead the API drops a message on a queue and returns immediately.

On Day 3 the Service Bus namespace does not exist yet, so:
- Handlers depend on the **interface** `IServiceBusPublisher`.
- The current implementation `ServiceBusPublisher` just **logs** the JSON it would send.
- On Day 5 we replace only that one class with the real `Azure.Messaging.ServiceBus` client. **No handler/controller code changes** — that is the payoff of coding to an interface.

### 3.4 Commands vs Queries (CQRS)
- **Commands** (`ApplyLeaveCommand`, `CancelLeaveCommand`) change data.
- **Queries** (`GetMyLeavesQuery`, `GetLeaveByIdQuery`) only read; their handlers use `AsNoTracking()` for speed since nothing is modified.

### 3.5 Ownership enforced in the data layer
`GetLeaveByIdQuery` and `CancelLeaveCommand` both filter by `Id == ... && UserId == currentUser`. A user asking for someone else's application id simply gets nothing back (404 / not found) — authorization is enforced by the query itself, not a separate check that could be forgotten.

### 3.6 Cancel is state-guarded
Only a `Pending` application can be cancelled by the employee:
```csharp
if (application.Status != LeaveStatus.Pending.ToString())
    throw new Exception($"Only pending applications can be cancelled. Current status: {application.Status}.");
```

---

## 4. Request Flow (Apply)

```
POST /api/leave/apply  (JWT cookie sent automatically)
        ↓
LeaveController.Apply() — reads UserId from claims, builds ApplyLeaveCommand
        ↓
_mediator.Send(command)
        ↓
ValidationBehavior → ApplyLeaveCommandValidator (dates, reason, ids)
        ↓ valid
ApplyLeaveCommandHandler.Handle()
   - leave type active?      (DB)
   - user active?            (DB)
   - TotalDays = end-start+1
   - save LeaveApplication (Status = Pending)   (DB)
   - publish LeaveApplied message               (stub logs it)
        ↓
returns LeaveApplicationDto → 200 OK
```

---

## 5. How to Test (Swagger)

Run the API (F5), open `http://localhost:5078/swagger`.

1. **Login first** (Day 2): `POST /api/auth/login` with the seeded employee
   ```json
   { "email": "employee@leaveportal.com", "password": "Employee@123" }
   ```
   > Note: the seed file used placeholder password hashes. If login fails, register a fresh user via `POST /api/auth/register`, then log in as that user.

2. **Apply:** `POST /api/leave/apply`
   ```json
   {
     "leaveTypeId": 1,
     "startDate": "2026-07-01",
     "endDate": "2026-07-03",
     "reason": "Family function"
   }
   ```
   Expect **200** with `totalDays: 3`, `status: "Pending"`. Check the API console — you'll see the `[ServiceBus STUB] Would publish ...` log line.

3. **My history:** `GET /api/leave/my` → array containing the new application.

4. **Get by id:** `GET /api/leave/1` → the application. Try an id you don't own → **404**.

5. **Cancel:** `PUT /api/leave/1/cancel` → status becomes `Cancelled`. Call it again → **400** ("Only pending applications can be cancelled").

6. **Validation:** `POST /api/leave/apply` with `endDate` before `startDate` → **400** with the validation message.

7. **No auth:** call any endpoint after `POST /api/auth/logout` → **401**.

---

## 6. Things to Note / Watch For

- **No leave-balance check yet.** Applying does not yet verify the employee has enough days, and does not deduct from `LeaveBalances`. That is **Day 6** (deduction happens on approval).
- **No document upload yet.** `DocumentUrl` stays null. Blob upload is **Day 7**.
- **Service Bus is a stub.** Messages are only logged, no email is sent. Real bus + email = Days 5.
- **Manager review fields** (`ReviewedBy`, `ReviewComment`, `ReviewedAt`) are untouched until **Day 4**.
- **DateOnly types.** `StartDate`/`EndDate` are `DateOnly` (not `DateTime`) because the DB columns are `DATE`. JSON format is `"yyyy-MM-dd"`.
- **Auto-registration still holds.** New handlers were picked up by the existing `AddMediatR(... RegisterServicesFromAssembly ...)` and new validators by `AddValidatorsFromAssemblyContaining<>` — no Program.cs change was needed for those. Only the new `IServiceBusPublisher` registration was added.

---

## 7. Next — Day 4: Leave Approval (Manager)

- Manager views all pending applications (across employees).
- Manager approves/rejects with a comment → sets `ReviewedBy`, `ReviewComment`, `ReviewedAt`, `Status`.
- Publishes `LeaveApproved` / `LeaveRejected` messages.
- `[Authorize(Roles = "Manager")]` on those endpoints.
