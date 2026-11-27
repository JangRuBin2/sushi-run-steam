// 밥물 끓어오름: 원 안 여러 곳에 거품 경고 → 잠시 뒤 펑 하고 솟아 위에 선 캐릭터를 튀겨 올려요.
// 판정은 여기서 직접 (권위 쪽). 보이는 것은 이 오브젝트 아래에 만들고 지워요.
using System.Collections.Generic;
using UnityEngine;

namespace SushiRun.Runtime
{
    [DisallowMultipleComponent]
    public sealed class EruptionField : MonoBehaviour
    {
        [Tooltip("끓어오를 원의 가운데 (월드, 바닥 높이)")]
        public Vector3 Center;

        [Tooltip("이 반지름 안에서 골라요 (m)")]
        public float AreaRadius = 7f;

        [Tooltip("한 번에 끓어오르는 곳 수")]
        public int Count = 3;

        [Tooltip("끓어오름 사이 간격 (초)")]
        public float Interval = 7f;

        [Tooltip("거품이 보인 뒤 솟기까지 (초)")]
        public float Warning = 1.6f;

        [Tooltip("이 안의 캐릭터를 튀겨 올려요 (m)")]
        public float BlastRadius = 2.3f;

        public float KnockHorizontal = 5f;
        public float KnockUp = 10f;
        public float Stun = 0.8f;

        public Material WarnMat;
        public Material BlastMat;

        /// <summary>다음 끓어오름까지 남은 초 (화면 글씨용).</summary>
        public float NextIn => Mathf.Max(0f, _next - Time.time);

        private sealed class Spot
        {
            public Vector3 Pos;
            public float BlastAt;
            public float EndAt;
            public GameObject Warn;
            public GameObject Column;
            public bool Blasted;
        }

        private readonly List<Spot> _spots = new List<Spot>();
        private float _next;

        private void Start()
        {
            _next = Time.time + 3f;
        }

        /// <summary>판을 다시 시작할 때 (지금 끓는 것 지우고 처음 간격부터).</summary>
        public void ResetCycle()
        {
            Clear();
            _next = Time.time + 3f;
        }

        private void Update()
        {
            float now = Time.time;
            if (now >= _next)
            {
                _next = now + Interval;
                for (int i = 0; i < Count; i++)
                {
                    Spawn(now);
                }
            }
            for (int i = _spots.Count - 1; i >= 0; i--)
            {
                Spot s = _spots[i];
                if (!s.Blasted)
                {
                    if (s.Warn != null)
                    {
                        // 보글보글: 점점 빨리 들썩
                        float k = 1f - (s.BlastAt - now) / Warning;
                        float bob = Mathf.Abs(Mathf.Sin(now * (8f + 20f * k))) * 0.15f * (0.4f + k);
                        s.Warn.transform.localScale = new Vector3(BlastRadius * 2f, 0.25f + bob, BlastRadius * 2f);
                    }
                    if (now >= s.BlastAt)
                    {
                        Blast(s);
                    }
                }
                else
                {
                    if (s.Column != null)
                    {
                        float k = Mathf.Clamp01((s.EndAt - now) / 0.5f);
                        s.Column.transform.localScale = new Vector3(BlastRadius * 1.3f * k, 4f * k, BlastRadius * 1.3f * k);
                    }
                    if (now >= s.EndAt)
                    {
                        if (s.Column != null)
                        {
                            Destroy(s.Column);
                        }
                        _spots.RemoveAt(i);
                    }
                }
            }
        }

        private void Spawn(float now)
        {
            Vector2 r = Random.insideUnitCircle * AreaRadius;
            var s = new Spot { Pos = Center + new Vector3(r.x, 0f, r.y), BlastAt = now + Warning };
            s.Warn = ProtoMaterials.VisualPart(transform, "BoilWarning", PrimitiveType.Sphere, Vector3.zero, Vector3.one, Quaternion.identity, WarnMat, false);
            s.Warn.transform.position = s.Pos;
            _spots.Add(s);
        }

        private void Blast(Spot s)
        {
            s.Blasted = true;
            s.EndAt = Time.time + 0.5f;
            if (s.Warn != null)
            {
                Destroy(s.Warn);
                s.Warn = null;
            }
            s.Column = ProtoMaterials.VisualPart(transform, "BoilColumn", PrimitiveType.Sphere, Vector3.zero, Vector3.one, Quaternion.identity, BlastMat, false);
            s.Column.transform.position = s.Pos + Vector3.up * 1.2f;
            List<SushiCharacter> all = SushiCharacter.All;
            for (int i = 0; i < all.Count; i++)
            {
                SushiCharacter ch = all[i];
                if (ch == null || ch.IsDespawned || ch.IsDown)
                {
                    continue;
                }
                Vector3 v = ch.transform.position - s.Pos;
                if (v.y < -0.5f || v.y > 2f)
                {
                    continue;
                }
                v.y = 0f;
                if (v.magnitude > BlastRadius)
                {
                    continue;
                }
                Vector3 dir = v.sqrMagnitude > 0.01f ? v.normalized : Vector3.forward;
                ch.Knock(dir * KnockHorizontal + Vector3.up * KnockUp, Stun);
            }
        }

        private void Clear()
        {
            for (int i = 0; i < _spots.Count; i++)
            {
                if (_spots[i].Warn != null)
                {
                    Destroy(_spots[i].Warn);
                }
                if (_spots[i].Column != null)
                {
                    Destroy(_spots[i].Column);
                }
            }
            _spots.Clear();
        }

        private void OnDestroy()
        {
            Clear();
        }
    }
}
