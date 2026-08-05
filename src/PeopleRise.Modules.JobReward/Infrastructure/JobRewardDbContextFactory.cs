using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PeopleRise.Modules.JobReward.Infrastructure;

internal class JobRewardDbContextFactory : IDesignTimeDbContextFactory<JobRewardDbContext>
{
    public JobRewardDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<JobRewardDbContext>()
                .UseNpgsql("Host=localhost;Port=5432;Database=pr_ef_design;Username=postgres;Password=123456")
                .Options;

        return new JobRewardDbContext(options);
    }
}
