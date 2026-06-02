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
- **Sesión:** 14
- **Fase activa:** Fase 1 — MVP Familia (lista para piloto)
- **Branch activo:** `main`
- **Último commit:** feat: el padre ve, al guardar, cómo el tutor acompañará a su hijo

## Funciona hoy
- ✅ Prompt maestro endurecido (Sesión 14)
  - Nunca menciona diagnósticos/etiquetas — recibe comportamientos, no la etiqueta TDAH
  - Nunca usa insultos coloquiales; figura de autoridad cercana
  - Registro elástico (espeja la energía del alumno) + foco rígido (el puente: trae de
    vuelta en el mismo mensaje, nunca dos seguidos fuera de tema)
  - Exige esfuerzo, no formalidad; brevedad y texto plano por defecto
  - Instrucciones del padre y resumen previo se aplican en silencio
- ✅ Backtest del prompt: `tools/prompt-harness.ps1` (12 escenarios, juez Haiku, 12/12 verde)
  - ⚠️ El prompt está duplicado en el script — sincronizar al tocar `BuildSystemPrompt`
- ✅ `/Students/Edit`: al guardar muestra un mensaje cálido del tutor (redactado por Claude)
  explicando cómo acompañará al hijo — token_event Feature="explain", fallback si la API falla
- ✅ Login de alumno `/Entrar` — usuario + PIN configurado por el padre desde `/Students/Edit`
- ✅ Guard de acceso: `subscription_status` chequeado en Verify (padre) y Entrar (alumno)
  - `trial` / `active` → acceso normal
  - `trial_expired` / `suspended` / `cancelled` → página `/Blocked` con mensaje claro
  - Familias sin estado o en `waitlist` → bloqueadas automáticamente
- ✅ Aula acepta sesión de alumno o de padre
- ✅ Racha de días 🔥 visible en el aula y en el dashboard del padre
- ✅ Botonera del aula: **Quiz** (modal) · **Tarjetas** (modal flip) · **Examen de práctica** (modal a/b/c/d con puntaje)
  - Quiz y Examen de práctica comparten el mismo modal — label diferenciado en el contador
  - Fix: `ExtractJsonArray` extrae JSON aunque Claude lo envuelva en bloques markdown
- ✅ Prompt caching — ahorro ~90% en tokens del material por sesión activa
- ✅ Prompt anti-bucle: tutor avanza cuando el alumno solo confirma lo sabido
- ✅ PDF upload integrado al input del chat
  - Botón 📎 en el input bar — sin botón viejo en el sidebar
  - Drag & drop sobre el área del chat con overlay visual
  - Durante la carga: burbuja del tutor con mensajes rotativos simpáticos cada 2.5s
  - Avatar del tutor pulsa en terracota `#C1440E` mientras procesa cualquier respuesta
- ✅ OCR fallback vía Claude Vision para PDFs escaneados
- ✅ Secciones de material: PDF segmentado automáticamente en secciones temáticas por Haiku
  - Progreso persistente entre sesiones (`material_section_index`)
  - Sidebar con barra ✅/▶/○ y botones Siguiente/Anterior
  - "Nueva sesión" preserva material y secciones — solo borra mensajes
- ✅ Nav contextual en el aula (reemplaza el nav de landing)
  - Logo real → link al dashboard
  - Nombre del alumno
  - Mensaje de racha según streak (0/1/2-6/7-13/14+ días)
- ✅ Logo real (JPG) en nav, footer y favicon — listo para swap a SVG
- ✅ Error log en DB (`public.error_logs`) — visible en `/admin` con últimos 50 errores
  - `ErrorLogService` nunca tira excepción — errores internos nunca se exponen al cliente
- ✅ Tipo de cambio MEP en tiempo real (dolarapi.com, cache 60 min)
- ✅ Dashboard padre rediseñado: intercambios hoy/semana/mes, gasto ARS, racha por hijo
- ✅ Admin `/admin?token=ADMIN_TOKEN` — familias, señales de riesgo, tokens por feature, waitlist
- ✅ Monitor de piloto en admin: KR1/KR2 calculados, KR3/KR4 inputs manuales, semáforo 🟢🟡🔴, señal de vencimientos ≤3 días
- ✅ Alertas Telegram al anotarse en waitlist

## Funciona desde antes
- ✅ mitutoria.app live en Railway, deploy automático push → Railway ~2 min
- ✅ Landing page con demo en vivo (5 mensajes sin login) + lista de espera funcional → DB
- ✅ Auth magic link — Login + Verify, Resend integrado, APP_BASE_URL, forwarded headers
- ✅ Sesión con cookie HttpOnly 7 días
- ✅ Dashboard padre — lista de hijos, botones "Editar" y "Abrir aula"
- ✅ Dashboard padre — gráfico de barras Chart.js de consumo por hijo por día del mes
- ✅ Perfil padre `/Profile` — nombre, apodo, rol (Padre/Madre)
- ✅ Agregar hijo `/Students/Add` — nombre, apodo, género, nivel, año, TDAH
- ✅ Editar hijo `/Students/Edit/{id}` — datos básicos + estilo de aprendizaje + config TDAH
- ✅ Aula `/Classroom/{studentId}` — layout dos paneles (sidebar + chat full-height)
  - AJAX chat sin reload, typing indicator, burbujas con avatar
  - Input dinámico, Enter envía, Shift+Enter nueva línea
  - Sidebar: secciones del material, apunte extra, resumen compacto
  - Footer oculto en el aula, layout 100dvh
- ✅ Integración Anthropic API — Haiku 4.5, prompt socrático v1
- ✅ token_events registrado después de cada llamada
- ✅ Límite mensual configurable `MONTHLY_TOKEN_LIMIT`
- ✅ Migraciones aplicadas en Railway vía TablePlus (manual)
- ✅ PostgreSQL en Railway: esquemas `auth`, `academic`, `billing`, `public`

## No funciona / pendiente

### Piloto (pasos manuales antes de invitar familias)
- ⬜ Correr SQL `AddMaterialSections` en TablePlus (si no está aplicado)
- ⬜ Correr SQL `AddFamilyBilling` en TablePlus (si no está aplicado)
- ⬜ Crear tabla `public.error_logs` en TablePlus
- ⬜ Activar familias del piloto: `UPDATE auth.families SET subscription_status='trial', trial_ends_at=now()+interval '30 days' WHERE email='...'`

### Fase 2 — Producto
- ⬜ MercadoPago — suscripción mensual plana, un solo plan, webhook → actualiza `subscription_status`
- ⬜ Consentimiento parental (condición legal de lanzamiento)
- ⬜ Modo lectura del padre en `/Classroom/{id}`
- ⬜ Avatares personalizables tutor y alumno (galería fija de emojis, sin upload — post-piloto)
- ⬜ Resumen cualitativo automático por sesión (killer feature dashboard)
- ⬜ Jerarquía Materia → Tema → Secciones (hoy en Classroom, migrable en V2)

---

## Decisión de arquitectura — migraciones de DB

> Flujo correcto: TablePlus → SQL manual → insertar en `__EFMigrationsHistory` → clase vacía en código.
> **IMPORTANTE:** toda propiedad nueva en una entidad necesita `HasColumnName` en `AppDbContext`
> o Railway falla al arrancar con error "column X does not exist".

## Decisión de arquitectura — modelo de cobro

> Cobro **plano mensual** (NO créditos). Activación manual durante el piloto desde TablePlus.
> MercadoPago Suscripciones es el target para V2.

## Próximos pasos (Sesión 15) — la carrera para el piloto, requiere cerebro dedicado
1. Verificar que los SQLs pendientes están aplicados en Railway
2. Invitar las primeras familias del piloto
3. MercadoPago — suscripción plana, un solo plan
4. Consentimiento parental
5. Higiene: rotar la API key (estuvo en texto plano en el harness) + `git remote set-url`
   (GitHub renombró el repo a `Haalvarez/mitutoria`)

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

> `public.error_logs` — crear manualmente, no tiene migración EF.

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

---

## Estructura del repo
```
mitutoria/
├── miTutoria.Web/
│   ├── Data/
│   │   ├── AppDbContext.cs             ← HasColumnName para todas las props nuevas
│   │   ├── Entities/
│   │   │   ├── Auth/Family.cs          ← + CreatedAt, SubscriptionStatus, TrialEndsAt, PaidUntil, IsAccessAllowed
│   │   │   ├── Auth/User.cs
│   │   │   ├── Academic/Classroom.cs   ← + MaterialSections, MaterialSectionIndex, MaterialOcrSource
│   │   │   ├── Academic/Message.cs
│   │   │   ├── Billing/TokenEvent.cs
│   │   │   └── ErrorLog.cs             ← Nuevo: public.error_logs
│   │   └── Migrations/
│   ├── Infrastructure/
│   │   ├── ErrorLogService.cs          ← Nuevo
│   │   ├── ExchangeRateService.cs
│   │   └── TelegramService.cs
│   ├── Pages/
│   │   ├── Index.cshtml                ← Landing
│   │   ├── Login.cshtml
│   │   ├── Blocked.cshtml              ← trial_expired / suspended / cancelled
│   │   ├── Auth/Verify.cshtml          ← + guard subscription_status
│   │   ├── Entrar.cshtml               ← + guard subscription_status
│   │   ├── Dashboard/Index.cshtml
│   │   ├── Students/Edit.cshtml
│   │   ├── Classroom/Index.cshtml      ← PDF drag&drop, mensajes animados, secciones, nav contextual
│   │   ├── Admin/Index.cshtml          ← + monitor piloto + error log
│   │   └── Shared/_Layout.cshtml       ← nav condicional classroom vs landing, logo JPG
│   ├── wwwroot/
│   │   ├── css/site.css                ← + drop-overlay, chat-attach-btn, upload-thinking, classroom-nav, tutor-thinking
│   │   └── img/logo.jpg                ← Logo real
│   └── Program.cs
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

---

## Backlog — ideas anotadas
- [ ] Avatares personalizables tutor y alumno — galería fija emojis, sin upload, post-piloto
- [ ] Logo en SVG (usar vectorizer.io con el JPG actual)
- [ ] TTS y reconocimiento de voz (TDAH)
- [ ] Materias → Temas → Secciones (jerarquía completa, V2)
- [ ] Agenda con fechas de examen
- [ ] Resumen de sesión para el padre
- [ ] PWA — instalar como app desde el celular
- [ ] Ambiente staging
- [ ] Consentimiento parental explícito
- [ ] Modo lectura padre en el aula
- [ ] Alertas de inactividad en dashboard padre

---

*Actualizado al cierre de Sesión 14*
