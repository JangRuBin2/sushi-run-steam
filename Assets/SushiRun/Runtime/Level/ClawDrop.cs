// 인형뽑기 집게: 위에서 누군가(또는 아무 데)를 골라 그 위로 가고(바닥에 빨간 원 경고) → 쾅 내려찍어 둘레를 튕겨 내고
// 바닥 전체를 흔들어(가볍게 들썩) → 다시 올라가요. 판정은 여기서 직접 (권위 쪽). 집게 모양은 Claw 자식으로 받아요.
using System.Collections.Generic;
using UnityEngine;

namespace SushiRun.Runtime
{
    [DisallowMultipleComponent]
    public sealed class ClawDrop : MonoBehaviour
    {
        public Vector3 Center;
        public float AreaRadius = 8f;
        public float Interval = 12f;
        public float MoveSeconds = 1.6f;
        public float DropSeconds = 0.35f;
        public float RiseSeconds = 1.2f;
        public float Height = 9f;
        public float Radius = 2.5f;
        public float KnockHorizontal = 8f;
        public float KnockUp = 7f;
        public float Stun = 1f;

        [Tooltip("내려찍을 때 이 반지름 안 모두가 가볍게 들썩 (바닥 흔들림)")]
        public float ShakeRadius = 12f;

        public Transform Claw;
        public Material WarnMat;

        private enum Phase { Idle, Move, Drop, Rise }

        private Phase _phase = Phase.Idle;
        private float _phaseAt;
        private float _next;
        private Vector3 _from;
        private Vector3 _target;
        private GameObject _warn;

        private Vector3 Home => Center + Vector3.up * Height;

        private void Start()
        {
            ResetCycle();
        }

        public void ResetCycle()
        {
            _phase = Phase.Idle;
            _next = Time.time + 5f;
            if (_warn != null)
            {
                Destroy(_warn);
                _warn = null;
            }
            if (Claw != null)
            {
                Claw.position = Home;
            }
        }

        private void Update()
        {
            if (Claw == null)
            {
                return;
            }
            float now = Time.time;
            float t = now - _phaseAt;
            switch (_phase)
            {
                case Phase.Idle:
                    Claw.position = Home + new Vector3(Mathf.Sin(now * 0.7f) * 1.5f, Mathf.Sin(now * 2f) * 0.2f, Mathf.Cos(now * 0.5f) * 1.5f);
                    if (now >= _next)
                    {
                        _next = now + Interval;
                        _from = Claw.position;
                        _target = PickTarget();
                        _warn = ProtoMaterials.VisualPart(transform, "ClawWarning", PrimitiveType.Cylinder, Vector3.zero, new Vector3(Radius * 2f, 0.01f, Radius * 2f), Quaternion.identity, WarnMat, false);
                        _warn.transform.position = _target + Vector3.up * 0.03f;
                        Begin(Phase.Move, now);
                    }
                    break;
                case Phase.Move:
                {
                    float k = Mathf.SmoothStep(0f, 1f, t / MoveSeconds);
                    Claw.position = Vector3.Lerp(_from, new Vector3(_target.x, Home.y, _target.z), k);
                    if (t >= MoveSeconds)
                    {
                        Begin(Phase.Drop, now);
                    }
                    break;
                }
                case Phase.Drop:
                {
                    float k = Mathf.Clamp01(t / DropSeconds);
                    Claw.position = new Vector3(_target.x, Mathf.Lerp(Home.y, _target.y + 1.2f, k * k), _target.z);
                    if (t >= DropSeconds)
                    {
                        Slam();
                        Begin(Phase.Rise, now);
                    }
                    break;
                }
                case Phase.Rise:
                {
                    float k = Mathf.Clamp01(t / RiseSeconds);
                    Claw.position = new Vector3(_target.x, Mathf.Lerp(_target.y + 1.2f, Home.y, k), _target.z);
                    if (t >= RiseSeconds)
                    {
                        _phase = Phase.Idle;
                    }
                    break;
                }
            }
        }

        private void Begin(Phase p, float now)
        {
            _phase = p;
            _phaseAt = now;
        }

        /// <summary>구역 안에 서 있는 캐릭터 하나 (없으면 아무 데). 고른 순간 자리 — 움직이면 피할 수 있어요.</summary>
        private Vector3 PickTarget()
        {
            var cands = new List<Vector3>();
            List<SushiCharacter> all = SushiCharacter.All;
            for (int i = 0; i < all.Count; i++)
            {
                SushiCharacter ch = all[i];
                if (ch == null || ch.IsDespawned || !ch.IsGrounded)
                {
                    continue;
                }
                Vector3 v = ch.transform.position - Center;
                v.y = 0f;
                if (v.magnitude <= AreaRadius)
                {
                    cands.Add(ch.transform.position);
                }
            }
            if (cands.Count > 0)
            {
                return cands[Random.Range(0, cands.Count)];
            }
            Vector2 r = Random.insideUnitCircle * AreaRadius;
            return Center + new Vector3(r.x, 0f, r.y);
        }

        private void Slam()
        {
            if (_warn != null)
            {
                Destroy(_warn);
                _warn = null;
            }
            List<SushiCharacter> all = SushiCharacter.All;
            for (int i = 0; i < all.Count; i++)
            {
                SushiCharacter ch = all[i];
                if (ch == null || ch.IsDespawned || ch.IsDown)
                {
                    continue;
                }
                Vector3 v = ch.transform.position - _target;
                if (v.y < -1.5f || v.y > 3f)
                {
                    continue;
                }
                v.y = 0f;
                float d = v.magnitude;
                Vector3 dir = d > 0.1f ? v / d : Vector3.forward;
                if (d <= Radius)
                {
                    ch.Knock(dir * KnockHorizontal + Vector3.up * KnockUp, Stun);
                }
                else if (d <= ShakeRadius && ch.IsGrounded)
                {
                    ch.Knock(dir * 1.5f + Vector3.up * 3f, 0.15f); // 바닥 흔들림: 살짝 들썩
                }
            }
        }

        private void OnDestroy()
        {
            if (_warn != null)
            {
                Destroy(_warn);
            }
        }
    }
}
