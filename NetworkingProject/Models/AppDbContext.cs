using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data.Entity;
namespace NetworkingProject.Models
{
    public class AppDbContext : DbContext
    {
        public AppDbContext() : base("name=MyDbConnection2") { }

        public DbSet<BookModel> Books { get; set; }
    }
}