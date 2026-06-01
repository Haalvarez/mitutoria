using miTutoria.Web.Data;
using miTutoria.Web.Data.Entities;

namespace miTutoria.Web.Infrastructure;

public class ErrorLogService(AppDbContext db)
{
    public async Task LogAsync(string source, Exception ex, string? context = null)
    {
        try
        {
            db.ErrorLogs.Add(new ErrorLog
            {
                Source  = source,
                Message = ex.Message,
                Detail  = ex.InnerException?.Message is { } inner
                    ? $"{inner}\n{ex.StackTrace?[..Math.Min(ex.StackTrace.Length, 800)]}"
                    : ex.StackTrace?[..Math.Min(ex.StackTrace?.Length ?? 0, 800)],
                Context = context
            });
            await db.SaveChangesAsync();
        }
        catch
        {
            // el log nunca puede romper la app
        }
    }
}
