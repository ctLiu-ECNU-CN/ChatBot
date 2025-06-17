namespace ConsoleApp1.models;

public class Player
{
    public string UserId { get; set; }
    public bool IsReady { get; set; } = false;
    public List<Card> HoleCards { get; set; } = new();
    
    public string Role { get; set; }
}


public class GameSession
{
    public string GroupId { get; }
    public Dictionary<string, Player> Players { get; } = new();
    public List<Card> CommunityCards { get; } = new();
    public PokerDeck Deck { get; } = new();
    public bool IsStarted { get; set; } = false;

    public GameSession(string groupId) => GroupId = groupId;
}

public class PokerDeck
{
    private static readonly string[] Suits = { "♠", "♥", "♣", "♦" };
    private static readonly string[] Ranks = { "2", "3", "4", "5", "6", "7", "8", "9", "10", "J", "Q", "K", "A" };
    private List<Card> cards;
    private Random random = new();

    public PokerDeck()
    {
        cards = (from s in Suits from r in Ranks select new Card(s, r)).ToList();
    }

    public Card DrawCard()
    {
        if (cards.Count == 0) throw new InvalidOperationException("卡牌已抽完");
        var index = random.Next(cards.Count);
        var card = cards[index];
        cards.RemoveAt(index);
        return card;
    }
}


