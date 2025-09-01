Quick Start
	0.	Prerequisites

	•	.NET 8 SDK
	•	(Optional) SQLite CLI

	1.	Restore & Build

	•	cd api
	•	dotnet restore
	•	dotnet build -c Debug

	2.	Apply migrations (SQLite, dev)

	•	dotnet new tool-manifest (run once; ignore if already created)
	•	dotnet tool install dotnet-ef –version 9.0.8 (or: dotnet tool update dotnet-ef –version 9.0.8)
	•	dotnet tool run dotnet-ef – migrations add InitialCreateSqlite
	•	dotnet tool run dotnet-ef – database update

	3.	Run

	•	dotnet run
	•	Or bind to this guide’s endpoint: dotnet run –no-launch-profile –urls http://127.0.0.1:5080

Health: GET http://127.0.0.1:5080/health
Sample: GET http://127.0.0.1:5080/api/stores
Swagger (Development): http://127.0.0.1:5080/swagger

Note: If you run without –urls, the dev profile may pick a different port (e.g., 5047).

⸻

Configuration

Environment variables
	•	ASPNETCORE_ENVIRONMENT = Development (enables Swagger, console exporters, etc.)
	•	Api__Key = dev-super-secret (API key expected in X-Api-Key header)
	•	ConnectionStrings__Default = Data Source=hospoops.dev.db (defaults to SQLite file in dev; use SQL Server in prod)

CORS
	•	Dev policy allows http://localhost:3000
	•	In production, configure allow-list via Cors:AllowedOrigins in appsettings.*.json

Example appsettings.Development.json:
{
“Api”: { “Key”: “dev-super-secret” },
“ConnectionStrings”: { “Default”: “Data Source=hospoops.dev.db” },
“Cors”: { “AllowedOrigins”: [ “http://localhost:3000” ] }
}

⸻

Security

API Key
	•	All non-preflight requests require header X-Api-Key.
	•	Example: curl -H ‘X-Api-Key: dev-super-secret’ http://127.0.0.1:5080/api/stores

CORS & Preflight (OPTIONS)
	•	Preflight requests (OPTIONS) are short-circuited with 204 and CORS headers for allowed origins.
	•	Preflight bypasses API key and rate limiter.
	•	Disallowed origins still receive 204 without CORS headers → blocked by the browser.

Security Headers (added by middleware)
	•	X-Frame-Options: DENY
	•	X-Content-Type-Options: nosniff
	•	Referrer-Policy: strict-origin-when-cross-origin
	•	Content-Security-Policy: object-src ‘none’; form-action ‘self’; frame-ancestors ‘none’
	•	Cross-Origin-Opener-Policy: same-origin
	•	Cross-Origin-Embedder-Policy: credentialless
	•	Cross-Origin-Resource-Policy: same-site

⸻

Rate Limiting

Policy name: api
Fixed window: 5 requests / 10 seconds per Remote IP (no queue; auto-replenish)
On exceed: 429 Too Many Requests with Retry-After: 10

Apply scope
	•	Globally: app.MapControllers().RequireRateLimiting(“api”)
	•	Or per controller: [EnableRateLimiting(“api”)]

Quick check (expect 200 x5 then 429 with Retry-After: 10)
	•	Repeat 5 times: curl -s -o /dev/null -w “%{http_code}\n” -H ‘X-Api-Key: dev-super-secret’ http://127.0.0.1:5080/api/stores
	•	Sixth call should return 429; header Retry-After: 10 present

⸻

Observability

Serilog
	•	Structured request logging
	•	Redacts X-Api-Key in logs
	•	Adds X-Correlation-Id (also returned as a response header)

OpenTelemetry
	•	Traces: ASP.NET Core + HttpClient
	•	Metrics: ASP.NET Core + HttpClient + Kestrel + RateLimiter
	•	Development: console exporters enabled
	•	Production: OTLP exporter (metrics) enabled; point to your collector
	•	Noise filters in tracing: /health, /swagger

⸻

Project Structure

hospo-ops/
└─ api/  (.NET WebAPI)
├─ Controllers/  (Stores, Employees, EOD, …)
├─ Data/         (EF Core DbContext)
├─ Models/       (Entities + DTOs)
├─ Migrations/   (EF migrations)
└─ Program.cs    (Composition root)

Domain Features
	•	Stores: CRUD, unique name enforcement
	•	Employees: CRUD with store-level validation, hire date / role validation, paging & filtering
	•	EOD Reports: CRUD with store+date uniqueness, validation, cascade on delete

⸻

Testing

Run all tests
	•	dotnet build -c Debug
	•	dotnet test api.tests -v minimal

Recent run: 23/23 passing (includes rate-limit and CRUD tests).

⸻

Handy Commands

Port in use / stuck process (macOS)
	•	Free :5080 → lsof -ti :5080 | xargs -r kill -9

Start app in background and tail logs
	•	export ASPNETCORE_ENVIRONMENT=Development
	•	export Api__Key=‘dev-super-secret’
	•	rm -f /tmp/api.pid /tmp/api.log
	•	dotnet run –project api –no-launch-profile –urls http://127.0.0.1:5080 > /tmp/api.log 2>&1 &
	•	echo $! > /tmp/api.pid
	•	tail -f /tmp/api.log

Stop background app
	•	[ -f /tmp/api.pid ] && kill “$(cat /tmp/api.pid)” 2>/dev/null || true
	•	rm -f /tmp/api.pid

⸻

Troubleshooting

Address already in use / port conflict
	•	Kill processes bound to :5080 using the command above.

curl shows 000
	•	App not listening yet, wrong URL, or missing http:// scheme. Check /tmp/api.log.

Preflight blocked from frontend
	•	Ensure the origin is in the CORS allow-list and you’re hitting the correct port/URL.

401 Unauthorized
	•	Missing or incorrect X-Api-Key.

⸻

Roadmap
	•	MVP setup (SQLite + EF Core) — done
	•	EOD Report CRUD — done
	•	Square API integration
	•	AI-powered daily sales analysis
	•	Switch to SQL Server in production
	•	Deploy to Azure

⸻

License

MIT

Tip: For a one-shot end-to-end verification (build → test → run → rate-limit check → stop), adapt the commands under “Handy Commands” into a local script (e.g., scripts/e2e.sh).