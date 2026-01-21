# Abies.Conduit.IntegrationTests

These tests are intended as **near-E2E integration tests**: they verify Conduit's update/view logic and API calls without Playwright and without Aspire AppHost.

## Idea

- We inject a fake `HttpClient` into `Abies.Conduit.Services.ApiClient`.
- We test deterministically:
  - which API endpoints are called
  - which state/DOM renders result from responses

> Note: this is intentionally not a browser test layer. For real browser behavior, we keep a small Playwright smoke suite.
