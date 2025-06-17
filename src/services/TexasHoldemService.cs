using ConsoleApp1.game;
using ConsoleApp1.models;

public class TexasHoldemService
{
    public string JoinGame(string groupId, string userId)
    {
        var session = GameSessionManager.GetOrCreateSession(groupId);
        if (!session.Players.ContainsKey(userId))
        {
            session.Players[userId] = new Player { UserId = userId };
            return $"玩家 {userId} 加入了德州扑克对局。";
        }
        return $"你已在对局中。";
    }

    public string MarkReady(string groupId, string userId)
    {
        var session = GameSessionManager.GetOrCreateSession(groupId);
        if (!session.Players.ContainsKey(userId))
            return "你还未加入对局，请先发送 /加入对局";

        session.Players[userId].IsReady = true;

        // 发牌信息
        return "请私聊我发送 /底牌 查看你的手牌。";
    }

    public string DealHoleCards(string userId, string groupId)
    {
        var session = GameSessionManager.GetOrCreateSession(groupId);
        if (!session.Players.TryGetValue(userId, out var player)) return "你不在游戏中。";
        if (player.HoleCards.Count == 2) return "你已经拿到手牌了。";

        player.HoleCards.Add(session.Deck.DrawCard());
        player.HoleCards.Add(session.Deck.DrawCard());
        return $"你的手牌是：{string.Join(" ", player.HoleCards)}";
    }

    public string TryStartGame(string groupId)
    {
        var session = GameSessionManager.GetOrCreateSession(groupId);
        if (session.IsStarted) return "游戏已开始。";

        if (session.Players.Values.All(p => p.IsReady))
        {
            session.IsStarted = true;
            return $"所有玩家已准备，德州扑克对局开始！请准备下注～";
        }

        return "仍有玩家未准备。";
    }
}