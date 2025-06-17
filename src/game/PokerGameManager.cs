namespace ConsoleApp1.game;


    public class PokerGameManager
    {
        public bool GameStarted { get; private set; } = false;
        public List<string> Players { get; private set; } = new();
        public HashSet<string> ReadyPlayers { get; private set; } = new();
        public Dictionary<string, List<string>> PlayerHands { get; private set; } = new();

        private List<string> deck;

        public void JoinGame(string userId)
        {
            if (!Players.Contains(userId))
                Players.Add(userId);
        }

        public bool MarkReady(string userId)
        {
            if (!Players.Contains(userId)) return false;
            ReadyPlayers.Add(userId);
            return ReadyPlayers.Count == Players.Count;
        }

        public void StartGame()
        {
            GameStarted = true;
            ShuffleDeck();
            DealHands();
        }

        private void ShuffleDeck()
        {
            var suits = new[] { "♠", "♥", "♣", "♦" };
            var ranks = new[] { "2", "3", "4", "5", "6", "7", "8", "9", "10", "J", "Q", "K", "A" };
            deck = suits.SelectMany(s => ranks.Select(r => $"{s}{r}")).ToList();
            var rnd = new Random();
            deck = deck.OrderBy(_ => rnd.Next()).ToList();
        }

        private void DealHands()
        {
            foreach (var player in Players)
            {
                PlayerHands[player] = deck.Take(2).ToList();
                deck.RemoveRange(0, 2);
            }
        }

        public List<string> GetPlayerHand(string userId)
        {
            return PlayerHands.ContainsKey(userId) ? PlayerHands[userId] : new List<string>();
        }
    }
