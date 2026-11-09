# RaceDay Platform — REST API & ASP.NET Core MVC Web Application (Parts 2 & 3)

<img width="1920" height="1080" alt="RaceDay API dashboard" src="https://github.com/user-attachments/assets/23df9e73-fb59-44a2-9c84-8fd60e3f7eb7" />

YouTube Link (Part 2):
https://youtu.be/MI8AG6iuDBY?si=QFPeglouq8kheIX5

**RaceDay** is a full-stack .NET 10 platform for managing athletic race events (Running, Walking, and Cycling), participant category enrolments, cloud-based media storage via **Azure Blob Storage**, and official race timing & results.

---

## Architecture Overview

The solution (`RaceDay.slnx`) consists of two strictly separated projects under `src/`:

1. **`src/RaceDay.Api` (Part 2 RESTful Web API)**
   - Owns all database access (Entity Framework Core + SQL Server), domain business logic, JWT authentication, and **Azure Blob Storage** (`Azure.Storage.Blobs`) integration.
   - Automatically initialises the database schema and seeds realistic South African race events, categories, enrolments (`Confirmed` and `Pending`), and completed race results on startup.
2. **`src/RaceDay.Web` (Part 3 ASP.NET Core MVC Web Application)**
   - Consumes `RaceDay.Api` exclusively via a typed `HttpClient` (`IRaceDayApiClient` / `RaceDayApiClient`).
   - Contains **zero direct database access** and **zero replicated business logic**.
   - Maintains user authentication state and role (`Organiser` vs `Participant`) across pages using server-side ASP.NET Core Session (`HttpOnly`, `SameSite=Lax`, anti-CSRF protected) and custom authorization filters (`[RequireLogin]`, `[RequireRole]`).
   - Containerised using a multi-stage production `Dockerfile`.

---

## Key Features (Part 3)

youtube video:https://youtu.be/1c6dtrQWZak?si=IDnyg7vTM_SNgt_-

### Authentication & Role-Based Navigation
- Register and log in as either an **Organiser** or **Participant** through the MVC interface (`/Account/Register`, `/Account/Login`).
- Distinct role-aware navigation bars and sub-navigation menus for Organisers and Participants.
- Protected routes redirect unauthenticated visitors to Login and prevent cross-role access.
- Secure POST logout (`/Account/Logout`) clears session state and prevents back-button cache leaks.

### Organiser Interface (`/Organiser/*`)
- **Dashboard (`/Organiser/Dashboard`)**: Summary KPIs, all created events, total enrolments per event, and chronological upcoming race countdown.
- **Events Management (`/Organiser/Events`, `CreateEvent`, `EditEvent`)**: Create, edit, and delete events (`Name`, `Description`, `EventDate`, `StartTime`, `Location`, `City`, `Province`, `Distance`, `EventType`, `Status`) and upload an **Event Banner Image** stored in Azure Blob Storage via the API.
- **Category Management (`/Organiser/Categories`)**: Add and manage age or distance categories (`Name`, `Distance`, `EntryFee`, `MaxParticipants`) for each event.
- **Enrolments Roster (`/Organiser/Enrolments`)**: View all participants enrolled for a specific event, filter by status, and update enrolment status (`Confirmed`, `Pending`, `Registered`, `Withdrawn`, `DNS`) with colour-coded badges.
- **Results Entry & Publishing (`/Organiser/Results`)**: Capture and publish finish times (`HH:mm:ss`), finishing positions, and statuses (`Finished`, `DNF`, `DSQ`), and mark events as `Completed`.

### Participant Interface (`/Events/*`, `/Participant/*`, `/Profile`)
- **Explore Upcoming Events (`/Events`)**: Browse upcoming events with filtering by keyword, discipline (`Running`, `Walking`, `Cycling`), city, and date range.
- **Event Details & Instant Enrolment (`/Events/Details/{id}`)**: View full event details, the Azure Blob Storage banner image, available categories, remaining capacity, and official results leaderboard. Logged-in Participants can enrol in any open category with one click.
- **My Enrolments (`/Participant/Enrolments`)**: Personal view of all enrolled events, assigned race bib numbers, and visually distinct colour-coded enrolment statuses (**Green** for `Confirmed`, **Amber** for `Pending` / `Registered`, **Red** for `Withdrawn` / `DNS`).
- **My Results (`/Participant/Results`)**: Personal race history across all completed events showing event name, date, category, recorded finish time, and formatted finishing position (e.g. `2nd out of 3 finishers` / `47th out of 312 finishers`).
- **Profile Page (`/Profile`)**: View and update personal details and upload a **Profile Picture** stored in Azure Blob Storage via the Part 2 API.

---

## Seeded Demo Accounts

When `RaceDay.Api` starts, it automatically seeds demo accounts and sample events so you can test immediately:

| Role | Email | Password |
| --- | --- | --- |
| **Organiser** | `organiser@raceday.co.za` | `RaceDay@2026` |
| **Participant** | `runner@raceday.co.za` | `RaceDay@2026` |

---

## Running with Docker (Single Command)

The MVC application (`RaceDay.Web`) is containerised with a multi-stage `Dockerfile` at the repository root.

### Start the Container with a Single Command

Build and run the containerised MVC application on port `8080` (automatically connecting to the API running on the host at `http://host.docker.internal:52070`):

```bash
docker build -t raceday-web . && docker run --rm -p 8080:8080 -e API_BASE_URL=http://host.docker.internal:52070 --name raceday-web raceday-web
```

Or if the image is already built, start it with a single `docker run` command:

```bash
docker run --rm -p 8080:8080 -e API_BASE_URL=http://host.docker.internal:52070 --name raceday-web raceday-web
```

Then open **http://localhost:8080** in your browser.

*(Optional)* You can also use Docker Compose:

```bash
docker compose up --build
```

---

## Running Locally (Development)

### Prerequisites
- .NET 10 SDK
- SQL Server LocalDB (or SQL Server instance)
- Azure Storage Account connection string or Azurite emulator (configured under `AzureStorage:ConnectionString` in `src/RaceDay.Api/appsettings.json`; defaults to `UseDevelopmentStorage=true` with automatic safe fallback if offline)

### 1. Start the Part 2 API (`src/RaceDay.Api`)

```bash
dotnet run --project src/RaceDay.Api/RaceDay.Api.csproj --launch-profile RaceDay.Api
```

- API listens on `http://localhost:52070` (and `https://localhost:52069`) with Swagger UI at root.

### 2. Start the Part 3 MVC Web App (`src/RaceDay.Web`)

In a second terminal:

```bash
dotnet run --project src/RaceDay.Web/RaceDay.Web.csproj --launch-profile RaceDay.Web
```

- MVC Web application listens on `https://localhost:52071` and `http://localhost:52072`.

---

## API Endpoints Summary (`RaceDay.Api`)

| Endpoint | Method | Role | Description |
| --- | --- | --- | --- |
| `/api/Auth/register` | POST | Public | Register a new Organiser or Participant |
| `/api/Auth/login` | POST | Public | Authenticate and return a JWT token |
| `/api/Events` | GET | Public | Get events with optional search, type, city, and date filters |
| `/api/Events/mine` | GET | Organiser | Get all events created by the authenticated Organiser |
| `/api/Events/{id}` | GET | Public | Get event details and categories by ID |
| `/api/Events` | POST | Organiser | Create a new event |
| `/api/Events/{id}` | PUT | Organiser | Update an event |
| `/api/Events/{id}/banner` | POST | Organiser | Upload event banner image to Azure Blob Storage |
| `/api/Events/{id}` | DELETE | Organiser | Delete an event |
| `/api/Categories/event/{eventId}` | GET | Public | Get categories for an event |
| `/api/Categories` | POST | Organiser | Create a category for an event |
| `/api/Categories/{id}` | DELETE | Organiser | Delete a category |
| `/api/Enrolments` | POST | Participant | Enrol in a race category |
| `/api/Enrolments/my` | GET | Participant | Get all enrolments for the authenticated Participant |
| `/api/Enrolments/user/{userId}` | GET | Auth | Get enrolments for a user |
| `/api/Enrolments/event/{eventId}` | GET | Organiser | Get all participant enrolments for an event |
| `/api/Enrolments/{id}/status` | PUT | Organiser | Update enrolment status (`Confirmed`, `Pending`, `Registered`, `Withdrawn`, `DNS`) |
| `/api/Results` | POST | Organiser | Submit or update a participant's finish time and position |
| `/api/Results/event/{eventId}` | GET | Public | Get published results for an event |
| `/api/Results/my` | GET | Participant | Get personal race history with `PositionDisplay` (e.g. `47th out of 312 finishers`) |
| `/api/Profile/me` | GET / PUT | Auth | View or update personal profile information |
| `/api/Profile/me/picture` | POST | Auth | Upload profile picture to Azure Blob Storage |
