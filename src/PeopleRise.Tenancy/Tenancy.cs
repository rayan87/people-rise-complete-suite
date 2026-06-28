using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PeopleRise.ControlPlane;

namespace PeopleRise.Tenancy;

/// <summary>DEV ONLY: reads X-User-Id. Swap for JWT/OIDC auth in production.</summary>
public sealed class CurrentUserMiddleware(RequestDelegate next)
{
    public async Task Invoke(HttpContext ctx, ICurrentUser user)
    {
        if (ctx.Request.Headers.TryGetValue("X-User-Id", out var v) && Guid.TryParse(v, out var id))
            user.Set(id);
        await next(ctx);
    }
}

/// <summary>Resolves the active tenant from X-Tenant-Id, validates the caller's access grant,
/// and binds the per-request connection string. Authorization and routing happen together.</summary>
public sealed class TenantResolutionMiddleware(RequestDelegate next)
{
    public async Task Invoke(HttpContext ctx, ICurrentUser user, ITenantContext tenant,
                             ControlPlaneDbContext cp, TenantConnectionFactory factory)
    {
        if (ctx.Request.Headers.TryGetValue("X-Tenant-Id", out var raw) && Guid.TryParse(raw, out var tid))
        {
            if (!user.IsAuthenticated)
            { ctx.Response.StatusCode = StatusCodes.Status401Unauthorized; await ctx.Response.WriteAsync("Missing X-User-Id."); return; }

            var access = await cp.Access.Include(a => a.Tenant)
                .FirstOrDefaultAsync(a => a.UserId == user.UserId && a.TenantId == tid
                                       && a.Tenant!.Status == TenantStatus.Active);
            if (access is null)
            { ctx.Response.StatusCode = StatusCodes.Status403Forbidden; await ctx.Response.WriteAsync("No access to tenant, or tenant is not active."); return; }

            tenant.Set(access.TenantId, factory.ForDatabase(access.Tenant!.DbName));
        }
        await next(ctx);
    }
}

public static class TenancyExtensions
{
    public static IServiceCollection AddTenancy(this IServiceCollection s, string tenantConnectionTemplate)
    {
        s.AddScoped<ICurrentUser, CurrentUser>();
        s.AddScoped<ITenantContext, TenantContext>();
        s.AddSingleton(new TenantConnectionFactory(tenantConnectionTemplate));
        return s;
    }

    public static IApplicationBuilder UseTenancy(this IApplicationBuilder app)
    {
        app.UseMiddleware<CurrentUserMiddleware>();
        app.UseMiddleware<TenantResolutionMiddleware>();
        return app;
    }
}
