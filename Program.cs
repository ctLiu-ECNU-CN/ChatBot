using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using ConsoleApp1;
using ConsoleApp1.game;
using ConsoleApp1.models;
using ConsoleApp1.utils;
using MyBot.Api;
using MyBot.Datas;
using MyBot.Expansions.Bot;
using MyBot.Models.MessageModels;
using MyBot.Services;
using Mysqlx.Notice;
using Newtonsoft.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;

class Program
{
    private static int msgSeq = 0;
    private static GomokuService _gomokuService;
    private readonly PrivateMessageService _privateMessageService;
    

    public class DatabaseConfig
    {
        public string Server { get; set; }
        public string Database { get; set; }
        public string UserId { get; set; }
        public string Password { get; set; }

        public DatabaseConfig(string server, string database, string userId, string password)
        {
            Server = server;
            Database = database;
            UserId = userId;
            Password = password;
        }
        public string ConnectionString => 
            $"Server={Server};Database={Database};Uid={UserId};Pwd={Password};";
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
        var idiomRecordPath = Path.Combine(desktopPath, "机器人", "idiomRecord.json");

        var botData = ConfigLoader.LoadBotConfig(configFilePath);
        var idiomsList = ConfigLoader.LoadIdiomsConfig(idiomsFilePath);
        //数据库信息
        //var dataBase = new DatabaseConfig("localhost","texas_holdem_db","root","347934");
        
        // 初始化数据库服务
        var dbService = new DatabaseService(botData.Database.ConnectionString);

        var signService = new SignService(dbService);
        var accessInfo = new OpenApiAccessInfo()
        {
            BotQQ = botData.BotQQ ,
            BotAppId = botData.BotAppId ,
            BotToken = botData.BotToken ,
            BotSecret = botData.BotSecret
        };

        QQChannelApi apiProvider = new(accessInfo);
        apiProvider.UseBotIdentity();
        apiProvider.UseSandBoxMode();
        var bot = new ChannelBot(apiProvider);
        var api = apiProvider.GetChatMessageApi();
        var idiomGameService = new IdiomGameService(idiomsList,idiomsFilePath);
        var ollamaService = new OllamaService();
        
        var wordGuessingGameService = new WordGuessingGameService(apiProvider);
        bot.RegisterChatEvent();
        //单聊被动回复
        var privateMessageService = new PrivateMessageService(apiProvider);
        TexasGameManager texasGameManager = new TexasGameManager();
        //必须初始化管理类
        texasGameManager.InitGame();
        TexasMessageService texasMessageService = new TexasMessageService(texasGameManager,api);
        bot.ReceivedChatUserMessage += async (message) =>
        {
            // 处理私聊消息
            await texasMessageService.HandlePrivateMessageAsync(message);

        };
        //群聊被动回复
        int msg_sql = 2277;
        bot.ReceivedChatGroupMessage += async (message) =>
        {
            wordGuessingGameService.setMessageApi(message);

            string[] messageParts = message.Content.Trim().Split(' ');
            string userMessage = messageParts[0];

            Console.WriteLine($"收到消息!\n内容：{message.Content}\n 用户ID:{message.Author.MemberOpenId}");

            int round = texasGameManager.CurrentBetRound;

            if (userMessage.StartsWith("/加入德州"))
            {
                if (round != -1)
                {
                    await api.SendGroupMessageAsync(message.GroupOpenId, "游戏已开始，无法加入。", msgSeq: (msg_sql++) % 113,passiveMsgId: message.Id);
                    return;
                }
                await texasMessageService.HandleJoinGameAsync(message);
            }
            else if (userMessage.Trim() == "准备")
            {
               
                if (round != -1)
                {
                    await api.SendGroupMessageAsync(message.GroupOpenId, "游戏已开始，无法再次准备。", msgSeq: (msg_sql++) % 113,passiveMsgId: message.Id);
                    return;
                }
                await texasMessageService.HandleReadyAsync(message);
                Console.WriteLine($"当前轮到{texasGameManager.GetCurrentPlayer().Role}操作");
            }
            else if (userMessage.StartsWith("/下注"))
            {
                Console.WriteLine("下注操作");
                Console.WriteLine($"当前轮到{texasGameManager.GetCurrentPlayer().Role}操作");
                if (round < 0 || round > 3)
                {
                    await api.SendGroupMessageAsync(message.GroupOpenId, "当前不允许下注，请等待下注轮开始。", msgSeq: (msg_sql++) % 113,passiveMsgId: message.Id);
                    await texasMessageService.NoticeCurrentPlayerAsync(message);
                    return;
                }
                await texasMessageService.HandleBetActionAsync(message);
                await texasMessageService.NoticeCurrentPlayerAsync(message);
            }
            else if (userMessage.StartsWith("/弃牌") || userMessage.StartsWith("/跟注") || userMessage.StartsWith("/加注"))
            {
                Console.WriteLine($"游戏进度{round}三选一操作");
                Console.WriteLine($"当前轮到{texasGameManager.GetCurrentPlayer().Role}操作");                         
                if (round < 0 || round > 3)
                {
                    await api.SendGroupMessageAsync(message.GroupOpenId, "当前不在有效的下注轮中，无法执行该操作。", msgSeq: (msg_sql++) % 113,passiveMsgId: message.Id);
                    await texasMessageService.NoticeCurrentPlayerAsync(message);
                    return;
                }
                await texasMessageService.HandleInGameCommandAsync(message);
                await texasMessageService.NoticeCurrentPlayerAsync(message);
                if (texasGameManager.IsBettingRoundOver())
                {
                    texasGameManager.CurrentBetRound++;
                    api.SendGroupMessageAsync(message.GroupOpenId, "当前所有玩家下了均注,进入下一阶段", msgSeq: (msg_sql++) % 113,passiveMsgId: message.Id);
                    await texasMessageService.NoticeCurrentPlayerAsync(message);
                    await texasMessageService.NoticeCurrentPlayerAsync(message);
                }
            }

            else if (userMessage.StartsWith("/成语接龙"))
            {
                await api.SendGroupMessageAsync(message.GroupOpenId, "没人玩,摆烂咯。", msgSeq: (msg_sql++) % 113,passiveMsgId: message.Id);
                //await idiomGameService.StartGameAsync(api, message.GroupOpenId, message.Id);
            }
            else if (userMessage.StartsWith("/我接"))
            {
                await api.SendGroupMessageAsync(message.GroupOpenId, "没人玩,摆烂咯。", msgSeq: (msg_sql++) % 113,passiveMsgId: message.Id);
                //var msg = string.Join(" ", messageParts.Skip(0));;
               // await idiomGameService.HandleUserMessageAsync(api, message.GroupOpenId, message.Id,userMessage: msg, message.Author.MemberOpenId, message.Author.UserOpenId, idiomsList);
            }
            else if (userMessage == "/签到")
            {
                var result = signService.Sign(message.Author.MemberOpenId);
                await api.SendGroupMessageAsync(message.GroupOpenId, result, passiveMsgId: message.Id);
            }
            else if (userMessage.StartsWith("/猜单词"))
            {
                int wordLength = 5; // 默认长度为 5
                if (messageParts.Length > 1)
                {
                    if (int.TryParse(messageParts[1], out int length))
                    {
                        if (length >= 2 && length <= 11)
                        {
                            wordLength = length;
                        }
                        else
                        {
                            await api.SendGroupMessageAsync(message.GroupOpenId,
                                "单词长度必须在 2 到 11 之间，请重新输入！",
                                passiveMsgId: message.Id,
                                msgSeq: (msgSeq++ % 1311));
                            return;
                        }
                    }
                    else
                    {
                        await api.SendGroupMessageAsync(message.GroupOpenId,
                            "请输入有效的数字作为单词长度！",
                            passiveMsgId: message.Id,
                            msgSeq: (msgSeq++ % 1311));
                        return;
                    }
                }

                await wordGuessingGameService.StartGameAsync(api, message.GroupOpenId, message.Id, wordLength);
            }
            else if (userMessage.StartsWith("/我猜单词是"))
            {
                if (!wordGuessingGameService.IsGameStarted())
                {
                    await api.SendGroupMessageAsync(message.GroupOpenId,
                        "请先使用 /猜单词 命令启动游戏！",
                        passiveMsgId: message.Id,
                        msgSeq: (msgSeq++ % 1311));
                    return;
                }

                var guessedWord = string.Join(" ", messageParts.Skip(1)).Trim();

                await wordGuessingGameService.HandleGuessAsync(
                    api,
                    message.GroupOpenId,
                    message.Id,
                    $"/我猜单词是 {guessedWord}",
                    message.Author.MemberOpenId
                );
            }

            else if (userMessage == "/deepseek")
            {
                try
                {
                    var prompt = string.Join(" ", messageParts.Skip(1));;
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
            else if (userMessage == "/AI画图")
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
        else if (userMessage == "/五子棋")
    {
        _gomokuService = new GomokuService();
        var boardImage = _gomokuService.GenerateBoardImage();
        string imagePath = Path.Combine(picturePath, "gomoku_board.png");
        boardImage.Save(imagePath);

        var uploadResult = await CloudObjectStorage.UploadFileAsync(imagePath);
        //await api.SendGroupMessageAsync(message.GroupOpenId, $"五子棋游戏开始！", passiveMsgId: message.Id);
        await apiProvider.GetChatMessageApi().SendGroupMessage(message, uploadResult.Url);
    }
    else if (userMessage == "/开始五子棋")
    {
        _gomokuService = new GomokuService();
        var boardImage = _gomokuService.GenerateBoardImage();
        string imagePath = Path.Combine(picturePath, "gomoku_board.png");
        boardImage.Save(imagePath);

        var uploadResult = await CloudObjectStorage.UploadFileAsync(imagePath);
        //await api.SendGroupMessageAsync(message.GroupOpenId, $"五子棋游戏开始！\n{uploadResult.Url}", passiveMsgId: message.Id);
        await apiProvider.GetChatMessageApi().SendGroupMessage(message, uploadResult.Url);
    }
    else if (userMessage == "/下棋" && _gomokuService != null)
    {
        if (messageParts.Length != 3)
        {
            await api.SendGroupMessageAsync(message.GroupOpenId, "请输入正确的坐标，例如：/下棋 a 3", passiveMsgId: message.Id);
            return;
        }

        // 提取行字母和列数字
        string rowInput = messageParts[1];
        string colInput = messageParts[2];

        // 验证行字母是否为单个字母，并在范围内
        if (rowInput.Length != 1 || !char.IsLetter(rowInput[0]))
        {
            await api.SendGroupMessageAsync(message.GroupOpenId, "行坐标错误，请输入单个字母，范围：a-o！", passiveMsgId: message.Id);
            return;
        }

        char rowLetter = char.ToLower(rowInput[0]);
        if (rowLetter < 'a' || rowLetter > 'o')
        {
            await api.SendGroupMessageAsync(message.GroupOpenId, "行坐标错误，字母范围必须是 a-o！", passiveMsgId: message.Id);
            return;
        }

        // 验证列数字是否为有效整数，并在范围内
        if (!int.TryParse(colInput, out int colNumber) || colNumber < 1 || colNumber > 15)
        {
            await api.SendGroupMessageAsync(message.GroupOpenId, "列坐标错误，请输入数字，范围：1-15！", passiveMsgId: message.Id);
            return;
        }

        // 调用服务放置棋子
        if (!_gomokuService.PlaceStone(rowLetter, colNumber))
        {
            await api.SendGroupMessageAsync(message.GroupOpenId, "非法操作，请检查坐标！", passiveMsgId: message.Id);
            return;
        }

        int x = rowLetter - 'a';
        int y = colNumber - 1;

        // 检查玩家胜利
        if (_gomokuService.CheckWin(x, y))
        {
            var boardImage = _gomokuService.GenerateBoardImage();
            string imagePath = Path.Combine(picturePath, "gomoku_board.png");
            boardImage.Save(imagePath);

            var uploadResult = await CloudObjectStorage.UploadFileAsync(imagePath);
            await apiProvider.GetChatMessageApi().SendGroupMessage(message, uploadResult.Url);
            //await api.SendGroupMessageAsync(message.GroupOpenId, $"玩家胜利！\n{uploadResult.Url}", passiveMsgId: message.Id);
            _gomokuService = null;
            return;
        }

        var (aiX, aiY) = _gomokuService.GetBestMove(depth: 3, isMaximizingPlayer: false);

        if (_gomokuService.PlaceStone((char)('a' + aiX), aiY + 1))
        {
            // 检查 AI 胜利
            if (_gomokuService.CheckWin(aiX, aiY))
            {
                var boardImage = _gomokuService.GenerateBoardImage();
                string imagePath = Path.Combine(picturePath, "gomoku_board.png");
                boardImage.Save(imagePath);

                var uploadResult = await CloudObjectStorage.UploadFileAsync(imagePath);
                await apiProvider.GetChatMessageApi().SendGroupMessage(message, uploadResult.Url);
                _gomokuService = null;
                return;
            }
        }
        /*// AI 随机落子
        Random random = new Random();
        int aiX, aiY;
        do
        {
            aiX = random.Next(0, 15);
            aiY = random.Next(0, 15);
        } while (!_gomokuService.PlaceStone((char)('a' + aiX), aiY + 1));

        // 检查 AI 胜利
        if (_gomokuService.CheckWin(aiX, aiY))
        {
            var boardImage = _gomokuService.GenerateBoardImage();
            string imagePath = Path.Combine(picturePath, "gomoku_board.png");
            boardImage.Save(imagePath);

            var uploadResult = await CloudObjectStorage.UploadFileAsync(imagePath);
            await apiProvider.GetChatMessageApi().SendGroupMessage(message, uploadResult.Url);
            //await api.SendGroupMessageAsync(message.GroupOpenId, $"机器人胜利！\n{uploadResult.Url}", passiveMsgId: message.Id);
            _gomokuService = null;
            return;
        }*/

        // 返回更新后的棋盘
        var updatedBoardImage = _gomokuService.GenerateBoardImage();
        string updatedImagePath = Path.Combine(picturePath, "gomoku_board.png");
        updatedBoardImage.Save(updatedImagePath);

        var updatedUploadResult = await CloudObjectStorage.UploadFileAsync(updatedImagePath);
        //await api.SendGroupMessageAsync(message.GroupOpenId, $"当前棋局：\n{updatedUploadResult.Url}", passiveMsgId: message.Id);
        await apiProvider.GetChatMessageApi().SendGroupMessage(message, updatedUploadResult.Url);
    }
    else if (userMessage.StartsWith("/emoji查询"))
    {
        string emoji = message.Content.Trim().Substring("/emoji查询".Length).Trim();
        if (string.IsNullOrEmpty(emoji))
        {
            await api.SendGroupMessageAsync(message.GroupOpenId, "请输入一个emoji进行查询！", passiveMsgId: message.Id);
            return;
        }

        var combinations = await GetEmojiCombinationsAsync(emoji);
        if (combinations == null || combinations.Length == 0)
        {
            await api.SendGroupMessageAsync(message.GroupOpenId, $"没有找到与{emoji}合成的其他表情！", passiveMsgId: message.Id);
            return;
        }

        string resultMessage = "与这个emoji可以合成的表情包括：\n";
        foreach (var combo in combinations)
        {
            resultMessage += $"{combo.Emoji} ({combo.HexValue})\n";
        }

        await api.SendGroupMessageAsync(message.GroupOpenId, resultMessage, passiveMsgId: message.Id);
    }
    else if (userMessage.StartsWith("/emoji合成"))
    {
        string[] emojiParts = message.Content.Trim().Split(' '); 
        var emojis = emojiParts.Skip(1)  // 跳过命令 "/emoji合成"
            .Where(part => !string.IsNullOrEmpty(part) && part != "+") // 过滤掉 "+" 符号
            .ToArray();

        if (emojis.Length < 2)
        {
            await api.SendGroupMessageAsync(message.GroupOpenId, "请输入至少两个emoji进行合成！", passiveMsgId: message.Id,msgSeq:msgSeq++);
            return;
        }

        string generatedImagePath = await GenerateEmojiImageAsync(emojis, picturePath);
        if (string.IsNullOrEmpty(generatedImagePath))
        {
            await api.SendGroupMessageAsync(message.GroupOpenId, "合成图像失败，请稍后再试！", passiveMsgId: message.Id,msgSeq:msgSeq++);
            return;
        }

        var uploadedResult = await CloudObjectStorage.UploadFileAsync(generatedImagePath);
        await api.SendGroupMessageAsync(message.GroupOpenId, "合成的emoji图像如下:", passiveMsgId: message.Id,msgSeq:msgSeq++);
        await apiProvider.GetChatMessageApi().SendGroupMessage(message, uploadedResult.Url,msgSeq++);
    }
    else if (userMessage.StartsWith("/emoji随机合成"))
    {
        string generatedImagePath = await GenerateRandomEmojiImageAsync(picturePath);
        if (string.IsNullOrEmpty(generatedImagePath))
        {
            await api.SendGroupMessageAsync(message.GroupOpenId, "随机合成失败，请稍后再试！", passiveMsgId: message.Id);
            return;
        }

        var uploadedResult = await CloudObjectStorage.UploadFileAsync(generatedImagePath);
        await api.SendGroupMessageAsync(message.GroupOpenId, "随机合成的emoji图像如下:", passiveMsgId: message.Id, msgSeq:msgSeq++);
        msgSeq++;
        await apiProvider.GetChatMessageApi().SendGroupMessage(message, uploadedResult.Url,msgSeq);
    }

    
            else 
            {
                var markdown = new MessageMarkdown
                {
                    CustomTemplateId = "\n102715870_1743213210", // 使用申请的模板ID
                    Params = new List<MessageMarkdownParams>
                    {
                        new MessageMarkdownParams
                        {
                            Key = "title",
                            Values = new List<string> { "这是标题" }
                        },
                        new MessageMarkdownParams
                        {
                            Key = "data1",
                            Values = new List<string> { "这是内容" }
                        }
                    }
                };

                var keyboard = new MessageKeyboard
                {
                    Id = "102715870_1743213210" // 在QQ开放平台预先配置好的键盘模板ID
                };
                try
                {
                    //await api.SendGroupMessageAsync(message.GroupOpenId, "功能维护中", passiveMsgId: message.Id);
                    var response = await api.SendGroupMessageAsync(
                        openId: message.GroupOpenId,
                        content:"测试...",
                        msgType:ChatMessageType.Markdown,
                        keyboard: keyboard,
                        passiveMsgId: message.Id
                    );

                }catch (
                    Exception exception
                )
                {
                    Console.WriteLine(exception.Message);
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

// 在线加载emoji图像

    private static async Task<EmojiCombination[]> GetEmojiCombinationsAsync(string emoji)
    {
        try
        {
            string url = $"http://promptpan.com/emoji/{Uri.EscapeDataString(emoji)}/combinations";
            using (HttpClient client = new HttpClient())
            {
                HttpResponseMessage response = await client.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    string jsonString = await response.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<EmojiCombination[]>(jsonString);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"获取表情合成组合失败: {ex.Message}");
        }
        return null;
    }
    private static async Task<string> GenerateRandomEmojiImageAsync(string picturePath)
    {
        try
        {
            string url = "http://promptpan.com/mix/random";

            using (HttpClient client = new HttpClient())
            {
                HttpResponseMessage response = await client.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"随机表情合成请求失败: {response.StatusCode}");
                    return null;
                }

                byte[] imageBytes = await response.Content.ReadAsByteArrayAsync();
                string savedImagePath = Path.Combine(picturePath, $"emoji_random_combined_{DateTime.Now.Ticks}.png");

                await File.WriteAllBytesAsync(savedImagePath, imageBytes);

                return savedImagePath;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"生成随机emoji图像失败: {ex.Message}");
            return null;
        }
    }

// 定义用于接收合成结果的类
    public class EmojiCombination
    {
        public string HexValue { get; set; }
        public string Emoji { get; set; }
    }
    private static async Task<string> GenerateEmojiImageAsync(string[] emojis, string picturePath)
    {
        try
        {
            if (emojis.Length < 2)
            {
                Console.WriteLine("至少需要两个 emoji 进行合成。");
                return null;
            }

            string emoji1 = Uri.EscapeDataString(emojis[0]);
            string emoji2 = Uri.EscapeDataString(emojis[1]);

            string url = $"http://promptpan.com/mix/{emoji1}/{emoji2}";

            using (HttpClient client = new HttpClient())
            {
                HttpResponseMessage response = await client.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"表情合成请求失败: {response.StatusCode}");
                    return null;
                }

                byte[] imageBytes = await response.Content.ReadAsByteArrayAsync();
                string savedImagePath = Path.Combine(picturePath, $"emoji_combined_{DateTime.Now.Ticks}.png");

                await File.WriteAllBytesAsync(savedImagePath, imageBytes);

                return savedImagePath;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"生成emoji图像失败: {ex.Message}");
            return null;
        }
    }
private static async Task<string> GenerateImageWithStableDiffusionAsync(string prompt, string picturePath)
    {
        try
        {
            // Stable Diffusion WebUI 的 API 地址
            string apiUrl = "http://127.0.0.1:7861/sdapi/v1/txt2img"; // 根据你的实际地址修改

            // 构造请求体
            var requestBody = new
            {
                enable_hr = true,
                denoising_strength = 0.75,
                hr_scale = 2,
                hr_second_pass_steps = 10,
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
                cfg_scale = 10,
                width = 512,
                height = 512,
                restore_faces = true,
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
                sampler_index = "DDIM", // 采样方法
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
}
