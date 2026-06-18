# DT.DAS V2 Architecture Notes

## Layer responsibilities

- `DT.DAS.Domain` contains core entities, enums, and abstractions. It must not depend on SQL Server, ASP.NET Core, Hangfire, FTP, or parsing libraries.
- `DT.DAS.Application` contains use cases and business orchestration. It is organized by business modules: `Acquisition`, `Configs`, `Tasks`, and `PostProcessing`.
- `DT.DAS.Infrastructure` contains technical implementations. It is organized by capability: `Persistence`, `FileAccess`, `Parsing`, and `Jobs`.
- `DT.DAS.WebApi` is the HTTP entrypoint. Controllers live under `Modules/{BusinessModule}` and keep routes stable for clients.

## How to extend

- Add a new business endpoint under `src/DT.DAS.WebApi/Modules/{ModuleName}`.
- Put request/response contracts beside the owning Application module, usually under `Application/{ModuleName}/Contracts`.
- Put use-case orchestration in `Application/{ModuleName}/Services` and expose only the needed interface in `Application/{ModuleName}`.
- Put Dapper repositories and table scripts under `Infrastructure/Persistence`.
- Put new file protocols under `Infrastructure/FileAccess/Providers` by implementing `IFileProvider`.
- Put new parsers under `Infrastructure/Parsing/Parsers` by implementing `IDataParser` and registering them in `DataParserFactory`.

## Startup and Swagger

Run the API with:

```powershell
dotnet run --project src/DT.DAS.WebApi/DT.DAS.WebApi.csproj
```

Development Swagger UI is available at `/swagger`. XML comments are enabled in `DT.DAS.WebApi.csproj`; keep controller and contract comments current so Swagger remains useful.

## Database scripts

SQL scripts live under `src/DT.DAS.Infrastructure/Persistence/Scripts/{ModuleName}`. Use numbered, idempotent scripts such as `001_create_acquisition_task_logs.sql` and guard table creation with `OBJECT_ID` checks.
