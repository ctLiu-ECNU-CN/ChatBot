using ConsoleApp1.models;

namespace ConsoleApp1.game;

public static class GameSessionManager
{
    private static readonly Dictionary<string, GameSession> Sessions = new();

    public static GameSession GetOrCreateSession(string groupId)
    {
        if (!Sessions.ContainsKey(groupId))
            Sessions[groupId] = new GameSession(groupId);
        return Sessions[groupId];
    }

    public static void ResetSession(string groupId) => Sessions.Remove(groupId);
}