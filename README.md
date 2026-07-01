# GHB DP2 Backend

A repository for GHB DP2 Backend.

## Project Structure

The solution is structured in a clean architecture pattern:

- **GHB.DP2.Application**: Application services and logic
- **GHB.DP2.Domain**: Core domain models and business logic
- **GHB.DP2.Infrastructure**: External concerns like database, file services, etc.

## Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Docker](https://www.docker.com/products/docker-desktop)
- [PostgreSQL](https://www.postgresql.org/download/) (or use the Docker compose file)

## Development Setup

### Database and Dependencies

Start required dependencies by running the following command in the `src` directory:

```bash
docker-compose -f docker-compose.development.yml -p ghb-dp2-api up -d
```

### Running the API

```bash
cd src/GHB.DP2.Api
dotnet run
```

The API will be available at:

- HTTP: <http://localhost:5233>

## Entity Framework Migrations

### Automatic Migrations

The application now handles database migrations automatically:

- **Development Mode (DEBUG)**: Migrations are available through the `/migrations` endpoint. You can access this endpoint to apply pending migrations.
- **Production Mode**: Migrations are applied automatically when the application starts using `ApplyMigrationsAsync<Dp2DbContext>()`.

### Manual Migration Commands

To create a new migration:

```bash
cd Backend/GHB.DP2.Infrastructure
dotnet ef migrations add <MigrationName> --context <MyDbContext> -- -c "Server=localhost;Database=GHB.DP2;User Id=postgres;Password=postgres;"
```

To remove the last migration:

```bash
cd Backend/GHB.DP2.Infrastructure

dotnet ef database update <PreviousMigrationName> --context <MyDbContext> -- -c "Server=localhost;Database=GHB.DP2;User Id=postgres;Password=postgres;"
dotnet ef migrations remove -o Migrations --context <MyDbContext> -- -c "Server=localhost;Database=GHB.DP2;User Id=postgres;Password=postgres;"
```

### Development Migration Endpoint

In development mode, you can manually trigger migrations by accessing:

**Linux/macOS:**

```bash
curl --location 'http://localhost:5233/migrations' \
--header 'Content-Type: application/x-www-form-urlencoded' \
--data-urlencode 'context=GHB.DP2.Infrastructure.Dp2DbContext, GHB.DP2.Infrastructure, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null'
```

**Windows (PowerShell):**

```powershell
curl --location 'http://localhost:5233/migrations' `
--header 'Content-Type: application/x-www-form-urlencoded' `
--data-urlencode 'context=GHB.DP2.Infrastructure.Dp2DbContext, GHB.DP2.Infrastructure, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null'
```

**Windows (CMD):**

```cmd
curl --location "http://localhost:5233/migrations" ^
--header "Content-Type: application/x-www-form-urlencoded" ^
--data-urlencode "context=GHB.DP2.Infrastructure.Dp2DbContext, GHB.DP2.Infrastructure, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null"
```

This endpoint is only available in DEBUG builds for security reasons.
