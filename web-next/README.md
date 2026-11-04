# RondiTrack web-next

Next.js App Router frontend for the existing .NET API. Authentication stays in the encrypted HttpOnly session; Server Actions and BFF handlers add the API token on the server.

## Run locally

Use Node.js and the .NET 10 SDK, with PostgreSQL available to the API.

1. From the repository root, apply the new schema to your development database:
   `dotnet ef database update --project RondiTrack.Infrastructure --startup-project .`
2. Start the API with its existing connection string and JWT configuration: `dotnet run`.
3. In `web-next/.env.local`, configure `API_BASE_URL` with the API origin and `SESSION_SECRET` with a random base64url-encoded 32-byte key. Keep this file private.
4. In `web-next`, run `npm ci`, then `npm run dev`.

Treasurers can create an invite from their stokvel details page. Members open **Proof of payment** on a contribution, including older contributions in **Contribution history and payment proofs**. Only a stokvel's treasurers see **Close cycle**.

## API check

The original architecture had memberships, contributions, monthly closing and unique cycle/member arrears, but no invite, proof, close-preview or manual close endpoints. Assignment 9.1 adds these using the existing DbContext, models, authorization identity, service layer, Problem Details and PostgreSQL migrations. Automated integration tests exercise real HTTP requests against isolated PostgreSQL databases.

All paths below start with `/api/v1` and require a signed-in user.

| Check | Implemented contract |
| --- | --- |
| Create, preview and join invites | A treasurer of that stokvel creates a seven-day invite using `POST /stokvels/{id}/invites` with `rules` and `schedule`. `GET /invites/{code}` previews it; `POST /invites/{code}/join` joins as the token's own user. Unknown code: 404; expired code: 410; preview for an existing member: 200 with `alreadyMember: true`. Repeated/concurrent joins return 200 and keep one membership. Preview/join use the existing Financial rate-limit policy (429 when exhausted). |
| Every preview field | `stokvelId`, `name`, `contributionAmountCents`, `schedule`, `rules`, `memberCount`, `alreadyMember`. This is an explicit response DTO: no member identities, balances or payout order. |
| Upload and download | `POST`, `GET` and `DELETE /stokvels/{id}/cycles/{cycleId}/contributions/{memberId}/proof`. Upload uses multipart field `file`. JPEG, PNG and PDF only, up to 5 MiB. The API recognizes byte signatures, ignoring supplied filename and MIME type. |
| Proof visibility | Only the uploader and a Treasurer belonging to the contribution's stokvel may download. Other members, Chairpersons without that Treasurer role, and treasurers of another stokvel receive the same 404 as an absent proof. A member uploads/replaces/removes their own proof only. |
| Close preview | `GET /contributioncycles/{id}/close-preview` returns `cycleId`, `cycleName`, `asOfDate` (Johannesburg calendar date), `version`, `members` (`userId`, `name`, `amountCents`) and `totalCents`. The shared `CycleClosing.OutstandingCents` calculation is used by previews, manual closes and the existing monthly job. |
| Changed preview and repeated close | `POST /contributioncycles/{id}/close` requires JSON `{ "version": "..." }` and `Idempotency-Key`. Changed inputs return 409 with no arrears written. A durable receipt returns the first 200 result for an identical retry. Reusing the key for a different close conflicts. Non-treasurers receive 403. |

Proof metadata at the same path plus `/details` returns only `hasProof`, `canUpload` and `locked` after checking contribution access. It never returns file bytes or public links. API responses are not cacheable.

## Join flow

The state machine is the `JoinStep` union and `transition` function in `lib/join-flow.ts`. The step and code live in query parameters (`/join?code=...&step=rules`) so the invite itself, reload and browser Back use the same state without a second store.

| Step | Shows | Moves on |
| --- | --- | --- |
| `code` (1 of 3) | A prefilled invite code; unknown/expired/rate-limit feedback | Checking the code navigates to `rules`; the server validates the invite. |
| `rules` (2 of 3) | Name, rules, amount, schedule and member count | Agreeing goes to `confirm`; Back keeps the code. |
| `confirm` (3 of 3) | The same rules, amount and schedule, with no repeated inputs | A Server Action joins with the person's session token. Pending submission is disabled; database serialization and the membership composite key protect retries. |

Missing/invalid codes cannot stay on rules/confirm: the server redirects to the code step with the appropriate message. Existing members receive a direct stokvel link. Expired and unknown invites are ordinary screens, and 429 asks the person to wait. Every step heading receives focus and announces its number; the list uses `aria-current="step"`.

The code form uses Next.js `Form` with push navigation so both steps remain in browser history. `/join` is protected by `proxy.ts`. Sign-in carries the complete original path/query in `next`; `safeNext` accepts local paths only and rejects external destinations, backslashes and control characters.

## Uploads

| Hop | Limit | What happens above it |
| --- | --- | --- |
| Browser file input | 5 MiB file (5,242,880 bytes), JPEG/PNG/PDF | A specific message under the input; no request is submitted. |
| Next.js proxy buffering | Existing default: 10 MiB | Larger raw requests exceed buffering; supported files are well below this. |
| Next.js Server Action | 6 MiB total request (`serverActions.bodySizeLimit`) | Framework rejects a deliberately bypassed oversized request. Normal users see the browser's inline 5 MiB message first. |
| API HTTP/multipart parser | 6 MiB total request | 413 for a request exceeding the parser limit; browser validation normally prevents it. |
| API file validation | 5 MiB file; JPEG/PNG/PDF byte signatures | 400 with a file message, shown under the same input. A renamed text file is rejected even when it claims to be PDF. |

The extra 1 MiB at transport hops covers multipart boundaries and fields. The integration test uploads a file one byte below the API file limit. Upload errors from both browser and API appear next to the input. Successful uploads produce a dismissible toast.

Downloads stream through `/bff/stokvels/{id}/cycles/{cycleId}/contributions/{memberId}/proof`. The handler checks the session, adds its token and streams an attachment with `X-Content-Type-Options: nosniff` and `Cache-Control: private, no-store`. File bytes live in the private database, never in `public/` or a public storage URL. Download links are offered for one's own contributions or when one's role in that particular stokvel is Treasurer; the API independently enforces access.

When a cycle is no longer Active, upload/replacement/removal is refused with 409 and the UI offers downloads only. Closing and file changes share the same database lock, so a replacement cannot slip through a concurrent close.

## Closing a cycle

The preview contains the affected names, each outstanding amount in integer cents, the total, the South African as-of date and a SHA-256 version over the calculation inputs. The dialog renders these values directly using the existing `formatCents`; it does not calculate arrears in the browser. An empty affected-member list explicitly says nobody goes into arrears.

The native modal has `role="alertdialog"`, a consequence description and initial focus on Cancel. Escape/cancel returns focus to the opener. While the close is executing, dismissal is disabled until the answer arrives. Loading uses name/amount skeleton rows, and confirmation stays disabled until a preview exists. The button names the cycle and total arrears it will record.

A close acquires a PostgreSQL transaction lock shared with contribution recording and the existing monthly job, plus table locks protecting membership/cycle inputs. It calculates and checks the version inside that transaction before saving the close, arrears and idempotency receipt together. Contributions recheck cycle status while holding the same lock. The existing unique cycle/member arrears index remains the final duplicate safeguard.

A 409 keeps the dialog open, displays the reason and loads a new preview. The treasurer must confirm again with a new key. Unexpected failures keep the original version and key for retry, including after reopening the dialog. A 403 stays an inline permission message instead of signing the user out.

## Feedback

- Inline messages: incorrect invite, expired invite, rate limiting, expected authorization/conflict responses and file validation. These sit beside the decision or input that needs attention.
- Toasts: upload/removal success; unexpected API/network failures. Failure toasts show a correlation reference and remain until explicitly dismissed. No auto-dismiss timer is used. A locally generated reference is sent with the API request and retained when no response arrives.
- Dialog: closing a cycle, because it irreversibly records money owed. Its consequence is supplied by the API and stale consent is rejected.

## Validation

Run from the repository root:

```powershell
dotnet test RondiTrack.Tests --logger 'console;verbosity=minimal'
```

Run from `web-next`:

```powershell
npm run lint
npm test
npm run build
npm audit
```

Verified on 4 November 2026:

| Check | Result |
| --- | --- |
| API build | Passed, zero warnings/errors |
| Full API suite | 75 passed |
| Frontend unit tests | 23 passed |
| ESLint | Passed, no findings |
| Production build / TypeScript | Passed |
| npm audit | Zero vulnerabilities |
| Chrome flows | Passed: sign-in return, Back twice, focus, inline file errors, near-limit upload, private downloads, per-stokvel permissions, stale close/re-confirm, proof locking |
| Unexpected failure toast | Passed: reference present, persists, explicitly dismissible |

The API suite covers invite privacy/expiry, repeated joins, proof authorization in two stokvels, byte signatures, near-limit upload, proof locking, stale close and concurrent identical close requests. Frontend unit tests cover the step transitions, safe sign-in destinations and browser file rules. The production build includes TypeScript checking.

Dependency versions in `package.json` are exact and transitive versions are locked. Compatible eligible releases are used for Next/eslint-config-next 16.4.2 (15 October), jose 6.2.13 (20 October) and postcss 8.5.31 (18 October). The dependency graph was initially resolved with a 21 October cutoff. See the security exception below.

## Known gaps

- The assignment's two-week age rule and zero-audit rule conflict on 4 November 2026. npm reports [GHSA-g3hv-75wg-m249](https://github.com/advisories/GHSA-g3hv-75wg-m249) for every minimatch version below 11.0.1; that first fix was published on 27 October. The implementation prioritizes zero vulnerabilities with an exact `minimatch: 11.0.1` override and documents the age exception rather than leaving the reported vulnerability. Its required dependency graph may also contain newer releases.
- No screen recording is included. The requested video-encoder download was declined. A recording still needs to be added to the eventual hand-in.
- No PR has been opened or changes pushed: the user's branch/publishing restrictions take precedence over the PDF's submission instructions. `ASSIGNMENT-9.1-PR.md` is a local draft for the eventual PR.
- Supported image formats are JPEG and PNG; other image formats are intentionally rejected. Upload progress and invite revocation are optional stretches and are not implemented.
