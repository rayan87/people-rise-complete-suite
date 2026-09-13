using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PeopleRise.Core.Infrastructure;

internal class CoreDbContextFactory : IDesignTimeDbContextFactory<CoreDbContext>
{
    public CoreDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<CoreDbContext>()
                .UseNpgsql("Host=localhost;Port=5432;Database=pr_ef_design;Username=postgres;Password=123456")
                .Options;

        return new CoreDbContext(options);
    }
}
