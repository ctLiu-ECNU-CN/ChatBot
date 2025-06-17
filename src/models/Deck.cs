namespace ConsoleApp1.models;



public class Deck
{
    private Stack<Card> _cards = new();

    public Deck()
    {
        var suits = new[] { "♠", "♥", "♣", "♦" };
        var ranks = new[] { "2", "3", "4", "5", "6", "7", "8", "9", "10", "J", "Q", "K", "A" };
        var allCards = from suit in suits
            from rank in ranks
            select new Card(suit, rank);
        _cards = new Stack<Card>(allCards);
    }

    public void Shuffle()
    {
        var rng = new Random();
        _cards = new Stack<Card>(_cards.OrderBy(_ => rng.Next()));
    }

    public Card Draw() => _cards.Pop();
    
    public bool HasCards()
    {
        return _cards.Count > 0;
    }
}
