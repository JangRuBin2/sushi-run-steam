using System.Collections.Generic;
using SushiRun.Core.Combat;
using Xunit;

namespace SushiRun.Core.Tests
{
    public class AttackLogicTests
    {
        private static AttackCandidate C(int id, float x, float z = 0, bool alive = true, bool down = false, bool immune = false) =>
            new AttackCandidate { Id = id, Position = new Vec3(x, 0, z), Alive = alive, Down = down, Immune = immune };

        [Fact]
        public void PicksOnlyValidTargetsInRadiusNearestFirst()
        {
            var cfg = AttackConfig.Default;
            var list = new List<AttackCandidate>
            {
                C(1, 0), // 자기 자신
                C(2, 2.0f),
                C(3, 1.0f),
                C(4, 2.9f), // 반지름 밖
                C(5, 0.5f, alive: false),
                C(6, 0.5f, down: true),
                C(7, 0.5f, immune: true),
                C(9, -1.0f), // 3번과 같은 거리 → id 순
            };
            var ids = AttackLogic.PickTargets(1, Vec3.Zero, list, cfg);
            Assert.Equal(new List<int> { 3, 9, 2 }, ids);
        }

        [Fact]
        public void KnockPointsAwayAndUp()
        {
            var cfg = AttackConfig.Default;
            var v = AttackLogic.KnockFor(Vec3.Zero, new Vec3(0, 0, 1), new Vec3(3, 5, 0), cfg);
            Assert.Equal(cfg.KnockHorizontal, v.X, 3);
            Assert.Equal(cfg.KnockUp, v.Y, 3);
            Assert.Equal(0f, v.Z, 3);
            var same = AttackLogic.KnockFor(Vec3.Zero, new Vec3(0, 0, 1), Vec3.Zero, cfg);
            Assert.Equal(cfg.KnockHorizontal, same.Z, 3); // 같은 자리 → 보는 방향
            var none = AttackLogic.KnockFor(Vec3.Zero, Vec3.Zero, Vec3.Zero, cfg);
            Assert.Equal(-cfg.KnockHorizontal, none.Z, 3);
        }

        [Fact]
        public void CooldownAndImmunity()
        {
            var cfg = AttackConfig.Default;
            var book = new AttackBook();
            Assert.True(book.Record(1, new List<int> { 2 }, 10f, cfg));
            Assert.False(book.Record(1, new List<int>(), 17f, cfg));
            Assert.True(book.CanAttack(1, 18f));
            Assert.True(book.IsImmune(2, 11.9f));
            Assert.False(book.IsImmune(2, 12f));
            Assert.Equal(18f, book.ReadyAt(1));
        }
    }
}
