using System.Linq.Dynamic.Core;
using UserManagementAPI.DTOs;

namespace UserManagementAPI.Services;

public interface IAuthService {
    Task<AuthResponse> RegisterAsync(RegisterRequest request);
    Task<AuthResponse> LoginAsync(LoginRequest request);
    Task<AuthResponse> RefreshTokenAsync(string token);
    Task RevokeTokenAsync(string token);
    Task LogoutAllAsync(Guid userId);
}

public interface IUserService {
    Task<PagedResult<UserResponse>> GetAllAsync(UserQueryParams queryParams);
    Task<UserResponse> GetByIdAsync(Guid id);
    Task<UserResponse> UpdateAsync(Guid id, UpdateUserRequest request);
    Task ChangePasswordAsync(Guid id, ChangePasswordRequest request);
    Task UpdateRoleAsync(Guid id, UpdateRoleRequest request);
    Task DeactivateAsync(Guid id);
    Task ActivateAsync(Guid id);
    Task DeleteAsync(Guid id);
}