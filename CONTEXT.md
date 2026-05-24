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
- **Sesión:** 3
- **Fase activa:** Fase 1 — MVP Familia
- **Branches pendientes de mergear a main:** `feature/landing-inclusive`, `feature/version-footer`, `feature/auth-db`
- **Último commit:** fix: WebApplicationOptions + EF design-time factory

## Funciona hoy
- ✅ mitutoria.app live en Railway
- ✅ Deploy automático: push → Railway en ~2 min
- ✅ Landing page publicada con lenguaje inclusivo (mamá o papá)
- ✅ Footer muestra git hash desde RAILWAY_GIT_COMMIT_SHA
- ✅ .gitignore, global.json, railway.json, nixpacks.toml configurados
- ✅ PostgreSQL en Railway con esquemas `auth` y `academic`
- ✅ EF Core + modelos: Family, User, Subject, Classroom, Message
- ✅ Migración inicial aplicada — tablas creadas en Railway
- ✅ TablePlus conectado a Railway DB (conexión pública)

## No funciona / pendiente
- ⬜ Mergear features pendientes a main
- ⬜ Auth / Login
- ⬜ Dashboard padre/madre
- ⬜ Aula estudiante
- ⬜ Integración Anthropic API

---

## Próximos 3 pasos (Fase 1)
1. Mergear `feature/landing-inclusive`, `feature/version-footer`, `feature/auth-db` → main
2. Auth / Login — registro y login de padre/madre + hijos
3. Dashboard padre/madre — crear/ver aulas

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
│   │   │   └── Academic/
│   │   │       ├── Subject.cs
│   │   │       ├── Classroom.cs
│   │   │       └── Message.cs
│   │   └── Migrations/
│   ├── Infrastructure/
│   │   └── VersionPageFilter.cs
│   ├── Pages/
│   │   ├── Index.cshtml
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
6. Ramas: feature/xxx → develop → main (nunca directo a main)
7. Al cerrar sesión → actualizar CONTEXT.md + CHANGES.md
```

---

## Ramas
| Rama | Propósito |
|---|---|
| `main` | Production → Railway auto-deploy |
| `develop` | Integración |
| `feature/landing-inclusive` | Pendiente de mergear — lenguaje inclusivo landing |
| `feature/version-footer` | Pendiente de mergear — git hash en footer |
| `feature/auth-db` | Pendiente de mergear — EF Core + DB |

---

## DB Railway
| Dato | Valor |
|---|---|
| Host interno | postgres.railway.internal:5432 |
| Host público (dev local) | zephyr.proxy.rlwy.net:21740 |
| Database | railway |
| Esquemas | auth, academic, public (__EFMigrations) |
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

---

*Actualizado al cierre de Sesión 2*
