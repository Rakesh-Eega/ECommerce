using ECommerce.Identity.Domain.Entities;
using ECommerce.Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using static ECommerce.Identity.Application.DTOs.AuthDtos;

namespace ECommerce.Identity.Application.Services
{
    public interface IAuthService
    {
        Task<(AuthResponse? Response, string? Error)> RegisterAsync(RegisterRequest request);
        Task<(AuthResponse? Response, string? Error)> LoginAsync(LoginRequest request);
        Task<(AuthResponse? Response, string? Error)> RefreshTokenAsync(string refreshToken);
        Task<bool> LogoutAsync(string refreshToken);
        Task<(AuthResponse? Response, string? Error)> RegisterAsSellerAsync(RegisterRequest request);
        Task<(bool Success, string? Error)> UpdateUserRoleAsync(Guid userId, string role);
    }

    public class AuthService : IAuthService
    {
        private readonly AppDbContext _db;
        private readonly ITokenService _tokenService;
        private readonly ILogger<AuthService> _logger;

        public AuthService(AppDbContext db, ITokenService tokenService,
                           ILogger<AuthService> logger)
        {
            _db = db;
            _tokenService = tokenService;
            _logger = logger;
        }
        private static readonly string[] AllowedRoles = ["Customer", "Seller", "Admin"];

        public async Task<(AuthResponse?, string?)> RegisterAsync(RegisterRequest request)
        {
            var exists = await _db.Users.AnyAsync(u => u.Email == request.Email.ToLowerInvariant());
            if (exists)
                return (null, "Email already registered.");

            var user = AppUser.Create(request.Email, request.Password,
                                       request.FirstName, request.LastName);
            var refreshToken = RefreshToken.Create(user.Id);

            _db.Users.Add(user);
            _db.RefreshTokens.Add(refreshToken);
            await _db.SaveChangesAsync();

            _logger.LogInformation("New user registered: {Email}", user.Email);

            return (BuildAuthResponse(user, refreshToken.Token), null);
        }

        public async Task<(AuthResponse?, string?)> LoginAsync(LoginRequest request)
        {
            var user = await _db.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email.ToLowerInvariant());

            if (user is null || !user.VerifyPassword(request.Password))
                return (null, "Invalid credentials."); // NEVER specify which field is wrong

            if (!user.IsActive)
                return (null, "Account deactivated. Contact support.");

            // Revoke old tokens (single session per user — change if multi-device needed)
            var oldTokens = await _db.RefreshTokens
                .Where(r => r.UserId == user.Id && !r.IsRevoked)
                .ToListAsync();
            oldTokens.ForEach(t => t.Revoke("New login"));

            var refreshToken = RefreshToken.Create(user.Id);
            _db.RefreshTokens.Add(refreshToken);
            await _db.SaveChangesAsync();

            return (BuildAuthResponse(user, refreshToken.Token), null);
        }

        public async Task<(AuthResponse?, string?)> RegisterAsSellerAsync(RegisterRequest request)
        {
            var exists = await _db.Users.AnyAsync(u => u.Email == request.Email.ToLowerInvariant());
            if (exists)
                return (null, "Email already registered.");

            var user = AppUser.Create(request.Email, request.Password,
                                       request.FirstName, request.LastName);

            user.AssignRole("Seller"); // Only Seller — never Admin from this endpoint

            var refreshToken = RefreshToken.Create(user.Id);
            _db.Users.Add(user);
            _db.RefreshTokens.Add(refreshToken);
            await _db.SaveChangesAsync();

            return (BuildAuthResponse(user, refreshToken.Token), null);
        }

        public async Task<(bool Success, string? Error)> UpdateUserRoleAsync(
        Guid userId, string role)
        {
            if (!AllowedRoles.Contains(role))
                return (false, $"Invalid role. Allowed roles: {string.Join(", ", AllowedRoles)}");

            var user = await _db.Users.FindAsync(userId);
            if (user is null)
                return (false, "User not found.");

            var previousRole = user.Role;
            user.AssignRole(role);
            await _db.SaveChangesAsync();

            _logger.LogInformation(
                "Role updated for {Email}: {PreviousRole} → {NewRole}",
                user.Email, previousRole, role);

            return (true, null);
        }

        public async Task<(AuthResponse?, string?)> RefreshTokenAsync(string token)
        {
            var refreshToken = await _db.RefreshTokens
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.Token == token);

            if (refreshToken is null || !refreshToken.IsActive)
                return (null, "Invalid or expired refresh token.");

            // Rotate the refresh token
            refreshToken.Revoke("Token rotation");

            var newRefreshToken = RefreshToken.Create(refreshToken.UserId);
            _db.RefreshTokens.Add(newRefreshToken);
            await _db.SaveChangesAsync();

            return (BuildAuthResponse(refreshToken.User, newRefreshToken.Token), null);
        }

        public async Task<bool> LogoutAsync(string token)
        {
            var refreshToken = await _db.RefreshTokens
                .FirstOrDefaultAsync(r => r.Token == token);

            if (refreshToken is null) return false;

            refreshToken.Revoke("Logout");
            await _db.SaveChangesAsync();
            return true;
        }

        private AuthResponse BuildAuthResponse(AppUser user, string refreshToken)
        {
            var accessToken = _tokenService.GenerateAccessToken(user);
            return new AuthResponse(
                AccessToken: accessToken,
                RefreshToken: refreshToken,
                AccessTokenExpiry: DateTime.UtcNow.AddMinutes(15),
                User: new UserDto(user.Id, user.Email, user.FirstName, user.LastName, user.Role)
            );
        }
    }
}
