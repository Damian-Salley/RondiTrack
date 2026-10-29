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





=================================================================================================================================





## Assignment 5.4 — Clean Architecture & Integration Tests

### 1. Project Structure
- **RondiTrack:** ASP.NET Core Web API, controllers, services, validation and exception handling.
- **RondiTrack.Domain:** Domain models, business exceptions and repository interfaces.
- **RondiTrack.Infrastructure:** PostgreSQL database access, EF Core DbContext, repositories and migrations.
- **RondiTrack.Tests:** Unit tests and integration tests.

### 2. Clean Architecture
- The application separates domain logic, infrastructure and API responsibilities.
- The Domain project does not depend on Infrastructure or the API.
- Infrastructure depends on Domain.
- The API references Domain and Infrastructure.
- Repository interfaces separate application logic from database implementation.

### 3. Requirements
- .NET SDK 10
- PostgreSQL
- Docker Desktop
- Entity Framework Core CLI tools

### 4. Database Configuration
- PostgreSQL is used as the application database.
- Entity Framework Core manages database operations and migrations.
- The development database connection string is stored using .NET User Secrets.
- Database migrations are stored in RondiTrack.Infrastructure.

Configure the connection string:

    dotnet user-secrets set "ConnectionStrings:RondiTrack" "Host=localhost;Port=5432;Database=ronditrack;Username=postgres;Password=YOUR_PASSWORD"

Apply migrations:

    dotnet ef database update --project .\RondiTrack.Infrastructure\RondiTrack.Infrastructure.csproj --startup-project .\RondiTrack.csproj

### 5. Running the Application
Restore dependencies:

    dotnet restore

Build the application:

    dotnet build .\RondiTrack.csproj

Run the API:

    dotnet run --project .\RondiTrack.csproj

### 6. Integration Tests
- Integration tests use Testcontainers for .NET.
- Docker Desktop must be running before executing integration tests.
- Tests automatically start PostgreSQL 17 containers.
- EF Core migrations are applied to the temporary test databases.
- Test data is inserted automatically.
- Each integration test uses an isolated PostgreSQL database.
- Tests do not access or delete the development database.
- Temporary containers are disposed of after testing.

### 7. Running Tests
Run all tests:

    dotnet test .\RondiTrack.Tests\RondiTrack.Tests.csproj

Run tests without rebuilding:

    dotnet test .\RondiTrack.Tests\RondiTrack.Tests.csproj --no-build

Run a specific test:

    dotnet test .\RondiTrack.Tests\RondiTrack.Tests.csproj --filter "FullyQualifiedName~DuplicateStokvel_Returns422"

### 8. Test Results
- Total tests: 25
- Passed: 25
- Failed: 0
- Skipped: 0

All 25 tests passed successfully.

### 9. Error Handling
- The API uses centralized exception handling.
- HTTP 400 is returned for request validation errors.
- HTTP 404 is returned when a resource cannot be found.
- HTTP 409 is returned for idempotency conflicts.
- HTTP 422 is returned for business rule violations and duplicate resource identifiers.
- HTTP 500 is returned for unexpected server errors.
- Error responses use ProblemDetails with a correlation ID.

### 10. Database Transactions
- Payout processing uses an EF Core database transaction.
- Updating a contribution cycle and creating a payout are performed atomically.
- EF Core's execution strategy supports database retries.
- Failed payout transactions are rolled back.





=======================================================================================================================================




---

# Assignment 6.1 — Authentication & Stokvel-Scoped Authorization

## 1. Overview

Assignment 6.1 introduces authentication and authorization to RondiTrack.

Users can register, log in, refresh their access tokens and log out.

The API uses JWT authentication and PostgreSQL-backed refresh tokens.

Authorization is based on each user's membership and role within a specific stokvel.

This distinction is important because a user can have different roles in different stokvels.

For example, a user can be a Treasurer in one stokvel and a regular Member in another.

## 2. User Registration

**Endpoint:** `POST /api/auth/register`

Registration includes:

- Password hashing using ASP.NET Core's `PasswordHasher<User>`.
- Password validation requiring at least 12 characters.
- FluentValidation for request validation.
- Email normalization to prevent duplicate accounts with different email capitalization.
- A unique database index on normalized email addresses.
- HTTP 400 for invalid registration details.
- HTTP 409 for duplicate email addresses.

Passwords are never stored as plaintext.

Every newly registered user receives the platform role `User`.

Users cannot assign themselves the `Admin` role during registration.

## 3. JWT Authentication

**Endpoint:** `POST /api/auth/login`

After successful authentication, the API issues a JWT access token.

### Access Token Configuration

| Setting | Value |
|---|---|
| Token type | JWT |
| Maximum lifetime | 15 minutes |
| ClockSkew | Zero |
| Issuer | RondiTrack |
| Audience | RondiTrack |
| Signing algorithm | Symmetric signing |
| Signing key | Base64-encoded key containing at least 32 bytes |

`ClockSkew = TimeSpan.Zero` ensures that an expired token is rejected immediately rather than allowing an additional grace period.

The JWT contains the following claims:

| Claim | Purpose |
|---|---|
| `sub` | Identifies the authenticated user |
| `jti` | Uniquely identifies the access token |
| Role | Identifies the user's platform-level role |

The platform role is separate from stokvel membership roles.

The access token does not contain stokvel-specific roles because those permissions can change while the token remains valid.

A platform role change may remain reflected in an existing JWT until that token expires.

## 4. Refresh Tokens

**Endpoints:**

- `POST /api/auth/refresh`
- `POST /api/auth/logout`

Refresh tokens are random values rather than JWTs.

Only their hashes are stored in PostgreSQL.

### Refresh-Token Rotation

Each refresh token can be used only once.

When a refresh request succeeds:

1. The existing refresh token is invalidated.
2. A replacement refresh token is generated.
3. The replacement token's hash is stored.
4. The replacement refresh token is delivered in a cookie.
5. A new JWT access token is returned.

### Refresh-Token Reuse Detection

If an already-used refresh token is presented again, the API treats it as possible token theft.

The refresh-token family associated with that login is revoked.

This also invalidates newer refresh tokens descended from the reused token.

The request returns HTTP 401.

A legitimate user whose session is affected must log in again.

### Logout

Logout revokes the session's refresh tokens.

Logout is idempotent, meaning repeated logout requests do not create additional changes or errors.

## 5. Refresh-Token Cookie Security

Refresh tokens are delivered through an HTTP cookie rather than in the response body.

| Cookie setting | Value | Reason |
|---|---|---|
| HttpOnly | true | Prevents JavaScript from reading the refresh token |
| Secure | true | Restricts transmission to HTTPS |
| SameSite | Strict | Reduces cross-site request forgery risk |
| Path | `/api/auth` | Limits the cookie to authentication endpoints |

### Frontend Token Storage

The frontend should keep JWT access tokens in memory rather than persistent browser storage.

Refresh tokens should remain in the HttpOnly cookie managed by the browser.

Storing either token in `localStorage` increases the risk of token theft through cross-site scripting.

Storing access tokens in memory means they are lost when the page is reloaded, but the application can obtain a replacement using the refresh-token cookie.

HttpOnly cookies reduce JavaScript access to refresh tokens, although HTTPS and appropriate CSRF protections remain important.

## 6. Secure-by-Default Authorization

RondiTrack uses a fallback authorization policy.

This means endpoints require authentication unless explicitly marked with `[AllowAnonymous]`.

Authentication endpoints that require anonymous access remain publicly accessible.

OpenAPI and Scalar documentation endpoints are also accessible without a JWT in the Development environment.

### Authentication Failures

| Situation | HTTP status |
|---|---|
| Missing access token | 401 Unauthorized |
| Invalid or expired access token | 401 Unauthorized |
| Authenticated user lacks permission | 403 Forbidden |

Authentication and authorization failures use the `application/problem+json` response format.

Login also avoids revealing whether an email address exists.

An unknown email and an incorrect password produce the same HTTP 401 problem response.

## 7. Stokvel-Scoped Authorization

RondiTrack implements resource-based authorization using:

- `StokvelAccessRequirement`
- `StokvelAccessHandler`
- ASP.NET Core authorization policies
- Membership information stored in PostgreSQL

The handler checks the authenticated user's membership and role for the requested stokvel.

### Stokvel Roles

| Role | Permissions |
|---|---|
| Member | View stokvel information, view cycles and contributions, record own contributions |
| Treasurer | Member permissions, open cycles, record contributions for other members, process payouts |
| Chairperson | Treasurer permissions, update stokvel details, manage members |

### Authorization Policies

**StokvelMember**

Requires the authenticated user to belong to the requested stokvel.

**StokvelManager**

Requires the authenticated user to be a Treasurer or Chairperson of the requested stokvel.

**StokvelChairperson**

Requires the authenticated user to be the Chairperson of the requested stokvel.

The policies use database membership information rather than trusting stokvel roles from JWT claims.

## 8. Authorization Matrix

The following matrix describes the intended authorization behavior.

Successful requests may still fail because of validation or business rules.

| Endpoint / Operation | Non-member | Member | Treasurer | Chairperson | Platform Admin* |
|---|---|---|---|---|---|
| View a specific stokvel | 404 | 200 | 200 | 200 | 404 |
| View stokvel cycles | 404 | 200 | 200 | 200 | 404 |
| View stokvel contributions | 404 | 200 | 200 | 200 | 404 |
| Record own contribution | 404 | Success | Success | Success | 404 |
| Record another member's contribution | 404 | 403 | Success | Success | 404 |
| Open a contribution cycle | 404 | 403 | Success | Success | 404 |
| Process a payout | 404 | 403 | Success | Success | 404 |
| Update stokvel details | 404 | 403 | 403 | Success | 404 |
| Manage stokvel members | 404 | 403 | 403 | Success | 404 |

*The Platform Admin column assumes the administrator is not also a member of the stokvel. Platform Admin status does not automatically grant stokvel membership.*

### Platform-Level Operations

| Endpoint | Ordinary authenticated user | Platform Admin |
|---|---|---|
| `GET /api/stokvels` | 200 — own stokvels | 200 — own stokvels |
| `GET /api/stokvels/admin/all` | 403 | 200 — all stokvels |

Unauthenticated requests to protected endpoints return 401.

## 9. Why Non-Members Receive 404

A user who does not belong to a stokvel receives HTTP 404.

This prevents the API from revealing whether another person's stokvel exists.

For example, a user should not be able to discover private stokvels by trying different IDs.

A user who belongs to a stokvel but lacks permission receives HTTP 403.

The user already knows the stokvel exists, so returning 404 would not provide additional privacy.

This distinction reduces unnecessary information disclosure.

## 10. Why Stokvel Roles Are Not Stored in JWTs

JWT access tokens represent information captured when they are issued.

If a Treasurer is removed from a stokvel, an existing JWT could remain valid for up to 15 minutes.

Storing the Treasurer role in the JWT would allow outdated permissions to remain usable during that period.

RondiTrack avoids this by checking the `StokvelMembers` table when authorizing stokvel operations.

This means membership changes can take effect without waiting for an access token to expire.

Platform-level roles are handled separately.

## 11. Role-Based vs Policy-Based Authorization

RondiTrack demonstrates both authorization approaches.

### Role-Based Authorization

The platform administrator endpoint uses:

```csharp
[Authorize(Roles = "Admin")]
```

This is the single plain role check used for a platform-level operation.

The rule is simple: only a platform administrator can access the endpoint.

If the rule changed to allow another platform role, the role-based authorization attribute would need updating.

### Policy-Based Authorization

Stokvel operations use custom authorization policies.

For example:

```csharp
await _authorization.AuthorizeAsync(
    User,
    stokvelId,
    "StokvelManager");
```

The authorization handler checks the user's current membership and role in PostgreSQL.

If the permission rules changed, the authorization requirement or handler could be updated without duplicating role checks throughout the controllers.

### Why Both Are Used

Platform roles apply to the entire application.

Stokvel roles apply only within a specific stokvel.

A user can be a Chairperson in one stokvel and a Member in another.

Policy-based authorization is therefore more appropriate for stokvel-specific permissions.

## 12. Platform Administrator Security

The `User` entity contains a `PlatformRole` property.

Supported values are:

- `User`
- `Admin`

New users receive `User` by default.

The registration endpoint does not accept a platform role.

The Admin role must be assigned through a trusted administrative process rather than a public registration or profile-update endpoint.

The Admin role is included in the JWT and is used by the platform-level role check.

An Admin is not automatically authorized to perform operations inside every stokvel.

## 13. Signing-Key Security

The JWT signing key is stored using .NET user-secrets.

The application validates the signing key during startup.

The key must:

- Be present in configuration.
- Be valid Base64.
- Decode to at least 32 bytes.

If these requirements are not met, the application fails to start with a descriptive error.

Real signing keys, passwords and refresh tokens must never be committed to Git or written to application logs.

## 14. Integration Testing

RondiTrack uses xUnit and Testcontainers with PostgreSQL.

The test fixture starts a temporary PostgreSQL container, applies EF Core migrations and runs integration tests against the application.

This allows tests to run independently of the developer's local PostgreSQL database.

### Authentication Tests

The integration tests cover:

- Requests without JWTs returning 401.
- Unknown email and incorrect password returning identical problem responses.
- Refresh-token rotation.
- Refresh-token replay detection.
- Revocation of the newest token after replay.
- Refresh rejection after logout.

### Authorization Tests

The integration tests cover:

- Non-members receiving 404.
- Members without sufficient permissions receiving 403.
- Treasurers passing the payout authorization check.
- Members recording their own contributions.
- Members being forbidden from recording another member's contribution.

Existing integration tests must continue passing.

### Running Tests

From the solution directory:

```powershell
dotnet build
dotnet test .\RondiTrack.Tests\RondiTrack.Tests.csproj --verbosity normal
```

Docker must be running because Testcontainers starts PostgreSQL containers.

The final test run should also be performed with the development PostgreSQL database stopped.

### Final Test Result

**Pending final verification.**

Record the actual test count, failures and successful tests here after the final run.

## 15. Authentication and Refresh Logs

The assignment requires a real log excerpt showing login and refresh activity without exposing secrets.

**Pending: Insert an actual sanitized log excerpt from the application.**

Example of the required format (illustrative only, not an actual captured log):

```text
[Information] Login completed successfully.
[Information] Refresh token rotated successfully.
```

The final submitted excerpt must come from a real application run.

Confirm that the captured logs do not contain:

- Plaintext passwords
- JWT access tokens
- Refresh tokens
- JWT signing keys

## 16. Remaining Verification and Limitations

Before submission, the following must be confirmed:

- The new contribution authorization integration test passes.
- The Treasurer test verifies a successful operation, not only that authorization was passed.
- The Admin endpoint rejects ordinary users with 403.
- A trusted Admin can access the Admin endpoint successfully.
- The complete integration test suite passes with the development database stopped.
- A real login and refresh log excerpt has been captured.
- No secrets are present in the repository or its Git history.

These items are listed as pending verification rather than reported as completed.

---




====================================================================================================================================





---

# Assignment 6.2 — Observability and POPIA-Compliant Telemetry

## 1. Overview

RondiTrack uses OpenTelemetry for distributed tracing, application metrics, and structured logging.

The implementation includes:
- HTTP request tracing.
- PostgreSQL database tracing.
- Custom business tracing for payout processing.
- Metrics for contributions, payouts, and authorization refusals.
- POPIA-conscious structured logging.
- W3C trace correlation for API errors.
- Anonymous liveness and readiness health checks.
- Automated integration tests.

**OpenTelemetry service name:** `ronditrack-api`

Telemetry is exported using OTLP when the `OTEL_EXPORTER_OTLP_ENDPOINT` environment variable is configured.

## 2. Distributed Tracing

HTTP requests are traced automatically, excluding `/health` endpoints.

Payout processing uses a custom span named `payout.process`.

### Trace Hierarchy

The following trace was verified in Aspire Dashboard:

```text
POST /api/stokvels/{stokvelId}/cycles/{contributionCycleId}/payout
│
├── PostgreSQL spans
│
└── payout.process
    ├── PostgreSQL span
    ├── PostgreSQL span
    └── Additional PostgreSQL spans
```

### Verified Trace Results

| Property | Result |
|---|---|
| Service | ronditrack-api |
| HTTP operation | POST .../payout |
| Business span | payout.process |
| Database spans | PostgreSQL |
| Total spans | 14 |
| Trace depth | 3 |
| Duration | 76.76 ms |
| Trace ID | 3709ebca69d236abfef0c8d3b184996e |

The trace confirms that PostgreSQL operations are nested inside the `payout.process` business span.

### Custom Trace Attributes

| Attribute | Description |
|---|---|
| payout.operation | Identifies the payout operation |
| payout.outcome | Indicates success or failure |

Custom trace attributes do not include passwords, authentication tokens, email addresses, or financial amounts.

### Screenshot Evidence

The Aspire Dashboard screenshot is stored at:

`docs/aspire-payout-trace.png`

It demonstrates the HTTP request, custom payout span, and nested PostgreSQL database spans.

## 3. Business Metrics

RondiTrack uses an OpenTelemetry Meter named `RondiTrack.Business`.

### Metrics Inventory

| Metric | Type | Description | Attributes |
|---|---|---|---|
| ronditrack.contributions.recorded | Counter | Counts successfully recorded contributions | None |
| ronditrack.payouts.processed | Counter | Counts payout processing outcomes | outcome |
| ronditrack.authorization.refusals | Counter | Counts authorization refusals | result |

### Bounded Attribute Values

| Attribute | Allowed Values |
|---|---|
| outcome | success, failure |
| result | not_found, forbidden |

These attributes have low cardinality and do not expose personal information.

The contribution counter increments after a successful contribution.

The payout counter increments for successful and failed payout operations.

The authorization refusal counter increments when access to a stokvel resource is denied.

## 4. Structured Logging and POPIA

RondiTrack uses structured logging to record operational and security events while limiting exposure of personal information.

### Logging Safeguards

- Passwords are not intentionally logged.
- Access tokens and refresh tokens are not intentionally logged.
- Authentication failures are recorded without supplied credentials.
- Refresh-token reuse is logged as a security event without the token value.
- Authorization refusals are recorded without authentication secrets.
- Request completion logs contain the HTTP method, status code, and correlation ID.
- EF Core database command parameter values are not logged.
- Expected client errors are not logged as server exceptions.

### Privacy Audit

| Area | Logged Information | Privacy Protection |
|---|---|---|
| HTTP requests | Method, status, correlation ID | No request bodies or tokens |
| Successful login | User ID | No password or token |
| Failed login | Security event | No supplied credentials |
| Refresh-token reuse | Security event | No token value |
| Authorization refusal | User ID, result, trace information | No authentication secrets |
| Database operations | SQL command information | Parameter values hidden |
| API errors | Error type, correlation ID | Expected errors handled without unnecessary exception details |
| Payout tracing | Operation and outcome | No personal information in custom tags |

### POPIA Verification

Integration tests inspect captured application logs to check that sensitive values are not exposed during authentication and contribution workflows.

The tested values include:
- Email addresses.
- Passwords.
- Access tokens.
- Refresh tokens.
- A distinctive contribution amount.

These tests verify the covered logging scenarios but do not constitute a complete legal POPIA compliance audit.

## 5. Error Correlation

RondiTrack uses W3C trace identifiers to correlate API errors with telemetry.

Error responses include a `correlationId`.

Example:

```json
{
  "type": "about:blank",
  "title": "Unauthorized",
  "status": 401,
  "correlationId": "<trace-id>"
}
```

When a request contains a valid W3C `traceparent` header, its trace ID is preserved.

Correlation IDs are included in:
- Global exception responses.
- Controller validation errors.
- Authentication failures (401).
- Authorization failures (403).

Integration tests verify trace correlation behavior.

## 6. Health Checks

RondiTrack provides two anonymous health endpoints.

| Endpoint | Purpose | Authentication |
|---|---|---|
| GET /health/live | Checks whether the application is running | Not required |
| GET /health/ready | Checks PostgreSQL database availability | Not required |

### Liveness

A healthy application returns:

```text
HTTP 200
Healthy
```

### Readiness

When PostgreSQL is available:

```text
HTTP 200
Healthy
```

When PostgreSQL is unavailable:

```text
HTTP 503
Unhealthy
```

The database readiness check has a configured timeout of 10 seconds.

### Measured Health-Check Results

| Scenario | HTTP Status | Response | Duration |
|---|---|---|---|
| Initial request with database running | 200 | Healthy | 3.577 s |
| Subsequent request with database running | 200 | Healthy | 0.036 s |
| Database stopped | 503 | Unhealthy | 0.120 s |
| Database restarted | 200 | Healthy | 0.084 s |

These measurements confirm that readiness reflects PostgreSQL availability.

Health endpoints are excluded from HTTP tracing.

## 7. Running Aspire Dashboard

Aspire Dashboard is used to inspect OpenTelemetry traces, metrics, and logs.

### Prerequisites

- .NET 10 SDK.
- Docker Desktop.
- PostgreSQL.
- Configured RondiTrack database connection string.

### Step 1 — Start Aspire

Run the following command:

```bash
docker run -d --name ronditrack-aspire \
  -p 18888:18888 \
  -p 18889:18889 \
  -e DOTNET_DASHBOARD_UNSECURED_ALLOW_ANONYMOUS=true \
  -e ASPIRE_DASHBOARD_OTLP_ENDPOINT_URL=http://0.0.0.0:18889 \
  mcr.microsoft.com/dotnet/aspire-dashboard:latest
```

Open the dashboard:

http://localhost:18888

**Note:** Anonymous access is enabled for local development only. Do not expose the dashboard publicly.

### Step 2 — Configure OTLP

In Git Bash, run:

```bash
export OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:18889
export OTEL_EXPORTER_OTLP_PROTOCOL=grpc
```

### Step 3 — Run the API

```bash
dotnet run
```

### Step 4 — Generate Telemetry

Send an HTTP request:

```bash
curl -i http://localhost:5274/api/users
```

The API port may differ depending on the local configuration.

### Step 5 — View Traces

Open Aspire Dashboard and select **Traces**.

To inspect payout tracing, execute a successful payout or run the payout integration test.

The payout trace should contain:

```text
HTTP POST
  └── payout.process
        └── PostgreSQL spans
```

### Step 6 — Run the Payout Test

In a terminal where the OTLP environment variables are configured:

```bash
dotnet test RondiTrack.Tests/RondiTrack.Tests.csproj --filter "FullyQualifiedName~PayoutMetricTests"
```

Refresh Aspire Dashboard after the test completes.

### Step 7 — Stop or Restart Aspire

Stop:

```bash
docker stop ronditrack-aspire
```

Restart:

```bash
docker start ronditrack-aspire
```

## 8. Integration Testing

Observability tests use:
- xUnit.
- ASP.NET Core WebApplicationFactory.
- PostgreSQL Testcontainers.

### Test Coverage

| Test Class | Purpose |
|---|---|
| ObservabilityTests | Verifies liveness, readiness, and W3C trace correlation |
| PayoutMetricTests | Verifies successful payout metric increments |
| PopiaLoggingTests | Checks for sensitive information in captured logs |
| StokvelAuthorizationTests | Verifies authorization behavior and refusal scenarios |

### Run All Tests

```bash
dotnet test RondiTrack.Tests/RondiTrack.Tests.csproj
```

### Run Observability-Related Tests

```bash
dotnet test RondiTrack.Tests/RondiTrack.Tests.csproj --filter "FullyQualifiedName~ObservabilityTests|FullyQualifiedName~PayoutMetricTests|FullyQualifiedName~PopiaLoggingTests|FullyQualifiedName~StokvelAuthorizationTests"
```

### Verified Payout Test Result

```text
Test Run Successful.
Total tests: 1
Passed: 1
Failed: 0
```

The payout integration test verifies that a successful payout increments `ronditrack.payouts.processed` with `outcome=success`.

Aspire Dashboard also confirmed an end-to-end payout trace containing 14 spans, including nested PostgreSQL activity.

## 9. Telemetry Configuration

| Setting | Description |
|---|---|
| OTEL_EXPORTER_OTLP_ENDPOINT | OpenTelemetry collector endpoint |
| OTEL_EXPORTER_OTLP_PROTOCOL | OTLP transport protocol |
| ConnectionStrings:RondiTrack | PostgreSQL connection string |
| Jwt:Key | JWT signing key |

OTLP export is enabled only when an endpoint is configured.

Secrets must not be committed to source control.

## 10. Verification Summary

| Requirement | Status |
|---|---|
| OpenTelemetry service registration | Implemented |
| HTTP tracing | Verified in Aspire |
| Health endpoint tracing exclusion | Configured |
| Custom payout.process span | Verified in Aspire |
| Nested PostgreSQL spans | Verified in Aspire |
| Contribution metric | Implemented |
| Payout metric | Verified by integration test |
| Authorization refusal metric | Implemented |
| Bounded telemetry attributes | Implemented |
| W3C trace correlation | Covered by integration tests |
| POPIA-conscious logging | Covered by integration tests |
| Anonymous liveness endpoint | Verified |
| PostgreSQL readiness endpoint | Verified |
| Database failure readiness response | Verified |
| Aspire screenshot | Captured |

---






====================================================================================================================================





---

# Assignment 6.3 — Ready for Frontend

This assignment prepares the RondiTrack API for integration with a frontend application.

## 1. API Versioning

All API endpoints use the `/api/v1` prefix.

Examples:

- `POST /api/v1/auth/register`
- `POST /api/v1/auth/login`
- `GET /api/v1/stokvels`
- `GET /api/v1/me/home`

API routes are centralized in `Constants/ApiRoutes.cs` to ensure consistency.

## 2. CORS Configuration

The API supports requests from the development frontend:

`http://localhost:3000`

The allowed origin is configured in `appsettings.Development.json`.

The CORS policy:

- Allows GET, POST, PUT, PATCH, and DELETE requests.
- Allows the `Content-Type`, `Authorization`, and `Idempotency-Key` headers.
- Supports credentialed requests.
- Exposes the `Retry-After` and `Idempotency-Replayed` response headers.
- Handles browser preflight requests.
- Includes appropriate CORS headers on 401 and 403 responses.

Production origins must be explicitly configured before deployment.

### Authentication Cookies

Refresh tokens are stored in HttpOnly, Secure cookies.

The cookies use:

- `HttpOnly = true`
- `Secure = true`
- `SameSite = Strict`
- A restricted authentication cookie path

The frontend must use `credentials: "include"` when making requests that require cookies.

Because the refresh cookie uses `SameSite=Strict`, the frontend and API should be deployed on the same site.

For production, a same-origin deployment or same-site deployment behind a reverse proxy is recommended.

Cross-origin CORS support does not override browser SameSite cookie restrictions.

## 3. Rate Limiting

Rate limiting protects authentication and financial operations.

### Login Rate Limiting

Endpoint:

`POST /api/v1/auth/login`

The login endpoint uses a fixed-window rate limiter.

Default configuration:

- 5 requests per IP address
- 60-second window
- No request queue

### Financial Rate Limiting

Financial operations use a per-user token-bucket rate limiter.

Default configuration:

- 20 requests per authenticated user
- 20-token burst capacity
- 20 tokens replenished every 60 seconds
- No request queue

### Rate Limit Responses

When a rate limit is exceeded, the API returns:

`429 Too Many Requests`

The response includes:

- A `Retry-After` header.
- A ProblemDetails JSON response.
- A `correlationId` for tracing.

Rate limiting runs before the controller action so rejected requests do not execute financial operations or consume idempotency keys.

## 4. Frontend Home Endpoint

Endpoint:

`GET /api/v1/me/home`

This endpoint returns a summary of the authenticated user's stokvel memberships.

Each summary includes:

- Stokvel ID
- Stokvel name
- The user's role
- Current contribution cycle
- Contribution cycle status
- Amount paid during the current cycle
- Amount still owed
- Next payout date, when available

### Privacy

The endpoint uses the authenticated user's identity.

Users only receive information about stokvels to which they belong.

The endpoint does not expose other members' private financial information.

### JavaScript Compatibility

Monetary amounts are returned as decimal strings to avoid JavaScript floating-point precision problems.

Dates are represented using UTC timestamps.

Missing information is represented using nullable fields where appropriate.

## 5. Error Handling

The API uses ProblemDetails responses for errors.

Error responses include appropriate HTTP status codes and a correlation ID for troubleshooting.

Validation errors use camelCase property names to match frontend JSON conventions.

Internal server errors do not expose sensitive implementation details.

JSON number handling is configured to be strict.

Numeric values remain numeric in the API contract unless intentionally represented as strings, such as monetary amounts.

## 6. OpenAPI and TypeScript Client

The API exposes an OpenAPI document during development.

OpenAPI endpoint:

`/openapi/v1.json`

The generated OpenAPI specification is used to produce TypeScript types.

The TypeScript client is located in:

`clients/typescript/`

### Dependencies

The client uses pinned versions:

- `openapi-typescript`: 7.10.1
- `typescript`: 5.9.3

### Generate TypeScript Types

Start the API:

```bash
dotnet run
```

From the project root, download the OpenAPI document:

```bash
curl http://localhost:5274/openapi/v1.json -o openapi.json
```

The URL and port must match the address on which the API is running.

Then generate the TypeScript definitions:

```bash
cd clients/typescript
npx openapi-typescript ../../openapi.json -o src/api.d.ts
```

### TypeScript Type Checking

Run:

```bash
npm run typecheck
```

This runs TypeScript with strict type checking without generating JavaScript output.

### Typed Frontend Example

The file:

`clients/typescript/src/home.ts`

contains a typed function for calling:

`GET /api/v1/me/home`

The function:

- Uses types generated from OpenAPI.
- Sends the JWT access token in the Authorization header.
- Includes credentials for cookie-based authentication.
- Returns a typed response.

### Contract Change Verification

A deliberate backend property rename was tested.

The process was:

1. Rename a property in the C# response DTO.
2. Regenerate the OpenAPI specification.
3. Regenerate the TypeScript definitions.
4. Run TypeScript type checking.
5. Observe the TypeScript compilation error.
6. Restore the original property and regenerate the types.

This demonstrates that frontend code can detect breaking API contract changes during compilation.

## 7. Integration Testing

Integration tests use:

- xUnit
- ASP.NET Core WebApplicationFactory
- Testcontainers
- PostgreSQL

The test suite verifies API behavior using a real PostgreSQL test container.

Assignment 6.3 includes CORS integration tests covering:

- Successful browser preflight requests.
- CORS headers on 401 Unauthorized responses.
- CORS headers on 403 Forbidden responses.

### Run Integration Tests

From the project root:

```bash
dotnet test RondiTrack.Tests/RondiTrack.Tests.csproj
```

Latest verified result:

```text
Total: 40
Passed: 40
Failed: 0
Skipped: 0
```

### Verify TypeScript

```bash
cd clients/typescript
npm run typecheck
```

Latest verified result: Passed.

## 8. Dependency Security

NuGet dependencies were checked for known vulnerabilities.

The Entity Framework Core Design package was updated to version `10.0.13`.

Run the following command to check dependencies:

```bash
dotnet list RondiTrack.csproj package --vulnerable --include-transitive
```

Latest verified result:

`No vulnerable packages reported by the configured NuGet sources.`

---





==================================================================================================================================





---

## Assignment 6.4 — Docker, Background Services and CI

### Docker Setup

RondiTrack runs using Docker Compose with two services:

- **API:** ASP.NET Core (.NET 10), running on port 8080.
- **Database:** PostgreSQL 17, accessible only within the Docker network.

The API uses a multi-stage Dockerfile and runs as a non-root user.

To configure the application:

```bash
cp .env.example .env
```

Update `.env` with the required database credentials and application secrets.

Start the application:

```bash
docker compose up -d --build
```

Check the containers:

```bash
docker compose ps
```

Stop the application:

```bash
docker compose down
```

The API documentation is available at:

`http://localhost:8080/scalar/v1`

**Security:** The `.env` file must not be committed to Git. Only `.env.example` should be tracked.

### Monthly Contribution Cycle Closing

RondiTrack includes a background service that checks for expired contribution cycles.

The service:

- Uses the `Africa/Johannesburg` timezone to determine monthly cycle deadlines.
- Checks for expired cycles when the application starts and periodically afterward.
- Closes expired contribution cycles automatically.
- Calculates outstanding contributions for each stokvel member.
- Stores arrears as integer cents to avoid floating-point precision issues.
- Uses PostgreSQL transactions, advisory locking and unique constraints to prevent duplicate arrears during concurrent closing attempts.
- Processes previously expired cycles after application downtime.

### Housekeeping

A separate background service periodically removes:

- Expired refresh tokens from PostgreSQL.
- Expired records from the in-memory idempotency store.

Both background services use dependency injection and `TimeProvider`.

### Automated Testing

Integration tests use PostgreSQL Testcontainers.

The monthly cycle-closing tests verify:

- Expired cycles close correctly.
- Arrears are calculated correctly.
- Repeated execution does not duplicate arrears.
- Multiple expired cycles are processed.
- Concurrent closing attempts do not duplicate records.
- Future cycles remain active.

Run the tests locally:

```bash
dotnet test RondiTrack.Tests/RondiTrack.Tests.csproj
```

Docker Desktop must be running for tests that use PostgreSQL Testcontainers.

### GitHub Actions CI

The workflow is defined in `.github/workflows/ci.yml`.

It automatically:

1. Restores dependencies.
2. Builds the .NET application and test projects.
3. Runs automated tests.
4. Builds the Docker image only after the tests pass.

A failed test prevents the Docker build job from running.

### CI Evidence

Successful CI run:

https://github.com/Damian-Salley/RondiTrack/actions/runs/39505131056

Earlier failed Docker build:

https://github.com/Damian-Salley/RondiTrack/actions/runs/39504734887

The earlier failure occurred before the Dockerfile and other Assignment 6.4 changes were pushed.

**Note:** The earlier failed Docker build is not evidence of a failed pull-request test. Separate evidence is required if the assignment specifically requests a red PR test run.

**Failed pull-request CI run (intentional test failure):**

https://github.com/Damian-Salley/RondiTrack/actions/runs/39506686542
https://github.com/Damian-Salley/RondiTrack/actions/runs/39506686542?utm_source=chatgpt.com

This run demonstrates that when an automated test fails, the
Build and Test job fails and the Docker image build is skipped.






==================================================================================================================================






## Assignment 7.2 — Styled, Responsive Stokvel View

Assignment 7.2 extends the RondiTrack frontend with a responsive dashboard, contribution cycle history, status indicators, and theme support.

### Features

* **Responsive dashboard:** Stokvel and contribution cards adapt to desktop and mobile screen sizes.
* **Contribution cycle history:** Displays cycles in descending order, with the newest cycle first.
* **Expandable cycle groups:** Individual cycles can be expanded or collapsed, with an Expand all / Collapse all option.
* **Cycle statuses:** Displays Open, Closed, and Paid out badges.
* **Contribution statuses:** Displays Paid, Confirmed, and Pending badges.
* **Due-date countdown:** Open cycles display the number of days remaining until their due date.
* **Financial information:** Displays amounts collected, amounts due, and member contributions in South African Rand.
* **Theme controls:** Includes Light, Dark, and System theme options.
* **UI components:** Uses Tailwind CSS and shadcn/ui components.

### Backend Changes

The backend includes:

* An endpoint for retrieving stokvel contribution cycles.
* Cycle information including status, due date, total collected, and amount due.
* Arrears information in the authenticated user's home response.
* Development seed data demonstrating Active, Closed, and Paid cycles, including a member with outstanding arrears.

### Running the Frontend

From the `web` directory:

```bash
npm install
npm run dev
```

Open the local address displayed by Vite (configured to use port 3000).

The frontend currently uses static demonstration data to display the dashboard. Live API integration is not required for this demonstration.

### Validation

The following commands were executed successfully:

```bash
npm run typecheck
npm run lint
npm run test
npm run build
```

**Test results:** 18 tests passed across three test files.

### Responsive Design

The dashboard was visually checked using Chrome's iPhone 16 device emulation.

Stokvel cards stack vertically on mobile screens, while cycle groups and contribution information use responsive layouts.

### Screenshots

Desktop and mobile screenshots can be added here to demonstrate the dashboard layout and contribution cycle history.






==================================================================================================================================






## Assignment 7.3 — Live Data

### Overview

Assignment 7.3 connects the React frontend to the RondiTrack backend API, replacing demonstration data with authenticated, live information.

The dashboard allows authenticated users to view their stokvels, switch between stokvels, view contribution cycles, and load contributions.

### API Contract

The frontend uses:

- `openapi-typescript` to generate TypeScript definitions from the backend OpenAPI specification.
- `openapi-fetch` for typed API requests.
- TanStack React Query for fetching, caching, and managing server data.

The generated API types are stored in `web/src/api/schema.d.ts`.

To regenerate the API types, start the backend and run the following command from the `web` directory:

```bash
npm run generate
```

The frontend uses the `VITE_API_BASE_URL` environment variable to identify the backend API.

### Authentication and Session Management

Authentication is implemented in `web/src/auth/session.ts`.

The implementation includes:

- Access tokens stored in memory rather than local storage.
- Refresh tokens managed through HttpOnly cookies.
- Automatic token refresh when an API request receives HTTP 401.
- Single-flight refresh to prevent multiple simultaneous refresh requests.
- One retry of the original request after a successful refresh.
- Logout functionality that clears the authenticated session.

### Single-Flight Refresh Proof

Six authentication tests were implemented using Vitest.

The tests verify authentication behaviour, including token refresh and concurrent requests.

To demonstrate the importance of single-flight refresh, the implementation was deliberately modified to allow multiple simultaneous refresh requests.

The test correctly detected the problem.

After restoring the single-flight implementation, all authentication tests passed.

Evidence files:

- `web/auth-tests-failing.txt`
- `web/auth-tests-passing.txt`

### Live Dashboard and Stokvel Switching

The dashboard retrieves stokvel information, contribution cycles, and contributions from the backend API.

Each stokvel is identified by its unique ID.

React Query uses the selected stokvel ID when retrieving its associated information, preventing data from different stokvels from being mixed.

### Stokvel Switching Proof

A React Testing Library test was created in:

`web/src/App.test.tsx`

The test verifies that:

1. The first stokvel's contributions are displayed initially.
2. Selecting another stokvel displays its contributions.
3. Contributions belonging to the previous stokvel are no longer displayed.

The application was deliberately modified to continue requesting contributions for the first stokvel after switching.

The test failed as expected.

After restoring the correct implementation, the test passed.

Evidence files:

- `web/stokvel-switch-failing.txt`
- `web/stokvel-switch-passing.txt`

### Error Handling

The frontend includes reusable error-handling functionality:

- `web/src/api/errors.ts`
- `web/src/components/ErrorView.tsx`

The implementation provides understandable messages for common HTTP errors, including:

- 400 — Invalid request
- 401 — Expired session
- 403 — Forbidden access
- 404 — Resource not found
- 409 — Conflict
- 429 — Too many requests
- 500 — Server error

The error utilities also support correlation IDs and parsing `Retry-After` values expressed in seconds or HTTP dates.

Seven error-handling tests passed successfully.

### Shared-Phone Logout Check

The following manual verification was performed:

1. Logged into the application.
2. Selected the second stokvel.
3. Logged out.
4. Confirmed that the login screen appeared.
5. Logged back in.
6. Confirmed that the dashboard started with the first stokvel selected.

The application clears the React Query cache during logout and resets the selected stokvel when the dashboard is mounted again.

This helps prevent a subsequent user on the same device from seeing the previous user's cached dashboard information.

### React Query DevTools

TanStack React Query DevTools is included in the frontend to inspect query activity and cached data during development.

Queries for stokvel-specific data use the selected stokvel ID.

**Screenshot evidence:** Add a React Query DevTools screenshot showing the selected stokvel's query keys.

### Testing and Verification

The following checks were completed successfully:

| Check | Result |
|---|---|
| Authentication tests | 6 passed |
| Error-handling tests | 7 passed |
| Stokvel-switching test | 1 passed |
| Existing frontend tests | 18 passed |
| Total tests | 32 passed individually |
| TypeScript type checking | Passed |
| Production build | Passed |

To run the checks from the `web` directory:

```bash
npm test
npm run typecheck
npm run lint
npm run build
```

### Known Gaps

- React Query DevTools screenshot evidence must be added to the submission.
- Error messages support correlation IDs and retry delays, but complete propagation of API response headers into the dashboard error component has not yet been verified.
- The final combined test suite and lint checks should be rerun before submission.

### Demonstration

The demonstration recording should show:

1. Logging into RondiTrack.
2. Viewing live stokvel data.
3. Switching between stokvels.
4. Viewing contribution cycles and contributions.
5. Inspecting React Query DevTools.
6. Logging out and confirming the session is cleared.

The recording should be no longer than two minutes.




====================================================================================================================================





## Assignment 7.4 — Record a Contribution

### Backend
- Added a cycle-specific endpoint for recording contributions:
  `POST /api/v1/stokvels/{stokvelId}/cycles/{cycleId}/contributions`
- Validates contribution requests and amounts in cents.
- Prevents duplicate contributions.
- Requires an `Idempotency-Key` header to support safe retries.
- Handles contribution errors and rate limiting.

### Frontend
- Added a Record Contribution form to the stokvel dashboard.
- Uses React Hook Form and Zod for input validation.
- Converts South African Rand amounts to integer cents.
- Allows Treasurers to select a stokvel member.
- Allows ordinary members to record contributions for themselves.
- Prevents submissions when the contribution cycle is inactive.
- Displays submission success and error messages.
- Reuses the original idempotency key when retrying an uncertain submission.
- Refreshes dashboard data after successful submissions.

### Verification
Frontend checks:
- `npm run typecheck`
- `npm test` — 56 tests passing
- `npm run build`

Backend integration tests are included in
`RondiTrack.Tests/Assignment74ContributionTests.cs`.





======================================================================================================================================





# RondiTrack — Next.js Frontend (Assignment 8.1)

This directory contains the Next.js 16 frontend for RondiTrack, created alongside the existing React/Vite frontend in `web/`.

The application uses server-side authentication, encrypted sessions, and authenticated API requests to the ASP.NET Core backend.

## Technology Stack

* Next.js 16 (App Router)
* React 19
* TypeScript
* jose (encrypted session cookies)
* Vitest (automated testing)
* ASP.NET Core Web API
* PostgreSQL

## Setup

### Prerequisites

* Node.js 24
* npm
* Running RondiTrack ASP.NET Core backend
* PostgreSQL database configured for the backend

### Installation

From the `web-next/` directory:

```bash
npm install
```

Create a `.env.local` file containing:

```env
API_BASE_URL=http://localhost:5274
SESSION_SECRET=<your-generated-secret>
```

Generate a secure session secret using:

```bash
node -e "console.log(require('crypto').randomBytes(32).toString('base64url'))"
```

Replace `<your-generated-secret>` with the generated value.

Do not commit `.env.local` or expose the session secret.

### Running the Application

Start the ASP.NET Core backend from the repository root:

```bash
dotnet run
```

Start the Next.js frontend from `web-next/`:

```bash
npm run dev
```

Open `http://localhost:3000/login` and sign in using an existing RondiTrack account.

## Routes

| Route                  | Description                                        |
| ---------------------- | -------------------------------------------------- |
| `/`                    | Redirects to `/stokvels`                           |
| `/login`               | Sign-in page                                       |
| `/stokvels`            | Lists the authenticated user's stokvels            |
| `/stokvels/[id]`       | Displays details for an individual stokvel         |
| `/dashboard/member`    | Displays the user's stokvel membership information |
| `/dashboard/treasurer` | Displays stokvels where the user is Treasurer      |

## Authentication and Session Security

Authentication uses Next.js Server Actions and the existing ASP.NET Core authentication endpoints.

### Login

1. The user submits their email and password through the login form.
2. A Next.js Server Action sends the credentials to the backend.
3. The backend returns an access token and refresh token.
4. Next.js stores the tokens in an encrypted session.
5. The browser receives an HttpOnly `session` cookie.
6. The user is redirected to `/stokvels`.

### Session Cookie

The session cookie uses:

* **HttpOnly:** Prevents direct access through browser JavaScript.
* **SameSite=Lax:** Restricts cross-site cookie transmission.
* **Secure:** Enabled in production.
* **Encryption:** Session contents are encrypted using `jose`.
* **Expiration:** Seven days.

The session encryption key is configured using `SESSION_SECRET`.

### Authenticated API Requests

Backend API requests are performed by the Next.js server.

The server retrieves the access token from the encrypted session and sends it to the backend using the Bearer authentication scheme.

The browser does not receive the backend access token through the application's API request headers.

Unauthenticated users attempting to access protected pages are redirected to `/login`.

### Logout

Logout calls the backend logout endpoint, clears the Next.js session cookie, and redirects the user to `/login`.

## Treasurer Role Filtering

Treasurer permissions are evaluated separately for each stokvel.

The `getTreasurerStokvels()` function in `lib/roles.ts` filters the user's stokvels according to their individual membership roles.

For example, a user who is Treasurer in Stokvel A but Member in Stokvel B will only see Stokvel A on the Treasurer Dashboard.

This filtering controls dashboard visibility. The backend remains responsible for enforcing authorization on protected operations.

## Automated Tests

Run the Treasurer role-filtering tests:

```bash
npm run test
```

The tests verify:

1. Only stokvels with the Treasurer role are returned.
2. Non-Treasurer stokvels are excluded.
3. An empty input returns an empty result.

**Result:** 3 tests passed.

## Quality Checks

Run:

```bash
npm run lint
npm run build
npm run test
npm audit
```

Assignment 8.1 verification results:

| Check            | Result            |
| ---------------- | ----------------- |
| ESLint           | Passed            |
| Production build | Passed            |
| TypeScript       | Passed            |
| Vitest           | 3/3 tests passed  |
| npm audit        | 0 vulnerabilities |

## Manual Security Verification

The following checks were completed using the running application and Chrome Developer Tools:

* Login successfully creates an authenticated session.
* The browser receives a `session` cookie with HttpOnly enabled.
* Authenticated pages load successfully.
* The browser's `/stokvels` request does not contain an `Authorization: Bearer` header.
* Logout clears the authenticated session.
* Accessing a protected route after logout redirects to `/login`.

## Project Structure

```text
web-next/
├── app/
│   ├── actions/
│   │   └── auth.ts
│   ├── dashboard/
│   │   ├── member/
│   │   └── treasurer/
│   ├── login/
│   ├── stokvels/
│   │   └── [id]/
│   ├── layout.tsx
│   └── page.tsx
├── lib/
│   ├── api.ts
│   ├── roles.ts
│   ├── roles.test.ts
│   └── session.ts
├── .env.local
└── package.json
```

`.env.local` is a local configuration file and must not be committed.

## Future Development

Assignment 8.2 will extend this application with additional features, including improved session refresh handling, contribution submission, and streaming server-rendered content.

The existing React/Vite frontend remains available in `web/`.
