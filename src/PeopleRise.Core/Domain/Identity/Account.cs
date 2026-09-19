using Microsoft.AspNetCore.Identity;

namespace PeopleRise.Core.Domain;

/// <summary>Organization identity (Core Spec §11): a tenant-local login, never an Employee. "Two
/// identity systems exist in the platform and they are deliberately unlinked" - this is the
/// tenant-side one; the control plane's AppUser is the other, and nothing joins them. Built on
/// ASP.NET Core Identity - <see cref="UserManager{Account}"/> owns password hashing/validation,
/// Email is Identity's own field ("the account carries its own email... the roster holds no
/// contact details by design").</summary>
internal class Account : IdentityUser<Guid>
{
    public Account()
    {
        Id = Guid.CreateVersion7();
    }

    /// <summary>Optional - "not every employee has an account... not every account is an employee."
    /// Never the actor itself: "where the account points at an employee, that employee is context
    /// resolved from it."</summary>
    public Guid? EmployeeId { get; private set; }

    public Employee? Employee { get; private set; }

    public AccountStatus Status { get; private set; } = AccountStatus.Active;

    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    public DateTime? ClosedAt { get; private set; }

    /// <summary>§11: "an account may authenticate against the organization's own directory through
    /// a named adapter, in which case the account holds a directory identifier and the credential
    /// lives there... authorization is never delegated." PasswordHash stays unset for these
    /// accounts - no adapter is built yet (§11.5/§13), this is the schema placeholder for one.</summary>
    public string? DirectoryProvider { get; private set; }

    public string? DirectoryUserId { get; private set; }

    public static Account Create(string email, Guid? employeeId = null) => new()
    {
        UserName = email,
        Email = email,
        EmployeeId = employeeId,
    };

    public void LinkEmployee(Guid employeeId)
    {
        EmployeeId = employeeId;
    }

    public void UnlinkEmployee()
    {
        EmployeeId = null;
    }

    public void SetDirectory(string provider, string directoryUserId)
    {
        DirectoryProvider = provider;
        DirectoryUserId = directoryUserId;
    }

    /// <summary>§11: "a separated employee's account is closed... these are two events on two
    /// records, and neither cascades into the other" - callers decide when to close an account,
    /// never triggered automatically from Employee.Separate(). Closed, never deleted.</summary>
    public void Close()
    {
        if (Status == AccountStatus.Closed)
        { 
            return; 
        }

        Status = AccountStatus.Closed;
        ClosedAt = DateTime.UtcNow;
        LockoutEnabled = true;
        LockoutEnd = DateTimeOffset.MaxValue;
    }
}

public enum AccountStatus 
{ 
    Active, 
    Closed
}
