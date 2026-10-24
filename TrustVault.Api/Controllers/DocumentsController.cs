
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TrustVault.Api.Data;
using TrustVault.Api.Models;
using TrustVault.Api.Services;

namespace TrustVault.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DocumentsController : ControllerBase
{
    private readonly TrustVaultDbContext _db;
    private readonly SupabaseStorageService _storage;
    private readonly ILogger<DocumentsController> _logger;

    public DocumentsController(
        TrustVaultDbContext db,
        SupabaseStorageService storage,
        ILogger<DocumentsController> logger)
    {
        _db = db;
        _storage = storage;
        _logger = logger;
    }

    // List owned documents and documents shared with the user.
    [HttpGet]
    public async Task<IActionResult> GetDocuments()
    {
        var userId = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var documents = await _db.Documents
            .AsNoTracking()
            .Where(d =>
                d.OwnerId == userId ||
                _db.DocumentPermissions.Any(p =>
                    p.DocumentId == d.Id &&
                    p.UserId == userId &&
                    p.PermissionType == "Read" &&
                    !p.IsRevoked))
            .Select(d => new
            {
                d.Id,
                d.FileName,
                d.ContentType,
                d.CreatedAt,
                IsOwner = d.OwnerId == userId
            })
            .ToListAsync();

        return Ok(documents);
    }

    // Upload a document to the private Supabase bucket.
    [HttpPost("upload")]
    [RequestSizeLimit(10_000_000)]
    public async Task<IActionResult> UploadDocument(
        IFormFile file)
    {
        var userId = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        if (file == null || file.Length == 0)
            return BadRequest("Please select a file.");

        if (file.Length > 10_000_000)
            return BadRequest("Maximum file size is 10 MB.");

        var originalName = Path.GetFileName(file.FileName);

        if (string.IsNullOrWhiteSpace(originalName))
            return BadRequest("Invalid file name.");

        var documentId = Guid.NewGuid();

        // Store only a generated object key, not a local disk path.
        var storagePath = documentId.ToString("N") + ".bin";

        try
        {
            await using (var stream = file.OpenReadStream())
            {
                await _storage.UploadAsync(
                    storagePath,
                    stream,
                    "application/octet-stream");
            }

            var document = new Document
            {
                Id = documentId,
                FileName = originalName,
                ContentType = "application/octet-stream",
                StoragePath = storagePath,
                OwnerId = userId,
                CreatedAt = DateTime.UtcNow
            };

            _db.Documents.Add(document);

            _db.AuditLogs.Add(new AuditLog
            {
                UserId = userId,
                DocumentId = document.Id,
                Action = "DocumentUpload",
                Success = true,
                Timestamp = DateTime.UtcNow,
                Details = "Document uploaded successfully."
            });

            await _db.SaveChangesAsync();

            _logger.LogInformation(
                "Document {DocumentId} uploaded by user {UserId}",
                document.Id,
                userId);

            return Created(
                $"/api/Documents/{document.Id}",
                new
                {
                    document.Id,
                    document.FileName,
                    document.CreatedAt
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Document upload failed for user {UserId}",
                userId);

            throw;
        }
    }

    // Download only for the owner or a user with active Read permission.
    [HttpGet("{id:guid}/download")]
    public async Task<IActionResult> DownloadDocument(Guid id)
    {
        var userId = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var document = await _db.Documents
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == id);

        var hasPermission = false;

        if (document != null)
        {
            hasPermission = document.OwnerId == userId;

            if (!hasPermission)
            {
                hasPermission = await _db.DocumentPermissions
                    .AnyAsync(p =>
                        p.DocumentId == id &&
                        p.UserId == userId &&
                        p.PermissionType == "Read" &&
                        !p.IsRevoked);
            }
        }

        if (document == null || !hasPermission)
        {
            _db.AuditLogs.Add(new AuditLog
            {
                UserId = userId,
                DocumentId = id,
                Action = "DocumentDownload",
                Success = false,
                Timestamp = DateTime.UtcNow,
                Details =
                    "Download denied: document missing or permission not granted."
            });

            await _db.SaveChangesAsync();

            _logger.LogWarning(
                "Document download denied for user {UserId}, document {DocumentId}",
                userId,
                id);

            return NotFound(
                "Document not found or access denied.");
        }

        if (string.IsNullOrWhiteSpace(document.StoragePath))
            return NotFound("Stored file not found.");

        byte[] fileBytes;

        try
        {
            fileBytes = await _storage.DownloadAsync(
                document.StoragePath);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(
                ex,
                "Supabase download failed for document {DocumentId}",
                id);

            return StatusCode(
                StatusCodes.Status502BadGateway,
                "Unable to retrieve the document from storage.");
        }

        _db.AuditLogs.Add(new AuditLog
        {
            UserId = userId,
            DocumentId = id,
            Action = "DocumentDownload",
            Success = true,
            Timestamp = DateTime.UtcNow,
            Details = "Document downloaded successfully."
        });

        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "Document {DocumentId} downloaded by user {UserId}",
            id,
            userId);

        return File(
            fileBytes,
            document.ContentType ?? "application/octet-stream",
            document.FileName);
    }
}
