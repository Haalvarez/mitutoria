# CONTEXT.md — miTutorIA
> Pegar este archivo al inicio de cada sesión con Claude.
> Actualizar al cierre de cada sesión.

---

## 🎯 Objetivo Último (OÚ)
Lanzar una plataforma educativa multi-tenant con IA controlada por padres,
monetizable, construida sobre ASP.NET Core 8 + Railway.
**Criterio de éxito de Fase 1:** Vika, Dasha y Egor pueden loguearse
y chatean en su aula desde mitutoria.app.

---

## Estado actual
- **Sesión:** 4
- **Fase activa:** Fase 1 — MVP Familia
- **Branch activo:** `feature/parent-profile`
- **Branches pendientes de mergear a main:** `feature/fix-login-flow`, `feature/fix-landing-login-link`, `feature/dashboard-parent`
- **Último commit:** chore: update CONTEXT.md end of session 4

## Funciona hoy
- ✅ mitutoria.app live en Railway
- ✅ Deploy automático: push → Railway en ~2 min
- ✅ Landing page publicada con lenguaje inclusivo (mamá o papá)
- ✅ Footer muestra git hash desde RAILWAY_GIT_COMMIT_SHA
- ✅ Landing tiene link directo a `/Login`
- ✅ La página real de login está en `Pages/Login.cshtml` (no en `Pages/Auth/Login.cshtml`)
- ✅ .gitignore, global.json, railway.json, nixpacks.toml configurados
- ✅ PostgreSQL en Railway con esquemas `auth`, `academic` y `billing`
- ✅ EF Core + modelos: Family, User, Subject, Classroom, Message
- ✅ Migración inicial aplicada — tablas creadas en Railway
- ✅ TokenEvent entity + migración aplicada
- ✅ `feature/auth-db` mergeada a `develop` y `main`
- ✅ Auth magic link — Login + Verify pages
- ✅ Resend integrado con mitutoria.app
- ✅ Forwarded headers habilitados para respetar `https` detrás del proxy de Railway
- ✅ Remitente de Resend configurable vía `RESEND_FROM`
- ✅ Hotfix directo en `main`: `Login.OnPostAsync` ahora muestra errores visibles en pantalla si falla save/send
- ✅ Hotfix directo en `main`: `Pages/Login.cshtml` renderiza errores de `ModelState` arriba del formulario
- ✅ Hotfix directo en `main`: el magic link ahora usa `APP_BASE_URL` y apunta a `/Auth/Verify` con casing correcto
- ✅ Login muestra confirmación visual y oculta el formulario después de enviar el magic link
- ✅ Login funciona de punta a punta — magic link enviado, token verificado, sesión creada, redirect a `/Dashboard`
- ✅ Dashboard padre disponible en `/Dashboard` con protección por sesión y lista de estudiantes de la familia
- ✅ Verify guarda `FamilyId` en sesión y redirige al dashboard padre
- ✅ Perfil padre en `/Profile` — editar nombre, apodo y rol (Padre/Madre), guarda y redirige
- ✅ `/Profile` con estilos `.form-field`, layout sin duplicados y labels visibles
- ✅ `/Dashboard` con estilos `.dashboard-header`, `.students-list` y botón “+ Agregar hijo” (apunta a `/Students/Add`)
- ✅ `FamilyName` muestra `Nickname ?? Name ?? Email` — nunca el email crudo
- ✅ CSS: bloque `.page-main`, `.form-field`, `.dashboard-header`, `.students-list` agregado a `site.css`
- ✅ `Family` extendida con `Nickname` y `ParentRole` enum
- ✅ Migración `AddParentProfile` aplicada vía `db.Database.Migrate()` en startup (auto-migrate)
- ✅ Sesión con cookie HttpOnly 7 días
- ✅ TablePlus conectado a Railway DB (conexión pública)

## No funciona / pendiente
- ⬜ Implementar página `/Students/Add` para agregar hijos desde el dashboard
- ⬜ Mergear features pendientes a main
- ⬜ Aula estudiante
- ⬜ Integración Anthropic API

---

## Próximos 3 pasos (Fase 1)
1. Implementar `/Students/Add` — formulario para agregar hijo con nombre y email
2. Listar hijos en dashboard con link a su aula
3. Crear aula estudiante (`/Classroom/{id}`)

---

## Decisiones técnicas tomadas
| Decisión | Motivo |
|---|---|
| WebApplicationOptions con ContentRootPath | WebHost.UseContentRoot causaba conflicto en design-time |
| Sin UseHttpsRedirection en Production | Railway maneja SSL en su proxy |
| Puerto 8080 en Railway Networking | App bindea a 8080 por defecto |
| global.json rollForward: latestMajor | SDK 8.0.0 exacto no disponible localmente |
| cd out && dotnet miTutoria.Web.dll | wwwroot debe estar en working directory |
| RAILWAY_GIT_COMMIT_SHA leído directo en Layout | Evita complejidad de filtros/ViewData |
| Esquemas PostgreSQL: auth + academic | Separación de responsabilidades sin múltiples DBs |
| Un solo schema público para EF Migrations | __EFMigrationsHistory en public por defecto |
| IDesignTimeDbContextFactory | EF design-time no puede resolver DI en migraciones |
| appsettings.Development.json en .gitignore | Connection string nunca va al repo |
| ConnectionString público Railway para dev local | DATABASE_URL interno no es accesible desde máquina local |
| Npgsql 8.0.11 + EF Design 8.0.27 | Versiones compatibles con .NET 8 |
| Esquema billing para token_events | Separación de responsabilidades financieras |
| Sin primary constructors en DbContext | Railway usa SDK preview 8.0.100-preview.5 que no los soporta |
| NIXPACKS_DOTNET_VERSION=8.0 + rollForward:latestFeature | Railway usaba SDK preview 8.0.100-preview.5 — incompatible con EF Core en runtime |
| Dockerfile propio en lugar de nixpacks | Preview SDK 8.0.100-preview.5 de nixpacks es incompatible con EF Core 8.x en runtime |
| Sin Procfile | Con Dockerfile propio Railway usa ENTRYPOINT — Procfile causa conflicto |
| railway.json sin buildCommand ni startCommand | Con Dockerfile Railway usa ENTRYPOINT directamente |
| Magic link con Resend | Sin contraseñas — token expira en 15 min |
| UseForwardedHeaders con X-Forwarded-For/X-Forwarded-Proto | Railway termina TLS en proxy y el magic link debe salir con `https` |
| `RESEND_FROM` configurable con fallback | Evita hardcodear el remitente y permite variar el sender por entorno |
| `APP_BASE_URL` configurable con fallback al request actual | Permite generar links correctos detrás de proxy/domino y evita depender de `Request.Host` |
| `sent=true` leído desde `Request.Query` en `OnGet` | Garantiza que la vista muestre el estado de confirmación después del redirect post-envío |
| Sesión via AddSession con cookie HttpOnly 7 días | Persistencia simple para FamilyId mientras llega auth completa |
| Dashboard padre protegido por `FamilyId` en sesión | La página `/Dashboard` requiere sesión válida y carga solo estudiantes de la familia actual |
| `User.Role` se maneja como `UserRole` enum | El filtro de estudiantes debe usar `UserRole.Student`, no el string `"student"` |
| `Family.ParentRole` se maneja como `ParentRole` enum con conversión a string | Permite almacenar `"Padre"` o `"Madre"` en la DB de forma legible |
| `db.Database.Migrate()` en startup | Railway no corre `dotnet ef` — las migraciones se aplican automáticamente al arrancar la app |
| Migraciones creadas manualmente | Incompatibilidad de SDK local (8.0.0 requerido, solo 9.x/10.x disponible) — los archivos `.cs` se escriben a mano siguiendo el patrón existente |

---

## Estructura del repo
```
mitutoria/
├── miTutoria.sln
├── miTutoria.Web/
│   ├── Data/
│   │   ├── AppDbContext.cs
│   │   ├── AppDbContextFactory.cs
│   │   ├── Entities/
│   │   │   ├── Auth/
│   │   │   │   ├── Family.cs
│   │   │   │   └── User.cs
│   │   │   ├── Academic/
│   │   │   │   ├── Subject.cs
│   │   │   │   ├── Classroom.cs
│   │   │   │   └── Message.cs
│   │   │   └── Billing/
│   │   │       └── TokenEvent.cs
│   │   └── Migrations/
│   ├── Infrastructure/
│   │   └── VersionPageFilter.cs
│   ├── Pages/
│   │   ├── Index.cshtml
│   │   ├── Login.cshtml
│   │   ├── Auth/Verify.cshtml
│   │   ├── Dashboard/Index.cshtml
│   │   ├── Profile/Index.cshtml
│   │   └── Shared/_Layout.cshtml
│   ├── wwwroot/
│   │   ├── css/site.css
│   │   └── js/site.js
│   ├── appsettings.json
│   ├── appsettings.Development.json  ← en .gitignore, nunca commitear
│   └── Program.cs
├── Procfile
├── railway.json
├── nixpacks.toml
├── global.json
├── .gitignore
├── CHANGES.md
├── CONTEXT.md        ← este archivo
└── PROJECT.md
```

---

## Workflow de sesión
```
1. Pegar CONTEXT.md al inicio → Claude lee estado real
2. Claude genera prompts → Copilot Agent ejecuta (modelo: GPT-5.4)
3. Prompt incluye siempre: crear rama al inicio + commit al final + actualizar CHANGES.md
4. Claude in Chrome → verificar comportamiento en vivo
5. Un prompt = un commit con prefijo feat/fix/chore/style/docs
6. Ramas: feature/xxx → develop → main (nunca directo a main). Copilot Agent SIEMPRE actualiza CONTEXT.md en el POST-CAMBIO de cada commit. No es opcional.
7. Al cerrar sesión → actualizar CONTEXT.md + CHANGES.md
```

---

## Ramas
| Rama | Propósito |
|---|---|
| `main` | Production → Railway auto-deploy |
| `develop` | Integración |
| `feature/dashboard-parent` | Dashboard padre con protección por sesión |
| `feature/parent-profile` | Perfil padre — Nickname, ParentRole, página /Profile |
| `feature/fix-login-flow` | Pendiente de mergear — fix magic link/login flow |
| `feature/fix-landing-login-link` | Integra landing inclusivo + footer versionado + link a Login |
| `feature/landing-inclusive` | Mergeada en `feature/fix-landing-login-link` |
| `feature/version-footer` | Mergeada en `feature/fix-landing-login-link` |

---

## DB Railway
| Dato | Valor |
|---|---|
| Host interno | postgres.railway.internal:5432 |
| Host público (dev local) | zephyr.proxy.rlwy.net:21740 |
| Database | railway |
| Esquemas | auth, academic, billing, public (__EFMigrations) |
| Cliente recomendado | TablePlus |

---

## Costos actuales
| Ítem | Costo |
|---|---|
| mitutoria.app (Porkbun) | $10.81/año |
| Railway Hobby | ~$5/mes |
| PostgreSQL Railway | incluido en Hobby |
| Anthropic API | $0 (fase 3) |
| **Total** | **~$6/mes** |

---

## Backlog — ideas anotadas (no urgentes)
> Estas ideas son válidas pero van después de Fase 1.
> No desviar la sesión hacia estas hasta tener el MVP funcionando.

- [ ] Ambiente staging (develop → staging.mitutoria.app)
- [ ] Error tracking en DB (Serilog + tabla Errors)
- [ ] Analytics de comportamiento (uso por aula, sesión, hijo)
- [ ] TO-DO system para priorizar features (GitHub Projects)
- [ ] BYOK (bring your own API key) para familias avanzadas
- [ ] Marketplace de aulas / plantillas por materia
- [ ] Idioma portugués para mercado Brasil
- [ ] Docker local para dev aislado (bloqueado — Docker Desktop no arranca)
- [ ] Auth estudiante via slug memorable: mitutoria.app/u/{apodo}
  → "Hola {nombre}, ingresá tu email" → magic link → sesión estudiante
  → Fase 2 — el padre solo comunica la URL, sin contraseñas ni emails recordados

---

## POST-CAMBIO
- Commit en `feature/parent-profile`: `feat: add parent profile page with nickname and role`
- `Family.cs` extendida con `Nickname` y `ParentRole` enum (`Padre` / `Madre`)
- `AppDbContext` registra conversión a string para `Family.ParentRole`
- `Pages/Profile/Index.cshtml.cs` y `Index.cshtml` implementan `/Profile` protegida por sesión
- `Pages/Dashboard/Index.cshtml` incluye link "Editar perfil" → `/Profile`
- Migración `AddParentProfile` **pendiente** — requiere SDK 8 disponible en la máquina local
- `CHANGES.md` actualizado con la sesión S31

---

*Actualizado al cierre de Sesión 8*
