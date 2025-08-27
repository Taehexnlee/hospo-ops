Hospo-Ops API

Overview

Hospo-Ops is a restaurant operations management API built with .NET 8 WebAPI and EF Core.
The project is currently in the MVP stage and focuses on the EOD (End of Day) reporting system.
	•	Development DB: SQLite (lightweight & cross-platform)
	•	Production DB: SQL Server (planned for deployment)

⸻

Quick Start

0. Prerequisites
	•	.NET 8 SDK
	•	(Optional) SQLite CLI for inspecting the local DB

1. Restore & Build
cd api
dotnet restore
dotnet build -c Debug

2. Apply Migrations & Create DB
dotnet new tool-manifest 2>/dev/null || true
dotnet tool install dotnet-ef --version "9.0.8" 2>/dev/null || dotnet tool update dotnet-ef --version "9.0.8"

dotnet tool run dotnet-ef -- migrations add InitialCreateSqlite
dotnet tool run dotnet-ef -- database update

3. Run the API
dotnet run

	•	Health Check → http://localhost:5047/health
	•	Example Endpoint → http://localhost:5047/api/eod/1/2025-08-27

⸻
Project Structure

hospo-ops/
 └── api/              # .NET WebAPI project
     ├── Controllers/  # API Controllers
     ├── Data/         # EF Core DbContext
     ├── Models/       # Entity Models
     ├── Migrations/   # EF Core migrations
     └── Program.cs    # App startup


Roadmap
	•	MVP Setup (SQLite + EF Core)
	•	EOD Report CRUD
	•	Square API integration
	•	AI-powered daily sales analysis
	•	Switch to SQL Server in production
	•	Deploy to Azure

License

MIT