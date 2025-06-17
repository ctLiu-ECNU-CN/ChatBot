using ConsoleApp1.models;
using ConsoleApp1.utils;
using MyBot.Api;
using MyBot.Models.MessageModels;

namespace ConsoleApp1.game;

public class TexasMessageService
{
    private readonly TexasGameManager _manager;
    private readonly ChatMessageApi _api;
    private static int msg_sql = 18514;

    public TexasMessageService(TexasGameManager manager, ChatMessageApi api)
    {
        _manager = manager;
        _api = api;
    }

    // 游戏玩家加入阶段的消息交互
    public async Task HandleJoinGameAsync(ChatMessage message)
    {
        Console.WriteLine(_manager.GameStarted);
        //如果游戏未开始
        if (!_manager.GameStarted)
        {
            var role = _manager.AddPlayer(message.Author.MemberOpenId);
            if (role != null)
            {
                try
                {
                    await _api.SendGroupMessageAsync(message.GroupOpenId, $"扮演{role}已加入对局！",
                        msgSeq:
                        (msg_sql++) % 113,passiveMsgId:message.Id);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    
                }
            }
            else
            {
                await _api.SendGroupMessageAsync(message.GroupOpenId, $"用户加入失败！请重试",
                    msgSeq:
                    (msg_sql++) % 113,passiveMsgId:message.Id);
            }
        }
        else
        {
            await _api.SendGroupMessageAsync(message.GroupOpenId, $" 游戏已经开始啦，等他们结束吧",msgSeq:
                (msg_sql++)%113,passiveMsgId:message.Id);
        }
    }
    //盲注阶段处理

    
    // 摊牌阶段
    public async Task ShowdownAsync(ChatMessage message)
    {
        // 获取未弃牌的玩家
        var alivePlayers = _manager.Players.Where(p => !p.IsFolded).ToList();
        var results = new List<(TexasPlayer player, int score)>();
        

        // 评估每个玩家的手牌
        foreach (var player in alivePlayers)
        {
            var allCards = new List<Card>(player.HandCards);
            allCards.AddRange(_manager.CommunityCards);

            // 评估手牌并获取分数
            var handResult = HandEvaluator.EvaluateHand(allCards);
            results.Add((player, handResult.GetScore()));
        }

        // 找出最大分数
        int maxScore = results.Max(r => r.score);

        // 找出所有得分相同的玩家（可能并列）
        var winners = results.Where(r => r.score == maxScore).Select(r => r.player).ToList();

// 构造展示信息
        var showMessage = "【摊牌阶段】\n";
        foreach (var (player, result) in results)
        {
            var cardsStr = string.Join(" ", player.HandCards.Select(c => c.ToString()));
            showMessage += $"{player.UserId} 手牌：{cardsStr} —— 得分：{result}\n"; // 显示手牌和得分
        }

// 显示赢家信息
        showMessage += $"\n赢家：{string.Join("、", winners.Select(w => w.UserId))}\n" +
                       $"赢得彩池：{_manager.Pot} 筹码";

        await _api.SendGroupMessageAsync(message.GroupOpenId, showMessage,
            msgSeq: (msg_sql++) % 113, passiveMsgId: message.Id);

// 重置游戏状态
        _manager.ResetBetsForNewRound();

    }
    
    
    public async Task HandleBetActionAsync(ChatMessage message)
    {
        string[] parts = message.Content.Trim().Split(' ');
        string action = parts[0].Replace("/", "").ToLower(); // 统一操作名
        int raiseAmount = 0;

        if (action == "raise" && parts.Length >= 2)
        {
            if (!int.TryParse(parts[1], out raiseAmount))
            {
                await _api.SendGroupMessageAsync(message.GroupOpenId, "加注金额格式错误。",
                    msgSeq: (msg_sql++) % 113, passiveMsgId: message.Id);
                return;
            }
        }

        // 下注阶段 1-3
        if (_manager.CurrentBetRound is >= 1 and <= 3)
        {
            var currentPlayer = _manager.GetCurrentPlayer();
            if (currentPlayer.UserId != message.Author.MemberOpenId)
            {
                await _api.SendGroupMessageAsync(message.GroupOpenId, "还不是你操作的轮次。",
                    msgSeq: (msg_sql++) % 113, passiveMsgId: message.Id);
                return;
            }

            string result = _manager.HandleAction(currentPlayer.UserId, action, raiseAmount);

            await _api.SendGroupMessageAsync(message.GroupOpenId, result,
                msgSeq: (msg_sql++) % 113, passiveMsgId: message.Id);

            if (result.Contains("未知") || result.Contains("无效"))
                return; // 操作失败，不推进游戏

            // 操作成功，检查是否一轮结束
            if (_manager.IsBettingRoundOver())
            {
                Console.WriteLine("当前下注轮次结束");
            }
            else
            {
                _manager.AdvanceToNextPlayer();

                var nextPlayer = _manager.GetCurrentPlayer();
                await _api.SendGroupMessageAsync(message.GroupOpenId,
                    $"轮到 {nextPlayer.Role} 操作。剩余筹码:{nextPlayer.Chips} 当前最大下注:{_manager.CurrentBetAmount}",
                    msgSeq: (msg_sql++) % 113, passiveMsgId: message.Id);
            }
        }
        else
        {
            await _api.SendGroupMessageAsync(message.GroupOpenId, "当前不在下注阶段。",
                msgSeq: (msg_sql++) % 113, passiveMsgId: message.Id);
        }
    }




    
    // 准备阶段
   public async Task HandleReadyAsync(ChatMessage message)
{
    if (_manager.CurrentBetRound != -1)
        return; // 游戏已经开始了

    if (_manager.ReadyPlayers.Contains(message.Author.MemberOpenId))
    {
        await _api.SendGroupMessageAsync(message.GroupOpenId, "你已经准备过了！",
            msgSeq: (msg_sql++) % 113, passiveMsgId: message.Id);
        return;
    }

    _manager.MarkReady(message.Author.MemberOpenId);
    await _api.SendGroupMessageAsync(message.GroupOpenId, 
        $"玩家已准备，已准备 {_manager.ReadyPlayers.Count}/{_manager.Players.Count}",
        msgSeq: (msg_sql++) % 113, passiveMsgId: message.Id);
    await _api.SendGroupMessageAsync(message.GroupOpenId, 
        $"游戏开始后，请私聊我查看底牌", 
        msgSeq: (msg_sql++) % 113, passiveMsgId: message.Id);

    if (_manager.Players.Count < 2)
    {
        await _api.SendGroupMessageAsync(message.GroupOpenId, 
            "至少需要2名玩家才能开始游戏。",
            msgSeq: (msg_sql++) % 113, passiveMsgId: message.Id);
        return;
    }

    if (_manager.ReadyPlayers.Count == _manager.Players.Count)
    {
        _manager.StartGame(); // 应该在里面设置CurrentBetRound=0
        Console.WriteLine(_manager.GameStarted);
    }

    if (_manager.GameStarted)
    {
        await _api.SendGroupMessageAsync(message.GroupOpenId, 
            $"所有玩家已准备，底牌已经发放, 私聊任意内容查看底牌！",
            msgSeq: (msg_sql++) % 113, passiveMsgId: message.Id);

        // 小盲注
        var sbResult = _manager.HandleAction(_manager.GetSBUserId(), "blind", _manager.CurrentBetAmount / 2);
        await _api.SendGroupMessageAsync(message.GroupOpenId, sbResult,
            msgSeq: (msg_sql++) % 113, passiveMsgId: message.Id);

        // 大盲注
        var bbResult = _manager.HandleAction(_manager.GetBBUserId(), "blind", _manager.CurrentBetAmount);
        await _api.SendGroupMessageAsync(message.GroupOpenId, bbResult,
            msgSeq: (msg_sql++) % 113, passiveMsgId: message.Id);

        _manager.AdvanceToNextPlayer();

        var nextPlayer = _manager.GetCurrentPlayer();
        NoticeCurrentPlayerAsync(message);
    }
}

public async Task NoticeCurrentPlayerAsync(ChatMessage message)
{
    await _api.SendGroupMessageAsync(message.GroupOpenId,
        $"\n轮到玩家{_manager.GetCurrentPlayer().Role}操作！" +
        $"\n\t可以选择以下操作" +
        $"\n\t\t- 跟注（Call）" +
        $"\n\t\t- 加注（Raise）" +
        $"\n\t\t- 弃牌（Fold）" +
        $"\n\t\t- 全押（All-in）",
        msgSeq: (msg_sql++) % 113, passiveMsgId: message.Id);
}


    //单聊，用于看底牌
    public async Task HandlePrivateMessageAsync(ChatMessage message)
    {
        Console.WriteLine($"收到玩家 {message.Author.UserOpenId} 的消息: {message.Content}");

        if (_manager.GameStarted == false)
        {
            await _api.SendUserMessageAsync(message.Author.UserOpenId, "等待足够多的玩家准备。", passiveMsgId: message.Id);
            return;
        }
        var cards = _manager.DealHandCards(message.Author.UserOpenId);
        
        // 确保 cards 不为空，并且有手牌
        if (cards.Count == 0)
        {
            await _api.SendUserMessageAsync(message.Author.UserOpenId, "你还没有加入游戏或尚未准备。", passiveMsgId: message.Id);
            return;
        }

        await _api.SendUserMessageAsync(message.Author.UserOpenId, $"你的底牌是：{cards[0]} 和 {cards[1]}", passiveMsgId: message.Id, msgSeq: (msg_sql++) % 113);
    }


    public async Task HandleInGameCommandAsync(ChatMessage message)
    {
        var userId = message.Author.MemberOpenId;
        string[] messageParts = message.Content.Trim().Split(' ');
        string content = messageParts[0];
        Console.WriteLine($"HandleInGameCommandAsync被调用\n" +
                          "\t 命令:{content}");

        //var game = _manager.GetGame(message.GroupOpenId);
        if (_manager == null)
        {
            await _api.SendGroupMessageAsync(message.GroupOpenId, "当前没有正在进行的德州扑克游戏。",
                msgSeq: (msg_sql++) % 113, passiveMsgId: message.Id);
            return;
        }

        if (!_manager.GameStarted)
        {
            await _api.SendGroupMessageAsync(message.GroupOpenId, "游戏尚未开始。",
                msgSeq: (msg_sql++) % 113, passiveMsgId: message.Id);
            return;
        }

        if (_manager.GetCurrentPlayer().UserId != userId)
        {
            await _api.SendGroupMessageAsync(message.GroupOpenId, "还没轮到你行动，请等待其他玩家。",
                msgSeq: (msg_sql++) % 113, passiveMsgId: message.Id);
            return;
        }

        string response = "";
        if (content.StartsWith("/弃牌"))
        {
            Console.WriteLine($"HandleIngame方法:{content}");
            response = _manager.HandleAction(userId, "fold");
        }
        else if (content.StartsWith("/跟注"))
        {
            Console.WriteLine($"HandleIngame方法:{content}");
            response = _manager.HandleAction(userId, "call");
        }
        else if (content.StartsWith("/加注"))
        {
            Console.WriteLine($"HandleIngame方法:{content}");
            var parts = int.Parse(messageParts[1]);
            if (messageParts.Length < 2 || !int.TryParse(messageParts[1], out int raiseAmount) || raiseAmount <= 0)
            {
                await _api.SendGroupMessageAsync(message.GroupOpenId, "请输入有效的加注金额，例如：/加注 50",
                    msgSeq: (msg_sql++) % 113, passiveMsgId: message.Id);
                return;
            }

            // 判断加注是否合法：大于当前下注金额
            if (raiseAmount < _manager.CurrentBetAmount)
            {
                await _api.SendGroupMessageAsync(message.GroupOpenId, $"加注金额必须大于当前下注金额（当前：{_manager.CurrentBetAmount}）",
                    msgSeq: (msg_sql++) % 113, passiveMsgId: message.Id);
                return;
            }

            // 判断玩家余额是否足够
            var player = _manager.GetCurrentPlayer();
            if (player == null || player.Chips < raiseAmount)
            {
                await _api.SendGroupMessageAsync(message.GroupOpenId, $"你的筹码不足，当前筹码：{player?.Chips ?? 0}",
                    msgSeq: (msg_sql++) % 113, passiveMsgId: message.Id);
                return;
            }
            response = _manager.HandleAction(userId, "raise", raiseAmount);
        }
        else
        {
            await _api.SendGroupMessageAsync(message.GroupOpenId, "无效命令，请使用 /弃牌 /跟注 /加注",
                msgSeq: (msg_sql++) % 113, passiveMsgId: message.Id);
            return;
        }

        await _api.SendGroupMessageAsync(message.GroupOpenId, response,
            msgSeq: (msg_sql++) % 113, passiveMsgId: message.Id);
    }
}