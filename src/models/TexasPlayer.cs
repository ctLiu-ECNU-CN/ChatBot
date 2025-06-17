namespace ConsoleApp1.models;

public class TexasPlayer
{
    public string UserId { get; set; }
    public List<Card> HandCards { get; set; } = new();
    public int Chips { get; set; } = 5000; // 初始筹码
    public int CurrentBet { get; set; } = 0; // 当前下注额
    public bool IsFolded { get; set; } = false; // 是否弃牌
    public bool IsAllIn { get; set; } = false; // 是否 All-In
    public string Role { get; set; }
    public TexasPlayer(string userId)
    {
        UserId = userId;
    }
}

