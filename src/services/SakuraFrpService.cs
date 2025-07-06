// src/services/SakuraFrpService.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using ConsoleApp1.utils;

namespace ConsoleApp1.SakuraFrp
{
    public class SakuraFrpService
    {
        private readonly string _logDirectory;

        public SakuraFrpService(string logDirectory)
        {
            _logDirectory = logDirectory;
        }

        /// <summary>
        /// 从最新日志中获取所有穿透地址
        /// </summary>
        public async Task<List<TunnelAddress>> GetAllTunnelAddressesAsync()
        {
            try
            {
                // 获取最新的日志文件
                string latestLogFile = FileHelper.GetLatestLogFile(_logDirectory);
                Console.WriteLine($"读取最新日志: {latestLogFile}");

                // 读取日志内容
                string logContent = await File.ReadAllTextAsync(latestLogFile);
                //Console.Out.WriteLine(logContent);
                // 使用正则表达式提取隧道名称和地址
                var addresses = new List<TunnelAddress>();
                // 正则表达式
                var regex = new Regex(@"frpc\[([^\|]+)\|Info\] 使用 >>([^<]+)<< 连接你的隧道");
                
                foreach (Match match in regex.Matches(logContent))
                {
                    if (match.Success && match.Groups.Count >= 3)
                    {
                        addresses.Add(new TunnelAddress
                        {
                            TunnelName = match.Groups[1].Value,
                            Address = match.Groups[2].Value
                        });
                    }
                }

                // 去重（按隧道名保留最新的地址）
                return addresses
                    .GroupBy(a => a.TunnelName)
                    .Select(g => g.Last()) // 取最后出现的记录（最新的）
                    .ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"获取穿透地址失败: {ex.Message}");
                return new List<TunnelAddress>();
            }
        }
    }

    /// <summary>
    /// 穿透地址信息
    /// </summary>
    public class TunnelAddress
    {
        public string TunnelName { get; set; }
        public string Address { get; set; }
    }
}