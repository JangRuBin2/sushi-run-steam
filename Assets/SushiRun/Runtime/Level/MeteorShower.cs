// 떨어지는 운석: 구역 안 아무 데나 빨간 경고 원 → 운석이 떨어져 둘레 캐릭터를 튕겨 내고 → 잠깐 바위로 남아 길을 막아요.
// 판정은 여기서 직접 (권위 쪽). 보이는 것·바위는 이 오브젝트 아래에 만들고 지워요.
using System.Collections.Generic;
using UnityEngine;

namespace SushiRun.Runtime
{
    [DisallowMultipleComponent]
    public sealed class MeteorShower : MonoBehaviour
    {
        [Tooltip("떨어질 구역 가운데 (월드)")]
        public Vector3 AreaCenter;

        [Tooltip("구역 크기 (x 폭, z 길이)")]
        public Vector2 AreaSize = new Vector2(7f, 26f);

        [Tooltip("운석 사이 평균 간격 (초)")]
        public float Interval = 2.2f;

        [Tooltip("경고 원이 뜬 뒤 떨어지기까지 (초)")]
        public float Warning = 1.4f;

        [Tooltip("이 안의 캐릭터를 튕겨 내요 (m)")]
        public float Radius = 2.2f;

        public float KnockHorizontal = 8f;
        public float KnockUp = 5f;
        public float Stun = 1f;

        [Tooltip("떨어진 뒤 바위로 남는 시간 (초)")]
        public float RockSeconds = 5f;

        public float FallHeight = 30f;

        public Material MeteorMat;
        public Material RockMat;
        public Material WarnMat;

        private sealed class Drop
        {
            public Vector3 Ground;
            public float ImpactAt;
            public GameObject Warn;
            public GameObject Meteor;
            public GameObject Rock;
            public float RockUntil;
        }

        private readonly List<Drop> _drops = new List<Drop>();
        private float _next;

        private void Start()
        {
            _next = Time.time + 2f;
        }

        private void Update()
        {
            float now = Time.time;
            if (now >= _next)
            {
                _next = now + Interval * Random.Range(0.7f, 1.3f);
                TrySpawn(now);
            }
            for (int i = _drops.Count - 1; i >= 0; i--)
            {
                Drop d = _drops[i];
                if (d.Meteor != null)
                {
                    float k = Mathf.Clamp01(1f - (d.ImpactAt - now) / Mathf.Max(0.05f, Warning));
                    d.Meteor.transform.position = d.Ground + Vector3.up * (0.9f + FallHeight * (1f - k * k)); // 점점 빨라지며 떨어짐
                    d.Meteor.transform.Rotate(90f * Time.deltaTime, 140f * Time.deltaTime, 0f, Space.Self);
                    if (d.Warn != null)
                    {
                        float pulse = 1f + 0.12f * Mathf.Sin(now * 18f);
                        d.Warn.transform.localScale = new Vector3(Radius * 2f * pulse, 0.01f, Radius * 2f * pulse);
                    }
                    if (now >= d.ImpactAt)
                    {
                        Impact(d, now);
                    }
                }
                else if (now >= d.RockUntil)
                {
                    if (d.Rock != null)
                    {
                        Destroy(d.Rock);
                    }
                    _drops.RemoveAt(i);
                }
                else if (d.Rock != null && d.RockUntil - now < 0.6f)
                {
                    d.Rock.transform.position += Vector3.down * (3f * Time.deltaTime); // 가라앉으며 사라짐
                }
            }
        }

        private void TrySpawn(float now)
        {
            Vector3 p = AreaCenter + new Vector3(Random.Range(-0.5f, 0.5f) * AreaSize.x, 0f, Random.Range(-0.5f, 0.5f) * AreaSize.y);
            RaycastHit hit;
            if (!Physics.Raycast(p + Vector3.up * 5f, Vector3.down, out hit, 12f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                return;
            }
            var d = new Drop { Ground = hit.point, ImpactAt = now + Warning };
            d.Warn = ProtoMaterials.VisualPart(transform, "MeteorWarning", PrimitiveType.Cylinder, Vector3.zero, Vector3.one, Quaternion.identity, WarnMat, false);
            d.Warn.transform.position = hit.point + Vector3.up * 0.02f;
            d.Meteor = ProtoMaterials.VisualPart(transform, "Meteor", PrimitiveType.Sphere, Vector3.zero, Vector3.one * 1.8f, Quaternion.identity, MeteorMat);
            d.Meteor.transform.position = hit.point + Vector3.up * FallHeight;
            _drops.Add(d);
        }

        private void Impact(Drop d, float now)
        {
            Destroy(d.Meteor);
            d.Meteor = null;
            if (d.Warn != null)
            {
                Destroy(d.Warn);
                d.Warn = null;
            }
            List<SushiCharacter> all = SushiCharacter.All;
            for (int i = 0; i < all.Count; i++)
            {
                SushiCharacter ch = all[i];
                if (ch == null || ch.IsDespawned || ch.IsDown)
                {
                    continue;
                }
                Vector3 v = ch.transform.position - d.Ground;
                if (Mathf.Abs(v.y) > 2.5f)
                {
                    continue;
                }
                v.y = 0f;
                if (v.magnitude > Radius)
                {
                    continue;
                }
                Vector3 dir = v.sqrMagnitude > 0.01f ? v.normalized : new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f)).normalized;
                ch.Knock(dir * KnockHorizontal + Vector3.up * KnockUp, Stun);
            }
            // 바위로 남아 길을 막아요
            d.Rock = ProtoMaterials.VisualPart(transform, "MeteorRock", PrimitiveType.Sphere, Vector3.zero, new Vector3(2.2f, 1.5f, 2.2f), Quaternion.Euler(0f, Random.value * 360f, 0f), RockMat);
            d.Rock.transform.position = d.Ground + Vector3.up * 0.45f;
            d.Rock.AddComponent<SphereCollider>();
            d.RockUntil = now + RockSeconds;
        }

        private void OnDestroy()
        {
            for (int i = 0; i < _drops.Count; i++)
            {
                Drop d = _drops[i];
                if (d.Meteor != null)
                {
                    Destroy(d.Meteor);
                }
                if (d.Warn != null)
                {
                    Destroy(d.Warn);
                }
                if (d.Rock != null)
                {
                    Destroy(d.Rock);
                }
            }
            _drops.Clear();
        }
    }
}
