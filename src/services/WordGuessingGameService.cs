using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ConsoleApp1.models;
using MyBot.Api;
using MyBot.Models.MessageModels;
using MySql.Data.MySqlClient;
using MyBot.Services;

public class WordGuessingGameService
{
    private string _targetWord; // 目标单词
    private int _remainingAttempts; // 剩余猜测次数
    private int _wordLength; // 单词长度
    private List<string> _guessedWords; // 用户猜测的单词记录
    private static int msgSeq = 5214;
    private QQChannelApi _apiChannelApi;
    ChatMessage _message;
    private UploadedResult uploadedResult;
    private bool _isGameStarted = false;

    public void setMessageApi(ChatMessage message)
    {
        _message = message;
    }

    public WordGuessingGameService(QQChannelApi qqChannelApi)
    {
        _guessedWords = new List<string>();
        _apiChannelApi = qqChannelApi;
    }

    // 启动游戏，选择目标单词
    public async Task StartGameAsync(ChatMessageApi api, string groupOpenId, string messageId, int wordLength = 6)
    {
        _wordLength = wordLength;
        _remainingAttempts = 7; // 初始化为7次机会
        _guessedWords.Clear(); // 清空猜测记录
        _isGameStarted = true;

        // 从数据库中获取符合长度的单词
        _targetWord = await GetRandomWordAsync(wordLength);
        _targetWord = _targetWord.ToUpper();
        if (_targetWord == null)
        {
            await api.SendGroupMessageAsync(groupOpenId, "没有找到符合该长度的单词，请稍后再试！", passiveMsgId: messageId, msgSeq:
                (msgSeq++ % 1311));
            return;
        }

        // 通知用户游戏开始
        await api.SendGroupMessageAsync(groupOpenId, $"游戏开始！请猜一个长度为 {_wordLength} 的单词！你有 {_remainingAttempts} 次机会。",
            passiveMsgId: messageId, msgSeq:
            (msgSeq++ % 1311));
        try
        {
            var imagePath = await GenerateGuessImage(_guessedWords, _wordLength);
            var response = await _apiChannelApi.GetChatMessageApi().SendGroupMessage(_message, imagePath);
        }
        catch(Exception ex)
        {
            await api.SendGroupMessageAsync(groupOpenId, "处理图片发生了错误，稍后再试！", passiveMsgId: messageId, msgSeq:
                (msgSeq++ % 1311));
        }
        
        
    }

    // 处理用户的猜测
    public async Task HandleGuessAsync(ChatMessageApi api, string groupOpenId, string messageId, string userMessage, string userId)
    {
        if (!IsGameStarted())
        {
            await api.SendGroupMessageAsync(groupOpenId, "游戏尚未开始，请先启动游戏！", passiveMsgId: messageId, msgSeq:
                (msgSeq++ % 1311));
            return;
        }

        if (_remainingAttempts <= 0)
        {
            await api.SendGroupMessageAsync(groupOpenId, "游戏已结束，没有机会再猜了！", passiveMsgId: messageId, msgSeq:
                (msgSeq++ % 1311));
            _isGameStarted = false;
            return;
        }

        // 获取用户猜测的单词
        var parts = userMessage.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2 || !Regex.IsMatch(parts[1], @"^[A-Za-z]+$"))
        {
            await api.SendGroupMessageAsync(groupOpenId, "请按正确格式输入猜测，例如：/我猜单词是 猜测单词", passiveMsgId: messageId,
                msgSeq:
                (msgSeq++ % 1311));
            return;
        }

        string guessedWord = parts[1].Trim().ToUpper();

        // 检查用户猜测的单词是否合法
        if (guessedWord.Length != _wordLength)
        {
            await api.SendGroupMessageAsync(groupOpenId, $"确保你的猜测单词长度为 {_wordLength}！", passiveMsgId: messageId,
                msgSeq:
                (msgSeq++ % 1311));
            return;
        }
        // **新增加的检查步骤：验证单词是否在数据库中**
        if (!await IsValidWordAsync(guessedWord))
        {
            await api.SendGroupMessageAsync(groupOpenId, "您猜的单词不在字典中，请输入合法单词！", passiveMsgId: messageId, msgSeq:
                (msgSeq++ % 1311));
            return;
        }

        // 记录猜测的单词并递减剩余次数
        _guessedWords.Add(guessedWord);
        _remainingAttempts--;

        // 生成猜测反馈
        string feedback = GetGuessFeedback(guessedWord);

        try
        {
            var imagePath = await GenerateGuessImage(_guessedWords, _wordLength);

            // 获取单词的发音、中文解释和例句
            var wordDetails = await GetWordDetailsAsync(guessedWord);
            var wordInfoMessage =
                $"发音：{wordDetails.Phonetic}\n中文解释：{wordDetails.Definitions}\n例句：{wordDetails.ExampleSentence}";

            //await api.SendGroupMessageAsync(groupOpenId, feedback, passiveMsgId: messageId, msgSeq:(msgSeq++ % 1311));
            await api.SendGroupMessageAsync(groupOpenId, wordInfoMessage, passiveMsgId: messageId, msgSeq:
                (msgSeq++ % 1311));
            //await api.SendGroupMessageAsync(groupOpenId, "这次猜测的结果是：", passiveMsgId: messageId, msgSeq:
                //(msgSeq++ % 1311));
            var response = await _apiChannelApi.GetChatMessageApi().SendGroupMessage(_message, imagePath);

            if (guessedWord == _targetWord)
            {
                await api.SendGroupMessageAsync(groupOpenId,
                    $"恭喜你猜对了！单词是 {_targetWord}。\n发音：{wordDetails.Phonetic}\n中文解释：{wordDetails.Definitions}\n例句：{wordDetails.ExampleSentence}",
                    passiveMsgId: messageId, msgSeq:
                    (msgSeq++ % 1311));
                _isGameStarted = false;
                _remainingAttempts = 0; // 猜中后结束游戏
                return;
            }
            else if (_remainingAttempts > 0)
            {
            //    await api.SendGroupMessageAsync(groupOpenId, $"还有 {_remainingAttempts} 次机会！继续猜吧！",
            //        passiveMsgId: messageId, msgSeq:
            //        (msgSeq++ % 1311));
            }
            else
            {
                await api.SendGroupMessageAsync(groupOpenId,
                    $"看来没有人能猜出来呢。正确的单词是 {_targetWord}。\n发音：{wordDetails.Phonetic}\n中文解释：{wordDetails.Definitions}\n例句：{wordDetails.ExampleSentence}",
                    passiveMsgId: messageId, msgSeq:
                    (msgSeq++ % 1311));
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"处理猜测时发生错误: {ex.Message}");
            await api.SendGroupMessageAsync(groupOpenId, "处理您的猜测时发生了错误，稍后再试！", passiveMsgId: messageId, msgSeq:
                (msgSeq++ % 1311));
        }
    }

    private async Task<bool> IsValidWordAsync(string guessedWord)
    {
        string connectionString = "Server=localhost;Database=Dictionary;User ID=root;Password=347934;";
        string query = @"
        SELECT COUNT(*) 
        FROM dictionary 
        WHERE UPPER(word) = @word"; // 将单词转换为大写进行比较

        try
        {
            using (var connection = new MySqlConnection(connectionString))
            {
                await connection.OpenAsync();
                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@word", guessedWord );
                    var count = Convert.ToInt32(await command.ExecuteScalarAsync());
                    return count > 0; // 如果大于0则说明单词存在
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error checking valid word: {ex.Message}");
            return false; // 发生错误时返回false
        }
    }

    // 获取符合长度的随机单词
    private async Task<string> GetRandomWordAsync(int wordLength)
    {
        string connectionString =
            "Server=localhost;Database=Dictionary;User ID=root;Password=347934;";
        string countQuery = @"
            SELECT COUNT(*) 
            FROM dictionary 
            WHERE LENGTH(word) = @wordLength";

        string wordQuery = @"
            SELECT word 
            FROM dictionary 
            WHERE LENGTH(word) = @wordLength 
            LIMIT 1 OFFSET @offset";

        try
        {
            using (var connection = new MySqlConnection(connectionString))
            {
                await connection.OpenAsync();

                // 获取符合条件的单词总数
                using (var countCommand = new MySqlCommand(countQuery, connection))
                {
                    countCommand.Parameters.AddWithValue("@wordLength", wordLength);
                    var totalCount = Convert.ToInt32(await countCommand.ExecuteScalarAsync());

                    if (totalCount == 0)
                    {
                        return null; // 没有符合条件的单词
                    }

                    // 生成随机偏移量
                    Random random = new Random();
                    int offset = random.Next(0, totalCount);

                    // 获取随机单词
                    using (var wordCommand = new MySqlCommand(wordQuery, connection))
                    {
                        wordCommand.Parameters.AddWithValue("@wordLength", wordLength);
                        wordCommand.Parameters.AddWithValue("@offset", offset);
                        var result = await wordCommand.ExecuteScalarAsync();
                        return result?.ToString(); // 返回查询到的单词
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching word from database: {ex.Message}");
            return null; // 如果查询失败，则返回空
        }
    }

    // 获取单词的详细信息（发音、中文解释、例句）
    private async Task<WordDetails> GetWordDetailsAsync(string word)
    {
        string connectionString =
            "Server=localhost;Database=Dictionary;User ID=root;Password=347934;";
        string query = @"
            SELECT phonetic, definition, translation 
            FROM dictionary 
            WHERE word = @word";

        try
        {
            using (var connection = new MySqlConnection(connectionString))
            {
                await connection.OpenAsync();
                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@word", word);
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return new WordDetails
                            {
                                Phonetic = reader["phonetic"]?.ToString(),
                                Definitions = reader["translation"]?.ToString(),
                                ExampleSentence = reader["definition"]?.ToString()
                            };
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching word details from database: {ex.Message}");
        }

        return new WordDetails
        {
            Phonetic = "无法获取发音",
            Definitions = "无法获取定义",
            ExampleSentence = "无法获取例句"
        };
    }

    // 生成猜测的反馈
    private string GetGuessFeedback(string guessedWord)
    {
        var feedback = new List<string>();

        for (int i = 0; i < _wordLength; i++)
        {
            if (guessedWord[i] == _targetWord[i])
            {
                feedback.Add($"[{guessedWord[i]}]"); // 正确字母且位置正确 -> 绿色
            }
            else if (_targetWord.Contains(guessedWord[i]))
            {
                feedback.Add($"({guessedWord[i]})"); // 正确字母但位置不对 -> 红色
            }
            else
            {
                feedback.Add($"[{guessedWord[i]}]"); // 字母不在单词中 -> 灰色
            }
        }

        return string.Join(" ", feedback);
    }

    // 生成猜测结果图像
    private void DrawRoundedRectangle(Graphics graphics, Rectangle rect, Color fillColor, Color borderColor, int cornerRadius, float borderWidth)
    {
        using var path = new GraphicsPath();
        // 添加圆角
        path.AddArc(rect.X, rect.Y, cornerRadius, cornerRadius, 180, 90);
        path.AddArc(rect.Right - cornerRadius, rect.Y, cornerRadius, cornerRadius, 270, 90);
        path.AddArc(rect.Right - cornerRadius, rect.Bottom - cornerRadius, cornerRadius, cornerRadius, 0, 90);
        path.AddArc(rect.X, rect.Bottom - cornerRadius, cornerRadius, cornerRadius, 90, 90);
        path.CloseFigure();

        // 填充矩形
        using var fillBrush = new SolidBrush(fillColor);
        graphics.FillPath(fillBrush, path);

        // 绘制边框（加粗）
        using var borderPen = new Pen(borderColor, borderWidth);
        graphics.DrawPath(borderPen, path);
    }


private async Task<string> GenerateGuessImage(List<string> guesses, int wordLength)
{
    const int MaxRows = 7;
    int cols = wordLength;
    int cellSize = 70; // 单元格大小
    int fontSize = 30; // 字体大小
    int progressFontSize = 11; // 进度条字体大小
    int progressBarHeight = 20;
    int cornerRadius = 18; // 圆角半径
    int cellSpacing = 10; // 单元格间隔大小
    int horizontalPadding = 25; // 左右边距
    int verticalPadding = 25;// 上下编剧
    
    int bitmapWidth = (cols * (cellSize + cellSpacing) - cellSpacing) + (2 * horizontalPadding);
    int bitmapHeight = (MaxRows * (cellSize + cellSpacing) - cellSpacing) + progressBarHeight + verticalPadding * 2;

    // Wordle风格颜色
    Color correctColor = ColorTranslator.FromHtml("#6aaa64");
    Color presentColor = ColorTranslator.FromHtml("#c9b458");
    Color wrongColor = ColorTranslator.FromHtml("#787c7e");
    Color emptyBorderColor = Color.LightGray;
    Color emptyTextColor = ColorTranslator.FromHtml("#d3d6da");
    Color progressBarBackground = ColorTranslator.FromHtml("#e0e0e0");

    // 计算位图的宽度，包括左右边距
    using var bitmap = new Bitmap(
        bitmapWidth,bitmapHeight
    );

    using var graphics = Graphics.FromImage(bitmap);
    graphics.Clear(Color.White);

    using var font = new Font("Consolas", fontSize, FontStyle.Bold);
    using var progressFont = new Font("Consolas", progressFontSize, FontStyle.Bold);
    using var stringFormat = new StringFormat
    {
        Alignment = StringAlignment.Center,
        LineAlignment = StringAlignment.Center
    };

    // 计算已猜次数和剩余机会
    int totalGuesses = guesses.Count;
    int remainingAttempts = MaxRows - totalGuesses;

    // 绘制进度条
    int progressBarWidth = (int)((cols * cellSize) * 0.8); // 进度条宽度为 80%
    int progressBarX = (bitmap.Width - progressBarWidth) / 2;
    int progressBarY = horizontalPadding; // 向下移动进度条的位置，留出一行空白

    // 进度条背景
    graphics.FillRectangle(new SolidBrush(progressBarBackground), progressBarX, progressBarY, progressBarWidth, progressBarHeight);

    // 已猜进度
    int filledWidth = (int)(progressBarWidth * (totalGuesses / (float)MaxRows));
    graphics.FillRectangle(new SolidBrush(correctColor), progressBarX, progressBarY, filledWidth, progressBarHeight);

    // 绘制进度条文字
    string progressText = $"已猜 {totalGuesses}/{MaxRows} 次";
    graphics.DrawString(progressText, progressFont, Brushes.Black, new Rectangle(progressBarX, progressBarY, progressBarWidth, progressBarHeight), stringFormat);

    // 开始绘制猜测结果
    for (int row = 0; row < MaxRows; row++)
    {
        bool isGuessed = row < guesses.Count;

        string guess = isGuessed ? guesses[row] : string.Empty;
        List<Color> colors = isGuessed
            ? GetGuessColors(guess, correctColor, presentColor, wrongColor)
            : Enumerable.Repeat(Color.White, cols).ToList();

        for (int col = 0; col < cols; col++)
        {
            char letter = col < guess.Length ? guess[col] : ' ';
            // 计算单元格的矩形，包括间隔和左右边距
            var rect = new Rectangle(
                horizontalPadding + col * (cellSize + cellSpacing),
                verticalPadding + progressBarHeight + cellSpacing + row * (cellSize + cellSpacing),
                cellSize,
                cellSize
            );

            // 使用自定义方法绘制圆角矩形
            DrawRoundedRectangle(graphics, rect, colors[col], emptyBorderColor, cornerRadius,3f);

            // 字母颜色
            Brush textBrush = isGuessed ? Brushes.White : new SolidBrush(emptyTextColor);
            graphics.DrawString(letter.ToString().ToUpper(), font, textBrush, rect, stringFormat);
        }
    }

    string tempPath = Path.Combine(Path.GetTempPath(), $"guess_result_{Guid.NewGuid()}.png");
    bitmap.Save(tempPath, System.Drawing.Imaging.ImageFormat.Png);

    try
    {
        var uploaded = await CloudObjectStorage.UploadFileAsync(tempPath);
        return uploaded.Url;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"图片上传失败: {ex.Message}");
        throw;
    }
    finally
    {
        if (File.Exists(tempPath)) File.Delete(tempPath);
    }
}




    // 判断字母颜色
    private List<Color> GetGuessColors(string guess, Color correctColor, Color presentColor, Color wrongColor)
    {
        var result = new List<Color>();
        var targetWordUpper = _targetWord.ToUpper(); // 将目标单词转为大写

        // 记录哪些字母在目标单词中已经被使用过
        bool[] targetUsed = new bool[targetWordUpper.Length];

        // 第一遍：标记正确位置的字母
        for (int i = 0; i < guess.Length; i++)
        {
            char guessLetter = char.ToUpper(guess[i]); // 将猜测的字母转为大写
            if (i < targetWordUpper.Length && guessLetter == targetWordUpper[i])
            {
                result.Add(correctColor);
                targetUsed[i] = true; // 标记这个位置已经用过
            }
            else
            {
                result.Add(Color.Empty); // 先填充空色
            }
        }

        // 第二遍：标记位置不对的字母
        for (int i = 0; i < guess.Length; i++)
        {
            char guessLetter = char.ToUpper(guess[i]);
            if (result[i] == Color.Empty) // 只处理未标记的
            {
                for (int j = 0; j < targetWordUpper.Length; j++)
                {
                    if (!targetUsed[j] && guessLetter == targetWordUpper[j])
                    {
                        result[i] = presentColor;
                        targetUsed[j] = true; // 标记这个字母已被使用
                        break;
                    }
                }
            }
        }

        // 处理未猜中的字母，默认为 wrongColor
        for (int i = 0; i < result.Count; i++)
        {
            if (result[i] == Color.Empty)
            {
                result[i] = wrongColor;
            }
        }

        return result;
    }




    // 检查游戏是否已启动
    public bool IsGameStarted()
    {
        return _isGameStarted;
    }
}

// 用于存储API返回的单词详情
public class WordDetails
{
    public string Phonetic { get; set; }
    public string Definitions { get; set; }
    public string ExampleSentence { get; set; }
}
