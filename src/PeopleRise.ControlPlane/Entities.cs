using PeopleRise.SharedKernel;

namespace PeopleRise.ControlPlane;

public enum TenantStatus { Active, Archived }
public enum OwnerType { Consulting, Client }      // Model A = Consulting, Model B = Client
public enum AccessRole { Consultant, ClientAdmin, ClientUser }

/// <summary>A tenant is always a client organization. Its data lives in its own database (DbName).</summary>
public class Tenant : Entity
{
    public string Name { get; set; } = "";
    public string DbName { get; set; } = "";
    public TenantStatus Status { get; set; } = TenantStatus.Active;
    public OwnerType OwnerType { get; set; } = OwnerType.Consulting;
}

public class AppUser : Entity
{
    public string Email { get; set; } = "";
    public string DisplayName { get; set; } = "";
}

/// <summary>The cross-tenant grant. A consultant (Model A) has many rows; a client user (Model B) has one.
/// The A to B handover is inserting access rows for the client's own users - not a data migration.</summary>
public class UserTenantAccess : Entity
{
    public Guid UserId { get; set; }
    public AppUser? User { get; set; }
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }
    public AccessRole Role { get; set; } = AccessRole.Consultant;
}
