using ConsoleApp1.models;
using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Collections.Generic;

public class OllamaService
{
    private readonly HttpClient _httpClient;
    private const string ApiKey = "sk-6a9e4828b8ec468f9b9886afb1c3fbc7"; // 替换为你的 API Key

    public OllamaService()
    {
        _httpClient = new HttpClient();
        _httpClient.BaseAddress = new Uri("https://chat.ecnu.edu.cn/open/api/v1/"); // 设置新的 API 地址
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {ApiKey}"); // 认证
    }

    public async Task<string> GetReplyAsync(string prompt)
    {
        var request = new
        {
            model = "ecnu-max", // 设定模型
            stream = false,
            messages = new List<object>
            {
                new { role = "system", content = "" },

                new { role = "user", content = prompt }
            }
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("chat/completions", content);
        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"ChatECNU API 请求失败: {response.StatusCode}");
        }

        var responseJson = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"请求内容: {json}");
        Console.WriteLine($"响应内容: {responseJson}");

        // 解析 JSON 响应
        try
        {
            var chatResponse = JsonSerializer.Deserialize<ChatECNUResponse>(responseJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true // 允许匹配不同大小写
            });

            return chatResponse?.Choices?[0]?.Message?.Content ?? "抱歉，我无法理解你的问题。";
        }
        catch (Exception ex)
        {
            Console.WriteLine($"JSON 解析错误: {ex.Message}");
            return "抱歉，解析响应时出错。";
        }
    }
}


