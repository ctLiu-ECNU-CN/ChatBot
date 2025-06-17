using ConsoleApp1.models;
using ConsoleApp1.utils;
using MyBot.Models;

namespace ConsoleApp1.game;

public class TexasGameManager
{
    public int Pot { get; set; } = 0;
    public int CurrentBetAmount { get;  set; } = 0; // 当前轮最大下注额
    public int CurrentPlayerIndex { get;  set; } = 0; // 当前该谁行动
    public int DealerIndex { get;  set; } = 0; // 当前庄家索引

    public bool IsSomebodyAllInPlay { get;  set; } = false;// 是否有人All in
    public bool GameStarted { get;  set; } = false;
    public List<TexasPlayer> Players { get; } = new(); // 当前游戏参与的玩家列表
    public List<string> ReadyPlayers { get; } = new(); // 已准备的玩家 UserId
    public string GroupId { get; private set; } // 当前所在群聊 ID
    public Deck Deck { get; private set; } // 当前牌堆
    public List<Card> CommunityCards { get; private set; } = new(); // 公共牌列表
    public int CurrentBetRound { get; set; } = -1; // 当前轮次：-1-未开始, 0-盲注, 1-翻牌, 2-转牌, 3-河牌, 4-摊牌

    public void InitGame()
    {
        ReadyPlayers.Clear();
        CommunityCards.Clear();
        GameStarted = false;
        CurrentBetRound = -1;
        DealerIndex = 0;
        //CurrentPlayerIndex = 0;
        Players.Clear();
        Console.WriteLine("游戏初始化完成");
    }
    
    // 如果是true,那么当前下注轮结束
    public bool IsBettingRoundOver()
    {
        // 获取未弃牌、未All-In的玩家
        var activePlayers = Players
            .Where(p => !p.IsFolded && !p.IsAllIn)
            .ToList();

        // 若只有一个未弃牌玩家，则轮结束
        if (Players.Count(p => !p.IsFolded) <= 1)
            return true;

        // 如果还有未操作的玩家或者下注金额不同，则尚未结束
        int? expectedBet = null;
        foreach (var player in activePlayers)
        {
            if (expectedBet == null)
                expectedBet = player.CurrentBet;
            else if (player.CurrentBet != expectedBet)
                return false;
        }

        CurrentBetRound++;
        return true;
    }

    
    // 每一轮结束后,重置下注额度
    public void ResetBetsForNewRound()
    {
        foreach (var player in Players)
        {
            player.CurrentBet = 0;
        }

        CurrentBetAmount = 0;
    }
    
    public class HandResult
    {
        public string HandName { get; set; }    // 比如 "同花顺"
        public int Rank { get; set; }           // 数值越大牌越大
    }
    


    private List<string> RolePool { get; set; } = new List<string>
    {
        "公爵", "猛禽", "天使", "小丑", "猎鹰", "战士", "魔术师", "龙骑士", "忍者", "王子", "刺客", "原神玩家"
    };

    public string AddPlayer(string userId)
    {
        if (Players.Any(p => p.UserId == userId)) return null;

        var random = new Random();
        if (RolePool.Count == 0)
        {
            Console.WriteLine("没有足够的角色名可供分配。");
            return null;
        }

        int roleIndex = random.Next(RolePool.Count);
        string playerRole = RolePool[roleIndex];
        RolePool.RemoveAt(roleIndex);

        var player = new TexasPlayer(userId: userId)
        {
            Role = playerRole
        };

        Players.Add(player);
        CurrentPlayerIndex = (DealerIndex + 2)%Players.Count;
        Console.WriteLine($"玩家 {userId} 加入游戏，角色为 {playerRole}。当前玩家: {string.Join(", ", Players.Select(p => $"{p.UserId}({p.Role})"))}");
        
        return playerRole;
    }

    public void MarkReady(string userId)
    {
        if (!ReadyPlayers.Contains(userId))
            ReadyPlayers.Add(userId);

        Console.WriteLine($"玩家 {userId} 已准备，当前准备人数: {ReadyPlayers.Count}/{Players.Count}");

        if (ReadyPlayers.Count == Players.Count && Players.Count >= 2)
        {
            StartGame();
        }
    }

    public void StartGame()
    {
        Console.WriteLine("游戏开始，洗牌并准备发牌！");
        Deck = new Deck();
        Deck.Shuffle();
        GameStarted = true;
        CurrentBetRound = 0;

        foreach (var player in Players)
        {
            player.HandCards.Clear();
            player.HandCards.Add(Deck.Draw());
            player.HandCards.Add(Deck.Draw());
        }
    }

    public int GetBBPlayerIndex()
    {
        return (DealerIndex + 2)%Players.Count;
    }
    
    public int GetSBPlayerIndex()
    {
        return (DealerIndex + 1)%Players.Count;
    }
    
    public string GetBBUserId()
    {
        return Players[(DealerIndex + 2)%Players.Count].UserId;
    }
    public string GetSBUserId()
    {
        return Players[(DealerIndex + 1)%Players.Count].UserId;
    }
    
    public void AdvanceToNextPlayer()
    {
        do
        {
            CurrentPlayerIndex = (CurrentPlayerIndex + 1) % Players.Count;
        } while (Players[CurrentPlayerIndex].IsFolded || Players[CurrentPlayerIndex].IsAllIn);
    }

    public string HandleAction(string userId, string action, int raiseAmount = 0)
    {
        var player = Players.FirstOrDefault(p => p.UserId == userId);
        if (player == null || player.IsFolded || player.IsAllIn) return "无效玩家或状态。";

        Console.WriteLine($"当前游戏阶段:{CurrentBetRound} \t 处理用户${userId}\t 操作:{action}");
        action = action.ToLower();
        if (CurrentBetRound == 0 && action == "blind")
        {
            int smallBlind = 10;
            int bigBlind = 20;
            int playerIndex = Players.IndexOf(player);
            if (playerIndex == (DealerIndex + 1) % Players.Count)
            {
                player.Chips -= smallBlind;
                player.CurrentBet = smallBlind;
                Pot += smallBlind;
                return $"玩家 {player.Role} 支付小盲注 {smallBlind}。";
            }
            else if (playerIndex == (DealerIndex + 2) % Players.Count)
            {
                player.Chips -= bigBlind;
                player.CurrentBet = bigBlind;
                Pot += bigBlind;
                CurrentBetAmount = bigBlind;
                return $"玩家 {player.Role} 支付大盲注 {bigBlind}。";
            }
            else
            {
                return "当前不需要盲注。";
            }
        }

        switch (action)
        {
            case "call":
                int toCall = CurrentBetAmount - player.CurrentBet;
                if (player.Chips <= toCall)
                {
                    Pot += player.Chips;
                    player.CurrentBet += player.Chips;
                    player.Chips = 0;
                    player.IsAllIn = true;
                    AdvanceToNextPlayer();
                    return $"{player.Role} 选择全押（All-in）！";
                }
                else
                {
                    player.Chips -= toCall;
                    player.CurrentBet += toCall;
                    Pot += toCall;
                    return $"{player.Role} 跟注 {toCall} 筹码。";
                }

            case "raise":
                if (raiseAmount <= 0 || raiseAmount <= (CurrentBetAmount - player.CurrentBet))
                    return "加注金额不足。";

                int totalRaise = raiseAmount + (CurrentBetAmount - player.CurrentBet);
                if (player.Chips < totalRaise)
                    return "筹码不足以加注。";

                player.Chips -= totalRaise;
                player.CurrentBet += totalRaise;
                CurrentBetAmount = player.CurrentBet;
                Pot += totalRaise;
                AdvanceToNextPlayer();
                return $"{player.Role} 加注到 {CurrentBetAmount}。";

            case "fold":
                player.IsFolded = true;
                AdvanceToNextPlayer();
                return $"{player.Role} 弃牌了。";

            default:
                return "未知操作。";
        }
    }

    public List<Card> DealHandCards(string userId)
    {
        var player = Players.FirstOrDefault(p => p.UserId == userId);
        if (player == null)
        {
            Console.WriteLine($"玩家 {userId} 不在游戏中。");
            return new List<Card>();
        }

        return player.HandCards;
    }

    public List<Card> RevealCommunityCards()
    {
        if (!GameStarted || Deck == null || !Deck.HasCards())
        {
            Console.WriteLine("不能翻牌，游戏未开始或牌堆不足。");
            return new List<Card>();
        }

        switch (CurrentBetRound)
        {
            case 1:
                Deck.Draw();
                CommunityCards.Add(Deck.Draw());
                CommunityCards.Add(Deck.Draw());
                CommunityCards.Add(Deck.Draw());
                CurrentBetRound = 2;
                break;
            case 2:
                Deck.Draw();
                CommunityCards.Add(Deck.Draw());
                CurrentBetRound = 3;
                break;
            case 3:
                Deck.Draw();
                CommunityCards.Add(Deck.Draw());
                CurrentBetRound = 4;
                break;
            case 4:
                Console.WriteLine("所有公共牌已翻完。");
                break;
        }

        return new List<Card>(CommunityCards);
    }
    
    public void ResetForNewRound()
    {
        foreach (var player in Players)
        {
            player.HandCards.Clear();
            player.CurrentBet = 0;
            player.IsFolded = false;
            player.IsAllIn = false;
        }

        CommunityCards.Clear();
        Pot = 0;
        CurrentBetAmount = 0;
        CurrentPlayerIndex = 0;
        CurrentBetRound = -1;
    }


    public void ResetGame()
    {
        Console.WriteLine("重置游戏状态。");
        GameStarted = false;
        Players.Clear();
        ReadyPlayers.Clear();
        CommunityCards.Clear();
        CurrentBetRound = 0;
    }

    public TexasPlayer GetCurrentPlayer()
    {
        if (Players.Count == 0) return null;

        var player = Players[CurrentPlayerIndex];
        if (!player.IsFolded && !player.IsAllIn)
            return player;

        return null; // 当前玩家无法操作，但我们不在这里推进
    }


    public void NextPlayer()
    {
        for (int i = 1; i <= Players.Count; i++)
        {
            int idx = (CurrentPlayerIndex + i) % Players.Count;
            var next = Players[idx];
            if (!next.IsFolded && !next.IsAllIn)
            {
                CurrentPlayerIndex = idx;
                return;
            }
        }
    }

    public void BeginBettingRound()
    {
        foreach (var player in Players)
        {
            player.CurrentBet = 0;
        }

        CurrentBetAmount = 0;
        CurrentPlayerIndex = 0;

        Console.WriteLine("新一轮下注开始！");
    }

    public void Showdown()
    {
        if (!GameStarted || CurrentBetRound < 4)
        {
            Console.WriteLine("未到摊牌阶段，无法判断胜负。");
            return;
        }

        Console.WriteLine("进入摊牌阶段，开始评估所有玩家手牌！");

        var results = new List<(TexasPlayer Player, int Score)>();

        foreach (var player in Players)
        {
            if (player.HandCards.Count != 2) continue;

            var combined = new List<Card>(player.HandCards);
            combined.AddRange(CommunityCards);
            utils.HandEvaluator.HandValue value = HandEvaluator.EvaluateHand(combined);
            int score = value.GetScore();

            results.Add((player, score));

            Console.WriteLine($"玩家 {player.UserId} 分数: {score} 手牌: {string.Join(",", player.HandCards.Select(c => c.ToString()))}");
        }

        var maxScore = results.Max(r => r.Score);
        var winners = results.Where(r => r.Score == maxScore).Select(r => r.Player).ToList();

        Console.WriteLine($"胜者: {string.Join(", ", winners.Select(p => p.UserId))}，得分: {maxScore}");
    }

    public int GetPlayerIndexById(string userId)
    {
        var player = Players.FirstOrDefault(p => p.UserId == userId);
        return player != null ? Players.IndexOf(player) : -1;
    }

    public void SetDealer()
    {
        DealerIndex = (DealerIndex + 1) % Players.Count;
        Console.WriteLine($"当前庄家是 {Players[DealerIndex].UserId} ({Players[DealerIndex].Role})");
    }
    
}