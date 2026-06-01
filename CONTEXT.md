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
- **Sesión:** 11
- **Fase activa:** Fase 1 — MVP Familia (casi completa)
- **Branch activo:** `main`
- **Último commit:** feat: admin panel + alertas Telegram waitlist

## Funciona hoy
- ✅ Login de alumno `/Entrar` — usuario + PIN configurado por el padre desde `/Students/Edit`
- ✅ Aula acepta sesión de alumno o de padre
- ✅ Racha de días 🔥 visible en el aula y en el dashboard del padre
- ✅ Botonera del aula: Quiz · Tarjetas (modal flip) · Simulacro (modal a/b/c/d con puntaje)
- ✅ Prompt caching — ahorro ~90% en tokens del material por sesión activa
- ✅ Prompt anti-bucle: tutor avanza cuando el alumno solo confirma lo sabido
- ✅ PDF upload AJAX (sin reload) con OCR fallback vía Claude Vision para PDFs escaneados
- ✅ Tipo de cambio MEP en tiempo real (dolarapi.com, cache 60 min)
- ✅ Dashboard padre rediseñado: intercambios hoy/semana/mes, gasto ARS, racha por hijo
- ✅ Admin `/admin?token=ADMIN_TOKEN` — familias, señales de riesgo, tokens por feature, waitlist
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

### Fase 2 — Auth del alumno (decisión de arquitectura)
- ⬜ Login de alumno con usuario (slug, ej: `vika`) + PIN de 4-6 dígitos hasheado
  - El padre genera las credenciales desde `/Students/Edit/{id}` y se las da al hijo
  - Página `/Entrar` — formulario usuario + PIN, sesión separada a la del padre
  - El aula detecta el tipo de sesión: alumno → chat activo / padre → lectura solamente
  - El botón "Abrir aula" del padre pasa a ser historial en modo lectura (supervisión, no impersonación)
  - **Requiere:** columnas `StudentUsername` + `StudentPinHash` en `users`, nueva migración, nueva página de login, lógica de sesión dual en el aula

### Fase 2 — Producto
- ⬜ MercadoPago — sistema de créditos en ARS (diseño detallado abajo)
- ⬜ Panel de admin — uso por familia, saldo API maestra, pagos recibidos
- ⬜ Consentimiento parental (condición legal de lanzamiento)
- ⬜ Resumen cualitativo automático al cerrar sesión (killer feature dashboard)
- ⬜ Alertas de inactividad + CTA para hijos sin actividad en el dashboard
- ⬜ Historial de sesiones en modo lectura para el padre
- ⬜ Acordeón de materias/temas por aula (base para mapa curricular y metas)
- ⬜ Quiz aprobado trackeable (requiere flujo estructurado a/b/c/d)
- ⬜ Logros/achievements calculados (primera sesión, 7 días de racha, etc.)
- ⬜ Mergear ramas pendientes: `feature/fix-login-flow`, `feature/fix-landing-login-link`, `feature/dashboard-parent`

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

## Próximos pasos (Sesión 12)
1. Modo lectura para el padre en `/Classroom/{id}` — punto ciego de seguridad identificado
2. MercadoPago — créditos en ARS con webhook casi desatendido (diseño detallado en sección billing)
3. Resumen cualitativo automático por sesión (killer feature del dashboard padre)
4. Configurar env vars Railway: `ADMIN_TOKEN`, `TELEGRAM_BOT_TOKEN`, `TELEGRAM_CHAT_ID`

---

## 💡 MercadoPago — plan casi desatendido (pendiente implementar)

**Arquitectura:**
- `billing.credit_accounts` — `family_id` (FK único), `balance_ars` decimal, `updated_at`
- `billing.credit_events` — `family_id`, `amount_ars` (+compra / -consumo), `type` ('purchase'/'consume'), `mp_payment_id` nullable, `created_at`
- Reemplaza el límite de tokens (`MONTHLY_TOKEN_LIMIT`) por chequeo de saldo

**Flujo de compra:**
1. Padre va a `/Billing/Recargar` — elige paquete ($2.000 / $5.000 / $10.000 ARS)
2. Sistema crea preferencia MP con `external_reference = family_id` (no email, más confiable)
3. Padre paga por link o QR de MP
4. MP envía webhook a `/api/mp-webhook` (firmado, verificar con `x-signature`)
5. Webhook: verificar firma → acreditar `credit_accounts.balance_ars` → insertar `credit_event` tipo 'purchase'

**Flujo de consumo:**
- Cada llamada a Claude: descontar `cost_usd * ars_rate` del saldo
- Si saldo ≤ 0: bloquear con mensaje al alumno y notificación al padre
- Registrar `credit_event` tipo 'consume'

**Panel de admin:**
- Ruta protegida por env var `ADMIN_TOKEN` — `GET /admin?token=...`
- Ver: todas las familias, saldo, consumo del mes en USD y ARS, pagos recibidos
- Ver: saldo total API maestra (suma de cost_usd de todos los token_events)

**Margen:**
- Precio al usuario en ARS incluye ~50% margen sobre costo real en USD × MEP
- Ej: paquete $5.000 ARS → costo API real ≈ $2.500 ARS → ganancia $2.500

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
| `ADMIN_TOKEN` | Protege `/admin` — string largo elegido por el operador |
| `TELEGRAM_BOT_TOKEN` | Token del bot Telegram (BotFather → `/newbot`) |
| `TELEGRAM_CHAT_ID` | Chat ID del operador — ver `/getUpdates` de la API |
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
