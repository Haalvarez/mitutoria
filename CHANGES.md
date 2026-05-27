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
