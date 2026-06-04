using miTutoria.Web.Inbox;
using Xunit;

namespace miTutoria.Tests;

public class SpanishDateTests
{
    private static readonly DateTime Ref = new(2026, 5, 28, 0, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData("28 may", 2026, 5, 28)]
    [InlineData("5 de junio", 2026, 6, 5)]
    [InlineData("1 de septiembre", 2026, 9, 1)]
    [InlineData("15 dic", 2026, 12, 15)]
    public void ParseaFechasEnEspanol(string raw, int y, int mo, int d)
    {
        var r = InboxProcessor.ParseSpanishDate(raw, Ref);
        Assert.NotNull(r);
        Assert.Equal(new DateTime(y, mo, d, 0, 0, 0, DateTimeKind.Utc), r);
    }

    [Theory]
    [InlineData("")]
    [InlineData("la semana que viene")]
    [InlineData("32 may")]
    public void DevuelveNullSiNoParsea(string raw)
    {
        Assert.Null(InboxProcessor.ParseSpanishDate(raw, Ref));
    }
}
