using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConsoleApp1.Models
{
    /// <summary>
    /// 机器人用户实体（与 users 表对应）
    /// </summary>
    public class BotUser
    {
        /// <summary>
        /// 用户ID（对应OpenGroupID，主键）
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// 用户昵称
        /// </summary>
        public string? Nickname { get; set; }  // 允许为null（对应表中null）

        /// <summary>
        /// 地区
        /// </summary>
        public string? Region { get; set; }   // 允许为null

        /// <summary>
        /// 最后活跃时间（自动更新）
        /// </summary>
        public DateTime? LastActiveTime { get; set; }  // 允许为null（表中默认值）

        /// <summary>
        /// 创建时间（自动生成）
        /// </summary>
        public DateTime? CreatedAt { get; set; }  // 允许为null（表中默认值）
    }

    /// <summary>
    /// 实体配置（确保与表结构完全匹配）
    /// </summary>
    public class BotUserConfiguration : IEntityTypeConfiguration<BotUser>
    {
        public void Configure(EntityTypeBuilder<BotUser> builder)
        {
            builder.ToTable("users", t => t.HasComment("机器人用户表"));

            // 主键映射
            builder.HasKey(u => u.Id);
            builder.Property(u => u.Id)
                .HasColumnName("id")  // 显式指定对应数据库列名
                .HasMaxLength(64)
                .HasComment("用户ID（对应OpenGroupID）");

            // 其他字段映射
            builder.Property(u => u.Nickname)
                .HasColumnName("nickname")  // 对应表中 nickname
                .HasMaxLength(128)
                .IsUnicode(true)
                .HasComment("用户昵称");

            builder.Property(u => u.Region)
                .HasColumnName("region")  // 对应表中 region
                .HasMaxLength(64)
                .IsUnicode(true)
                .HasComment("地区");

            builder.Property(u => u.LastActiveTime)
                .HasColumnName("last_active_time")  // 对应表中 last_active_time
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .ValueGeneratedOnAddOrUpdate()
                .HasComment("最后活跃时间");

            builder.Property(u => u.CreatedAt)
                .HasColumnName("created_at")  // 关键：对应表中 created_at
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .ValueGeneratedOnAdd()
                .HasComment("创建时间");
        }
    }
}