using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ConsoleApp1;
using ConsoleApp1.config;
using ConsoleApp1.models;
using ConsoleApp1.SakuraFrp;
using ConsoleApp1.services;
using ConsoleApp1.utils;
using ConsoleApp1.Utils;
using MyBot.Api;
using MyBot.Datas;
using MyBot.Expansions.Bot;
using MyBot.Services;

class Program
{
    private static async Task<string> GenerateImageWithStableDiffusionAsync(string prompt, string picturePath)
    {
        try
        {
            // 发送 HTTP 请求
            using (var httpClient = new HttpClient())
            {
                var content = new StringContent(JsonSerializer.Serialize(AppConfig.StableDiffusion.DefaultRequestBody), Encoding.UTF8, "application/json");
                var response = await httpClient.PostAsync(AppConfig.StableDiffusion.ApiUrl,content);

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Stable Diffusion API 请求失败: {response.StatusCode}");
                    return null;
                }

                // 解析响应
                var responseJson = await response.Content.ReadAsStringAsync();
                var responseData = JsonSerializer.Deserialize<StableDiffusionResponse>(responseJson);

                // 保存生成的图片到指定目录
                string savedImagePath = Path.Combine(picturePath, "generated_image.png"); // 保存的图片路径
                byte[] imageBytes = Convert.FromBase64String(responseData.images[0]); // 解码 Base64 图片数据
                await File.WriteAllBytesAsync(savedImagePath, imageBytes);

                Console.WriteLine($"图片生成成功，保存到: {savedImagePath}");
                return savedImagePath;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"调用 Stable Diffusion API 失败: {ex.Message}");
            return null;
        }
    }



    static async Task Main(string[] args)
    {
        AppConfig.InitializeDirectories();
        var desktopPath = AppConfig.DesktopPath;
        var picturePath = Path.Combine(desktopPath, "机器人", "picture");

        // 确保目录存在
        Directory.CreateDirectory(picturePath);
        
        // 读取配置信息
        var sakuraConfigPath = Path.Combine(desktopPath, "机器人","sakura.json");
        var configFilePath = Path.Combine(desktopPath, "机器人", "bot.json");
        var idiomsFilePath = Path.Combine(desktopPath, "机器人", "idiom.json");
        var signFilePath = Path.Combine(desktopPath, "机器人", "sign.json");

        var sakuraConfig = ConfigLoader.LoadSakuraConfig(sakuraConfigPath);
        var botData = ConfigLoader.LoadBotConfig(configFilePath);
        var idiomsList = ConfigLoader.LoadIdiomsConfig(idiomsFilePath);
        var nameDict = sakuraConfig.BuildFriendlyNameMapping();// 构建隧道名和地址映射表
        
        
        var accessInfo = new OpenApiAccessInfo()
        {
            BotQQ = botData.BotQQ,
            BotAppId = botData.BotAppId,
            BotToken = botData.BotToken,
            BotSecret = botData.BotSecret
        };

        QQChannelApi apiProvider = new(accessInfo);
        apiProvider.UseBotIdentity();
        apiProvider.UseSandBoxMode(); 
        
        
        // 微服务启动
        var md2ImageService = new MarkdownImageService(picturePath);
        var bot = new ChannelBot(apiProvider);
        var idiomGameService = new IdiomGameService(idiomsList);
        var signService = new SignService(signFilePath);
        var ollamaService = new OllamaService();
        var sakuraService = new SakuraFrpService(sakuraConfig.LogDirectory);


        // 机器人服务
        bot.RegisterChatEvent();

        bot.ReceivedChatGroupMessage += async (message) =>
        {
            var api = apiProvider.GetChatMessageApi();
            string[] messageParts = message.Content.Trim().Split(' ');
            string userMessage = messageParts[0];

            Console.WriteLine($"收到消息 ID:{message.Id}, Title:{userMessage}, 作者ID:{message.GroupOpenId}");

            if (userMessage == "/MC地址")
            {
                var addresses = await sakuraService.GetAllTunnelAddressesAsync();
                var addressFormatter =AddressFormatter.FormatAndFilterAddresses(addresses, nameDict);
                var markdownContent = new StringBuilder();
                markdownContent.AppendLine("# MC服务器地址");
                markdownContent.AppendLine("> 以下是可用的服务器连接地址（实时更新）\n 请不要将地址暴露给陌生人");
                markdownContent.AppendLine(); // 空行分隔
                markdownContent.AppendLine("| 服务器名称 | 连接地址 |");
                markdownContent.AppendLine("|------------|----------|");
                foreach (var address in addressFormatter)
                {
                    //构建markdown语句
                    // 假设 address 格式为 "服务器名称: 连接地址"
                    var parts = address.Split(new[] { ':' }, 2);
                    if (parts.Length == 2)
                    {
                        var serverName = parts[0].Trim();
                        var serverAddress = parts[1].Trim();
                        markdownContent.AppendLine($"| {serverName} | `{serverAddress}` |");
                    }
                }

                // 最终的 Markdown 文本（可直接作为参数传入）
                string mdText = markdownContent.ToString();
                var convertAndSaveImagepath = await md2ImageService.ConvertAndSaveImageAsync(mdText, "服务器地址");
                if (convertAndSaveImagepath != null)
                {
                    // 上传到 COS
                    var uploadedResult = await CloudObjectStorage.UploadFileAsync(convertAndSaveImagepath);
                    Console.WriteLine($"图片上传成功，URL: {uploadedResult.Url}");

                    // 将图片 URL 发送到群
                    var response = await apiProvider.GetChatMessageApi().SendGroupMessage(message, uploadedResult.Url);
                    Console.WriteLine($"图片发送成功，消息 ID: {response.Id}");
                }
                else
                {
                    await api.SendGroupMessageAsync(message.GroupOpenId, "获取失败,手动联系下lct", passiveMsgId: message.Id);
                }

                Console.WriteLine(convertAndSaveImagepath);
            }

        // if (userMessage == "/成语接龙")
        // {
        //     await idiomGameService.StartGameAsync(api, message.GroupOpenId, message.Id);
        // }
        // else if (userMessage == "/签到")
        // {
        //     var result = signService.Sign(message.Author.MemberOpenId);
        //     await api.SendGroupMessageAsync(message.GroupOpenId, result, passiveMsgId: message.Id);
        // }
        // else if (userMessage == "/deepseek")
        // {
        //     try
        //     {
        //         var prompt = messageParts[1];
        //         Console.WriteLine($"用户的prompt{prompt}");
        //         try
        //         {
        //             var reply = await ollamaService.GetReplyAsync(prompt);
        //             await api.SendGroupMessageAsync(message.GroupOpenId, reply, passiveMsgId: message.Id);
        //         }
        //         catch (Exception ex)
        //         {
        //             Console.WriteLine(ex.Message);
        //             await api.SendGroupMessageAsync(message.GroupOpenId, "服务异常,已终止思考.", passiveMsgId: message.Id);
        //         }
        //     }
        //     catch (Exception ex)
        //     {
        //         Console.WriteLine(ex.Message);
        //         await api.SendGroupMessageAsync(message.GroupOpenId, "请输入问题噢!\n样例: /deepseek 什么是类加载?", passiveMsgId: message.Id);
        //     }
        // }
        // else if (userMessage == "/AI绘图")
        // {
        //     try
        //     {
        //         // 提取 prompt
        //         string prompt = string.Join(" ", messageParts.Skip(1)); // 去掉 "/生成图片"，剩下的部分作为 prompt
        //         if (string.IsNullOrEmpty(prompt))
        //         {
        //             await api.SendGroupMessageAsync(message.GroupOpenId, "请输入生成图片的描述，例如：/生成图片 一只可爱的猫", passiveMsgId: message.Id);
        //             return;
        //         }
        //
        //         // 调用 Stable Diffusion WebUI 
        //         string generatedImagePath = await GenerateImageWithStableDiffusionAsync(prompt, picturePath);
        //         if (string.IsNullOrEmpty(generatedImagePath))
        //         {
        //             await api.SendGroupMessageAsync(message.GroupOpenId, "图片生成失败，请稍后重试。", passiveMsgId: message.Id);
        //             return;
        //         }
        //
        //         // 上传到 COS
        //         var uploadedResult = await CloudObjectStorage.UploadFileAsync(generatedImagePath);
        //         Console.WriteLine($"图片上传成功，URL: {uploadedResult.Url}");
        //
        //         // 将图片 URL 发送到群
        //         var response = await apiProvider.GetChatMessageApi().SendGroupMessage(message, uploadedResult.Url);
        //         Console.WriteLine($"图片发送成功，消息 ID: {response.Id}");
        //     }
        //     catch (Exception ex)
        //     {
        //         Console.WriteLine($"生成图片或上传失败: {ex.Message}");
        //         await api.SendGroupMessageAsync(message.GroupOpenId, "生成图片失败，请稍后重试。", passiveMsgId: message.Id);
        //     }
        // }
    };

        bot.OnError += exception =>
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"错误: {exception}");
            Console.ResetColor();
        };

        bot.AuthenticationSuccess += () => Console.WriteLine("鉴权成功");
        bot.AuthenticationError += () => Console.WriteLine("鉴权失败");
        bot.OnConnected += () => Console.WriteLine("机器人已连接!");
        await bot.OnlineAsync();

        Console.WriteLine("按 Q 退出机器人");
        while (true)
        {
            if (Console.ReadKey(true).Key == ConsoleKey.Q)
            {
                Console.WriteLine("正在关闭机器人...");
                await bot.OfflineAsync();
                break;
            }
        }
        Console.WriteLine("机器人已退出.");
    }
}
