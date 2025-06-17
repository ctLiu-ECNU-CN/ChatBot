namespace ConsoleApp1.models;

using System.Text.Json.Serialization;


// 机器人连接参数
public sealed class BotData
{
    [JsonPropertyName("id")]
    public string? BotQQ { get; init; }

    [JsonPropertyName("appId")]
    public string? BotAppId { get; init; }

    [JsonPropertyName("token")]
    public string? BotToken { get; init; }

    [JsonPropertyName("secret")]
    public string? BotSecret { get; init; }
    
    [JsonPropertyName("database")]
    public DatabaseConfig? Database { get; init; } // 修改类型
}

public class DatabaseConfig 
{
    [JsonPropertyName("Server")]
    public string Server { get; set; } = "localhost";

    [JsonPropertyName("Database")]
    public string Database { get; set; } = "texas_holdem_db";

    [JsonPropertyName("UserId")]
    public string UserId { get; set; } = "root";

    [JsonPropertyName("Password")]
    public string Password { get; set; } = "347934";

    public string ConnectionString => 
        $"Server={Server};Database={Database};Uid={UserId};Pwd={Password};CharSet=utf8mb4;SslMode=Preferred;";
}


// 成语类数据结构
public sealed class Idiom
{
    [JsonPropertyName("word")]
    public string Word { get; set; } = "";

    [JsonPropertyName("first")]
    public string First { get; set; } = "";

    [JsonPropertyName("last")]
    public string Last { get; set; } = "";

    [JsonPropertyName("derivation")]
    public string Derivation { get; set; } = "";

    [JsonPropertyName("pinyin")]
    public string Pinyin { get; set; } = "";

    [JsonPropertyName("explanation")]
    public string Explanation { get; set; } = "";
}


// 成员信息表
public class SignRecord
{
    public string UserId { get; set; } // 用户ID
    public DateTime SignTime { get; set; } // 签到时间
    public int TotalPoints { get; set; } // 总积分
    public int ConsecutiveDays { get; set; } // 连续签到天数
}

public class OllamaRequest
{
    public string Model { get; set; } = "deepseek-r1:latest"; // 使用的模型名称
    public string Prompt { get; set; } // 用户输入的内容
    public bool Stream { get; set; } = false; // 是否流式响应
}

public class OllamaResponse
{
    public string Model { get; set; } // 模型名称
    public string CreatedAt { get; set; } // 创建时间
    
    [JsonPropertyName("response")]
    public string Response { get; set; } // Ollama 的回复
    public bool Done { get; set; } // 是否完成
    public string DoneReason { get; set; } // 完成原因
    public int[] Context { get; set; } // 上下文
    public long TotalDuration { get; set; } // 总耗时
    public long LoadDuration { get; set; } // 加载耗时
    public int PromptEvalCount { get; set; } // 提示评估次数
    public long PromptEvalDuration { get; set; } // 提示评估耗时
    public int EvalCount { get; set; } // 评估次数
    public long EvalDuration { get; set; } // 评估耗时
}

// 定义 ChatECNU 响应数据结构
public class ChatECNUResponse
{
    public List<Choice> Choices { get; set; }
}

public class Choice
{
    public Message Message { get; set; }
}

public class Message
{
    public string Content { get; set; }
}

public class IdiomGameRecord
{
    public string UserId { get; set; } // 用户ID
    public string UserName { get; set; } // 用户名
    public int TotalGames { get; set; } // 总接龙次数
    public int LongestChain { get; set; } // 最长接龙记录
}

public class CosConfig
{
    /// <summary>
    /// 表示是否以 HTTPS 进行访问.
    ///</summary>
    public bool IsHttps { get; set; } = true;

    /// <summary>
    /// 是否需要调试日志的输出。
    /// </summary>
    public bool DebugLog { get; set; } = false;

    /// <summary>
    /// 表示超时的时间(秒为单位)。
    ///</summary>
    public int ExpiredTime { get; set; } = 300;

    /// <summary>
    /// 本地的 Secret ID。
    /// </summary>
    public string SecretId { get; set; } = string.Empty;

    /// <summary>/// 本地的 Secret Key.
    /// </summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    ///存储桶的区域。///</summary>public string Region { get; set;}= string.Empty;
    /// <summary>
    /// 表示存储桶的名称。
    /// </summary>
    public string Bucket { get; set; } = string.Empty;

    /// <summary>/// 表示存储桶的路径地址。图片等文件的文件名，会直接添加到该地址的后面，形成的结果 URL 就是文件的最终访问地址///地址为临时地址，临时可访问的有效时长为 1个小时。
    /// </summary>
    public string Link { get; set; } = string.Empty;

    public string Region { get; set; }
}

public class UploadedResult
{
    public string Url { get; }
    public string ETag { get; }

    public UploadedResult(string url, string eTag)
    {
        Url = url;
        ETag = eTag;
    }
}
public class StableDiffusionResponse
{
    public List<string> images { get; set; }
    public object parameters { get; set; }
    public string info { get; set; }
}

public class MessageButton
{
    public string Text { get; set; } // 按钮文本
    public string Value { get; set; } // 按钮值（回调值）
    public string Type { get; set; } = "callback"; // 按钮类型（固定为 "callback"）
}
public enum Suit { Hearts, Diamonds, Clubs, Spades }
public enum Rank { Two = 2, Three, Ace }
