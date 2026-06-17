# Architecture

## Stack

- **Framework**: ASP.NET Core 8 MVC (Razor Views)
- **Database**: SQL Server via Entity Framework Core 8
- **Auth**: Cookie-based authentication + JWT bearer, BCrypt password hashing
- **Image processing**: SixLabors.ImageSharp

## Project Structure

```
Controllers/     # MVC controllers (Home, Blog, Account, Admin)
Models/          # EF Core entities
ViewModels/      # View-specific DTOs
Views/           # Razor views (.cshtml)
Services/        # Business logic (BlogService, UserService, FileAndAIService)
Data/            # ApplicationDbContext
Filters/         # Action filters (AuthFilters)
Helpers/         # Utility classes (AuthHelper)
Migrations/      # SQL scripts (SampleData.sql)
wwwroot/         # Static assets (css, js)
```

## Domain Models

- **User** — username, email, BCrypt hash, role (User / Admin)
- **Blog** — title, short/long description, destination, cover image, status (Draft / Published / Hidden), view count
- **Comment** — soft-delete via `IsDeleted`
- **BlogImage** — multiple images per blog with sort order
- **BlogTag** — tags stored per blog

## Key Services

| Service | Responsibility |
|---|---|
| `BlogService` | CRUD for blogs, comments, tags |
| `UserService` | Registration, login, profile |
| `FileUploadService` | Image upload/delete with extension + size validation |
| `AIService` (in FileAndAIService) | AI-assisted content (in progress) |

## Auth Flow

Roles: `User` and `Admin`. `AuthFilters` enforces access at the controller/action level. Cookie auth is used for MVC views; JWT bearer is available for API calls.
