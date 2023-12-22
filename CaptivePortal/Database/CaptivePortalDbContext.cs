using CaptivePortal.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace CaptivePortal.Database
{
    public class CaptivePortalDbContext : DbContext
    {
        public DbSet<Person> Persons { get; set; }

        public string DatabaseFilePath { get; private set; }

        public CaptivePortalDbContext()
        {
            var folder = Environment.SpecialFolder.LocalApplicationData;
            var path = Environment.GetFolderPath(folder);
            DatabaseFilePath = Path.Join(path, "captive-portal.db");
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            => optionsBuilder.UseSqlite($"Data Source={DatabaseFilePath}");
    }
}
