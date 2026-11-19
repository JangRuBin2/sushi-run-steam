// 회전 막대(가운데 축을 지나는 선)가 어떤 점을 언제 쓸고 지나가는지 — 봇이 "보고 판단"할 때 쓰는 계산.
// 각도는 위에서 본 도(°), 막대 각도에서 점까지 잰 값. 막대는 양쪽으로 뻗은 선이라 180°마다 같은 모양이에요.
using System;

namespace SushiRun.Core.Movement
{
    public static class SweepTiming
    {
        /// <summary>반지름 radius인 점에서 막대가 닿는 각도 폭의 절반 (도). halfWidth = 막대 반두께 + 몸 반지름.
        /// 축에 너무 가까우면 늘 닿아요 (90).</summary>
        public static float HalfAngleDeg(float radius, float halfWidth)
        {
            if (radius <= halfWidth)
            {
                return 90f;
            }
            return (float)(Math.Atan(halfWidth / radius) * 180.0 / Math.PI);
        }

        /// <summary>막대가 점에 닿는 가장 가까운 구간 [start, end] (지금부터 초). 지금 닿아 있으면 start = 0.
        /// pointDeg = 막대에서 점까지 각도 (막대가 도는 방향이 +면 omega > 0). 안 돌면 start = 무한대.</summary>
        public static void NextContact(float pointDeg, float omegaDeg, float radius, float halfWidth, out float start, out float end)
        {
            float half = HalfAngleDeg(radius, halfWidth);
            if (half >= 90f)
            {
                start = 0f;
                end = float.PositiveInfinity;
                return;
            }
            float w = Math.Abs(omegaDeg);
            float diff = WrapLine(pointDeg);
            if (w < 1e-3f)
            {
                start = Math.Abs(diff) <= half ? 0f : float.PositiveInfinity;
                end = float.PositiveInfinity;
                return;
            }
            float ahead = omegaDeg > 0f ? diff : -diff; // 막대가 점까지 더 돌아야 하는 각도
            if (Math.Abs(diff) <= half)
            {
                start = 0f;
                end = (ahead + half) / w;
                return;
            }
            if (ahead < 0f)
            {
                ahead += 180f;
            }
            start = (ahead - half) / w;
            end = (ahead + half) / w;
        }

        /// <summary>t초 뒤에 막대가 점에 닿아 있나요.</summary>
        public static bool InContactAt(float pointDeg, float omegaDeg, float radius, float halfWidth, float t)
        {
            float half = HalfAngleDeg(radius, halfWidth);
            if (half >= 90f)
            {
                return true;
            }
            return Math.Abs(WrapLine(pointDeg - omegaDeg * t)) <= half;
        }

        /// <summary>점프한 뒤 발이 clearHeight보다 높이 있는 구간 [rise, fall] (초). 못 넘으면 rise = fall = 무한대.</summary>
        public static void JumpClearWindow(float jumpSpeed, float gravity, float clearHeight, out float rise, out float fall)
        {
            if (clearHeight <= 0f)
            {
                rise = 0f;
                fall = gravity > 0f ? 2f * jumpSpeed / gravity : float.PositiveInfinity;
                return;
            }
            // h(t) = v t - g t² / 2 = c
            float disc = jumpSpeed * jumpSpeed - 2f * gravity * clearHeight;
            if (gravity <= 0f || disc < 0f)
            {
                rise = float.PositiveInfinity;
                fall = float.PositiveInfinity;
                return;
            }
            float s = (float)Math.Sqrt(disc);
            rise = (jumpSpeed - s) / gravity;
            fall = (jumpSpeed + s) / gravity;
        }

        /// <summary>선은 180°마다 같아요 → (-90, 90]로.</summary>
        public static float WrapLine(float deg)
        {
            float a = deg % 180f;
            if (a > 90f)
            {
                a -= 180f;
            }
            else if (a <= -90f)
            {
                a += 180f;
            }
            return a;
        }
    }
}
