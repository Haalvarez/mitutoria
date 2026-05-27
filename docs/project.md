# miTutorIA

## Estado actual
Landing page rediseñada: copy alineado con frame de acompañamiento
(no control), referencias a TDAH removidas del copy público, numeración
decorativa eliminada de features, iconos Tabler reemplazando emojis.
Base visual (paleta, tipografía, grain, cursor) conservada intacta.

## Rama activa
feat/design-identity-v1 — base: main

## Arquitectura / decisiones clave
- ASP.NET Core 8 Razor Pages, Railway hosting
- Layout mínimo: nav fija + footer + cursor custom
- Design system: --paper/--ink/--rust/--sage/--dust/--warm
- Tabler icons via CDN jsdelivr (webfont v3.19.0)

## Pendiente
- [ ] Verificar render de Tabler icons en prod (Railway)
- [ ] Revisar hero-watermark "pensar" en mobile (20vw puede quedar grande)
- [ ] MVP core: prompt maestro v1, loop importar→chat, token_events
- [ ] Auth mínima: magic link, cuenta = familia
- [ ] Schema DB: accounts, students, sessions, messages, token_events, plans

## Contexto para próxima sesión Claude
El rediseño visual es intencional y conservador — no ampliar sin
discutir primero. El siguiente paso de producto es el prompt maestro v1,
no más cambios de landing.
