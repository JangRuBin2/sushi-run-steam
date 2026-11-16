// 순수 로직 공용 수학. UnityEngine을 참조하지 않아요 (dotnet test로 클라우드에서 검증).
// Luau(로블록스판)와 같은 답을 내야 하는 곳은 여기 함수를 써요.
using System;

namespace SushiRun.Core
{
    public static class CoreMath
    {
        /// <summary>Luau math.round와 같은 반올림 (.5는 0에서 먼 쪽). C# Math.Round 기본값은 은행가 반올림이라 다르게 나와요.</summary>
        public static int RoundHalfAway(double value)
        {
            return (int)Math.Round(value, MidpointRounding.AwayFromZero);
        }

        public static int Clamp(int value, int min, int max)
        {
            return value < min ? min : (value > max ? max : value);
        }

        public static float Clamp(float value, float min, float max)
        {
            return value < min ? min : (value > max ? max : value);
        }

        public static float Clamp01(float value)
        {
            return Clamp(value, 0f, 1f);
        }

        /// <summary>current를 target 쪽으로 maxDelta만큼 옮겨요 (넘지 않게).</summary>
        public static float MoveTowards(float current, float target, float maxDelta)
        {
            float diff = target - current;
            if (Math.Abs(diff) <= maxDelta)
            {
                return target;
            }
            return current + Math.Sign(diff) * maxDelta;
        }
    }

    /// <summary>엔진과 무관한 2D/3D 벡터 (순수 로직 전용). Runtime에서 UnityEngine.Vector3로 바꿔 써요.</summary>
    public struct Vec3 : IEquatable<Vec3>
    {
        public float X;
        public float Y;
        public float Z;

        public Vec3(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public static readonly Vec3 Zero = new Vec3(0f, 0f, 0f);

        public float Length => (float)Math.Sqrt(X * X + Y * Y + Z * Z);
        public float FlatLength => (float)Math.Sqrt(X * X + Z * Z);

        public static Vec3 operator +(Vec3 a, Vec3 b) => new Vec3(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        public static Vec3 operator -(Vec3 a, Vec3 b) => new Vec3(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        public static Vec3 operator *(Vec3 a, float s) => new Vec3(a.X * s, a.Y * s, a.Z * s);

        public static float Distance(Vec3 a, Vec3 b) => (b - a).Length;

        public bool Equals(Vec3 other) => X == other.X && Y == other.Y && Z == other.Z;
        public override bool Equals(object obj) => obj is Vec3 other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(X, Y, Z);
        public override string ToString() => $"({X:0.###}, {Y:0.###}, {Z:0.###})";
    }
}
