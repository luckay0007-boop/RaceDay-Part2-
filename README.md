# RaceDay API

<img width="1920" height="1080" alt="RaceDay API dashboard" src="https://github.com/user-attachments/assets/23df9e73-fb59-44a2-9c84-8fd60e3f7eb7" />

RaceDay API is a .NET 10 ASP.NET Core backend designed for managing race events, participant registrations, category enrolments, and official race results. The system supports role-based access control for two main user roles:

- Organiser
- Participant

It provides a secure REST API with JWT authentication, SQL Server persistence via Entity Framework Core, and Swagger-based API documentation.

## Overview

This project is built to support the full lifecycle of a race event:

1. Users register and authenticate.
2. Organisers create and manage event listings.
3. Organisers define race categories and fees.
4. Participants register for categories.
5. Results are submitted and retrieved for individual events.

The API is structured around a clean service layer, EF Core data access, and controller-based endpoints.

---

## Features

- User registration and login
- JWT-based authentication and authorization
- Role-based access control
- Event management
- Category creation per event
- Participant enrolment management
- Race result submission and tracking
- Swagger / OpenAPI documentation
- Automatic DB migration on startup
- SQL Server integration using EF Core
- BCrypt-based password hashing

---

## Tech Stack

- ASP.NET Core Web API
- .NET 10
- C#
- Entity Framework Core
- SQL Server / LocalDB
- JWT Bearer Authentication
- Swagger / Swashbuckle
- BCrypt.Net-Next

---

## Project Structure

```text
RaceDay-Part2-/
├── Controllers/
│   ├── AuthController.cs
│   ├── CategoriesController.cs
│   ├── EnrolmentsController.cs
│   ├── EventsController.cs
│   └── ResultsController.cs
├── Data/
│   └── RaceDayDbContext.cs
├── DTOs/
│   ├── Auth/
│   ├── Enrolments/
│   ├── Events/
│   └── Results/
├── Middleware/
│   └── ExceptionHandlingMiddleware.cs
├── Models/
│   ├── Category.cs
│   ├── Enrolment.cs
│   ├── Event.cs
│   ├── Result.cs
│   ├── Role.cs
│   ├── User.cs
│   └── UserRole.cs
├── Services/
│   ├── AuthService.cs
│   ├── CategoryService.cs
│   ├── EnrolmentService.cs
│   ├── EventService.cs
│   ├── ResultService.cs
│   ├── IAuthService.cs
│   ├── ICategoryService.cs
│   ├── IEnrolmentService.cs
│   ├── IEventService.cs
│   └── IResultService.cs
├── appsettings.json
├── appsettings.Development.json
├── Program.cs
├── RaceDay.Api.csproj
├── RaceDay.Api.http
├── RaceDay.Api.slnx
├── README.md
└── .gitignore
```

---

## API Endpoints

### Authentication

| Endpoint | Method | Description |
| --- | --- | --- |
| `/api/Auth/register` | POST | Register a new user |
| `/api/Auth/login` | POST | Authenticate and return a JWT token |

### Events

| Endpoint | Method | Description |
| --- | --- | --- |
| `/api/Events` | GET | Get all events |
| `/api/Events/{id}` | GET | Get event by ID |
| `/api/Events` | POST | Create a new event (Organiser only) |
| `/api/Events/{id}` | PUT | Update an event (Organiser only) |
| `/api/Events/{id}` | DELETE | Delete an event (Organiser only) |

### Categories

| Endpoint | Method | Description |
| --- | --- | --- |
| `/api/Categories/event/{eventId}` | GET | Get categories for an event |
| `/api/Categories` | POST | Create a category (Organiser only) |

### Enrolments

| Endpoint | Method | Description |
| --- | --- | --- |
| `/api/Enrolments` | POST | Register a participant for a category |
| `/api/Enrolments/user/{userId}` | GET | Get all enrolments for a user |

### Results

| Endpoint | Method | Description |
| --- | --- | --- |
| `/api/Results` | POST | Submit a race result (Organiser only) |
| `/api/Results/event/{eventId}` | GET | Get results for an event |

---

## Authentication & Authorization

This project uses JWT bearer tokens for secure access control.

### Roles

The seeded roles in the database are:

- Organiser
- Participant

### Example Authorization Header

```http
Authorization: Bearer <your-jwt-token>
```

Users must be authenticated and authorized according to their assigned role before accessing protected endpoints.

---

## Database Configuration

The project uses SQL Server and is configured in `appsettings.json`.

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=RaceDay;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

If you are using a different SQL Server instance, update the connection string to match your environment.

---

## JWT Configuration

JWT settings are defined in `appsettings.json`:

```json
"JwtSettings": {
  "SecretKey": "RaceDay-SuperSecret-JWT-Key-2026-Must-Be-At-Least-32-Characters-Long!",
  "Issuer": "RaceDay.Api",
  "Audience": "RaceDay.Client",
  "ExpiryInHours": "24"
}
```

Important: for production, replace the default secret with a secure environment-based secret.

---

## Prerequisites

Before running the project, make sure you have:

- .NET 10 SDK
- SQL Server LocalDB or another compatible SQL Server instance
- Visual Studio 2022 or VS Code
- Git

---

## Getting Started

### 1. Clone the repository

```bash
git clone https://github.com/luckay0007-boop/RaceDay-Part2-.git
cd RaceDay-Part2-
```

### 2. Restore dependencies

```bash
dotnet restore
```

### 3. Configure the database

Update `appsettings.json` if needed to match your local SQL Server configuration.

### 4. Run the API

```bash
dotnet run
```

The application will start and automatically apply EF Core migrations in development mode.

### 5. Open Swagger UI

When running in development, Swagger loads at the root URL:

```text
https://localhost:<port>/
```

This provides interactive documentation and testing tools for all endpoints.

---

## Security Notes

- Passwords are hashed with BCrypt.
- JWT tokens are signed using the configured secret key.
- Authorization is enforced through role-based attributes.
- Production secrets should be stored in secure environment variables or secret management tools.

---

## Business Flow

The project follows this typical workflow:

1. A user registers as an organiser or participant.
2. The organiser creates a race event.
3. Event categories are created with distance and entry fees.
4. Participants register for categories.
5. Organisers submit race results.
6. Data is returned through the API for reporting and display.

---

## Contributing

Contributions are welcome. To contribute:

1. Fork the repository
2. Create a feature branch
3. Commit your changes
4. Open a pull request with a clear description

---

## License

This project does not currently include an explicit license file. If you plan to distribute or publish it publicly, consider adding a license such as MIT.

---

## Project Status

RaceDay API is a backend service focused on race event management and participant registration. It is ready for local development, API testing, and extension for frontend integration or additional race-management features.

---

## Notes

This README reflects the current architecture and setup flow of the RaceDay API. If your database configuration or deployment environment differs, update the connection strings and environment variables accordingly.
