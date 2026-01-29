# PostgreSQL Linux Migration Design

## Overview

Migrate QBECommerce from SQL Server on Windows to PostgreSQL on Linux, with the application running in Docker and Nginx Proxy Manager handling reverse proxy and SSL.

## Architecture

```
┌──────────────────────────────────────────────────────────────┐
│                      Linux Server                            │
│                                                              │
│  ┌───────────────────────────────────────┐    ┌───────────┐ │
│  │            Docker                      │    │ PostgreSQL│ │
│  │  ┌───────────────┐   ┌─────────────┐  │    │  (native) │ │
│  │  │ Nginx Proxy   │──▶│   App       │  │───▶│   :5432   │ │
│  │  │ Manager       │   │   :8080     │  │    └───────────┘ │
│  │  │ (custom SSL)  │   └─────────────┘  │                  │
│  │  └───────────────┘                    │                  │
│  └───────────────────────────────────────┘                  │
└──────────────────────────────────────────────────────────────┘
```

**Components:**
- **PostgreSQL** - Runs natively on Linux host (port 5432)
- **App container** - .NET 9 Blazor app (port 8080)
- **Nginx Proxy Manager** - Reverse proxy with custom SSL certificate (ports 80, 443, 81 for admin)

**Development environment:**
- PostgreSQL runs in Docker on Windows
- App runs with `dotnet run` or optionally in Docker

## Code Changes

### 1. NuGet Package Swap

**Remove from QBExternalWebLibrary.csproj:**
```xml
<PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="9.0.7" />
```

**Add:**
```xml
<PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="9.0.4" />
```

### 2. DbContext Configuration (Program.cs)

**Change:**
```csharp
options.UseSqlServer(connectionString)
```

**To:**
```csharp
options.UseNpgsql(connectionString)
```

### 3. Connection Strings

**appsettings.Development.json:**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=QBCommerceDB;Username=postgres;Password=devpassword"
  }
}
```

**appsettings.Production.json:**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=host.docker.internal;Database=QBCommerceDB;Username=qbcommerce;Password=SECURE_PASSWORD_HERE"
  }
}
```

### 4. Migrations

Delete existing migrations folder and create fresh initial migration:

```bash
rm -rf QBExternalWebLibrary/QBExternalWebLibrary/Migrations/
dotnet ef migrations add InitialCreate --project QBExternalWebLibrary/QBExternalWebLibrary
```

### 5. Dockerfiles

**API Dockerfile** (`Dockerfile.api` in solution root):

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["ShopQualityboltWeb/ShopQualityboltWeb/ShopQualityboltWeb.csproj", "ShopQualityboltWeb/ShopQualityboltWeb/"]
COPY ["QBExternalWebLibrary/QBExternalWebLibrary/QBExternalWebLibrary.csproj", "QBExternalWebLibrary/QBExternalWebLibrary/"]
RUN dotnet restore "ShopQualityboltWeb/ShopQualityboltWeb/ShopQualityboltWeb.csproj"
COPY . .
WORKDIR "/src/ShopQualityboltWeb/ShopQualityboltWeb"
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ShopQualityboltWeb.dll"]
```

**Blazor Frontend Dockerfile** (`Dockerfile.blazor` in solution root):

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["ShopQualityboltWebBlazor/ShopQualityboltWebBlazor.csproj", "ShopQualityboltWebBlazor/"]
COPY ["QBExternalWebLibrary/QBExternalWebLibrary/QBExternalWebLibrary.csproj", "QBExternalWebLibrary/QBExternalWebLibrary/"]
RUN dotnet restore "ShopQualityboltWebBlazor/ShopQualityboltWebBlazor.csproj"
COPY . .
WORKDIR "/src/ShopQualityboltWebBlazor"
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ShopQualityboltWebBlazor.dll"]
```

### 6. docker-compose.yml (Development)

Full stack development with PostgreSQL, API, and Blazor frontend:

```yaml
# Run with: docker-compose up -d
# Access: Blazor UI at http://localhost:5000, API at http://localhost:5278

services:
  db:
    image: postgres:16
    container_name: qbcommerce-postgres-dev
    environment:
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: devpassword
      POSTGRES_DB: QBCommerceDB
    ports:
      - "5432:5432"
    volumes:
      - pgdata:/var/lib/postgresql/data
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U postgres"]
      interval: 10s
      timeout: 5s
      retries: 5
    networks:
      - qbcommerce-dev

  api:
    build:
      context: .
      dockerfile: Dockerfile.api
    container_name: qbcommerce-api-dev
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ASPNETCORE_URLS=http://+:8080
      - ConnectionStrings__DefaultConnectionString=Host=db;Database=QBCommerceDB;Username=postgres;Password=devpassword
    ports:
      - "5278:8080"
    depends_on:
      db:
        condition: service_healthy
    networks:
      - qbcommerce-dev

  blazor:
    build:
      context: .
      dockerfile: Dockerfile.blazor
    container_name: qbcommerce-blazor-dev
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ASPNETCORE_URLS=http://+:8080
      - ApiSettings__BaseAddress=http://api:8080
    ports:
      - "5000:8080"
    depends_on:
      - api
    networks:
      - qbcommerce-dev

volumes:
  pgdata:

networks:
  qbcommerce-dev:
    driver: bridge
```

### 7. docker-compose.prod.yml (Production)

PostgreSQL runs natively on the host:

```yaml
# Deploy with: docker-compose -f docker-compose.prod.yml up -d

services:
  api:
    build:
      context: .
      dockerfile: Dockerfile.api
    container_name: qbcommerce-api
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ASPNETCORE_URLS=http://+:8080
      # Connection string can also be set via environment variable:
      # - ConnectionStrings__DefaultConnectionString=Host=host.docker.internal;Database=QBCommerceDB;Username=qbcommerce;Password=YOUR_SECURE_PASSWORD
    extra_hosts:
      - "host.docker.internal:host-gateway"
    restart: unless-stopped
    networks:
      - qbcommerce-network

  blazor:
    build:
      context: .
      dockerfile: Dockerfile.blazor
    container_name: qbcommerce-blazor
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ASPNETCORE_URLS=http://+:8080
      - ApiSettings__BaseAddress=http://api:8080
    depends_on:
      - api
    restart: unless-stopped
    networks:
      - qbcommerce-network

  nginx-proxy-manager:
    image: jc21/nginx-proxy-manager:latest
    container_name: nginx-proxy-manager
    ports:
      - "80:80"
      - "443:443"
      - "81:81"   # Admin UI
    volumes:
      - ./npm-data:/data
      - ./npm-letsencrypt:/etc/letsencrypt
    restart: unless-stopped
    networks:
      - qbcommerce-network

networks:
  qbcommerce-network:
    driver: bridge
```

## Production Server Setup

### Install PostgreSQL

```bash
# Ubuntu/Debian
sudo apt update
sudo apt install postgresql postgresql-contrib
sudo systemctl start postgresql
sudo systemctl enable postgresql
```

### Create Database and User

```bash
sudo -u postgres psql

CREATE USER qbcommerce WITH PASSWORD 'yoursecurepassword';
CREATE DATABASE QBCommerceDB OWNER qbcommerce;
GRANT ALL PRIVILEGES ON DATABASE QBCommerceDB TO qbcommerce;
\q
```

### Configure PostgreSQL for Docker Access

Edit `/etc/postgresql/16/main/pg_hba.conf`:
```
host    QBCommerceDB    qbcommerce    172.17.0.0/16    scram-sha-256
```

Restart PostgreSQL:
```bash
sudo systemctl restart postgresql
```

### Nginx Proxy Manager Setup

1. Access admin UI at `http://your-server-ip:81`
2. Default login: `admin@example.com` / `changeme`
3. Change default password
4. Add Proxy Host for the **Blazor frontend** (main site):
   - Domain: `yourdomain.com`
   - Forward hostname: `blazor`
   - Forward port: `8080`
   - Enable Websockets (required for Blazor Server)
   - SSL tab: Upload custom certificate (.crt and .key files)
5. Add Proxy Host for the **API** (if needed externally):
   - Domain: `api.yourdomain.com` or `yourdomain.com/api`
   - Forward hostname: `api`
   - Forward port: `8080`
   - SSL tab: Use same certificate

## What Stays the Same

- All models/entities
- All services and repositories
- All Blazor pages and components
- ASP.NET Core Identity (works with PostgreSQL)
- Error logging infrastructure

## Implementation Progress

### COMPLETED (2026-01-28)

- [x] Swap NuGet packages (SqlServer → Npgsql) in both .csproj files
- [x] Update Program.cs to use `UseNpgsql()`
- [x] Update connection strings in appsettings.Development.json and appsettings.Production.json
- [x] Delete old SQL Server migrations
- [x] Create fresh PostgreSQL migration (`InitialCreate`)
- [x] Create Dockerfile.api (API backend)
- [x] Create Dockerfile.blazor (Blazor frontend)
- [x] Create docker-compose.yml (full stack dev: PostgreSQL + API + Blazor)
- [x] Create docker-compose.prod.yml (production: API + Blazor + Nginx Proxy Manager)
- [x] Create .dockerignore
- [x] Fix unrelated build issues (unused Azure/Identity imports)
- [x] Build succeeds with PostgreSQL provider

### REMAINING STEPS

1. **Install Docker Desktop on Windows dev machine** ✓ DONE
   - WSL 2 enabled, Docker Desktop running

2. **Build and start full dev stack** ✓ DONE
   - All containers running (PostgreSQL, API, Blazor)
   - Admin user seeded

3. **Install Docker on Linux production server** ← YOU ARE HERE
   ```bash
   cd C:\Projects\QBECommerce_git\QBECommerce
   docker-compose up -d --build
   ```
   Access:
   - Blazor UI: http://localhost:5000
   - API/Swagger: http://localhost:5278/swagger

3. **Install Docker on Linux production server**
   ```bash
   sudo apt update
   sudo apt install docker.io docker-compose-plugin
   sudo systemctl start docker
   sudo systemctl enable docker
   sudo usermod -aG docker $USER
   ```

4. **Install PostgreSQL on Linux production server**
   ```bash
   sudo apt install postgresql postgresql-contrib
   sudo systemctl start postgresql
   sudo systemctl enable postgresql
   ```

5. **Create production database and user**
   ```bash
   sudo -u postgres psql
   CREATE USER qbcommerce WITH PASSWORD 'your-secure-password';
   CREATE DATABASE QBCommerceDB OWNER qbcommerce;
   GRANT ALL PRIVILEGES ON DATABASE QBCommerceDB TO qbcommerce;
   \q
   ```

6. **Configure PostgreSQL for Docker access**
   Edit `/etc/postgresql/16/main/pg_hba.conf`:
   ```
   host    QBCommerceDB    qbcommerce    172.17.0.0/16    scram-sha-256
   ```
   Then: `sudo systemctl restart postgresql`

7. **Deploy to production**
   - Copy docker-compose.prod.yml to server
   - Update password in compose file or use environment variable
   - Run `docker-compose -f docker-compose.prod.yml up -d`

8. **Configure Nginx Proxy Manager**
   - Access http://your-server-ip:81
   - Upload custom SSL certificate
   - Create proxy host for `blazor:8080` (main site, enable websockets)
   - Optionally create proxy host for `api:8080` (if API needs external access)

### Files Changed

| File | Status |
|------|--------|
| `QBExternalWebLibrary/QBExternalWebLibrary.csproj` | Modified |
| `ShopQualityboltWeb/ShopQualityboltWeb.csproj` | Modified |
| `ShopQualityboltWeb/ShopQualityboltWeb/Program.cs` | Modified |
| `ShopQualityboltWeb/ShopQualityboltWeb/appsettings.Development.json` | Modified |
| `ShopQualityboltWeb/ShopQualityboltWeb/appsettings.Production.json` | Modified |
| `QBExternalWebLibrary/Migrations/` | Recreated for PostgreSQL |
| `Dockerfile.api` | Created (API backend) |
| `Dockerfile.blazor` | Created (Blazor frontend) |
| `docker-compose.yml` | Created (full dev stack) |
| `docker-compose.prod.yml` | Created (production) |
| `.dockerignore` | Created |
| `Controllers/Api/AccountsController.cs` | Fixed unused import |
| `Controllers/Api/PunchOutSessionsController.cs` | Fixed unused import |
| `Services/Http/IdentityApiService.cs` | Fixed unused import |
| `Services/LocalStorageService.cs` | Fixed unused import |
