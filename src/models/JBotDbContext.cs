using ConsoleApp1.Models;
using Microsoft.EntityFrameworkCore;



namespace ConsoleApp1.models
{

    public class JBotDbContext : DbContext
    {
        public JBotDbContext(DbContextOptions<JBotDbContext> options)
            : base(options)
        {
        }

        public DbSet<BotUser> BotUsers { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // 配置实体
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(JBotDbContext).Assembly);

            // 其他配置...
        }
    }
}
