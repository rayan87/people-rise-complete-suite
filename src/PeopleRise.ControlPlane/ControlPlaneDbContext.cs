using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using PeopleRise.SharedKernel;

namespace PeopleRise.ControlPlane;

/// <summary>Single, platform-wide database: tenant registry, users, and access grants.</summary>
public class ControlPlaneDbContext(DbContextOptions<ControlPlaneDbContext> options) : DbContext(options)
{
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<UserTenantAccess> Access => Set<UserTenantAccess>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);
        b.Entity<Tenant>().HasIndex(x => x.DbName).IsUnique();
        b.Entity<AppUser>().HasIndex(x => x.Email).IsUnique();
        b.Entity<UserTenantAccess>().HasIndex(x => new { x.UserId, x.TenantId }).IsUnique();
        b.ApplyConventions();
    }

    // Override the bool overloads so the guard runs on EVERY save path (sync and async).
    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    { EfConventions.ApplyTimestampsAndImmutability(ChangeTracker); return base.SaveChanges(acceptAllChangesOnSuccess); }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken ct = default)
    { EfConventions.ApplyTimestampsAndImmutability(ChangeTracker); return base.SaveChangesAsync(acceptAllChangesOnSuccess, ct); }
}
