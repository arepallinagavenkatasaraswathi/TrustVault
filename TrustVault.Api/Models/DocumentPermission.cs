namespace TrustVault.Api.Models;

public class DocumentPermission
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid DocumentId { get; set; }

    public string UserId { get; set; } = string.Empty;

    public string PermissionType { get; set; } = "Read";

    public string GrantedBy { get; set; } = string.Empty;

    public DateTime GrantedAt { get; set; } = DateTime.UtcNow;

    public bool IsRevoked { get; set; } = false;

    public DateTime? RevokedAt { get; set; }
}