## [Sesión 17] — 2026-06-03

### fix — chat en texto plano de verdad + caras en las burbujas
- El tutor a veces filtraba markdown (`**Gerunds**`, `*enjoy*`). Doble arreglo:
  prompt reforzado (prohíbe explícitamente asteriscos/markdown) + limpieza en el
  render (`FormatMessage` server + `plainText` en JS) — garantía aunque el modelo resbale
- Las burbujas del chat ahora muestran la **cara del tutor** (TutorAvatar) y la **cara
  del alumno** (Avatar) en vez de "T" y la inicial; el label usa el nombre del tutor
- Cubre todas las burbujas: server, JS (addBubble), typing, upload, quiz, examen

### feat — ciclo de facturación por familia + scheduler de alertas + tope en USD
- **Ciclo anclado en la familia** (no calendario): ciclo = `[ancla - 1 mes, ancla)` con
  `ancla = PaidUntil ?? TrialEndsAt`. Pagó el 23 → vence el 23 (AddMonths, sin derivar)
- **Térmica en USD** (`TERMICA_USD`, default $15): el corte del aula ahora mide el costo del
  ciclo en USD, no tokens. Solo frena abuso real (familia normal = centavos). Reemplaza `TERMICA_TOKENS`
- **Scheduler `PilotMonitorService`** cada 6h: avisa a Telegram SOLO en positivo —
  (a) costo del ciclo supera `TERMICA_USD_ALERTA` (default $5), (b) trial/pago vence en ≤3 días.
  Dedup por ciclo con `cost_alert_marker` / `renewal_alert_marker` (no repite hasta cambiar el ciclo)
- Admin: "cerca de la térmica" ahora compara costo USD del ciclo vs `TERMICA_USD`
- Migración `20260603250000_AddPilotAlertMarkers` (idempotente)

### feat — avatares: cara del alumno + nombre y cara del tutor
- 3 campos en el alumno (`Avatar`, `TutorName`, `TutorAvatar`), galería fija de emojis, sin upload
- Picker de caras en `/Students/Edit` (alumno + tutor) + nombre del tutor
- La cara del alumno reemplaza la inicial en el dashboard
- El nav del aula muestra la cara + nombre del tutor; el tutor se auto-nombra en el prompt
- Migración `20260603240000_AddAvatars` (idempotente)

### feat — intereses del alumno → tutor que conecta (fidelización)
- Campo `Interests` en el perfil del alumno (fútbol, manga, etc.), editable en `/Students/Edit`
- El prompt del aula lo usa con mesura: "para conectar o dar un ejemplo cuando venga al pelo,
  sin forzarlo en cada respuesta ni desviar el tema"
- El mensaje del tutor al padre (Explain) también lo menciona
- Migración `20260603230000_AddInterestsToUser` (idempotente, corre sola en el arranque)

### feat — admin unificado, en USD, con detalle a demanda
- Merge de las 2 tablas de familias en **una sola, ordenable por columna** (click en header)
- Costo por familia en **USD billete** (últimos 30 días); se quitó ARS del admin
- **Modal ajax por familia** (`?handler=Detail`): costo y actividad por hijo + uso
  nominal por herramienta (chat/quiz/tarjetas/examen), que reemplaza el bloque global
  "Tokens por feature"
- **Scoreboard**: tarjetas KR1–KR4 + tira de contadores de riesgo (cerca de la térmica,
  sin consentir, inactivas 7d, sin material) → se ubican ordenando la lista
- Plata: Gasto mes (USD) + **Proyección fin de mes** + Histórico. Se eliminó el "saldo
  estimado" (Anthropic no expone saldo por API; con recarga automática el env var era falso)
- `MONTHLY_TOKEN_LIMIT` → renombrado **`TERMICA_TOKENS`** (default 5M, pilot-safe) y mensaje
  de corte más amable. El tope en USD por ciclo queda para el bloque del scheduler.

### feat — landing: features de utilidad como argumento de venta
- Nueva sección "No solo conversa. Lo pone a estudiar." entre "Cómo funciona" y la
  cita de tensión, con 4 tarjetas: Estudia su material · Tarjetas de repaso ·
  Simulacro de examen · Quiz rápido
- Encuadre honesto y deliberado: "Estudia su material" NO dice "no usa internet"
  (sería falso) — dice que se planta en la fotocopia/PDF de la clase y explica con
  los mismos ejemplos que vio el alumno, no con una respuesta genérica de manual
- Las tarjetas y simulacros se enmarcan como "estudio activo / ponerse a prueba",
  nunca como "te lo resolvemos" — refuerza el diferencial en vez de diluirlo
- Paso 02 del "Cómo funciona" ahora menciona subir el material de la clase
- El demo público sigue siendo solo texto (el socratismo se demuestra; la utilidad se cuenta)


### feat — Consentimiento parental mínimo (condición legal de lanzamiento)
- Nueva página `/Consentimiento` con texto honesto sobre qué datos se guardan y derechos Ley 25.326
- Checkbox de aceptación explícito; guard en `/Auth/Verify`: si `consent_at` es null → redirige al consentimiento antes del dashboard
- Persiste `consent_at` (UTC), `consent_ip` y `consent_version="v1"` en `auth.families`
- Familias que ya tienen sesión activa no son interrumpidas; el guard sólo aplica en el primer acceso post-magic-link

### feat — Invitación de familias desde la waitlist en /admin
- El botón "Invitar al trial" vive **por fila en la waitlist** (se quitó el formulario
  suelto de email): activa la familia como `trial` 30d, genera magic link 48hs y manda
  el mail de bienvenida vía Resend. Confirmación antes de enviar.
- Las filas ya invitadas muestran "✅ ya invitada" en vez del botón
- El mail de invitación tiene texto de bienvenida al piloto (distinto al magic link de login)
- Nota: ahora solo se invita a quien esté en la waitlist; para alguien externo, anotarlo
  primero en la waitlist (o TablePlus)

### fix — cerrar la puerta de alta y bugs de columnas
- `Login` ya no crea familias desconocidas: si el email no existe o está en `waitlist`,
  muestra mensaje. La única puerta de alta ahora es la invitación desde `/admin`
- `error_logs`: `HasColumnName` para todas las columnas (tabla creada a mano, minúscula) —
  resolvía el error "column e.Id does not exist" en `/admin`
- `OnPostInviteAsync` usa `FirstOrDefaultAsync` para no explotar con emails duplicados
- Nav autenticado: páginas internas muestran Inicio/Salir, no los links del landing
- Endpoint `/Salir` que limpia la sesión

### feat — retoques del monitor de admin
- Tooltips (ⓘ) en KR1–KR4 explicando qué mide cada uno y qué hacer en 🔴
- Leyenda del semáforo (🟢🟡🔴⚪) y aclaración de que Hoy/Semana/Mes = intercambios de chat
- Tooltips en señales de riesgo (qué es "cerca del límite", etc.)
- KR2 ahora agrupa la racha por familia (antes lista plana, era decorativa)
- Columna "Consentimiento" en el monitor de piloto (✅/⏳ pendiente)
- Tarjeta "Saldo API estimado": crédito manual (`ANTHROPIC_CREDIT_USD`) − consumo histórico

### feat — dashboard del padre orientado a enganche (no a costo)
- Sacada la tarjeta "Gasto del mes" en ARS: anclar precio en un piloto gratis
  asusta y no aporta. El costo sigue visible solo en `/admin`
- En su lugar: "Días activos este mes" y "Materiales cargados" (señales de uso)
- Mensaje de enganche arriba: celebra la mejor racha (≥3 días) o avisa quién
  estudió hoy — el "recibo de valor" para quien paga
- Barra de suscripción: estado (prueba/activa) + cuánto falta para el vencimiento
  (la misma lógica sirve para fin de trial y renovación mensual) + botón
  "Quiero pagar" deshabilitado (se habilita al activar el cobro; generará el link MP)
- Sparkline de actividad de los últimos 15 días por hijo en cada tarjeta (barras
  CSS, sin JS extra) — hace visible el hábito y ayuda a justificar el pago

### feat — layout más ancho en Dashboard y Editar perfil
- Variante `.page-main--wide` (920px) para páginas con contenido en columnas
- `Students/Edit`: las secciones del formulario fluyen en 2 columnas (`.edit-grid`)
  en pantallas anchas — menos scroll, mejor uso del ancho

### chore
- Migración `20260603210000_AddConsentToFamily` — aplicar en TablePlus antes de push:
  `ALTER TABLE auth.families ADD COLUMN IF NOT EXISTS consent_at timestamptz;`
  `ALTER TABLE auth.families ADD COLUMN IF NOT EXISTS consent_ip text;`
  `ALTER TABLE auth.families ADD COLUMN IF NOT EXISTS consent_version text;`
- Constraint pendiente en TablePlus: `ALTER TABLE auth.families ADD CONSTRAINT families_email_unique UNIQUE ("Email");`
- Env var opcional `ANTHROPIC_CREDIT_USD` para el saldo estimado de API

---

## [Sesión 15] — 2026-06-02

### feat — Mochila: materias por alumno (Classroom = cuaderno de materia)
- Un alumno puede tener varias materias; cada una es un Classroom con su nombre,
  modo pedagógico, historia de chat y material. "Mismo pupitre, distinto cuaderno":
  no cambia de ambiente, cambia el foco del tutor
- Selector de materia 🎒 en el sidebar (arriba del material) + "＋ Nueva materia"
- `GetActiveClassroomAsync`: resuelve el cuaderno activo (sesión → más reciente → crea "General")
- Handlers `CreateSubject` (infiere el modo del nombre) y `SwitchSubject`
- Fix: las 3 queries que hacían `SingleOrDefault(c.StudentId==studentId)` habrían
  reventado con varias materias — ahora resuelven el cuaderno activo

### feat — Modo pedagógico por tipo de materia
- `PedagogicalMode` (Resolución / Comprensión), inferido del nombre de la materia
- El prompt se ramifica: en Resolución (mate/física) no da el resultado; en Comprensión
  (historia/lengua) explica conceptos pero el alumno sintetiza — no le escribe el resumen
- La regla de resumen, antes hardcodeada en "no resumís", ahora es condicional al modo
- Foco de materia: si el alumno trae otra materia, el tutor sugiere cambiarla arriba (no pelea)
- Calibración fina del modo Comprensión = TODO post-piloto (anotado en el harness)

### chore
- Migración `20260603120000_AddSubjectMochila` (name, mode, last_active_at) — aplicada en TablePlus
- harness sincronizado con la estructura nueva + escenario "trae otra materia"

---

## [Sesión 14] — 2026-06-02

### fix — Prompt maestro (a partir de incidentes reales con una alumna)
- El tutor ya no menciona el diagnóstico: recibe comportamientos ("sé paciente,
  celebrá micro-logros"), nunca la etiqueta TDAH — la mención desmotivaba y distraía
- El tutor ya no usa insultos coloquiales ("boludo"); regla explícita de autoridad cercana
- Sacado `"qué fiaca"` de los ejemplos de vocabulario (modelaba desgano)

### feat — Modelo de tono: registro elástico, foco rígido
- Cuatro bloques nuevos en `BuildSystemPrompt`: Cómo respondés (brevedad, texto plano,
  no inventar) · Registro (espejá la energía del alumno, exigí esfuerzo no formalidad) ·
  Foco innegociable (el puente: un beat + gancho, nunca dos mensajes fuera de tema;
  contención sin hacer de psicólogo) · Tono y lenguaje
- Instrucciones del padre y resumen previo se aplican en silencio (nunca se revelan)

### feat — El padre ve cómo acompañará el tutor
- `/Students/Edit`: al guardar queda en la página y muestra automáticamente un mensaje
  cálido en prosa, redactado por Claude desde la configuración (1ª persona, sin etiquetas)
- Registrado como `token_event` Feature="explain"; fallback cálido si la API falla
- Tarjeta suave, sin borde-acento de alerta

### tooling — Backtest del prompt
- `tools/prompt-harness.ps1`: 12 escenarios (jailbreaks + derrape) con juez Haiku que
  puntúa leakage/insulto/etiqueta/regionalismo/drift. Corrida inicial: 12/12 verde
- Correr antes de mergear cambios del prompt (hoy el prompt vive duplicado en el script)

---

## [Sesión 13] — 2026-06-01

### feat — UX del aula
- PDF upload integrado al input del chat
  - Botón 📎 (paperclip) en el input bar dispara el selector de archivo
  - Drag & drop sobre el área del chat con overlay visual (borde punteado brand color)
  - Si se suelta un archivo que no es PDF → burbuja simpática de error
  - Sidebar: sin botón viejo de subida — solo muestra estado del material y apunte extra
- Mensajes animados durante la carga del PDF
  - Burbuja del tutor con nombre del archivo y mensajes rotativos cada 2.5s con fade
  - Mensajes: "Leyendo página por página...", "Identificando temas...", "Ya casi 🎯", etc.
  - Al terminar: burbuja se convierte en el ack del tutor
- Avatar del tutor animado mientras piensa
  - Durante el typing indicator: avatar pulsa de sage verde → terracota `#C1440E` con halo
  - CSS puro, 1.8s por ciclo, sin JS extra
- Nav contextual en el aula (reemplaza el nav de landing)
  - Logo → link al dashboard
  - Nombre del alumno al centro
  - Mensaje de racha a la derecha según streak (0 / 1 / 2-6 / 7-13 / 14+ días)
- Logo real JPG en nav landing, nav aula, footer y favicon
  - `wwwroot/img/logo.jpg` — listo para swap a SVG con vectorizer.io
- "Simulacro" renombrado a "Examen de práctica" en botón y modal

### feat — Quiz en modal
- Quiz ahora devuelve JSON estructurado y abre el mismo modal que el examen
- Modal muestra label correcto: "Quiz · Pregunta 1 de 5" o "Examen de práctica · Pregunta 1 de 6"
- `ExtractJsonArray`: extrae el array JSON aunque Claude lo envuelva en bloques markdown
  - Fix definitivo para tarjetas y simulacro que a veces caían al fallback de texto plano

### feat — Error log
- Tabla `public.error_logs` (SQL manual en TablePlus)
- `ErrorLogService`: registra source, message, detail, context — nunca tira excepción
- Errores internos logueados en DB, nunca expuestos al cliente
- Admin: sección "Errores recientes" con los últimos 50

### fix
- `HasColumnName` para todas las columnas nuevas de Classroom y Family en `AppDbContext`
  - Sin esto EF genera nombres C# con comillas y Railway falla al arrancar
- `HasColumnType("jsonb")` para `material_sections` — EF enviaba text, Postgres esperaba jsonb
- `SaveMaterial`: try/catch en segmentación y en `SaveChangesAsync` — devuelve JSON de error en lugar de 500 mudo

### pendiente (anotado para después del piloto)
- Avatares personalizables: alumno y tutor eligen de galería fija de emojis — sin upload, mínima superficie de falla

---

## [Sesión 12] — 2026-05-31

### feat — Secciones de material (Opción B)
- Classroom: segmentación automática del PDF en secciones temáticas
  - `SegmentMaterialAsync`: llama Haiku tras el OCR, devuelve `[{title, content}]`
  - Si el parse falla o el texto es corto → fallback a sección única automática
  - Máximo 5 secciones, máximo 40k chars al segmentador
- Classroom: progreso entre sesiones
  - `material_section_index` persiste en DB — el alumno continúa donde quedó
  - "Nueva sesión" solo borra mensajes y CompactSummary; el material y las secciones se preservan
- Classroom sidebar: barra de progreso visual ✅/▶/○ + botones Siguiente/Anterior
  - AJAX: `OnPostAdvanceSectionAsync` actualiza el índice y recarga la página
  - Si hay una sola sección: muestra solo el título, sin botones de navegación
  - Label "Apunte extra del docente" cuando hay PDF cargado (texto pegado = material complementario)
- BuildSystemPrompt: inyecta `sections[index].content` + "Sección X de N: título. Ya trabajadas: ..."
  - Fallback a `classroom.Material` si no hay secciones (texto pegado sin PDF)
  - Quiz, flashcards y simulacro siguen usando `classroom.Material` completo (sin cambio)
- Nuevas columnas en `academic.classrooms`: `material_sections jsonb`, `material_section_index int`, `material_ocr_source text`
- Migración vacía `20260531000000_AddMaterialSections` (SQL aplicado manualmente en TablePlus)

### feat — Estado de suscripción y guard de acceso
- `Family`: nuevos campos `CreatedAt`, `SubscriptionStatus`, `TrialEndsAt`, `PaidUntil`, propiedad `IsAccessAllowed`
  - `SubscriptionStatus` valores: `waitlist` / `trial` / `active` / `trial_expired` / `suspended` / `cancelled`
  - `IsAccessAllowed` = `status` es `trial` o `active`
- Guard en `Verify.cshtml.cs` (magic link padre): si `!IsAccessAllowed` → `/Blocked?status=...`
- Guard en `Entrar.cshtml.cs` (login alumno): igual, Include Family antes de verificar PIN
- Nueva página `/Blocked`: mensaje diferenciado según `trial_expired` vs `suspended`/`cancelled`
- Migración vacía `20260531100000_AddFamilyBilling` (SQL aplicado manualmente en TablePlus)
- Activación de familias del piloto: manual vía TablePlus (`UPDATE families SET subscription_status='trial'...`)

### feat — Monitor de piloto en admin
- Admin: nueva sección "Monitor de Piloto" antes de la Waitlist
  - Cohorte = familias con `subscription_status = 'trial' OR 'active'`
  - KR1 Retención: familias con ≥ N intercambios en últimos 7 días ("X de Y"), badge rojo si > 3 días sin actividad
  - KR2 Hábito: hijos con racha ≥ 7 días ("X de Y"), tabla con racha actual de cada alumno
  - KR3 Intención de pago: input manual (número + nota libre), no persiste en DB
  - KR4 Referidos: input manual (contador + nombres), no persiste en DB
  - Semáforo 🟢🟡🔴 para los 4 KR con umbrales configurables por env var (`PILOT_KR1_THRESHOLD`, etc.)
  - KR3/KR4 se calculan en el render desde query params — se completan al cierre del piloto
- Admin: señal de vencimiento — lista familias con `trial_ends_at` o `paid_until` ≤ 3 días
- Umbrales default: KR1≥6, KR2≥5, KR3≥5, KR4≥3 (ajustables por config)
- `Kr1MinExchanges` default 3 (configurable con `PILOT_KR1_MIN_EXCHANGES`)

### decisiones de arquitectura
- Cobro futuro: plano mensual (NO créditos, NO `credit_accounts`, NO `credit_events`)
- Activación del piloto: manual desde TablePlus, sin portal de pago
- KR3/KR4: no persisten en DB por diseño — son datos de cierre, se completan una vez
- Secciones en Classroom (no en Subject/Topic): suficiente para el piloto, migrable a jerarquía en V2
- Texto pegado: no pasa por segmentación (sección única implícita, sin llamar a Claude)

### env vars nuevas (Railway — opcionales, tienen default)
| Variable | Default | Uso |
|---|---|---|
| `PILOT_KR1_MIN_EXCHANGES` | 3 | Intercambios mínimos en 7 días para considerar familia activa |
| `PILOT_KR1_THRESHOLD` | 6 | Umbral semáforo KR1 |
| `PILOT_KR2_THRESHOLD` | 5 | Umbral semáforo KR2 |
| `PILOT_KR3_THRESHOLD` | 5 | Umbral semáforo KR3 |
| `PILOT_KR4_THRESHOLD` | 3 | Umbral semáforo KR4 |

---

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
