using CyberManagement.Api.DTOs;
using CyberManagement.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CyberManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;
    private readonly IAuditService _audit;

    public AuthController(IAuthService auth, IAuditService audit)
    {
        _auth = auth;
        _audit = audit;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<LoginResponse>>> Login([FromBody] LoginRequest request)
    {
        var result = await _auth.LoginAsync(request.Username, request.Password);
        if (result == null)
        {
            await _audit.LogAsync(null, request.Username, "LOGIN_FAILED",
                description: "تلاش ورود ناموفق", ipAddress: GetIpAddress(), responseCode: 401);
            return Unauthorized(new ApiResponse<LoginResponse>(false, null, "نام کاربری یا رمز عبور اشتباه است"));
        }

        await _audit.LogAsync(result.User.Id, request.Username, "LOGIN",
            description: "ورود موفق", ipAddress: GetIpAddress(), responseCode: 200);

        return Ok(new ApiResponse<LoginResponse>(true, result, "ورود موفق"));
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<UserDto>>> Me()
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user = await _auth.GetCurrentUserAsync(userId);
        return user == null
            ? NotFound(new ApiResponse<UserDto>(false, null, "کاربر یافت نشد"))
            : Ok(new ApiResponse<UserDto>(true, user));
    }
    
    [HttpPost("logout")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<object>>> Logout()
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var username = User.FindFirstValue(ClaimTypes.Name);
        await _audit.LogAsync(userId, username, "LOGOUT", ipAddress: GetIpAddress());
        return Ok(new ApiResponse<object>(true, null, "خروج موفق"));
    }

    private string GetIpAddress()
    {
        return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}
