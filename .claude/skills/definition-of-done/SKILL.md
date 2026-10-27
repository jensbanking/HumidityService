---
name: definition-of-done
description: Checks a HumidityService roadmap feature against the project's Definition of Done before it may be marked [x] in claude.md, and produces a pass/warn/fail report for the user to approve. Use when a feature is believed finished, before ticking it off, or when the user runs /definition-of-done.
argument-hint: "<feature, e.g. \"Feature 6\">"
---

# Definition of Done

Checks one feature from `claude.md` ("Technical Features Blueprint") against the project's rules
before it may be marked `[x]`. `claude.md` is the source of truth for the rules; this skill is
the procedure for checking them.

The argument names the feature (e.g. `Feature 6`). If it's missing, ask which feature.

**Never mark the feature `[x]` yourself as part of this skill.** The output is a report; the
feature is ticked off only after the user has approved that report.

## New work vs. existing debt

The verdict is about the feature, not about the whole codebase:

- ✅/⚠️/❌ in the table judge **the feature's own changes** only.
- Problems found by the solution-wide checks (vulnerable packages, unused packages, coverage,
  missing OpenTelemetry, ...) that existed before the feature go in the report's
  **"Eksisterende gæld (blokerer ikke)"** section: visible, but not blocking.
- Existing debt **does** block (❌) when:
  - the feature makes it worse (adds another vulnerable or unused package, lowers coverage,
    copies the pattern into new code);
  - the feature changes the code the debt lives in - clean it up while there;
  - the debt was deferred **to this feature** - in the feature's text in `claude.md`, or agreed
    with the user (ask if unsure). Deferred debt must be resolved where it was deferred to, not
    passed on silently.
- `dotnet test` must pass and the 80 % coverage gate must hold regardless of who caused a
  failure - CI blocks the merge anyway.

To tell new from existing, compare with `main` before the feature (e.g. run the check on
`git stash`/a `main` worktree, or read the feature's diff).

## 1. Establish scope

- Read the feature's section in `claude.md` - every bullet, including "Open decision" notes.
- Find the code that implements it: the feature branch's diff against `main`
  (`git diff main...HEAD`), or, if already merged, the commits/PRs for the feature
  (`git log --oneline`). Note which projects and Terraform modules it touches.
- Read the changed code. Don't judge from the diff stat or file names alone.

## 2. Check every rule

For each item, decide ✅ (met), ⚠️ (met with a caveat, or deliberately deferred) or ❌ (not met),
and point to the evidence (`file:line`, test name, command output). "Not applicable" is fine
when it truly is, with one line saying why.

### 2.1 Feature requirements
- Go through the feature's bullets one by one: where is each implemented? Anything not done is ❌
  unless the user has explicitly deferred it (then ⚠️, naming where it is recorded).
- The "Location Configuration" rules hold where relevant: per-location isolation, a failure for
  one location doesn't stop the others, the slug is used consistently.

### 2.2 Architecture (Clean Architecture)
- **Domain** holds the business rules (entities, value objects, rules such as thresholds and
  validation) and has no references to other layers and no packages.
- **Application** orchestrates: interfaces (ports), use cases/handlers, DTOs. Business rules
  that have crept in here belong in Domain.
- **Infrastructure** only integrates: API clients, storage, SQL, email, configuration binding.
  No business decisions.
- **FunctionHost** only has triggers and DI wiring.
- Dependencies point inward only.
- No unused packages (`PackageReference`s no code uses), no dead code, no leftover
  "planned"/TODO code paths.

### 2.3 Coding standards
- Nullable, implicit usings, file-scoped namespaces.
- XML documentation on every public type and member.
- Async/await all the way; `CancellationToken` passed through.
- Constructor injection only; no service locator (`IServiceProvider` lookups in services).
- Exceptions are never swallowed: logged with context (location slug, ids) and rethrown or
  recorded as a failure.

### 2.4 Resilience
- Every new outbound call (OAuth, REST, Blob, SQL, email) has a Polly retry.
- Retries only on transient failures (see `Infrastructure/Resilience/AzureRetryPipeline.cs` and
  `HandleTransientHttpError`), and only one retry layer (Azure SDK clients use
  `AzureRetryPipeline.WithoutSdkRetries`).

### 2.5 Security
- No secrets in source control (search the diff for keys, connection strings, passwords).
- Managed Identity for Azure resources wherever possible; secrets that must exist live in
  Key Vault and reach the app as Key Vault references.
- Role assignments are as narrow as they can be (scope and role).

### 2.6 Observability
- Failures are visible beyond a log line: a failed function invocation, a metric, or an alert.
- Logs are structured (message templates with named properties) and carry context.
- OpenTelemetry / Application Insights requirements in `claude.md` are met for the new code.

### 2.7 Tests
- New logic in Domain/Application/Infrastructure has tests (xUnit, NSubstitute), including
  failure paths and per-location isolation.
- Run, and quote the result lines:
  - `dotnet test HumidityService.sln`
  - `dotnet list HumidityService.sln package --vulnerable --include-transitive`
- Coverage: CI fails below 80 % line coverage for Application and Infrastructure. If the feature
  is on a branch with an open PR, read the CI run's coverage summary; otherwise measure locally
  with `dotnet test HumidityService.sln --collect:"XPlat Code Coverage"`.
- If Terraform changed: `terraform fmt -check -recursive terraform` and `terraform validate` in
  each affected environment directory that is already initialized (don't run `terraform init`
  for every environment locally - each init downloads ~250 MB of providers; CI validates all).

### 2.8 Documentation
- `Readme.md`, the layer READMEs (`src/*/README.md`), `terraform/README.md` and module READMEs
  describe what now exists.
- New app settings are documented (Readme's `local.settings.json` example, Terraform app
  settings).
- `claude.md` is updated where the implementation consciously differs from the feature text
  (ask the user before changing requirements).

### 2.9 Code review
- Run the `/code-review` skill on the feature's changes and handle every finding: fix it, or
  state in the report why it stays.

## 3. Report

Reply in the user's language with:

```
## Definition of Done: <feature>

| # | Område | Status | Bemærkning / evidens |
|---|---|---|---|
| 2.1 | Krav | ✅/⚠️/❌ | ... |
| 2.2 | Arkitektur | ... | ... |
| 2.3 | Kodestandarder | ... | ... |
| 2.4 | Robusthed | ... | ... |
| 2.5 | Sikkerhed | ... | ... |
| 2.6 | Observability | ... | ... |
| 2.7 | Tests | ... | <test/coverage/vulnerability results> |
| 2.8 | Dokumentation | ... | ... |
| 2.9 | Code review | ... | <findings and how each was handled> |

**Krav punkt for punkt:** <each feature bullet → where it is implemented, or what is missing>

**Skal rettes før godkendelse:** <every ❌, or "intet">
**Bevidste afvigelser:** <every ⚠️ and why>
**Eksisterende gæld (blokerer ikke):** <pre-existing problems found by the solution-wide checks, or "intet">
**Brugerens verifikation:** <what the user should check in the deployed environment>
```

Then stop and ask the user to approve. Fix ❌ items first if the user wants that, and re-run the
affected checks.

## 4. After approval

Only when the user approves the report: change the feature's `### - [ ]` to `### - [x]` in
`claude.md` (on a branch, committed only when the user asks).
