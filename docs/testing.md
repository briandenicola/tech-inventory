# Testing Guide

> **Authority**: This document operationalizes PRD §7.5 *Local Testing & Validation*
> and constitution §7 *Testing*. When this guide and those documents conflict,
> the PRD and constitution win — and this guide should be updated to match.

This is the developer-facing guide to writing and running tests in this
project. If you've just cloned the repo, start with [Quick Start](#-quick-start).
If you're about to write a test, start with [Choosing the Right Test Type](#-choosing-the-right-test-type).

---

## 📑 Table of Contents

- [Philosophy](#-philosophy)
- [Quick Start](#-quick-start)
- [Choosing the Right Test Type](#-choosing-the-right-test-type)
- [Backend Unit Tests (xUnit)](#-backend-unit-tests-xunit)
- [Backend Integration Tests (Testcontainers)](#-backend-integration-tests-testcontainers)
- [Frontend Unit Tests (Vitest)](#-frontend-unit-tests-vitest)
- [Playwright E2E Tests](#-playwright-e2e-tests)
- [Accessibility Tests](#-accessibility-tests)
- [Performance Tests (Lighthouse CI)](#-performance-tests-lighthouse-ci)
- [Contract Tests](#-contract-tests)
- [Test Data & Fixtures](#-test-data--fixtures)
- [Authentication in Tests](#-authentication-in-tests)
- [The `make test` Contract](#-the-make-test-contract)
- [Debugging Failing Tests](#-debugging-failing-tests)
- [Flaky Test Policy](#-flaky-test-policy)
- [Writing a New Critical Journey](#-writing-a-new-critical-journey)
- [CI Behavior](#-ci-behavior)
- [For AI Agents (Copilot)](#-for-ai-agents-copilot)
- [Common Pitfalls](#-common-pitfalls)

---

## 🧭 Philosophy

We test for **three reasons** — in priority order:

1. **Confidence to ship**: a green build means a deployable build
2. **Confidence to change**: tests are scaffolding for refactoring
3. **Executable specification**: tests document what the system actually does

We do **not** test for:

- Coverage numbers (coverage is a *floor*, not a target)
- "Because the tool can"
- To compensate for missing types or weak validation

### Non-negotiables

- ✅ Every change has tests at the appropriate level
- ✅ Playwright is the only E2E framework
- ✅ Tests run locally with one command (`make test`)
- ✅ CI runs the exact same commands a developer runs
- ✅ Flaky tests are bugs — fix or delete within a working day
- ✅ Tests own their data — no shared fixtures across files
- ❌ No mocked databases in integration tests
- ❌ No mocked API in E2E tests
- ❌ No tests that require internet access

---

## 🚀 Quick Start

### First-time setup

```bash
# Prereqs: Docker, .NET 10 SDK, Node 22+, GNU Make
git clone <repo> && cd <repo>

# Install JS deps and Playwright browsers
cd src/TechInventory.Web && npm ci
npx playwright install --with-deps
cd -

# Restore .NET deps
dotnet restore