using CaptivePortal.Database;
using CaptivePortal.Database.Entities;
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
            
            string? hash = await db.Persons
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
            CancellationToken cancellationToken = default)
        {
            if (email is null || oldPassword is null || newPassword is null) return false;
            
            Person? person = await db.Persons
                .AsNoTracking()
                .Where(x => x.Email == email)
                .FirstOrDefaultAsync(cancellationToken);
            if (person is null) return false;

            if (!Sodium.PasswordHash.ArgonHashStringVerify(person.Hash, oldPassword)) return false;

            string newHash = GetHash(newPassword);

            int modified = await db.Persons
                .Where(x => x.Email == email)
                .ExecuteUpdateAsync(x => x
                    .SetProperty(p => p.Hash, newHash)
                , cancellationToken);

            return (modified > 0); // TODO error reporting if modifying more than 1
        }

        public string GetHash(string password)
        {
            return Sodium.PasswordHash.ArgonHashString(password, strength);
        }
    }
}
