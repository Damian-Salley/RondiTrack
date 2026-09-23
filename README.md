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



=================================================================================================================================================



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