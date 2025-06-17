using MyBot.Api;
using MyBot.Models.MessageModels;

namespace ConsoleApp1.utils;

// 私聊消息服务类
public class PrivateMessageService
{
    private readonly QQChannelApi _apiProvider;
    private readonly ChatMessageApi _chatMessageApi;
    // 用于记录群聊中 @ 机器人的消息与用户 openId 的映射
    private Dictionary<string, string> _userPrivateMessageMap = new Dictionary<string, string>();

    public PrivateMessageService(QQChannelApi apiProvider)
    {
        _apiProvider = apiProvider;
        _chatMessageApi = apiProvider.GetChatMessageApi();
    }

    // 处理群聊中的 @ 机器人消息，获取参数并提醒用户私聊
    public async Task HandleGroupMessageAsync(ChatMessage message)
    {
        if (message.Content.Contains("/私聊"))
        {
            string userMessage = message.Content.Trim();
            string userId = NormalizeUserId(message.Author.MemberOpenId);  // 标准化 MemberOpenId
            string parameter = userMessage.Replace("/私聊", "").Trim(); // 获取参数
            Console.WriteLine($"得到参数：{parameter}\n user:{userId}");
            // 保存用户问题参数
            _userPrivateMessageMap[userId] = parameter;

            // 回复群聊：提醒用户私聊机器人
            await _chatMessageApi.SendGroupMessageAsync(message.GroupOpenId, "请私聊我", passiveMsgId: message.Id);
        }
    }

    private string NormalizeUserId(string userId)
    {
        return userId?.Trim().ToUpperInvariant();  // 去除空格并统一为大写
    }


    // 处理用户私聊消息
    public async Task HandlePrivateMessageAsync(ChatMessage message)
    {
        string userId = NormalizeUserId(message.Author.UserOpenId);  // 标准化 UserOpenId
        Console.WriteLine($"用户私聊id:{userId}");
        // 判断该用户是否之前发送过参数信息
        if (_userPrivateMessageMap.ContainsKey(userId))
        {
            string previousParameter = _userPrivateMessageMap[userId];
            // 回复私聊用户：告诉他们之前发送的参数
            await SendPrivateMessage(userId, $"你刚刚说的是: {previousParameter}", message);
        }
        else
        {
            // 如果没有参数信息，提醒用户
            await SendPrivateMessage(userId, "请先在群聊中使用 /私聊 命令，传递参数给我。", message);
        }
    }


    // 发送私聊消息给用户
    private async Task SendPrivateMessage(string userId, string msg,ChatMessage message)
    {
        var api = _apiProvider.GetChatMessageApi();
        var response = await api.SendUserMessageAsync(userId, msg,passiveMsgId:message.Id);
    }
}

