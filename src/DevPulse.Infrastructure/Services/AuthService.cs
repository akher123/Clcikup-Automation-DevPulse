using DevPulse.Application.Abstractions.Auth;
using DevPulse.Infrastructure.Identity;
using DevPulse.Infrastructure.Security;
using DevPulse.Shared.Common;
using DevPulse.Shared.Constants;
using DevPulse.Shared.Contracts.Auth;
using Microsoft.AspNetCore.Identity;

namespace DevPulse.Infrastructure.Services;

public sealed class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly JwtTokenGenerator _tokenGenerator;

    public AuthService(UserManager<ApplicationUser> userManager, JwtTokenGenerator tokenGenerator)
    {
        _userManager = userManager;
        _tokenGenerator = tokenGenerator;
    }

    public async Task<Result<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return Result<LoginResponse>.Failure("Email and password are required.");
        }

        var user = await _userManager.FindByEmailAsync(request.Email.Trim());
        if (user is null || !user.IsActive)
        {
            return Result<LoginResponse>.Failure("Invalid email or password.");
        }

        if (!await _userManager.CheckPasswordAsync(user, request.Password))
        {
            return Result<LoginResponse>.Failure("Invalid email or password.");
        }

        var roles = await _userManager.GetRolesAsync(user);
        var (token, expiresAtUtc) = _tokenGenerator.GenerateToken(user, roles.ToList());
        var role = roles.FirstOrDefault() ?? AppRoles.User;
        var userDto = MapToDto(user, role);

        return Result<LoginResponse>.Success(new LoginResponse(token, expiresAtUtc, userDto));
    }

    internal static UserDto MapToDto(ApplicationUser user, string role) =>
        new(user.Id, user.Email ?? string.Empty, user.DisplayName, role, user.IsActive, user.CreatedAtUtc);
}
