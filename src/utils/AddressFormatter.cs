using System.Collections.Generic;
using System.Linq;
using ConsoleApp1.SakuraFrp;

namespace ConsoleApp1.Utils
{
    public static class AddressFormatter
    {
        /// <summary>
        /// 将穿透地址列表中的隧道名称替换为友好名称，并过滤掉不在映射中的隧道
        /// </summary>
        /// <param name="tunnelAddresses">原始隧道地址列表</param>
        /// <param name="friendlyNameMappings">友好名称映射（键：原始隧道名，值：友好名称）</param>
        /// <returns>替换并过滤后的地址列表</returns>
        public static List<string> FormatAndFilterAddresses(
            List<TunnelAddress> tunnelAddresses,
            Dictionary<string, string> friendlyNameMappings)
        {
            if (tunnelAddresses == null || !tunnelAddresses.Any())
                return new List<string>();
                
            // 过滤并替换隧道名称
            return tunnelAddresses
                .Where(a => friendlyNameMappings.ContainsKey(a.TunnelName))
                .Select(a => $"{friendlyNameMappings[a.TunnelName]}:{a.Address}")
                .ToList();
        }
        

    }
}