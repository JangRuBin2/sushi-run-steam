// "쫀득쫀득" 캐릭터 모션 — 몸 스쿼시·스트레치, 기울기, 토핑 출렁임 (순수 계산, 그리기는 Runtime/JellyBody).
// 판정과 무관한 화면 연출이에요: 히트박스는 그대로이고 보이는 몸만 늘었다 줄었다 해요.
//
// 원리
//   - 몸 세로 배율(squash)은 스프링: 목표 = 1(가만히) + 숨쉬기 + 걸음 통통 + 공중 늘어남.
//     착지·점프·다이브·맞음은 스프링에 "속도"를 넣어서 → 눌렸다가 목표를 지나쳐 늘어나고 몇 번 출렁이다 멈춰요 (damping < 1).
//   - 부피 보존: 세로 s면 가로·앞뒤는 1/√s (눌리면 옆으로 퍼짐 — 말랑한 느낌의 핵심).
//   - 기울기: 가속도 반대쪽으로 상체가 뒤처졌다 따라오는 스프링 (출발할 때 뒤로, 멈출 때 앞으로 쏠림).
//   - 토핑: 몸보다 느슨한 스프링으로 가속도 반대로 밀렸다 돌아와요 (생선이 출렁, 우니가 흔들).
using System;

namespace SushiRun.Core.Motion
{
    [Serializable] // Unity 인스펙터에서 바로 고칠 수 있게 (System 특성이라 엔진 참조 없음)
    public sealed class JellyTuning
    {
        // 몸 스쿼시·스트레치
        public float SquashFrequency = 3.2f;
        public float SquashDamping = 0.30f; // 낮을수록 더 출렁 (0.2~0.45 권장)
        public float MinScaleY = 0.62f;
        public float MaxScaleY = 1.30f;
        public float AirStretchPerSpeed = 0.022f; // 세로 속도 1 m/s당 늘어나는 양 (공중)
        public float MaxAirStretch = 0.18f;
        public float LandKickPerSpeed = 0.8f; // 착지 속도 1 m/s당 눌림 스프링에 넣는 속도
        public float MaxLandKick = 9f;
        public float MinLandSpeed = 1.2f; // 이보다 살살 내려앉으면 착지 출렁임 없음
        public float JumpKick = 3.2f; // 점프 순간 위로 늘어나는 속도
        public float DiveKick = 2.6f;
        public float HitKick = 5.0f;
        public float GetUpKick = 2.4f;

        // 숨쉬기·걸음
        public float BreathAmplitude = 0.018f;
        public float BreathPeriod = 2.4f;
        public float WalkSquashAmplitude = 0.06f; // 달릴 때 발걸음마다 통통
        public float StepsPerMeter = 1.6f;
        public float WalkSpeedForFullBounce = 5.0f;

        // 기울기 (도)
        public float LeanFrequency = 2.6f;
        public float LeanDamping = 0.42f;
        public float LeanPerAccel = 1.6f; // 가속도 1 m/s²당 기울기
        public float MaxLean = 16f;

        // 토핑 출렁임 (m, 몸 위 기준)
        public float ToppingFrequency = 4.2f;
        public float ToppingDamping = 0.22f;
        public float ToppingLagPerAccel = 0.010f;
        public float MaxToppingLag = 0.12f;
        public float ToppingLandKick = 0.08f; // 착지 속도 1 m/s당 토핑이 아래로 눌리는 속도

        public static JellyTuning Default => new JellyTuning();
    }

    /// <summary>한 프레임 입력. LocalAcceleration은 캐릭터 기준 (X 오른쪽, Z 앞), m/s².</summary>
    public struct JellyInput
    {
        public float Dt;
        public float LocalAccelX;
        public float LocalAccelZ;
        public float VerticalVelocity;
        public float HorizontalSpeed;
        public bool Grounded;
    }

    /// <summary>그릴 자세. Scale은 몸 배율, Lean은 도(앞으로 숙임 +, 오른쪽 기울기 +), 토핑은 몸 기준 오프셋(m).</summary>
    public struct JellyPose
    {
        public float ScaleX;
        public float ScaleY;
        public float ScaleZ;
        public float LeanPitch;
        public float LeanRoll;
        public float ToppingX;
        public float ToppingY;
        public float ToppingZ;
        public float ToppingScaleY;

        public static JellyPose Rest => new JellyPose { ScaleX = 1f, ScaleY = 1f, ScaleZ = 1f, ToppingScaleY = 1f };
    }

    public sealed class JellyMotion
    {
        private readonly JellyTuning _tuning;
        private Spring1D _squash = new Spring1D(1f);
        private Spring1D _pitch;
        private Spring1D _roll;
        private Spring1D _toppingX;
        private Spring1D _toppingY;
        private Spring1D _toppingZ;
        private float _time;
        private float _walkPhase;

        public JellyMotion(JellyTuning tuning = null)
        {
            _tuning = tuning ?? JellyTuning.Default;
        }

        public JellyTuning Tuning => _tuning;
        public JellyPose Pose { get; private set; } = JellyPose.Rest;

        /// <summary>착지. impactSpeed = 닿기 직전 아래로 떨어지던 속도(양수, m/s).</summary>
        public void Land(float impactSpeed)
        {
            float speed = Math.Abs(impactSpeed);
            if (speed < _tuning.MinLandSpeed)
            {
                return;
            }
            float kick = Math.Min(speed * _tuning.LandKickPerSpeed, _tuning.MaxLandKick);
            _squash.Kick(-kick);
            _toppingY.Kick(-speed * _tuning.ToppingLandKick);
        }

        public void Jump() => _squash.Kick(_tuning.JumpKick);

        public void Dive() => _squash.Kick(_tuning.DiveKick);

        /// <summary>맞음·넘어짐. strength 0~1.</summary>
        public void Hit(float strength = 1f)
        {
            float s = CoreMath.Clamp01(strength);
            _squash.Kick(-_tuning.HitKick * s);
            _toppingY.Kick(0.6f * s);
        }

        public void GetUp() => _squash.Kick(_tuning.GetUpKick);

        public JellyPose Step(JellyInput input)
        {
            float dt = Math.Max(0f, input.Dt);
            var t = _tuning;
            _time += dt;

            // 목표 세로 배율: 1 + 숨쉬기 + 걸음 통통 + 공중 늘어남
            float target = 1f;
            if (t.BreathPeriod > 0f)
            {
                target += t.BreathAmplitude * (float)Math.Sin(_time * 2.0 * Math.PI / t.BreathPeriod);
            }
            if (input.Grounded && input.HorizontalSpeed > 0.2f)
            {
                _walkPhase += input.HorizontalSpeed * t.StepsPerMeter * dt;
                float speedFactor = CoreMath.Clamp01(input.HorizontalSpeed / Math.Max(t.WalkSpeedForFullBounce, 0.01f));
                // 한 걸음에 한 번 눌렸다 펴짐 (|sin|이라 항상 아래로 — 통통 튀는 느낌)
                target -= t.WalkSquashAmplitude * speedFactor * (float)Math.Abs(Math.Sin(_walkPhase * Math.PI));
            }
            if (!input.Grounded)
            {
                target += Math.Min(Math.Abs(input.VerticalVelocity) * t.AirStretchPerSpeed, t.MaxAirStretch);
            }

            _squash.Step(target, t.SquashFrequency, t.SquashDamping, dt);
            float scaleY = CoreMath.Clamp(_squash.Value, t.MinScaleY, t.MaxScaleY);
            if (scaleY != _squash.Value)
            {
                _squash.Value = scaleY; // 한계에 닿으면 거기서 되튕김
                _squash.Velocity = -_squash.Velocity * 0.35f;
            }
            float side = 1f / (float)Math.Sqrt(scaleY);

            // 기울기: 가속도 방향으로 숙임 (출발할 때 앞으로 숙이며 달려 나가고, 급정거하면 뒤로 젖혀졌다 앞으로 출렁)
            float pitchTarget = CoreMath.Clamp(input.LocalAccelZ * t.LeanPerAccel, -t.MaxLean, t.MaxLean);
            float rollTarget = CoreMath.Clamp(input.LocalAccelX * t.LeanPerAccel, -t.MaxLean, t.MaxLean);
            var leanCoef = SpringCoefficients.For(t.LeanFrequency, t.LeanDamping, dt);
            _pitch.Apply(pitchTarget, leanCoef);
            _roll.Apply(rollTarget, leanCoef);

            // 토핑: 가속도 반대쪽으로 뒤처짐
            float lagX = CoreMath.Clamp(-input.LocalAccelX * t.ToppingLagPerAccel, -t.MaxToppingLag, t.MaxToppingLag);
            float lagZ = CoreMath.Clamp(-input.LocalAccelZ * t.ToppingLagPerAccel, -t.MaxToppingLag, t.MaxToppingLag);
            var toppingCoef = SpringCoefficients.For(t.ToppingFrequency, t.ToppingDamping, dt);
            _toppingX.Apply(lagX, toppingCoef);
            _toppingZ.Apply(lagZ, toppingCoef);
            _toppingY.Apply(0f, toppingCoef);
            _toppingY.Value = CoreMath.Clamp(_toppingY.Value, -t.MaxToppingLag, t.MaxToppingLag);

            var pose = new JellyPose
            {
                ScaleX = side,
                ScaleY = scaleY,
                ScaleZ = side,
                LeanPitch = CoreMath.Clamp(_pitch.Value, -t.MaxLean * 1.5f, t.MaxLean * 1.5f),
                LeanRoll = CoreMath.Clamp(_roll.Value, -t.MaxLean * 1.5f, t.MaxLean * 1.5f),
                ToppingX = _toppingX.Value,
                ToppingY = _toppingY.Value,
                ToppingZ = _toppingZ.Value,
                // 토핑은 몸이 눌릴 때 조금 덜 눌리고(무게), 늘어날 때 조금 덜 늘어나요
                ToppingScaleY = 1f + (scaleY - 1f) * 0.5f,
            };
            Pose = pose;
            return pose;
        }
    }
}
