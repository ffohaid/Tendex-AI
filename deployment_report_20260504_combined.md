# Deployment Report — 2026-05-04

## Overview

This report documents the final production deployment unblock for the Tendex AI fix batch dated 2026-05-04. The deployment pipeline was repeatedly failing during backend image build in GitHub Actions because the runtime stage attempted to install `curl` through `apt-get`, and the package index refresh was failing inside the CI environment.

## Root Cause

The backend runtime image depended on an `apt-get update && apt-get install curl` step only to support container health checks. This created an avoidable external package-manager dependency in the runtime stage. When Debian package indexes intermittently failed to download in GitHub Actions, the entire backend image build failed before deployment could continue.

## Applied Fix

The runtime image was redesigned to avoid package installation completely.

| Area | Change |
| --- | --- |
| `backend/Dockerfile` | Removed the runtime-stage `apt-get` / `curl` installation dependency. |
| `backend/src/TendexAI.ContainerHealthcheck/` | Added a lightweight .NET console healthcheck utility that probes `http://127.0.0.1:8080/api/v1/health`. |
| `backend/Dockerfile` | Published the healthcheck utility during the build stage and copied it into the runtime image. |
| `backend/Dockerfile` | Replaced the image `HEALTHCHECK` command to execute the .NET healthcheck utility instead of `curl`. |
| `infrastructure/docker-compose.prod.yml` | Replaced the production backend service healthcheck to use the same .NET utility, keeping health semantics consistent between image and compose orchestration. |

## Validation Summary

The repository diff was reviewed to confirm that the backend runtime and production compose definitions no longer contain `curl`-based health checks or runtime `apt-get install curl` steps.

## Deployment Status

The fix was committed as `1636343` with message `fix(docker): replace curl healthcheck with dotnet probe`, pushed to `main`, and deployed successfully through GitHub Actions run **73** (`run_id: 25324611079`). The workflow completed successfully across all stages: **Test Gate**, **Build Docker Images**, and **Deploy to Production**.

## Files Included In This Fix

| File | Purpose |
| --- | --- |
| `backend/Dockerfile` | Removes package-manager dependency and publishes the new healthcheck tool. |
| `backend/src/TendexAI.ContainerHealthcheck/TendexAI.ContainerHealthcheck.csproj` | Defines the standalone .NET healthcheck utility. |
| `backend/src/TendexAI.ContainerHealthcheck/Program.cs` | Implements the HTTP probe logic with proper exit codes. |
| `infrastructure/docker-compose.prod.yml` | Aligns production orchestration healthcheck with the new probe. |

## Live Verification

The live environment responded successfully after deployment. An HTTP HEAD request to `https://mof.netaq.pro/` returned **HTTP/2 200** from Nginx, and a browser verification confirmed that the platform loads the Arabic login page at `https://mof.netaq.pro/auth/login?redirect=/` without a visible runtime error screen.

## Outcome

The production deployment blocker caused by runtime-stage `curl` installation is resolved. The fix batch is now deployed successfully on the live environment, and the backend image no longer depends on transient `apt-get` package index availability during CI builds.
