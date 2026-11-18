// 시제품 봇 — ISushiInput을 구현해서 사람과 같은 컨트롤러로 달려요 (부딪히고 때릴 상대).
// Race: 맵이 준 길(BotPath)을 조금씩 벗어나며 따라가고, 길 끝·낭떠러지에서 점프.
// Final: 접시 위를 돌아다니며 가장자리를 피하고, 낮은 꼬치가 다가오면 점프, 높은 꼬치가 가까우면 점프 참기.
// 가끔 다이브·공격. 판정은 없어요 (입력만 만듦).
using System.Collections.Generic;
using UnityEngine;

namespace SushiRun.Runtime
{
    [DisallowMultipleComponent]
    public sealed class WanderBot : MonoBehaviour, ISushiInput
    {
        public PrototypeLevel Level;

        [Range(0f, 1f)]
        [Tooltip("실력: 낮은 막대를 제때 넘는 확률이자, 건너뛰기 전에 내릴 자리를 살피는 확률")]
        public float Skill = 0.75f;

        [Tooltip("공격 반지름 안에 누가 있을 때 초당 공격 확률")]
        public float AttackChancePerSecond = 0.2f;

        [Tooltip("평지에서 초당 다이브 확률")]
        public float DiveChancePerSecond = 0.05f;

        [Tooltip("점프로 건너갈 때, 내릴 자리에 낮은 막대가 (체공 시간 + 이만큼) 안에 오면 기다려요 (초)")]
        public float LandingClearMargin = 0.6f;

        private SushiCharacter _ch;
        private AttackAbility _attack;
        private Vector2 _move;
        private bool _jump;
        private bool _dive;
        private bool _attackPress;
        private int _wp;
        private Vector3 _offset;
        private float _speed = 1f;
        private Vector3 _wanderTarget;
        private float _nextWanderAt;
        private float _nextJumpAllowed;
        private float _suppressJumpUntil;
        private Vector3 _stuckCheckPos;
        private float _stuckCheckAt;
        private readonly Dictionary<KnockHazard, float> _hazardDecided = new Dictionary<KnockHazard, float>();
        private int _carefulForWp = -1; // 이 길 점에서 살필지 정한 결과 (점마다 한 번만 정해요)
        private bool _careful;

        public Vector2 MoveWorld => _move;

        public bool ConsumeJump()
        {
            bool v = _jump;
            _jump = false;
            return v;
        }

        public bool ConsumeDive()
        {
            bool v = _dive;
            _dive = false;
            return v;
        }

        public bool ConsumeAttack()
        {
            bool v = _attackPress;
            _attackPress = false;
            return v;
        }

        public void ClearPresses()
        {
            _jump = false;
            _dive = false;
            _attackPress = false;
        }

        private void Awake()
        {
            _ch = GetComponent<SushiCharacter>();
            _attack = GetComponent<AttackAbility>();
            _speed = Random.Range(0.8f, 1f);
            if (_ch != null)
            {
                _ch.Respawned += OnRespawned;
            }
        }

        private void OnDestroy()
        {
            if (_ch != null)
            {
                _ch.Respawned -= OnRespawned;
            }
        }

        private void OnRespawned()
        {
            _hazardDecided.Clear();
            _carefulForWp = -1;
            _nextWanderAt = 0f;
            _stuckCheckAt = Time.time + 1.5f;
            _stuckCheckPos = transform.position;
            if (Level == null || Level.BotPath.Count == 0)
            {
                return;
            }
            // 가장 가까운 길 점부터 다시
            float best = float.MaxValue;
            int bestIndex = 0;
            Vector3 pos = transform.position;
            for (int i = 0; i < Level.BotPath.Count; i++)
            {
                float d = (Level.BotPath[i].Position - pos).sqrMagnitude;
                if (d < best)
                {
                    best = d;
                    bestIndex = i;
                }
            }
            _wp = bestIndex;
            NewOffset();
        }

        private void NewOffset()
        {
            Vector2 o = Random.insideUnitCircle * 0.7f;
            _offset = new Vector3(o.x, 0f, o.y);
        }

        private void Update()
        {
            float now = Time.time;
            if (_ch == null || Level == null || _ch.IsDespawned)
            {
                _move = Vector2.zero;
                return;
            }
            Vector3 pos = transform.position;
            Vector3 target;
            bool waitAtEdge = false;

            if (Level.BotPath.Count > 0)
            {
                _wp = Mathf.Clamp(_wp, 0, Level.BotPath.Count - 1);
                BotWaypoint wp = Level.BotPath[_wp];
                target = wp.Position + (wp.Jump ? Vector3.zero : _offset);
                Vector3 to = target - pos;
                to.y = 0f;
                float dist = to.magnitude;
                if (wp.Jump && dist < wp.Radius + 0.3f)
                {
                    // 내릴 자리(다음 길 점)를 낮은 막대가 곧 쓸고 지나가면 가장자리에서 기다렸다 뛰어요
                    Vector3 landing = Level.BotPath[Mathf.Min(_wp + 1, Level.BotPath.Count - 1)].Position;
                    if (_ch.IsGrounded && IsCarefulHere() && LowBarArrivesWithin(landing, AirTime() + LandingClearMargin))
                    {
                        waitAtEdge = true;
                    }
                    else
                    {
                        if (_ch.IsGrounded)
                        {
                            TryJump(now);
                        }
                        Advance();
                    }
                }
                else if (!wp.Jump && dist < wp.Radius)
                {
                    Advance();
                }
            }
            else
            {
                if (now >= _nextWanderAt || (_wanderTarget - pos).sqrMagnitude < 1f)
                {
                    PickWanderTarget(now);
                }
                target = _wanderTarget;
            }

            Vector3 dir = target - pos;
            dir.y = 0f;
            if (waitAtEdge)
            {
                dir = Vector3.zero;
            }
            if (dir.sqrMagnitude > 1e-4f)
            {
                dir.Normalize();
            }
            else
            {
                dir = Vector3.zero;
            }

            // 낭떠러지 확인 (앞 0.9 m 아래에 바닥이 없으면)
            if (_ch.IsGrounded && dir != Vector3.zero)
            {
                Vector3 probe = pos + dir * 0.9f + Vector3.up * 0.5f;
                bool ground = Physics.Raycast(probe, Vector3.down, 2.5f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
                if (!ground)
                {
                    if (Level.BotEdgeJumps)
                    {
                        // 건너편 내릴 자리를 낮은 막대가 곧 쓸면 가장자리에서 멈춰 기다려요
                        if (IsCarefulHere() && LowBarArrivesWithin(pos + dir * 2.5f, AirTime() + LandingClearMargin))
                        {
                            dir = Vector3.zero;
                        }
                        else
                        {
                            TryJump(now);
                        }
                    }
                    else
                    {
                        // 가장자리 → 가운데 쪽으로 방향을 틀고 목표도 새로
                        Vector3 toCenter = Level.WanderCenter - pos;
                        toCenter.y = 0f;
                        dir = toCenter.sqrMagnitude > 1e-4f ? toCenter.normalized : -dir;
                        PickWanderTarget(now);
                    }
                }
            }

            CheckHazards(now, pos);

            // 가끔 다이브 (앞이 바닥일 때만)
            if (_ch.IsGrounded && dir != Vector3.zero && Random.value < DiveChancePerSecond * Time.deltaTime)
            {
                Vector3 far = pos + dir * 3f + Vector3.up * 0.5f;
                if (Physics.Raycast(far, Vector3.down, 2.5f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                {
                    _dive = true;
                }
            }

            // 가끔 공격 (반지름 안에 누가 있으면)
            if (_attack != null && _attack.CanAttackNow && !Level.BotAttacksPaused && SomeoneNear(pos, _attack.Config.Radius * 0.9f)
                && Random.value < AttackChancePerSecond * Time.deltaTime)
            {
                _attackPress = true;
            }

            // 끼어서 못 움직이면 점프 + 길에서 조금 비켜 보기
            if (now >= _stuckCheckAt)
            {
                if ((pos - _stuckCheckPos).sqrMagnitude < 0.09f && dir != Vector3.zero && _ch.IsGrounded)
                {
                    TryJump(now);
                    NewOffset();
                }
                _stuckCheckPos = pos;
                _stuckCheckAt = now + 1.5f;
            }

            _move = new Vector2(dir.x, dir.z) * _speed;
        }

        private void Advance()
        {
            if (Level.BotPath.Count == 0)
            {
                return;
            }
            _wp = Mathf.Min(_wp + 1, Level.BotPath.Count - 1);
            NewOffset();
        }

        private void PickWanderTarget(float now)
        {
            Vector2 p = Random.insideUnitCircle * Level.WanderRadius;
            _wanderTarget = Level.WanderCenter + new Vector3(p.x, 0f, p.y);
            _nextWanderAt = now + Random.Range(1.5f, 3.5f);
        }

        private void TryJump(float now)
        {
            if (now < _nextJumpAllowed || now < _suppressJumpUntil)
            {
                return;
            }
            _jump = true;
            _nextJumpAllowed = now + 0.45f;
        }

        /// <summary>회전 막대가 내 쪽으로 오는 시간을 계산 → 낮으면 제때 점프, 높으면 점프 참기.</summary>
        private void CheckHazards(float now, Vector3 pos)
        {
            List<KnockHazard> hazards = KnockHazard.Active;
            for (int i = 0; i < hazards.Count; i++)
            {
                KnockHazard h = hazards[i];
                if (h == null || h.Rotator == null)
                {
                    continue;
                }
                Vector3 pivot = h.Rotator.transform.position;
                Vector3 v = pos - pivot;
                v.y = 0f;
                float dist = v.magnitude;
                if (dist > h.HalfLength + 0.6f || dist < 0.3f)
                {
                    continue;
                }
                float omega = h.Rotator.YawRateDeg;
                // 같은 축으로 도는 접시 위에 서 있으면 막대는 (막대 속도 - 접시 속도)로 다가와요
                var plate = _ch.GroundPlatform as Rotator;
                if (plate != null && plate != h.Rotator && (plate.transform.position - pivot).sqrMagnitude < 0.25f)
                {
                    omega -= plate.YawRateDeg;
                }
                if (Mathf.Abs(omega) < 1f)
                {
                    continue;
                }
                Vector3 barDir = h.transform.forward;
                barDir.y = 0f;
                // 막대는 양쪽으로 뻗어서 "선" — 각도를 -90~90으로
                float a = Vector3.SignedAngle(barDir, v, Vector3.up);
                if (a > 90f)
                {
                    a -= 180f;
                }
                else if (a < -90f)
                {
                    a += 180f;
                }
                float time = a / omega; // 막대가 내 각도에 닿기까지 (초), 음수면 지나감
                if (time < 0f)
                {
                    time += 180f / Mathf.Abs(omega); // 반대쪽 끝이 다음에 옴
                }

                if (h.Low)
                {
                    if (time < 0.2f && _ch.IsGrounded)
                    {
                        float decided;
                        if (_hazardDecided.TryGetValue(h, out decided) && now < decided)
                        {
                            continue;
                        }
                        _hazardDecided[h] = now + 0.5f;
                        if (Random.value < Skill)
                        {
                            _suppressJumpUntil = 0f;
                            _nextJumpAllowed = 0f;
                            TryJump(now);
                        }
                    }
                }
                else if (time < 0.7f)
                {
                    _suppressJumpUntil = now + 0.15f; // 높은 꼬치 아래에서는 뛰지 않기
                }
            }
        }

        /// <summary>지금 길 점에서 내릴 자리를 살피나요 (실력 확률, 점마다 한 번 정함 — 매 프레임 다시 뽑으면 결국 다 살피게 돼요).</summary>
        private bool IsCarefulHere()
        {
            if (_carefulForWp != _wp)
            {
                _carefulForWp = _wp;
                _careful = Random.value < Skill;
            }
            return _careful;
        }

        /// <summary>점프 한 번의 체공 시간 (초).</summary>
        private float AirTime()
        {
            float g = Mathf.Abs(Physics.gravity.y) * Mathf.Max(0.1f, _ch.GravityScale);
            return 2f * _ch.Tuning.JumpSpeed / g;
        }

        /// <summary>낮은 회전 막대가 seconds 안에 point를 쓸고 지나가나요 (월드 기준 회전 속도).</summary>
        private static bool LowBarArrivesWithin(Vector3 point, float seconds)
        {
            List<KnockHazard> hazards = KnockHazard.Active;
            for (int i = 0; i < hazards.Count; i++)
            {
                KnockHazard h = hazards[i];
                if (h == null || !h.Low || h.Rotator == null)
                {
                    continue;
                }
                Vector3 v = point - h.Rotator.transform.position;
                v.y = 0f;
                float dist = v.magnitude;
                if (dist > h.HalfLength + 0.6f || dist < 0.3f)
                {
                    continue;
                }
                float omega = h.Rotator.YawRateDeg;
                if (Mathf.Abs(omega) < 1f)
                {
                    continue;
                }
                Vector3 barDir = h.transform.forward;
                barDir.y = 0f;
                float a = Vector3.SignedAngle(barDir, v, Vector3.up);
                if (a > 90f)
                {
                    a -= 180f;
                }
                else if (a < -90f)
                {
                    a += 180f;
                }
                float time = a / omega;
                if (time < 0f)
                {
                    time += 180f / Mathf.Abs(omega);
                }
                if (time < seconds)
                {
                    return true;
                }
            }
            return false;
        }

        private bool SomeoneNear(Vector3 pos, float radius)
        {
            float r2 = radius * radius;
            List<SushiCharacter> all = SushiCharacter.All;
            for (int i = 0; i < all.Count; i++)
            {
                SushiCharacter o = all[i];
                if (o == null || o == _ch || o.IsDespawned || o.IsDown)
                {
                    continue;
                }
                if ((o.transform.position - pos).sqrMagnitude <= r2)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
