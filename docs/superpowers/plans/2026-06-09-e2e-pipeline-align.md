# E2E Pipeline Alignment Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Align the `E2E` stage of `build/azure-pipeline.yml` with Umbraco-CMS's nightly E2E step approach, adapted so the test site is the in-solution `Umbraco.Web.TestSite.V17` run from source.

**Architecture:** Single-stage YAML edit. Replace the whole `E2E` stage's run/wait/stop steps: drop `dotnet publish` + `Copy uSync` (run `dotnet run --no-build` in place from `src/Umbraco.Web.TestSite.V17`), capture PID into pipeline variable `AcceptanceTestProcessId`, wait via `npx wait-on https-get://...` (HTTP 200, cert bypass), one Playwright install, stop via `kill -15 $(AcceptanceTestProcessId)`, Umbraco-style artifact name. The stage `variables` block, setup steps, env block, and JUnit publish are preserved.

**Tech Stack:** Azure DevOps Pipelines YAML; .NET SDK; Node.js; Playwright; `wait-on`. No local pipeline runner — verification is YAML parse validity + leftover-token grep + a diff review against the spec.

**Working directory for all commands:** repository root `D:\Umbraco\Repos\Umbraco.Cms.Search` (the file path is `build/azure-pipeline.yml`).

---

### Task 1: Rewrite the E2E stage

The change is internally interdependent (the stop step references the variable set by the run step; the wait step depends on the new run step), so the entire `E2E` stage is replaced in one atomic edit.

**Files:**
- Modify: `build/azure-pipeline.yml` (the `- stage: E2E` block only)

- [ ] **Step 1: Read the current file**

Read `build/azure-pipeline.yml`. Locate the `E2E` stage: it begins at the line `  - stage: E2E ` (note the trailing space) and ends immediately before `  - stage: Dependency_Track`.

- [ ] **Step 2: Replace the entire `E2E` stage**

Replace everything from `  - stage: E2E ` up to (but NOT including) `  - stage: Dependency_Track` with EXACTLY this block:

```yaml
  - stage: E2E
    displayName: E2E Tests
    dependsOn: Build
    variables:
      npm_config_cache_e2e: $(Pipeline.Workspace)/.npm_e2e
      # Enable console logging in Release mode
      SERILOG__WRITETO__0__NAME: Async
      SERILOG__WRITETO__0__ARGS__CONFIGURE__0__NAME: Console
      # Set unattended install settings
      UMBRACO__CMS__UNATTENDED__INSTALLUNATTENDED: true
      UMBRACO__CMS__UNATTENDED__UNATTENDEDUSERNAME: Playwright Test
      UMBRACO__CMS__UNATTENDED__UNATTENDEDUSERPASSWORD: UmbracoAcceptance123!
      UMBRACO__CMS__UNATTENDED__UNATTENDEDUSEREMAIL: playwright@umbraco.com
      # Custom Umbraco settings
      UMBRACO__CMS__CONTENT__CONTENTVERSIONCLEANUPPOLICY__ENABLECLEANUP: false
      UMBRACO__CMS__GLOBAL__DISABLEELECTIONFORSINGLESERVER: true
      UMBRACO__CMS__GLOBAL__INSTALLMISSINGDATABASE: true
      UMBRACO__CMS__GLOBAL__ID: 00000000-0000-0000-0000-000000000042
      UMBRACO__CMS__GLOBAL__VERSIONCHECKPERIOD: 0
      UMBRACO__CMS__GLOBAL__USEHTTPS: true
      UMBRACO__CMS__HEALTHCHECKS__NOTIFICATION__ENABLED: false
      UMBRACO__CMS__KEEPALIVE__DISABLEKEEPALIVETASK: true
      UMBRACO__CMS__WEBROUTING__UMBRACOAPPLICATIONURL: https://localhost:44324/
      ASPNETCORE_URLS: https://localhost:44324
    jobs:
      - job:
        displayName: E2E Tests (SQLite)
        pool:
          vmImage: 'ubuntu-latest'
        timeoutInMinutes: 30
        variables:
          CONNECTIONSTRINGS__UMBRACODBDSN: "Data Source=|DataDirectory|/Umbraco.sqlite.db;Cache=Shared;Foreign Keys=True;Pooling=True"
          CONNECTIONSTRINGS__UMBRACODBDSN_PROVIDERNAME: Microsoft.Data.Sqlite
        steps:
          # Checkout source (full clone for Nerdbank.GitVersioning)
          - checkout: self
            fetchDepth: 0

          # Setup environment
          - task: DownloadPipelineArtifact@2
            displayName: Download NuGet artifacts
            inputs:
              artifact: build_output
              path: $(Build.SourcesDirectory)

          - task: UseDotNet@2
            displayName: Use .NET SDK from global.json
            inputs:
              useGlobalJson: true

          - task: NodeTool@0
            displayName: Use Node.js
            inputs:
              versionSpec: '22.x'

          - pwsh: |
              "UMBRACO_USER_LOGIN=$(UMBRACO__CMS__UNATTENDED__UNATTENDEDUSEREMAIL)
              UMBRACO_USER_PASSWORD=$(UMBRACO__CMS__UNATTENDED__UNATTENDEDUSERPASSWORD)
              URL=$(ASPNETCORE_URLS)" | Out-File .env
            displayName: Generate .env
            workingDirectory: $(Build.SourcesDirectory)/src/Umbraco.Test.Search.AcceptanceTest

          # Cache NPM packages
          - script: mkdir -p $(npm_config_cache_e2e)
            displayName: Create NPM cache directory

          - task: Cache@2
            displayName: Cache NPM packages
            inputs:
              key: '"npm_e2e" | "$(Agent.OS)" | src/Umbraco.Test.Search.AcceptanceTest/package-lock.json'
              restoreKeys: |
                "npm_e2e" | "$(Agent.OS)"
                "npm_e2e"
              path: $(npm_config_cache_e2e)

          # Restore NPM packages
          - script: npm ci --no-fund --no-audit --prefer-offline
            displayName: Restore NPM packages
            workingDirectory: src/Umbraco.Test.Search.AcceptanceTest

          # Generate HTTPS dev certificate
          - script: dotnet dev-certs https --trust
            displayName: Generate HTTPS dev certificate
            continueOnError: true

          # Run the test site from source (build output comes from the Build stage artifact)
          - bash: |
              nohup dotnet run --project Umbraco.Web.TestSite.V17.csproj --configuration $(buildConfiguration) --no-build --no-launch-profile > $(Build.ArtifactStagingDirectory)/testsite.log 2>&1 &
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

          # Wait for the application to respond (HTTP 200), bypassing the self-signed dev cert
          - script: npm install wait-on
            displayName: Install wait-on package
            workingDirectory: src/Umbraco.Test.Search.AcceptanceTest

          - script: npx wait-on -v --interval 1000 --timeout 120000 https-get://localhost:44324
            displayName: Wait for application
            workingDirectory: src/Umbraco.Test.Search.AcceptanceTest
            env:
              NODE_TLS_REJECT_UNAUTHORIZED: 0

          # Install Playwright (Chromium) with OS dependencies
          - script: npx playwright install --with-deps chromium
            displayName: Install Playwright (Chromium)
            workingDirectory: src/Umbraco.Test.Search.AcceptanceTest

          # Run acceptance tests
          - script: npm run test
            displayName: Run Playwright tests
            workingDirectory: src/Umbraco.Test.Search.AcceptanceTest
            env:
              CI: true

          # Stop the application
          - bash: kill -15 $(AcceptanceTestProcessId)
            displayName: Stop application
            condition: and(succeededOrFailed(), ne(variables.AcceptanceTestProcessId, ''))

          # Publish test artifacts
          - task: PublishPipelineArtifact@1
            displayName: Publish test artifacts
            condition: succeededOrFailed()
            inputs:
              targetPath: src/Umbraco.Test.Search.AcceptanceTest/results
              artifactName: "Acceptance Test Results - $(Agent.JobName) - Attempt #$(System.JobAttempt)"

          # Publish test results
          - task: PublishTestResults@2
            displayName: Publish test results
            condition: succeededOrFailed()
            inputs:
              testResultsFormat: 'JUnit'
              testResultsFiles: 'src/Umbraco.Test.Search.AcceptanceTest/results/results.xml'
              testRunTitle: 'Acceptance Test Results'
              failTaskOnFailedTests: true

```

Note: the trailing blank line above keeps the one blank line separating the `E2E` stage from `  - stage: Dependency_Track`. Do not delete or alter the `Dependency_Track` stage.

- [ ] **Step 3: Verify the YAML still parses**

Run from the repo root:
```
npx --yes js-yaml build/azure-pipeline.yml > /dev/null && echo "YAML_OK"
```
Expected: prints `YAML_OK` (exit 0). If it errors, fix the indentation/structure (Azure `$(...)` macros are plain strings and are fine for the parser).

- [ ] **Step 4: Verify removed tokens are gone and the install is singular**

Run from the repo root:
```
echo "=== should be EMPTY (removed) ===" ; grep -nE "dotnet publish|Copy uSync|testsite\.pid|testsite/testsite\.log|Build\.ArtifactStagingDirectory\)/testsite" build/azure-pipeline.yml || echo "none-good"
echo "=== playwright install occurrences (expect exactly 1) ===" ; grep -nc "playwright install" build/azure-pipeline.yml
echo "=== new anchors present (expect 3 lines) ===" ; grep -nE "AcceptanceTestProcessId|wait-on -v|kill -15" build/azure-pipeline.yml
```
Expected: the first grep prints `none-good`; the playwright count is `1`; the third grep shows the three new lines (the `##vso` setvariable + `kill -15` reference both contain `AcceptanceTestProcessId`, plus `wait-on -v`).

- [ ] **Step 5: Commit**

```bash
git add build/azure-pipeline.yml
git commit -m "Align E2E stage with Umbraco-CMS: dotnet run, wait-on, kill -15"
```
Then run `git show --stat HEAD` and confirm it changed exactly one file: `build/azure-pipeline.yml`. (There are unrelated changes elsewhere in the working tree — do NOT use `git add -A`.)

---

### Task 2: Final diff review against the spec

**Files:** none (review only)

- [ ] **Step 1: Diff and read**

Run `git show HEAD -- build/azure-pipeline.yml` and read the whole E2E stage in context.

- [ ] **Step 2: Confirm against the spec checklist**

Verify each of these is true in the committed file:
1. No `dotnet publish` step; no `Copy uSync data` step.
2. `Run application` step uses `dotnet run --project Umbraco.Web.TestSite.V17.csproj --configuration $(buildConfiguration) --no-build --no-launch-profile`, `workingDirectory: src/Umbraco.Web.TestSite.V17`, backgrounded with `nohup ... &`, and sets `AcceptanceTestProcessId` via `##vso[task.setvariable]`.
3. The `env:` block on `Run application` still contains all the unattended-install, Umbraco settings, and both `CONNECTIONSTRINGS__*` entries (kept as-is).
4. Wait uses `npm install wait-on` then `npx wait-on -v --interval 1000 --timeout 120000 https-get://localhost:44324` with `NODE_TLS_REJECT_UNAUTHORIZED: 0`.
5. Exactly one Playwright install: `npx playwright install --with-deps chromium`.
6. Test run unchanged (`npm run test`, `CI: true`).
7. Stop uses `kill -15 $(AcceptanceTestProcessId)` with `condition: and(succeededOrFailed(), ne(variables.AcceptanceTestProcessId, ''))`.
8. Artifact name is `"Acceptance Test Results - $(Agent.JobName) - Attempt #$(System.JobAttempt)"`; `PublishTestResults@2` JUnit step unchanged (`failTaskOnFailedTests: true`).
9. The stage `variables` block, `dependsOn: Build`, pool `ubuntu-latest`, `timeoutInMinutes: 30`, and the sqlite job `variables` are preserved.
10. The `Build`, `UnitTests`, `IntegrationTests`, and `Dependency_Track` stages are untouched.

- [ ] **Step 3: Report**

Report any deviation from the above. If all 10 hold, the change is complete. (A real validation requires a pipeline run on the branch — note that as the remaining out-of-band verification: the `Run application` step backgrounds the site and sets `AcceptanceTestProcessId`, `wait-on` returns within 120s, tests run, JUnit publishes, and the app is stopped.)

---

## Notes for the implementer

- This repo has **no local Azure Pipelines runner**; do not attempt to execute the pipeline. YAML-parse + token-grep + diff review are the gates.
- `dotnet run --no-build` relies on `Umbraco.Web.TestSite.V17` having been compiled in the `Build` stage (it is in `src/Umbraco.Cms.Search.sln`) and shipped in the `build_output` artifact — do not add a build/publish step.
- Keep the `env:` block on `Run application` verbatim (a misconfigured unattended install fails silently). This is a deliberate decision, not an oversight.
- Do not extract templates or add a SQL Server E2E matrix — out of scope.
