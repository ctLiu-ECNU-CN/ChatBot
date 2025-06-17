using System;
using System.Collections.Generic;
using System.Linq;
using ConsoleApp1.models;

namespace ConsoleApp1.utils
{
    public static class HandEvaluator
    {
        //定义牌型
        public enum HandRank
        {
            HighCard = 1, // 高牌
            OnePair, // 对子
            TwoPair, // 两对子
            ThreeOfKind, // 三条
            Straight, // 顺子
            Flush, // 同花
            FullHouse, // 三条+两对
            FourOfKind, // 四条
            StraightFlush, // 同花顺
            RoyalFlush // 皇家同花顺
        }

        public class HandValue
        {
            public HandRank Rank { get; set; }
            public List<int> RankedValues { get; set; } = new List<int>();

            public override string ToString() => $"{Rank} ({string.Join(",", RankedValues)})";

            public int GetScore()
            {
                switch (Rank)
                {
                    case HandRank.HighCard:
                        return RankedValues.Max(); // 高牌，返回最大牌

                    case HandRank.OnePair:
                        return RankedValues[0] * 10; // 对子，返回对子牌的值，乘以10

                    case HandRank.TwoPair:
                        return RankedValues[0] * 10 + RankedValues[1]; // 两对，返回两对的值

                    case HandRank.ThreeOfKind:
                        return RankedValues[0] * 100; // 三条，乘以100

                    case HandRank.Straight:
                        return RankedValues[0]; // 顺子，返回顺子的最大牌

                    case HandRank.Flush:
                        return RankedValues.Max(); // 同花，返回最大牌

                    case HandRank.FullHouse:
                        return RankedValues[0] * 100 + RankedValues[1]; // 葫芦，返回三条和对子

                    case HandRank.FourOfKind:
                        return RankedValues[0] * 1000; // 四条，返回四条牌的值，乘以1000

                    case HandRank.StraightFlush:
                        return RankedValues[0] * 1000; // 同花顺，返回最高牌，乘以1000

                    case HandRank.RoyalFlush:
                        return 10000; // 皇家同花顺，固定返回最大分数

                    default:
                        throw new InvalidOperationException("Unknown HandRank");
                }
            }
        }

        private static readonly Dictionary<string, int> RankMap = new()
        {
            ["2"] = 2, ["3"] = 3, ["4"] = 4, ["5"] = 5, ["6"] = 6, ["7"] = 7,
            ["8"] = 8, ["9"] = 9, ["10"] = 10, ["J"] = 11, ["Q"] = 12,
            ["K"] = 13, ["A"] = 14
        };

        public static int CompareHands(List<Card> cards1, List<Card> cards2)
        {
            var best1 = EvaluateBestHand(cards1);
            var best2 = EvaluateBestHand(cards2);
            return CompareHandValues(best1, best2);
        }

        public static int CompareHandValues(HandValue h1, HandValue h2)
        {
            if (h1.Rank != h2.Rank)
                return h1.Rank.CompareTo(h2.Rank);

            for (int i = 0; i < h1.RankedValues.Count; i++)
            {
                if (i >= h2.RankedValues.Count) return 1;
                if (h1.RankedValues[i] > h2.RankedValues[i]) return 1;
                if (h1.RankedValues[i] < h2.RankedValues[i]) return -1;
            }
            return 0;
        }

        public static HandValue EvaluateBestHand(List<Card> cards)
        {
            if (cards.Count < 5)
                throw new ArgumentException("至少需要5张牌");

            var allCombos = GetAll5CardCombinations(cards);
            HandValue best = null;
            foreach (var combo in allCombos)
            {
                var value = EvaluateHand(combo);
                if (best == null || CompareHandValues(value, best) > 0)
                    best = value;
            }
            return best;
        }

        private static List<List<Card>> GetAll5CardCombinations(List<Card> cards)
        {
            var results = new List<List<Card>>();
            int n = cards.Count;
            for (int a = 0; a < n; a++)
                for (int b = a + 1; b < n; b++)
                    for (int c = b + 1; c < n; c++)
                        for (int d = c + 1; d < n; d++)
                            for (int e = d + 1; e < n; e++)
                                results.Add(new List<Card> { cards[a], cards[b], cards[c], cards[d], cards[e] });
            return results;
        }

        public static HandValue EvaluateHand(List<Card> cards)
        {
            var values = cards.Select(c => RankMap[c.Rank]).OrderByDescending(v => v).ToList();
            var suits = cards.Select(c => c.Suit).ToList();
            var isFlush = suits.Distinct().Count() == 1;
            var isStraight = IsStraight(values, out int highStraight);

            var groups = values.GroupBy(v => v).OrderByDescending(g => g.Count()).ThenByDescending(g => g.Key).ToList();
            var counts = groups.Select(g => g.Count()).ToList();

            if (isStraight && isFlush)
            {
                if (highStraight == 14) return new HandValue { Rank = HandRank.RoyalFlush, RankedValues = new List<int> { 14 } };
                return new HandValue { Rank = HandRank.StraightFlush, RankedValues = new List<int> { highStraight } };
            }

            if (counts[0] == 4)
                return new HandValue { Rank = HandRank.FourOfKind, RankedValues = new List<int> { groups[0].Key, groups[1].Key } };

            if (counts[0] == 3 && counts[1] >= 2)
                return new HandValue { Rank = HandRank.FullHouse, RankedValues = new List<int> { groups[0].Key, groups[1].Key } };

            if (isFlush)
                return new HandValue { Rank = HandRank.Flush, RankedValues = values };

            if (isStraight)
                return new HandValue { Rank = HandRank.Straight, RankedValues = new List<int> { highStraight } };

            if (counts[0] == 3)
                return new HandValue
                {
                    Rank = HandRank.ThreeOfKind,
                    RankedValues = new List<int> { groups[0].Key }.Concat(GetKickers(groups, 1)).ToList()
                };

            if (counts[0] == 2 && counts[1] == 2)
                return new HandValue
                {
                    Rank = HandRank.TwoPair,
                    RankedValues = new List<int> { groups[0].Key, groups[1].Key, groups[2].Key }
                };

            if (counts[0] == 2)
                return new HandValue
                {
                    Rank = HandRank.OnePair,
                    RankedValues = new List<int> { groups[0].Key }.Concat(GetKickers(groups, 1)).ToList()
                };

            return new HandValue { Rank = HandRank.HighCard, RankedValues = values };
        }

        private static bool IsStraight(List<int> values, out int highCard)
        {
            var distinct = values.Distinct().OrderByDescending(v => v).ToList();
            for (int i = 0; i <= distinct.Count - 5; i++)
            {
                bool isStraight = true;
                for (int j = 1; j < 5; j++)
                {
                    if (distinct[i + j - 1] - 1 != distinct[i + j])
                    {
                        isStraight = false;
                        break;
                    }
                }
                if (isStraight)
                {
                    highCard = distinct[i];
                    return true;
                }
            }

            if (distinct.Contains(14) && distinct.Contains(2) && distinct.Contains(3) &&
                distinct.Contains(4) && distinct.Contains(5))
            {
                highCard = 5;
                return true;
            }

            highCard = 0;
            return false;
        }

        private static List<int> GetKickers(List<IGrouping<int, int>> groups, int skip)
        {
            return groups.Skip(skip).Select(g => g.Key).ToList();
        }
    }
}
