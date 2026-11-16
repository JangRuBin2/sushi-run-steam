// 캐릭터 이동 규칙 (순수). Runtime/SushiCharacter가 매 FixedUpdate 부르고, 결과(원하는 수평 속도·점프·다이브)를
// Rigidbody에 적용해요. 물리(중력·충돌)는 Unity가, "언제 무엇을 할 수 있나"는 여기서.
//
// 단위는 미터. 로블록스판 수치(스터드)는 1 stud ≈ 0.28 m로 옮긴 뒤 조작감 시제품에서 다시 맞춰요(1단계 스펙).
using System;

namespace SushiRun.Core.Movement
{
    public enum MotorMode
    {
        Normal,
        Diving, // 다이브 중 (앞으로 날아감, 조작 약하게)
        Prone, // 다이브 착지 뒤 엎드림 (잠깐 못 움직임)
        Down, // 맞아서 넘어짐 (@_@)
        GettingUp,
    }

    [Serializable] // Unity 인스펙터에서 바로 고칠 수 있게 (System 특성이라 엔진 참조 없음)
    public sealed class MoveTuning
    {
        public float RunSpeed = 6.2f;
        public float GroundAccel = 48f;
        public float GroundDecel = 60f;
        public float AirAccel = 16f;
        public float AirDecel = 4f;
        public float TurnSpeedDeg = 900f;
        public float JumpSpeed = 7.4f;
        public float CoyoteTime = 0.10f; // 발판에서 떨어진 직후에도 점프 가능
        public float JumpBuffer = 0.12f; // 착지 직전 누른 점프를 기억
        public float DiveForwardSpeed = 9.5f; // 로블록스 40 studs/s ≈ 11.2 m/s → 몸이 작아 조금 낮춤
        public float DiveUpSpeed = 3.8f;
        public float DiveAirMaxUp = 4.5f; // 공중 다이브는 위쪽 속도를 이만큼까지만 (점프-다이브 연타 방지)
        public float DiveCooldown = 1.5f;
        public float DiveMaxFlight = 1.0f;
        public float DiveSteer = 3f; // 다이브 중 방향 조작 (m/s²)
        public float ProneTime = 0.5f;
        public float DownTime = 1.0f;
        public float GetUpTime = 0.3f;

        public static MoveTuning Default => new MoveTuning();
    }

    public struct MoveInput
    {
        public float MoveX; // 카메라 기준을 이미 월드로 바꾼 방향 (길이 0~1)
        public float MoveZ;
        public bool JumpPressed; // 이번 틱에 눌림
        public bool DivePressed;
    }

    public struct MotorContext
    {
        public float Time;
        public float Dt;
        public bool Grounded;
        public float VelX;
        public float VelY;
        public float VelZ;
        public float FacingDeg; // 지금 바라보는 방향 (Y축 회전, 0 = +Z)
    }

    public struct MoveCommand
    {
        public float VelX; // 적용할 수평 속도
        public float VelZ;
        public float? SetVelY; // 값이 있으면 세로 속도를 이걸로 (점프·다이브)
        public float FacingDeg;
        public bool Jumped;
        public bool Dived;
        public bool Landed; // 이번 틱에 땅에 닿음 (연출용)
        public float LandImpactSpeed;
        public bool GotUp;
    }

    public sealed class MotorState
    {
        public MotorMode Mode = MotorMode.Normal;
        public float ModeUntil;
        public float LastGroundedTime = float.NegativeInfinity;
        public float JumpPressedTime = float.NegativeInfinity;
        public float DiveReadyAt;
        public bool WasGrounded = true;
        public float LastAirVelY;
    }

    public static class MoveLogic
    {
        /// <summary>맞아서 넘어짐 (서버 판정 결과를 받아 부름). 다이브·점프 중이어도 바로 넘어져요.</summary>
        public static void KnockDown(MotorState state, float now, MoveTuning tuning)
        {
            state.Mode = MotorMode.Down;
            state.ModeUntil = now + tuning.DownTime;
        }

        public static bool CanControl(MotorState state) =>
            state.Mode == MotorMode.Normal || state.Mode == MotorMode.Diving;

        public static MoveCommand Step(MotorState state, MoveInput input, MotorContext ctx, MoveTuning tuning = null)
        {
            tuning ??= MoveTuning.Default;
            var cmd = new MoveCommand { VelX = ctx.VelX, VelZ = ctx.VelZ, FacingDeg = ctx.FacingDeg };
            float now = ctx.Time;
            float dt = Math.Max(0f, ctx.Dt);

            // 착지 감지 (연출·다이브 종료용)
            if (ctx.Grounded && !state.WasGrounded)
            {
                cmd.Landed = true;
                cmd.LandImpactSpeed = Math.Max(0f, -state.LastAirVelY);
                if (state.Mode == MotorMode.Diving)
                {
                    state.Mode = MotorMode.Prone;
                    state.ModeUntil = now + tuning.ProneTime;
                }
            }
            if (!ctx.Grounded)
            {
                state.LastAirVelY = ctx.VelY;
            }
            if (ctx.Grounded)
            {
                state.LastGroundedTime = now;
            }
            state.WasGrounded = ctx.Grounded;

            if (input.JumpPressed)
            {
                state.JumpPressedTime = now;
            }

            // 시간이 지난 상태 넘기기
            if (state.Mode == MotorMode.Diving && now >= state.ModeUntil)
            {
                state.Mode = MotorMode.Prone;
                state.ModeUntil = now + tuning.ProneTime;
            }
            if ((state.Mode == MotorMode.Prone || state.Mode == MotorMode.Down) && now >= state.ModeUntil && ctx.Grounded)
            {
                state.Mode = MotorMode.GettingUp;
                state.ModeUntil = now + tuning.GetUpTime;
                cmd.GotUp = true;
            }
            if (state.Mode == MotorMode.GettingUp && now >= state.ModeUntil)
            {
                state.Mode = MotorMode.Normal;
            }

            float inX = input.MoveX;
            float inZ = input.MoveZ;
            float inLen = (float)Math.Sqrt(inX * inX + inZ * inZ);
            if (inLen > 1f)
            {
                inX /= inLen;
                inZ /= inLen;
                inLen = 1f;
            }

            switch (state.Mode)
            {
                case MotorMode.Normal:
                {
                    float targetX = inX * tuning.RunSpeed;
                    float targetZ = inZ * tuning.RunSpeed;
                    bool accelerating = inLen > 0.01f;
                    float rate = ctx.Grounded
                        ? (accelerating ? tuning.GroundAccel : tuning.GroundDecel)
                        : (accelerating ? tuning.AirAccel : tuning.AirDecel);
                    ApproachFlat(ref cmd.VelX, ref cmd.VelZ, targetX, targetZ, rate * dt);
                    if (accelerating)
                    {
                        float wanted = (float)(Math.Atan2(inX, inZ) * 180.0 / Math.PI);
                        cmd.FacingDeg = TurnTowards(ctx.FacingDeg, wanted, tuning.TurnSpeedDeg * dt);
                    }

                    bool canJump = now - state.LastGroundedTime <= tuning.CoyoteTime;
                    bool jumpQueued = now - state.JumpPressedTime <= tuning.JumpBuffer;
                    if (canJump && jumpQueued)
                    {
                        cmd.SetVelY = tuning.JumpSpeed;
                        cmd.Jumped = true;
                        state.JumpPressedTime = float.NegativeInfinity;
                        state.LastGroundedTime = float.NegativeInfinity; // 코요테 한 번만
                    }
                    else if (input.DivePressed && now >= state.DiveReadyAt)
                    {
                        float rad = cmd.FacingDeg * (float)Math.PI / 180f;
                        float fx = (float)Math.Sin(rad);
                        float fz = (float)Math.Cos(rad);
                        cmd.VelX = fx * tuning.DiveForwardSpeed;
                        cmd.VelZ = fz * tuning.DiveForwardSpeed;
                        cmd.SetVelY = ctx.Grounded
                            ? tuning.DiveUpSpeed
                            : Math.Min(Math.Max(ctx.VelY, 0f) + tuning.DiveUpSpeed * 0.5f, tuning.DiveAirMaxUp);
                        cmd.Dived = true;
                        state.Mode = MotorMode.Diving;
                        state.ModeUntil = now + tuning.DiveMaxFlight;
                        state.DiveReadyAt = now + tuning.DiveCooldown;
                    }
                    break;
                }
                case MotorMode.Diving:
                    // 날아가는 중에는 살짝만 방향을 틀 수 있어요
                    cmd.VelX += inX * tuning.DiveSteer * dt;
                    cmd.VelZ += inZ * tuning.DiveSteer * dt;
                    break;
                default:
                    // 엎드림·넘어짐·일어나는 중: 조작 없음, 땅에서는 미끄러지며 멈춰요
                    if (ctx.Grounded)
                    {
                        ApproachFlat(ref cmd.VelX, ref cmd.VelZ, 0f, 0f, tuning.GroundDecel * 0.5f * dt);
                    }
                    break;
            }
            return cmd;
        }

        private static void ApproachFlat(ref float vx, ref float vz, float tx, float tz, float maxDelta)
        {
            float dx = tx - vx;
            float dz = tz - vz;
            float len = (float)Math.Sqrt(dx * dx + dz * dz);
            if (len <= maxDelta || len < 1e-6f)
            {
                vx = tx;
                vz = tz;
                return;
            }
            vx += dx / len * maxDelta;
            vz += dz / len * maxDelta;
        }

        public static float TurnTowards(float currentDeg, float targetDeg, float maxDeltaDeg)
        {
            float delta = DeltaAngle(currentDeg, targetDeg);
            if (Math.Abs(delta) <= maxDeltaDeg)
            {
                return targetDeg;
            }
            return currentDeg + Math.Sign(delta) * maxDeltaDeg;
        }

        public static float DeltaAngle(float from, float to)
        {
            float d = (to - from) % 360f;
            if (d > 180f)
            {
                d -= 360f;
            }
            if (d < -180f)
            {
                d += 360f;
            }
            return d;
        }
    }
}
