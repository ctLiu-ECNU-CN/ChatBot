using System;
using System.Data;
using MySql.Data.MySqlClient;

namespace MyBot.Services
{
    public class SignService
    {
        private readonly DatabaseService _dbService;
        private const int DailyPoints = 10;
        private const int WeeklyBonus = 50;

        public SignService(DatabaseService dbService)
        {
            _dbService = dbService;
        }

        public string Sign(string userId)
        {
            using var conn = _dbService.CreateConnection();
            conn.Open();
            using var transaction = conn.BeginTransaction();
            try
            {
                if (HasSignedToday(userId, conn, transaction))
                    return "今日已签到，请明天再来！";

                var (currentPoints, consecutiveDays) = GetUserStats(userId, conn, transaction);
                consecutiveDays = CalculateConsecutiveDays(userId, consecutiveDays, conn, transaction);
                int points = CalculatePoints(consecutiveDays);

                UpdateUserRecord(userId, points, consecutiveDays, conn, transaction);
                LogSignActivity(userId, points, conn, transaction);

                int todaySignCount = GetTodaySignCount(conn, transaction);
                transaction.Commit();

                return $"签到成功！获得 {points} 积分（总积分：{currentPoints + points}），" +
                       $"连续签到 {consecutiveDays} 天。今日第 {todaySignCount} 位签到用户。";
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                Console.WriteLine($"[签到异常] {ex}");
                return "签到失败，请稍后再试或联系管理员";
            }
        }

        private bool HasSignedToday(string userId, MySqlConnection conn, MySqlTransaction transaction)
        {
            const string query = @"
                SELECT COUNT(*) 
                FROM sign_logs 
                WHERE user_id = @userId 
                AND DATE(sign_time) = CURDATE()";

            return _dbService.ExecuteScalar<int>(conn, transaction, query, new MySqlParameter("@userId", userId)
            ) > 0;
        }

        private (int points, int days) GetUserStats(string userId, MySqlConnection conn, MySqlTransaction transaction)
        {
            const string query = @"
                SELECT total_points, consecutive_days 
                FROM users 
                WHERE user_id = @userId 
                FOR UPDATE";

            using var reader = _dbService.ExecuteReader(conn, transaction, query, new MySqlParameter("@userId", userId)
            );
            if (reader.Read())
            {
                return (
                    reader.GetInt32("total_points"),
                    reader.GetInt32("consecutive_days")
                );
            }

            reader.Close();
            InitializeNewUser(userId, conn, transaction);

            using var newReader = _dbService.ExecuteReader(conn, transaction, query, new MySqlParameter("@userId", userId)
            );
            newReader.Read();
            return (
                newReader.GetInt32("total_points"),
                newReader.GetInt32("consecutive_days")
            );
        }

        private void InitializeNewUser(string userId, MySqlConnection conn, MySqlTransaction transaction)
        {
            const string insertQuery = @"
                INSERT INTO users (user_id, registration_date, last_login) 
                VALUES (@userId, NOW(), NOW())";

            _dbService.ExecuteNonQuery(conn, transaction, insertQuery, new MySqlParameter("@userId", userId)
            );
        }

        private int CalculateConsecutiveDays(string userId, int currentDays, MySqlConnection conn, MySqlTransaction transaction)
        {
            const string query = @"
                SELECT 
                    CASE 
                        WHEN MAX(DATE(sign_time)) = CURDATE() - INTERVAL 1 DAY THEN @currentDays + 1 
                        ELSE 1 
                    END
                FROM sign_logs 
                WHERE user_id = @userId";

            return _dbService.ExecuteScalar<int>(conn, transaction, query, 
                new("@userId", userId),
                new("@currentDays", currentDays));
        }

        private int CalculatePoints(int consecutiveDays)
        {
            return consecutiveDays % 7 == 0 ? DailyPoints + WeeklyBonus : DailyPoints;
        }

        private void UpdateUserRecord(string userId, int points, int days, MySqlConnection conn, MySqlTransaction transaction)
        {
            const string updateQuery = @"
                INSERT INTO users 
                    (user_id, total_points, consecutive_days, last_login)
                VALUES 
                    (@userId, @points, @days, NOW())
                ON DUPLICATE KEY UPDATE
                    total_points = total_points + VALUES(total_points),
                    consecutive_days = VALUES(consecutive_days),
                    last_login = VALUES(last_login)";

            _dbService.ExecuteNonQuery(conn, transaction, updateQuery,
                new("@userId", userId),
                new("@points", points),
                new("@days", days));
        }

        private void LogSignActivity(string userId, int points, MySqlConnection conn, MySqlTransaction transaction)
        {
            const string logQuery = @"
                INSERT INTO sign_logs (user_id, sign_time, points_awarded) 
                VALUES (@userId, NOW(), @points)";

            _dbService.ExecuteNonQuery(conn, transaction, logQuery,
                new("@userId", userId),
                new("@points", points));
        }

        private int GetTodaySignCount(MySqlConnection conn, MySqlTransaction transaction)
        {
            const string countQuery = @"
                SELECT COUNT(DISTINCT user_id) 
                FROM sign_logs 
                WHERE DATE(sign_time) = CURDATE()";

            return _dbService.ExecuteScalar<int>(conn, transaction, countQuery);
        }
    }

    public class DatabaseService
    {
        private readonly string _connectionString;

        public DatabaseService(string connectionString)
        {
            _connectionString = connectionString + ";Pooling=true;Max Pool Size=100;";
        }

        public MySqlConnection CreateConnection()
        {
            return new MySqlConnection(_connectionString);
        }

        public int ExecuteNonQuery(MySqlConnection conn, MySqlTransaction transaction, string sql, params MySqlParameter[] parameters)
        {
            using var cmd = new MySqlCommand(sql, conn, transaction);
            cmd.Parameters.AddRange(parameters);
            return cmd.ExecuteNonQuery();
        }

        public T ExecuteScalar<T>(MySqlConnection conn, MySqlTransaction transaction, string sql, params MySqlParameter[] parameters)
        {
            using var cmd = new MySqlCommand(sql, conn, transaction);
            cmd.Parameters.AddRange(parameters);
            return (T)Convert.ChangeType(cmd.ExecuteScalar(), typeof(T));
        }

        public MySqlDataReader ExecuteReader(MySqlConnection conn, MySqlTransaction transaction, string sql, params MySqlParameter[] parameters)
        {
            var cmd = new MySqlCommand(sql, conn, transaction);
            cmd.Parameters.AddRange(parameters);
            return cmd.ExecuteReader(); // 调用方需手动Close
        }
    }
}