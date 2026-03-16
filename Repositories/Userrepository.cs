using Microsoft.EntityFrameworkCore;
using UserManagementAPI.Data;
using UserManagementAPI.DTOs;
using UserManagementAPI.Models;

namespace UserManagementAPI.Repositories;

public class UserRepository(AppDbContext context) : IUserRepository {
    public async Task<User?> GetByIdAsync(Guid id)
        => await context.Users.Include(u => u.RefreshTokens).FirstOrDefaultAsync(u => u.Id == id);

    public async Task<User?> GetByEmailAsync(string email)
        => await context.Users.Include(u => u.RefreshTokens)
                               .FirstOrDefaultAsync(u => u.Email == email.ToLower());

    public async Task<(IEnumerable<User> Users, int Total)> GetAllAsync(UserQueryParams q) {
        var query = context.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(q.Search))
            query = query.Where(u => u.Name.Contains(q.Search) || u.Email.Contains(q.Search));

        if (!string.IsNullOrWhiteSpace(q.Role))
            query = query.Where(u => u.Role == q.Role);

        if (q.IsActive.HasValue)
            query = query.Where(u => u.IsActive == q.IsActive.Value);

        var total = await query.CountAsync();

        query = q.SortBy.ToLower() switch {
            "name" => q.SortOrder == "asc" ? query.OrderBy(u => u.Name) : query.OrderByDescending(u => u.Name),
            "email" => q.SortOrder == "asc" ? query.OrderBy(u => u.Email) : query.OrderByDescending(u => u.Email),
            "createdat" => q.SortOrder == "asc" ? query.OrderBy(u => u.CreatedAt) : query.OrderByDescending(u => u.CreatedAt),
            _ => query.OrderByDescending(u => u.CreatedAt)
        };

        var users = await query.Skip((q.Page - 1) * q.PageSize).Take(q.PageSize).ToListAsync();
        return (users, total);
    }

    public async Task<User> CreateAsync(User user) {
        user.Email = user.Email.ToLower();
        context.Users.Add(user);
        await context.SaveChangesAsync();
        return user;
    }

    public async Task<User> UpdateAsync(User user) {
        user.UpdatedAt = DateTime.UtcNow;
        context.Users.Update(user);
        await context.SaveChangesAsync();
        return user;
    }

    public async Task<bool> DeleteAsync(Guid id) {
        var user = await context.Users.FindAsync(id);
        if (user is null) return false;
        context.Users.Remove(user);
        await context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ExistsByEmailAsync(string email)
        => await context.Users.AnyAsync(u => u.Email == email.ToLower());

    public async Task<RefreshToken?> GetRefreshTokenAsync(string token)
        => await context.RefreshTokens.Include(r => r.User).FirstOrDefaultAsync(r => r.Token == token);

    public async Task AddRefreshTokenAsync(RefreshToken token) {
        context.RefreshTokens.Add(token);
        await context.SaveChangesAsync();
    }

    public async Task RevokeRefreshTokenAsync(string token, string? replacedByToken = null) {
        var refreshToken = await context.RefreshTokens.FirstOrDefaultAsync(r => r.Token == token);
        if (refreshToken is null) return;
        refreshToken.IsRevoked = true;
        refreshToken.ReplacedByToken = replacedByToken;
        await context.SaveChangesAsync();
    }

    public async Task RevokeAllUserRefreshTokensAsync(Guid userId) {
        var tokens = await context.RefreshTokens.Where(r => r.UserId == userId && !r.IsRevoked).ToListAsync();
        tokens.ForEach(t => t.IsRevoked = true);
        await context.SaveChangesAsync();
    }
}