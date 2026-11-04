# Assignment 9.1: Join flow, proof of payment and safe confirmations

A signed-in invitee can now review the public terms and join in three URL-backed steps without re-entering their code. Contributions support private JPEG/PNG/PDF proof attachments. A Treasurer closes a cycle only against an API-calculated preview, with stale previews rejected and repeated closes replaying a durable result.

The implementation reuses the encrypted session, Server Actions, BFF, DbContext and existing monthly arrears logic. It includes a migration for invites, private proof bytes and close receipts, and aligns the Entity Framework relational runtime with the API's existing 10.0.13 version.

## Validation output

From the repository root:

```text
> dotnet build RondiTrack.Tests --no-restore
Build succeeded.
    0 Warning(s)
    0 Error(s)

> dotnet test RondiTrack.Tests --no-restore --logger 'console;verbosity=minimal'
Passed! - Failed: 0, Passed: 75, Skipped: 0, Total: 75
```

From `web-next`:

```text
> npm run lint
> eslint
[exit 0; no findings]

> npm test
Test Files  4 passed (4)
     Tests  23 passed (23)

> npm run build
[production build and TypeScript pass]

> npm audit
found 0 vulnerabilities
```

Browser checks use generated accounts against the real API and an isolated PostgreSQL database:

- Signed-out invite returns to the same code/step after sign-in; headings receive focus; browser Back twice retains the code.
- A 6 MiB file fails inline without submission; renamed text claiming PDF is refused by the API inline; a supported file succeeds.
- A file one byte below 5 MiB passes all upload hops and downloads with the same length.
- BFF attachments have `nosniff` and `private, no-store`.
- Sipho downloads another member's proof where he is Treasurer and gets 404 where he is an ordinary member.
- Dialog focus begins on Cancel; Escape restores focus to the opener.
- A second browser records a payment between preview and close. The first browser sees the stale-preview refusal, reviews the replacement preview and confirms again.
- An unreachable API produces a failure toast with a correlation reference; it remains visible and can be dismissed explicitly.
- Closing locks the proof controls. API tests separately verify exact replay and arrears recorded once under concurrent retries.

## Deployment / review notes

Apply `AddJoinProofAndSafeClosing` using the existing EF migration workflow. No existing development or production database was migrated during this work; tests used temporary databases only.

Every direct npm dependency is exactly pinned. The first minimatch security fix, 11.0.1, was published on 27 October 2026, so it cannot simultaneously be two weeks old on 4 November and satisfy the zero-audit requirement. The implementation prioritizes the security fix and documents that age exception in the README. Other direct packages use eligible releases; the initial transitive graph was resolved with a 21 October cutoff.

The three-minute recording is still outstanding: the video-encoder download was declined. Suggested recording sequence:

1. Open an invite signed out, sign in, join, and press Back twice.
2. Try an oversized file and renamed text, then upload a proof.
3. Show the Treasurer/ordinary-member download difference across two stokvels.
4. Preview closing in one browser, record a payment in another and show the stale-preview response.
5. Confirm the replacement preview and show the resulting arrears once.

This is a local PR description draft. No commit, push, branch switch, merge or remote PR creation was performed.
