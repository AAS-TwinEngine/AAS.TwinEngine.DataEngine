---
name: architecture
description: 'Ensure .NET/C# code meets best practices for the solution/project.'
---

## Architecture (Onion / Clean Architecture)
- Maintain strict layer separation:
  - **DomainModel**: pure domain types only (no ASP.NET Core, no database/ADO.NET, no config/options, no logging).
  - **ApplicationLogic**: use cases, domain/application services, interfaces/ports; depends only on DomainModel.
  - **Infrastructure**: database, file system, external services; implements ApplicationLogic interfaces.
  - **Api**: HTTP/controllers/serialization; delegates to ApplicationLogic.
- Dependency direction: **Api** → **ApplicationLogic** → **DomainModel**; **Infrastructure** → **ApplicationLogic**.
- Never leak infrastructure/ASP.NET types into DomainModel/ApplicationLogic (e.g., `HttpContext`, `ControllerBase`, `DbConnection`, provider-specific types).
- Prefer defining ports (interfaces) in **ApplicationLogic** and implementing them in **Infrastructure**.

## API & DTOs
- Keep controllers thin: validation + request shaping + call handler/service + return mapped DTO.
- Do not return DomainModel types from controllers; always return DTOs under `Api/**/Responses`.
- Favor explicit request/response models over `JsonObject` when feasible; if dynamic JSON is required, isolate it to the API layer.
- Validation:
  - Validate route/query/body inputs consistently.
  - Keep validation rules close to request DTOs (or dedicated validators) and avoid duplication.

## Error Handling
- Use centralized exception handling via `ErrorController` (avoid scattered controller-level try/catch).
- Return stable JSON error contracts with correct HTTP status codes.
- Map **base exception types** in `ErrorController` (for example `NotFoundException`, `BadRequestException`, `ServiceUnavailableException`) rather than listing every custom exception.
- Ensure custom exceptions inherit from the correct base type and define safe, reusable default messages (`public const string DefaultMessage`).
- Keep exception messages human-readable, consistent in tone, and safe for external consumers (no stack traces, internal endpoint details, or sensitive data).
- Keep exception organization consistent:
  - `ApplicationLogic/Exceptions/Base`
  - `ApplicationLogic/Exceptions/Application`
  - `ApplicationLogic/Exceptions/Infrastructure`
- At boundaries, translate Infrastructure exceptions into Application exceptions before they reach the API. In some cases, the Infrastructure layer may directly throw application-level exceptions — especially if it has enough domain context to do so.
- Keep API mapping focused on base exception types; custom exceptions should inherit from the correct base.
- Define safe reusable messages in custom exceptions (prefer `public const string DefaultMessage`).
- Translate infrastructure exceptions at boundaries before returning to API consumers.
- Do not leak raw/internal exception details to clients; return stable error contracts and log safely.

## Clean Code & SOLID
- Ensure SOLID principles compliance
- Avoid code duplication through base classes and utilities
- Prefer small, cohesive classes and methods.
- Watch for SRP violations (especially “handler/service” classes growing too large): extract focused collaborators.
- Prefer descriptive names; avoid abbreviations unless well-known.
- Avoid duplicate logic; introduce shared helpers only if reuse is clear.
- Follow existing patterns in this repo:
  - `Api/**/Handler/*Handler.cs` orchestrates and delegates.
  - `ApplicationLogic/Services/**` contains business/use-case logic.
  - `Infrastructure/**` contains DB execution and provider implementations.

## Configuration & Settings

- Use strongly-typed configuration classes with data annotations
- Implement validation attributes (Required, NotEmptyOrWhitespace)
- Use IConfiguration binding for settings
- Support appsettings.json configuration files

### Unit Test Method Naming
Use one consistent pattern (choose the closest match to surrounding tests):
- Preferred: `{MethodUnderTest}_When{Condition}_{ReturnsOrThrows}{Expected}`
  - Example: `ExecuteQueryAsync_WhenNoRows_ThrowsResourceNotFoundException`
- Also acceptable (existing pattern): `{MethodUnderTest}_Should{Expectation}_When{Condition}`
  - Example: `DecodeBase64_ShouldThrow_OnNullOrWhitespace`
- For async tests, keep the `Async` suffix in the method under test portion of the name. 

