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

The code form uses Next.js `Form` with push navigation so both steps remain in browser history. Assignment 9.2 gives signed-out `/join` requests a generic invitation shell; inspecting an invitation and joining still require a session. Sign-in carries the complete original path/query in `next`; `safeNext` accepts local paths only and rejects external destinations, backslashes and control characters.

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
## Assignment 9.2 Plan

1. Money parsing/formatting, Johannesburg date ranges, contribution validation, safe return URLs and join transitions are already pure functions. API validation field mapping will also be pure; the form owns retry identity.
2. Vitest checks pure rules and synchronous client components with RTL. Playwright checks real cookies, redirects, authorization, totals, metadata and axe against the production build. Keyboard/NVDA and Lighthouse interaction measurements need separate human verification.
3. ContributionForm's boundary is recordContribution (fake Server Action in component tests). Its own React state, pending state and key generation stay real. No router/URL is used by this component; revalidation belongs to the action.
4. Login permits five requests per IP per 60 seconds (Program.cs). A setup project signs in once and saves state; one worker saves rotating cookies after each test. An isolated Compose project uses a fresh database, development-only test seed and throwaway environment secrets.
5. `/` redirects to `/stokvels`; `/login` and `/register` are public and indexable. `/join` will expose a generic signed-out invitation shell with noindex. `/stokvels`, `/stokvels/[id]`, `/stokvels/[id]/contributions`, `/stokvels/[id]/proof`, `/dashboard/member`, `/dashboard/treasurer`, `/activity` require sessions and noindex. `/bff/me/activity`, `/bff/stokvels/[id]/contributions`, and proof downloads require sessions and X-Robots-Tag.
6. The home screen is `/stokvels`: heading and membership cards, waiting on authenticated API data. Fonts already use next/font, though body currently chooses Arial. The production audit confirmed a streamed stokvel-name card heading as LCP; measured results are recorded below.
7. No existing payout reorder UI, sliders, swipe or dragging was found. Join navigation preserves the invite code. Login allows paste and password managers; use username/current-password autocomplete. Contribution failures must preserve input despite React's automatic form reset.

## Assignment 9.2 — Running the tests

From the repository root, with Docker Desktop running:

```powershell
dotnet build
dotnet test RondiTrack.Tests/RondiTrack.Tests.csproj
Set-ExecutionPolicy -Scope Process Bypass
. ./scripts/assignment92-stack.ps1
Set-Location web-next
npm ci
npm run lint
npm run typecheck
npx tsc --noEmit
node scripts/repeat-unit.mjs
npx playwright install chromium
npm run build
npm run test:e2e
npx playwright show-report
npm audit --omit=dev
node scripts/check-release-age.mjs
```

The execution-policy override applies only to this PowerShell process and is needed on systems that block unsigned local scripts. Dot-source the helper so generated secrets remain in that terminal. It uses only the `ronditrack-assignment92` Compose project and a nonpersistent PostgreSQL data directory. It never uses your normal Compose volume. Do not enable `Assignment92TestSeed` against existing data: it refuses a nonempty database and refuses Production. The opt-in seed migrates the database and creates one test account, 40 members and twelve months of contributions for each independently assigned test stokvel. Stokvel names are unique per seed. Browser setup signs in once per run, one worker reuses its storage state and saves rotated cookies after each test. Saved sessions and generated reports are ignored by Git.

For three independent browser runs, rebuild once, then repeat the following from the root **in the same terminal**, changing the output filename to `playwright-2.txt` and `playwright-3.txt` for the next runs:

```powershell
. ./scripts/assignment92-stack.ps1 -Action down
. ./scripts/assignment92-stack.ps1
Push-Location web-next
npm run test:e2e 2>&1 | Tee-Object ../.tmp-assignment92/playwright-1.txt
if ($LASTEXITCODE -ne 0) { throw 'Playwright failed' }
Pop-Location
```

Fresh stacks prevent a previous contribution or closed cycle from affecting another run. Each browser test has a separate seeded stokvel ID; the journey owns ID 3 and the accessibility test owns ID 4. Do not run the browser suite against your personal database. Ports 8080 and 3000 must be free.

## Assignment 9.2 — What's checked where

| Behaviour | Test/check | Tool |
| --- | --- | --- |
| Rand prefixes, grouping, NBSP, decimal digits, exact 1999 cents, maximum, refusals and zero round trip | `lib/formatters.test.ts` | Vitest |
| Positive-only contributions and API field-name mapping | `lib/formatters.test.ts`, `Assignment92MoneyTests.cs` | Vitest / xUnit |
| Real Server Action HTTP 400 mapping, exact POST cents/key, dropped connection | `lib/contribution-action.test.ts` | Vitest / MSW 3 with unhandled frames rejected |
| Aggregate totals beyond browser integer precision stay exact on the JSON wire | `Assignment92MoneyTests.cs` | xUnit serialization regression |
| SAST month boundaries, midnight deadlines, calendar filters | `lib/south-african-dates.test.ts`, `lib/formatters.test.ts`, `lib/contribution-filters.test.ts` | Vitest in UTC and Johannesburg |
| Field errors, 409/422 references, preserved values, pending clicks, retry key and next key | `app/components/ContributionForm.test.tsx` | RTL with userEvent |
| Same Sipho, two stokvel roles | `app/components/StokvelActions.test.tsx` | RTL; generated API fixtures |
| Real 403, continued authentication, redirect and JSON 401 | `e2e/assignment92.spec.ts` | Playwright / Compose API |
| Exact displayed total increase after recording | `e2e/assignment92.spec.ts` | Playwright with no reload |
| Home, stokvel, amount error and Close Cycle dialog | `e2e/assignment92.spec.ts` | axe WCAG 2.2 AA tags; serious/critical fail |
| Private noindex and signed-out invite/WhatsApp metadata | `e2e/assignment92.spec.ts` | Playwright |
| Keyboard, focus movement, speech and touch target spacing | Checklist below | Manual Windows NVDA/browser |
| LCP, CLS and interaction INP | Protocol below | Lighthouse mobile / Timespan |

No `data-testid` selectors or fixed sleeps are used. The form owns the key because it knows whether an operation is the unchanged retry or the next contribution. Its Server Action and React hooks remain real in production; component tests fake only the action boundary. A changed payload gets a fresh key, an unchanged uncertain retry keeps its key, and success clears the fields and replaces the key.

The explicit API maximum is 9,007,199,254,740,991 cents, matching the largest exact JavaScript JSON integer. The previous Int64 contract could accept values beyond browser precision. Parsing builds cents with BigInt and rejects overflow without rounding; formatting accepts integer cents. Parsing zero is useful for totals and round trips; contribution validation separately rejects zero.

### A test that found a bug

The new collected-total field is a decimal string on the JSON wire: a sum of individually valid contributions can exceed JavaScript's exact integer range. The C# regression test serializes 9,007,199,254,740,993 cents and checks the exact string, and the frontend formats it with BigInt. This field was added in 9.2; existing numeric fields retain their contracts.

Repeating the browser suite found a real test-stack startup race: PostgreSQL's Unix-socket health check could pass for its temporary initialization server before TCP was listening. The API's migration then failed with connection refused. The test Compose override checks `pg_isready -h 127.0.0.1` so the API starts only after the final TCP server is ready. Browser setup also polls API readiness without fixed sleeps and fails if it never becomes healthy.

The first refusal/retry tests failed because React automatically reset the selected member after a Server Action returned an error. The retry then failed native required-field validation instead of sending the original contribution. Controlled inputs and prevention of the automatic reset preserve both fields; success explicitly clears them. The tests wait for the returned message before asserting or retrying. A separate test-harness issue (returning a mock from beforeEach made it a cleanup callback) was corrected in the test, rather than changing production code to accommodate it.

## Assignment 9.2 — Accessibility

The following are **code-inspection/test findings**, not a claim that a screen reader or manual keyboard pass was performed. Automated axe results and final command results are recorded with the final evidence.

| Criterion | Flow | What failed | Fix | Verification/source |
| --- | --- | --- | --- | --- |
| 3.3.7 | Contribution refusal/retry | React cleared the selected member | Controlled fields; prevent automatic reset; clear after success | RTL refusal and retry regression tests |
| 3.3.1 / 4.1.3 | Contribution errors | API field errors were discarded and fields had no associations | Map AmountInCents to amount and UserId to member; aria-invalid, aria-describedby, alert and reference summary | Unit mapping + RTL association tests; browser axe error state |
| 3.3.8 | Login | Email autocomplete did not identify the login username | autocomplete=username and existing current-password; paste remains available | Code inspection; password-manager/NVDA check pending |
| 2.4.7 / 2.4.11 | All controls / long lists | No shared explicit visible focus or reserved scroll space | Visible focus outline and block scroll padding/margins | Code inspection; keyboard pass pending |
| 2.4.11 | Persistent feedback | Fixed bottom toast could cover focused controls | Keep dismissible feedback in document flow | Code inspection; keyboard pass pending |
| 2.5.8 | Compact controls | No shared 24px control minimum | Minimum width/height, including checkboxes | Code inspection; touch/spacing pass pending |
| 2.4.3 | Close Cycle | Initial Cancel could become disabled while preview loaded | Keep Cancel enabled during preview loading | Playwright focus, Escape and trigger restoration passed; manual speech pending |
| 1.4.3 | Home and stokvel (light theme) | axe found low-contrast gray/green text and dark text on blue links; global anchor colour overrode utility colours | Accessible light-theme text colours; let link utility colours apply | First automated axe run; regression rerun recorded with final results |

Existing native Close Cycle dialog supplies modal focus containment, Escape and restoration to its trigger. Join headings receive focus, steps have aria-current, and Back retains the code in the URL. Proof input errors already use accessible descriptions and status feedback. No payout reorder UI or dragging interaction exists, so a non-drag reorder implementation is not applicable; demonstrate the existing payout flow and explicitly explain that limitation in the recording.

Manual verification on Windows (pending): start NVDA, use Tab/Shift+Tab/Enter/Space only, and check login with paste and a password manager. Join with a pasted invite, go forward then Back, and confirm the code remains and each heading is announced. Record an invalid amount, hear the field error, correct it, and hear success; write down the actual words NVDA reads for an amount and a Johannesburg date. Upload a valid proof, try an invalid file, open/download it and return without losing focus. Open Close Cycle, verify initial Cancel focus, Tab containment, Escape and trigger restoration; inspect the amounts before confirming on disposable data. At a phone viewport, tab through a long history and ensure no focused control is obscured. Check 24px targets and meaningful spacing. Also check 320 CSS pixels/400% zoom and reduced motion if completing the optional stretch. Do not mark these checks done without performing them.

## Assignment 9.2 — Performance

Use `npm run build` then `npm run start`, not development mode. The isolated fixture has 40 members and a year of contributions. Measure `/stokvels` and `/stokvels/1` signed in, three mobile Lighthouse Navigation runs each, no extensions, simulated slow 4G (150ms RTT, 1,638.4 Kbps throughput), 4x CPU slowdown. Record all three values and use the middle sorted value. Save the JSON/HTML reports outside tracked files. For INP, start Lighthouse Timespan, record a contribution on a fresh test stokvel and open Close Cycle, then end the Timespan; navigation TBT is **not INP**. Record actual interaction INP, or use web-vitals onINP if the installed Lighthouse does not expose it. Run before any performance optimization and repeat after any measured fix.

| Screen / metric | Original frontend | Final frontend | Target / interpretation |
| --- | --- | --- | --- |
| Home LCP, median | 0.954s | 0.810s | <2.5s; both pass |
| Home navigation CLS, median | 0 | 0 | <0.1; both pass |
| Stokvel LCP, median | 0.975s | 0.967s | Recorded for comparison |
| Stokvel streaming navigation CLS, median | 0 | 0 | <0.1; both pass |
| Contribution + Close Cycle Timespan INP, median | 77.8ms | 67.1ms | <200ms; actual interactions, not TBT |
| Interaction Timespan CLS, median | 0.025469 | 0.038674 | Both <0.1 |

Measured with Lighthouse 13.0.1 and Playwright's Chromium 148 on the production servers. Navigation: mobile 360×640, simulated slow 4G (150ms RTT, 1,638.4 Kbps), 4× CPU. Interaction: three real contribution-and-Close-Cycle Timespans, mobile viewport, DevTools throttling (562.5ms request latency, 1,474.56 Kbps download, 675 Kbps upload), 4× CPU. Every interaction used an untouched stokvel with the same 40-member/year-long fixture shape. These are local synthetic measurements, not field data from a physical phone.

The original frontend was built from a read-only `git archive HEAD` snapshot in the ignored temporary directory; no branch switch occurred. The original and final frontend used the same installed runtime dependencies and current disposable backend/data. The snapshot's only adaptation was Turbopack's workspace root to resolve the shared dependency directory. This reconstructs a frontend baseline; it is not a claim that an original-backend production deployment was measured before work began. The earlier intermediate baseline is not substituted for the original source results.

No performance optimization was justified by the measured medians. Accessibility, exact money, privacy and revalidation fixes remain; the small timing differences are not claimed as improvements or regressions attributable to a specific optimization. The home LCP diagnostic identifies a streamed stokvel-name card heading, rather than an image. The measured navigation CLS stayed below 0.1 while contributions streamed; this is evidence for this fixture/run, not a guarantee for every device. Reports are in `.tmp-assignment92/performance/` and remain ignored because raw audit settings may contain temporary authentication headers.

Private proofs still use authenticated BFF attachment streaming; they never enter the shared next/image cache. There are no design photos needing next/image; the public social image uses next/og. Manual physical-phone verification remains useful.

## Assignment 9.2 — Metadata and indexing

| Route | Session | Indexing | Preview |
| --- | --- | --- | --- |
| `/` | Redirect to home | Redirect | Product metadata |
| `/login`, `/register` | Public | Indexable | Product title, description and social image |
| `/join?code=…` | Generic signed-out shell; session required to validate/join | noindex, nofollow | Generic invitation; no private name, member or amount |
| `/stokvels`, `/stokvels/[id]`, `/stokvels/[id]/contributions`, `/stokvels/[id]/proof` | Required | Shared `(signed-in)` layout noindex | Product name only |
| `/dashboard/member`, `/dashboard/treasurer`, `/activity` | Required | Same layout noindex | Product name only |
| All `/bff/…` routes, proof downloads | Required; JSON 401 without session | X-Robots-Tag noindex, nofollow; private no-store | No private response without session |

`generateMetadata` is used only on the invite page and never fetches the authenticated preview endpoint for a signed-out bot. It cannot safely distinguish expired/valid invites without authentication; it explicitly asks the recipient to sign in. Proxy redirects authenticated-only pages with a local next URL. Join now renders its generic shell publicly; validation and joining still require a session. No robots.txt Disallow rules are added: crawlers must be able to read noindex. Metadata is static/product-only even when signed in. Public Open Graph images contain only product text.

With a real test invite generated from the treasurer view, capture a signed-out WhatsApp head (replace CODE):

```powershell
curl.exe -sS -A 'WhatsApp/2' 'http://localhost:3000/join?code=CODE' -o .tmp-assignment92/invite.html
$html = Get-Content .tmp-assignment92/invite.html -Raw
[regex]::Match($html, '(?s)<head>.*?</head>').Value
```

Paste the actual head in the PR. Verify it has no stokvel/member name or amount. Repeat with a deployed preview checker after deployment.

## Assignment 9.2 — CI

Backend and frontend jobs run beside each other on every pull request. Frontend runs npm ci, lint, next typegen + tsc, Vitest in both time zones and a production audit. The dependent e2e job creates temporary JWT/session secrets, starts the isolated Compose stack, installs Chromium, builds production and runs browser/axe/metadata tests. Failed runs retain the HTML report and traces for seven days; treat these as private test artifacts. Compose cleanup runs even on failure. The existing Docker build job remains gated on backend tests.

Red CI URL/screenshot: **pending user GitHub action**. Green CI URL: **pending user GitHub action**. A local passing command is not evidence that GitHub CI has passed.

After reviewing the working tree, the following are **commands for you to execute**, not actions Codex performed. First stage the reviewed assignment files, commit them on feature/assignment-9.2 and push the implementation. To show red first without breaking the final branch, create a separate worktree from that reviewed commit:

```powershell
git diff --check
git add -- .dockerignore .github/workflows/ci.yml .gitignore Controllers/MeController.cs DTOs/Me/HomeStokvelResponse.cs Program.cs RondiTrack.csproj RondiTrack.Domain/Models/ContributionLimits.cs RondiTrack.Infrastructure/Database/Seeding/Assignment92TestSeeder.cs RondiTrack.Tests/Assignment92MoneyTests.cs Services/RondiTrackService.cs Validators/RecordCycleContributionRequestValidator.cs compose.e2e.yaml scripts/assignment92-stack.ps1 web-next
git diff --cached --check
git commit -m 'Implement Assignment 9.2 tests accessibility and privacy'
git push -u origin feature/assignment-9.2
git worktree add ../RondiTrack-assignment92-red -b demo/assignment-9.2-red feature/assignment-9.2
Push-Location ../RondiTrack-assignment92-red
$path = 'web-next/lib/formatters.test.ts'
$source = Get-Content -LiteralPath $path -Raw
$source.Replace('["19,99", 1999]', '["19,99", 1998]') | Set-Content -LiteralPath $path
git add -- web-next/lib/formatters.test.ts
git commit -m 'Demonstrate red CI with deliberately wrong cents expectation'
git push -u origin demo/assignment-9.2-red
gh pr create --base main --head demo/assignment-9.2-red --title 'Assignment 9.2 red CI demonstration' --body 'Deliberately wrong money expectation. Evidence only; do not merge.'
Pop-Location
# Capture the failed frontend job URL and screenshot. Close the red PR without merging.
gh pr create --base main --head feature/assignment-9.2 --title 'Assignment 9.2: Tests, performance and accessibility' --body-file web-next/ASSIGNMENT-9.2-PR.md
```

Capture the green run, paste both links into this README and the final PR description, and add the required five-minute recording. Do not merge the deliberately failing branch. The local final implementation must remain green.

## Assignment 9.2 — Known gaps

Release-age exception, discussed with the user: keep the original fixed `minimatch@11.0.1` override. The registry dates it October 27, 2026 at 22:26 UTC; it reaches 14 days on November 11 at 00:26 SAST. On November 8, all older compatible releases are affected by [GHSA-g3hv-75wg-m249](https://github.com/advisories/GHSA-g3hv-75wg-m249), so full age compliance and the available security fix cannot both be achieved today. MSW is pinned to age-compliant 3.0.3. The release-age script deliberately reports the minimatch exception until the cutoff is met; run `node scripts/check-release-age.mjs 2026-11-11T00:00:00Z` to evaluate a November 11 submission. Do not describe today's exception as a passing age check.

Manual keyboard/NVDA speech, phone target-spacing judgement, GitHub red/green evidence, recording and deployed link previews require the checks above. Synthetic mobile Navigation and interaction Timespan measurements are complete; field/device measurements are not claimed. The legacy generated API contract contains optional/string-number fields; role fixtures deliberately use that generated contract so incompatible renames fail compilation. The previous API allowed Int64 contribution values beyond JavaScript's exact integer range; the new explicit ceiling closes that compatibility gap. Final results and any remaining automated failures are recorded separately in the PR description.

### Actual automated results (November 8, 2026)

Backend: build passed with zero warnings/errors; 81 tests passed, zero failed/skipped. Frontend: standard npm ci, lint, next typegen + tsc, standalone tsc and production build passed. All 68 Vitest tests passed ten consecutive runs, alternating UTC and Africa/Johannesburg. All six Playwright tests (including setup) passed three fresh-stack production runs in 34.4s, 14.6s and 14.8s; the axe checks had no serious/critical violations and no rules disabled. Both full and production-only npm audits returned zero vulnerabilities. NuGet application/test-project transitive audits reported no vulnerable packages. Actual repeated output is pasted in `ASSIGNMENT-9.2-PR.md`; reports/traces and raw working evidence remain ignored.

Set `SITE_URL` to the deployed public origin when building for deployment so public social image URLs resolve to the correct host. Local builds default to http://localhost:3000. The standard local stack was stopped after verification; use the Running the tests commands to recreate it.


To open the successful journey trace after the browser run (from web-next):

```powershell
$journeyTrace = Get-ChildItem test-results -Filter trace.zip -Recurse | Where-Object { $_.FullName -match 'R150-50' } | Select-Object -First 1
npx playwright show-trace $journeyTrace.FullName
```
