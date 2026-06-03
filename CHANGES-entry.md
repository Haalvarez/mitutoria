## Sesión 17 — Consentimiento parental y alta de familias

**feat:** consentimiento parental mínimo + invitación desde /admin

### Cambios
- **Consentimiento parental** (`/Consentimiento`): página nueva con texto honesto (Ley 25.326),
  checkbox explícito y persistencia de `consent_at`, `consent_ip`, `consent_version="v1"`
- **Guard en Verify**: después de setear la sesión, si `consent_at` es null → redirige a `/Consentimiento`
- **Invitación desde /admin**: formulario que crea/activa la familia como trial (30 días),
  genera magic link (48hs) y envía mail de bienvenida al piloto vía Resend — sin TablePlus
- **Migración** `20260603210000_AddConsentToFamily`: 3 columnas nuevas en `auth.families`

### SQL a aplicar en TablePlus antes del push
```sql
ALTER TABLE auth.families ADD COLUMN IF NOT EXISTS consent_at timestamptz;
ALTER TABLE auth.families ADD COLUMN IF NOT EXISTS consent_ip text;
ALTER TABLE auth.families ADD COLUMN IF NOT EXISTS consent_version text;

INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260603210000_AddConsentToFamily', '8.0.0');
```

### No tocado
- ROADMAP.md
- Prompt maestro
- Classroom / mochila
