// 감쇠 스프링 (해석해 — dt가 커도 터지지 않고, 프레임레이트가 달라도 같은 모양).
// 참고: Ryan Juckett, "Damped Springs" (2012). frequency(Hz)가 클수록 빨리 따라가고,
// damping < 1이면 목표를 지나쳤다 돌아오는 "쫀득한" 출렁임, 1이면 딱 멈춤, > 1이면 느리게 다가가요.
using System;

namespace SushiRun.Core.Motion
{
    public struct SpringCoefficients
    {
        public float PosPos;
        public float PosVel;
        public float VelPos;
        public float VelVel;

        public static SpringCoefficients For(float frequencyHz, float damping, float dt)
        {
            var c = new SpringCoefficients { PosPos = 1f, PosVel = 0f, VelPos = 0f, VelVel = 1f };
            if (dt <= 0f || frequencyHz <= 0f)
            {
                return c;
            }
            double omega = 2.0 * Math.PI * frequencyHz;
            double zeta = Math.Max(0.0, damping);
            double t = dt;
            const double Epsilon = 1e-4;

            if (zeta > 1.0 + Epsilon)
            {
                double za = -omega * zeta;
                double zb = omega * Math.Sqrt(zeta * zeta - 1.0);
                double z1 = za - zb;
                double z2 = za + zb;
                double e1 = Math.Exp(z1 * t);
                double e2 = Math.Exp(z2 * t);
                double invTwoZb = 1.0 / (2.0 * zb);
                double e1Over = e1 * invTwoZb;
                double e2Over = e2 * invTwoZb;
                double z1e1Over = z1 * e1Over;
                double z2e2Over = z2 * e2Over;
                c.PosPos = (float)(e1Over * z2 - z2e2Over + e2);
                c.PosVel = (float)(-e1Over + e2Over);
                c.VelPos = (float)((z1e1Over - z2e2Over + e2) * z2);
                c.VelVel = (float)(-z1e1Over + z2e2Over);
            }
            else if (zeta < 1.0 - Epsilon)
            {
                double omegaZeta = omega * zeta;
                double alpha = omega * Math.Sqrt(1.0 - zeta * zeta);
                double expTerm = Math.Exp(-omegaZeta * t);
                double cosTerm = Math.Cos(alpha * t);
                double sinTerm = Math.Sin(alpha * t);
                double invAlpha = 1.0 / alpha;
                double expSin = expTerm * sinTerm;
                double expCos = expTerm * cosTerm;
                double expOmegaZetaSinOverAlpha = expTerm * omegaZeta * sinTerm * invAlpha;
                c.PosPos = (float)(expCos + expOmegaZetaSinOverAlpha);
                c.PosVel = (float)(expSin * invAlpha);
                c.VelPos = (float)(-expSin * alpha - omegaZeta * expOmegaZetaSinOverAlpha);
                c.VelVel = (float)(expCos - expOmegaZetaSinOverAlpha);
            }
            else
            {
                double expTerm = Math.Exp(-omega * t);
                double timeExp = t * expTerm;
                double timeExpFreq = timeExp * omega;
                c.PosPos = (float)(timeExpFreq + expTerm);
                c.PosVel = (float)timeExp;
                c.VelPos = (float)(-omega * timeExpFreq);
                c.VelVel = (float)(-timeExpFreq + expTerm);
            }
            return c;
        }
    }

    /// <summary>값 하나를 목표로 끌어당기는 스프링. Kick으로 속도를 더하면 출렁여요.</summary>
    public struct Spring1D
    {
        public float Value;
        public float Velocity;

        public Spring1D(float value)
        {
            Value = value;
            Velocity = 0f;
        }

        public void Step(float target, float frequencyHz, float damping, float dt)
        {
            Apply(target, SpringCoefficients.For(frequencyHz, damping, dt));
        }

        public void Apply(float target, SpringCoefficients c)
        {
            float offset = Value - target;
            float newOffset = offset * c.PosPos + Velocity * c.PosVel;
            Velocity = offset * c.VelPos + Velocity * c.VelVel;
            Value = newOffset + target;
        }

        public void Kick(float velocity)
        {
            Velocity += velocity;
        }
    }
}
