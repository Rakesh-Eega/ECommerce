namespace ECommerce.Identity.Domain.Entities
{
    public class AppUser
    {
        public Guid Id { get; private set; } = Guid.NewGuid();
        public string Email { get; private set; } = string.Empty;
        public string PasswordHash { get; private set; } = string.Empty;
        public string FirstName { get; private set; } = string.Empty;
        public string LastName { get; private set; } = string.Empty;
        public string Role { get; private set; } = "Customer"; // Customer | Seller | Admin
        public bool IsEmailVerified { get; private set; } = false;
        public bool IsActive { get; private set; } = true;
        public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
        public List<RefreshToken> RefreshTokens { get; private set; } = new();

        private AppUser() { } // EF Core

        public static AppUser Create(string email, string password,
                                      string firstName, string lastName)
        {
            return new AppUser
            {
                Email = email.ToLowerInvariant().Trim(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12),
                FirstName = firstName,
                LastName = lastName
            };
        }

        public bool VerifyPassword(string password)
            => BCrypt.Net.BCrypt.Verify(password, PasswordHash);

        public void AssignRole(string role) => Role = role;
        public void Deactivate() => IsActive = false;
        public void VerifyEmail() => IsEmailVerified = true;
    }
}
