using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;
using System;
using ConsoleApp1.config;
using ConsoleApp1.models;
using Microsoft.Extensions.Logging;

namespace ConsoleApp1.utils
{

    /// <summary>
    /// 数据库上下文工厂，用于创建和配置 DbContext
    /// </summary>
    public static class DbContextFactory
    {
        /// <summary>
        /// 创建数据库上下文
        /// </summary>
        public static JBotDbContext CreateDbContext(DbConfig config)
        {
            var services = new ServiceCollection();

            // 配置数据库上下文
            services.AddDbContext<JBotDbContext>(options =>
            {
                // 根据配置选择数据库提供程序
                if (config.Provider.Equals("MySQL", StringComparison.OrdinalIgnoreCase))
                {
                    options.UseMySql(
                        config.ConnectionString,
                        ServerVersion.AutoDetect(config.ConnectionString),
                        mySqlOptions =>
                        {
                            mySqlOptions.EnableRetryOnFailure(
                                maxRetryCount: config.MaxRetryCount,
                                maxRetryDelay: config.MaxRetryDelay,
                                errorNumbersToAdd: null);
                        });
                }
                else if (config.Provider.Equals("SQLite", StringComparison.OrdinalIgnoreCase))
                {
                    options.UseSqlite(config.ConnectionString);
                }
                else
                {
                    throw new NotSupportedException($"不支持的数据库提供程序: {config.Provider}");
                }

                // 启用日志记录（开发环境）
                if (config.LoggingEnabled)
                {
                    options.LogTo(Console.WriteLine, LogLevel.Information)
                        .EnableSensitiveDataLogging();
                }
            });

            var serviceProvider = services.BuildServiceProvider();
            return serviceProvider.GetRequiredService<JBotDbContext>();
        }
    }
}