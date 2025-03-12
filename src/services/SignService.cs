using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using ConsoleApp1.models;
using MyBot.Models;

namespace MyBot.Services
{
    public class SignService
    {
        private readonly string _signRecordsFilePath;
        private const int DailyPoints = 10; // 每日签到积分
        private const int WeeklyBonus = 50; // 连续签到一周的额外积分

        public SignService(string signRecordsFilePath)
        {
            _signRecordsFilePath = signRecordsFilePath;

            // 如果文件不存在，则创建一个空列表
            if (!File.Exists(_signRecordsFilePath))
            {
                File.WriteAllText(_signRecordsFilePath, "[]");
            }
        }

        // 获取所有签到记录
        private List<SignRecord> LoadSignRecords()
        {
            var jsonString = File.ReadAllText(_signRecordsFilePath);

            // 如果文件内容为空，则返回一个空列表
            if (string.IsNullOrWhiteSpace(jsonString))
            {
                return new List<SignRecord>();
            }

            try
            {
                return JsonSerializer.Deserialize<List<SignRecord>>(jsonString) ?? new List<SignRecord>();
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"JSON 解析失败: {ex.Message}");
                return new List<SignRecord>();
            }
        }

        // 保存签到记录
        private void SaveSignRecords(List<SignRecord> records)
        {
            var jsonString = JsonSerializer.Serialize(records, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_signRecordsFilePath, jsonString);
        }

        // 用户签到
        public string Sign(string userId)
        {
            var records = LoadSignRecords();

            // 检查用户是否已经签到过
            var today = DateTime.Today;
            var userRecord = records.FirstOrDefault(r => r.UserId == userId);

            if (userRecord != null && userRecord.SignTime.Date == today)
            {
                return "今日已签到，请明天再来！";
            }

            // 统计当天签到人数
            int todaySignCount = records.Count(r => r.SignTime.Date == today);

            // 计算连续签到天数
            int consecutiveDays = 1;
            if (userRecord != null && userRecord.SignTime.Date == today.AddDays(-1))
            {
                consecutiveDays = userRecord.ConsecutiveDays + 1;
            }

            // 计算积分
            int points = DailyPoints;
            if (consecutiveDays % 7 == 0)
            {
                points += WeeklyBonus; // 连续签到一周，额外奖励
            }

            // 更新或添加签到记录
            if (userRecord == null)
            {
                userRecord = new SignRecord
                {
                    UserId = userId,
                    SignTime = today,
                    TotalPoints = points,
                    ConsecutiveDays = consecutiveDays
                };
                records.Add(userRecord);
            }
            else
            {
                userRecord.SignTime = today;
                userRecord.TotalPoints += points;
                userRecord.ConsecutiveDays = consecutiveDays;
            }

            // 保存记录
            SaveSignRecords(records);

            return $"签到成功！获得 {points} 积分。当前总积分：{userRecord.TotalPoints}，连续签到 {consecutiveDays} 天。今天是第 {todaySignCount + 1} 个签到的用户。";
        }
    }
}