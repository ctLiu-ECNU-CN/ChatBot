using ConsoleApp1.Models;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using ConsoleApp1.models;

namespace ConsoleApp1.Services
{
    /// <summary>
    /// 签到服务类，处理用户签到逻辑
    /// </summary>
    public class SignService
    {
        private readonly JBotDbContext _dbContext;

        /// <summary>
        /// 构造函数注入数据库上下文
        /// </summary>
        /// <param name="dbContext">数据库上下文实例</param>
        public SignService(JBotDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        /// <summary>
        /// 处理用户签到逻辑
        /// </summary>
        /// <param name="userId">用户ID（对应OpenGroupID）</param>
        /// <param name="nickname">用户昵称（首次签到时必填）</param>
        /// <param name="region">用户地区（可选）</param>
        /// <returns>签到结果消息</returns>
        public async Task<string> ProcessSignAsync(string userId)
        {
            // 1. 检查用户是否已存在于数据库
            var existingUser = await _dbContext.BotUsers
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (existingUser != null)
            {
                // 2. 用户已存在：更新最后活跃时间并返回问候消息
                existingUser.LastActiveTime = DateTime.Now;
                await _dbContext.SaveChangesAsync();
                if (string.IsNullOrWhiteSpace(existingUser.Nickname))
                {
                return $"欢迎回来！今天也要元气满满哦~, 可以绑定昵称噢！";
                    
                }
                return $"欢迎回来，{existingUser.Nickname}！今天也要元气满满哦~";
            }
            else
            {
                var newUser = new BotUser
                {
                    Id = userId,
                    LastActiveTime = DateTime.Now,
                    // CreatedAt 会由数据库自动生成（DEFAULT CURRENT_TIMESTAMP）
                };

                _dbContext.BotUsers.Add(newUser);
                await _dbContext.SaveChangesAsync();
                return $"🎉 欢迎新用户 完成首次签到！已为你注册账号~";
            }
        }
    }
}