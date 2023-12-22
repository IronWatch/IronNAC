using CaptivePortal.Database;
using CaptivePortal.Database.Entities;
using CaptivePortal.Models;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.EntityFrameworkCore;

namespace CaptivePortal.Services
{
    public class WebAuthenticationService
    {
        private readonly CaptivePortalDbContext db;
        private readonly Sodium.PasswordHash.StrengthArgon strength = Sodium.PasswordHash.StrengthArgon.Interactive;

        public WebAuthenticationService(
            CaptivePortalDbContext db)
        {
            this.db = db;
        }
        
        public async Task<bool> ValidateLoginAsync(
            string? email, 
            string? password, 
            CancellationToken cancellationToken = default)
        {
            if (email is null || password is null) return false;
            
            string? hash = await db.Users
                .AsNoTracking()
                .Where(x => x.Email == email)
                .Select(x => x.Hash)
                .FirstOrDefaultAsync(cancellationToken);

            if (hash is null) return false;

            return Sodium.PasswordHash.ArgonHashStringVerify(hash, password);
        }

        public async Task<bool> ChangePasswordAsync(
            string? email,
            string? oldPassword,
            string? newPassword,
            bool changePasswordNextLogin = false,
            CancellationToken cancellationToken = default)
        {
            if (email is null || oldPassword is null || newPassword is null) return false;
            
            User? user = await db.Users
                .AsNoTracking()
                .Where(x => x.Email == email)
                .FirstOrDefaultAsync(cancellationToken);
            if (user is null) return false;

            if (!Sodium.PasswordHash.ArgonHashStringVerify(user.Hash, oldPassword)) return false;

            return await SetPasswordAsync(email, newPassword, changePasswordNextLogin, cancellationToken);
        }

        public async Task<bool> SetPasswordAsync(
            string? email,
            string? password,
            bool changePasswordNextLogin = false,
            CancellationToken cancellationToken = default)
        {
            if (email is null || password is null) return false;

            string newHash = GetHash(password);

            int modified = await db.Users
                .Where(x => x.Email == email)
                .ExecuteUpdateAsync(x => x
                    .SetProperty(p => p.Hash, newHash)
                    .SetProperty(p => p.ChangePasswordNextLogin, changePasswordNextLogin)
                , cancellationToken);

            return (modified > 0); // TODO error reporting if modifying more than 1
        }

        public string GetHash(string password)
        {
            return Sodium.PasswordHash.ArgonHashString(password, strength);
        }

        public async Task<AccessToken?> WebLoginAsync(ProtectedLocalStorage protectedLocalStorage, string? email, string? password, CancellationToken cancellationToken = default)
        {
            bool valid = await ValidateLoginAsync(email, password);
            if (!valid) return null;

            User? user = await db.Users
                .AsNoTracking()
                .Where(x => x.Email == email)
                .FirstOrDefaultAsync();
            if (user is null) return null;

            AccessToken accessToken = new()
            {
                UserId = user.Id,
                Name = user.Name,
                Email = user.Email,
                IsStaff = user.IsStaff,
                IsAdmin = user.IsAdmin,
                IssuedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(5),
                RefreshToken = Guid.NewGuid(),
                RefreshTokenIssuedAt = DateTime.UtcNow,
                RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(1)
            };

            UserSession session = new()
            {
                UserId = user.Id,
                RefreshToken = accessToken.RefreshToken,
                RefreshTokenIssuedAt = accessToken.RefreshTokenIssuedAt,
                RefreshTokenExpiresAt = accessToken.RefreshTokenExpiresAt
            };

            db.Add(session);
            await db.SaveChangesAsync(cancellationToken);

            await protectedLocalStorage.SetAsync(nameof(AccessToken), accessToken);

            return accessToken;
        }

        public async Task<AccessToken?> WebCheckLoggedInAsync(ProtectedLocalStorage protectedLocalStorage, CancellationToken cancellationToken = default)
        {
            ProtectedBrowserStorageResult<AccessToken> plsResult 
                = await protectedLocalStorage.GetAsync<AccessToken>(nameof(AccessToken));

            if (!plsResult.Success) return null;

            AccessToken? accessToken = plsResult.Value;
            if (accessToken is null) return null;

            if (accessToken.IssuedAt > DateTime.UtcNow ||
                accessToken.ExpiresAt <= DateTime.UtcNow)
            {
                accessToken = await ExchangeRefreshTokenAsync(protectedLocalStorage, accessToken, cancellationToken);
            }

            return accessToken;
        }

        public async Task WebLogoutAsync(ProtectedLocalStorage protectedLocalStorage, CancellationToken cancellationToken = default)
        {
            await protectedLocalStorage.DeleteAsync(nameof(AccessToken));
        }

        public async Task<AccessToken?> ExchangeRefreshTokenAsync(ProtectedLocalStorage protectedLocalStorage, AccessToken oldAccessToken, CancellationToken cancellationToken = default)
        {
            if (oldAccessToken.RefreshTokenIssuedAt > DateTime.UtcNow ||
                oldAccessToken.RefreshTokenExpiresAt <= DateTime.UtcNow)
            {
                return null;
            }

            UserSession? userSession = await db.UserSessions
                .Include(x => x.User)
                .Where(x => x.UserId == oldAccessToken.UserId)
                .Where(x => x.RefreshToken == oldAccessToken.RefreshToken)
                .FirstOrDefaultAsync(cancellationToken);
            if (userSession is null) return null;

            if (userSession.RefreshTokenIssuedAt > DateTime.UtcNow ||
                userSession.RefreshTokenExpiresAt <= DateTime.UtcNow)
            {
                return null;
            }

            AccessToken newAccessToken = new()
            {
                UserId = userSession.UserId,
                Name = userSession.User.Name,
                Email = userSession.User.Email,
                IsStaff = userSession.User.IsStaff,
                IsAdmin = userSession.User.IsAdmin,
                IssuedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(5),
                RefreshToken = userSession.RefreshToken,
                RefreshTokenIssuedAt = userSession.RefreshTokenIssuedAt,
                RefreshTokenExpiresAt = userSession.RefreshTokenExpiresAt
            };

            await protectedLocalStorage.SetAsync(nameof(AccessToken), newAccessToken);

            return newAccessToken;
        }
    }
}
