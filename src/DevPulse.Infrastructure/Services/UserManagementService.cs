using DevPulse.Application.Abstractions.Auth;
using DevPulse.Infrastructure.Identity;
using DevPulse.Shared.Common;
using DevPulse.Shared.Constants;
using DevPulse.Shared.Contracts.Auth;
using Microsoft.AspNetCore.Identity;

namespace DevPulse.Infrastructure.Services;

public sealed class UserManagementService : IUserManagementService
{
    private readonly UserManager<ApplicationUser> _userManager;

    public UserManagementService(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<IReadOnlyList<UserDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var users = _userManager.Users.OrderBy(u => u.Email).ToList();
        var result = new List<UserDto>(users.Count);

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            result.Add(AuthService.MapToDto(user, roles.FirstOrDefault() ?? AppRoles.User));
        }

        return result;
    }

    public async Task<Result<UserDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user is null)
        {
            return Result<UserDto>.Failure("User not found.");
        }

        var roles = await _userManager.GetRolesAsync(user);
        return Result<UserDto>.Success(AuthService.MapToDto(user, roles.FirstOrDefault() ?? AppRoles.User));
    }

    public async Task<Result<UserDto>> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
    {
        var validationError = ValidateUserInput(request.Email, request.Password, request.DisplayName, request.Role);
        if (validationError is not null)
        {
            return Result<UserDto>.Failure(validationError);
        }

        var existing = await _userManager.FindByEmailAsync(request.Email.Trim());
        if (existing is not null)
        {
            return Result<UserDto>.Failure("A user with this email already exists.");
        }

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = request.Email.Trim(),
            Email = request.Email.Trim(),
            DisplayName = request.DisplayName.Trim(),
            EmailConfirmed = true,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        var createResult = await _userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
        {
            return Result<UserDto>.Failure(createResult.Errors.Select(e => e.Description));
        }

        var roleResult = await _userManager.AddToRoleAsync(user, request.Role);
        if (!roleResult.Succeeded)
        {
            await _userManager.DeleteAsync(user);
            return Result<UserDto>.Failure(roleResult.Errors.Select(e => e.Description));
        }

        return Result<UserDto>.Success(AuthService.MapToDto(user, request.Role));
    }

    public async Task<Result<UserDto>> UpdateAsync(Guid id, UpdateUserRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.DisplayName))
        {
            return Result<UserDto>.Failure("Display name is required.");
        }

        if (!AppRoles.All.Contains(request.Role))
        {
            return Result<UserDto>.Failure("Invalid role.");
        }

        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user is null)
        {
            return Result<UserDto>.Failure("User not found.");
        }

        user.DisplayName = request.DisplayName.Trim();
        user.IsActive = request.IsActive;

        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            return Result<UserDto>.Failure(updateResult.Errors.Select(e => e.Description));
        }

        var currentRoles = await _userManager.GetRolesAsync(user);
        if (!currentRoles.Contains(request.Role))
        {
            if (currentRoles.Count > 0)
            {
                var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
                if (!removeResult.Succeeded)
                {
                    return Result<UserDto>.Failure(removeResult.Errors.Select(e => e.Description));
                }
            }

            var addResult = await _userManager.AddToRoleAsync(user, request.Role);
            if (!addResult.Succeeded)
            {
                return Result<UserDto>.Failure(addResult.Errors.Select(e => e.Description));
            }
        }

        return Result<UserDto>.Success(AuthService.MapToDto(user, request.Role));
    }

    public async Task<Result> ChangePasswordAsync(Guid id, ChangePasswordRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.NewPassword))
        {
            return Result.Failure("New password is required.");
        }

        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user is null)
        {
            return Result.Failure("User not found.");
        }

        var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, resetToken, request.NewPassword);
        return result.Succeeded
            ? Result.Success()
            : Result.Failure(result.Errors.Select(e => e.Description));
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user is null)
        {
            return Result.Failure("User not found.");
        }

        var roles = await _userManager.GetRolesAsync(user);
        if (roles.Contains(AppRoles.Admin))
        {
            var adminCount = 0;
            foreach (var admin in _userManager.Users)
            {
                if (await _userManager.IsInRoleAsync(admin, AppRoles.Admin))
                {
                    adminCount++;
                }
            }

            if (adminCount <= 1)
            {
                return Result.Failure("Cannot delete the last administrator.");
            }
        }

        var result = await _userManager.DeleteAsync(user);
        return result.Succeeded
            ? Result.Success()
            : Result.Failure(result.Errors.Select(e => e.Description));
    }

    private static string? ValidateUserInput(string email, string password, string displayName, string role)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return "Email is required.";
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            return "Password is required.";
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            return "Display name is required.";
        }

        if (!AppRoles.All.Contains(role))
        {
            return "Invalid role.";
        }

        return null;
    }
}
