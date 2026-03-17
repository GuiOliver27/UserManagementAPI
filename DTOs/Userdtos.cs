namespace UserManagementAPI.DTOs;

public record RegisterRequest(
    string Name,
    string Email,
    string Password,
    string? PhoneNumber = null
);

public record LoginRequest(
    string Email,
    string Password
);

public record AuthResponse(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt,
    UserResponse User
);

public record RefreshTokenRequest(string Token);

public record UserResponse(
    Guid Id,
    string Name,
    string Email,
    string Role,
    bool IsActive,
    string? ProfilePictureUrl,
    string? profilePictureUrl,
    DateTime CreatedAt,
    DateTime? LastLoginAt
);

public record UpdateUserRequest(
    string? Name,
    string? PhoneNumber,
    string? ProfilePictureUrl
);

public record ChangePasswordRequest(
    string CurrentPassword,
    string NewPassword
);

public record UpdateRoleRequest(string Role);

public record PageResult<T>(
    IEnumerable<T> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages
);

public record UserQueryParams(
    int Page = 1,
    int PageSize = 10,
    string? Search = null,
    string? Role = null,
    bool? IsActive = null,
    string SortBy = "CreatedAt",
    string SortOrder = "desc"
);

public record ApiResponse<T>(bool Success, string? Message, T? Data, IEnumerable<string>? Errors = null);