# Align E2E Stage with Umbraco-CMS (test site = Web.TestSite.V17)

**Date:** 2026-06-09
**File:** `build/azure-pipeline.yml` — `E2E` stage only
**Scope:** "Như trên + dọn dẹp" — align the run/wait/stop step *patterns* with Umbraco-CMS's nightly E2E templates, plus safe cleanup. No new jobs, no template extraction.

## Goal

Make the existing `E2E` stage follow Umbraco-CMS's E2E step approach (`nightly-E2E-run-application-template.yml` + `nightly-E2E-run-tests-template.yml`), adapted to the fact that this repo's test site is the in-solution `Umbraco.Web.TestSite.V17` project (Umbraco-CMS instead scaffolds a fresh `UmbracoProject` from templates).

## Reference (Umbraco-CMS nightly E2E)

- **Run application:** `nohup dotnet run --project UmbracoProject --configuration <cfg> --no-build --no-launch-profile > .../playwright.log 2>&1 &` then `echo "##vso[task.setvariable variable=AcceptanceTestProcessId]$!"`.
- **Run tests:** `npm install wait-on` → `npx wait-on -v --interval 1000 --timeout 120000 <tcp:port|url>` → `npx playwright install chromium` → run test command (`CI: true`) → `kill -15 $(AcceptanceTestProcessId)` (`succeededOrFailed`) → copy results → `PublishPipelineArtifact` (named per job/attempt) → `PublishTestResults` (JUnit).

## Why we can drop publish + uSync copy

`Umbraco.Web.TestSite.V17` is in `src/Umbraco.Cms.Search.sln` and is therefore compiled during the `Build` stage. The `build_output` artifact published by `Build` is the whole `$(Build.SourcesDirectory)` (including `src/Umbraco.Web.TestSite.V17/bin` and `obj`). The E2E job already downloads `build_output` into `$(Build.SourcesDirectory)`, so `dotnet run --no-build` works directly from source — no `dotnet publish` needed.

Running in place from `src/Umbraco.Web.TestSite.V17` (as the working directory) makes the app's ContentRoot that project folder, so the Development config path `../_uSync/Shared/` resolves to `src/_uSync/Shared/` (which exists in the repo). The current `Copy uSync data` step (which exists only because the published output had no sibling `_uSync`) is therefore no longer needed.

## Kept unchanged

`checkout: self` (fetchDepth 0) · `DownloadPipelineArtifact build_output` → SourcesDirectory · `UseDotNet` (global.json) · `NodeTool` (22.x) · `Generate .env` for the AcceptanceTest project · NPM cache + `npm ci` · `dotnet dev-certs https --trust` (continueOnError) · all stage/job `variables` (unattended install, Umbraco settings, sqlite connection string) · `PublishTestResults@2` (JUnit, failTaskOnFailedTests).

## Changes

### 1. Remove `dotnet publish` step and `Copy uSync data` step
Delete both. The app runs from source instead (below).

### 2. Replace "Start test site" with Umbraco-style "Run application"
```yaml
- bash: |
    nohup dotnet run --project Umbraco.Web.TestSite.V17.csproj \
      --configuration $(buildConfiguration) --no-build --no-launch-profile \
      > $(Build.ArtifactStagingDirectory)/testsite.log 2>&1 &
    echo "##vso[task.setvariable variable=AcceptanceTestProcessId]$!"
  displayName: Run application
  workingDirectory: src/Umbraco.Web.TestSite.V17
  env:
    ASPNETCORE_ENVIRONMENT: Development
    ASPNETCORE_URLS: $(ASPNETCORE_URLS)
    UMBRACO__CMS__UNATTENDED__INSTALLUNATTENDED: $(UMBRACO__CMS__UNATTENDED__INSTALLUNATTENDED)
    UMBRACO__CMS__UNATTENDED__UNATTENDEDUSERNAME: $(UMBRACO__CMS__UNATTENDED__UNATTENDEDUSERNAME)
    UMBRACO__CMS__UNATTENDED__UNATTENDEDUSERPASSWORD: $(UMBRACO__CMS__UNATTENDED__UNATTENDEDUSERPASSWORD)
    UMBRACO__CMS__UNATTENDED__UNATTENDEDUSEREMAIL: $(UMBRACO__CMS__UNATTENDED__UNATTENDEDUSEREMAIL)
    UMBRACO__CMS__GLOBAL__INSTALLMISSINGDATABASE: $(UMBRACO__CMS__GLOBAL__INSTALLMISSINGDATABASE)
    UMBRACO__CMS__GLOBAL__USEHTTPS: $(UMBRACO__CMS__GLOBAL__USEHTTPS)
    UMBRACO__CMS__GLOBAL__DISABLEELECTIONFORSINGLESERVER: $(UMBRACO__CMS__GLOBAL__DISABLEELECTIONFORSINGLESERVER)
    UMBRACO__CMS__GLOBAL__VERSIONCHECKPERIOD: $(UMBRACO__CMS__GLOBAL__VERSIONCHECKPERIOD)
    UMBRACO__CMS__CONTENT__CONTENTVERSIONCLEANUPPOLICY__ENABLECLEANUP: $(UMBRACO__CMS__CONTENT__CONTENTVERSIONCLEANUPPOLICY__ENABLECLEANUP)
    UMBRACO__CMS__HEALTHCHECKS__NOTIFICATION__ENABLED: $(UMBRACO__CMS__HEALTHCHECKS__NOTIFICATION__ENABLED)
    UMBRACO__CMS__KEEPALIVE__DISABLEKEEPALIVETASK: $(UMBRACO__CMS__KEEPALIVE__DISABLEKEEPALIVETASK)
    CONNECTIONSTRINGS__UMBRACODBDSN: $(CONNECTIONSTRINGS__UMBRACODBDSN)
    CONNECTIONSTRINGS__UMBRACODBDSN_PROVIDERNAME: $(CONNECTIONSTRINGS__UMBRACODBDSN_PROVIDERNAME)
```
Decision: the explicit `env:` block is **kept as-is** (safety — guarantees unattended install + correct DB; not relying on Azure's pipeline-variable→env auto-mapping for the install-critical settings). URL comes from `ASPNETCORE_URLS`; `--no-launch-profile` ignores `launchSettings.json`.

### 3. Replace the `curl` readiness loop with `wait-on` (HTTP 200)
```yaml
- script: npm install wait-on
  displayName: Install wait-on package
  workingDirectory: src/Umbraco.Test.Search.AcceptanceTest

- script: npx wait-on -v --interval 1000 --timeout 120000 https-get://localhost:44324
  displayName: Wait for application
  workingDirectory: src/Umbraco.Test.Search.AcceptanceTest
  env:
    NODE_TLS_REJECT_UNAUTHORIZED: 0
```
Decision: wait on the **HTTPS URL via `https-get://`** (GET, expects 2xx/3xx) rather than `tcp:44324`, so readiness means the app actually responds (preserves the guarantee the old curl loop gave — important because the search site needs uSync import + index build before the frontend serves results). `NODE_TLS_REJECT_UNAUTHORIZED=0` bypasses the self-signed dev cert. The host is derived from `ASPNETCORE_URLS` (`https://localhost:44324`).

### 4. Single Playwright install
Remove the duplicate. Keep exactly one, after the wait step:
```yaml
- script: npx playwright install --with-deps chromium
  displayName: Install Playwright (Chromium)
  workingDirectory: src/Umbraco.Test.Search.AcceptanceTest
```
`--with-deps` is required because this job (unlike Umbraco's setup template) has no separate dependency-install step.

### 5. Run tests — unchanged
```yaml
- script: npm run test
  displayName: Run Playwright tests
  workingDirectory: src/Umbraco.Test.Search.AcceptanceTest
  env:
    CI: true
```

### 6. Replace PID-file stop with variable-based stop
```yaml
- bash: kill -15 $(AcceptanceTestProcessId)
  displayName: Stop application
  condition: and(succeededOrFailed(), ne(variables.AcceptanceTestProcessId, ''))
```
(Linux only — this job runs on `ubuntu-latest`. No Windows/`Stop-Process` variant needed.)

### 7. Artifact naming (Umbraco style)
```yaml
- task: PublishPipelineArtifact@1
  displayName: Publish test artifacts
  condition: succeededOrFailed()
  inputs:
    targetPath: src/Umbraco.Test.Search.AcceptanceTest/results
    artifactName: "Acceptance Test Results - $(Agent.JobName) - Attempt #$(System.JobAttempt)"
```
`PublishTestResults@2` (JUnit from `.../results/results.xml`, `failTaskOnFailedTests: true`) stays unchanged.

## Final E2E job step order

1. `checkout: self` (fetchDepth 0)
2. Download `build_output` → SourcesDirectory
3. `UseDotNet` (global.json)
4. `NodeTool` 22.x
5. Generate `.env`
6. NPM cache create + `Cache@2` + `npm ci`
7. `dotnet dev-certs https --trust` (continueOnError)
8. **Run application** (`dotnet run --no-build --no-launch-profile`, PID → `AcceptanceTestProcessId`)
9. `npm install wait-on`
10. **Wait** (`npx wait-on https-get://localhost:44324`, `NODE_TLS_REJECT_UNAUTHORIZED=0`)
11. **Playwright install** (`--with-deps chromium`, once)
12. **Run tests** (`npm run test`, `CI: true`)
13. **Stop** (`kill -15 $(AcceptanceTestProcessId)`, `succeededOrFailed`)
14. Publish artifacts (job/attempt name)
15. Publish test results (JUnit)

## Verification

- YAML lints / parses (Azure DevOps validation, or a local YAML parse).
- A pipeline run on the branch: the `Run application` step backgrounds the site and sets `AcceptanceTestProcessId`; `wait-on` returns within timeout; all acceptance tests run; JUnit results publish; the app is stopped in the `succeededOrFailed` step.
- Confirm `dotnet run --no-build` succeeds against the downloaded `build_output` (i.e. `Umbraco.Web.TestSite.V17` bin is present from the Build stage).

## Out of scope / not changed

- No SQL Server E2E matrix (Umbraco runs both; this repo's E2E remains SQLite-only).
- No extraction into separate template files.
- No change to other stages (`Build`, `UnitTests`, `IntegrationTests`, `Dependency_Track`).
- Pre-existing acceptance-test cold-start sensitivity (search index warmup) is mitigated, not removed, by the HTTP-200 wait + CI `retries: 2`.
