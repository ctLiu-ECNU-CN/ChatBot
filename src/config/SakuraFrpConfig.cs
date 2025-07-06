namespace ConsoleApp1.config
{
    public class SakuraFrpConfig
    {
        public string ApiKey { get; set; }
        
        // Sakura FRP的工作目录
        public string LogDirectory { get; set; }
        
        public List<string> TunnelNames { get; set; }
        
        public List<string> FriendlyNames { get; set; }
        
        // 将两个列表按索引位置映射为 Dictionary
        public Dictionary<string, string> BuildFriendlyNameMapping()
        {
            if (TunnelNames == null || FriendlyNames == null || 
                TunnelNames.Count != FriendlyNames.Count)
            {
                throw new InvalidOperationException("隧道名列表和友好名称列表必须存在且长度相同");
            }

            return TunnelNames
                .Zip(FriendlyNames, (tunnel, friendly) => new { tunnel, friendly })
                .ToDictionary(pair => pair.tunnel, pair => pair.friendly);
        }
    }
    

};

