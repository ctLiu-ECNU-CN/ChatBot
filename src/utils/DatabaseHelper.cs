using ConsoleApp1.config;
using ConsoleApp1.models;
using ConsoleApp1.utils;
using Microsoft.Extensions.Configuration;

public sealed class DatabaseHelper
{
    #region 单例实现
    private static readonly Lazy<DatabaseHelper> _instance = 
        new Lazy<DatabaseHelper>(() => new DatabaseHelper());

    public static DatabaseHelper Instance => _instance.Value;

    private DatabaseHelper() 
    {
        // 默认使用当前目录下的配置文件
        if (string.IsNullOrEmpty(_configFilePath))
        {
            _configFilePath = Path.Combine(Directory.GetCurrentDirectory(), "DbConfig.json");
        }
        else
        {
            _configFilePath = Path.Combine(_configFilePath, "DbConfig.json");
            
        }
    }
    #endregion

    private string _configFilePath;
    private DbConfig _config = new DbConfig();

    // 添加公共方法：允许在运行时设置配置文件路径
    public DatabaseHelper SetConfigPath(string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
            throw new ArgumentNullException(nameof(filePath));
            
        _configFilePath = filePath;
        LoadConfiguration(); // 重新加载配置
        return this; // 支持链式调用
    }

    // 其他方法保持不变...
    private void LoadConfiguration()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Path.GetDirectoryName(_configFilePath))
            .AddJsonFile(Path.GetFileName(_configFilePath), optional: false)
            .Build();

        _config = configuration.GetSection("Database").Get<DbConfig>() 
                  ?? throw new InvalidOperationException("无法加载数据库配置");
    }

    public JBotDbContext CreateDbContext()
    {
        return DbContextFactory.CreateDbContext(_config);
    }
}