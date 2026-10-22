
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using TrustVault.Api.Data;
using TrustVault.Api.Models;

namespace TrustVault.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly TrustVaultDbContext _db;
    private readonly IConfiguration _configuration;
    private readonly PasswordHasher<AppUser> _hasher = new();

    public AuthController(
        TrustVaultDbContext db,
        IConfiguration configuration)
    {
        _db = db;
        _configuration = configuration;
    }

    public record AuthRequest(string UserName, string Password);

    [HttpPost("register")]
    public async Task<IActionResult> Register(AuthRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.UserName) ||
            string.IsNullOrWhiteSpace(request.Password) ||
            request.UserName.Length > 100 ||
            request.Password.Length < 12 ||
            request.Password.Length > 128)
        {
            return BadRequest(
                "Username is required; password must be 12-128 characters.");
        }

        var userName = request.UserName.Trim();

        if (await _db.AppUsers.AnyAsync(
            u => u.UserName == userName))
        {
            return Conflict("Username already exists.");
        }

        var user = new AppUser { UserName = userName };
        user.PasswordHash = _hasher.HashPassword(
            user, request.Password);

        _db.AppUsers.Add(user);
        await _db.SaveChangesAsync();

        return Ok(new { message = "Registration successful." });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(AuthRequest request)
    {
        var user = await _db.AppUsers.FirstOrDefaultAsync(
            u => u.UserName == request.UserName.Trim());

        if (user == null)
            return Unauthorized("Invalid username or password.");

        var result = _hasher.VerifyHashedPassword(
            user, user.PasswordHash, request.Password);

        if (result == PasswordVerificationResult.Failed)
            return Unauthorized("Invalid username or password.");

        var key = _configuration["Jwt:Key"];
        if (string.IsNullOrEmpty(key) || Encoding.UTF8.GetByteCount(key) < 32)
            return StatusCode(500, "JWT configuration is invalid.");

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.UserName)
        };

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(30),
            signingCredentials: credentials);

        return Ok(new
        {
            token = new JwtSecurityTokenHandler().WriteToken(token),
            expiresAt = token.ValidTo
        });
    }
}
