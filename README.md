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