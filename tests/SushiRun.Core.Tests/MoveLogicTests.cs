using SushiRun.Core.Movement;
using Xunit;

namespace SushiRun.Core.Tests
{
    public class MoveLogicTests
    {
        private const float Dt = 1f / 60f;

        private static MotorContext Ctx(float time, bool grounded, float vx = 0, float vy = 0, float vz = 0, float facing = 0) =>
            new MotorContext { Time = time, Dt = Dt, Grounded = grounded, VelX = vx, VelY = vy, VelZ = vz, FacingDeg = facing };

        [Fact]
        public void AcceleratesToRunSpeed()
        {
            var s = new MotorState();
            var t = MoveTuning.Default;
            float vz = 0;
            for (int i = 0; i < 60; i++)
            {
                vz = MoveLogic.Step(s, new MoveInput { MoveZ = 1 }, Ctx(i * Dt, true, vz: vz), t).VelZ;
            }
            Assert.InRange(vz, t.RunSpeed - 0.01f, t.RunSpeed + 0.01f);
        }

        [Fact]
        public void CoyoteJumpAllowedBrieflyAfterLeavingGround()
        {
            var s = new MotorState();
            MoveLogic.Step(s, default, Ctx(0f, true));
            var late = MoveLogic.Step(s, new MoveInput { JumpPressed = true }, Ctx(0.08f, false));
            Assert.True(late.Jumped);

            var s2 = new MotorState();
            MoveLogic.Step(s2, default, Ctx(0f, true));
            var tooLate = MoveLogic.Step(s2, new MoveInput { JumpPressed = true }, Ctx(0.2f, false));
            Assert.False(tooLate.Jumped);
        }

        [Fact]
        public void JumpBufferFiresOnLanding()
        {
            var s = new MotorState { WasGrounded = false };
            MoveLogic.Step(s, new MoveInput { JumpPressed = true }, Ctx(1.0f, false, vy: -3));
            var landed = MoveLogic.Step(s, default, Ctx(1.08f, true));
            Assert.True(landed.Landed);
            Assert.True(landed.Jumped);
        }

        [Fact]
        public void DiveThenProneThenGetUpWithCooldown()
        {
            var t = MoveTuning.Default;
            var s = new MotorState();
            var dive = MoveLogic.Step(s, new MoveInput { DivePressed = true }, Ctx(0f, true, facing: 90), t);
            Assert.True(dive.Dived);
            Assert.InRange(dive.VelX, t.DiveForwardSpeed - 0.01f, t.DiveForwardSpeed + 0.01f); // 90도 = +X
            Assert.Equal(MotorMode.Diving, s.Mode);

            MoveLogic.Step(s, default, Ctx(0.1f, false, vy: -2), t);
            var land = MoveLogic.Step(s, default, Ctx(0.4f, true), t);
            Assert.True(land.Landed);
            Assert.Equal(MotorMode.Prone, s.Mode);

            var getUp = MoveLogic.Step(s, default, Ctx(0.4f + t.ProneTime + 0.01f, true), t);
            Assert.True(getUp.GotUp);
            MoveLogic.Step(s, default, Ctx(0.4f + t.ProneTime + t.GetUpTime + 0.02f, true), t);
            Assert.Equal(MotorMode.Normal, s.Mode);

            var again = MoveLogic.Step(s, new MoveInput { DivePressed = true }, Ctx(1.2f, true), t);
            Assert.False(again.Dived); // 쿨다운 1.5초
            var ok = MoveLogic.Step(s, new MoveInput { DivePressed = true }, Ctx(1.6f, true), t);
            Assert.True(ok.Dived);
        }

        [Fact]
        public void KnockedDownIgnoresInputUntilRecovered()
        {
            var t = MoveTuning.Default;
            var s = new MotorState();
            MoveLogic.KnockDown(s, 0f, t);
            var cmd = MoveLogic.Step(s, new MoveInput { MoveZ = 1, JumpPressed = true }, Ctx(0.2f, true), t);
            Assert.False(cmd.Jumped);
            Assert.Equal(0f, cmd.VelZ);
            Assert.False(MoveLogic.CanControl(s));
            MoveLogic.Step(s, default, Ctx(t.DownTime + 0.01f, true), t);
            MoveLogic.Step(s, default, Ctx(t.DownTime + t.GetUpTime + 0.05f, true), t);
            Assert.True(MoveLogic.CanControl(s));
        }

        [Fact]
        public void AirDiveCapsUpwardSpeed()
        {
            var t = MoveTuning.Default;
            var s = new MotorState();
            var cmd = MoveLogic.Step(s, new MoveInput { DivePressed = true }, Ctx(0f, false, vy: 7f), t);
            Assert.True(cmd.Dived);
            Assert.True(cmd.SetVelY <= t.DiveAirMaxUp);
        }

        [Fact]
        public void TurnTakesShortestWay()
        {
            Assert.Equal(-170f, MoveLogic.DeltaAngle(170f, 0f), 3);
            Assert.Equal(20f, MoveLogic.DeltaAngle(350f, 10f), 3);
        }
    }
}
