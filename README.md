# RondiTrack

RondiTrack is a .NET 10 Web API for managing stokvels and their members. The API allows users and stokvels to be created, viewed, updated, and deleted. It also allows users to be added to and removed from stokvels.

## API Design

This project uses Controllers rather than Minimal APIs. Controllers were chosen because RondiTrack has multiple resources and several CRUD and membership endpoints. Controllers help keep these endpoints organised and separated, while Minimal APIs are more suitable for smaller and simpler APIs.

## Domain Rules

### User

A User must follow these rules:
- The ID must be greater than 0.
- First name cannot be empty.
- Last name cannot be empty.
- Email cannot be empty and must contain an `@` symbol.
- Phone number cannot be empty.

These rules are enforced by the User model when a user is created or updated to prevent the User from entering an invalid state.

### Stokvel

A Stokvel must follow these rules:
- The ID must be greater than 0.
- The name cannot be empty.
- The contribution amount must be greater than 0.
- The same User cannot be added to the same Stokvel more than once.

These rules are enforced by the Stokvel model to protect its state and ensure that its membership remains valid.

### Financial Data

`decimal` is used for contribution amounts because it is suitable for financial values and provides more precise decimal arithmetic than floating-point types such as `double`.

## Running the API

1. Make sure the .NET 10 SDK is installed.
2. Open a terminal in the RondiTrack project folder.
3. Run the application:

   ```bash
   dotnet run
4. The terminal will display the local address where the API is running.
5. Open the Scalar API interface in a browser by adding /scalar/v1 to the address.

   for example:
   http://localhost:5274/scalar/v1

The application uses an in-memory data store, so any changes to the data will be reset when the application is restarted.



=============================================================================================================================================



## Assignment 4.2 - Requests, Responses and Service Layer

### DTOs and Manual Mapping

RondiTrack uses request and response DTOs so that domain entities are not exposed directly through the API.

Create and update operations use request DTOs, while API responses use response DTOs.

Manual mapper classes are used to convert between DTOs and domain objects. Manual mapping was chosen because the application has a small number of models and the mappings are simple. It also keeps the transformations explicit and easy to understand without introducing an additional mapping library.

This is particularly useful for financial values such as contribution amounts because the mapping is visible and explicit.

### Service Layer

Business operations that involve decisions across multiple objects are handled by the service layer.

The service handles:

- Adding a member to a stokvel.
- Removing a member from a stokvel.
- Recording a member contribution.
- Checking whether the user is a member of the stokvel.
- Preventing duplicate contributions for the same member and cycle.
- Handling contribution idempotency.

The repository is responsible for storing and retrieving data, while the service is responsible for business decisions.

### Contribution Recording

A contribution records:

- The user ID.
- The stokvel ID.
- The contribution cycle.
- The contribution amount.

The contribution amount is obtained from the stokvel rather than supplied by the client. This prevents the client from changing the required contribution amount.

A member cannot record more than one contribution for the same stokvel and cycle.

### Idempotency

The contribution endpoint requires an `Idempotency-Key` header.

When a contribution request is received, the service creates a representation of the request using the stokvel ID, user ID and cycle.

If the idempotency key has not been used before, the contribution is processed and the original result is stored in the in-memory idempotency store.

If the same key is sent again with the same request data, the previously stored result is returned and another contribution is not created.

If the same key is reused with different request data, the request is rejected with `409 Conflict`.

The idempotency logic is handled in the service layer because it is part of the business operation rather than an HTTP/controller responsibility.

### Error Handling and Status Codes

Errors are returned using Problem Details (`application/problem+json`).

The API uses status codes according to the type of problem:

- `400 Bad Request` is used when the request itself is incomplete or malformed, such as when the required `Idempotency-Key` header is missing.
- `404 Not Found` is used when a requested resource such as a user or stokvel does not exist.
- `409 Conflict` is used when an idempotency key is reused with a different request.
- `422 Unprocessable Entity` is used when the request is structurally valid but violates a domain or business rule, such as attempting an invalid membership or contribution operation.

The difference between `400` and `422` is that `400` means the server cannot properly process the request as submitted, while `422` means the request is understood but its values or requested operation violate application rules.

### Testing

The API was tested using Scalar.

Idempotency was tested by sending the same contribution request twice with the same `Idempotency-Key`. The second request returned the original result without recording another contribution.

The same key was then reused with different contribution request data, which correctly returned `409 Conflict`.

Missing idempotency keys and nonexistent resources were also tested to verify Problem Details responses.




=============================================================================================================================================




## Assignment 4.3 — Validation and Centralized Error Handling

### Request Validation

RondiTrack uses FluentValidation to validate incoming request DTOs.

Validation is responsible for checking whether a request is well-formed before the application performs the requested operation.

Examples include:

- IDs must be greater than zero.
- Required names must not be empty.
- Email addresses must be correctly formatted.
- Contribution cycle numbers must be greater than zero.
- Monetary amounts must be greater than zero.

Validators only validate the shape of the request and do not access the repository to check whether resources exist.

### Validation vs Domain Exceptions

Validation answers:

> Is the request well-formed?

For example, a negative ID or invalid email address results in a `400 Bad Request`.

Domain exceptions answer:

> Is this operation allowed?

For example, requesting a user that does not exist or attempting to record a duplicate contribution is handled using a domain exception.

This keeps request validation separate from business rules.

### Domain Exception Hierarchy

RondiTrack uses the following domain exception hierarchy:

- `RondiTrackException`
  - `NotFoundException`
  - `BusinessRuleException`
  - `IdempotencyConflictException`

The centralized exception handler maps these exceptions to HTTP responses:

| Exception | HTTP Status |
|---|---|
| `RequestValidationException` | 400 Bad Request |
| `NotFoundException` | 404 Not Found |
| `IdempotencyConflictException` | 409 Conflict |
| `BusinessRuleException` | 422 Unprocessable Entity |
| Unexpected exception | 500 Internal Server Error |

Controllers do not format domain error responses themselves. They throw the appropriate exception and the centralized `GlobalExceptionHandler` creates the `application/problem+json` response.

### Duplicate Contributions and Idempotency Conflicts

Duplicate contributions and idempotency conflicts are treated as different failures.

A duplicate contribution occurs when a member has already contributed to the same contribution cycle. The request itself is understood, but it violates a RondiTrack business rule. Therefore, a `BusinessRuleException` is used and the API returns `422 Unprocessable Entity`.

An idempotency conflict occurs when an `Idempotency-Key` that was already used is reused with a different request payload. This is treated as a separate `IdempotencyConflictException` and returns `409 Conflict`.

If the same idempotency key is repeated with the same payload, the original contribution is returned rather than creating another contribution.

### Contribution Cycles

A `ContributionCycle` represents a specific collection period belonging to a stokvel.

Each contribution cycle contains:

- An ID
- A Stokvel ID
- A cycle number
- A target amount

One stokvel can therefore have multiple contribution cycles.

ContributionCycle CRUD communicates directly with the repository rather than having a separate service because the basic CRUD operations do not require enough additional domain logic to justify another service layer.

The contribution workflow does use the existing RondiTrack service because recording a contribution requires business-rule checks. Before recording a contribution, the application verifies that the requested ContributionCycle exists and belongs to the specified stokvel.

### Correlation IDs and Structured Logging

Every handled error response includes a correlation ID.

Example error response:

```json
{
  "title": "Business rule violated",
  "status": 422,
  "detail": "A contribution cycle with ID 101 already exists.",
  "correlationId": "0HNOSG1SD1J7U:00000006"
}
```

The same correlation ID is included in the corresponding application log:

```text
Request failed with correlation ID 0HNOSG1SD1J7U:00000006
```

This allows an API error response to be matched to the relevant server-side log entry.

### Scalar Verification

The API was manually verified using Scalar.

#### Malformed Request

An invalid user request returns:

```text
400 Bad Request
Content-Type: application/problem+json
```

The response uses the standardized validation ProblemDetails format and includes validation errors and a correlation ID.

#### Resource Not Found

Requesting a nonexistent user returns:

```text
404 Not Found
Content-Type: application/problem+json
```

This is produced by `NotFoundException` and the centralized exception handler.

#### Business Rule Violation

Attempting to create a resource with an ID that already exists returns:

```text
422 Unprocessable Entity
Content-Type: application/problem+json
```

#### ContributionCycle

A ContributionCycle was created for Stokvel 1:

```json
{
  "id": 102,
  "stokvelId": 1,
  "number": 2,
  "targetAmount": 5000
}
```

A contribution was then recorded against the real ContributionCycle:

```json
{
  "userId": 1,
  "stokvelId": 1,
  "cycle": 102,
  "contributionAmount": 500
}
```

#### Idempotency

Repeating the contribution using the same `Idempotency-Key` and the same payload returned the original contribution successfully.

Reusing the same key with a different payload returned:

```text
409 Conflict
Content-Type: application/problem+json
```

The response was handled by `IdempotencyConflictException` through the centralized exception handler.

### Automated Negative-Path Tests

xUnit integration tests verify the following negative paths:

1. Malformed request → `400 Bad Request`
2. Nonexistent resource → `404 Not Found`
3. Business-rule violation → `422 Unprocessable Entity`

Each test also verifies that the response content type is:

```text
application/problem+json
```

All three negative-path tests pass successfully.




================================================================================================================================




---

## Assignment 4.4 — Documentation & Testing

Assignment 4.4 strengthens the RondiTrack API contract and automated test coverage. No new endpoints or business rules were introduced. The focus was on documenting the existing API accurately and proving its behaviour through unit and integration tests.

### OpenAPI / Scalar Documentation

All existing API endpoints were reviewed and documented using OpenAPI metadata.

The documentation includes:

- Endpoint summaries and descriptions.
- Successful response types.
- Validation failure responses.
- Not-found responses.
- Business-rule violation responses.
- Idempotency conflict responses where applicable.
- Request and response schemas visible through Scalar.

Scalar was manually reviewed to confirm that the documented contract matches the implemented API behaviour.

### Automated Testing

The project contains both unit tests and integration tests.

**Unit tests** test domain and service behaviour directly without sending HTTP requests.

The unit tests cover:

- Rejecting duplicate stokvel membership.
- Rejecting removal of a user who is not a member.
- Rejecting duplicate contributions.
- Returning the original contribution when the same idempotency key and payload are repeated.
- Rejecting reuse of an idempotency key with a different payload.

**Integration tests** use `WebApplicationFactory<Program>` to exercise the real ASP.NET Core request pipeline.

The integration tests cover:

- Successful user creation and retrieval.
- Successful stokvel creation and retrieval.
- Successful contribution-cycle creation and retrieval.
- Request validation failures.
- Missing resources.
- Duplicate users.
- Duplicate stokvels.
- Duplicate contribution cycles.
- Duplicate stokvel membership.
- Removing a non-member.
- Contributions from non-members.
- Contribution cycles belonging to another stokvel.
- Duplicate contributions.
- Idempotent request replay.
- Idempotency-key conflicts.

### Test Result

The complete automated test suite was run using:

```bash
dotnet test RondiTrack.Tests/RondiTrack.Tests.csproj
```

Final result:

```text
Total tests: 23
Passed: 23
Failed: 0
Skipped: 0
```

### Edge Cases

Three additional edge cases were identified and tested.

#### 1. Empty Member Collection

A newly created stokvel has no members.

The test creates a new stokvel and requests its members. It asserts that the API returns `200 OK` with an empty collection (`[]`) rather than an error.

#### 2. Zero Contribution Amount

A contribution amount of exactly `0` tests the lower boundary of the rule requiring the amount to be greater than zero.

The edge-case test initially exposed a missing `CreateStokvelRequest` validation rule because the request reached the domain constructor and produced a `500 Internal Server Error`.

A FluentValidation rule requiring `ContributionAmount > 0` was added. The test now confirms that the invalid request is rejected with `400 Bad Request`.

#### 3. Contribution Cycle for a Missing Stokvel

The request itself can have a valid structure while referencing a stokvel that does not exist.

The test confirms that attempting to create a contribution cycle for a missing stokvel returns `404 Not Found`.

### Idempotency Verification

Idempotency behaviour is verified automatically.

The tests confirm that:

- Sending the same contribution request twice with the same `Idempotency-Key` returns the same successful result.
- Reusing an idempotency key with a different payload returns `409 Conflict`.
- Attempting the same contribution again with a different idempotency key is treated as a duplicate contribution and returns `422 Unprocessable Entity`.

### Deliberate Test Failure

To confirm that the automated tests genuinely detect broken business behaviour, the duplicate-member rule in `Stokvel.AddMember()` was temporarily disabled.

The targeted test:

```text
AddMember_WhenUserAlreadyMember_ThrowsBusinessRuleException
```

changed from passing to failing because no `BusinessRuleException` was thrown.

The business rule was then restored and the complete test suite was rerun successfully with all 23 tests passing.

This demonstrated the sequence:

```text
Rule enabled  -> test passes
Rule removed  -> test fails
Rule restored -> test passes
```

The deliberately broken implementation was not committed.

### Definition of Done

### Definition of Done

| Endpoint | Documented | Validated | Unit Tested | Integration Tested | Status Codes Reviewed |
|---|---|---|---|---|---|
| GET `/api/users` | Yes | Yes | No | No | Yes |
| GET `/api/users/{id}` | Yes | Yes | No | Yes | Yes |
| POST `/api/users` | Yes | Yes | No | Yes | Yes |
| PUT `/api/users/{id}` | Yes | Yes | No | No | Yes |
| DELETE `/api/users/{id}` | Yes | Yes | No | No | Yes |
| GET `/api/stokvels` | Yes | Yes | No | No | Yes |
| GET `/api/stokvels/{id}` | Yes | Yes | No | Yes | Yes |
| POST `/api/stokvels` | Yes | Yes | No | Yes | Yes |
| PUT `/api/stokvels/{id}` | Yes | Yes | No | No | Yes |
| DELETE `/api/stokvels/{id}` | Yes | Yes | No | No | Yes |
| GET `/api/stokvels/{stokvelId}/members` | Yes | Yes | No | Yes | Yes |
| POST `/api/stokvels/{stokvelId}/members/{userId}` | Yes | Yes | Yes | Yes | Yes |
| DELETE `/api/stokvels/{stokvelId}/members/{userId}` | Yes | Yes | Yes | Yes | Yes |
| POST `/api/stokvels/{stokvelId}/members/{userId}/contributions` | Yes | Yes | Yes | Yes | Yes |
| GET `/api/contribution-cycles` | Yes | Yes | No | No | Yes |
| GET `/api/contribution-cycles/{id}` | Yes | Yes | No | Yes | Yes |
| POST `/api/contribution-cycles` | Yes | Yes | No | Yes | Yes |
| PUT `/api/contribution-cycles/{id}` | Yes | Yes | No | No | Yes |
| DELETE `/api/contribution-cycles/{id}` | Yes | Yes | No | No | Yes |

The Definition of Done intentionally records endpoints without direct unit or integration tests as "No" rather than overstating the current automated coverage. Business rules and critical API paths have dedicated automated tests, while some simple CRUD operations are currently verified through the documented contract and manual Scalar review.

### Known Gaps and Constraints

RondiTrack continues to use in-memory storage as required by the assignment. Data is therefore reset when the application restarts.

Authentication and authorization are not implemented because they are outside the scope of Assignment 4.4.

No Entity Framework Core or PostgreSQL persistence was introduced.

The API documentation was manually reviewed in Scalar, while the automated test suite remains the primary proof of application behaviour.




==================================================================================================================================






## 5. Persistence and Database

### 5.1 PostgreSQL Setup

RondiTrack uses a local PostgreSQL database named `RondiTrack`.

PostgreSQL was installed and run locally. The database was created specifically for RondiTrack rather than using a shared/default database.

The application connects to PostgreSQL using:

- Host: `localhost`
- Port: `5432`
- Database: `RondiTrack`
- Username: `postgres`

The connection was tested independently through the PostgreSQL administration tools before being used by the API.

### 5.2 Secret Management

The PostgreSQL connection string is stored using .NET User Secrets rather than being committed to the repository.

User Secrets was initialised with:

'''bash
dotnet user-secrets init

The connection string was then stored using:

dotnet user-secrets set "ConnectionStrings:RondiTrack" "Host=localhost;Port=5432;Database=RondiTrack;Username=postgres;Password=YOUR_PASSWORD;Pooling=true"

The password is therefore not stored in appsettings.json or committed to Git.

5.3 Entity Framework Core

RondiTrack uses Entity Framework Core 10 with the PostgreSQL Npgsql provider.

All six persisted entities are represented in RondiTrackDbContext:

User
Stokvel
StokvelMember
ContributionCycle
Contribution
Payout

The EfRondiTrackRepository was introduced to persist application data through EF Core while keeping the existing repository interface unchanged.

The EF Core repository is registered as scoped because DbContext is scoped and is intended to be used within a request. A singleton repository holding a DbContext could cause the same context to be shared across requests and lead to concurrency and tracking problems.

5.4 EF Core Mapping Issue

The Contribution entity initially caused an EF Core mapping problem because its constructor parameter contributionAmount could not be bound to a mapped property.

The property was changed to:

public decimal ContributionAmount { get; private set; }

This allowed EF Core to map the property correctly while keeping the setter private to protect the domain model.

5.5 Migrations

The initial database schema was created using an EF Core migration:

dotnet ef migrations add InitialCreate
dotnet ef database update

The migration creates the required tables, primary keys, foreign keys and indexes in PostgreSQL.

5.6 Payout Processing

A minimal payout rule was introduced because Payout is new functionality for this assignment.

Members are ordered by UserId. The contribution cycle number determines the recipient's position in the rotation. The rotation wraps around when the number of members is exceeded.

The selected recipient must have made a contribution for the relevant cycle.

The payout amount is based on the stokvel's contribution amount multiplied by the number of members.

The payout operation creates a Payout record and marks the related ContributionCycle as Paid.

5.7 Transactions

Payout processing uses an explicit database transaction.

The transaction protects the two related database changes:

Marking the contribution cycle as Paid.
Creating the Payout record.

If either operation fails, the transaction is rolled back so that no partial state remains in the database.

A rollback integration test deliberately causes the payout operation to fail and then re-queries PostgreSQL to confirm that the cycle remains in its original state and that the failed payout was not persisted.

5.8 Testing

The existing integration and business-rule test suite was run against the real local PostgreSQL database.

The test suite was run using:

dotnet test

The tests cover the existing API behaviour as well as the new payout functionality, including successful payout processing and transaction rollback.

5.9 Definition of Done
Requirement	Completed
Existing tests still pass	Yes
PostgreSQL persistence implemented	Yes
EF Core DbContext configured	Yes
Initial migration created and applied	Yes
Repository swapped to EF Core	Yes
Payout processing implemented	Yes
Explicit transaction implemented	Yes
Transaction rollback tested	Yes
Persisted via EF Core	Yes
Explicit transaction tested	Yes




=================================================================================================================================



Assigment 5.2

## 6. Relationships and Query Behaviour

### 6.1 StokvelMember Relationship

StokvelMember is modelled as an explicit join entity between Stokvel and User.

It uses the natural composite primary key:

(UserId, StokvelId)

A surrogate Id was not added because the combination of UserId and StokvelId already uniquely identifies a membership.

StokvelMember also stores relationship-specific data:

- Role
- JoinedAtUtc

Contribution references a membership using the existing StokvelId and UserId values. A composite foreign key was configured from Contribution to StokvelMember.

Payout was not changed because the membership key did not require a change to the existing payout behaviour.

### 6.2 StokvelMember Access

The project does not use a generic IRepository<T> with a single Id lookup.

StokvelMember is mainly accessed through the Stokvel aggregate using the Members navigation collection.

A dedicated member lookup is also available where the composite StokvelId and UserId values are required.

This avoids introducing a surrogate Id only to support repository access.

### 6.3 Relationship Migrations

Two migrations were created for the relationship changes:

- AddRelationships
- AddContributionMemberRelationship

The migrations were inspected before being applied.

AddRelationships added Role and JoinedAtUtc to StokvelMembers.

The existing StokvelMember composite key was already present from the previous database schema, so the migration did not need to replace the primary key.

Existing rows receive migration default values for the new fields. This preserves the existing data, although the migration cannot recreate the original historical JoinedAtUtc value for memberships that already existed.

AddContributionMemberRelationship added the composite foreign key from Contribution to StokvelMember.

The migrations did not require dropping and recreating the StokvelMembers table.

Both migrations were successfully applied to PostgreSQL.

### 6.4 Additional Relationship

A one-to-many relationship was modelled between Stokvel and ContributionCycle.

Stokvel contains a ContributionCycles navigation collection and ContributionCycle contains a Stokvel navigation property.

ContributionCycle.StokvelId is used as the foreign key.

An alternative relationship that could also have been modelled was ContributionCycle to Contribution. Stokvel to ContributionCycle was chosen because it directly represents the cycles belonging to a stokvel and was sufficient for the assignment.

### 6.5 N+1 Query Demonstration

The following endpoint was used to demonstrate the N+1 query problem:

GET /api/stokvels/{stokvelId}/cycles/{cycleId}/contributions

The initial implementation first loaded all contributions without eager loading.

It then performed a separate StokvelMember and User query for every contribution.

The test contained 3 contributions.

The measured result was:

- 1 query to load the contributions
- 3 additional queries to load the related member and user data
- 4 SQL queries in total

This demonstrated the N+1 behaviour where the number of queries grows as:

1 + N

For the measured 3 contributions this resulted in 4 queries.

### 6.6 N+1 Fix - Eager Loading

The first solution used eager loading:

Include(contribution => contribution.Member)
ThenInclude(member => member.User)

The same endpoint and the same 3 contributions were tested again.

The measured result was:

- 1 SQL query in total

EF Core joined Contributions, StokvelMembers and Users in a single query.

This removed the N+1 behaviour.

However, the query also retrieved member and user fields that were not required by the response, including Role, JoinedAtUtc, Email and PhoneNumber.

### 6.7 N+1 Fix - Projection

The second solution used Select to project the database query directly into ContributionResponse.

The same endpoint and the same 3 contributions were tested again.

The measured result was:

- 1 SQL query in total

The projection selected only:

- UserId
- StokvelId
- Cycle
- ContributionAmount
- FirstName and LastName for MemberName

Projection was chosen as the implementation to ship because it removes the N+1 problem while also avoiding retrieval of unnecessary entity columns.

The final comparison was:

| Approach | Contributions | SQL Queries |
|---|---:|---:|
| Naive N+1 | 3 | 4 |
| Eager loading | 3 | 1 |
| Projection | 3 | 1 |

### 6.8 Loading Strategy

Lazy loading is not used.

Relationships are loaded deliberately depending on the read path.

The contribution endpoint uses projection because only a specific response shape is required.

GetStokvelByIdAsync uses eager loading for Members and their related Users where the full relationship is required.

GetStokvelsAsync eagerly loads Members so that MemberCount can be returned correctly.

This makes the required database access explicit and avoids hidden queries caused by lazy loading.

### 6.9 AsNoTracking

AsNoTracking is used for read-only queries where the returned entities are not going to be modified.

It is used for read-only operations including:

- Getting all users
- Getting all contribution cycles
- Getting all stokvels
- Getting contribution data for the N+1 comparison
- Getting projected contribution responses
- Getting all payouts

Methods that are shared with write operations remain tracked.

For example, GetStokvelByIdAsync remains tracked because the returned Stokvel can be used when adding or removing members.

GetContributionCycleByIdAsync also remains tracked because the contribution cycle can be modified during payout processing.

This prevents AsNoTracking from interfering with write operations while avoiding unnecessary tracking on read-only paths.

### 6.10 Testing

The existing test suite was run again after the relationship and query behaviour changes using:

dotnet test

The test suite succeeded after the Assignment 5.2 changes.

The relationship changes therefore did not break the existing tested behaviour.

The previous tests did not depend on a surrogate StokvelMember.Id because StokvelMember already used its composite membership identity rather than requiring a single Id.

### 6.11 Extended Definition of Done

| Requirement | Completed | Real Navigation Modelled | N+1 Measured / Fixed |
|---|---|---|---|
| User persistence | Yes | N/A | N/A |
| Stokvel persistence | Yes | Yes | N/A |
| StokvelMember relationship | Yes | Yes | N/A |
| ContributionCycle persistence | Yes | Yes | N/A |
| Contribution persistence | Yes | Yes | Yes |
| Payout persistence | Yes | N/A | N/A |
| PostgreSQL persistence | Yes | N/A | N/A |
| EF Core mappings | Yes | Yes | N/A |
| Relationship migrations | Yes | Yes | N/A |
| N+1 behaviour demonstrated | Yes | Yes | Yes |
| Eager-loading fix implemented | Yes | Yes | Yes |
| Projection fix implemented | Yes | Yes | Yes |
| AsNoTracking used on read paths | Yes | N/A | N/A |
| Lazy loading avoided | Yes | Yes | N/A |
| Existing tests still pass | Yes | N/A | N/A |

### 6.12 Remaining Gaps

Not every possible relationship in the domain has been converted into a navigation property.

Only the relationships required for the current application behaviour and Assignment 5.2 were added.

Additional relationships can be introduced later when required by future application behaviour rather than adding unnecessary relationships in advance.