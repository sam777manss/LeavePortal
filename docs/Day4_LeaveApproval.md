# Day 4 — Leave Approval Module (Manager)

**Branch:** `backend`
**Status:** ✅ Complete — build verified (0 errors, 0 warnings), smoke-tested end-to-end
**Pattern:** CQRS via MediatR + FluentValidation + role-based authorization (builds on Days 2–3)

---

## 1. What Day 4 Delivers

The Manager side of leave management. An authenticated **Manager** can:

| Action | Endpoint | Type | Authorization |
|---|---|---|---|
| View all pending applications | `GET /api/leave/pending` | Query (read) | `Manager` only |
| Approve a pending application | `PUT /api/leave/{id}/approve` | Command (write) | `Manager` only |
| Reject a pending application | `PUT /api/leave/{id}/reject` | Command (write) | `Manager` only |

> Employees applying / viewing / cancelling their own leave was **Day 3**.
> Day 4 is purely the review side and is gated to the Manager role.

---

## 2. Files Added & Why

### Core project (contracts, no logic)

| File | Role |
|---|---|
| `Commands/Leave/ApproveLeaveCommand.cs` | MediatR command. Carries application id + **ManagerId (from claims)** + optional comment. |
| `Commands/Leave/RejectLeaveCommand.cs` | MediatR command. Same, but comment is non-nullable (required by validator). |
| `Queries/Leave/GetPendingLeavesQuery.cs` | MediatR query → list of pending applications. **No UserId filter** — a manager sees everyone's. |
| `DTOs/Leave/PendingLeaveDto.cs` | Manager-facing response. Adds `EmployeeName`/`EmployeeEmail` so the manager knows *who* applied. |
| `DTOs/Leave/ApproveLeaveRequest.cs` | Request body for approve — only an optional `Comment`. |
| `DTOs/Leave/RejectLeaveRequest.cs` | Request body for reject — a `Comment` (required, enforced by validator). |
| `Validators/Leave/ApproveLeaveCommandValidator.cs` | id/manager > 0; comment length ≤ 1000 **only when provided** (`.When`). |
| `Validators/Leave/RejectLeaveCommandValidator.cs` | id/manager > 0; comment **`NotEmpty`** + length ≤ 1000. |

### Infrastructure project (implementations)

| File | Role |
|---|---|
| `Handlers/Leave/ApproveLeaveCommandHandler.cs` | "Only Pending can be approved" rule, stamps review fields, sets `Approved`, publishes `LeaveApproved`. |
| `Handlers/Leave/RejectLeaveCommandHandler.cs` | Same shape, sets `Rejected`, publishes `LeaveRejected`. Comment guaranteed present by validator. |
| `Handlers/Leave/GetPendingLeavesQueryHandler.cs` | Read-only projection of all `Pending` apps, **oldest-first (FIFO)**, with employee + leave-type names. |

### API project (entry point)

| File | Role |
|---|---|
| `Controllers/LeaveController.cs` (edited) | Added 3 endpoints, each `[Authorize(Roles = "Manager")]`. ManagerId read from claims. |

> **No `Program.cs` change.** New handlers were auto-discovered by the existing
> `AddMediatR(... RegisterServicesFromAssembly ...)` and new validators by
> `AddValidatorsFromAssemblyContaining<>`. This is the payoff of the Day 2 setup.

---

## 3. Key Decisions & Why (interview-ready)

### 3.1 Role-based authorization — layered `[Authorize]`
The controller has `[Authorize]` at the **class** level (must be logged in). The Day 4 methods add `[Authorize(Roles = "Manager")]` at the **method** level (must *also* be a Manager). They stack:

| Layer | Rule | Blocks |
|---|---|---|
| Class `[Authorize]` | Authenticated | Anonymous → **401 Unauthorized** |
| Method `[Authorize(Roles="Manager")]` | Has Manager role | Logged-in Employee → **403 Forbidden** |

This is the **401 vs 403** distinction: 401 = "I don't know who you are"; 403 = "I know who you are, and you're not allowed." The role comes from the `ClaimTypes.Role` claim baked into the JWT by `JwtService` at login — no manual role-checking code.

### 3.2 ManagerId comes from the JWT, never the request body
Same security principle as Day 3's UserId. The request body carries only the `Comment`; the application id comes from the route; the **manager identity** is read from the authenticated token:

```csharp
new ApproveLeaveCommand(id, CurrentUserId, request.Comment)
//                       ↑      ↑                ↑
//                  route   JWT claims     request body
```

A manager cannot be impersonated by editing a payload.

### 3.3 Reject requires a comment; approve does not
- `RejectLeaveCommand.Comment` is `string` (non-nullable) and the validator uses `NotEmpty()` — rejecting requires a reason so the employee learns *why*.
- `ApproveLeaveCommand.Comment` is `string?` and the validator uses `.When(x => x.Comment is not null)` — a manager may approve silently.

The type choice signals intent; the validator makes it a hard rule. `NotEmpty()` also rejects whitespace-only `"   "`.

### 3.4 Pending → Approved/Rejected state machine
Both handlers guard the status before acting:

```csharp
if (application.Status != LeaveStatus.Pending.ToString())
    throw new Exception($"Only pending applications can be approved. Current status: {application.Status}.");
```

This prevents double-processing — you cannot approve something already rejected, re-approve, or review a cancelled request. Transitions are one-way.

### 3.5 Review audit fields are now populated
The columns that sat `NULL` through Day 3 are stamped here — the audit trail of *who* decided *what*, *when*, and *why*:

```csharp
application.Status        = LeaveStatus.Approved.ToString();  // or Rejected
application.ReviewedBy     = request.ManagerId;   // from claims
application.ReviewComment  = request.Comment;
application.ReviewedAt     = DateTime.UtcNow;
application.UpdatedAt      = DateTime.UtcNow;
```

### 3.6 Manager query has no ownership filter — but a richer DTO
`GetMyLeavesQuery` (Day 3) filtered by `UserId` for ownership. `GetPendingLeavesQuery` deliberately does **not** — a manager reviews everyone's. Protection here is the **role check**, not ownership. Because the manager needs to know *who* applied, the result uses `PendingLeaveDto` (adds `EmployeeName`/`EmployeeEmail`) instead of reusing `LeaveApplicationDto`. Different consumer → different DTO shape.

### 3.7 FIFO review queue
`GetPendingLeavesQueryHandler` orders **ascending** by `CreatedAt` (Day 3's "my leaves" was descending). A review queue is first-in-first-out so the longest-waiting request surfaces at the top and nothing rots at the bottom.

### 3.8 PUT, not POST
Approve/reject **modify the state of an existing resource**, so they use `PUT` (consistent with Day 3's `cancel`). `POST` is reserved for creation (`apply`).

---

## 4. Request Flow (Approve)

```
PUT /api/leave/{id}/approve  (JWT cookie sent automatically)
        ↓
[Authorize] + [Authorize(Roles="Manager")]  → 401 if anonymous, 403 if not Manager
        ↓
LeaveController.Approve() — ManagerId from claims, builds ApproveLeaveCommand
        ↓
_mediator.Send(command)
        ↓
ValidationBehavior → ApproveLeaveCommandValidator (ids, comment length)
        ↓ valid
ApproveLeaveCommandHandler.Handle()
   - load application (+ LeaveType, + User)     (DB)
   - exists? is it Pending?
   - stamp Status/ReviewedBy/ReviewComment/ReviewedAt/UpdatedAt
   - SaveChanges                                (DB)
   - publish LeaveApproved message              (stub logs it)
        ↓
returns LeaveApplicationDto → 200 OK
```

---

## 5. How to Test (Swagger)

Run the API (F5), open `http://localhost:5078/swagger`.

> The seed `manager@leaveportal.com` / `employee@leaveportal.com` users have placeholder
> password hashes and **cannot log in**. Register fresh users instead.

1. **Register a Manager:** `POST /api/auth/register`
   ```json
   { "fullName": "Rajesh Manager", "email": "mgr@x.com", "password": "Mgr@123", "role": "Manager", "departmentId": 1 }
   ```
2. **Register an Employee** (role `"Employee"`), log in as the employee, and **apply** for leave (Day 3) so there is something pending.
3. **Log in as the Manager**, then `GET /api/leave/pending` → see the pending application(s) with the employee's name.
4. **Approve:** `PUT /api/leave/{id}/approve` with `{ "comment": "Approved, enjoy" }` → status `Approved`, `reviewedAt` set.
5. **Reject (validation):** `PUT /api/leave/{id}/reject` with `{ "comment": "" }` → **400** ("A comment is required when rejecting").
6. **Reject:** same with a real comment → status `Rejected`, comment recorded.
7. **Role check:** log in as the Employee and call `GET /api/leave/pending` → **403 Forbidden**.
8. **Outcome flows back:** as the Employee, `GET /api/leave/my` → see the Approved/Rejected statuses and the manager's comments.

---

## 6. Things to Note / Watch For

- **No leave-balance deduction yet.** Approval does not subtract days from `LeaveBalances`. That is **Day 6**.
- **Service Bus is still a stub.** `LeaveApproved`/`LeaveRejected` messages are only logged, no email sent. Real bus + email = **Day 5**.
- **No "manager can only review their department" rule.** Currently any Manager can review any application. Department-scoping could be added later if required.
- **Azure SQL Basic tier cold-start.** When the DB has been idle, the first request can take ~10–15s and may surface as a connection timeout; the retry succeeds once warm. Worth a sensible connection-retry/timeout on the frontend.
- **Auto-registration still holds.** No `Program.cs` edits were needed for the new handlers/validators.

---

## 7. Next — Day 5: Real Notifications

- Replace the `ServiceBusPublisher` stub with the real `Azure.Messaging.ServiceBus` client.
- Azure Function triggered by the queue → sends email (Gmail SMTP) → logs to `NotificationLogs`.
- No handler/controller changes — only the publisher implementation swaps, per the Day 3 interface decision.
