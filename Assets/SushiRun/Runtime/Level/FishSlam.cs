// 거대 생선 낙하: 바닥에 길쭉한 그림자 경고 → 큰 생선이 철퍼덕 떨어져 그 자리 캐릭터를 옆으로 튕겨 내고
// → 잠깐 누워서 길을 막다가 가라앉아요. 판정은 여기서 직접 (권위 쪽).
using System.Collections.Generic;
using UnityEngine;

namespace SushiRun.Runtime
{
    [DisallowMultipleComponent]
    public sealed class FishSlam : MonoBehaviour
    {
        public Vector3 Center;

        [Tooltip("생선 가운데가 떨어질 수 있는 범위 (가운데에서 x, z 반폭)")]
        public Vector2 AreaHalf = new Vector2(4f, 4f);

        public float Interval = 8f;
        public float Warning = 1.6f;
        public float FishLength = 11f;
        public float FishWidth = 2.6f;
        public float KnockHorizontal = 9f;
        public float KnockUp = 5f;
        public float Stun = 1f;
        public float LieSeconds = 2.5f;
        public float FallHeight = 20f;

        public Material FishMat;
        public Material BellyMat;
        public Material WarnMat;

        private GameObject _warn;
        private GameObject _fish;
        private Vector3 _ground;
        private Quaternion _rot;
        private float _impactAt = -1f;
        private float _lieUntil = -1f;
        private float _next;

        private void Start()
        {
            ResetCycle();
        }

        public void ResetCycle()
        {
            Clear();
            _next = Time.time + 4f;
        }

        private void Update()
        {
            float now = Time.time;
            if (_fish == null && now >= _next)
            {
                _next = now + Interval;
                Spawn(now);
            }
            if (_fish == null)
            {
                return;
            }
            if (_impactAt > 0f)
            {
                float k = Mathf.Clamp01(1f - (_impactAt - now) / Warning);
                _fish.transform.position = _ground + Vector3.up * (1.1f + FallHeight * (1f - k * k));
                _fish.transform.rotation = _rot * Quaternion.Euler(0f, 0f, Mathf.Sin(now * 12f) * 25f * (1f - k)); // 파닥파닥
                if (now >= _impactAt)
                {
                    Impact();
                }
            }
            else if (now >= _lieUntil)
            {
                _fish.transform.position += Vector3.down * (4f * Time.deltaTime); // 가라앉아요
                if (now >= _lieUntil + 1f)
                {
                    Clear();
                }
            }
            else
            {
                float flop = Mathf.Abs(Mathf.Sin(now * 9f)) * 0.25f;
                _fish.transform.position = _ground + Vector3.up * (1.1f + flop);
            }
        }

        private void Spawn(float now)
        {
            Vector3 p = Center + new Vector3(Random.Range(-AreaHalf.x, AreaHalf.x), 0f, Random.Range(-AreaHalf.y, AreaHalf.y));
            RaycastHit hit;
            _ground = Physics.Raycast(p + Vector3.up * 6f, Vector3.down, out hit, 12f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) ? hit.point : p;
            _rot = Quaternion.Euler(0f, Random.Range(0f, 180f), 0f);
            _warn = ProtoMaterials.VisualPart(transform, "FishShadow", PrimitiveType.Sphere, Vector3.zero, new Vector3(FishWidth, 0.04f, FishLength), _rot, WarnMat, false);
            _warn.transform.position = _ground + Vector3.up * 0.03f;
            _fish = BuildFish();
            _fish.transform.rotation = _rot;
            _fish.transform.position = _ground + Vector3.up * FallHeight;
            _impactAt = now + Warning;
        }

        /// <summary>참치 모양 (몸통 + 배 + 꼬리 + 눈). 보이기 전용 — 누워 있을 때만 상자 콜라이더.</summary>
        private GameObject BuildFish()
        {
            var go = new GameObject("GiantFish");
            go.transform.SetParent(transform, false);
            Transform t = go.transform;
            ProtoMaterials.VisualPart(t, "Body", PrimitiveType.Sphere, Vector3.zero, new Vector3(FishWidth, 2.2f, FishLength * 0.82f), Quaternion.identity, FishMat);
            ProtoMaterials.VisualPart(t, "Belly", PrimitiveType.Sphere, new Vector3(0f, -0.35f, 0.2f), new Vector3(FishWidth * 0.92f, 1.5f, FishLength * 0.7f), Quaternion.identity, BellyMat != null ? BellyMat : FishMat);
            for (int s = -1; s <= 1; s += 2)
            {
                ProtoMaterials.VisualPart(t, "Tail", PrimitiveType.Cube, new Vector3(0f, s * 0.7f, -FishLength * 0.46f), new Vector3(0.2f, 1.6f, 1.6f), Quaternion.Euler(s * 40f, 0f, 0f), FishMat);
                ProtoMaterials.VisualPart(t, "Fin", PrimitiveType.Cube, new Vector3(s * FishWidth * 0.45f, -0.2f, FishLength * 0.12f), new Vector3(0.12f, 0.6f, 1.2f), Quaternion.Euler(0f, s * 25f, s * 30f), FishMat);
                ProtoMaterials.VisualPart(t, "Eye", PrimitiveType.Sphere, new Vector3(s * FishWidth * 0.38f, 0.35f, FishLength * 0.32f), Vector3.one * 0.55f, Quaternion.identity, ProtoMaterials.Opaque(Color.white, 0.9f));
                ProtoMaterials.VisualPart(t, "Pupil", PrimitiveType.Sphere, new Vector3(s * FishWidth * 0.45f, 0.38f, FishLength * 0.34f), Vector3.one * 0.3f, Quaternion.identity, ProtoMaterials.Opaque(new Color(0.08f, 0.08f, 0.1f), 0.95f));
            }
            return go;
        }

        private void Impact()
        {
            _impactAt = -1f;
            _lieUntil = Time.time + LieSeconds;
            if (_warn != null)
            {
                Destroy(_warn);
                _warn = null;
            }
            _fish.transform.rotation = _rot;
            _fish.transform.position = _ground + Vector3.up * 1.1f;
            var box = _fish.AddComponent<BoxCollider>(); // 누워 있는 동안 길을 막아요
            box.size = new Vector3(FishWidth * 0.9f, 2f, FishLength * 0.85f);

            Quaternion inv = Quaternion.Inverse(_rot);
            List<SushiCharacter> all = SushiCharacter.All;
            for (int i = 0; i < all.Count; i++)
            {
                SushiCharacter ch = all[i];
                if (ch == null || ch.IsDespawned || ch.IsDown)
                {
                    continue;
                }
                Vector3 v = ch.transform.position - _ground;
                if (v.y < -1f || v.y > 3f)
                {
                    continue;
                }
                Vector3 local = inv * new Vector3(v.x, 0f, v.z);
                if (Mathf.Abs(local.x) > FishWidth * 0.5f + 0.6f || Mathf.Abs(local.z) > FishLength * 0.5f + 0.3f)
                {
                    continue;
                }
                // 생선 몸통 옆으로 튕겨 나가요
                Vector3 side = _rot * new Vector3(local.x >= 0f ? 1f : -1f, 0f, 0f);
                ch.Knock(side * KnockHorizontal + Vector3.up * KnockUp, Stun);
            }
        }

        private void Clear()
        {
            if (_warn != null)
            {
                Destroy(_warn);
            }
            if (_fish != null)
            {
                Destroy(_fish);
            }
            _warn = null;
            _fish = null;
            _impactAt = -1f;
            _lieUntil = -1f;
        }

        private void OnDestroy()
        {
            Clear();
        }
    }
}
