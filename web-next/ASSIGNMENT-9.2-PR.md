# Assignment 9.2: Tests, performance and accessibility

## Implementation

Exact rand parsing accepts currency prefixes, correctly grouped spaces and non-breaking spaces. Zero round-trips through formatting but is rejected as a contribution. The browser-safe integer ceiling is now enforced by the API validator/service as well, preventing amounts that cannot survive the JSON number contract exactly.

The actual contribution form has controlled fields, prevents automatic resets on refusal, announces field errors and references, and keeps its idempotency key for an unchanged retry. Shared stokvel controls allow typed role fixtures to exercise the same view used by the page. The API supplies the current cycle's collected total as an exact decimal string (sums can exceed browser integer precision), which revalidation updates after recording. A C# serialization regression test proves 9007199254740993 cents survives exactly.

Authenticated routes share a route-group layout with noindex and product-only metadata; URLs are unchanged. Signed-out invite pages show a generic invitation and sign-in link. They do not call the protected preview API or assert invite validity. BFF responses carry noindex headers, including authentication failures and private downloads. Proofs still use private authenticated streaming, never the image optimizer.

CI preserves backend tests, adds frontend checks in UTC/Johannesburg, and gates production Playwright/axe on both. Test-only Compose seeding requires Development, an explicit opt-in, an empty database and a temporary password. Each test uses a separate seeded stokvel; names are unique per fresh stack.

## Actual results

| Check | Actual result |
| --- | --- |
| Backend build | Passed; 0 warnings, 0 errors |
| Backend tests | 81 passed, 0 failed, 0 skipped |
| npm ci | Passed |
| ESLint, next typegen + tsc, standalone tsc, production build | Passed |
| Vitest | 68 tests × 10 consecutive runs; all passed; alternating UTC/Johannesburg |
| Playwright production / axe / privacy | 6 tests including auth setup × 3 fresh-stack runs; all passed; no 429 |
| npm audit and npm audit --omit=dev | 0 vulnerabilities in each |
| NuGet application and test project audits, including transitives | No vulnerable packages reported by configured sources |
| Release age | All checked pins pass except the documented existing minimatch 11.0.1 security exception until November 11 SAST |

The first diagnostic browser run found real light-theme contrast failures, fixed without disabling axe rules. Repetition also found a PostgreSQL bootstrap health-check race, fixed by testing TCP readiness. Other test-harness fixes included clearing inherited browser state for signed-out contexts, scoped status locators, and readiness polling that handles temporary connection resets. Final results below are the subsequent consecutive passing runs, not the earlier failed attempts.

Focus outlines, control target minimums, field associations/announcements, preserved entries, password-manager autocomplete and non-obscuring feedback are implemented. Automated keyboard checks verify Close Cycle Cancel focus, Escape and trigger restoration. Manual Windows NVDA observations remain pending.

Actual performance measurements and the curl head are recorded below. No speculative performance optimization was applied; the existing production navigation medians were already within the assignment targets.

## Manual evidence still required

- [ ] Red CI evidence: Playwright initially failed because the Ubuntu 26.04 runner was incompatible with Playwright 1.60.0. Failed job: https://github.com/Damian-Salley/RondiTrack/actions/runs/42526561033/job/128871979487?pr=5. Fixed by pinning the E2E runner to Ubuntu 24.04 in commit b69a76e. Screenshot still required.

- [x] Green Assignment 9.2 PR: https://github.com/Damian-Salley/RondiTrack/pull/5 — All GitHub Actions checks passed after commit b69a76e. CI runs: https://github.com/Damian-Salley/RondiTrack/actions

- [ ] Windows NVDA speech observations for one amount, one date, errors and success.

- [x] Keyboard-only verification completed for login, dashboard navigation, member selection, contribution submission and validation, payment proof upload/access, Close Cycle dialog focus management, and invitation rules navigation. All tested interactions worked using the keyboard. The final invitation submission was not independently verified.

- [ ] Five-minute demonstration recording, including a temporary money-rule failure (restore the rule afterward), browser report/trace, accessibility, performance and CI.
- [ ] Deployed preview checker after deployment.

Follow the exact reproduction and red/green demonstration commands in `README.md`. No branch switch, commit, push, merge or PR creation was performed by Codex.

## Ten consecutive Vitest runs — actual output

```text
Run 1/10 TZ=UTC



 RUN  v4.1.12 C:/Users/Damian/Desktop/RondiTrackApp/RondiTrack/web-next





 Test Files  8 passed (8)

      Tests  68 passed (68)

   Start at  18:21:19

   Duration  4.34s (transform 713ms, setup 4.19s, import 1.71s, tests 3.00s, environment 2.87s)





Run 2/10 TZ=Africa/Johannesburg



 RUN  v4.1.12 C:/Users/Damian/Desktop/RondiTrackApp/RondiTrack/web-next





 Test Files  8 passed (8)

      Tests  68 passed (68)

   Start at  20:21:24

   Duration  3.56s (transform 483ms, setup 2.53s, import 1.17s, tests 2.54s, environment 1.67s)





Run 3/10 TZ=UTC



 RUN  v4.1.12 C:/Users/Damian/Desktop/RondiTrackApp/RondiTrack/web-next





 Test Files  8 passed (8)

      Tests  68 passed (68)

   Start at  18:21:28

   Duration  4.01s (transform 487ms, setup 3.31s, import 1.19s, tests 2.67s, environment 2.25s)





Run 4/10 TZ=Africa/Johannesburg



 RUN  v4.1.12 C:/Users/Damian/Desktop/RondiTrackApp/RondiTrack/web-next





 Test Files  8 passed (8)

      Tests  68 passed (68)

   Start at  20:21:33

   Duration  3.78s (transform 634ms, setup 2.71s, import 1.48s, tests 2.74s, environment 1.72s)





Run 5/10 TZ=UTC



 RUN  v4.1.12 C:/Users/Damian/Desktop/RondiTrackApp/RondiTrack/web-next





 Test Files  8 passed (8)

      Tests  68 passed (68)

   Start at  18:21:37

   Duration  3.64s (transform 562ms, setup 2.64s, import 1.12s, tests 2.59s, environment 1.63s)





Run 6/10 TZ=Africa/Johannesburg



 RUN  v4.1.12 C:/Users/Damian/Desktop/RondiTrackApp/RondiTrack/web-next





 Test Files  8 passed (8)

      Tests  68 passed (68)

   Start at  20:21:41

   Duration  3.71s (transform 532ms, setup 3.40s, import 1.18s, tests 2.56s, environment 1.97s)





Run 7/10 TZ=UTC



 RUN  v4.1.12 C:/Users/Damian/Desktop/RondiTrackApp/RondiTrack/web-next





 Test Files  8 passed (8)

      Tests  68 passed (68)

   Start at  18:21:45

   Duration  4.10s (transform 467ms, setup 3.47s, import 1.35s, tests 2.64s, environment 2.64s)





Run 8/10 TZ=Africa/Johannesburg



 RUN  v4.1.12 C:/Users/Damian/Desktop/RondiTrackApp/RondiTrack/web-next





 Test Files  8 passed (8)

      Tests  68 passed (68)

   Start at  20:21:50

   Duration  3.72s (transform 492ms, setup 2.76s, import 1.11s, tests 2.77s, environment 1.67s)





Run 9/10 TZ=UTC



 RUN  v4.1.12 C:/Users/Damian/Desktop/RondiTrackApp/RondiTrack/web-next





 Test Files  8 passed (8)

      Tests  68 passed (68)

   Start at  18:21:54

   Duration  3.59s (transform 562ms, setup 2.79s, import 1.11s, tests 2.55s, environment 1.74s)





Run 10/10 TZ=Africa/Johannesburg



 RUN  v4.1.12 C:/Users/Damian/Desktop/RondiTrackApp/RondiTrack/web-next





 Test Files  8 passed (8)

      Tests  68 passed (68)

   Start at  20:21:58

   Duration  3.70s (transform 539ms, setup 2.56s, import 1.17s, tests 2.53s, environment 1.66s)
```

## Three consecutive production Playwright runs — actual test output

Node colour-warning boilerplate is omitted; the following lines are copied from the captured test output.

Run 1:

```text
Running 6 tests using 1 worker
  ok 1 [setup] › e2e\auth.setup.ts:4:6 › sign in once (2.9s)
  ok 2 [chromium] › e2e\assignment92.spec.ts:14:5 › member gets real API 403 and stays signed in (2.2s)
  ok 3 [chromium] › e2e\assignment92.spec.ts:27:5 › signed-out navigation has a safe return path and BFF returns JSON 401 (499ms)
  ok 4 [chromium] › e2e\assignment92.spec.ts:42:5 › R150,50 updates the displayed total by exactly 15050 cents (1.3s)
  ok 5 [chromium] › e2e\assignment92.spec.ts:55:5 › home, private stokvel, invalid contribution and close dialog pass axe (4.7s)
  ok 6 [chromium] › e2e\assignment92.spec.ts:78:5 › real invite signed-out metadata and WhatsApp head reveal no private data (1.3s)
  6 passed (34.4s)
```

Run 2:

```text
Running 6 tests using 1 worker
  ok 1 [setup] › e2e\auth.setup.ts:4:6 › sign in once (1.5s)
  ok 2 [chromium] › e2e\assignment92.spec.ts:14:5 › member gets real API 403 and stays signed in (1.1s)
  ok 3 [chromium] › e2e\assignment92.spec.ts:27:5 › signed-out navigation has a safe return path and BFF returns JSON 401 (427ms)
  ok 4 [chromium] › e2e\assignment92.spec.ts:42:5 › R150,50 updates the displayed total by exactly 15050 cents (1.4s)
  ok 5 [chromium] › e2e\assignment92.spec.ts:55:5 › home, private stokvel, invalid contribution and close dialog pass axe (4.3s)
  ok 6 [chromium] › e2e\assignment92.spec.ts:78:5 › real invite signed-out metadata and WhatsApp head reveal no private data (1.3s)
  6 passed (14.6s)
```

Run 3:

```text
Running 6 tests using 1 worker
  ok 1 [setup] › e2e\auth.setup.ts:4:6 › sign in once (1.6s)
  ok 2 [chromium] › e2e\assignment92.spec.ts:14:5 › member gets real API 403 and stays signed in (1.3s)
  ok 3 [chromium] › e2e\assignment92.spec.ts:27:5 › signed-out navigation has a safe return path and BFF returns JSON 401 (466ms)
  ok 4 [chromium] › e2e\assignment92.spec.ts:42:5 › R150,50 updates the displayed total by exactly 15050 cents (1.2s)
  ok 5 [chromium] › e2e\assignment92.spec.ts:55:5 › home, private stokvel, invalid contribution and close dialog pass axe (4.8s)
  ok 6 [chromium] › e2e\assignment92.spec.ts:78:5 › real invite signed-out metadata and WhatsApp head reveal no private data (1.2s)
  6 passed (14.8s)
```

## Files created, modified or moved

- `.dockerignore`
- `.github/workflows/ci.yml`
- `.gitignore`
- `Controllers/MeController.cs`
- `DTOs/Me/HomeStokvelResponse.cs`
- `Program.cs`
- `RondiTrack.Domain/Models/ContributionLimits.cs`
- `RondiTrack.Infrastructure/Database/Seeding/Assignment92TestSeeder.cs`
- `RondiTrack.Tests/Assignment92MoneyTests.cs`
- `RondiTrack.csproj`
- `Services/RondiTrackService.cs`
- `Validators/RecordCycleContributionRequestValidator.cs`
- `compose.e2e.yaml`
- `scripts/assignment92-stack.ps1`
- `web-next/ASSIGNMENT-9.2-PR.md`
- `web-next/README.md`
- `web-next/app/actions/contributions.ts`
- Moved `web-next/app/activity/ActivityFeed.tsx` → `web-next/app/(signed-in)/activity/ActivityFeed.tsx` (URL unchanged).
- Moved `web-next/app/activity/page.tsx` → `web-next/app/(signed-in)/activity/page.tsx` (URL unchanged).
- `web-next/app/bff/cycles/[id]/close-preview/route.ts`
- `web-next/app/components/CloseCycle.tsx`
- `web-next/app/components/ContributionForm.test.tsx`
- `web-next/app/components/ContributionForm.tsx`
- `web-next/app/components/Feedback.tsx`
- `web-next/app/components/PaymentProofLink.tsx`
- `web-next/app/components/StokvelActions.test.tsx`
- `web-next/app/components/StokvelActions.tsx`
- Moved `web-next/app/dashboard/member/page.tsx` → `web-next/app/(signed-in)/dashboard/member/page.tsx` (URL unchanged).
- Moved `web-next/app/dashboard/treasurer/page.tsx` → `web-next/app/(signed-in)/dashboard/treasurer/page.tsx` (URL unchanged).
- `web-next/app/globals.css`
- `web-next/app/join/page.tsx`
- `web-next/app/layout.tsx`
- `web-next/app/login/LoginForm.tsx`
- `web-next/app/opengraph-image.tsx`
- Moved `web-next/app/stokvels/[id]/RecentContributions.tsx` → `web-next/app/(signed-in)/stokvels/[id]/RecentContributions.tsx` (URL unchanged).
- Moved `web-next/app/stokvels/[id]/contributions/ContributionFilters.tsx` → `web-next/app/(signed-in)/stokvels/[id]/contributions/ContributionFilters.tsx` (URL unchanged).
- Moved `web-next/app/stokvels/[id]/contributions/ContributionHistoryList.tsx` → `web-next/app/(signed-in)/stokvels/[id]/contributions/ContributionHistoryList.tsx` (URL unchanged).
- Moved `web-next/app/stokvels/[id]/contributions/page.tsx` → `web-next/app/(signed-in)/stokvels/[id]/contributions/page.tsx` (URL unchanged).
- Moved `web-next/app/stokvels/[id]/page.tsx` → `web-next/app/(signed-in)/stokvels/[id]/page.tsx` (URL unchanged).
- Moved `web-next/app/stokvels/[id]/proof/ProofForm.tsx` → `web-next/app/(signed-in)/stokvels/[id]/proof/ProofForm.tsx` (URL unchanged).
- Moved `web-next/app/stokvels/[id]/proof/page.tsx` → `web-next/app/(signed-in)/stokvels/[id]/proof/page.tsx` (URL unchanged).
- Moved `web-next/app/stokvels/page.tsx` → `web-next/app/(signed-in)/stokvels/page.tsx` (URL unchanged).
- `web-next/e2e/assignment92.spec.ts`
- `web-next/e2e/auth.setup.ts`
- `web-next/eslint.config.mjs`
- `web-next/lib/api.ts`
- `web-next/lib/contribution-action.test.ts`
- `web-next/lib/contribution-errors.ts`
- `web-next/lib/contribution-validation.ts`
- `web-next/lib/formatters.test.ts`
- `web-next/lib/formatters.ts`
- `web-next/next.config.ts`
- `web-next/package-lock.json`
- `web-next/package.json`
- `web-next/playwright.config.ts`
- `web-next/proxy.ts`
- `web-next/scripts/check-assignment-82.mjs`
- `web-next/scripts/check-release-age.mjs`
- `web-next/scripts/repeat-unit.mjs`
- `web-next/tests/setup.ts`
- `web-next/vitest.config.mts`

## Actual mobile performance evidence

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


Original navigation:

```json
{"phase":"original","name":"home","run":1,"lcpMs":3046.0610000000006,"cls":0}
{"phase":"original","name":"home","run":2,"lcpMs":953.6469,"cls":0}
{"phase":"original","name":"home","run":3,"lcpMs":923.9554,"cls":0}
{"phase":"original","name":"home","medianLcpMs":953.6469,"medianCls":0}
{"phase":"original","name":"stokvel","run":1,"lcpMs":931.367726309204,"cls":0}
{"phase":"original","name":"stokvel","run":2,"lcpMs":974.717676309204,"cls":0}
{"phase":"original","name":"stokvel","run":3,"lcpMs":980.3556513092042,"cls":0}
{"phase":"original","name":"stokvel","medianLcpMs":974.717676309204,"medianCls":0}
```

Final navigation:

```json
{"phase":"final","name":"home","run":1,"lcpMs":2843.3340000000003,"cls":0}
{"phase":"final","name":"home","run":2,"lcpMs":810.1406737335205,"cls":0}
{"phase":"final","name":"home","run":3,"lcpMs":802.2123737335205,"cls":0}
{"phase":"final","name":"home","medianLcpMs":810.1406737335205,"medianCls":0}
{"phase":"final","name":"stokvel","run":1,"lcpMs":912.2766526184082,"cls":0}
{"phase":"final","name":"stokvel","run":2,"lcpMs":1025.1181526184082,"cls":0}
{"phase":"final","name":"stokvel","run":3,"lcpMs":967.3563526184082,"cls":0}
{"phase":"final","name":"stokvel","medianLcpMs":967.3563526184082,"medianCls":0}
```

Original interaction Timespans:

```json
{"phase":"original","id":9,"metrics":{"cumulative-layout-shift":{"value":0.025469000000000002,"display":"0.025"},"interaction-to-next-paint":{"value":79.107,"display":"80 ms"},"layout-shifts":{"display":"2 layout shifts found"}}}
{"phase":"original","id":10,"metrics":{"cumulative-layout-shift":{"value":0.025469000000000002,"display":"0.025"},"interaction-to-next-paint":{"value":77.764,"display":"80 ms"},"layout-shifts":{"display":"2 layout shifts found"}}}
{"phase":"original","id":11,"metrics":{"cumulative-layout-shift":{"value":0.025469000000000002,"display":"0.025"},"interaction-to-next-paint":{"value":66.739,"display":"70 ms"},"layout-shifts":{"display":"2 layout shifts found"}}}
```

Final interaction Timespans:

```json
{"phase":"final","id":6,"metrics":{"cumulative-layout-shift":{"value":0.038674,"display":"0.039"},"interaction-to-next-paint":{"value":66.48,"display":"70 ms"},"layout-shifts":{"display":"2 layout shifts found"}}}
{"phase":"final","id":7,"metrics":{"cumulative-layout-shift":{"value":0.038674,"display":"0.039"},"interaction-to-next-paint":{"value":67.1,"display":"70 ms"},"layout-shifts":{"display":"2 layout shifts found"}}}
{"phase":"final","id":8,"metrics":{"cumulative-layout-shift":{"value":0.038674,"display":"0.039"},"interaction-to-next-paint":{"value":74.424,"display":"70 ms"},"layout-shifts":{"display":"2 layout shifts found"}}}
```

## Actual signed-out WhatsApp curl head

Captured with `curl.exe -sS -A WhatsApp/2` against a real invitation generated by the treasurer test. No cookies were supplied. The full returned head follows (not a fabricated example).

```html
<head><meta charSet="utf-8"/><meta name="viewport" content="width=device-width, initial-scale=1"/><link rel="stylesheet" href="/_next/static/chunks/00px7a5ek6b6y.css" data-precedence="next"/><link rel="preload" as="script" fetchPriority="low" href="/_next/static/chunks/1x6obpmlu1_c2.js"/><script src="/_next/static/chunks/1rgtdfuk-uqi9.js" async=""></script><script src="/_next/static/chunks/29yl_eicdwzj2.js" async=""></script><script src="/_next/static/chunks/0_k0170ifn71m.js" async=""></script><script src="/_next/static/chunks/turbopack-2c3mg3r-05n1w.js" async=""></script><script src="/_next/static/chunks/3czkq7dzzvusk.js" async=""></script><script src="/_next/static/chunks/3fsjc20n0w6a7.js" async=""></script><script src="/_next/static/chunks/2qrxgkr85ypft.js" async=""></script><script src="/_next/static/chunks/2ipx0-6t-7qz_.js" async=""></script><script src="/_next/static/chunks/0m5kx1ao6deml.js" async=""></script><title>Stokvel invitation | RondiTrack</title><meta name="description" content="You have an invitation to join a stokvel on RondiTrack. Sign in to check it."/><meta name="robots" content="noindex, nofollow"/><meta property="og:title" content="Stokvel invitation | RondiTrack"/><meta property="og:description" content="Sign in to check your invitation."/><meta name="twitter:card" content="summary"/><meta name="twitter:title" content="Stokvel invitation | RondiTrack"/><meta name="twitter:description" content="Sign in to check your invitation."/><link rel="icon" href="/favicon.ico?favicon.117ezoe8m31dk.ico" sizes="48x48" type="image/x-icon"/><meta name="next-size-adjust" content=""/><script src="/_next/static/chunks/0cz1d0mv5g_q7.js" noModule=""></script></head>
```

The existing Assignment 8.2 contract/caching/concurrent-fetch checks also passed against the disposable API. Its five-iteration read-only benchmark measured 58.6ms sequential median and 48.2ms parallel median (1.22×); this is separate from Lighthouse and is not a Core Web Vitals score.