using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ConsoleApp1.services
{
    public class MarkdownImageService
    {
        private readonly HttpClient _httpClient;

        private string saveDirectory;
        // 配置图片转换服务
        private readonly string _apiUrl = "http://localhost:3000/api/markdown-to-image";

        public MarkdownImageService(string pictureUrl)
        {
            saveDirectory = pictureUrl;
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Accept.Add(
                new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
        }

        /// <summary>
        /// 调用Markdown转图片服务，并保存图片到本地
        /// </summary>
        /// <param name="markdownContent">源Markdown格式内容</param>
        /// <param name="saveDirectory">保存目录路径</param>
        /// <param name="fileName">保存的文件名（不含扩展名）</param>
        /// <param name="format">图片格式（默认png）</param>
        /// <param name="width">图片宽度（默认300）</param>
        /// <returns>保存后的完整文件路径</returns>
        public async Task<string> ConvertAndSaveImageAsync(
            string markdownContent,
            string fileName,
            string format = "png",
            int width = 300)
        {
            try
            {
                // 1. 构建请求参数
                var requestBody = new
                {
                    markdown = markdownContent,
                    config = new
                    {
                        format = format,
                        width = width,
                        theme = "light" // 可根据需要调整
                    }
                };

                // 序列化请求体为JSON
                string jsonBody = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

                // 2. 发送POST请求
                var response = await _httpClient.PostAsync(_apiUrl, content);
                response.EnsureSuccessStatusCode(); // 确保请求成功

                // 3. 读取图片二进制数据
                byte[] imageBytes = await response.Content.ReadAsByteArrayAsync();

                // 4. 确保保存目录存在
                if (!Directory.Exists(saveDirectory))
                {
                    Directory.CreateDirectory(saveDirectory);
                }

                // 5. 构建完整保存路径
                string fullFileName = $"{fileName}.{format}";
                string savePath = Path.Combine(saveDirectory, fullFileName);

                // 6. 保存图片到本地
                await File.WriteAllBytesAsync(savePath, imageBytes);

                return savePath;
            }
            catch (Exception ex)
            {
                throw new Exception($"转换或保存图片失败: {ex.Message}", ex);
            }
        }
    }
}