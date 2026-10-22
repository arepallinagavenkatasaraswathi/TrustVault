
using Microsoft.EntityFrameworkCore;
using TrustVault.Api.Data;

namespace TrustVault.Api.Services;

public class DocumentPermissionService
{
    private readonly TrustVaultDbContext _db;

    public DocumentPermissionService(TrustVaultDbContext db)
    {
        _db = db;
    }

    public async Task<bool> CanReadAsync(
        Guid documentId,
        string userId)
    {
        var document = await _db.Documents
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == documentId);

        if (document is null)
            return false;

        if (document.OwnerId == userId)
            return true;

        return await _db.DocumentPermissions
            .AsNoTracking()
            .AnyAsync(p =>
                p.DocumentId == documentId &&
                p.UserId == userId &&
                p.PermissionType == "Read" &&
                !p.IsRevoked);
    }
}
