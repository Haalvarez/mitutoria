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
- **Sesión:** 10
- **Fase activa:** Fase 1 — MVP Familia (casi completa)
- **Branch activo:** `main`
- **Último commit:** feat: classroom redesign — two-panel layout, AJAX chat, typing indicator, avatars

## Funciona hoy
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
  - Sidebar: material (PDF hasta 5MB con itext7 + texto pegado), config tutor, resumen compacto
  - Botones Compactar (Claude resume → borra mensajes) y Nueva sesión (borra todo)
  - Footer oculto en el aula, layout 100dvh
- ✅ Integración Anthropic API — Haiku 4.5, prompt socrático v1 con género y preferencias inyectadas
- ✅ token_events registrado después de cada llamada — FamilyId, UserId, tokens in/out, CostUsd, Feature
- ✅ Límite mensual configurable `MONTHLY_TOKEN_LIMIT` (default 500k tokens)
- ✅ Límite de material `MAX_MATERIAL_CHARS` (default 15k chars, trunca con aviso)
- ✅ Migraciones todas aplicadas en Railway vía `db.Database.Migrate()` en startup
- ✅ TablePlus conectado a Railway DB (conexión pública)
- ✅ PostgreSQL en Railway: esquemas `auth`, `academic`, `billing`, `public` (__EFMigrations)

## No funciona / pendiente
- ⬜ Verificar aula redesign (AJAX chat) en producción post-deploy
- ⬜ Dashboard muestra USD — convertir a ARS para el padre (ver modelo de créditos abajo)
- ⬜ Botonera del aula: Modo Examen, Generar Quiz, Simulacro
- ⬜ Materias por aula (hoy una aula por hijo sin materia)
- ⬜ MercadoPago — sistema de créditos en ARS (ver diseño abajo)
- ⬜ Consentimiento parental (condición legal de lanzamiento)
- ⬜ Mergear ramas pendientes: `feature/fix-login-flow`, `feature/fix-landing-login-link`, `feature/dashboard-parent`

---

## Próximos 3 pasos (Sesión 10)
1. Verificar aula AJAX en producción
2. Dashboard en ARS (no tokens) — mostrar crédito disponible y gasto en pesos
3. Botonera del aula — Modo Examen como primer botón

---

## 💡 Modelo de negocio — créditos en ARS (diseñado, pendiente implementar)

El padre **no quiere ver tokens**. Quiere ver pesos y saldo disponible.

**Flujo:**
- El padre compra créditos en ARS vía MercadoPago (QR o link de pago)
- Cada llamada a Claude descuenta del saldo según costo real en USD convertido a ARS
- El sistema bloquea cuando el saldo llega a cero
- MercadoPago webhook → acredita automáticamente al recibir pago confirmado

**Margen:**
- Precio al usuario: $X ARS → 50% margen → mitad cubre costo API en USD
- Ejemplo: usuario paga $10 USD equivalente en ARS → $5 USD va a API, $5 es ganancia
- El exchange rate debe revisarse periódicamente (hardcodeado o via API de tipo de cambio)

**Tablas a agregar:**
- `billing.credit_accounts` — saldo_ars, family_id, updated_at
- `billing.credit_events` — family_id, amount_ars, type (purchase/consume), mp_payment_id, created_at
- Reemplaza el límite de tokens por límite de saldo ARS

**Dashboard padre:**
- Mostrar: "Crédito disponible: $X.XX" y "Gastado este mes: $X.XX"
- No mostrar tokens ni USD al padre final

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

> Todas aplicadas. `AddHasAdhdToUser` nunca existió — sus columnas están en `AddStudentProfile`.

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
| `AddHasAdhdToUser` nunca existió | Columnas absorbidas en `AddStudentProfile` |
| `itext7` para extracción de PDF | PdfPig no tiene versión estable en NuGet |
| AJAX chat con handler `OnPostSendAsync` | Evita reload de página — mejor UX |
| `IAntiforgery` inyectado en `_Layout` | CSRF token en meta tag para fetch() desde JS |
| `ViewData["BodyClass"] = "classroom-page"` | Oculta footer y activa layout 100dvh solo en el aula |
| `DateTimeKind.Utc` en queries Npgsql | Npgsql rechaza DateTime sin zona en comparaciones con timestamptz |
| Chart.js vía CDN | Sin NuGet extra — gráfico de barras funcional en el dashboard |
| Demo público `/api/demo` en minimal API | Sin auth, limitado a 10 mensajes, llama Haiku directamente |
| Créditos en ARS (pendiente) | El padre entiende pesos, no tokens ni USD |

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
│   │   │   │   ├── User.cs         ← Gender, ExplanationLevel, Pref* enums
│   │   │   │   └── WaitlistEntry.cs
│   │   │   ├── Academic/
│   │   │   │   ├── Subject.cs
│   │   │   │   ├── Classroom.cs    ← Material, CompactSummary, SubjectId nullable
│   │   │   │   └── Message.cs
│   │   │   └── Billing/
│   │   │       └── TokenEvent.cs
│   │   └── Migrations/
│   ├── Pages/
│   │   ├── Index.cshtml            ← Landing + demo + waitlist
│   │   ├── Login.cshtml
│   │   ├── Auth/Verify.cshtml
│   │   ├── Dashboard/Index.cshtml  ← Cards + Chart.js por hijo
│   │   ├── Profile/Index.cshtml
│   │   ├── Students/Add.cshtml
│   │   ├── Students/Edit.cshtml    ← Género + prefs + TDAH config
│   │   ├── Classroom/Index.cshtml  ← Two-panel, AJAX, typing indicator
│   │   └── Shared/_Layout.cshtml   ← CSRF meta, body class, footer condicional
│   ├── wwwroot/css/site.css
│   └── Program.cs                  ← /api/demo endpoint público
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
| `ConnectionStrings__DefaultConnection` | PostgreSQL Railway |

---

## Backlog — ideas anotadas
- [ ] Botonera del aula: Modo Examen, Generar Quiz, Simulacro desde PDF
- [ ] Botones contextuales mid-chat ("¿Querés un ejemplo?", "¿Lo vemos de otra forma?")
- [ ] TTS y reconocimiento de voz (especialmente útil con TDAH)
- [ ] Materias por aula con temas en accordion
- [ ] Agenda con fechas de examen y registro de notas
- [ ] Resumen de sesión para el padre (qué trabajó, qué logró)
- [ ] PWA — instalar como app desde el celular (sin stores)
- [ ] OCR para PDFs escaneados vía Claude Vision (registrar como `feature=pdf_ocr`)
- [ ] Auth estudiante via slug: mitutoria.app/u/{apodo} → Fase 2
- [ ] Ambiente staging (develop → staging.mitutoria.app)
- [ ] Consentimiento parental explícito (condición legal antes de lanzar)

---

## POST-CAMBIO — Sesión 9
- Classroom `/Classroom/{studentId}` — aula completa con Anthropic API integrada
- Chat AJAX sin reload, typing indicator, burbujas con avatar, layout dos paneles
- token_events registrado por cada mensaje (Feature=chat) y compactación (Feature=compact)
- `/Students/Edit/{id}` — género, preferencias de aprendizaje, config TDAH con tacto
- Prompt socrático v1 usa pronombres según género y ajusta según preferencias
- Landing: demo en vivo (5 mensajes) + lista de espera cableada a DB
- Diseño del modelo de créditos en ARS para Fase 2 (pendiente implementar)

---

*Actualizado al cierre de Sesión 9 / inicio de Sesión 10*
