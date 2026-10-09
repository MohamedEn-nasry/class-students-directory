# Class Students Directory — Backend (ASP.NET Core 8 Web API)

Public directory. No login, no admin, no roles.
Anyone can list approved profiles; anyone can submit their own GitHub/LinkedIn links.

## Layout

```text
ClassDirectory.sln
src/ClassDirectory.Api/
  Program.cs                 # wiring: EF+Npgsql pooler, CORS, rate limit, Serilog, health, Swagger
  Controllers/ProfilesController.cs
  Data/AppDbContext.cs       # database-first mapping of existing public.profiles
  Data/Entities/Profile.cs
  DTOs/                      # CreateProfileRequest, ProfileResponse, PagedResult<T>
  Services/
    ProfileUrls.cs           # normalize + regex (mirrors DB CHECKs)
    ProfileValidator.cs      # name/track/link rules
    ProfileService.cs        # search (AsNoTracking) + create
    IpHashService.cs         # salted SHA-256 IP hash, never stores raw IP
    TurnstileService.cs      # optional Cloudflare Turnstile (behind flag)
  appsettings.json / appsettings.example.json
tests/ClassDirectory.Tests/  # xUnit: URL normalization + validation
supabase/profiles.sql        # reference DDL + CHECK constraints (table already exists)
Dockerfile
```

## API

### `GET /api/profiles?search=&track=&page=1&pageSize=24`

Returns approved profiles, newest first, paged with total count.

```json
{
  "items": [
    {
      "id": "uuid",
      "fullName": "Ada Lovelace",
      "githubUrl": "https://github.com/ada",
      "linkedinUrl": null,
      "track": "Backend",
      "createdAt": "2026-01-01T00:00:00Z"
    }
  ],
  "page": 1,
  "pageSize": 24,
  "total": 137,
  "totalPages": 6
}
```

- `search`: substring over name + URLs (case-insensitive, `ILIKE`).
- `track`: exact match (case-insensitive).
- `page`: 1-based, default 1. `pageSize`: default 24, max 100.

### `POST /api/profiles` → `201` / `400` / `409` / `429`

Request:

```json
{
  "fullName": "Ada Lovelace",
  "githubUrl": "https://github.com/ada",
  "linkedinUrl": "https://www.linkedin.com/in/ada",
  "track": "Backend",
  "website": "",
  "turnstileToken": "optional-if-enabled"
}
```

- `fullName` required, trimmed, max 60.
- `track` optional, trimmed, max 50, empty → `null`.
- At least one of `githubUrl` / `linkedinUrl` required.
- URLs are **normalized** (trim, require `https`, lowercase host, drop
  query/fragment, trim trailing slash, `www.github.com` → `github.com`)
  then matched against the same regex as the DB CHECKs:
  - GitHub: `^https://github\.com/[A-Za-z0-9](?:[A-Za-z0-9-]{0,37}[A-Za-z0-9])?(?:/[A-Za-z0-9_.\-]+)*/?$`
  - LinkedIn: `^https://([a-z0-9-]+\.)?linkedin\.com/(in|company)/[A-Za-z0-9_.\-%]+(?:/[A-Za-z0-9_.\-%]*)*/?$`
- `website` is a **honeypot** — frontend never renders it. Non-empty → fake
  `201` without persisting (bots can't tell).
- `turnstileToken` verified against Cloudflare only when `Turnstile:Enabled=true`.
- Errors: `400` returns `HttpValidationProblemDetails` with per-field
  `errors`; `409` returns `ProblemDetails` with `field: githubUrl|linkedinUrl`;
  `429` after **5 POSTs / IP / hour** (fixed window, `Retry-After` header).
- Only a salted SHA-256 hex of the IP is stored in `ip_hash`.

### `GET /health`

Liveness + `db` check (`SELECT 1` via `AddDbContextCheck`). Returns `200 Healthy`
or `503 Unhealthy`.

Swagger is served in Development only (`/swagger`).

## Configuration (env vars)

| Env var | Purpose |
|---|---|
| `ConnectionStrings__Supabase` | **Required.** Supabase pooler string (port `6543`). |
| `Cors__AllowedOrigins__0` … | **Required.** Exact frontend origin(s), e.g. `https://app.example.com`. |
| `IpHashing__Salt` | **Required.** Long random string for IP hashing. |
| `RateLimit__PostsPerHour` | Optional, default `5`. |
| `Turnstile__Enabled` | Optional, default `false`. |
| `Turnstile__SecretKey` | Required when Turnstile enabled. |
| `Database__DisablePreparedStatements` | Default `true` (needed for Supavisor transaction pooler). Set `false` only for direct connections (5432). |

See `src/ClassDirectory.Api/appsettings.example.json` for a full example.

Local dev (PowerShell):

```powershell
$env:ConnectionStrings__Supabase="Host=aws-0-xxx.pooler.supabase.com;Port=6543;Database=postgres;Username=postgres.xxxxx;Password=***;Pooling=true;SSL Mode=Require;Trust Server Certificate=true"
$env:Cors__AllowedOrigins__0="http://localhost:3000"
$env:IpHashing__Salt="dev-only-long-random-string"
dotnet run --project src/ClassDirectory.Api
```

## Deploy

### Option A — Azure App Service (Linux)

1. Create App Service (`.NET 8 LTS` runtime, Linux).
2. Set **App Settings** (env vars): `ConnectionStrings__Supabase`,
   `Cors__AllowedOrigins__0=https://your-frontend`, `IpHashing__Salt=<random>`.
   Optionally `Turnstile__Enabled=true` + `Turnstile__SecretKey`.
3. Deploy via ZIP / GitHub Actions:
   ```powershell
   dotnet publish src/ClassDirectory.Api -c Release -o publish
   Compress-Archive publish/* app.zip -Force
   az webapp deploy --resource-group <rg> --name <app> --src-path app.zip --type zip
   ```
4. Verify: `https://<app>.azurewebsites.net/health` → Healthy.
   No migrations run — the `public.profiles` table already exists.

### Option B — Docker (any host)

```powershell
docker build -t class-directory-api .
docker run -p 8080:8080 `
  -e ConnectionStrings__Supabase="Host=...pooler.supabase.com;Port=6543;..." `
  -e Cors__AllowedOrigins__0="https://your-frontend" `
  -e IpHashing__Salt="<random>" `
  class-directory-api
```

Health: `GET http://localhost:8080/health`.

### Supabase notes

- Table `public.profiles` already exists — EF never creates it
  (database-first `ToTable` mapping, no `Migrate()` call).
- If the CHECK constraints are missing, apply `supabase/profiles.sql`.
- Always use the **pooler** string (6543) in the app; keep
  `Database:DisablePreparedStatements=true` for transaction mode.

## Tests

```powershell
dotnet test
```

Covers URL normalization (https-only, host lowercasing, query/fragment
stripping, trailing-slash trim, www canonicalization, regex accept/reject)
and validator rules (name/track lengths, at-least-one-link, per-field errors).
