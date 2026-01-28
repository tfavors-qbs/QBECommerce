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

### 5. Dockerfile

Create `ShopQualityboltWeb/ShopQualityboltWeb/Dockerfile`:

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["ShopQualityboltWeb/ShopQualityboltWeb/ShopQualityboltWeb.csproj", "ShopQualityboltWeb/"]
COPY ["QBExternalWebLibrary/QBExternalWebLibrary/QBExternalWebLibrary.csproj", "QBExternalWebLibrary/"]
RUN dotnet restore "ShopQualityboltWeb/ShopQualityboltWeb.csproj"
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

### 6. docker-compose.yml (Development)

Create in solution root:

```yaml
services:
  db:
    image: postgres:16
    environment:
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: devpassword
      POSTGRES_DB: QBCommerceDB
    ports:
      - "5432:5432"
    volumes:
      - pgdata:/var/lib/postgresql/data

volumes:
  pgdata:
```

### 7. docker-compose.yml (Production)

Create on Linux server:

```yaml
services:
  app:
    image: your-registry/qbcommerce:latest
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ConnectionStrings__DefaultConnection=Host=host.docker.internal;Database=QBCommerceDB;Username=qbcommerce;Password=SECURE_PASSWORD_HERE
    extra_hosts:
      - "host.docker.internal:host-gateway"
    restart: unless-stopped

  nginx-proxy-manager:
    image: jc21/nginx-proxy-manager:latest
    ports:
      - "80:80"
      - "443:443"
      - "81:81"
    volumes:
      - ./npm-data:/data
      - ./npm-letsencrypt:/etc/letsencrypt
    restart: unless-stopped
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
4. Add Proxy Host:
   - Domain: `yourdomain.com`
   - Forward hostname: `app`
   - Forward port: `8080`
   - SSL tab: Upload custom certificate (.crt and .key files)

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
- [x] Create Dockerfile (in solution root)
- [x] Create docker-compose.yml (for local dev)
- [x] Create docker-compose.prod.yml (for production)
- [x] Create .dockerignore
- [x] Fix unrelated build issues (unused Azure/Identity imports)
- [x] Build succeeds with PostgreSQL provider

### REMAINING STEPS

1. **Install Docker Desktop on Windows dev machine** ← YOU ARE HERE
   - Download complete, restart required
   - After restart, Docker Desktop will start automatically

2. **Start local PostgreSQL and test**
   ```bash
   cd C:\Projects\QBECommerce_git\QBECommerce
   docker-compose up -d db
   ```
   Then run the app with `dotnet run` to verify it connects and creates tables.

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
   - Create proxy host pointing to app:8080

### Files Changed

| File | Status |
|------|--------|
| `QBExternalWebLibrary/QBExternalWebLibrary.csproj` | Modified |
| `ShopQualityboltWeb/ShopQualityboltWeb.csproj` | Modified |
| `ShopQualityboltWeb/ShopQualityboltWeb/Program.cs` | Modified |
| `ShopQualityboltWeb/ShopQualityboltWeb/appsettings.Development.json` | Modified |
| `ShopQualityboltWeb/ShopQualityboltWeb/appsettings.Production.json` | Modified |
| `QBExternalWebLibrary/Migrations/` | Recreated for PostgreSQL |
| `Dockerfile` | Created |
| `docker-compose.yml` | Created |
| `docker-compose.prod.yml` | Created |
| `.dockerignore` | Created |
| `Controllers/Api/AccountsController.cs` | Fixed unused import |
| `Controllers/Api/PunchOutSessionsController.cs` | Fixed unused import |
| `Services/Http/IdentityApiService.cs` | Fixed unused import |
| `Services/LocalStorageService.cs` | Fixed unused import |
