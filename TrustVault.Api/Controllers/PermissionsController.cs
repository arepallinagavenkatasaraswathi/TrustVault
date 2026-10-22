using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TrustVault.Api.Data;
using TrustVault.Api.Models;

namespace TrustVault.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PermissionsController : ControllerBase
{
    private readonly TrustVaultDbContext _db;
    private readonly ILogger<PermissionsController> _logger;

    public PermissionsController(
        TrustVaultDbContext db,
        ILogger<PermissionsController> logger)
    {
        _db = db;
        _logger = logger;
    }

    public record ShareRequest(string UserName);

    private async Task WriteAuditLog(
        string userId,
        Guid documentId,
        string action,
        bool success,
        string details)
    {
        _db.AuditLogs.Add(new AuditLog
        {
            UserId = userId,
            DocumentId = documentId,
            Action = action,
            Success = success,
            Timestamp = DateTime.UtcNow,
            Details = details
        });

        await _db.SaveChangesAsync();
    }

    [HttpPost("{documentId:guid}/share")]
    public async Task<IActionResult> ShareDocument(
        Guid documentId,
        ShareRequest request)
    {
        var ownerId = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(ownerId))
            return Unauthorized();

        if (request == null ||
            string.IsNullOrWhiteSpace(request.UserName))
        {
            return BadRequest("UserName is required.");
        }

        var document = await _db.Documents
            .FirstOrDefaultAsync(d => d.Id == documentId);

        if (document == null)
            return NotFound("Document not found.");

        if (document.OwnerId != ownerId)
            return Forbid();

        var targetUser = await _db.AppUsers
            .FirstOrDefaultAsync(u =>
                u.UserName == request.UserName.Trim());

        if (targetUser == null)
            return NotFound("User not found.");

        var targetUserId = targetUser.Id.ToString();

        if (targetUserId == ownerId)
            return BadRequest("Cannot share with yourself.");

        var existing = await _db.DocumentPermissions
            .FirstOrDefaultAsync(p =>
                p.DocumentId == documentId &&
                p.UserId == targetUserId);

        if (existing != null)
        {
            existing.PermissionType = "Read";
            existing.IsRevoked = false;
            existing.RevokedAt = null;
            existing.GrantedBy = ownerId;
            existing.GrantedAt = DateTime.UtcNow;
        }
        else
        {
            _db.DocumentPermissions.Add(
                new DocumentPermission
                {
                    DocumentId = documentId,
                    UserId = targetUserId,
                    PermissionType = "Read",
                    GrantedBy = ownerId,
                    GrantedAt = DateTime.UtcNow
                });
        }

        _db.AuditLogs.Add(new AuditLog
        {
            UserId = ownerId,
            DocumentId = documentId,
            Action = "DocumentShare",
            Success = true,
            Timestamp = DateTime.UtcNow,
            Details = $"Read access granted to user '{targetUser.UserName}'."
        });

        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "Document {DocumentId} shared by {OwnerId} with {TargetUser}",
            documentId, ownerId, targetUser.UserName);

        return Ok(new
        {
            message = "Read access granted.",
            documentId,
            userName = targetUser.UserName
        });
    }

    [HttpDelete("{documentId:guid}/share/{userName}")]
    public async Task<IActionResult> RevokeAccess(
        Guid documentId,
        string userName)
    {
        var ownerId = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(ownerId))
            return Unauthorized();

        var document = await _db.Documents
            .FirstOrDefaultAsync(d => d.Id == documentId);

        if (document == null)
            return NotFound("Document not found.");

        if (document.OwnerId != ownerId)
            return Forbid();

        var targetUser = await _db.AppUsers
            .FirstOrDefaultAsync(u => u.UserName == userName);

        if (targetUser == null)
            return NotFound("User not found.");

        var permission = await _db.DocumentPermissions
            .FirstOrDefaultAsync(p =>
                p.DocumentId == documentId &&
                p.UserId == targetUser.Id.ToString() &&
                !p.IsRevoked);

        if (permission == null)
        {
            await WriteAuditLog(
                ownerId,
                documentId,
                "DocumentRevoke",
                false,
                $"Revocation failed: no active permission for '{targetUser.UserName}'.");

            return NotFound("Active permission not found.");
        }

        permission.IsRevoked = true;
        permission.RevokedAt = DateTime.UtcNow;

        _db.AuditLogs.Add(new AuditLog
        {
            UserId = ownerId,
            DocumentId = documentId,
            Action = "DocumentRevoke",
            Success = true,
            Timestamp = DateTime.UtcNow,
            Details = $"Read access revoked from user '{targetUser.UserName}'."
        });

        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "Access to document {DocumentId} revoked by {OwnerId} for {TargetUser}",
            documentId, ownerId, targetUser.UserName);

        return Ok(new
        {
            message = "Access revoked successfully.",
            documentId,
            userName = targetUser.UserName
        });
    }
}