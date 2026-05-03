using CyberManagement.Api.Configuration;
using CyberManagement.Api.Data;
using CyberManagement.Api.DTOs;
using CyberManagement.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace CyberManagement.Api.Services;

public interface IAuthService
{
    Task<LoginResponse?> LoginAsync(string username, string password);
    Task<UserDto?> GetCurrentUserAsync(Guid userId);
}

public class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly JwtOptions _jwt;

    public AuthService(AppDbContext db, IOptions<JwtOptions> jwt)
    {
        _db = db;
        _jwt = jwt.Value;
    }

    public async Task<LoginResponse?> LoginAsync(string username, string password)
    {
        var user = await _db.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Username == username && u.IsActive);

        if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            return null;

        user.LastLogin = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        var token = GenerateToken(user);
        var expiresIn = _jwt.ExpiryHours * 3600;

        return new LoginResponse(
            token, "Bearer", expiresIn,
            new UserDto(user.Id, user.Username, user.Email, user.FullName, user.Role?.Name, user.IsActive, user.LastLogin));
    }

    public async Task<UserDto?> GetCurrentUserAsync(Guid userId)
    {
        var user = await _db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null) return null;
        return new UserDto(user.Id, user.Username, user.Email, user.FullName, user.Role?.Name, user.IsActive, user.LastLogin);
    }

    private string GenerateToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role?.Name ?? "viewer"),
        };
        var token = new JwtSecurityToken(
            issuer: _jwt.Issuer,
            audience: _jwt.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(_jwt.ExpiryHours),
            signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
