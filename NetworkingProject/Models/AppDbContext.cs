using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data.Entity;
namespace NetworkingProject.Models
{
    public class AppDbContext : DbContext
    {
        public AppDbContext() : base("name=NetProj_Web_db") { }

        public DbSet<BookModel> Books { get; set; }

        public System.Data.Entity.DbSet<NetworkingProject.Models.LibraryModel> LibraryModels { get; set; }
    }
}