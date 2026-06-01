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
- **Sesión:** 12
- **Fase activa:** Fase 1 — MVP Familia (lista para piloto)
- **Branch activo:** `main`
- **Último commit:** feat: secciones de material, guard de acceso y monitor de piloto

## Funciona hoy
- ✅ Login de alumno `/Entrar` — usuario + PIN configurado por el padre desde `/Students/Edit`
- ✅ Guard de acceso: `subscription_status` chequeado en Verify (padre) y Entrar (alumno)
  - `trial` / `active` → acceso normal
  - `trial_expired` / `suspended` / `cancelled` → página `/Blocked` con mensaje claro
  - Familias sin estado o en `waitlist` → bloqueadas automáticamente
- ✅ Aula acepta sesión de alumno o de padre
- ✅ Racha de días 🔥 visible en el aula y en el dashboard del padre
- ✅ Botonera del aula: Quiz · Tarjetas (modal flip) · Simulacro (modal a/b/c/d con puntaje)
- ✅ Prompt caching — ahorro ~90% en tokens del material por sesión activa
- ✅ Prompt anti-bucle: tutor avanza cuando el alumno solo confirma lo sabido
- ✅ PDF upload AJAX (sin reload) con OCR fallback vía Claude Vision para PDFs escaneados
- ✅ **Secciones de material:** PDF segmentado automáticamente en secciones temáticas por Haiku
  - Progreso persistente entre sesiones (`material_section_index` en DB)
  - Sidebar con barra ✅/▶/○ y botones Siguiente/Anterior
  - "Nueva sesión" preserva material y secciones — solo borra mensajes
  - Texto pegado = apunte complementario (no reemplaza secciones del PDF)
- ✅ Tipo de cambio MEP en tiempo real (dolarapi.com, cache 60 min)
- ✅ Dashboard padre rediseñado: intercambios hoy/semana/mes, gasto ARS, racha por hijo
- ✅ Admin `/admin?token=ADMIN_TOKEN` — familias, señales de riesgo, tokens por feature, waitlist
- ✅ **Monitor de piloto en admin:** KR1 (retención), KR2 (hábito/racha), KR3/KR4 inputs manuales, semáforo 🟢🟡🔴, señal de vencimientos ≤3 días
- ✅ Alertas Telegram al anotarse en waitlist (TELEGRAM_BOT_TOKEN + TELEGRAM_CHAT_ID)
- ✅ Landing: nav con 3 botones persistentes, hero en 2 columnas sin botones redundantes
- ✅ Waitlist guarda en DB — formulario funcional desde sesión 9

## Funciona desde antes
- ✅ mitutoria.app live en Railway, deploy automático push → Railway ~2 min
- ✅ Landing page con demo en vivo (5 mensajes sin login) + lista de espera funcional → DB
- ✅ Auth magic link — Login + Verify, Resend integrado, APP_BASE_URL, forwarded headers
- ✅ Sesión con cookie HttpOnly 7 días
- ✅ Dashboard padre — lista de hijos, botones "Editar" y "Abrir aula"
- ✅ Dashboard padre — gráfico de barras Chart.js de consumo por hijo por día del mes
- ✅ Dashboard padre — cards de tokens y costo USD del mes (DateTimeKind.Utc correcto)
- ✅ Perfil padre `/Profile` — nombre, apodo, rol (Padre/Madre)
- ✅ Agregar hijo `/Students/Add` — nombre, apodo, género, nivel, año, TDAH
- ✅ Editar hijo `/Students/Edit/{id}` — datos básicos + estilo de aprendizaje + config TDAH
  - Género (Femenino/Masculino/NoEspecificado) → pronombres correctos en el prompt
  - 5 checkboxes de preferencias de aprendizaje con descripción para padres
  - Sección TDAH: nivel de explicación (acordeAlAño/unPocoBasico/bastanteBasico) + 2 prefs extra
- ✅ Aula `/Classroom/{studentId}` — layout dos paneles (sidebar + chat full-height)
  - AJAX chat sin reload de página
  - Typing indicator (tres puntos animados)
  - Burbujas de chat con avatar circular, alineación asimétrica usuario/tutor
  - Input dinámico (crece con el texto), Enter envía, Shift+Enter nueva línea
  - Sidebar: material (PDF hasta 20MB con itext7 + texto pegado), secciones, resumen compacto
  - Footer oculto en el aula, layout 100dvh
- ✅ Integración Anthropic API — Haiku 4.5, prompt socrático v1 con género y preferencias inyectadas
- ✅ token_events registrado después de cada llamada — FamilyId, UserId, tokens in/out, CostUsd, Feature
- ✅ Límite mensual configurable `MONTHLY_TOKEN_LIMIT` (default 500k tokens)
- ✅ Migraciones todas aplicadas en Railway vía `db.Database.Migrate()` en startup
- ✅ TablePlus conectado a Railway DB (conexión pública)
- ✅ PostgreSQL en Railway: esquemas `auth`, `academic`, `billing`, `public` (__EFMigrations)

## No funciona / pendiente

### Piloto (pasos manuales antes de lanzar)
- ⬜ Correr SQL de migración `AddMaterialSections` en TablePlus (columnas `material_sections`, `material_section_index`, `material_ocr_source` en `academic.classrooms`)
- ⬜ Correr SQL de migración `AddFamilyBilling` en TablePlus (columnas `created_at`, `subscription_status`, `trial_ends_at`, `paid_until` en `auth.families` + backfill)
- ⬜ Activar familias del piloto manualmente: `UPDATE auth.families SET subscription_status='trial', trial_ends_at=now()+interval '30 days' WHERE email='...'`
- ⬜ Configurar env vars Railway: `ADMIN_TOKEN`, `TELEGRAM_BOT_TOKEN`, `TELEGRAM_CHAT_ID`

### Fase 2 — Producto
- ⬜ MercadoPago — suscripción mensual plana (NO créditos)
  - Un solo plan, webhook de pago confirmado → `UPDATE families SET subscription_status='active', paid_until=...`
  - Sin portal de auto-gestión por ahora — activación manual post-piloto
- ⬜ Consentimiento parental (condición legal de lanzamiento)
- ⬜ Resumen cualitativo automático al cerrar sesión (killer feature dashboard) — descartado para piloto
- ⬜ Modo lectura del padre en `/Classroom/{id}` — punto ciego de seguridad identificado
- ⬜ Alertas de inactividad + CTA para hijos sin actividad en el dashboard
- ⬜ Historial de sesiones en modo lectura para el padre
- ⬜ Quiz aprobado trackeable (requiere flujo estructurado a/b/c/d)
- ⬜ Jerarquía Materia → Tema → Secciones (hoy: secciones viven en Classroom — migrable en V2)

---

## Decisión de arquitectura — migraciones de DB

> Las migraciones automáticas con EF Core (`db.Database.Migrate()`) en Railway son frágiles
> cuando el SDK local difiere del entorno de deploy. **Preferir siempre TablePlus.**
>
> **Flujo correcto para cambios de schema:**
> 1. Conectarse a Railway DB desde TablePlus (host público: `zephyr.proxy.rlwy.net:21740`)
> 2. Ejecutar el SQL de la migración manualmente en TablePlus
> 3. Insertar el registro en `public."__EFMigrationsHistory"` con el nombre de la migración y version `8.0.0`
> 4. Agregar la clase de migración vacía en el código (solo para que EF no la aplique dos veces)
>
> Así Railway arranca sin intentar migrar nada que ya esté aplicado.

## Decisión de arquitectura — modelo de cobro

> **Cobro plano mensual (NO créditos).** Se descartó el modelo de `credit_accounts` / `credit_events`.
> El padre paga una suscripción fija. El control de abuso se hace con `MONTHLY_TOKEN_LIMIT` y las
> señales de riesgo del admin, no con un saldo en ARS.
>
> **MercadoPago Suscripciones** (API `preapproval`) es la implementación target para V2.
> Durante el piloto: activación manual desde TablePlus.

## Próximos pasos (Sesión 13)
1. Correr los dos SQLs pendientes en TablePlus y verificar que Railway arranca sin errores
2. Activar familias del piloto (5-6) manualmente
3. MercadoPago — suscripción plana, un solo plan, webhook → actualiza `subscription_status`
4. Consentimiento parental (condición legal antes de cobrar)

---

## Migraciones aplicadas en Railway
| Archivo | Contenido |
|---|---|
| `20260524233347_InitialCreate` | Tablas base: families, users, subjects, classrooms, messages |
| `20260527221738_AddTokenEvents` | Tabla `billing.token_events` |
| `20260528000817_AddMagicTokenToFamily` | Campos `MagicToken` y `MagicTokenExpiry` en `families` |
| `20260530191338_AddStudentProfile` | Columnas `has_adhd`, `nickname`, `school_level`, `grade` en `users` |
| `20260601000000_AddParentProfile` | Columnas `nickname`, `parent_role` en `families` |
| `20260602120000_MakeSubjectIdNullable` | SubjectId nullable en classrooms (raw SQL) |
| `20260602130000_AddMaterialToClassroom` | Columna `Material` en classrooms |
| `20260602140000_AddClassroomExtras` | Columna `CompactSummary` en classrooms |
| `20260602150000_AddStudentProfile2` | Gender, ExplanationLevel, 7 columnas Pref* en users |
| `20260602160000_AddWaitlist` | Tabla `auth.waitlist_entries` |
| `20260531000000_AddMaterialSections` | `material_sections jsonb`, `material_section_index int`, `material_ocr_source text` en classrooms — **PENDIENTE TABLEPLUS** |
| `20260531100000_AddFamilyBilling` | `created_at`, `subscription_status`, `trial_ends_at`, `paid_until` en families — **PENDIENTE TABLEPLUS** |

> Todas aplicadas salvo las dos últimas (pendiente SQL manual en TablePlus).

---

## Decisiones técnicas tomadas
| Decisión | Motivo |
|---|---|
| WebApplicationOptions con ContentRootPath | WebHost.UseContentRoot causaba conflicto en design-time |
| Sin UseHttpsRedirection en Production | Railway maneja SSL en su proxy |
| Dockerfile propio en lugar de nixpacks | Preview SDK 8.0.100-preview.5 de nixpacks incompatible con EF Core 8.x |
| Sin Procfile | Con Dockerfile Railway usa ENTRYPOINT directamente |
| Magic link con Resend | Sin contraseñas — token expira en 15 min |
| UseForwardedHeaders | Railway termina TLS en proxy — magic link debe salir con `https` |
| `db.Database.Migrate()` en startup | Railway no corre `dotnet ef` — auto-migrate al arrancar |
| Migraciones manuales con raw SQL | SDK local incompatible — se escriben a mano con `migrationBuilder.Sql()` |
| `itext7` para extracción de PDF | PdfPig no tiene versión estable en NuGet |
| AJAX chat con handler `OnPostSendAsync` | Evita reload de página — mejor UX |
| `IAntiforgery` inyectado en `_Layout` | CSRF token en meta tag para fetch() desde JS |
| `ViewData["BodyClass"] = "classroom-page"` | Oculta footer y activa layout 100dvh solo en el aula |
| `DateTimeKind.Utc` en queries Npgsql | Npgsql rechaza DateTime sin zona en comparaciones con timestamptz |
| Chart.js vía CDN | Sin NuGet extra — gráfico de barras funcional en el dashboard |
| Demo público `/api/demo` en minimal API | Sin auth, limitado a 10 mensajes, llama Haiku directamente |
| Cobro plano mensual (NO créditos) | El padre entiende pesos, no tokens; créditos agregan complejidad sin valor |
| Secciones en Classroom (no Subject/Topic) | Suficiente para piloto; jerarquía completa es V2 |
| KR3/KR4 no persisten en DB | Son datos de cierre de piloto, se completan una vez — query params en admin |
| Guard por `subscription_status` | Cerrar acceso indefinido por magic link sin romper familias activas |

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
│   │   │   │   ├── Family.cs           ← + CreatedAt, SubscriptionStatus, TrialEndsAt, PaidUntil, IsAccessAllowed
│   │   │   │   ├── User.cs             ← Gender, ExplanationLevel, Pref* enums, StudentUsername
│   │   │   │   └── WaitlistEntry.cs
│   │   │   ├── Academic/
│   │   │   │   ├── Subject.cs
│   │   │   │   ├── Classroom.cs        ← Material, CompactSummary, SubjectId nullable, MaterialSections, MaterialSectionIndex, MaterialOcrSource
│   │   │   │   └── Message.cs
│   │   │   └── Billing/
│   │   │       └── TokenEvent.cs
│   │   └── Migrations/
│   ├── Pages/
│   │   ├── Index.cshtml                ← Landing + demo + waitlist
│   │   ├── Login.cshtml
│   │   ├── Blocked.cshtml              ← Nueva — trial_expired / suspended / cancelled
│   │   ├── Auth/Verify.cshtml          ← + guard subscription_status
│   │   ├── Entrar.cshtml               ← + guard subscription_status (Include Family)
│   │   ├── Dashboard/Index.cshtml      ← Cards + Chart.js por hijo
│   │   ├── Profile/Index.cshtml
│   │   ├── Students/Add.cshtml
│   │   ├── Students/Edit.cshtml        ← Género + prefs + TDAH config + usuario/PIN alumno
│   │   ├── Classroom/Index.cshtml      ← Two-panel, AJAX, typing indicator, secciones sidebar
│   │   ├── Admin/Index.cshtml          ← + Monitor de piloto KR1-KR4 + vencimientos
│   │   └── Shared/_Layout.cshtml       ← CSRF meta, body class, footer condicional
│   ├── Infrastructure/
│   │   ├── ExchangeRateService.cs
│   │   ├── TelegramService.cs
│   │   └── VersionPageFilter.cs
│   ├── wwwroot/css/site.css            ← + estilos section-progress, section-nav
│   └── Program.cs                      ← /api/demo endpoint público
├── railway.json
├── global.json
├── CHANGES.md
├── CONTEXT.md
└── ROADMAP.md
```

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

## Variables de entorno Railway
| Variable | Uso |
|---|---|
| `ANTHROPIC_API_KEY` | Llamadas a Claude |
| `RESEND_API_KEY` | Magic link emails |
| `RESEND_FROM` | Remitente configurable (default: noreply@mitutoria.app) |
| `APP_BASE_URL` | Base URL para magic links detrás de proxy |
| `MONTHLY_TOKEN_LIMIT` | Límite mensual de tokens por familia (default: 500000) |
| `MAX_MATERIAL_CHARS` | Límite de caracteres de material inyectado (default: 15000) |
| `ADMIN_TOKEN` | Protege `/admin` — string largo elegido por el operador |
| `TELEGRAM_BOT_TOKEN` | Token del bot Telegram (BotFather → `/newbot`) |
| `TELEGRAM_CHAT_ID` | Chat ID del operador — ver `/getUpdates` de la API |
| `PILOT_KR1_MIN_EXCHANGES` | Intercambios mínimos 7d para familia activa (default: 3) |
| `PILOT_KR1_THRESHOLD` | Umbral semáforo KR1 (default: 6) |
| `PILOT_KR2_THRESHOLD` | Umbral semáforo KR2 (default: 5) |
| `PILOT_KR3_THRESHOLD` | Umbral semáforo KR3 (default: 5) |
| `PILOT_KR4_THRESHOLD` | Umbral semáforo KR4 (default: 3) |
| `ConnectionStrings__DefaultConnection` | PostgreSQL Railway |

---

## Backlog — ideas anotadas
- [ ] TTS y reconocimiento de voz (especialmente útil con TDAH)
- [ ] Materias → Temas → Secciones (jerarquía completa, V2)
- [ ] Agenda con fechas de examen y registro de notas
- [ ] Resumen de sesión para el padre (qué trabajó, qué logró)
- [ ] PWA — instalar como app desde el celular (sin stores)
- [ ] Auth estudiante via slug: mitutoria.app/u/{apodo} → Fase 2
- [ ] Ambiente staging (develop → staging.mitutoria.app)
- [ ] Consentimiento parental explícito (condición legal antes de cobrar)
- [ ] Modo lectura padre en el aula (supervisión sin chat)
- [ ] Alertas de inactividad en dashboard padre

---

*Actualizado al cierre de Sesión 12*
