using System;
using SushiRun.Core.Motion;
using Xunit;

namespace SushiRun.Core.Tests
{
    public class JellyMotionTests
    {
        private static JellyTuning Quiet()
        {
            var t = new JellyTuning();
            t.BreathAmplitude = 0f; // 숨쉬기 없이 정확히 1로 돌아오는지 보려고
            return t;
        }

        private static JellyInput Idle(float dt) => new JellyInput { Dt = dt, Grounded = true };

        private static JellyPose Run(JellyMotion m, float seconds, float dt, Func<float, JellyInput> input = null)
        {
            JellyPose pose = m.Pose;
            int steps = (int)Math.Round(seconds / dt);
            for (int i = 0; i < steps; i++)
            {
                pose = m.Step(input != null ? input(i * dt) : Idle(dt));
            }
            return pose;
        }

        [Fact]
        public void VolumeIsPreserved()
        {
            var m = new JellyMotion();
            m.Land(9f);
            for (int i = 0; i < 240; i++)
            {
                var p = m.Step(new JellyInput { Dt = 1f / 120f, Grounded = i % 50 > 10, VerticalVelocity = -3f, HorizontalSpeed = 5f, LocalAccelZ = 8f });
                Assert.InRange(p.ScaleX * p.ScaleY * p.ScaleZ, 0.999f, 1.001f);
                Assert.Equal(p.ScaleX, p.ScaleZ);
            }
        }

        [Fact]
        public void SettlesToRest()
        {
            var m = new JellyMotion(Quiet());
            m.Land(8f);
            m.Hit();
            var p = Run(m, 4f, 1f / 60f);
            Assert.InRange(p.ScaleY, 0.995f, 1.005f);
            Assert.InRange(p.LeanPitch, -0.01f, 0.01f);
            Assert.InRange(p.ToppingY, -0.001f, 0.001f);
        }

        [Fact]
        public void LandingSquashesThenOvershoots()
        {
            // "쫀득": 착지하면 눌렸다가 1을 지나쳐 늘어났다가 멈춰요
            var m = new JellyMotion(Quiet());
            m.Land(8f);
            float min = 1f, maxAfterMin = 0f;
            bool reachedMin = false;
            for (int i = 0; i < 120; i++)
            {
                float y = m.Step(Idle(1f / 120f)).ScaleY;
                if (!reachedMin && y < min) min = y;
                if (min < 0.9f && y > min + 0.001f) reachedMin = true;
                if (reachedMin) maxAfterMin = Math.Max(maxAfterMin, y);
            }
            Assert.True(min < 0.85f, $"squash min {min}");
            Assert.True(maxAfterMin > 1.03f, $"overshoot {maxAfterMin}");
        }

        [Fact]
        public void SoftLandingDoesNothing()
        {
            var m = new JellyMotion(Quiet());
            m.Land(0.5f);
            var p = m.Step(Idle(1f / 60f));
            Assert.InRange(p.ScaleY, 0.999f, 1.001f);
        }

        [Fact]
        public void HugeImpactStaysWithinLimits()
        {
            var t = Quiet();
            var m = new JellyMotion(t);
            m.Land(500f);
            m.Hit(5f);
            for (int i = 0; i < 300; i++)
            {
                var p = m.Step(Idle(1f / 60f));
                Assert.InRange(p.ScaleY, t.MinScaleY, t.MaxScaleY);
            }
        }

        [Fact]
        public void FallingStretches()
        {
            var m = new JellyMotion(Quiet());
            var p = Run(m, 0.6f, 1f / 60f, _ => new JellyInput { Dt = 1f / 60f, Grounded = false, VerticalVelocity = -8f });
            Assert.True(p.ScaleY > 1.08f, $"stretch {p.ScaleY}");
        }

        [Fact]
        public void LeansIntoAccelerationAndToppingLags()
        {
            var m = new JellyMotion(Quiet());
            var p = Run(m, 0.25f, 1f / 60f, _ => new JellyInput { Dt = 1f / 60f, Grounded = true, LocalAccelZ = 10f, HorizontalSpeed = 2f });
            Assert.True(p.LeanPitch > 3f, $"pitch {p.LeanPitch}");
            Assert.True(p.ToppingZ < -0.02f, $"topping {p.ToppingZ}");
        }

        [Fact]
        public void FrameRateIndependent()
        {
            var a = new JellyMotion(Quiet());
            var b = new JellyMotion(Quiet());
            a.Land(7f);
            b.Land(7f);
            float ya = Run(a, 0.3f, 1f / 30f).ScaleY;
            float yb = Run(b, 0.3f, 1f / 240f).ScaleY;
            Assert.InRange(ya - yb, -0.002f, 0.002f);
        }

        [Fact]
        public void SpringRegimesAllSettle()
        {
            foreach (float damping in new[] { 0.2f, 1f, 2f })
            {
                var s = new Spring1D(0f);
                s.Kick(5f);
                for (int i = 0; i < 600; i++) s.Step(1f, 2f, damping, 1f / 60f);
                Assert.InRange(s.Value, 0.999f, 1.001f);
            }
            // 큰 dt에서도 터지지 않음
            var big = new Spring1D(0f);
            big.Step(1f, 10f, 0.3f, 0.5f);
            Assert.False(float.IsNaN(big.Value));
            Assert.InRange(big.Value, -2f, 3f);
        }
    }
}
