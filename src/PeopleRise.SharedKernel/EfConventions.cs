using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace PeopleRise.SharedKernel;

/// <summary>
/// Shared EF conventions so every context behaves the same:
/// snake_case names, enums stored as text, exact-decimal money, char(3) currency,
/// auto timestamps, and insert-only enforcement for ImmutableEntity.
/// </summary>
public static class EfConventions
{
    public static string ToSnake(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        var sb = new StringBuilder(s.Length + 8);
        for (int i = 0; i < s.Length; i++)
        {
            char c = s[i];
            if (char.IsUpper(c))
            {
                if (i > 0 && (!char.IsUpper(s[i - 1]) || (i + 1 < s.Length && char.IsLower(s[i + 1]))))
                    sb.Append('_');
                sb.Append(char.ToLowerInvariant(c));
            }
            else sb.Append(c);
        }
        return sb.ToString();
    }

    public static void ApplyConventions(this ModelBuilder b)
    {
        // 1. snake_case tables + columns
        foreach (var et in b.Model.GetEntityTypes())
        {
            et.SetTableName(ToSnake(et.GetTableName() ?? et.ClrType.Name));
            foreach (var p in et.GetProperties())
                p.SetColumnName(ToSnake(p.Name));
        }

        // 2. typed conventions (collect first, then apply)
        var enums = new List<(Type t, string n)>();
        var decimals = new List<(Type t, string n)>();
        var currencies = new List<(Type t, string n)>();
        foreach (var et in b.Model.GetEntityTypes())
            foreach (var p in et.GetProperties())
            {
                var ct = Nullable.GetUnderlyingType(p.ClrType) ?? p.ClrType;
                if (ct.IsEnum) enums.Add((et.ClrType, p.Name));
                else if (ct == typeof(decimal)) decimals.Add((et.ClrType, p.Name));
                if (p.Name == "Currency" && ct == typeof(string)) currencies.Add((et.ClrType, p.Name));
            }
        foreach (var (t, n) in enums) b.Entity(t).Property(n).HasConversion<string>().HasMaxLength(32);
        foreach (var (t, n) in decimals) b.Entity(t).Property(n).HasPrecision(18, 4);   // money: exact, never float
        foreach (var (t, n) in currencies) b.Entity(t).Property(n).HasColumnType("char(3)");
    }

    /// <summary>Stamp timestamps and enforce insert-only on ImmutableEntity. Call from SaveChanges.</summary>
    public static void ApplyTimestampsAndImmutability(ChangeTracker tracker)
    {
        var now = DateTime.UtcNow;
        foreach (var e in tracker.Entries())
        {
            if (e.Entity is ImmutableEntity imm)
            {
                if (e.State is EntityState.Modified or EntityState.Deleted)
                    throw new InvalidOperationException(
                        $"{e.Entity.GetType().Name} is insert-only; corrections must create a new row, not edit history.");
                if (e.State == EntityState.Added && imm.CreatedAt == default) imm.CreatedAt = now;
            }
            else if (e.Entity is Entity ent)
            {
                if (e.State == EntityState.Added) { if (ent.CreatedAt == default) ent.CreatedAt = now; ent.UpdatedAt = now; }
                else if (e.State == EntityState.Modified) ent.UpdatedAt = now;
            }
        }
    }
}
