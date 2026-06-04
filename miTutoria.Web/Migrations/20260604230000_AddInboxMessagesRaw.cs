using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using miTutoria.Web.Data;

#nullable disable

namespace miTutoria.Web.Migrations
{
    /// <summary>
    /// Track 2 — Capa 1: tabla cruda de mails de Classroom capturados vía Apps Script.
    /// Idempotente (IF NOT EXISTS). Schema 'inbox'. Dedup por gmail_id (índice único).
    /// Lleva [DbContext] + [Migration] para que db.Database.Migrate() la aplique sola en el arranque.
    /// </summary>
    [DbContext(typeof(AppDbContext))]
    [Migration("20260604230000_AddInboxMessagesRaw")]
    public partial class AddInboxMessagesRaw : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE SCHEMA IF NOT EXISTS inbox;

                CREATE TABLE IF NOT EXISTS inbox.inbox_messages_raw (
                    id            serial PRIMARY KEY,
                    source        text NOT NULL DEFAULT '',
                    gmail_id      text NOT NULL,
                    message_date  timestamptz NOT NULL,
                    to_address    text NOT NULL DEFAULT '',
                    from_address  text NOT NULL DEFAULT '',
                    subject       text NOT NULL DEFAULT '',
                    plain_body    text NOT NULL DEFAULT '',
                    received_at   timestamptz NOT NULL DEFAULT now(),
                    processed     boolean NOT NULL DEFAULT false
                );

                CREATE UNIQUE INDEX IF NOT EXISTS ix_inbox_messages_raw_gmail_id
                    ON inbox.inbox_messages_raw (gmail_id);
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS inbox.inbox_messages_raw;");
        }
    }
}
