# Calgary 311 Explorer

Portfolio project: an ASP.NET Core Razor Pages app (C#, .NET 10, EF Core, SQLite) that syncs the City of Calgary's 311 Service Requests open dataset (`iahh-g8bj`) and lets users browse, filter and analyze it. See README.md for features, data fields and the roadmap.

## How we work on this project

Tavish is learning C# coming from Java, and he needs to be able to explain every part of this code in a job interview. So:

- **Claude writes the feature code.** After each piece, walk Tavish through it: the approach, the C# and ASP.NET concepts involved, and how they compare to Java.
- Keep the code readable over clever, with short comments on the non-obvious parts, so it's easy to explain.
- When something fails, explain what the error means as part of fixing it.
- After finishing a roadmap step, tick it off in README.md.

## Commands

From the repository root:

- Build: `dotnet build`
- Test: `dotnet test`
- Run the web app: `dotnet run --project src/Calgary311.Web`
- New migration: `dotnet ef migrations add <Name> --project src/Calgary311.Web`
- Apply migrations: `dotnet ef database update --project src/Calgary311.Web`

## Conventions

- Nullable reference types are on; keep warnings at zero.
- Use `async`/`await` for database and HTTP calls.
- Inject dependencies through constructors (primary constructors are fine).
- API settings come from `OpenCalgaryOptions` (appsettings.json, section `OpenCalgary`); don't hardcode URLs.
- The local database (`*.db`) is not committed.

## Data notes

- API: `https://data.calgary.ca/resource/iahh-g8bj.json`, Socrata SoQL (`$where`, `$order`, `$limit`, `$offset`).
- Dates come back as strings like `2026-10-01T00:00:00.000`.
- `service_request_id` is unique; use it to update existing rows instead of inserting duplicates.
- The full dataset is 7M+ rows; only load a recent window (default 90 days).
