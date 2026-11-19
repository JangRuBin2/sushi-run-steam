using SushiRun.Core.Movement;
using Xunit;

namespace SushiRun.Core.Tests
{
    public class SweepTimingTests
    {
        [Fact]
        public void WrapLineKeepsMinusNinetyToNinety()
        {
            Assert.Equal(10f, SweepTiming.WrapLine(190f), 3);
            Assert.Equal(-80f, SweepTiming.WrapLine(100f), 3);
            Assert.Equal(90f, SweepTiming.WrapLine(-90f), 3);
            Assert.Equal(0f, SweepTiming.WrapLine(180f), 3);
        }

        [Fact]
        public void NextContactAheadOfBar()
        {
            // 점이 막대보다 30° 앞, 60°/s, 반지름 3 m, 폭 0.6 m → 반각 ≈ 11.3°
            SweepTiming.NextContact(30f, 60f, 3f, 0.6f, out float start, out float end);
            float half = SweepTiming.HalfAngleDeg(3f, 0.6f);
            Assert.Equal((30f - half) / 60f, start, 3);
            Assert.Equal((30f + half) / 60f, end, 3);
        }

        [Fact]
        public void NextContactJustPassedWaitsForOtherEnd()
        {
            // 막대가 막 지나감 (점이 20° 뒤) → 반대쪽 끝이 160° 돌아서 와요
            SweepTiming.NextContact(-20f, 60f, 3f, 0.6f, out float start, out _);
            float half = SweepTiming.HalfAngleDeg(3f, 0.6f);
            Assert.Equal((160f - half) / 60f, start, 3);
        }

        [Fact]
        public void NegativeOmegaMirrors()
        {
            SweepTiming.NextContact(-30f, -60f, 3f, 0.6f, out float a, out _);
            SweepTiming.NextContact(30f, 60f, 3f, 0.6f, out float b, out _);
            Assert.Equal(b, a, 4);
        }

        [Fact]
        public void TouchingNowAndNearAxis()
        {
            SweepTiming.NextContact(2f, 60f, 3f, 0.6f, out float s, out float e);
            Assert.Equal(0f, s);
            Assert.True(e > 0f);

            SweepTiming.NextContact(45f, 60f, 0.4f, 0.6f, out s, out e);
            Assert.Equal(0f, s);
            Assert.True(float.IsPositiveInfinity(e));
        }

        [Fact]
        public void InContactAtMatchesNextContact()
        {
            SweepTiming.NextContact(50f, 88f, 2.5f, 0.67f, out float s, out float e);
            Assert.False(SweepTiming.InContactAt(50f, 88f, 2.5f, 0.67f, s - 0.02f));
            Assert.True(SweepTiming.InContactAt(50f, 88f, 2.5f, 0.67f, (s + e) * 0.5f));
            Assert.False(SweepTiming.InContactAt(50f, 88f, 2.5f, 0.67f, e + 0.02f));
        }

        [Fact]
        public void JumpClearWindowMatchesBallistics()
        {
            // 점프 7.4 m/s, 중력 9.81 × 2.2, 막대 위 0.66 m
            SweepTiming.JumpClearWindow(7.4f, 9.81f * 2.2f, 0.66f, out float rise, out float fall);
            float g = 9.81f * 2.2f;
            Assert.Equal(0.66f, 7.4f * rise - 0.5f * g * rise * rise, 3);
            Assert.Equal(0.66f, 7.4f * fall - 0.5f * g * fall * fall, 3);
            Assert.True(rise > 0.08f && rise < 0.13f);

            SweepTiming.JumpClearWindow(2f, g, 5f, out rise, out _);
            Assert.True(float.IsPositiveInfinity(rise));
        }
    }
}
