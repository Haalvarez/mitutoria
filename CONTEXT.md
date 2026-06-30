# CONTEXT.md — miTutorIA
> Pegar este archivo al inicio de cada sesión con Claude.
> Actualizar al cierre de cada sesión.

---

## 🎯 Objetivo Último (OÚ)
Lanzar una plataforma educativa multi-tenant con IA controlada por padres,
monetizable, construida sobre ASP.NET Core 8 + Railway.
**Criterio de éxito de Fase 1:** Vika, Dasha y Egor pueden loguearse
y chatean en su aula desde mitutoria.app. ✅ ALCANZADO en Sesión 15.

**Criterio de éxito de Fase 2 (piloto):** 5-10 familias conocidas usando la
plataforma con uso sostenido en 2-3 semanas.

---

## Estado actual
- **Sesión:** 19 (2026-06-30)
- **Fase activa:** Fase 2 — Piloto cerrado (Track 1) + Fase 4 exploratoria (Track 2)
- **Branch activo:** `main` (Track 1) · `feature/inbox-pipeline` por abrir (Track 2)
- **Último commit:** fix: el tutor ahora ve todo el material del PDF + waitlist con nombre/WhatsApp + histórico en admin (Sesión 19)
- **Pendiente de esta sesión:** correr `tools/prompt-harness.ps1` (incluye escenario nuevo "Trabada de verdad" + dimensión `no_ayuda`) para validar el prompt antes de invitar familias.

## Tracks en paralelo — disciplina de foco

> Track 1 tiene prioridad. Track 2 NO mergea a main hasta que Track 1 tenga datos del piloto.

### Track 1 — Piloto cerrado (rama `main`)
Lo único que destraba invitar familias reales.

1. Verificar SQLs aplicados en TablePlus:
   - `AddMaterialSections` (material_sections jsonb, material_section_index, material_ocr_source)
   - `AddFamilyBilling` (created_at, subscription_status, trial_ends_at, paid_until)
   - `public.error_logs` (crear manualmente, sin migración EF)
2. **Página de consentimiento parental mínima** en alta (checkbox + texto honesto + persistencia: fecha, IP, versión). Condición legal de lanzamiento, no negociable.
3. Setup de **staging en Railway** (`staging.mitutoria.app`, rama `develop`, DB separada, basic auth, robots noindex).
4. Activar familias del piloto vía TablePlus: `UPDATE auth.families SET subscription_status='trial', trial_ends_at=now()+interval '30 days' WHERE email='...'`
5. Invitar 5-10 familias conocidas. Piloto **sin cobro** las primeras 2-3 semanas.
6. Medición de uso real (token_events) durante 2-3 semanas.

### Track 2 — Inbox / captura de Classroom (rama `feature/inbox-pipeline`)
Feature exploratoria, condicionada a validación.

**Hipótesis a validar:** "Padres dispuestos a configurar forward de Gmail para recibir alertas de tareas pendientes de sus hijos."

1. **Sprint 1 (semana 1):**
   - Setup Postmark Inbound (sandbox)
   - DNS: subdominio `in.mitutoria.app` con MX a Postmark
   - Endpoint webhook autenticado (validar firma Postmark)
   - Análisis de 30-50 mails reales de Classroom (Vika, conocidos) → tabla de variantes en `docs/classroom-mail-types.md`
   - Parser en C# como class library `miTutoria.Inbox/` con fixtures `.eml`
   - Modelo de datos: `InboxAlias`, `InboxMessageRaw`, `DetectedAssignment`
2. **Checkpoint: validación con 3 padres externos** antes de Sprint 2. Mockup estático del mail diario. Si <2/3 se entusiasman → parar y volver a Track 1.
3. **Sprint 2 (semana 2):** generación de alias por usuario, página de onboarding con instrucciones de forward, captura del código de verificación de Gmail, vista "Mis tareas".
4. **Sprint 3 (semana 3):** notificaciones (mail nueva tarea, resumen diario, alerta de urgencia 24hs antes).
5. **Sprint 4 (semana 4):** dashboard del padre, manejo de errores, multi-hijo, testing con 1-2 padres reales en staging.

**Vika como usuaria beta en staging durante todo el track.**

### Track 3 — Cobro y régimen formal (PARQUEADO ~2 meses)
No se discute hasta ~2 meses antes de cobrar de verdad. Roadmap específico al activarse.

**Decisiones pendientes anotadas:**
- CUIT activo (verificado en ARCA Sesión 16) pero **sin impuestos activos** — requiere reinscripción para facturar.
- Camino tentativo: reinscripción monotributo categoría A → cuenta MP vendedor → cobro por QR con `external_reference` por familia → webhooks → `subscription_status`.
- Validar antes que justifique el costo mensual del monotributo.

---

## Funciona hoy
- ✅ Material completo del PDF en contexto (Sesión 19)
  - El tutor recibe **todo** el material (`classroom.Material` verbatim, cap 30k), no solo la sección activa
  - Las secciones quedan como puntero pedagógico ("estás en la sección X de Y"), no como filtro
  - Corrige el bug por el que el tutor "no se basaba en el material" al preguntar por otra sección
- ✅ Waitlist con nombre obligatorio + WhatsApp opcional (Sesión 19)
  - Nombre requerido, `Phone` nullable, alerta Telegram + columna WhatsApp (link `wa.me`) en `/admin`
- ✅ Columna "Interc. total" (histórico) en la tabla de familias de `/admin` (Sesión 19)
- ✅ Prompt del tutor calibrado (Sesión 18)
  - Regla absoluta contra insultos/groserías ("boludo" etc.) en todo contexto, aunque el alumno los use
  - Material como **ancla, no cárcel**: tolera preguntas genuinas de la misma materia fuera del PDF; solo deriva al cambiar de MATERIA
  - **Escalera de ayuda** cuando el alumno se traba de verdad (achica el paso → enseña el concepto → pista concreta → ejemplo parecido); nunca da el resultado de SU ejercicio
  - Harness con dimensión `no_ayuda` + escenario "Trabada de verdad" (sincronizado con `BuildSystemPrompt`)
- ✅ Subida de material por **foto** además de PDF (Sesión 18) — JPG/PNG/WebP/GIF vía Claude Vision (tope 5 MB), con `accept`/validación/UI actualizados
- ✅ Analítica de landing (Sesión 18) — tabla `public.landing_hits` (filtra bots y preview de WhatsApp) + tarjetas en `/admin`: hits hoy/7d/histórico + conversión a waitlist
- ✅ Landing responsive en mobile (Sesión 18) — 6 fixes: sin scroll horizontal, titular que escala, botón "Enviar" del demo visible, nav abreviado ("Estudiante"/"Familiar"), botón flotante como píldora inferior
- ✅ Mochila — materias por alumno (Sesión 15)
  - `Classroom` = cuaderno de materia: un alumno tiene varias, cada una con nombre,
    modo pedagógico, historia de chat y material propios
  - Selector 🎒 en el sidebar + "＋ Nueva materia" (infiere modo del nombre)
  - `GetActiveClassroomAsync` resuelve el cuaderno activo (sesión → reciente → crea "General")
  - Modo pedagógico (Resolución / Comprensión): el prompt se ramifica
  - Foco de materia: si el alumno trae otra materia, el tutor sugiere cambiarla arriba
  - ⬜ Falta: borrar/renombrar materia, ícono y orden manual (hoy alfabético)
  - ⬜ Calibración fina del modo Comprensión
- ✅ Prompt maestro endurecido (Sesión 14)
  - Nunca menciona diagnósticos/etiquetas; figura de autoridad cercana
  - Registro elástico + foco rígido (el puente)
  - Exige esfuerzo, no formalidad; brevedad y texto plano
- ✅ Backtest del prompt: `tools/prompt-harness.ps1` (12 escenarios, juez Haiku, 12/12 verde)
  - ⚠️ El prompt está duplicado en el script — sincronizar al tocar `BuildSystemPrompt`
- ✅ `/Students/Edit`: mensaje cálido del tutor al guardar (token_event Feature="explain")
- ✅ Login alumno `/Entrar` — usuario + PIN
- ✅ Guard de acceso por `subscription_status` → `/Blocked`
- ✅ Aula acepta sesión de alumno o de padre
- ✅ Racha 🔥 en aula y dashboard
- ✅ Botonera del aula: Quiz, Tarjetas, Examen de práctica (modales)
- ✅ Prompt caching — ahorro ~90% en tokens del material por sesión
- ✅ Prompt anti-bucle
- ✅ PDF upload integrado al chat + drag&drop + mensajes animados
- ✅ OCR fallback vía Claude Vision para PDFs escaneados
- ✅ Secciones de material persistentes (sidebar ✅/▶/○)
- ✅ Nav contextual en el aula (logo, nombre, mensaje de racha)
- ✅ Logo real (JPG) en nav, footer y favicon
- ✅ Error log en DB visible en `/admin`
- ✅ Tipo de cambio MEP en tiempo real (cache 60 min)
- ✅ Dashboard padre rediseñado con intercambios y gasto ARS
- ✅ Admin `/admin?token=...` con monitor de piloto (KR1/KR2/KR3/KR4)
- ✅ Alertas Telegram al anotarse en waitlist

## Funciona desde antes
- ✅ mitutoria.app live en Railway, deploy automático push → Railway ~2 min
- ✅ Landing con demo en vivo + lista de espera
- ✅ Magic link (Resend), sesión cookie HttpOnly 7 días
- ✅ Dashboard padre — lista de hijos, gráfico Chart.js
- ✅ Perfil padre, agregar/editar hijo
- ✅ Aula `/Classroom/{studentId}` con chat AJAX
- ✅ token_events después de cada llamada
- ✅ Migraciones manuales vía TablePlus

## No funciona / pendiente

### Track 1 — Piloto (próximos pasos Sesión 16)
- ⬜ Correr SQLs pendientes en TablePlus (ver lista arriba)
- ⬜ Página de consentimiento parental mínima en alta
- ⬜ Setup staging en Railway
- ⬜ Activar familias del piloto y enviar invitaciones

### Track 2 — Inbox (rama por abrir)
- ⬜ Abrir rama `feature/inbox-pipeline` desde `develop`
- ⬜ Postmark cuenta sandbox
- ⬜ Sprint 1 según detalle arriba

### Backlog Fase 5+ — Producto
- ⬜ Modo lectura del padre en `/Classroom/{id}`
- ⬜ Avatares personalizables (galería emojis, post-piloto)
- ⬜ Resumen cualitativo automático por sesión
- ⬜ Jerarquía Materia → Tema → Secciones (V2)
- ⬜ PWA

---

## Decisión de arquitectura — migraciones de DB

> Flujo correcto: TablePlus → SQL manual → insertar en `__EFMigrationsHistory` → clase vacía en código.
> **IMPORTANTE:** toda propiedad nueva en una entidad necesita `HasColumnName` en `AppDbContext`
> o Railway falla al arrancar con error "column X does not exist".

## Decisión de arquitectura — modelo de cobro

> Cobro **plano mensual** (NO créditos). Activación manual durante el piloto desde TablePlus.
> MercadoPago **cobro por QR con `external_reference`** es el target inicial (no `preapproval`
> todavía: menos fricción y suficiente para piloto que se vuelca al régimen común).
> Track 3 parqueado, retomar en ~2 meses con roadmap específico.

## Decisión de arquitectura — ambientes

> Staging en Railway con rama `develop` y DB separada. Track 2 vive ahí hasta que valide.
> Track 1 sigue pusheando a `main` directamente (sin merge desde develop hasta que esté maduro).

## Decisión de arquitectura — Inbox / inbound mail

> Postmark Inbound como proveedor (mejor DX que SendGrid/Mailgun para este uso).
> Subdominio dedicado `in.mitutoria.app` con MX propio para no interferir con outbound.
> Alias **por padre** (no por hijo): un solo forward configurado, parser asigna a hijo por contenido.
> Toggle activo/inactivo = flag en DB, no corta recepción (mails entran, se descartan si inactivo).
> Retención: HTML crudo 30 días, datos estructurados indefinido.

---

## Migraciones aplicadas en Railway
| Archivo | Contenido |
|---|---|
| `20260524233347_InitialCreate` | Tablas base |
| `20260527221738_AddTokenEvents` | `billing.token_events` |
| `20260528000817_AddMagicTokenToFamily` | `MagicToken`, `MagicTokenExpiry` en families |
| `20260530191338_AddStudentProfile` | `has_adhd`, `nickname`, `school_level`, `grade` en users |
| `20260601000000_AddParentProfile` | `nickname`, `parent_role` en families |
| `20260602120000_MakeSubjectIdNullable` | SubjectId nullable en classrooms |
| `20260602130000_AddMaterialToClassroom` | `Material` en classrooms |
| `20260602140000_AddClassroomExtras` | `CompactSummary` en classrooms |
| `20260602150000_AddStudentProfile2` | Gender, ExplanationLevel, Pref* en users |
| `20260602160000_AddWaitlist` | `auth.waitlist_entries` |
| `20260531000000_AddMaterialSections` | `material_sections jsonb`, `material_section_index`, `material_ocr_source` — **verificar TablePlus** |
| `20260531100000_AddFamilyBilling` | `created_at`, `subscription_status`, `trial_ends_at`, `paid_until` en families — **verificar TablePlus** |
| `20260603120000_AddSubjectMochila` | `name`, `mode`, `last_active_at` en classrooms — **aplicado en TablePlus (Sesión 15)** |
| `20260620120000_AddLandingHits` | `public.landing_hits` (analítica de la landing) — **auto-aplicable: corre sola con `Migrate()` en el arranque, NO requiere TablePlus** |
| `20260630120000_AddWaitlistPhone` | `"Phone"` en `auth.waitlist_entries` (WhatsApp opcional) — **auto-aplicable: `ADD COLUMN IF NOT EXISTS`, corre sola con `Migrate()`, NO requiere TablePlus** |

> `public.error_logs` — crear manualmente, no tiene migración EF.
> **Sesión 16+ (Track 2):** se agregarán tablas `inbox_aliases`, `inbox_messages_raw`, `detected_assignments` cuando arranque Sprint 1.

---

## Decisiones técnicas tomadas
| Decisión | Motivo |
|---|---|
| WebApplicationOptions con ContentRootPath | WebHost.UseContentRoot causaba conflicto en design-time |
| Sin UseHttpsRedirection en Production | Railway maneja SSL en su proxy |
| Dockerfile propio | nixpacks incompatible con EF Core 8.x |
| Magic link con Resend | Sin contraseñas — token expira en 15 min |
| UseForwardedHeaders | Railway termina TLS en proxy |
| Migraciones manuales con raw SQL | SDK local incompatible |
| `itext7` para extracción de PDF | PdfPig no tiene versión estable |
| AJAX chat con `OnPostSendAsync` | Evita reload de página |
| `IAntiforgery` en `_Layout` | CSRF token en meta tag para fetch() |
| `ViewData["BodyClass"] = "classroom-page"` | Oculta footer y activa layout 100dvh |
| `DateTimeKind.Utc` en queries Npgsql | Npgsql rechaza DateTime sin zona |
| Chart.js vía CDN | Sin NuGet extra |
| Demo público `/api/demo` | Sin auth, limitado a 10 mensajes |
| Cobro plano mensual (NO créditos) | El padre entiende pesos, no tokens |
| Secciones en Classroom (no Subject/Topic) | Suficiente para piloto, migrable en V2 |
| KR3/KR4 no persisten en DB | Datos de cierre, se completan una vez |
| Guard por `subscription_status` | Cierra acceso indefinido por magic link |
| `ExtractJsonArray` en lugar de `JsonDocument.Parse` directo | Claude a veces envuelve JSON en markdown |
| `HasColumnName` obligatorio para toda propiedad nueva | Sin él EF genera nombre C# con comillas y Railway falla |
| Logo JPG en nav/footer/favicon | SVG pendiente — listo para swap |
| `ErrorLogService` scoped, nunca tira excepción | Errores de log no pueden romper la app |
| **Dual-track Sesión 16+** | Track 1 (piloto) no se posterga por Track 2 (inbox exploratorio) |
| **Postmark Inbound para Track 2** | Mejor DX, webhook JSON limpio, sandbox gratis |
| **Alias inbox por padre, no por hijo** | Onboarding más simple, parser asigna hijo por contenido |

---

## Estructura del repo
```
mitutoria/
├── miTutoria.Web/                    ← proyecto principal Razor Pages
│   ├── Data/
│   │   ├── AppDbContext.cs           ← HasColumnName para todas las props nuevas
│   │   ├── Entities/
│   │   │   ├── Auth/Family.cs        ← + CreatedAt, SubscriptionStatus, TrialEndsAt, PaidUntil
│   │   │   ├── Auth/User.cs
│   │   │   ├── Academic/Classroom.cs ← + MaterialSections, MaterialSectionIndex, MaterialOcrSource
│   │   │   ├── Academic/Message.cs
│   │   │   ├── Billing/TokenEvent.cs
│   │   │   └── ErrorLog.cs           ← public.error_logs
│   │   └── Migrations/
│   ├── Infrastructure/
│   │   ├── ErrorLogService.cs
│   │   ├── ExchangeRateService.cs
│   │   └── TelegramService.cs
│   ├── Pages/
│   │   ├── Index.cshtml              ← Landing
│   │   ├── Login.cshtml
│   │   ├── Blocked.cshtml
│   │   ├── Auth/Verify.cshtml        ← guard subscription_status
│   │   ├── Entrar.cshtml             ← guard subscription_status
│   │   ├── Dashboard/Index.cshtml
│   │   ├── Students/Edit.cshtml
│   │   ├── Classroom/Index.cshtml
│   │   ├── Admin/Index.cshtml
│   │   └── Shared/_Layout.cshtml
│   ├── wwwroot/
│   │   ├── css/site.css
│   │   └── img/logo.jpg
│   └── Program.cs
├── miTutoria.Inbox/                  ← (Track 2, por crear) class library: parser + entidades inbox
├── tools/
│   └── prompt-harness.ps1
├── docs/
│   └── classroom-mail-types.md       ← (Track 2, por crear) tabla de variantes parser
├── CHANGES.md
├── CONTEXT.md
└── ROADMAP.md
```

---

## DB Railway
| Dato | Valor |
|---|---|
| Host público (dev local) | zephyr.proxy.rlwy.net:21740 |
| Database | railway |
| Esquemas | auth, academic, billing, public |
| Cliente recomendado | TablePlus |

---

## Variables de entorno Railway
| Variable | Uso |
|---|---|
| `ANTHROPIC_API_KEY` | Llamadas a Claude |
| `RESEND_API_KEY` | Magic link emails |
| `RESEND_FROM` | Remitente (default: noreply@mitutoria.app) |
| `APP_BASE_URL` | Base URL para magic links |
| `MONTHLY_TOKEN_LIMIT` | Límite mensual tokens por familia (default: 500000) |
| `ADMIN_TOKEN` | Protege `/admin` |
| `TELEGRAM_BOT_TOKEN` | Alertas de waitlist |
| `TELEGRAM_CHAT_ID` | Chat ID del operador |
| `PILOT_KR1_MIN_EXCHANGES` | Intercambios mínimos 7d para familia activa (default: 3) |
| `PILOT_KR1_THRESHOLD` | Umbral semáforo KR1 (default: 6) |
| `PILOT_KR2_THRESHOLD` | Umbral semáforo KR2 (default: 5) |
| `PILOT_KR3_THRESHOLD` | Umbral semáforo KR3 (default: 5) |
| `PILOT_KR4_THRESHOLD` | Umbral semáforo KR4 (default: 3) |
| `ConnectionStrings__DefaultConnection` | PostgreSQL Railway |
| `POSTMARK_INBOUND_TOKEN` | (Track 2) firma de webhook Postmark |
| `INBOUND_DOMAIN` | (Track 2) `in.mitutoria.app` o `in-staging.mitutoria.app` |

---

## Backlog — ideas anotadas
- [ ] **Cobro / fiscalidad** — retomar en ~2 meses, roadmap específico
- [ ] Avatares personalizables tutor y alumno — galería fija emojis, post-piloto
- [ ] Logo en SVG (usar vectorizer.io con el JPG actual)
- [ ] TTS y reconocimiento de voz (TDAH)
- [ ] Materias → Temas → Secciones (jerarquía completa, V2)
- [ ] Agenda con fechas de examen
- [ ] Resumen de sesión para el padre
- [ ] PWA — instalar como app desde el celular
- [ ] Modo lectura padre en el aula
- [ ] Alertas de inactividad en dashboard padre
- [ ] Mochila — borrar/renombrar materia, ícono y orden manual
- [ ] Calibración fina del modo Comprensión
- [ ] Rotar API key de Anthropic (sin urgencia — no fue expuesta públicamente)

---

*Actualizado al cierre de Sesión 19 (2026-06-30) — fix clave previo a invitar familias: el tutor ahora ve TODO el material del PDF (no solo la sección activa); waitlist con nombre obligatorio + WhatsApp opcional; columna histórico de intercambios en /admin. Pruebas del harness pendientes.*
