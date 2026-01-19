# Abies.Conduit.IntegrationTests

Deze tests zijn bedoeld als **near‑E2E integratietests**: ze testen Conduit’s update/view logic en API-calls zonder Playwright en zonder Aspire AppHost.

## Idee

- We injecteren een fake `HttpClient` in `Abies.Conduit.Services.ApiClient`.
- We testen deterministisch:
  - welke API endpoints aangeroepen worden
  - welke state/DOM-renders ontstaan na responses

> Let op: dit is expres géén browser-testlaag. Voor echte browser-behavior houden we een kleine Playwright smoke suite.
