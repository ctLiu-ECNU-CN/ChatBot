
using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
namespace MyBot.Services
{


public class EmojiCombineService
    {
        private string _picturePath;
        private readonly HttpClient _httpClient;

        public EmojiCombineService()
        {
            // 强制使用 TLS 1.2
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            // 创建 HttpClient 实例
            _httpClient = new HttpClient();
        }

        public async Task<Image> LoadEmojiImageAsync(string emoji)
        {
            // 获取 emoji 的 Unicode 编码（例如：🍀 → 1f340）
            string unicode = char.ConvertToUtf32(emoji, 0).ToString("x");
        
            // 构建 Twemoji 图像的 URL
            string emojiImageUrl = $"https://twemoji.maxcdn.com/v/latest/72x72/{unicode}.png";

            try
            {
                // 下载 emoji 图片的字节数据
                var response = await _httpClient.GetByteArrayAsync(emojiImageUrl);

                // 将字节数据转换为图像
                using (var stream = new MemoryStream(response))
                {
                    return Image.FromStream(stream);
                }
            }
            catch (Exception ex)
            {
                // 捕获异常并输出详细错误信息
                Console.WriteLine($"加载 emoji 图像时出错：{ex.Message}");
                throw;
            }
        }

        public async Task<string> HandleEmojiCombineAsync(string userMessage)
        {
            try
            {
                // 解析用户输入的命令
                string[] messageParts = userMessage.Trim().Split(' ');
                var emojis = messageParts.Skip(2).ToArray(); // 跳过命令部分 "/emoji 合成"
                
                if (emojis.Length < 1)
                {
                    return "请输入至少一个emoji进行合成！";
                }

                // 判断是否只是文本合成
                if (emojis.Length == 1)
                {
                    return $"合成结果：\n{string.Join(" ", emojis)}";
                }

                // 调用图像合成方法生成合成图像
                string generatedImagePath = await GenerateEmojiImageAsync(emojis);
                if (string.IsNullOrEmpty(generatedImagePath))
                {
                    return "合成图像失败，请稍后再试！";
                }

                return generatedImagePath; // 返回图像文件路径
            }
            catch (WebException webEx)
            {
                Console.WriteLine($"WebException 错误：{webEx.Message}");
                if (webEx.InnerException != null)
                {
                    Console.WriteLine($"内层异常：{webEx.InnerException.Message}");
                }
                return "处理emoji合成时发生错误！";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"处理emoji合成失败: {ex.Message}");
                return "处理emoji合成时发生错误！";
            }
        }
        

        private async Task<string> GenerateEmojiImageAsync(string[] emojis)
        {
            try
            {
                // 创建一个空白图像，设置宽度和高度
                int width = 512;
                int height = 512;
                using (var bitmap = new Bitmap(width, height))
                {
                    using (var graphics = Graphics.FromImage(bitmap))
                    {
                        graphics.Clear(Color.White);

                        // 设置初始位置
                        int x = 10;
                        int y = 10;

                        foreach (var emoji in emojis)
                        {
                            var emojiImage = await LoadEmojiImageAsync(emoji);
                            graphics.DrawImage(emojiImage, new Point(x, y));

                            x += emojiImage.Width + 10; // 调整位置，以便下一个emoji不重叠

                            if (x > width - emojiImage.Width) // 超过宽度时换行
                            {
                                x = 10;
                                y += emojiImage.Height + 10;
                            }
                        }

                        // 保存图像
                        string savedImagePath = Path.Combine(_picturePath, "emoji_combined.png");
                        bitmap.Save(savedImagePath);

                        return savedImagePath;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"生成emoji图像失败: {ex.Message}");
                return null;
            }
        }
        
        
    }
}
