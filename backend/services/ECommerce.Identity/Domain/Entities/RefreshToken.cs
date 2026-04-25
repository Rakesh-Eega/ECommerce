namespace ECommerce.Identity.Domain.Entities
{
    public class RefreshToken
    {
        public Guid Id { get; private set; } = Guid.NewGuid();
        public Guid UserId { get; private set; }
        public string Token { get; private set; } = string.Empty;
        public DateTime ExpiresAt { get; private set; }
        public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
        public bool IsRevoked { get; private set; } = false;
        public string? RevokedReason { get; private set; }
        public AppUser User { get; private set; } = null!;

        private RefreshToken() { }

        public static RefreshToken Create(Guid userId, int expiryDays = 7)
        {
            return new RefreshToken
            {
                UserId = userId,
                Token = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(64)),
                ExpiresAt = DateTime.UtcNow.AddDays(expiryDays)
            };
        }

        public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
        public bool IsActive => !IsRevoked && !IsExpired;

        public void Revoke(string reason = "Explicit logout")
        {
            IsRevoked = true;
            RevokedReason = reason;
        }
    }
}
