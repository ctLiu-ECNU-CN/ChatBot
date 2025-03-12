using ConsoleApp1.models;

namespace ConsoleApp1.utils;

using System;
using System.IO;
using System.Text.Json;

public static class ConfigLoader
{
    public static BotData LoadBotConfig(string configFilePath)
    {
        if (!File.Exists(configFilePath))
        {
            throw new FileNotFoundException("配置文件 bot.json 未找到!");
        }

        var jsonString = File.ReadAllText(configFilePath);
        var botData = JsonSerializer.Deserialize<BotData>(jsonString);

        if (botData == null)
        {
            throw new InvalidOperationException("解析 bot.json 失败!");
        }

        return botData;
    }

    public static List<Idiom> LoadIdiomsConfig(string idiomsFilePath)
    {
        if (!File.Exists(idiomsFilePath))
        {
            throw new FileNotFoundException("成语文件 idioms.json 未找到!");
        }

        var idiomsJson = File.ReadAllText(idiomsFilePath);
        var idiomsList = JsonSerializer.Deserialize<List<Idiom>>(idiomsJson);

        if (idiomsList == null || idiomsList.Count == 0)
        {
            throw new InvalidOperationException("解析 idioms.json 失败或数据为空!");
        }

        return idiomsList;
    }
}