# Food Ordering API

RESTful backend for a food ordering platform, built with .NET 9 and Clean Architecture.

**Live Demo (Swagger):** https://foodorderingapi.runasp.net/swagger/index.html

---

## Overview

Food Ordering API is the backend of a full-stack food ordering platform. It exposes a versioned REST API with secure authentication, cart management, caching, and bilingual (English / Arabic) responses. It is consumed by an Angular frontend.

You can test every endpoint directly in the live Swagger UI, no installation needed.

## Features

- Authentication with ASP.NET Core Identity and JWT access tokens
- Refresh tokens stored in HttpOnly cookies (never exposed to JavaScript)
- Role-based authorization
- CQRS with MediatR
- Request validation with FluentValidation
- Distributed caching with Redis
- English and Arabic responses through language middleware
- Global exception handling and request logging middleware
- Swagger / OpenAPI documentation with API versioning
- Automatic admin account seeding

## Architecture

Clean Architecture with strict layer separation. Dependencies point inward only.

| Project | Responsibility |
|---|---|
| `FoodOrdering.Domain` | Entities and core business rules. No dependencies on other projects. |
| `FoodOrdering.Application` | Use cases (MediatR commands and queries), validators, AutoMapper profiles. |
| `FoodOrdering.Infrastructure` | EF Core, Identity, JWT, Redis, data access. |
| `FoodOrdering.API` | Controllers, middleware, Swagger, CORS, dependency injection. |

Request pipeline:

```
Exception handling -> Request logging -> Language -> HTTPS -> CORS -> Authentication -> Authorization -> Controllers
```

## Tech Stack

- .NET 9, ASP.NET Core Web API
- Entity Framework Core 9 with SQL Server
- ASP.NET Core Identity and JWT Bearer
- MediatR, FluentValidation, AutoMapper
- Redis (StackExchange.Redis)
- Swagger with API versioning

## Run Locally

Requirements: .NET 9 SDK, SQL Server (or LocalDB), Redis.

```bash
git clone https://github.com/<your-username>/<your-repo>.git
cd <your-repo>/FoodOrdering.API
```

Set your configuration with user secrets. Do not store real credentials in `appsettings.json`.

```bash
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=(localdb)\\mssqllocaldb;Database=FoodOrderingDB;Trusted_Connection=True;"
dotnet user-secrets set "ConnectionStrings:Redis" "localhost:6379"
dotnet user-secrets set "Jwt:Key" "<long-random-secret>"
dotnet user-secrets set "AdminAccount:Email" "<admin-email>"
dotnet user-secrets set "AdminAccount:Password" "<admin-password>"
```

Apply migrations and run:

```bash
dotnet ef database update --project ../FoodOrdering.Infrastructure --startup-project .
dotnet run
```

Open `https://localhost:<port>/swagger`.

## Test the Live API

1. Open https://foodorderingapi.runasp.net/swagger/index.html
2. Log in or register a user.
3. Copy the access token, click **Authorize**, and enter `Bearer <token>`.
4. Call any protected endpoint.

## Project Structure

```
FoodOrderingSolution/
  FoodOrdering.API/
  FoodOrdering.Application/
  FoodOrdering.Domain/
  FoodOrdering.Infrastructure/
  FoodOrderingSolution.sln
```

## Author

**Mohannad Emad** - Senior Software Developer (Angular and .NET Core)

- LinkedIn: https://www.linkedin.com/in/mohannad-emad-eldin-ramadan-499141263/
- Email: mohannad.emad535@gmail.com
- Phone: 01144244060
