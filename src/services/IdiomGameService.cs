using System.Text;
using System.Text.Json;
using ConsoleApp1.models;
using MyBot.Api;

public class IdiomGameService
{
    private readonly Dictionary<string, List<Idiom>> _idiomDict;
    private string? _lastWord;
    private static readonly Random _random = new Random();
    private Dictionary<string, int> _userChains; // 用户接龙的字典
    private string _recordFilePath;
    private List<IdiomGameRecord> _records;
    private static int msgSeq = 1371;

    public IdiomGameService(List<Idiom> idiomsList, string recordFilePath)
    {
        _idiomDict = new Dictionary<string, List<Idiom>>();
        _userChains = new Dictionary<string, int>(); // 用来存储用户的接龙状态
        _recordFilePath = recordFilePath;
        _records = LoadRecords();

        foreach (var idiom in idiomsList)
        {
            if (!_idiomDict.ContainsKey(idiom.First))
            {
                _idiomDict[idiom.First] = new List<Idiom>();
            }
            _idiomDict[idiom.First].Add(idiom);
        }
    }

    // 启动成语接龙游戏
    public async Task StartGameAsync(ChatMessageApi api, string groupOpenId, string messageId)
    {
        _lastWord = null; // 重置游戏状态
        _userChains.Clear(); // 清空用户接龙记录

        await api.SendGroupMessageAsync(groupOpenId, 
            "成语接龙开始！请使用 /我接 成语 来接第一个成语！",
            passiveMsgId: messageId,msgSeq:
            (msgSeq++)%7859);
    }

    // 结束当前游戏
    public async Task EndGameAsync(ChatMessageApi api, string groupOpenId, string messageId)
    {
        _lastWord = null;
        _userChains.Clear();

        await api.SendGroupMessageAsync(groupOpenId, 
            "成语接龙游戏已结束！", 
            passiveMsgId: messageId,msgSeq:
            (msgSeq++)%7859);
    }



    // 处理用户发送的消息，接龙的逻辑
    public async Task HandleUserMessageAsync(ChatMessageApi api, string groupOpenId, string messageId, string userMessage, string userId, string userName, List<Idiom> idiomsList)
{
    if (userMessage.StartsWith("/我接"))
    {
        // 提取用户输入的成语
        var parts = userMessage.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length < 2)
        {
            // 如果没有输入成语，提示用户
            await api.SendGroupMessageAsync(groupOpenId, "请输入要接的成语，例如：/我接 成语", passiveMsgId: messageId,msgSeq:
                (msgSeq++)%7859);
            return;
        }

        var userIdiom = parts[1].Trim(); // 获取用户输入的成语

        // 校验输入的成语是否合法
        var matchedIdiom = idiomsList.FirstOrDefault(i => i.Word == userIdiom);
        if (matchedIdiom == null)
        {
            await api.SendGroupMessageAsync(groupOpenId, "这不是一个有效的成语，请重新输入!", passiveMsgId: messageId,msgSeq:
                (msgSeq++)%7859);
            return;
        }

        // 如果这是第一个成语
        if (_lastWord == null)
        {
            _lastWord = matchedIdiom.Last; // 设置 _lastWord 为第一个成语的最后一个字

            await api.SendGroupMessageAsync(groupOpenId,
                $"好！你先来：{matchedIdiom.Word} ({matchedIdiom.Pinyin})，轮到我了！",
                passiveMsgId: messageId,msgSeq:
                (msgSeq++)%7859);

            await TrySendNextIdiomAsync(api, groupOpenId, messageId);
        }
        else
        {
            // 如果 _lastWord 不为 null，说明已经开始了接龙，检查接龙规则
            if (matchedIdiom.First != _lastWord)
            {
                await api.SendGroupMessageAsync(groupOpenId, $"成语不符合接龙规则！当前应该以 `{_lastWord}` 开头的成语！", passiveMsgId: messageId,msgSeq:
                    (msgSeq++)%7859);
                return;
            }

            // 合法，继续游戏
            _lastWord = matchedIdiom.Last;

            UpdateRecord(userId, userName, _userChains.ContainsKey(userId) ? _userChains[userId] : 0);

            await api.SendGroupMessageAsync(groupOpenId,
                $"不错！你接：{matchedIdiom.Word} ({matchedIdiom.Pinyin})，该我啦！",
                passiveMsgId: messageId,msgSeq:
                (msgSeq++)%7859);

            if (!_userChains.ContainsKey(userId))
            {
                _userChains[userId] = 1;
            }
            else
            {
                _userChains[userId]++;
            }

            await TrySendNextIdiomAsync(api, groupOpenId, messageId);
        }
    }
    else if (userMessage.StartsWith("/成语接龙"))
    {
        await StartGameAsync(api, groupOpenId, messageId);
    }
    else if (userMessage.StartsWith("/成语结束"))
    {
        await EndGameAsync(api, groupOpenId, messageId);
    }
    else if (userMessage.StartsWith("/成语排行榜"))
    {
        var leaderboard = GetLeaderboard();
        await api.SendGroupMessageAsync(groupOpenId, leaderboard, passiveMsgId: messageId,msgSeq:
            (msgSeq++)%7859);
    }
    else
    {
        await api.SendGroupMessageAsync(groupOpenId, "请使用 `/我接 成语` 来接龙！", passiveMsgId: messageId,msgSeq:
            (msgSeq++)%7859);
    }
}

    // 其他已实现的方法（如更新记录、获取下一个成语等）


    private async Task TrySendNextIdiomAsync(ChatMessageApi api, string groupOpenId, string messageId)
    {
        if (_idiomDict.TryGetValue(_lastWord, out var nextIdioms) && nextIdioms.Count > 0)
        {
            var nextIdiom = nextIdioms[_random.Next(nextIdioms.Count)];
            try
            {
                _lastWord = nextIdiom.Last; // 记录接龙的最后一个字
                await api.SendGroupMessageAsync(groupOpenId,
                    $"我接: {nextIdiom.Word}({nextIdiom.Pinyin})，请继续!\n出处:{nextIdiom.Derivation} \n解释:{nextIdiom.Explanation}",
                    passiveMsgId: messageId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"发送失败: {ex.Message}");
                await api.SendGroupMessageAsync(groupOpenId,
                    "我尝试了好几个成语，但好像都不能说噢 😭",
                    passiveMsgId: messageId);
            }
        }
        else
        {
            _lastWord = null;
            await api.SendGroupMessageAsync(groupOpenId, "我接不上了，成语接龙结束!", passiveMsgId: messageId);
        }
    }

    private List<IdiomGameRecord> LoadRecords()
    {
        if (!File.Exists(_recordFilePath))
        {
            return new List<IdiomGameRecord>();
        }

        var jsonString = File.ReadAllText(_recordFilePath);
        return JsonSerializer.Deserialize<List<IdiomGameRecord>>(jsonString) ?? new List<IdiomGameRecord>();
    }

    private void SaveRecords()
    {
        var jsonString = JsonSerializer.Serialize(_records, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_recordFilePath, jsonString);
    }

    private void UpdateRecord(string userId, string userName, int chainLength)
    {
        var record = _records.FirstOrDefault(r => r.UserId == userId);
        if (record == null)
        {
            record = new IdiomGameRecord
            {
                UserId = userId,
                UserName = userName,
                TotalGames = 1,
                LongestChain = chainLength
            };
            _records.Add(record);
        }
        else
        {
            record.TotalGames++;
            if (chainLength > record.LongestChain)
            {
                record.LongestChain = chainLength;
            }
        }

        SaveRecords();
    }

    public string GetLeaderboard()
    {
        var sortedRecords = _records
            .OrderByDescending(r => r.LongestChain)
            .ThenByDescending(r => r.TotalGames)
            .Take(10)
            .ToList();

        if (sortedRecords.Count == 0)
        {
            return "暂无排行榜数据。";
        }

        var leaderboard = new StringBuilder("成语接龙排行榜：\n");
        for (int i = 0; i < sortedRecords.Count; i++)
        {
            var record = sortedRecords[i];
            leaderboard.AppendLine($"{i + 1}. {record.UserName} - 最长接龙：{record.LongestChain} 个成语，总接龙次数：{record.TotalGames}");
        }

        return leaderboard.ToString();
    }
}
