using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace miTutoria.Web.Data;

public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=zephyr.proxy.rlwy.net;Port=21740;Database=railway;Username=postgres;Password=kWiuTJGidGyiHHAlrJkPycjnomVpHHxL")
            .Options;
        return new AppDbContext(options);
    }
}