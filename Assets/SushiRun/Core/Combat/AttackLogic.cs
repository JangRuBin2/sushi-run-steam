// 모두의 공격 — 로블록스판 src/shared/AttackLogic.luau의 C# 이식 (같은 규칙, 단위만 미터).
// 공격은 모든 캐릭터가 같아요. 스킨은 연출만 바꿔요 (판정에 스킨 id 금지).
using System;
using System.Collections.Generic;

namespace SushiRun.Core.Combat
{
    [Serializable] // Unity 인스펙터에서 바로 고칠 수 있게 (System 특성이라 엔진 참조 없음)
    public sealed class AttackConfig
    {
        public float Radius = 2.8f; // 10 studs
        public float KnockHorizontal = 8.4f; // 30 studs/s
        public float KnockUp = 3.9f; // 14 studs/s
        public float Stun = 1.0f;
        public float Cooldown = 8f;
        public float Immunity = 2f;

        public static readonly AttackConfig Default = new AttackConfig();
    }

    public struct AttackCandidate
    {
        public int Id;
        public Vec3 Position;
        public bool Alive;
        public bool Down;
        public bool Immune;
    }

    public sealed class AttackBook
    {
        private readonly Dictionary<int, float> _readyAt = new Dictionary<int, float>();
        private readonly Dictionary<int, float> _immuneUntil = new Dictionary<int, float>();

        public bool CanAttack(int id, float now) => !_readyAt.TryGetValue(id, out float t) || now >= t;

        public bool IsImmune(int id, float now) => _immuneUntil.TryGetValue(id, out float t) && now < t;

        public float? ReadyAt(int id) => _readyAt.TryGetValue(id, out float t) ? t : (float?)null;

        /// <summary>공격 기록 (쿨다운 시작 + 맞은 사람 면역). 쿨다운 중이면 false, 아무것도 안 바꿔요.</summary>
        public bool Record(int attackerId, IReadOnlyList<int> hits, float now, AttackConfig config)
        {
            if (!CanAttack(attackerId, now))
            {
                return false;
            }
            _readyAt[attackerId] = now + config.Cooldown;
            foreach (int id in hits)
            {
                _immuneUntil[id] = now + config.Immunity;
            }
            return true;
        }

        public void Forget(int id)
        {
            _readyAt.Remove(id);
            _immuneUntil.Remove(id);
        }
    }

    public static class AttackLogic
    {
        /// <summary>반지름 안의 맞을 수 있는 대상, 가까운 순 (거리가 같으면 id 순).</summary>
        public static List<int> PickTargets(int attackerId, Vec3 attackerPosition, IEnumerable<AttackCandidate> candidates, AttackConfig config)
        {
            var hits = new List<(int id, float d)>();
            foreach (var c in candidates)
            {
                if (c.Id == attackerId || !c.Alive || c.Down || c.Immune)
                {
                    continue;
                }
                float d = Vec3.Distance(attackerPosition, c.Position);
                if (d <= config.Radius)
                {
                    hits.Add((c.Id, d));
                }
            }
            hits.Sort((a, b) => a.d == b.d ? a.id.CompareTo(b.id) : a.d.CompareTo(b.d));
            var ids = new List<int>(hits.Count);
            foreach (var h in hits)
            {
                ids.Add(h.id);
            }
            return ids;
        }

        /// <summary>바깥쪽(수평)으로 튕기는 속도. 같은 자리면 공격자가 보는 방향, 그것도 없으면 -Z.</summary>
        public static Vec3 KnockFor(Vec3 attackerPosition, Vec3 attackerLook, Vec3 targetPosition, AttackConfig config)
        {
            float dx = targetPosition.X - attackerPosition.X;
            float dz = targetPosition.Z - attackerPosition.Z;
            float len = (float)Math.Sqrt(dx * dx + dz * dz);
            if (len < 1e-6f)
            {
                dx = attackerLook.X;
                dz = attackerLook.Z;
                len = (float)Math.Sqrt(dx * dx + dz * dz);
                if (len < 1e-6f)
                {
                    dx = 0f;
                    dz = -1f;
                    len = 1f;
                }
            }
            return new Vec3(dx / len * config.KnockHorizontal, config.KnockUp, dz / len * config.KnockHorizontal);
        }
    }
}
