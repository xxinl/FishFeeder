using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace FeedControl
{
  public class DataContext : DbContext
  {
    public DbSet<Setting> Settings { get; set; }
    public DbSet<FeedLog> FeedLogs { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
      optionsBuilder.UseSqlite("Filename=./db.db");
    }
  }
}
