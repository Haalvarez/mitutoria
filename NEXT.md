# miTutorIA — Próximos pasos (roadmap corto)

> Lista operativa y accionable. El documento estratégico vive en [ROADMAP.md](ROADMAP.md).
> Orden = prioridad. Actualizado: 2026-08-03.

---

## 🔴 Ahora (esta semana)

### 1. Trials viejos: hoy tienen acceso indefinido — decidir qué hacer
**Estado real:** el acceso se decide solo por `SubscriptionStatus` (`"trial"`/`"active"`) en
`Family.IsAccessAllowed` — **no mira la fecha `TrialEndsAt`**. No hay ningún job que pase
`"trial"` → `"expired"` al vencer. Resultado: **todo trial viejo sigue con acceso completo.**

**Enfoque acordado:** el chico es el *cartero* (entra al Classroom a diario; el padre casi nunca
entra al Dashboard). Avisos suaves que empujan al padre al Dashboard. Corte real recién con MP en prod.

- [x] **Banner "cartero" en el Classroom** (aviso al chico para que transmita al padre). No corta.
      Estados: `expiring` (≤3 días) y `expired`. Dismissible (localStorage).
- [x] **Banner informativo en el Dashboard** (padre): "vence en X días / venció" + botón "Activar mi acceso"
      (visible solo si `MpEnabled`). No corta.
- [ ] **Ver quiénes son.** Query: familias con `SubscriptionStatus == "trial"` y `TrialEndsAt < hoy`.
      (Idealmente mostrarlo en el panel Admin como "trials vencidos".)
- [ ] **Con MP en prod — corte real:** chequeo de fecha en `IsAccessAllowed` **o** job que marque `"expired"`,
      con período de gracia y **perdonando a los pilotos actuales** (grandfather).
- [ ] **Mail/WhatsApp de vencimiento al padre** (canal directo, complementa al cartero). Ya hay Resend + teléfono.

### 2. Anti-bot en waitlist — HECHO, falta verificar en prod
- [x] Honeypot (`website`) + time-check (<2.5s) en el form de la landing.
- [ ] Confirmar tras deploy que no vuelven a entrar inscripciones basura.

### 3. SEO base — HECHO, falta post-deploy
- [x] Meta description + Open Graph + Twitter cards + JSON-LD (SoftwareApplication + FAQ).
- [x] `robots.txt` + `sitemap.xml`.
- [ ] **Post-deploy:** dar de alta el sitio en Google Search Console y enviar el sitemap.
- [ ] (Nice-to-have) imagen OG dedicada 1200×630 en vez de reusar el logo cuadrado.

---

## 🟡 Pronto (este mes)

### 4. Pasar MercadoPago a producción
- [ ] Cambiar `MP_ACCESS_TOKEN` al token de producción (`APP_USR-...`).
- [ ] Setear `APP_BASE_URL=https://mitutoria.app` y verificar que `/api/pay/webhook` responde por HTTPS desde afuera.
- [ ] Habilitar `PayEnabled` en las familias piloto.
- [ ] Confirmar `CUOTA_ARS`.
- [ ] Probar un pago real de punta a punta (con importe bajo o promo).

### 5. SEO — contenido para búsquedas laterales
- [ ] 1-2 páginas/artículos que respondan dudas reales de padres
      ("¿está bien que mi hijo use IA para la tarea?", "cómo evitar que copie con ChatGPT").
      Google (guía de IA) premia contenido útil con punto de vista propio, no trucos.

---

## 🟢 Después

- [ ] Validación del webhook de MP con firma HMAC (`x-signature`). Hoy no es crítico
      porque se re-consulta el pago a la API, pero es cinturón + tirantes.
- [ ] Revisar el resto del ROADMAP estratégico (V2: panel de padres, generadores, etc.).

---

## 🛠️ Notas de entorno (build)

- **El proyecto es net8.0.** El `global.json` fija el SDK en `8.0.400` con `rollForward: latestFeature`
  (versión válida — feature band, NO `8.0.0`, que es inválido y rompía la resolución del SDK).
- **Dos instalaciones de .NET en la máquina:** el SDK vive en el **x64** (`C:\Program Files\dotnet`,
  con SDKs 8.0.x/9.0.x/10-preview). El **x86** (`C:\Program Files (x86)\dotnet`) solo tiene runtimes,
  **ningún SDK**. El PATH de máquina se reordenó para que el x64 vaya primero → `dotnet` a secas ya
  resuelve el SDK. Si vuelve a fallar con "SDK no encontrado", chequear con `(Get-Command dotnet).Source`
  que apunte al x64.
- **Ojo:** los comandos `dotnet` corren el `global.json` **solo desde la raíz del repo**; fuera del repo
  agarra el SDK más nuevo (10-preview).
- **EF Core unificado en 8.0.11** (Design + Npgsql). No mezclar con 8.0.27 (daba warning MSB3277).
- **En la sesión de la CLI de Claude** el `dotnet` a secas resuelve al x86 (sin SDK); compilar con
  el x64 explícito: `"/c/Program Files/dotnet/dotnet.exe" build miTutoria.Web/miTutoria.Web.csproj`.
- Recordatorio: las **migraciones son SQL a mano** (idempotente), no usar `dotnet ef migrations add`.
