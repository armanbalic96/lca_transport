# Transport GWP Calculator (.NET + Angular)

A port of the Claude Design prototype *Transport GWP Calculator (Wizard)*: a transport register plus a
three-step wizard (Cargo → Route → Results) that calculates transport GWP (EN 15804+A2, GWP-total) for a shipment.

```
backend/   ASP.NET Core 10 Web API (Domain / Application / Infrastructure / Api / Tests)
frontend/  Angular 21, standalone components, signals, HTML + SCSS
```

## What moved where

| Prototype (React, in the browser) | This version |
|---|---|
| `LCA_DATA` reference data | `TransportGwp.Domain/ReferenceData/TransportReferenceData.cs`, served by `GET /api/referencedata` |
| `legCalc`, `LCA_API.*` (impact, mode comparison, validation), statistics, indicators | `TransportGwp.Application/Implementation/TransportCalculator.cs` → `POST /api/calculations` |
| Report / CSV / JSON export | `ExportService.cs` → `POST /api/calculations/export/{report\|csv\|json}` |
| Transport register in `localStorage` | SQLite via EF Core → `/api/transports` (summary recomputed server-side on every save) |
| Google Places / Routes called from the browser with the key in the page | Proxied by `GeoService.cs` → `/api/geo/*` (server key stays on the server; Nominatim fallback) |
| `window.claude.complete` (AI recommendations, "Explain this result") | `AiAdvisor.cs` + `ClaudeLlmClient.cs` (official Anthropic SDK) → `/api/ai/*` |
| CSS (LCA Tools design tokens) | `frontend/src/styles/_tokens.scss`, `_app.scss`, `_wizard.scss` |

The frontend never calculates GWP itself: it posts the wizard state to `/api/calculations` (debounced) and renders the result.

### Intentional differences from the prototype

- **Consistent distances:** in the prototype the single-route totals used the Google road distance while the export,
  statistics and mode comparison used great-circle × routing factor. Here every number uses the same distance.
- **Mode comparison** keeps the Google road distance when swapping between road modes.
- **Validation** checks the whole route (first leg's origin → last leg's destination) when multi-leg is on.
- The AI assistant bar (free-text route entry) and the stakeholder report panel were defined in the prototype but
  not shown in the wizard, so they are not ported. "AI recommendations" and "Explain this result" are.

## Running locally

Requirements: .NET SDK 10, Node 24 / npm 11.

```bash
dotnet run --project backend/TransportGwp.Api --launch-profile http
```

```bash
npm --prefix frontend start
```

Open http://localhost:4200. The Angular dev server proxies `/api` to http://localhost:5081 (`frontend/proxy.conf.json`).
The SQLite file `transports.db` is created next to the API on first start (`EnsureCreated` — switch to EF migrations before production).

### Optional configuration (keep keys out of appsettings.json)

Everything works without keys: place search falls back to OpenStreetMap Nominatim, distances fall back to
great-circle × routing factor, the map shows a placeholder and the AI panels are hidden.

```bash
cd backend/TransportGwp.Api
dotnet user-secrets init
dotnet user-secrets set "GoogleMaps:ServerApiKey" "<key with Places API (New) + Routes API>"
dotnet user-secrets set "GoogleMaps:BrowserApiKey" "<key with Maps JavaScript API, restricted by HTTP referrer>"
dotnet user-secrets set "Claude:ApiKey" "<Anthropic API key>"
```

The AI requests use `claude-opus-5-5` at low effort with server-side refusal fallback enabled. Only aggregated
route figures are sent (modes, distances, GWP, cargo materials) — no transport names or user data.

## Tests

```bash
dotnet test backend
```

Covers the calculation rules (load adjustment, distance sources, vehicle/fuel/return multipliers, cargo by volume,
validation, mode ranking, statistics), the register service, exports, polyline decoding and AI-reply parsing.
