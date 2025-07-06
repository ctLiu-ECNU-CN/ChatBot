namespace ConsoleApp1.config
{

    /// <summary>
    /// 数据库配置实体
    /// </summary>
    public class DbConfig
    {
        /// <summary>
        /// 数据库连接字符串
        /// </summary>
        public string ConnectionString { get; set; } = string.Empty;

        /// <summary>
        /// 数据库提供程序（MySQL 或 SQLite）
        /// </summary>
        public string Provider { get; set; } = "MySQL";

        /// <summary>
        /// 是否启用 EF Core 日志记录
        /// </summary>
        public bool LoggingEnabled { get; set; } = false;

        /// <summary>
        /// 数据库操作失败时的最大重试次数
        /// </summary>
        public int MaxRetryCount { get; set; } = 3;

        /// <summary>
        /// 数据库操作失败时的最大重试延迟
        /// </summary>
        public TimeSpan MaxRetryDelay { get; set; } = TimeSpan.FromSeconds(30);
    }
}