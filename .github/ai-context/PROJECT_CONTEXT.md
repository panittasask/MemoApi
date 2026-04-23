# Project Context (Backend)

## Stack

- ASP.NET Core API
- Entity Framework Core with migrations
- Controllers in `Controllers/`
- DTOs in `DTOs/`
- Data context in `Data/ApplicationDbContext.cs`

## Conventions

- Keep controller endpoints focused and thin.
- Put data contracts in DTOs.
- Keep migration history consistent and non-destructive.

## Current Main Areas

- Auth and token flow
- Dashboard and workflow APIs
- History and settings APIs

## Notes

- Prefer minimal, focused changes.
- Avoid touching unrelated files.
