namespace miTutoria.Web.Infrastructure;

/// <summary>
/// Latido en memoria del scheduler (PilotMonitorService) para el health-check del admin.
/// Singleton: el HostedService y el admin viven en el mismo proceso.
/// Se reinicia en cada deploy → muestra "esperando" hasta la 1ª corrida (~2 min post-arranque).
/// </summary>
public class SchedulerHeartbeat
{
    public DateTime? LastRunUtc { get; private set; }

    public void Mark() => LastRunUtc = DateTime.UtcNow;
}
