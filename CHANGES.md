## [Sesión 11] — 2026-05-31

### feat
- PDF upload vía AJAX — sin reload, muestra "Subiendo y leyendo…", feedback en sidebar
  - Fallback OCR: si iText7 no extrae texto (PDF escaneado/imagen), reintenta con Claude Vision (base64)
  - Si Claude tampoco puede leer el PDF, muestra error claro al usuario
  - `[RequestSizeLimit]` y `[RequestFormLimits]` a 21MB a nivel de clase
- Prompt caching: system prompt marcado con `cache_control: ephemeral` en todos los calls a Claude
  - Ahorro ~90% en tokens del material durante sesión activa (cache dura 5 min, se renueva con cada hit)
- Tarjetas (flashcards) en modal HTML interactivo
  - Claude devuelve JSON, JS renderiza modal con nav prev/next y flip frente/dorso
  - Historial guarda placeholder "📇 Tarjetas generadas" en lugar del JSON crudo
- Simulacro de examen: reemplaza el "Modo Examen" toggle
  - 6 preguntas a/b/c/d generadas por Claude desde el material, en modal
  - Feedback inmediato verde/rojo al responder, muestra la correcta si falla
  - Puntaje final con mensaje según resultado (≥70% / ≥50% / <50%)
  - Si el alumno sube un PDF de examen real, el simulacro replica ese formato
- Admin panel `/admin?token=ADMIN_TOKEN`
  - Cards globales: familias, costo USD/ARS del mes, total waitlist
  - Tokens por feature del mes (chat, quiz, flashcards, exam, material_ack, etc.)
  - Señales de riesgo: cerca del límite / sin actividad 7+ días / alumnos sin material
  - Tabla de familias: intercambios hoy/semana/mes, costo USD/ARS, último uso
  - Tabla de waitlist completa con fecha
- Alertas Telegram: notificación al anotarse en la waitlist (nombre, email, total)
  - `TelegramService` ignora silenciosamente si las env vars no están configuradas
- Landing: nav con 3 botones persistentes (Soy estudiante / Acceso familiar / Probarlo en vivo)
- Landing: hero-bottom rediseñado en 2 columnas — texto contextual + copy de posicionamiento
  - Botones de acción movidos al nav, hero queda limpio

### fix
- PDF: material no se pisa si el textarea de texto se envía vacío (solo `clearMaterial=true` borra)
- PDF: mensaje del tutor siempre aparece al cargar — fallback si el ack de Claude falla
- PDF: error claro si iText7 extrae texto vacío en lugar de guardar string vacío silenciosamente
- Prompt: anti-bucle de confirmación — si el alumno solo valida lo sabido, tutor avanza directo
- Aula: `confirm()` del botón Nueva Sesión eliminado
- Admin: `Context` → `HttpContext` en la vista Razor

### seguridad (análisis)
- Acceso al aula sin login confirmado NO es bug: `ResolveStudentAsync` verifica `FamilyId` en sesión
- Username de alumno único globalmente (cross-family) — validado antes de guardar
- PIN no requiere unicidad por diseño — el credencial es `usuario + PIN`
- Punto ciego identificado: el padre entra al aula en modo interactivo (debería ser lectura)
  → pendiente: modo lectura para padre en `/Classroom/{id}`

### env vars nuevas (Railway)
| Variable | Uso |
|---|---|
| `ADMIN_TOKEN` | Protege `/admin` — string largo elegido por el operador |
| `TELEGRAM_BOT_TOKEN` | Token del bot de Telegram (BotFather) |
| `TELEGRAM_CHAT_ID` | Chat ID del operador para recibir alertas |

---

## [Sesión 10] — 2026-05-31 (continuación)
### feat
- Classroom: botonera Quiz / Tarjetas / Modo Examen sobre el input de chat
  - Quiz: genera 5 preguntas de opción múltiple basadas en el material (sin respuestas visibles)
  - Tarjetas: genera 8 flashcards FRENTE/DORSO del material
  - Modo Examen: toggle AJAX que cambia el system prompt — solo preguntas, sin pistas ni confirmaciones
- Classroom: racha de días seguidos (🔥 N) en el sidebar, calculada desde token_events
- Dashboard: rediseño completo — student cards con métricas de actividad (hoy/semana/mes)
  - Reemplaza tokens/USD por intercambios y gasto en ARS
  - Badge 🔥 racha por hijo (≥2 días) o "Estudió hoy" si streak=1
  - Gráfico cambia de tokens a intercambios por día
  - Texto empático: "Así va el aprendizaje de tu familia este mes"
- Dashboard: tasa de cambio MEP en tiempo real desde dolarapi.com (cache 60 min, fallback 1400)
  - `ArsRate` guardada en cada `token_events` al momento de la transacción
  - Gasto histórico en ARS siempre correcto aunque cambie el tipo de cambio
- Students: botón "¿Cómo va a actuar el tutor?" en `/Students/Edit/{id}`
  - Panel desplegable en lenguaje para el padre: cómo habla, cómo acompaña, TDAH, reglas invariables
  - Sin llamada a Claude — generado desde la config guardada, instantáneo

### fix
- Classroom: cerrado jailbreak de identidad — tutor no cambia comportamiento si alguien declara ser adulto
- Classroom: botón "← Dashboard" removido del aula (dominio del padre, no del alumno)
- Classroom: sección "Configurar tutor" removida del sidebar del aula (queda en /Students/Edit)
- Classroom: botón "Compactar" removido del UI visible (sigue disponible como lógica interna)

### material y tutor
- Tutor reconoce el material al cargarlo — mensaje automático al chat (feature `material_ack`)
- Tutor nunca dice "no veo archivos" — instrucción explícita en el prompt cuando hay material
- Saludo inicial contextual: con material → menciona el tema / sin material → pregunta en qué trabajar
- PDF auto-submit al seleccionar archivo — sin botón Guardar para PDF, Guardar solo para texto pegado
- Límite PDF aumentado de 5MB a 20MB (hardcodeado)
- Fix: toggle TDAH en edición del alumno ahora es switch prominente
- Fix: Nivel + Año en una fila en edición del alumno
- Fix: etiquetas del formulario más legibles (minúsculas, mayor contraste)
- Fix: mensaje de confirmación con instrucciones de acceso después de configurar usuario/PIN

### auth alumno
- `/Entrar`: nueva página de login para alumnos — usuario + PIN numérico
- `/Students/Edit`: el padre configura usuario y PIN desde la edición del hijo
  - Validación: usuario único en todo el sistema, PIN mínimo 4 dígitos
  - Muestra estado "✅ puede entrar con usuario X" cuando ya tiene acceso
- Migración `AddStudentUsername`: columna `StudentUsername` nullable + índice único en `auth.users`
- Aula: `ResolveStudentAsync` acepta sesión de alumno (`StudentId`) O sesión de padre (`FamilyId`)
  - Alumno solo accede a su propio aula
  - PIN hasheado con `PasswordHasher<User>` de ASP.NET Core (reutiliza columna `PasswordHash`)
- Landing: botón "Soy estudiante" → `/Entrar` + renombrado "Entrar" → "Acceso familiar"

### prompt
- Tono rioplatense bonaerense más específico: lista de palabras válidas + lista de regionalismos prohibidos (brete, órale, chévere)
- Ajuste de vocabulario por año escolar: 4 franjas (≤3 primaria / ≤6 primaria / ≤3 secundaria / ≥4 secundaria)
- Instrucción explícita de negativa al pedido de resumen del PDF: redirige al Quiz o Tarjetas
- Modo Examen: instrucción en el prompt — solo preguntas, sin confirmaciones ni pistas

---

## [Sesión 9] — 2026-05-30
### feat
- Classroom: aula `/Classroom/{studentId}` con chat completo — dos paneles, AJAX sin reload, typing indicator, burbujas con avatar
- Classroom: integración Anthropic API — Haiku 4.5, prompt socrático v1 con género y preferencias
- Classroom: PDF upload hasta 5MB con itext7, texto pegado, límite configurable `MAX_MATERIAL_CHARS`
- Classroom: compactación de historial — Claude resume, guarda en `CompactSummary`, borra mensajes
- Classroom: prompt personalizable por hijo desde sidebar del aula
- Students: `/Students/Edit/{id}` — género, 5 preferencias de aprendizaje, config TDAH (nivel explicación + 2 prefs)
- Dashboard: gráfico de barras Chart.js — consumo por hijo por día del mes
- Dashboard: cards de tokens totales y costo USD del mes
- Landing: demo en vivo con 5 mensajes sin login — 4 botones pre-cargados (honestos y tramposos)
- Landing: lista de espera cableada a DB (`auth.waitlist_entries`)
- token_events: registrado después de cada llamada (Feature=chat/compact), con CostUsd calculado al momento
- Límite mensual `MONTHLY_TOKEN_LIMIT` (default 500k tokens) — bloquea antes de llamar a Claude
- Modelo de créditos en ARS diseñado (pendiente implementar): créditos ARS, 50% margen, MercadoPago webhook

### fix
- Classroom: migración `MakeSubjectIdNullable` con raw SQL (DropFK antes de AlterColumn)
- Dashboard: `DateTimeKind.Utc` en queries Npgsql para timestamptz
- Dashboard: serialización JSON movida al PageModel (JsonSerializer no disponible en bloques Razor)
- Layout: CSRF token en meta tag para fetch() AJAX desde JS
- Footer: git hash removido del footer visible al usuario

### style
- Classroom: layout dos paneles — sidebar 280px + chat 100dvh
- Classroom: burbujas asimétricas (usuario derecha rust, tutor izquierda sage), avatar circular con inicial
- Classroom: input auto-resize, Enter envía, Shift+Enter nueva línea, botón SVG integrado
- Classroom: footer oculto en el aula, body class `classroom-page`
- Landing: sección demo sobre fondo oscuro con botones honestos/tramposos diferenciados por color

## [Sesiones 1-8] — feature/landing-base
### feat
- User: `Nickname` y `SchoolLevel` agregados al modelo
- Students: `/Students/Add` con nombre, apodo, nivel, año y TDAH
- Profile: `/Profile` para editar nombre, apodo y rol (Padre/Madre)
- Family: extendida con `Nickname` y `ParentRole` enum
- Dashboard: dashboard padre protegido por sesión
- Login: confirmación visual post-envío de magic link
### fix
- Profile: layout y estilos `.form-field`
- Dashboard: `FamilyName` muestra `Nickname ?? Name ?? Email`
### style
- CSS: `.page-main`, `.form-field`, `.dashboard-header`, `.students-list`
- Landing: lenguaje inclusivo — "mamá o papá"
