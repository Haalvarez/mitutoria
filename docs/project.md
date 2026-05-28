# miTutorIA

## Estado actual
Landing rediseñada deployada en producción (Railway).
feat/design-identity-v1 mergeada a develop y main.

## Rama activa
main — en producción

## Arquitectura / decisiones clave
- ASP.NET Core 8 Razor Pages, Railway hosting
- Layout mínimo: nav fija + footer + cursor custom
- Design system: --paper/--ink/--rust/--sage/--dust/--warm
- Tabler icons via CDN jsdelivr (webfont v3.19.0)

## Pendiente
- [ ] Verificar render en mitutoria.app (Chrome Extension)
- [ ] Pendiente merge de feature/auth-db y feature/version-footer a develop
- [ ] MVP core: prompt maestro v1
- [ ] Auth mínima: magic link, cuenta = familia
- [ ] Schema DB: accounts, students, sessions, messages, token_events, plans

## Contexto para próxima sesión Claude
El rediseño visual es intencional y conservador — no ampliar sin
discutir primero. El siguiente paso de producto es el prompt maestro v1,
no más cambios de landing.
