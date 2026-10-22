
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
public class DocumentsController : ControllerBase
{
    private readonly TrustVaultDbContext _db;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<DocumentsController> _logger;

    public DocumentsController(
        TrustVaultDbContext db,
        IWebHostEnvironment environment,
        ILogger<DocumentsController> logger)
    {
        _db = db;
        _environment = environment;
        _logger = logger;
    }

    // List documents owned by the current user
    // and documents shared with the current user.
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

    // Upload a document.
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

        var storageRoot = Path.Combine(
            _environment.ContentRootPath,
            "Storage");

        Directory.CreateDirectory(storageRoot);

        var documentId = Guid.NewGuid();
        var storageName = documentId.ToString("N") + ".bin";
        var storagePath = Path.Combine(
            storageRoot,
            storageName);

        try
        {
            await using (var stream = new FileStream(
                storagePath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None))
            {
                await file.CopyToAsync(stream);
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
            if (System.IO.File.Exists(storagePath))
                System.IO.File.Delete(storagePath);

            _logger.LogError(
                ex,
                "Document upload failed for user {UserId}",
                userId);

            throw;
        }
    }

    // Download only if the user owns the document
    // or has an active Read permission.
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
                Details = "Download denied: document missing or permission not granted."
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

        var storageRoot = Path.GetFullPath(
            Path.Combine(
                _environment.ContentRootPath,
                "Storage"));

        var storagePath = Path.GetFullPath(
            document.StoragePath);

        var comparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        if (!storagePath.StartsWith(
                storageRoot + Path.DirectorySeparatorChar,
                comparison))
        {
            _logger.LogError(
                "Invalid storage path for document {DocumentId}",
                id);

            return NotFound("Stored file not found.");
        }

        if (!System.IO.File.Exists(storagePath))
            return NotFound("Stored file not found.");

        var fileBytes = await System.IO.File.ReadAllBytesAsync(
            storagePath);

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
            "application/octet-stream",
            document.FileName);
    }
}
