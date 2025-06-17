using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ConsoleApp1.models;
using MyBot.Api;
using MyBot.Models;

namespace MyBot.Services
{
    public class IdiomGameService
    {
        private readonly Dictionary<string, List<Idiom>> _idiomDict;
        private string? _lastWord;
        private static readonly Random _random = new Random(); // 随机数生成器

        public IdiomGameService(List<Idiom> idiomsList)
        {
            _idiomDict = new Dictionary<string, List<Idiom>>();
            foreach (var idiom in idiomsList)
            {
                if (!_idiomDict.ContainsKey(idiom.First))
                {
                    _idiomDict[idiom.First] = new List<Idiom>();
                }
                _idiomDict[idiom.First].Add(idiom);
            }
        }

        public async Task StartGameAsync(ChatMessageApi api, string groupOpenId, string messageId)
        {
            _lastWord = null; // 重置游戏状态
            await api.SendGroupMessageAsync(groupOpenId, "成语接龙开始！请发送第一个成语:", passiveMsgId: messageId);
        }

        public async Task HandleUserMessageAsync(ChatMessageApi api, string groupOpenId, string messageId, string userMessage, List<Idiom> idiomsList)
        {
            if (_lastWord == null)
            {
                // 检查用户输入的第一个成语是否有效
                var matchedIdiom = idiomsList.FirstOrDefault(i => i.Word == userMessage);
                if (matchedIdiom == null)
                {
                    await api.SendGroupMessageAsync(groupOpenId, "这不是一个有效的成语，请重新输入!", passiveMsgId: messageId);
                    return;
                }

                // 记录第一个成语的最后一个字
                _lastWord = matchedIdiom.Last;
                await TrySendNextIdiomAsync(api, groupOpenId, messageId);
            }
            else
            {
                // 检查用户输入的成语是否符合接龙规则
                var matchedIdiom = idiomsList.FirstOrDefault(i => i.Word == userMessage);
                if (matchedIdiom == null)
                {
                    await api.SendGroupMessageAsync(groupOpenId, "这不是一个有效的成语，请重新输入!", passiveMsgId: messageId);
                    return;
                }

                if (matchedIdiom.First != _lastWord)
                {
                    await api.SendGroupMessageAsync(groupOpenId, "你的成语不符合接龙规则!", passiveMsgId: messageId);
                    return;
                }

                // 记录新的 lastWord
                _lastWord = matchedIdiom.Last;
                await TrySendNextIdiomAsync(api, groupOpenId, messageId);
            }
        }

        private async Task TrySendNextIdiomAsync(ChatMessageApi api, string groupOpenId, string messageId)
        {
            if (_idiomDict.TryGetValue(_lastWord, out var nextIdioms) && nextIdioms.Count > 0)
            {
                // 从符合条件的成语列表中随机选择一个
                var nextIdiom = nextIdioms[_random.Next(nextIdioms.Count)];

                try
                {
                    _lastWord = nextIdiom.Last; // 记录接龙的最后一个字
                    Console.WriteLine($"机器人尝试接的成语: {nextIdiom.Word}");

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
        private readonly string _recordFilePath; // 记录文件路径
private List<IdiomGameRecord> _records; // 用户表现记录

public IdiomGameService(List<Idiom> idiomsList, string recordFilePath)
{
    _idiomDict = new Dictionary<string, List<Idiom>>();
    foreach (var idiom in idiomsList)
    {
        if (!_idiomDict.ContainsKey(idiom.First))
        {
            _idiomDict[idiom.First] = new List<Idiom>();
        }
        _idiomDict[idiom.First].Add(idiom);
    }

    _recordFilePath = recordFilePath;
    _records = LoadRecords();
}

// 加载用户表现记录
private List<IdiomGameRecord> LoadRecords()
{
    if (!File.Exists(_recordFilePath))
    {
        return new List<IdiomGameRecord>();
    }

    var jsonString = File.ReadAllText(_recordFilePath);
    return JsonSerializer.Deserialize<List<IdiomGameRecord>>(jsonString) ?? new List<IdiomGameRecord>();
}

// 保存用户表现记录
private void SaveRecords()
{
    var jsonString = JsonSerializer.Serialize(_records, new JsonSerializerOptions { WriteIndented = true });
    File.WriteAllText(_recordFilePath, jsonString);
}

// 更新用户表现记录
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

// 获取排行榜
public string GetLeaderboard()
{
    var sortedRecords = _records
        .OrderByDescending(r => r.LongestChain)
        .ThenByDescending(r => r.TotalGames)
        .Take(10) // 取前10名
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
}