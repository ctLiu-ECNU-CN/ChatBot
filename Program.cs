using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ConsoleApp1;
using ConsoleApp1.models;
using ConsoleApp1.utils;
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
            // Stable Diffusion WebUI 的 API 地址
            string apiUrl = "http://127.0.0.1:7861/sdapi/v1/txt2img"; // 根据你的实际地址修改

            // 构造请求体
            var requestBody = new
            {
                enable_hr = false,
                denoising_strength = 0,
                hr_scale = 2,
                hr_second_pass_steps = 0,
                hr_resize_x = 0,
                hr_resize_y = 0,
                hr_prompt = "",
                hr_negative_prompt = "",
                prompt = prompt, // 正向关键字

                seed = -1,
                subseed = -1,
                subseed_strength = 0,
                seed_resize_from_h = -1,
                seed_resize_from_w = -1,
                batch_size = 1,
                n_iter = 1,
                steps = 50,
                cfg_scale = 7,
                width = 512,
                height = 512,
                restore_faces = false,
                tiling = false,
                do_not_save_samples = false,
                do_not_save_grid = false,
                eta = 0,
                s_min_uncond = 0,
                s_churn = 0,
                s_tmax = 0,
                s_tmin = 0,
                s_noise = 1,
                override_settings = new { }, // 覆盖性配置
                override_settings_restore_afterwards = true,
                script_args = new string[] { }, // lora 模型参数配置
                sampler_index = "Euler", // 采样方法
                send_images = true, // 是否发送图像
                save_images = false, // 是否在服务端保存生成的图像
                alwayson_scripts = new { } // alwayson配置
            };

            // 发送 HTTP 请求
            using (var httpClient = new HttpClient())
            {
                var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
                var response = await httpClient.PostAsync(apiUrl, content);

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
        var desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        var picturePath = Path.Combine(desktopPath, "机器人", "picture");

        // 确保目录存在
        Directory.CreateDirectory(picturePath);

        var configFilePath = Path.Combine(desktopPath, "机器人", "bot.json");
        var idiomsFilePath = Path.Combine(desktopPath, "机器人", "idiom.json");
        var signFilePath = Path.Combine(desktopPath, "机器人", "sign.json");

        var botData = ConfigLoader.LoadBotConfig(configFilePath);
        var idiomsList = ConfigLoader.LoadIdiomsConfig(idiomsFilePath);

        var accessInfo = new OpenApiAccessInfo()
        {
            BotQQ = botData.BotQQ ?? "3889621847",
            BotAppId = botData.BotAppId ?? "102715870",
            BotToken = botData.BotToken ?? "kgO3CNGPgTMQK4KBzeJm4qK4hiQ1sWS5",
            BotSecret = botData.BotSecret ?? "SrGf4TtJj9ZzPqHi9a1SuMoGiAc5Y1Ux"
        };

        QQChannelApi apiProvider = new(accessInfo);
        apiProvider.UseBotIdentity();
        apiProvider.UseSandBoxMode();
        var bot = new ChannelBot(apiProvider);

        var idiomGameService = new IdiomGameService(idiomsList);
        var signService = new SignService(signFilePath);
        var ollamaService = new OllamaService();

        bot.RegisterChatEvent();

        bot.ReceivedChatGroupMessage += async (message) =>
        {
            var api = apiProvider.GetChatMessageApi();
            string[] messageParts = message.Content.Trim().Split(' ');
            string userMessage = messageParts[0];

            Console.WriteLine($"收到消息 ID:{message.Id}, Title:{userMessage}, 作者:{message.Author}");

            if (userMessage == "/成语接龙")
            {
                await idiomGameService.StartGameAsync(api, message.GroupOpenId, message.Id);
            }
            else if (userMessage == "/签到")
            {
                var result = signService.Sign(message.Author.MemberOpenId);
                await api.SendGroupMessageAsync(message.GroupOpenId, result, passiveMsgId: message.Id);
            }
            else if (userMessage == "/deepseek")
            {
                try
                {
                    var prompt = messageParts[1];
                    Console.WriteLine($"用户的prompt{prompt}");
                    try
                    {
                        var reply = await ollamaService.GetReplyAsync(prompt);
                        await api.SendGroupMessageAsync(message.GroupOpenId, reply, passiveMsgId: message.Id);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                        await api.SendGroupMessageAsync(message.GroupOpenId, "服务异常,已终止思考.", passiveMsgId: message.Id);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    await api.SendGroupMessageAsync(message.GroupOpenId, "请输入问题噢!\n样例: /deepseek 什么是类加载?", passiveMsgId: message.Id);
                }
            }
            else if (userMessage == "/AI绘图")
            {
                try
                {
                    // 提取 prompt
                    string prompt = string.Join(" ", messageParts.Skip(1)); // 去掉 "/生成图片"，剩下的部分作为 prompt
                    if (string.IsNullOrEmpty(prompt))
                    {
                        await api.SendGroupMessageAsync(message.GroupOpenId, "请输入生成图片的描述，例如：/生成图片 一只可爱的猫", passiveMsgId: message.Id);
                        return;
                    }

                    // 调用 Stable Diffusion WebUI 
                    string generatedImagePath = await GenerateImageWithStableDiffusionAsync(prompt, picturePath);
                    if (string.IsNullOrEmpty(generatedImagePath))
                    {
                        await api.SendGroupMessageAsync(message.GroupOpenId, "图片生成失败，请稍后重试。", passiveMsgId: message.Id);
                        return;
                    }

                    // 上传到 COS
                    var uploadedResult = await CloudObjectStorage.UploadFileAsync(generatedImagePath);
                    Console.WriteLine($"图片上传成功，URL: {uploadedResult.Url}");

                    // 将图片 URL 发送到群
                    var response = await apiProvider.GetChatMessageApi().SendGroupMessage(message, uploadedResult.Url);
                    Console.WriteLine($"图片发送成功，消息 ID: {response.Id}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"生成图片或上传失败: {ex.Message}");
                    await api.SendGroupMessageAsync(message.GroupOpenId, "生成图片失败，请稍后重试。", passiveMsgId: message.Id);
                }
            }
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
