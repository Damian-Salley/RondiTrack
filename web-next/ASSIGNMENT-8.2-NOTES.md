# Assignment 8.2 — Implementation Notes

## 1. Cache Components

Next.js Cache Components and Partial Prefetching are enabled.

The application uses React Suspense boundaries to stream authenticated stokvel information while keeping the page shell available.

The stokvel details page separates its main content from the recent contributions section so that independent data can be rendered as it becomes available.

Authenticated data is fetched dynamically rather than stored in shared caches.

## 2. Private Data and Caching

| Data                | Caching strategy                | Reason                            |
| ------------------- | ------------------------------- | --------------------------------- |
| Authenticated user  | `no-store`                      | Contains personal information     |
| User's stokvels     | `no-store`                      | Membership information is private |
| Stokvel members     | `no-store`                      | Contains member identities        |
| Contributions       | `no-store`                      | Contains financial records        |
| Contribution cycles | `no-store`                      | Cycle information may change      |
| Public page shell   | Static rendering where possible | Does not expose private data      |

Private API requests use:

`cache: "no-store"`

This prevents the application from intentionally storing authenticated API responses in Next.js's shared Data Cache.

The application does not intentionally place user IDs, email addresses, access tokens, refresh tokens, or other personal information in cache tags or cache keys.

### Privacy considerations

Financial and membership information must only be accessible to authorized users.

The application therefore:

* Requires authentication for protected pages.
* Uses backend authorization to verify stokvel membership.
* Prevents nonmembers from accessing private stokvel information.
* Avoids shared caching of authenticated API responses.
* Uses secure, HTTP-only cookies for session information.

## 3. Parallel Fetching and Streaming

The stokvel details page uses `Promise.all()` to execute independent API requests concurrently.

### Access verification

Before loading private stokvel information, the page calls:

`getStokvel(stokvelId)`

If the backend indicates that the user cannot access the stokvel, the page returns a Next.js 404 response.

This prevents unauthorized users from loading the remaining private information.

### Concurrent requests

After access has been verified, the page starts loading recent contributions.

It then fetches the remaining information concurrently:

```typescript
const contributionsPromise = getRecentContributions(stokvelId);

const [cycles, members, homeStokvels] = await Promise.all([
    getContributionCycles(stokvelId),
    getStokvelMembers(stokvelId),
    getHomeStokvels(),
]);
```

### Suspense streaming

Recent contributions are rendered inside a separate Suspense boundary.

While the contributions request is pending, the application displays a loading placeholder.

This allows the main stokvel information to render without waiting for the contributions section to finish.

### Why parallel fetching is used

Sequential requests must wait for each preceding request to complete.

Parallel requests can execute concurrently when they do not depend on one another.

This can reduce total waiting time, particularly when individual API requests have significant latency.

However, parallel execution is not guaranteed to be faster in every environment.

## 4. Real Backend Performance Benchmark

A real backend benchmark was performed to compare sequential and parallel execution of authenticated API requests.

The earlier simulated benchmark has been replaced with measurements obtained from the running RondiTrack backend.

### Benchmark environment

| Property            | Value                   |
| ------------------- | ----------------------- |
| Backend             | ASP.NET Core (.NET 10)  |
| Database            | PostgreSQL              |
| API address         | `http://localhost:5274` |
| Stokvel ID          | `9002`                  |
| Authentication      | JWT access token        |
| Request type        | Authenticated GET       |
| Number of endpoints | 4                       |
| Iterations          | 5                       |
| Measurement         | Median execution time   |

### Endpoints measured

The benchmark executes the following read-only requests:

```http
GET /api/v1/stokvels/9002

GET /api/v1/stokvels/9002/members

GET /api/v1/stokvels/9002/contributions

GET /api/v1/me/home
```

All four endpoints must return successful responses before measurements begin.

The benchmark does not create, update, or delete contributions or other financial records.

### Benchmark results

| Execution approach  | Median duration |
| ------------------- | --------------: |
| Sequential requests |         22.8 ms |
| Parallel requests   |         25.0 ms |
| Measured speedup    |           0.91× |

**Result:** Parallel execution was approximately 2.2 ms slower than sequential execution in this local test.

### Interpretation

The benchmark did not demonstrate a performance improvement from parallel fetching in the tested environment.

The backend and database were running locally, resulting in very low request latency.

Under these conditions, the overhead of executing multiple requests concurrently can outweigh the time saved through parallel execution.

The measured difference was small, and five iterations are insufficient to establish that sequential fetching is consistently faster.

Parallel fetching remains useful because it allows independent requests to execute concurrently, which can reduce waiting time when network or backend latency is higher.

### Measurement limitations

These results measure the combined execution time of four backend API requests.

They do not measure:

* Complete browser page-loading time.
* React rendering performance.
* Network performance under production conditions.
* The time required to establish an authenticated session.
* The initial stokvel access check performed by the Next.js page.

The benchmark therefore demonstrates the performance characteristics of the API requests in the local development environment, not an overall page-load speedup.

### Reproducing the benchmark

From the `web-next` directory, run:

```bash
node scripts/run-benchmark.mjs
```

The benchmark runner authenticates locally and executes:

```bash
node scripts/check-assignment-82.mjs
```

The benchmark uses a valid access token without intentionally printing it or saving it to disk.

**Security note:** The current temporary benchmark runner echoes the password during entry. This must be corrected or the runner removed before final submission.

## 5. Contribution API

### Endpoint

```http
POST /api/v1/stokvels/{stokvelId}/cycles/{cycleId}/contributions
```

### Request body

```json
{
  "userId": 123,
  "amountInCents": 25000
}
```

### Required header

```http
Idempotency-Key: <unique-request-key>
```

The request uses integer cents to avoid floating-point precision problems when handling financial amounts.

For example:

| Amount  | Value sent |
| ------- | ---------: |
| R0.10   |       `10` |
| R1.00   |      `100` |
| R250.00 |    `25000` |

### Frontend validation

The contribution form validates amounts before submitting them.

The application uses Zod validation and converts valid rand amounts into integer cents.

The form also prevents users from submitting invalid or nonpositive contribution amounts.

### Idempotency

Each contribution request includes an idempotency key.

The backend associates the key with the request payload.

If the same key is submitted again with the same payload, the backend can return the previously recorded contribution rather than creating another one.

If the same key is reused with a different payload, the backend rejects the request with an idempotency conflict.

This protects against accidental duplicate processing.

### Duplicate contribution protection

The backend also checks whether a member already has a contribution for the specified stokvel and cycle.

Only one contribution is permitted per member per contribution cycle.

Attempting to record another contribution for the same member and cycle results in a conflict.

This behavior was observed when attempting to record a second contribution for Sample Contributor in Community Savings, Cycle 1.

## 6. Member and Treasurer Authorization

The contribution interface changes according to the authenticated user's role.

### Member

A regular Member can record contributions for themselves.

The Member interface does not provide a dropdown for selecting another member.

The backend also prevents Members from submitting contributions on behalf of other users, even if they bypass the frontend interface.

### Treasurer

A Treasurer can select another stokvel member and record a contribution on that person's behalf.

The backend verifies that the authenticated user has the Treasurer role before permitting this operation.

### Nonmember

A user who does not belong to a stokvel cannot access its private information or record contributions for it.

The backend returns HTTP 404 for unauthorized nonmember access.

The Next.js stokvel details page displays a 404 page when access is denied.

### Authorization rules

| Scenario                                              | Expected response |
| ----------------------------------------------------- | ----------------- |
| Member records their own valid contribution           | Allowed           |
| Member records another member's contribution          | `403 Forbidden`   |
| Treasurer records another member's valid contribution | `201 Created`     |
| Nonmember attempts to record a contribution           | `404 Not Found`   |
| Member attempts a duplicate contribution              | Conflict response |

Authorization is enforced by the backend, not solely by hiding controls in the frontend.

## 7. Authorization Integration Tests

The backend authorization tests are located in:

`RondiTrack.Tests/StokvelAuthorizationTests.cs`

The tests use an isolated PostgreSQL Testcontainer.

This prevents the authorization tests from modifying the normal development database.

### Tests performed

The existing authorization tests verify Member, Treasurer, and nonmember permissions.

An additional integration test was added for the cycle-based contribution endpoint used by Assignment 8.2.

### Verified results

| Test                                 | Expected | Actual | Result |
| ------------------------------------ | -------- | ------ | ------ |
| Member records for another member    | 403      | 403    | Passed |
| Nonmember attempts a contribution    | 404      | 404    | Passed |
| Treasurer records for another member | 201      | 201    | Passed |
| Existing authorization tests         | Pass     | Pass   | Passed |

### Final test summary

```text
Test summary:
Total: 3
Passed: 3
Failed: 0
Skipped: 0

Build succeeded.
```

### Running the tests

From the repository root:

```bash
dotnet test RondiTrack.Tests/RondiTrack.Tests.csproj --filter "FullyQualifiedName~StokvelAuthorizationTests" --verbosity normal
```

### Test environment configuration

The integration-test application uses a separate PostgreSQL container.

Its login rate limit is increased to 100 requests per 60 seconds to prevent the test suite's repeated authentication requests from triggering HTTP 429.

This override applies to the test factory.

The normal application rate-limiting configuration remains unchanged.

## 8. Authentication and Session Management

The application uses JWT access tokens and refresh tokens.

### Access tokens

The backend issues short-lived JWT access tokens.

Access tokens expire after approximately 15 minutes.

The Next.js application uses the access token when making authenticated backend requests.

### Refresh tokens

The backend issues refresh tokens that can be used to obtain new access tokens.

Refresh tokens are rotated when successfully used.

The backend tracks refresh-token families and detects reuse of previously consumed tokens.

### Next.js session

The Next.js application stores encrypted session information in an HTTP-only cookie.

Session encryption uses the `jose` library.

The session contains the information needed by the server to perform authenticated API requests and refresh access tokens.

The session cookie uses secure cookie settings, including:

* HTTP-only access.
* SameSite protection.
* Secure transport in production.
* A defined expiration period.

### Protected routes

The Next.js proxy protects authenticated stokvel and dashboard routes.

Unauthenticated users are redirected to the login page.

### Refresh coordination

The application includes a refresh coordinator to reduce the risk of concurrent requests attempting to rotate the same refresh token.

Requests sharing the same refresh token can reuse an in-progress refresh operation within the same server process.

### Known limitations

The current refresh coordinator uses in-memory state.

This means coordination is limited to a single running server process.

It does not provide distributed coordination across multiple Next.js instances.

Additional edge-case testing is still required for refresh-token expiry, stale browser cookies, concurrent tabs, and logout behavior.

The implementation should not be described as fully race-proof across multiple server instances.

## 9. Error Handling

The contribution workflow handles common API errors and displays feedback to the user.

### Important responses

| HTTP status | Meaning                                        |
| ----------- | ---------------------------------------------- |
| 400         | Invalid request                                |
| 401         | Authentication required or expired             |
| 403         | User lacks permission                          |
| 404         | Resource unavailable or inaccessible           |
| 409         | Duplicate contribution or idempotency conflict |
| 422         | Business-rule validation failure               |
| 429         | Too many requests                              |

The frontend displays appropriate feedback when a contribution cannot be recorded.

### Unknown outcomes

Network interruptions can make it unclear whether a financial request succeeded.

The application uses idempotency keys to reduce the risk of duplicate processing.

However, further verification is required to ensure all ambiguous network and server-error outcomes are handled consistently before final submission.

## 10. Assignment Verification Script

The verification script is located at:

`web-next/scripts/check-assignment-82.mjs`

It checks:

1. The frontend and backend use the expected contribution API route.
2. Contribution requests include an idempotency key.
3. Contribution amounts are represented in integer cents.
4. Private API requests use `cache: "no-store"`.
5. The stokvel details page uses parallel fetching.
6. Suspense boundaries are configured.
7. Real backend request performance can be measured.

### Running the checks

From the `web-next` directory:

```bash
node scripts/check-assignment-82.mjs
```

The static checks can run without authentication.

The real backend benchmark requires a valid access token.

If no access token is supplied, the benchmark is skipped while the static checks still run.

## 11. Remaining Verification

Before submitting Assignment 8.2, the following work remains:

* Verify session expiry and logout behavior.
* Review refresh-token race conditions and document remaining limitations.
* Confirm contribution error handling for uncertain network outcomes.
* Review private caching behavior for sensitive data.
* Run the complete backend test suite.
* Run TypeScript checks and the Next.js production build.
* Run linting and review dependency audit findings.
* Remove temporary debugging code and unnecessary files.
* Update the main README with setup instructions and implementation details.
* Prepare the final demonstration and submission.

## 12. Current Status

**Assignment 8.2 progress: 75%**

Completed milestones include:

* Registration and login.
* Encrypted session handling.
* Access-token refresh coordination.
* Member and Treasurer contribution interfaces.
* Backend contribution authorization.
* Nonmember access restrictions.
* Duplicate contribution protection.
* Parallel fetching and Suspense streaming.
* Private API caching checks.
* Real backend performance measurements.
* Authorization integration tests.

The remaining work focuses on session edge cases, final verification, security cleanup, documentation, and submission preparation.
