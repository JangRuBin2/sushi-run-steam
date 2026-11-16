// 부딪히면 튕겨 넘어뜨리는 장애물 (휘두르는 젓가락·꼬치). 물리 충돌 메시지 대신 매 물리 틱 OverlapBox로
// 직접 찾아요 (키네마틱 회전체와 메시지 순서에 기대지 않게). 튕기는 방향 = 막대가 움직이는 방향.
using System.Collections.Generic;
using UnityEngine;

namespace SushiRun.Runtime
{
    [DisallowMultipleComponent]
    public sealed class KnockHazard : MonoBehaviour
    {
        [Tooltip("판정 상자 (보통 같은 오브젝트의 BoxCollider)")]
        public BoxCollider Box;

        public float KnockHorizontal = 9f;
        public float KnockUp = 4.5f;
        public float Stun = 1.0f;

        [Tooltip("같은 캐릭터를 다시 칠 때까지 (초)")]
        public float RepeatDelay = 0.8f;

        [Tooltip("판정 상자를 이만큼 키워서 봐요 (m)")]
        public float Margin = 0.06f;

        [Tooltip("낮은 막대 = 점프로 넘기 (봇 힌트). 아니면 높은 막대 = 땅에 붙어 있기")]
        public bool Low = true;

        /// <summary>켜진 장애물 목록 (봇이 피할 때 봐요).</summary>
        public static readonly List<KnockHazard> Active = new List<KnockHazard>();

        private static readonly Collider[] s_hits = new Collider[32];

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Active.Clear();
        }

        private readonly Dictionary<SushiCharacter, float> _lastHit = new Dictionary<SushiCharacter, float>();
        private IPlatformMotion _motion;

        /// <summary>이 막대를 돌리는 회전체 (없으면 null).</summary>
        public Rotator Rotator { get; private set; }

        /// <summary>막대 길이의 절반 (로컬 Z 방향, m).</summary>
        public float HalfLength => Box != null ? Box.size.z * Box.transform.lossyScale.z * 0.5f : 0f;

        private void Awake()
        {
            if (Box == null)
            {
                Box = GetComponent<BoxCollider>();
            }
            _motion = GetComponentInParent<IPlatformMotion>();
            Rotator = GetComponentInParent<Rotator>();
        }

        private void OnEnable()
        {
            if (!Active.Contains(this))
            {
                Active.Add(this);
            }
        }

        private void OnDisable()
        {
            Active.Remove(this);
        }

        private void FixedUpdate()
        {
            if (Box == null)
            {
                return;
            }
            Transform t = Box.transform;
            Vector3 center = t.TransformPoint(Box.center);
            Vector3 half = Vector3.Scale(Box.size, t.lossyScale) * 0.5f + Vector3.one * Margin;
            int n = Physics.OverlapBoxNonAlloc(center, half, s_hits, t.rotation, SushiCharacter.CharacterMask, QueryTriggerInteraction.Ignore);
            float now = Time.fixedTime;
            for (int i = 0; i < n; i++)
            {
                Collider c = s_hits[i];
                s_hits[i] = null;
                if (c == null)
                {
                    continue;
                }
                SushiCharacter ch = c.GetComponent<SushiCharacter>();
                if (ch == null || ch.IsDespawned || ch.IsDown)
                {
                    continue;
                }
                float last;
                if (_lastHit.TryGetValue(ch, out last) && now - last < RepeatDelay)
                {
                    continue;
                }
                _lastHit[ch] = now;

                Vector3 pos = ch.Center;
                Vector3 dir = _motion != null ? _motion.GetPointVelocity(pos) : Vector3.zero;
                dir.y = 0f;
                if (dir.sqrMagnitude < 0.01f)
                {
                    dir = pos - center;
                    dir.y = 0f;
                }
                if (dir.sqrMagnitude < 1e-6f)
                {
                    dir = -ch.transform.forward;
                }
                dir.Normalize();
                ch.Knock(dir * KnockHorizontal + Vector3.up * KnockUp, Stun);
            }
        }
    }
}
