namespace UserManagementAPI.Models;

public class RefreshToken {
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsRevoked { get; set; } = false;
    public string? ReplacedByToken { get; set; }

    public Guid UserId { get; set; }
    public User User { get; set; }
}