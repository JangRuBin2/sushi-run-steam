// "모두의 공격" 로컬 시제품 — 규칙은 Core/Combat/AttackLogic (로블록스판과 같은 규칙).
// 공격은 모든 캐릭터가 같아요 (스킨·팔레트는 연출에도 안 씀).
using System.Collections.Generic;
using SushiRun.Core;
using SushiRun.Core.Combat;
using SushiRun.Core.Movement;
using UnityEngine;

namespace SushiRun.Runtime
{
    [DisallowMultipleComponent]
    public sealed class AttackAbility : MonoBehaviour
    {
        /// <summary>모든 캐릭터가 같이 쓰는 쿨다운·면역 기록 (지금은 한 컴퓨터 안 — 2단계에서 서버로).</summary>
        public static AttackBook Book = new AttackBook();

        private static readonly List<AttackCandidate> s_candidates = new List<AttackCandidate>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Book = new AttackBook();
            s_candidates.Clear();
        }

        public AttackConfig Config = new AttackConfig();

        /// <summary>공격함 (맞은 수). 연출·JellyBody용.</summary>
        public event System.Action<int> Attacked;

        private SushiCharacter _ch;

        private void Awake()
        {
            _ch = GetComponent<SushiCharacter>();
        }

        /// <summary>다시 공격할 수 있을 때까지 남은 초 (0이면 준비됨).</summary>
        public float CooldownRemaining
        {
            get
            {
                if (_ch == null)
                {
                    return 0f;
                }
                float? ready = Book.ReadyAt(_ch.Id);
                return ready.HasValue ? Mathf.Max(0f, ready.Value - Time.time) : 0f;
            }
        }

        public bool CanAttackNow =>
            _ch != null && !_ch.IsDespawned && _ch.Mode != MotorMode.Down && _ch.Mode != MotorMode.GettingUp
            && Book.CanAttack(_ch.Id, Time.time);

        private void FixedUpdate()
        {
            if (_ch == null || _ch.Input == null)
            {
                return;
            }
            if (_ch.Input.ConsumeAttack())
            {
                TryAttack();
            }
        }

        public bool TryAttack()
        {
            if (_ch == null || _ch.IsDespawned)
            {
                return false;
            }
            MotorMode mode = _ch.Mode;
            if (mode == MotorMode.Down || mode == MotorMode.GettingUp)
            {
                return false; // 넘어져 있으면 못 해요
            }
            float now = Time.time;
            int id = _ch.Id;
            if (!Book.CanAttack(id, now))
            {
                return false;
            }

            Vector3 center = _ch.Center;
            s_candidates.Clear();
            List<SushiCharacter> all = SushiCharacter.All;
            for (int i = 0; i < all.Count; i++)
            {
                SushiCharacter other = all[i];
                if (other == null)
                {
                    continue;
                }
                s_candidates.Add(new AttackCandidate
                {
                    Id = other.Id,
                    Position = ToVec(other.Center),
                    Alive = !other.IsDespawned,
                    Down = other.IsDown,
                    Immune = Book.IsImmune(other.Id, now),
                });
            }
            List<int> hits = AttackLogic.PickTargets(id, ToVec(center), s_candidates, Config);
            Book.Record(id, hits, now, Config);

            Vector3 look = _ch.transform.forward;
            for (int h = 0; h < hits.Count; h++)
            {
                SushiCharacter target = FindById(hits[h]);
                if (target == null)
                {
                    continue;
                }
                Vec3 knock = AttackLogic.KnockFor(ToVec(center), ToVec(look), ToVec(target.Center), Config);
                target.Knock(new Vector3(knock.X, knock.Y, knock.Z), Config.Stun);
            }

            AttackBurstFx.Spawn(_ch.transform.position + Vector3.up * 0.35f, Config.Radius);
            Attacked?.Invoke(hits.Count);
            return true;
        }

        private static SushiCharacter FindById(int id)
        {
            List<SushiCharacter> all = SushiCharacter.All;
            for (int i = 0; i < all.Count; i++)
            {
                if (all[i] != null && all[i].Id == id)
                {
                    return all[i];
                }
            }
            return null;
        }

        private static Vec3 ToVec(Vector3 v)
        {
            return new Vec3(v.x, v.y, v.z);
        }
    }
}
