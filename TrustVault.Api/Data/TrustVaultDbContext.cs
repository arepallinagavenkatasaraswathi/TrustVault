
using Microsoft.EntityFrameworkCore;
using TrustVault.Api.Models;

namespace TrustVault.Api.Data;

public class TrustVaultDbContext : DbContext
{
    public TrustVaultDbContext(
        DbContextOptions<TrustVaultDbContext> options)
        : base(options)
    {
    }

    public DbSet<Document> Documents => Set<Document>();
    public DbSet<AppUser> AppUsers => Set<AppUser>();

    public DbSet<DocumentPermission> DocumentPermissions
        => Set<DocumentPermission>();

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
}
