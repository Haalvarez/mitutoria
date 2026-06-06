namespace miTutoria.Web.Data.Entities.Academic;

/// <summary>
/// Atención dedicada: una sesión de uso del Aula por un alumno (una carga de página).
/// El cliente acumula milisegundos por estado y los reporta por beats; el servidor hace
/// upsert por (student_id, client_key) — valores acumulativos, last-write-wins.
///   focused = pestaña visible + enfocada + con input reciente (concentrado real)
///   idle    = visible + enfocada pero sin input por > umbral (se descarta: ni acá ni allá)
///   away    = pestaña oculta o ventana sin foco (se fue a otra pestaña/app)
/// Concentración = focused / (focused + away). Solo lo ve el padre.
/// </summary>
public class FocusSession
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public string ClientKey { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastBeatAt { get; set; } = DateTime.UtcNow;
    public long FocusedMs { get; set; }
    public long IdleMs { get; set; }
    public long AwayMs { get; set; }
    public int Interruptions { get; set; }
}
