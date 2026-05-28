## [Unreleased]
### style
- Landing: inclusive language — "mamá o papá" instead of gendered "padre"
### feat
- Footer: show short Git hash from RAILWAY_GIT_COMMIT_SHA env var

## main — feature/landing-base

### Índice de sesiones
| Sesión | Descripción | Estado |
|--------|-------------|--------|
| S1 | Proyecto base Razor Pages + landing | ✅ completo |
| S2 | Railway config — Procfile + railway.json | ✅ completo |
| S3 | .gitignore para .NET + VS | ✅ completo |
| S4 | global.json para forzar .NET 8 en Railway | ✅ completo |
| S5 | Fix static files 404 | ✅ completo |
| S6 | Fix global.json rollForward para SDK local | ✅ completo |
| S7 | Fix wwwroot publish en .csproj | ✅ completo |
| S8 | Fix .gitignore — incluir wwwroot/css y wwwroot/js | ✅ completo |
| S9 | Fix Railway build desde subcarpeta | ✅ completo |
| S10 | Force redeploy Railway | ✅ completo |
| S11 | Fix ContentRoot y working directory para static files | ✅ completo |
| S12 | Deshabilitar HTTPS redirect en Production | ✅ completo |
| S13 | TokenEvent + esquema billing + migración | ✅ completo |
| S14 | Merge auth-db a develop y main | ✅ completo |
| S15 | Compatibilidad Railway SDK en AppDbContext | ✅ completo |
| S16 | Pin stable .NET 8 SDK para Railway | ✅ completo |
| S17 | global.json recreado en utf8NoBOM para nixpacks | ✅ completo |
| S18 | Dockerfile propio en lugar de nixpacks | ✅ completo |
| S19 | Procfile removido tras migración a Dockerfile | ✅ completo |
| S20 | railway.json delegado totalmente al Dockerfile | ✅ completo |
| S21 | Auth magic link con Resend | ✅ completo |
| S22 | Merge auth magic link a develop y main | ✅ completo |
| S23 | Fix login flow — forwarded headers, RESEND_FROM y redirect verify | ✅ completo |
| S24 | Merge landing-inclusive + version-footer y link a Login | ✅ completo |

### S1 — Proyecto base (aplicado)
- miTutoria.Web creado en .NET 8 Razor Pages
- Landing HTML convertida a Index.cshtml
- CSS y JS extraídos a wwwroot

### S2 — Railway config (aplicado)
- Procfile creado en raíz
- railway.json creado con build y start command para .NET 8

### S3 — .gitignore (aplicado)
- .gitignore creado con exclusiones estándar .NET
- .vs/, bin/, obj/, out/ excluidos

### S4 — global.json (aplicado)
- global.json creado en raíz
- Fuerza .NET 8 SDK en Railway (evita que tome .NET 6)

### S5 — Static files (aplicado)
- UseStaticFiles() confirmado en Program.cs
- wwwroot incluido explícitamente en publish

### S6 — global.json (aplicado)
- rollForward cambiado a latestMajor
- Permite cualquier SDK 8.x local y Railway toma el más reciente

### S7 — wwwroot publish (aplicado)
- .csproj actualizado para incluir wwwroot en publish
- Archivos css/site.css y js/site.js verificados

### S8 — .gitignore fix (aplicado)
- wwwroot/css y wwwroot/js ya no están excluidos
- wwwroot/lib sigue excluido (libman lo restaura)

### S9 — Railway publish (aplicado)
- railway.json actualizado para publicar desde miTutoria.Web
- nixpacks.toml creado con dotnet publish del proyecto web

### S11 — ContentRoot fix (aplicado)
- UseContentRoot(AppContext.BaseDirectory) en Program.cs
- startCommand cambiado a cd out && dotnet

### S13 — TokenEvent billing (aplicado)
- Entidad `TokenEvent` creada en `miTutoria.Web/Data/Entities/Billing/TokenEvent.cs`
- `AppDbContext` recreado y actualizado con `DbSet<TokenEvent>` y tabla `billing.token_events`
- Capa EF base recreada en `miTutoria.Web/Data/` para alinear el source con el snapshot existente
- Migración `AddTokenEvents` generada y aplicada con `dotnet ef database update`

### S14 — Merge auth-db (aplicado)
- `feature/auth-db` mergeada a `develop` con merge commit no-ff
- `develop` promovida a `main` con merge commit no-ff
- `CONTEXT.md` y `CHANGES.md` actualizados al estado post-merge
- Commit final de documentación creado en `main` para dejar trazado el estado post-merge

### S15 — AppDbContext compatibility fix (aplicado)
- Primary constructor removido de `miTutoria.Web/Data/AppDbContext.cs`
- Constructor tradicional `AppDbContext(DbContextOptions<AppDbContext> options)` agregado para compatibilidad con Railway SDK preview

### S16 — Stable SDK pin para Railway (aplicado)
- `nixpacks.toml` actualizado con `NIXPACKS_DOTNET_VERSION = "8.0"`
- `global.json` fijado en `8.0.0` con `rollForward: latestFeature`
- Se evita que Railway use SDK preview incompatible con EF Core en runtime

### S17 — global.json utf8NoBOM (aplicado)
- `global.json` recreado desde terminal sin usar el editor
- Codificación corregida a UTF-8 sin BOM para compatibilidad con nixpacks

### S18 — Dockerfile Railway (aplicado)
- `Dockerfile` creado en raíz para build/runtime con imágenes oficiales .NET 8
- `railway.json` actualizado para quitar el builder `NIXPACKS`
- `nixpacks.toml` reemplazado por comentario apuntando al Dockerfile raíz

### S19 — Procfile removido (aplicado)
- `Procfile` eliminado de la raíz del repo
- Railway queda configurado para usar el `ENTRYPOINT` del `Dockerfile` sin conflicto

### S20 — railway.json simplificado (aplicado)
- `railway.json` quedó solo con `$schema` y `restartPolicyType`
- `buildCommand` y `startCommand` eliminados para dejar el control completo al `Dockerfile`
### S21 — Auth magic link (aplicado)
- Paquete `Resend` agregado a `miTutoria.Web`
- `Family` extendida con `Email`, `MagicToken` y `MagicTokenExpiry`
- Migración `AddMagicTokenToFamily` generada y aplicada
- `Program.cs` actualizado con `AppDbContext`, Resend y sesión vía `AddSession`
- Páginas `Login` y `Auth/Verify` creadas para envío y validación del magic link

### S22 — Merge auth magic link (aplicado)
- `feature/auth-magic-link` mergeada a `develop` con merge commit no-ff
- `develop` promovida a `main` con merge commit no-ff
- `CONTEXT.md` y `CHANGES.md` preparados para el estado post-merge auth

### S23 — Fix login flow (aplicado)
- `Program.cs` actualizado con `UseForwardedHeaders` antes del pipeline de routing para respetar `X-Forwarded-Proto` en Railway
- `Program.cs` ahora lee `RESEND_FROM` desde configuración con fallback a `noreply@mitutoria.app`
- `Pages/Login.cshtml.cs` usa `IConfiguration` para el remitente del mail en lugar de hardcodearlo
- `Pages/Auth/Verify.cshtml.cs` redirige a `Index` en lugar de `Dashboard`, que todavía no existe

### S24 — Landing merge + login link (aplicado)
- `feature/landing-inclusive` mergeada en `feature/fix-landing-login-link`
- `feature/version-footer` mergeada en `feature/fix-landing-login-link`
- `Pages/Index.cshtml` actualizado con link a `/Login`
- Documentación corregida para reflejar que la ruta real es `Pages/Login.cshtml` y no `Pages/Auth/Login.cshtml`
