using System.Collections.Generic;
using SushiRun.Core;
using SushiRun.Core.Rules;
using Xunit;

namespace SushiRun.Core.Tests
{
    public class RoundRulesTests
    {
        // 로블록스판 Rules.luau로 만든 정답 (시작 인원 → 라운드마다 남은 인원). 이식이 같은 답을 내는지 고정해요.
        private const string LuauGolden = "4,2,1;5,3,2,1;6,4,2,1;7,4,2,1;8,5,3,1;9,6,3,2,1;10,7,4,2,1;11,7,4,2,1;12,8,4,2,1;13,8,4,2,1;14,9,5,3,1;15,10,6,3,1;16,10,6,3,1;17,11,6,3,1;18,12,7,4,1;19,12,7,4,1;20,13,7,4,1;21,14,8,4,1;22,14,8,4,1;23,15,8,4,1;24,16,9,5,1;25,15,7,3,1;26,16,7,3,1;27,16,7,3,1;28,17,8,3,1;29,17,8,3,1;30,18,8,3,1;31,19,9,4,1;32,19,9,4,1;33,20,9,4,1;34,20,9,4,1;35,21,9,4,1;36,22,10,4,1;37,22,10,4,1;38,23,10,4,1;39,23,10,4,1;40,24,11,4,1";

        private static List<int> Simulate(int start)
        {
            int roundCount = RoundRules.RoundCount(start);
            var seq = new List<int> { start };
            int alive = start;
            int? round = 1;
            while (round.HasValue)
            {
                alive = RoundRules.QualifyCount(alive, roundCount, round.Value, start);
                seq.Add(alive);
                round = RoundRules.NextRound(round.Value, roundCount, alive);
            }
            return seq;
        }

        [Fact]
        public void MatchesLuauForEveryRoomSize()
        {
            foreach (string entry in LuauGolden.Split(';'))
            {
                var expected = new List<int>();
                foreach (string n in entry.Split(','))
                {
                    expected.Add(int.Parse(n));
                }
                Assert.Equal(expected, Simulate(expected[0]));
            }
        }

        [Fact]
        public void RoundsHalfAwayFromZeroLikeLuau()
        {
            // 10 × 0.65 = 6.5 → Luau 7 (C# 기본 Math.Round면 6)
            Assert.Equal(7, RoundRules.QualifyCount(10, 4, 1));
            Assert.Equal(3, CoreMath.RoundHalfAway(2.5));
            Assert.Equal(-3, CoreMath.RoundHalfAway(-2.5));
        }

        [Fact]
        public void RoundCountBoundaries()
        {
            for (int n = 4; n <= 8; n++) Assert.Equal(3, RoundRules.RoundCount(n));
            for (int n = 9; n <= 40; n++) Assert.Equal(4, RoundRules.RoundCount(n));
        }

        [Fact]
        public void FinalAlwaysOneWinner()
        {
            Assert.Equal(1, RoundRules.QualifyCount(5, 3, 3));
            Assert.Equal(1, RoundRules.QualifyCount(2, 4, 4));
        }

        [Fact]
        public void SkipToFinalAndRoundToPlay()
        {
            Assert.True(RoundRules.ShouldSkipToFinal(2));
            Assert.False(RoundRules.ShouldSkipToFinal(3));
            Assert.Equal(4, RoundRules.RoundToPlay(2, 4, 2));
            Assert.Equal(2, RoundRules.RoundToPlay(2, 4, 3));
            Assert.Null(RoundRules.NextRound(4, 4, 1));
        }

        [Fact]
        public void EliminationPlaceFirstOutIsLast()
        {
            Assert.Equal(8, RoundRules.EliminationPlace(8, 1));
            Assert.Equal(1, RoundRules.EliminationPlace(8, 8));
        }

        [Fact]
        public void UnknownRoundThrows()
        {
            Assert.Throws<System.ArgumentException>(() => RoundRules.QualifyCount(10, 5, 1));
        }
    }
}
