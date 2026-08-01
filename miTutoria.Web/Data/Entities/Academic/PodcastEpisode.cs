using miTutoria.Web.Data.Entities.Auth;

namespace miTutoria.Web.Data.Entities.Academic;

// Audio-resumen estilo "podcast" (2 hosts) generado a partir del material de una materia.
// El audio (WAV) se guarda en Postgres (bytea) — decisión de MVP; migra a object storage si crece.
public class PodcastEpisode
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public User Student { get; set; } = null!;
    public int ClassroomId { get; set; }
    public Classroom Classroom { get; set; } = null!;

    public string Title { get; set; } = "Podcast";
    public byte[] Audio { get; set; } = Array.Empty<byte>();
    public string Mime { get; set; } = "audio/wav";
    public int DurationSec { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
