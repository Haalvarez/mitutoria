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
- **Sesión:** 2
- **Fase activa:** Fase 1 — MVP Familia
- **Branch activo:** por crear → `feature/auth-db`
- **Último commit:** fix: set content root + disable https redirect in production

## Funciona hoy
- ✅ mitutoria.app live en Railway
- ✅ Deploy automático: push → Railway en ~2 min
- ✅ Landing page publicada (mitutoria-v2.html → Index.cshtml)
- ✅ .gitignore, global.json, railway.json, nixpacks.toml configurados

## No funciona / pendiente
- ⬜ CSS/JS desde wwwroot (404 pendiente de verificar post último fix)
- ⬜ Base de datos PostgreSQL
- ⬜ Auth / Login
- ⬜ Dashboard padre
- ⬜ Aula estudiante
- ⬜ Integración Anthropic API

---

## Próximos 3 pasos (Fase 1)
1. Verificar que CSS/JS cargan en mitutoria.app
2. Crear branch `feature/auth-db`
3. PostgreSQL en Railway + modelos EF Core

---

## Decisiones técnicas tomadas
| Decisión | Motivo |
|---|---|
| UseContentRoot(AppContext.BaseDirectory) | wwwroot no se encontraba en Railway |
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
| `develop` | Integración — pendiente de crear |
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

*Actualizado al cierre de Sesión 1*
