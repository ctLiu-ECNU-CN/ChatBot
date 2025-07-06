

using System.IO;
using System.Linq;

namespace ConsoleApp1.utils
{
    public static class FileHelper
    {
        /// <summary>
        /// 获取指定目录中最新修改的日志文件
        /// </summary>
        public static string GetLatestLogFile(string directoryPath, string filePattern = "*.log")
        {
            if (!Directory.Exists(directoryPath))
            {
                throw new DirectoryNotFoundException($"日志目录不存在: {directoryPath}");
            }

            // 获取所有匹配的日志文件，并按最后修改时间排序
            var logFiles = Directory.GetFiles(directoryPath, filePattern)
                .OrderByDescending(f => new FileInfo(f).LastWriteTime)
                .ToList();

            if (logFiles.Count == 0)
            {
                throw new FileNotFoundException($"在目录 {directoryPath} 中未找到匹配的日志文件");
            }

            return logFiles[0];
        }
    }
}