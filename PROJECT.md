# miTutorIA — Project Master

> Plataforma educativa con IA controlada por padres.
> Dominio: mitutoria.app · Stack: ASP.NET Core 8 · Railway · PostgreSQL

---

## La idea en una oración

Un tutor digital con IA que los padres configuran —
que guía a los hijos a pensar, no a copiar.

---

## Propuesta de valor

| Para quién | Qué resuelve |
|---|---|
| Padres | Control real sobre cómo usa la IA su hijo |
| Estudiantes | Acceso a IA pedagógicamente guiada |
| Familias con NEE | Perfiles adaptados (TDAH, etc.) |

**Diferencial:** No es ChatGPT en un iframe.
Es un sistema donde el padre es el arquitecto del aprendizaje.

---

## Stack técnico

```
Frontend        ASP.NET Core 8 — Razor Pages
Hosting         Railway (mitutoria.app prod + staging.mitutoria.app)
Dominio         mitutoria.app (Porkbun)
Base de datos   PostgreSQL en Railway
Auth            Magic link (Resend) + cookie HttpOnly 7 días
IA              Anthropic API — Haiku 4.5 (prompt caching activo)
Pagos           MercadoPago (postergado, ver Fase 3)
Inbound mail    Postmark (Track 2 — feature exploratoria)
Repo            github.com/haalvarez/mitutoria
```

---

## Arquitectura de entidades (estado real)

```
Family (Tenant)
├── id, email, nickname, parent_role
├── subscription_status (trial / active / trial_expired / suspended / cancelled / waitlist)
├── trial_ends_at, paid_until, created_at
├── magic_token, magic_token_expiry
├── Users (hijos)
│   ├── id, family_id, nickname, gender, school_level, grade
│   ├── has_adhd, explanation_level, pref_*
│   └── login_username, login_pin
└── Classrooms (cuadernos de materia, una por hijo por materia)
    ├── id, student_id, name, mode (resolution/comprehension), last_active_at
    ├── material, material_sections (jsonb), material_section_index, material_ocr_source
    ├── compact_summary
    └── Messages
        └── role, content, created_at

billing.token_events    — registro append-only de cada llamada a Claude
public.error_logs       — errores internos para /admin
auth.waitlist_entries   — lista de espera del landing
```

---

## Fases de desarrollo

### ✅ Fase 0 — Infraestructura
- [x] Dominio mitutoria.app
- [x] Repo GitHub, deploy automático push → Railway
- [x] Landing page con demo en vivo y lista de espera

### ✅ Fase 1 — MVP Familia (COMPLETO al cierre de Sesión 15)
**Criterio:** Vika, Dasha y Egor pueden loguearse y chatear en su aula. ✅

- [x] PostgreSQL en Railway con esquemas auth, academic, billing, public
- [x] Magic link (Resend) + cookie HttpOnly 7 días
- [x] Dashboard padre con gráfico de consumo por hijo
- [x] Perfil padre y alta/edición de hijos
- [x] Aula `/Classroom/{id}` — chat AJAX, sidebar, full-height
- [x] Integración Anthropic API (Haiku 4.5) con prompt caching
- [x] Prompt maestro v1 endurecido (Sesión 14, harness 12/12)
- [x] token_events + límite mensual configurable
- [x] Demo público `/api/demo` (10 mensajes sin login)
- [x] Login alumno `/Entrar` con usuario + PIN
- [x] Guard de acceso por `subscription_status` → `/Blocked`
- [x] Racha de días en aula y dashboard
- [x] Botonera del aula: Quiz, Tarjetas, Examen de práctica
- [x] PDF upload integrado al chat + drag&drop + OCR fallback
- [x] Secciones de material persistentes
- [x] Mochila — materias por alumno (Sesión 15)
- [x] Admin `/admin` con monitor de piloto y error log
- [x] Tipo de cambio MEP en tiempo real
- [x] Alertas Telegram (waitlist)
- [x] Logo + favicon

### 🟡 Fase 2 — Piloto cerrado (Sesión 16+, EN PROGRESO)
**Criterio:** 5-10 familias conocidas usando la plataforma con valor sostenido.

- [ ] Verificar SQLs pendientes aplicados en Railway (`AddMaterialSections`, `AddFamilyBilling`, `error_logs`)
- [ ] Ambiente de staging en Railway (`staging.mitutoria.app`)
- [ ] Página de consentimiento parental mínima en alta (checkbox + texto + persistencia con fecha)
- [ ] Activar familias del piloto vía TablePlus (`subscription_status='trial'`)
- [ ] Invitar 5-10 familias conocidas
- [ ] Medición de uso real (token_events) durante 2-3 semanas
- [ ] **Cobro / fiscalidad: PARQUEADO hasta validar retención** — ver Backlog

### 🟡 Fase 3 — Cobro y régimen formal (parqueado, ~2 meses)
**Objetivo:** Pasar del piloto cerrado a cobro recurrente. Roadmap específico al activarse.

- [ ] Decisión fiscal: reinscripción monotributo categoría A
- [ ] Cuenta MP vendedor con CUIT regularizado
- [ ] MercadoPago — cobro por QR/link con `external_reference` por familia
- [ ] Webhooks MP → actualizar `subscription_status`
- [ ] Migración eventual a `preapproval` (suscripción recurrente) si volumen lo justifica
- [ ] Estructura de plan único + promo de conversión para familias del piloto

### 🔵 Fase 4 — Inbox / captura de Classroom (Track 2 — EXPLORATORIA)
**Objetivo:** Capturar avisos de Classroom vía forwarding de Gmail, sin OAuth.
**Condición de avance:** validación con 3 padres externos antes de invertir más allá de Sprint 1.

- [ ] Sprint 1: setup Postmark inbound + parser + persistencia (rama `feature/inbox-pipeline` a `develop`)
- [ ] Validación con 3 padres externos (mockup del mail diario)
- [ ] Sprint 2: onboarding del forward + vista "Mis tareas"
- [ ] Sprint 3: notificaciones (mail al padre, resumen diario, alertas de urgencia)
- [ ] Sprint 4: dashboard del padre + manejo de errores + multi-hijo

Vika como usuaria beta en staging durante todo el track.

### 🔵 Fase 5 — Beta cerrada externa
**Objetivo:** 10-20 familias fuera del círculo conocido.

- [ ] Onboarding self-service real (sin activación manual en TablePlus)
- [ ] Modo lectura del padre en `/Classroom/{id}`
- [ ] Resumen cualitativo automático por sesión
- [ ] Feedback loop estructurado
- [ ] Landing actualizada con testimonios reales

### 🔵 Fase 6 — Escala
**Objetivo:** Crecer fuera del boca-a-boca.

- [ ] Avatares personalizables (galería emojis)
- [ ] PWA — instalar desde celular
- [ ] BYOK para familias con su propia API key
- [ ] Idioma: portugués (Brasil)
- [ ] Referral program
- [ ] Integración con calendarios escolares (B2B2C)

---

## Tracks paralelos — disciplina de foco

> Track 1 tiene prioridad. Track 2 no puede mergear a `main` hasta que Track 1 tenga datos del piloto.

| Track | Rama | Estado | Bloquea a |
|---|---|---|---|
| **1 — Piloto** | `main` | Activo, prioridad alta | Track 2 merge a main |
| **2 — Inbox** | `feature/inbox-pipeline` → `develop` → staging | Activo en paralelo, sin merge a main | — |
| **3 — Cobro** | (no abierto) | Parqueado ~2 meses | — |

---

## Workflow de desarrollo

```
Claude (este chat)              Visual Studio + Copilot
─────────────────────           ──────────────────────
Arquitectura y diseño     →     Ejecutar prompts
Generar prompts Copilot   →     Leer archivos reales
Iterar UI/UX              →     Commitear (espíritu: cambio coherente = commit)
Revisar outputs           →     CHANGES.md actualizado en cada commit
                                Push → Railway auto-deploy
```

**Ritmo:** sesiones cuando hay foco — el Track 1 está cerca de cierre, el Track 2 es exploratorio.

---

## Ambientes

| Ambiente | Branch | URL | Uso |
|---|---|---|---|
| Producción | `main` | mitutoria.app | Familias reales del piloto |
| Staging | `develop` | staging.mitutoria.app (a configurar) | Pruebas pre-merge, Track 2, Vika beta |
| Local | `feature/*` | localhost:5000 | Desarrollo |

---

## Reglas del proyecto

1. **Un cambio coherente = un commit** — no acumular cambios dispersos sin commitear
2. **Claude no adivina** — siempre pide el estado real (archivos, DB) antes de generar
3. **Railway es la verdad** — si anda en Railway prod, anda
4. **Sin deuda técnica** — si algo duele ahora, se resuelve ahora
5. **Los beta testers mandan** — lo que digan Vika, Dasha y Egor importa más que cualquier teoría
6. **Track 1 antes que Track 2** — el piloto no se posterga por features exploratorias

---

## Costos actuales

| Ítem | Costo |
|---|---|
| Dominio mitutoria.app | ~$11/año (Porkbun) |
| Railway (prod + staging cuando entre) | ~$5-10/mes |
| Anthropic API | variable (cubierto con prompt caching + límite mensual) |
| Postmark (Track 2) | ~$10/mes cuando se active |
| Resend (magic links) | free tier alcanza |
| **Total actual** | **~$15/mes** |

---

## Backlog — ideas anotadas
- [ ] **Cobro y régimen fiscal** — retomar en ~2 meses, roadmap específico
- [ ] Avatares personalizables tutor y alumno (galería emojis, post-piloto)
- [ ] Logo en SVG (vectorizar el JPG actual)
- [ ] TTS y reconocimiento de voz (TDAH)
- [ ] Materias → Temas → Secciones (jerarquía completa, V2)
- [ ] Agenda con fechas de examen
- [ ] Resumen cualitativo automático por sesión (killer feature dashboard)
- [ ] PWA — instalar desde celular
- [ ] Modo lectura padre en el aula
- [ ] Alertas de inactividad en dashboard padre
- [ ] Mochila — borrar/renombrar materia, ícono y orden manual
- [ ] Calibración fina del modo Comprensión

---

## Contacto del proyecto

- Repo: github.com/haalvarez/mitutoria
- Live: https://mitutoria.app
- Stack decisions: CONTEXT.md (estado vivo) + ROADMAP.md (estrategia)

---

*Última actualización: Sesión 16 — replanificación dual-track (piloto + inbox exploratorio)*
