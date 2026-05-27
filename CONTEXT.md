# CONTEXT.md — miTutorIA
> Pegar este archivo al inicio de cada sesión con Claude.
> Actualizar al cierre de cada sesión.

---

## 🎯 Objetivo Último (OÚ)
Lanzar una plataforma educativa multi-tenant con IA controlada por padres,
monetizable, construida sobre ASP.NET Core 8 + Railway.
**Criterio de éxito de Fase 1:** Vika, Dasha y Egor pueden loguearse
y chatear en su aula desde mitutoria.app.

---

## Estado actual
- **Sesión:** 3
- **Fase activa:** Fase 1 — MVP Familia
- **Branch activo:** `main`
- **Último commit:** chore: update project.md post-merge to main

## Funciona hoy
- ✅ mitutoria.app live en Railway
- ✅ Deploy automático: push → Railway en ~2 min
- ✅ Landing rediseñada publicada en producción
- ✅ `feat/design-identity-v1` mergeada a `develop` y `main`
- ✅ .gitignore, global.json, railway.json, nixpacks.toml configurados

## No funciona / pendiente
- ⬜ Verificar render final en mitutoria.app (Chrome Extension)
- ⬜ Base de datos PostgreSQL
- ⬜ Auth / Login
- ⬜ Dashboard padre
- ⬜ Aula estudiante
- ⬜ Integración Anthropic API

---

## Próximos 3 pasos (Fase 1)
1. Verificar render en mitutoria.app
2. Mergear `feature/auth-db` y `feature/version-footer` a `develop`
3. MVP core: prompt maestro v1

---

## Decisiones técnicas tomadas
| Decisión | Motivo |
|---|---|
| No cambiar ContentRoot después de `WebApplication.CreateBuilder(args)` | En .NET 8 eso lanza `NotSupportedException`; se usa el content root por defecto |
| Sin UseHttpsRedirection en Production | Railway maneja SSL en su proxy |
| Puerto 8080 en Railway Networking | App bindea a 8080 por defecto |
| global.json rollForward: latestMajor | SDK 8.0.0 exacto no disponible localmente |
| cd out && dotnet miTutoria.Web.dll | wwwroot debe estar en working directory |

---

## Estructura del repo
```
mitutoria/
├── miTutoria.sln
├── miTutoria.Web/
│   ├── Pages/
│   │   ├── Index.cshtml
│   │   └── Shared/_Layout.cshtml
│   ├── wwwroot/
│   │   ├── css/site.css
│   │   └── js/site.js
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
3. Claude in Chrome → verificar comportamiento en vivo
4. Un prompt = un commit con prefijo feat/fix/chore/style/docs
5. Ramas: feature/xxx → develop → main (nunca directo a main)
6. Al cerrar sesión → actualizar CONTEXT.md + CHANGES.md
```

---

## Ramas
| Rama | Propósito |
|---|---|
| `main` | Production → Railway auto-deploy |
| `develop` | Integración activa |
| `feature/auth-db` | Próxima rama activa |

---

## Costos actuales
| Ítem | Costo |
|---|---|
| mitutoria.app (Porkbun) | $10.81/año |
| Railway Hobby | ~$5/mes |
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
- [ ] Claude in Chrome para verificar comportamiento en vivo
- [ ] BYOK (bring your own API key) para familias avanzadas
- [ ] Marketplace de aulas / plantillas por materia
- [ ] Idioma portugués para mercado Brasil

---

*Actualizado al cierre de Sesión 3*
