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
| Estudiantes | Acceso a IA pedagogicamente guiada |
| Familias con NEE | Perfiles adaptados (TDAH, etc.) |

**Diferencial:** No es ChatGPT en un iframe.
Es un sistema donde el padre es el arquitecto del aprendizaje.

---

## Stack técnico

```
Frontend        ASP.NET Core 8 — Razor Pages
Hosting         Railway (ya configurado)
Dominio         mitutoria.app (Porkbun)
Base de datos   PostgreSQL (Railway — fase 2)
Auth            ASP.NET Identity (fase 2)
IA              Anthropic API — claude-sonnet-4 (fase 3)
Pagos           Stripe (fase 4)
Repo            github.com/haalvarez/mitutoria
```

---

## Arquitectura de entidades

```
Familia (Tenant)
├── id, nombre, plan, stripe_customer_id
├── Usuarios
│   ├── Padre (rol: admin)
│   │   ├── Configura aulas
│   │   ├── Ve historial
│   │   └── Controla saldo/límite
│   └── Estudiante (rol: student)
│       ├── Perfil (edad, grado, NEE)
│       └── Accede a sus aulas
└── Aulas
    ├── id, nombre, materia
    ├── system_prompt (el corazón del producto)
    ├── nivel_restriccion (guía / mixto / estricto)
    └── Sesiones
        ├── mensajes[]
        └── tokens_usados
```

---

## Fases de desarrollo

### ✅ Fase 0 — Infraestructura (COMPLETO)
- [x] Dominio registrado: mitutoria.app
- [x] Repo GitHub: haalvarez/mitutoria
- [x] Deploy automático en Railway
- [x] Landing page publicada
- [x] Pipeline: push → deploy en 2 min

---

### 🔲 Fase 1 — MVP Familia (próxima sesión)
**Objetivo:** Una familia real (la tuya) puede usar la plataforma.

- [ ] Base de datos PostgreSQL en Railway
- [ ] Modelos: Familia, Usuario, Aula, Sesion
- [ ] Entity Framework Core + migraciones
- [ ] Auth básico: registro y login (ASP.NET Identity)
- [ ] Dashboard del padre
  - [ ] Ver / crear hijos
  - [ ] Crear / editar aulas
- [ ] Aula del estudiante
  - [ ] Chat básico (sin IA todavía — respuesta hardcodeada)
- [ ] Beta testers: tus 3 hijos

**Criterio de éxito:** Vika, Dasha y Egor pueden loguearse
y chatear en su aula.

---

### 🔲 Fase 2 — IA Real
**Objetivo:** El tutor responde con inteligencia.

- [ ] Integración Anthropic API (claude-sonnet-4)
- [ ] System prompt por aula (configurable por padre)
- [ ] Detección de intención (pedir respuesta vs pedir ayuda)
- [ ] Perfiles adaptados por hijo (TDAH, edad, grado)
- [ ] Historial de sesiones visible para padres
- [ ] Rate limiting por familia (control de costo)

**Criterio de éxito:** El tutor detecta "resolveme el ejercicio"
y responde con una pregunta en vez de la solución.

---

### 🔲 Fase 3 — Beta Cerrada
**Objetivo:** 10-20 familias externas probando.

- [ ] Multi-tenant real (aislamiento por familia)
- [ ] Invitación por código
- [ ] Sistema de límites de uso por plan
- [ ] Dashboard de métricas para padres
- [ ] Feedback loop con beta testers
- [ ] Landing actualizada con testimonios reales

**Beta testers primarios:** Chat de mamis
**Criterio de éxito:** 5 familias usan la plataforma
al menos 3 veces por semana.

---

### 🔲 Fase 4 — Monetización
**Objetivo:** Primeros ingresos reales.

- [ ] Stripe integrado
- [ ] Planes:
  - Free: 1 hijo, 50 mensajes/mes
  - Básico ($9/mes): 3 hijos, 300 mensajes/mes
  - Familia ($19/mes): hijos ilimitados, sin límite
- [ ] Upgrade flow en la app
- [ ] Emails transaccionales (bienvenida, límite cerca)

---

### 🔲 Fase 5 — Escala
**Objetivo:** Crecer fuera del círculo cercano.

- [ ] BYOK: familias con su propia API key
- [ ] Marketplace de aulas (plantillas por materia)
- [ ] Métricas de aprendizaje para padres
- [ ] Idioma: portugués (Brasil como segundo mercado)
- [ ] Referral program para el chat de mamis

---

## Workflow de desarrollo

```
Claude (este chat)              Visual Studio + Copilot
─────────────────────           ──────────────────────
Arquitectura y diseño     →     Ejecutar prompts
Generar prompts Copilot   →     Leer archivos reales
Iterar UI/UX              →     Commit por cada prompt
Revisar outputs           →     Push → Railway auto-deploy
```

**Ritmo:** 2 horas semanales · 1 sesión = 1 fase avanzada

---

## Reglas del proyecto

1. **Un prompt = un commit** — nunca acumular cambios sin commitear
2. **Claude no adivina** — siempre pide el estado real antes de generar
3. **Railway es la verdad** — si anda en Railway, anda
4. **Sin deuda técnica** — si algo duele ahora, se resuelve ahora
5. **Los beta testers mandan** — lo que digan Vika, Dasha y Egor importa más que cualquier teoría

---

## Costos actuales

| Ítem | Costo |
|---|---|
| Dominio mitutoria.app | $10.81/año (Porkbun) |
| Railway Hobby | $5/mes |
| Anthropic API | $0 (fase 3) |
| **Total actual** | **~$6/mes** |

---

## Contacto del proyecto

- Repo: github.com/haalvarez/mitutoria
- Live: https://mitutoria.app
- Stack decisions: este chat (guardar conversación)

---

*Última actualización: Sesión 1 — Infraestructura completa*
*Próxima sesión: Fase 1 — Base de datos + Auth + Dashboard padre*
