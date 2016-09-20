using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Backend_ASPNETCORE_RTM.Models
{
    public class UserDBContext : DbContext
    {
        public DbSet<UserModel> Users { get; set; }
        public DbSet<SecretsModel> Secrets { get; set; }
        public DbSet<UsedCodesModel> UsedCodes { get; set; }


        // truefree 20160523
        // DbContext DI를 위한 empty constructor
        // 이래야 Startup.cs에서 services.AddDbContext에 써준 options이 동작한다고...
        // Onconfiguring은 없애도 될 듯?!
        public UserDBContext(DbContextOptions<UserDBContext> options) : base(options)
        {
            Database.EnsureCreated();
        }

        //protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        //{
        //    base.OnConfiguring(optionsBuilder);
        //    optionsBuilder.UseSqlite("filename=WebAPIDB.sqlite");
        //}

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Primary Key 설정
            modelBuilder.Entity<UserModel>().HasKey(p => p.internalID);
            modelBuilder.Entity<UserModel>().HasAlternateKey(p => p.loginID);

            // index
            modelBuilder.Entity<UserModel>().HasIndex(p => p.loginID);
            // GUID 자동 생성...!
            modelBuilder.Entity<UserModel>().Property(p => p.internalID).ValueGeneratedOnAdd().IsConcurrencyToken();


            modelBuilder.Entity<SecretsModel>().HasKey(p => p.no);
            modelBuilder.Entity<UsedCodesModel>().HasKey(p => p.no);
            modelBuilder.Entity<SecretsModel>().Property(p => p.no).ValueGeneratedOnAdd();
            modelBuilder.Entity<UsedCodesModel>().Property(p => p.no).ValueGeneratedOnAdd();

            //base.OnModelCreating(modelBuilder);
        }

    }
}
