using AutoMapper;
using UserManagementAPI.DTOs;
using UserManagementAPI.Models;
using UserManagementAPI.Repositories;

namespace UserManagementAPI.Services;

public class AuthService(
    IUserRepository userRepo,
    IJwtService jwtService,
    IMapper mapper,
    IConfiguration config,
    ILogger<AuthService> logger) : IAuthService {
    private readonly int _refreshTokenDays = int.Parse(config["Jwt:RefreshTokenDays"] ?? "7");

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request) {
        if (await userRepo.ExistsByEmailAsync(request.Email))
            throw new InvalidOperationException("E-mail já cadastrado.");

        var user = mapper.Map<User>(request);
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

        await userRepo.CreateAsync(user);

        logger.LogInformation("Novo usuário registrado: {Email}", request.Email);
        return await GenerateAuthResponseAsync(user);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request) {
        var user = await userRepo.GetByEmailAsync(request.Email)
            ?? throw new UnauthorizedAccessException("Credenciais inválidas.");

        if (!user.IsActive)
            throw new UnauthorizedAccessException("Conta desativada. Contate o suporte.");

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Credenciais inválidas.");

        user.LastLoginAt = DateTime.UtcNow;
        await userRepo.UpdateAsync(user);

        logger.LogInformation("Login realizado: {Email}", user.Email);
        return await GenerateAuthResponseAsync(user);
    }

    public async Task<AuthResponse> RefreshTokenAsync(string token) {
        var refreshToken = await userRepo.GetRefreshTokenAsync(token)
            ?? throw new UnauthorizedAccessException("Token inválido.");

        if (refreshToken.IsRevoked) {
            await userRepo.RevokeAllUserRefreshTokensAsync(refreshToken.UserId);
            throw new UnauthorizedAccessException("Token revogado. Faça login novamente.");
        }

        if (refreshToken.ExpiresAt < DateTime.UtcNow)
            throw new UnauthorizedAccessException("Token expirado. Faça login novamente.");

        var newRefreshToken = CreateRefreshToken(refreshToken.UserId);
        await userRepo.RevokeRefreshTokenAsync(token, newRefreshToken.Token);
        await userRepo.AddRefreshTokenAsync(newRefreshToken);

        var user = refreshToken.User;
        var accessToken = jwtService.GenerateAccessToken(user);
        var expiresAt = DateTime.UtcNow.AddMinutes(double.Parse(config["Jwt:ExpirationMinutes"] ?? "60"));

        return new AuthResponse(accessToken, newRefreshToken.Token, expiresAt, Map(user));
    }

    public async Task RevokeTokenAsync(string token) {
        var refreshToken = await userRepo.GetRefreshTokenAsync(token)
            ?? throw new KeyNotFoundException("Token não encontrado.");

        await userRepo.RevokeRefreshTokenAsync(token);
        logger.LogInformation("Token revogado para usuário {UserId}", refreshToken.UserId);
    }

    public async Task LogoutAllAsync(Guid userId) {
        await userRepo.RevokeAllUserRefreshTokensAsync(userId);
        logger.LogInformation("Todos os tokens revogados para usuário {UserId}", userId);
    }

    private async Task<AuthResponse> GenerateAuthResponseAsync(User user) {
        var accessToken = jwtService.GenerateAccessToken(user);
        var refreshToken = CreateRefreshToken(user.Id);
        var expiresAt = DateTime.UtcNow.AddMinutes(double.Parse(config["Jwt:ExpirationMinutes"] ?? "60"));

        await userRepo.AddRefreshTokenAsync(refreshToken);
        return new AuthResponse(accessToken, refreshToken.Token, expiresAt, Map(user));
    }

    private RefreshToken CreateRefreshToken(Guid userId) => new() {
        Token = jwtService.GenerateRefreshToken(),
        UserId = userId,
        ExpiresAt = DateTime.UtcNow.AddDays(_refreshTokenDays)
    };

    private static UserResponse Map(User u) => new(
        u.Id, u.Name, u.Email, u.Role, u.IsActive,
        u.PhoneNumber, u.ProfilePictureUrl, u.CreatedAt, u.LastLoginAt);
}