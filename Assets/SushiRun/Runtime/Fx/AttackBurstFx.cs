// 공격 연출 (코드 FX): 반투명 원판이 퍼지며 사라지고, 하얀 "밥알"이 사방으로 튀어요. 끝나면 스스로 지워요.
using UnityEngine;

namespace SushiRun.Runtime
{
    public sealed class AttackBurstFx : MonoBehaviour
    {
        private const int GrainCount = 10;
        private const float RingTime = 0.35f;
        private const float Lifetime = 0.7f;

        private Transform _ring;
        private Material _ringMat;
        private Transform[] _grains;
        private Vector3[] _grainVel;
        private float _radius;
        private float _age;
        private Color _ringColor = new Color(1f, 0.97f, 0.9f, 0.55f);

        public static AttackBurstFx Spawn(Vector3 position, float radius)
        {
            var go = new GameObject("AttackBurstFx");
            go.transform.position = position;
            var fx = go.AddComponent<AttackBurstFx>();
            fx.Init(radius);
            return fx;
        }

        private void Init(float radius)
        {
            _radius = Mathf.Max(0.5f, radius);
            _ringMat = ProtoMaterials.Transparent(_ringColor);
            _ring = ProtoMaterials.VisualPart(transform, "Ring", PrimitiveType.Cylinder, Vector3.zero,
                new Vector3(0.4f, 0.02f, 0.4f), Quaternion.identity, _ringMat, false).transform;

            Material grain = ProtoMaterials.Opaque(new Color(1f, 0.995f, 0.96f), 0.5f);
            _grains = new Transform[GrainCount];
            _grainVel = new Vector3[GrainCount];
            for (int i = 0; i < GrainCount; i++)
            {
                float ang = (i / (float)GrainCount) * 360f + Random.Range(-12f, 12f);
                Vector3 dir = Quaternion.Euler(0f, ang, 0f) * Vector3.forward;
                _grainVel[i] = dir * Random.Range(4.5f, 7f) + Vector3.up * Random.Range(2.5f, 4.5f);
                _grains[i] = ProtoMaterials.VisualPart(transform, "Grain", PrimitiveType.Sphere, Vector3.up * 0.1f,
                    new Vector3(0.12f, 0.08f, 0.08f), Quaternion.Euler(0f, ang, 0f), grain, false).transform;
            }
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            _age += dt;

            // 원판: 빨리 퍼지고(ease-out) 투명해짐. 지름 = 공격 반지름 × 2
            float t = Mathf.Clamp01(_age / RingTime);
            float ease = 1f - (1f - t) * (1f - t);
            float d = Mathf.Lerp(0.4f, _radius * 2f, ease);
            if (_ring != null)
            {
                _ring.localScale = new Vector3(d, 0.02f, d);
            }
            Color c = _ringColor;
            c.a = _ringColor.a * (1f - t);
            ProtoMaterials.SetColor(_ringMat, c);

            // 밥알: 간단한 포물선 + 끝에서 작아짐
            float shrink = Mathf.Clamp01(1f - (_age - Lifetime * 0.6f) / (Lifetime * 0.4f));
            for (int i = 0; i < _grains.Length; i++)
            {
                _grainVel[i] += Physics.gravity * (1.6f * dt);
                Transform g = _grains[i];
                g.localPosition += _grainVel[i] * dt;
                g.Rotate(400f * dt, 0f, 250f * dt, Space.Self);
                g.localScale = new Vector3(0.12f, 0.08f, 0.08f) * shrink;
            }

            if (_age >= Lifetime)
            {
                Destroy(gameObject);
            }
        }

        private void OnDestroy()
        {
            if (_ringMat != null)
            {
                Destroy(_ringMat); // 이 연출 전용으로 만든 재질
            }
        }
    }
}
