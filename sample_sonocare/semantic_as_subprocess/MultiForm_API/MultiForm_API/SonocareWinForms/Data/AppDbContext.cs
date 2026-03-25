using Microsoft.EntityFrameworkCore;
using SonocareWinForms.Models;
using System.IO;

namespace SonocareWinForms.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<Patient> Patients { get; set; }
        public DbSet<Report> Reports { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // Use current directory for the database file
            string dbPath = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "sonocare.db");
            optionsBuilder.UseSqlite($"Data Source={dbPath}");
        }
    }
}
