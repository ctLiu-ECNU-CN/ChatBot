namespace ConsoleApp1.models;
public class Card
{
    public string Suit { get; }
    public string Rank { get; }
    public override string ToString() => $"{Rank}{Suit}";

    public Card(string suit, string rank)
    {
        Suit = suit; Rank = rank;
    }
}