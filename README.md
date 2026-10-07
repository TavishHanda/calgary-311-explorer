# Calgary 311 Explorer

A web app for browsing and analyzing the City of Calgary's 311 service requests. It pulls live data from the City's open data portal, stores it locally, and lets you search requests by community, service type and status, and see how long each department takes to close them.

Built with C#, ASP.NET Core Razor Pages, Entity Framework Core and SQLite.

> **Status:** in development. See the [roadmap](#roadmap) for progress.

## Features

- **Sync from Open Calgary:** loads recent 311 requests from the City's API and updates existing records on each run instead of duplicating them.
- **Browse and search:** filter requests by community, service type, status, department and date range, with a detail page for each request.
- **Dashboard:** most common request types, open vs. closed counts, and average days to close by department.
- **REST endpoint:** `GET /api/requests?community=Panorama Hills&status=Open` returns matching requests as JSON.
- **Tests:** xUnit tests cover data mapping, filtering and the days-to-close calculation.

## The data

The app uses the [311 Service Requests](https://data.calgary.ca/Services-and-Amenities/311-Service-Requests/iahh-g8bj) dataset (ID `iahh-g8bj`) from the City of Calgary's Open Data Portal. It covers requests from 2012 to the present, updates daily, and has over 7 million rows, so the app loads a recent window (90 days by default, about 140,000 requests).

The dataset is served through a Socrata API that returns JSON and accepts SoQL query parameters. For example, the five newest requests:

```
https://data.calgary.ca/resource/iahh-g8bj.json?$order=requested_date DESC&$limit=5
```

| API field | Model property | Example |
| --- | --- | --- |
| `service_request_id` | `ServiceRequestId` | Unique ID from the City |
| `requested_date` | `RequestedDate` | 2026-10-01 |
| `updated_date` | `UpdatedDate` | |
| `closed_date` | `ClosedDate` | Empty while open |
| `status_description` | `Status` | Open, Closed |
| `source` | `Source` | Phone, App, Other |
| `service_name` | `ServiceName` | Roads - Traffic Signal Construction Inquiry |
| `agency_responsible` | `AgencyResponsible` | OS - Mobility |
| `address` | `Address` | |
| `comm_code` / `comm_name` | `CommunityCode` / `CommunityName` | Panorama Hills |
| `longitude` / `latitude` | `Longitude` / `Latitude` | |

## Tech stack

| Layer | Technology |
| --- | --- |
| Language | C# (.NET 10) |
| Web framework | ASP.NET Core Razor Pages |
| Data access | Entity Framework Core |
| Database | SQLite |
| HTTP | HttpClient with System.Text.Json |
| Testing | xUnit |
| Tools | Visual Studio, Git, GitHub |

## Getting started

### Prerequisites

- [.NET SDK](https://dotnet.microsoft.com/download) (10.0 or later)
- Visual Studio 2022 or later with the **ASP.NET and web development** workload, or VS Code with the C# Dev Kit extension

### Run it locally

```bash
git clone https://github.com/TavishHanda/calgary-311-explorer.git
cd calgary-311-explorer

# First time only: install the EF Core tools and create the database
dotnet tool install --global dotnet-ef
cd src/Calgary311.Web
dotnet ef migrations add InitialCreate
dotnet ef database update

dotnet run
```

Open the URL shown in the terminal (usually `https://localhost:5001`).

### Run the tests

From the repository root:

```bash
dotnet test
```

### Configuration

Settings live in `src/Calgary311.Web/appsettings.json` under `OpenCalgary`:

| Setting | Default | What it does |
| --- | --- | --- |
| `DatasetId` | `iahh-g8bj` | The 311 dataset to load |
| `InitialSyncDays` | `90` | How many days of requests the first sync loads |
| `PageSize` | `5000` | Rows per API request |
| `AppToken` | empty | Optional [Socrata app token](https://dev.socrata.com/docs/app-tokens.html) for higher rate limits |

## Project structure

```
calgary-311-explorer/
├── Calgary311Explorer.slnx
├── src/
│   └── Calgary311.Web/
│       ├── Data/                 # AppDbContext
│       ├── Models/               # ServiceRequest
│       ├── Services/             # Sync service (to build)
│       ├── Pages/                # Razor Pages
│       ├── wwwroot/css/          # Styles
│       ├── OpenCalgaryOptions.cs # API settings
│       └── Program.cs
├── tests/
│   └── Calgary311.Tests/         # xUnit tests
└── docs/
    └── screenshots/
```

## Roadmap

- [x] **1. Setup:** solution, web and test projects, `ServiceRequest` model, `AppDbContext`, starter page
- [ ] **2. Database:** first migration (`InitialCreate`) and database created locally
- [ ] **3. Sync service:** fetch requests from the API in pages, map JSON to `ServiceRequest`, insert new rows and update changed ones; a button or command to run it
- [ ] **4. Browse page:** table of requests with filters (community, service type, status, department, date range) and paging
- [ ] **5. Request detail page:** everything known about one request
- [ ] **6. API endpoint:** `GET /api/requests` with the same filters
- [ ] **7. Dashboard:** top service types, open vs. closed, average days to close by department
- [ ] **8. Tests:** 8–10 xUnit tests on mapping, filters and dashboard calculations
- [ ] **9. Docs:** screenshots and a short walkthrough in this README

Stretch: a map of open requests using the coordinates, scheduled daily sync, deployment to Azure App Service.

## What I learned

_To fill in once built: what was new coming from Java, working with a real public API, and what I'd do differently._

## Data license

Contains information licensed under the City of Calgary's open data terms of use. See the [dataset page](https://data.calgary.ca/Services-and-Amenities/311-Service-Requests/iahh-g8bj) for details.

## Author

**Tavish Handa** · [LinkedIn](https://www.linkedin.com/in/tavish-handa) · [GitHub](https://github.com/TavishHanda)
