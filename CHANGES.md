## [Unreleased]
### feat
- Profile: nueva página `/Profile` para editar nombre, apodo y rol (Padre/Madre)
- Family: extendida con `Nickname` y `ParentRole` enum
- Dashboard: agregar dashboard padre protegido por sesión en `/Dashboard`
- Login: mostrar mensaje de confirmación y ocultar el formulario después de enviar el magic link
### fix
- Auth: redirect post-verify ahora apunta a `/Dashboard`
- Login: usar `APP_BASE_URL` para construir el magic link y corregir casing de `/Auth/Verify`
- Login: mostrar errores de `ModelState` arriba del formulario en `/Login`
### style
- Landing: inclusive language — "mamá o papá" instead of gendered "padre"
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
| S25 | Try/catch en OnPost del login para visibilidad de errores | ✅ completo |
| S26 | Mostrar errores de ModelState en la vista de Login | ✅ completo |
| S27 | APP_BASE_URL para magic link y ruta Verify con casing correcto | ✅ completo |
| S28 | Confirmación visual después de enviar el magic link | ✅ completo |
| S29 | Dashboard padre con protección de ruta por sesión | ✅ completo |
| S30 | Redirect post-verify al dashboard padre | ✅ completo |
| S31 | Perfil padre — Nickname + ParentRole + página /Profile | ✅ completo |

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

### S25 — Login OnPost try/catch (aplicado)
- `Pages/Login.cshtml.cs` envuelve desde `SaveChangesAsync` hasta `EmailSendAsync` en `try/catch`
- En caso de excepción, la página muestra `Error: {Tipo} — {Mensaje}` vía `ModelState`
- Hotfix aplicado directo en `main` para dar visibilidad inmediata al error en `/Login`

### S26 — Login muestra errores de ModelState (aplicado)
- `Pages/Login.cshtml` ahora renderiza los errores de `ViewData.ModelState` dentro de `.manifesto`, antes del `<form>`
- El usuario ve en pantalla los mensajes agregados por `Login.OnPostAsync` cuando falla el envío del magic link

### S27 — APP_BASE_URL + Verify route casing (aplicado)
- `Pages/Login.cshtml.cs` ahora prioriza `APP_BASE_URL` para construir el magic link del email
- La URL enviada apunta a `/Auth/Verify?token=...` con el casing real de la página Razor

### S28 — Confirmación post-envío en Login (aplicado)
- `Pages/Login.cshtml.cs` ahora lee `sent=true` desde `Request.Query` en `OnGet`
- `Pages/Login.cshtml` muestra un mensaje de confirmación y oculta el formulario cuando el magic link ya fue enviado

### S29 — Dashboard padre protegido por sesión (aplicado)
- `Pages/Dashboard/Index.cshtml.cs` valida `FamilyId` en sesión y redirige a `/Login` si no existe
- `Pages/Dashboard/Index.cshtml.cs` carga la `Family` actual y expone solo usuarios con rol `Student`
- `Pages/Dashboard/Index.cshtml` crea la ruta `/Dashboard` y muestra nombre de familia y lista de hijos

### S30 — Redirect post-verify al dashboard (aplicado)
- `Pages/Auth/Verify.cshtml.cs` ahora redirige a `/Dashboard/Index` después de guardar `FamilyId` en sesión
- El flujo de magic link entra directo al dashboard padre protegido

### S31 — Perfil padre (aplicado)
- `Family.cs` extendida con `Nickname` (nullable string) y `ParentRole` enum (`Padre` / `Madre`)
- `AppDbContext` registra conversión a string para `ParentRole`
- `Pages/Profile/Index.cshtml.cs` y `Index.cshtml` implementan `/Profile` con protección por sesión
- Formulario permite editar nombre completo, apodo y rol; valida nombre no vacío; try/catch con ModelState
- Dashboard padre incluye link "Editar perfil" → `/Profile`
- Migración `AddParentProfile` pendiente de ejecutar con `dotnet ef` en entorno con SDK 8
