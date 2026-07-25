using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PeopleRise.ControlPlane;

namespace PeopleRise.Tenancy;

/// <summary>DEV ONLY: reads X-User-Id. Swap for JWT/OIDC auth in production.</summary>
public sealed class CurrentUserMiddleware(RequestDelegate next)
{
    public async Task Invoke(HttpContext httpContext, ICurrentUser user)
    {
        if (httpContext.Request.Headers.TryGetValue("X-User-Id", out var v) 
            && Guid.TryParse(v, out var id))
        {
            user.Set(id);
        }
            
        await next(httpContext);
    }
}

/// <summary>Resolves the active tenant from X-Tenant-Id, validates the caller's access grant,
/// and binds the per-request connection string. Authorization and routing happen together.</summary>
public sealed class TenantResolutionMiddleware(RequestDelegate next)
{
    public async Task Invoke(HttpContext httpContext, ICurrentUser user, ITenantContext tenant,
                             ControlPlaneDbContext controlPlaneDbContext, TenantConnectionFactory factory)
    {
        if (httpContext.Request.Headers.TryGetValue("X-Tenant-Id", out var raw) 
            && Guid.TryParse(raw, out var tenantId))
        {
            if (!user.IsAuthenticated)
            { 
                httpContext.Response.StatusCode = StatusCodes.Status401Unauthorized; 
                await httpContext.Response.WriteAsync("Missing X-User-Id."); 
                return; 
            }

            var access = await controlPlaneDbContext.Access.Include(a => a.Tenant)
                .FirstOrDefaultAsync(a => a.UserId == user.UserId && a.TenantId == tenantId
                                       && a.Tenant!.Status == TenantStatus.Active);
            if (access is null)
            { 
                httpContext.Response.StatusCode = StatusCodes.Status403Forbidden; 
                await httpContext.Response.WriteAsync("No access to tenant, or tenant is not active."); 
                return; 
            }

            tenant.Set(access.TenantId, factory.ForDatabase(access.Tenant!.DbName));
        }
        await next(httpContext);
    }
}

public static class TenancyExtensions
{
    public static IServiceCollection AddTenancy(this IServiceCollection services, string tenantConnectionTemplate)
    {
        services.AddScoped<ICurrentUser, CurrentUser>();
        services.AddScoped<ITenantContext, TenantContext>();
        services.AddSingleton(new TenantConnectionFactory(tenantConnectionTemplate));
        return services;
    }

    public static IApplicationBuilder UseTenancy(this IApplicationBuilder app)
    {
        app.UseMiddleware<CurrentUserMiddleware>();
        app.UseMiddleware<TenantResolutionMiddleware>();
        return app;
    }
}
