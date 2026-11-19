// 시제품 봇 — ISushiInput을 구현해서 사람과 같은 컨트롤러로 달려요 (부딪히고 때릴 상대).
// Race: 맵이 준 길(BotPath)을 조금씩 벗어나며 따라가고, 길 끝·낭떠러지에서 점프.
// Final: 접시 위를 돌아다니며 가장자리를 피하고, 낮은 꼬치가 다가오면 점프, 높은 꼬치가 가까우면 점프 참기.
// 사람처럼: 쓸고 지나가는 막대 앞에서는 막대를 보고 지나간 틈에 들어가고, 도는 접시 위에서는 밀리는 만큼
// 반대로 버티고, 막대가 오면 몸이 뜨는 시간을 감안해 미리 뛰어요. 실력이 낮을수록 판단·반응이 어긋나요.
// 가끔 다이브·공격. 판정은 없어요 (입력만 만듦).
using System.Collections.Generic;
using SushiRun.Core.Movement;
using UnityEngine;

namespace SushiRun.Runtime
{
    [DisallowMultipleComponent]
    public sealed class WanderBot : MonoBehaviour, ISushiInput
    {
        public PrototypeLevel Level;

        [Range(0f, 1f)]
        [Tooltip("실력: 막대를 보고 뛰는 확률, 들어갈 때를 고르는 눈·반응 속도, 도는 접시에서 버티는 정도")]
        public float Skill = 0.75f;

        [Tooltip("공격 반지름 안에 누가 있을 때 초당 공격 확률")]
        public float AttackChancePerSecond = 0.2f;

        [Tooltip("평지에서 초당 다이브 확률")]
        public float DiveChancePerSecond = 0.05f;

        [Tooltip("기다릴 때 도움닫기로 남겨 둘 거리 (m)")]
        public float RunUpDistance = 2.5f;

        [Header("막대 보고 건너기")]
        [Tooltip("막대가 쓰는 구역 입구가 이만큼 앞이면 멈춰서 들어갈 때를 봐요 (m)")]
        public float GateDistance = 1.5f;

        [Tooltip("들어갈지 판단할 때 앞으로 내다보는 길 (m)")]
        public float CrossLookahead = 14f;

        [Tooltip("실력 0일 때 막대 타이밍을 잘못 보는 정도 (초, ±)")]
        public float MaxJudgeError = 0.6f;

        [Tooltip("실력 0일 때 점프 반응이 어긋나는 정도 (초, ±)")]
        public float MaxReflexError = 0.12f;

        [Tooltip("누른 점프가 몸에 걸리기까지 (초, 대략 한두 물리 틱)")]
        public float JumpInputDelay = 0.04f;

        [Header("방해꾼")]
        [Tooltip("켜면 가까운 캐릭터를 쫓아가 밀고 때리고 다이브로 들이받아요 (판정은 같고 행동만 달라요)")]
        public bool Troublemaker;
        public float ChaseRadius = 5f;
        public float ChaseSeconds = 2f;
        public float ChaseCooldown = 3f;

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
        private bool _gateGo; // 이번 구역은 들어가기로 정함
        private float _waitSince = -1f;
        private float _nextJudgeAt;
        private float _reflexError;
        private bool _holding; // 기다리는 자리를 잡았음
        private Vector3 _holdPos;
        private float _runUpSince = -1f; // 점프 지점에서 가장자리까지 달려가는 중 (시작 시각)
        private float _edgeLead = 0.5f; // 이만큼 앞이 낭떠러지면 뛰어요
        private SushiCharacter _chase;
        private float _chaseUntil;
        private float _nextChaseAt;

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
            _holding = false;
            _runUpSince = -1f;
            ResetGate();
            _chase = null;
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
                target = PathPoint(_wp) + (wp.Jump ? Vector3.zero : _offset);
                Vector3 to = target - pos;
                to.y = 0f;
                float dist = to.magnitude;
                if (WaitForSweep(now, pos, wp, dist))
                {
                    // 앞 구역을 막대가 곧 쓸어요 → 막대가 지나가기를 보며 기다려요 (점프 지점이면 도움닫기 거리를 두고)
                    _runUpSince = -1f;
                    waitAtEdge = true;
                    if (wp.Jump && dist < wp.Radius + 0.3f + RunUpDistance)
                    {
                        target = pos - to; // 뒤로 물러나 도움닫기 자리 확보
                        waitAtEdge = false;
                    }
                }
                else if (wp.Jump && (_runUpSince >= 0f || dist < wp.Radius + 0.3f))
                {
                    // 사람처럼: 점프 지점에 오면 건너편을 보고 끝까지 달려가 가장자리에서 뛰어요 (아래 낭떠러지 확인이 누름).
                    // 이미 떠 있으면 넘어가요. 가장자리가 안 나오면 잠깐 뒤 그 자리에서 뛰어요.
                    if (!_ch.IsGrounded)
                    {
                        _runUpSince = -1f;
                        Advance();
                    }
                    else
                    {
                        if (_runUpSince < 0f)
                        {
                            _runUpSince = now;
                            _edgeLead = 0.5f + Random.value * (1f - Skill) * 1.2f; // 실력이 낮으면 일찍 뛰기도 해요
                        }
                        if (_wp + 1 < Level.BotPath.Count)
                        {
                            target = PathPoint(_wp + 1);
                        }
                        if (now - _runUpSince > 1.2f && TryJump(now, true))
                        {
                            _runUpSince = -1f;
                            Advance();
                        }
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

            if (Troublemaker)
            {
                Vector3 victim;
                if (ChaseTarget(now, pos, out victim))
                {
                    target = victim;
                    waitAtEdge = false;
                    _runUpSince = -1f; // 쫓는 동안은 점프하러 달려가지 않아요
                }
            }

            Vector3 dir = target - pos;
            dir.y = 0f;
            if (waitAtEdge)
            {
                // 기다리는 동안 그 자리를 지켜요: 도는 접시에 밀려 나가면 보고 되돌아와요 (실력이 낮으면 늦게 알아채요)
                if (!_holding)
                {
                    _holding = true;
                    _holdPos = pos;
                }
                dir = _holdPos - pos;
                dir.y = 0f;
                if (dir.magnitude < Mathf.Lerp(0.6f, 0.2f, Skill))
                {
                    dir = Vector3.zero;
                }
            }
            else
            {
                _holding = false;
            }
            if (dir.sqrMagnitude > 1e-4f)
            {
                dir.Normalize();
            }
            else
            {
                dir = Vector3.zero;
            }

            // 낭떠러지 확인 (앞 0.9 m, 점프하러 달려가는 중이면 _edgeLead 아래에 바닥이 없으면)
            if (_ch.IsGrounded && dir != Vector3.zero)
            {
                Vector3 probe = pos + dir * (_runUpSince >= 0f ? _edgeLead : 0.9f) + Vector3.up * 0.5f;
                bool ground = Physics.Raycast(probe, Vector3.down, 2.5f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
                if (!ground)
                {
                    if (Level.BotEdgeJumps)
                    {
                        TryJump(now, true);
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

            // 가끔 다이브 (앞이 끊김 없이 바닥일 때만 — 틈 너머 바닥을 보고 뛰어들지 않아요)
            if (_ch.IsGrounded && dir != Vector3.zero && Random.value < DiveChancePerSecond * Time.deltaTime && GroundAhead(pos, dir))
            {
                _dive = true;
            }

            // 방해꾼: 쫓던 상대가 바로 앞이면 다이브로 들이받기 (앞이 바닥일 때만)
            if (Troublemaker && _chase != null && _ch.IsGrounded && dir != Vector3.zero && GroundAhead(pos, dir))
            {
                Vector3 toV = _chase.transform.position - pos;
                toV.y = 0f;
                if (toV.magnitude < 2.2f && Vector3.Dot(toV.normalized, dir) > 0.8f && Random.value < 2f * Time.deltaTime)
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

            _move = SteerOnPlatform(dir * (_runUpSince >= 0f ? 1f : _speed)); // 도움닫기는 힘껏
        }

        /// <summary>도는 접시·벨트 위에서는 실려 가는 만큼 반대로 버텨요 (사람이 하듯, 실력이 낮으면 덜).</summary>
        private Vector2 SteerOnPlatform(Vector3 want)
        {
            if (_ch.IsGrounded && _ch.GroundPlatform != null && !_ch.IsDown)
            {
                float run = Mathf.Max(0.1f, _ch.Tuning.RunSpeed);
                Vector3 carry = _ch.PlatformVelocity;
                carry.y = 0f;
                want -= carry * (DriftCompensation() / run);
                want.y = 0f;
                if (want.sqrMagnitude > 1f)
                {
                    want.Normalize();
                }
            }
            return new Vector2(want.x, want.z);
        }

        private float DriftCompensation()
        {
            return Mathf.Lerp(0.5f, 1f, Skill);
        }

        private void ResetGate()
        {
            _gateGo = false;
            _waitSince = -1f;
            _nextJudgeAt = 0f;
        }

        /// <summary>쓸고 지나가는 막대 구역 앞에서 기다릴까요. 사람처럼: 막대를 보다가 지나간 틈에 들어가고,
        /// 실력이 낮을수록 타이밍을 잘못 보거나 못 참고 그냥 들어가요. 한 번 들어가기로 하면 멈추지 않아요.</summary>
        private bool WaitForSweep(float now, Vector3 pos, BotWaypoint wp, float dist)
        {
            if (!_ch.IsGrounded || InSweepZone(pos))
            {
                ResetGate(); // 공중이거나 이미 구역 안 → 건너는 중 (막대는 점프로)
                return false;
            }
            float entry;
            bool clear = SweepPathClear(pos, 0f, out entry);
            bool near = entry < GateDistance || (wp.Jump && entry < CrossLookahead && dist < wp.Radius + 0.3f + RunUpDistance + 1.5f);
            if (!near)
            {
                ResetGate();
                return false;
            }
            if (_gateGo)
            {
                return false;
            }
            if (_waitSince < 0f)
            {
                _waitSince = now;
            }
            if (now - _waitSince > Mathf.Lerp(1.5f, 7f, Skill))
            {
                _gateGo = true; // 못 참고 들어가요
                return false;
            }
            if (now >= _nextJudgeAt)
            {
                _nextJudgeAt = now + Mathf.Lerp(0.4f, 0.1f, Skill); // 반응 속도
                float error = Random.Range(-1f, 1f) * (1f - Skill) * MaxJudgeError;
                if (error != 0f)
                {
                    clear = SweepPathClear(pos, error, out entry);
                }
                if (clear)
                {
                    _gateGo = true;
                    return false;
                }
            }
            return true;
        }

        /// <summary>길 점 위치. 쓸고 지나가는 막대 구역 안의 점이면, 사람처럼 막대와 같은 방향으로 돌며 지나가는
        /// 쪽을 골라요 (반대쪽으로 지나가면 막대와 마주 보고 달려서 한 번은 꼭 만나요).</summary>
        private Vector3 PathPoint(int idx)
        {
            BotWaypoint w = Level.BotPath[idx];
            if (w.Jump || idx == 0 || idx >= Level.BotPath.Count - 1)
            {
                return w.Position;
            }
            KnockHazard h = SweepAt(w.Position);
            if (h == null)
            {
                return w.Position;
            }
            float omega = h.Rotator.YawRateDeg;
            if (Mathf.Abs(omega) < 1f)
            {
                return w.Position;
            }
            Vector3 d = Level.BotPath[idx + 1].Position - Level.BotPath[idx - 1].Position;
            d.y = 0f;
            if (d.sqrMagnitude < 1e-4f)
            {
                return w.Position;
            }
            d.Normalize();
            Vector3 pivot = h.Rotator.transform.position;
            Vector3 r = w.Position - pivot;
            r.y = 0f;
            // 이 점에서 d로 달릴 때 축 둘레로 도는 방향 (위에서 봐서 +면 막대의 + 회전과 같은 쪽)
            float turn = Vector3.Cross(r, d).y;
            if (turn * omega >= 0f)
            {
                return w.Position;
            }
            Vector3 n = new Vector3(-d.z, 0f, d.x); // 진행 방향의 옆
            return w.Position - 2f * Vector3.Dot(r, n) * n;
        }

        /// <summary>낮은 막대가 쓰는 구역 안인가요.</summary>
        private bool InSweepZone(Vector3 p)
        {
            List<KnockHazard> hazards = KnockHazard.Active;
            for (int i = 0; i < hazards.Count; i++)
            {
                KnockHazard h = hazards[i];
                if (h == null || !h.Low || h.Rotator == null)
                {
                    continue;
                }
                Vector3 v = p - h.Rotator.transform.position;
                v.y = 0f;
                if (v.magnitude <= h.HalfLength + BarHalfWidth(h))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>앞길(지금 위치 → 다음 길 점들)을 달려가면, 처음 들어가는 막대 구역을 빠져나갈 때까지
        /// 막대에 안 걸리나요. timeError = 타이밍을 잘못 보는 만큼 (초). entry = 그 구역 입구까지 거리 (없으면 무한대).</summary>
        private bool SweepPathClear(Vector3 pos, float timeError, out float entry)
        {
            entry = float.PositiveInfinity;
            float speed = Mathf.Max(1f, _ch.Tuning.RunSpeed * _speed);
            KnockHazard zone = null;
            Vector3 a = pos;
            float s0 = 0f;
            const float step = 0.35f;
            for (int k = 0; k < 3 && s0 < CrossLookahead; k++)
            {
                int idx = Mathf.Min(_wp + k, Level.BotPath.Count - 1);
                Vector3 b = PathPoint(idx);
                Vector3 seg = b - a;
                seg.y = 0f;
                float len = seg.magnitude;
                if (len < 1e-3f)
                {
                    continue;
                }
                for (float d = 0f; d <= len && s0 + d < CrossLookahead; d += step)
                {
                    Vector3 p = a + seg * (d / len);
                    float t = (s0 + d) / speed + timeError;
                    if (zone == null)
                    {
                        zone = SweepAt(p);
                        if (zone == null)
                        {
                            continue;
                        }
                        entry = s0 + d;
                    }
                    if (!InZone(zone, p))
                    {
                        return true; // 빠져나감
                    }
                    if (BarAt(zone, p, Mathf.Max(0f, t)))
                    {
                        return false;
                    }
                }
                s0 += len;
                a = b;
            }
            return true;
        }

        private KnockHazard SweepAt(Vector3 p)
        {
            List<KnockHazard> hazards = KnockHazard.Active;
            for (int i = 0; i < hazards.Count; i++)
            {
                KnockHazard h = hazards[i];
                if (h != null && h.Low && h.Rotator != null && InZone(h, p))
                {
                    return h;
                }
            }
            return null;
        }

        private bool InZone(KnockHazard h, Vector3 p)
        {
            Vector3 v = p - h.Rotator.transform.position;
            v.y = 0f;
            return v.magnitude <= h.HalfLength + BarHalfWidth(h);
        }

        /// <summary>t초 뒤 막대가 점 p에 닿아 있나요 (땅 기준으로 곧게 달린다고 보고 막대의 월드 회전 속도로).</summary>
        private bool BarAt(KnockHazard h, Vector3 p, float t)
        {
            Vector3 v = p - h.Rotator.transform.position;
            v.y = 0f;
            Vector3 barDir = h.transform.forward;
            barDir.y = 0f;
            float deg = Vector3.SignedAngle(barDir, v, Vector3.up);
            return SweepTiming.InContactAt(deg, h.Rotator.YawRateDeg, v.magnitude, BarHalfWidth(h), t);
        }

        /// <summary>막대 반두께 + 판정 여유 + 내 몸 반지름 (m) — 이만큼 가까우면 닿아요.</summary>
        private float BarHalfWidth(KnockHazard h)
        {
            float bar = h.Box != null ? h.Box.size.x * Mathf.Abs(h.Box.transform.lossyScale.x) * 0.5f : 0.16f;
            return bar + h.Margin + _ch.Radius;
        }

        /// <summary>막대 윗면 높이 (월드 y, 판정 여유 포함).</summary>
        private static float BarTop(KnockHazard h)
        {
            if (h.Box == null)
            {
                return h.transform.position.y + 0.3f;
            }
            Transform t = h.Box.transform;
            return t.TransformPoint(h.Box.center).y + h.Box.size.y * Mathf.Abs(t.lossyScale.y) * 0.5f + h.Margin;
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

        /// <param name="mustJump">길 끝·낭떠러지: 땅에 있으면 방금 뛴 뒤라도 눌러요 (높은 꼬치 아래 참기는 그대로)</param>
        private bool TryJump(float now, bool mustJump = false)
        {
            if ((!mustJump && now < _nextJumpAllowed) || now < _suppressJumpUntil)
            {
                return false;
            }
            _jump = true;
            _nextJumpAllowed = now + 0.45f;
            return true;
        }

        /// <summary>앞 3 m가 끊김 없이 바닥인가요 (다이브해도 되나).</summary>
        private static bool GroundAhead(Vector3 pos, Vector3 dir)
        {
            for (int i = 1; i <= 6; i++)
            {
                Vector3 p = pos + dir * (0.5f * i) + Vector3.up * 0.5f;
                if (!Physics.Raycast(p, Vector3.down, 2.5f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>회전 막대가 내 쪽으로 오는 시간을 계산 → 낮으면 몸이 뜨는 시간만큼 미리 점프, 높으면 점프 참기.</summary>
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
                float halfWidth = BarHalfWidth(h);
                if (dist > h.HalfLength + halfWidth)
                {
                    continue;
                }
                float omega = h.Rotator.YawRateDeg;
                // 같은 축으로 도는 접시 위에서는 버티지 못한 만큼 접시와 같이 돌아요 → 그만큼 막대가 빨리 다가와요
                var plate = _ch.GroundPlatform as Rotator;
                if (plate != null && plate != h.Rotator && (plate.transform.position - pivot).sqrMagnitude < 0.25f)
                {
                    omega -= plate.YawRateDeg * (1f - DriftCompensation());
                }
                if (Mathf.Abs(omega) < 1f)
                {
                    continue;
                }
                Vector3 barDir = h.transform.forward;
                barDir.y = 0f;
                float deg = Vector3.SignedAngle(barDir, v, Vector3.up);
                float start, end;
                SweepTiming.NextContact(deg, omega, dist, halfWidth, out start, out end);

                if (h.Low)
                {
                    if (!_ch.IsGrounded)
                    {
                        continue;
                    }
                    float decided;
                    if (_hazardDecided.TryGetValue(h, out decided) && now < decided)
                    {
                        continue;
                    }
                    float g = Mathf.Abs(Physics.gravity.y) * Mathf.Max(0.1f, _ch.GravityScale);
                    float rise, fall;
                    SweepTiming.JumpClearWindow(_ch.Tuning.JumpSpeed, g, BarTop(h) - pos.y, out rise, out fall);
                    if (float.IsPositiveInfinity(rise) || start > rise + JumpInputDelay + _reflexError)
                    {
                        continue;
                    }
                    // 이번에 지나가는 막대는 한 번만 정해요 (놓치면 그대로 맞아요 — 사람처럼)
                    _hazardDecided[h] = now + Mathf.Min(end, 1.5f) + 0.05f;
                    _reflexError = Random.Range(-1f, 1f) * (1f - Skill) * MaxReflexError;
                    if (Random.value < Skill)
                    {
                        _suppressJumpUntil = 0f;
                        _nextJumpAllowed = 0f;
                        TryJump(now);
                    }
                }
                else if (start < 0.7f)
                {
                    _suppressJumpUntil = now + 0.15f; // 높은 꼬치 아래에서는 뛰지 않기
                }
            }
        }

        /// <summary>방해꾼: 가까운 상대(플레이어 먼저)를 잠깐씩 쫓아가요. 쫓는 중이면 true.</summary>
        private bool ChaseTarget(float now, Vector3 pos, out Vector3 victim)
        {
            victim = pos;
            if (_chase != null && now < _chaseUntil && !_chase.IsDespawned && !_chase.IsDown)
            {
                victim = _chase.transform.position;
                return true;
            }
            if (_chase != null)
            {
                _chase = null;
                _nextChaseAt = now + ChaseCooldown;
            }
            if (now < _nextChaseAt || !_ch.IsGrounded)
            {
                return false;
            }
            SushiCharacter best = null;
            float bestScore = float.MaxValue;
            List<SushiCharacter> all = SushiCharacter.All;
            for (int i = 0; i < all.Count; i++)
            {
                SushiCharacter o = all[i];
                if (o == null || o == _ch || o.IsDespawned || o.IsDown)
                {
                    continue;
                }
                float d = (o.transform.position - pos).magnitude;
                if (d > ChaseRadius)
                {
                    continue;
                }
                float score = d - (Level.Player == o ? 2f : 0f); // 사람 플레이어를 조금 더 노려요
                if (score < bestScore)
                {
                    bestScore = score;
                    best = o;
                }
            }
            if (best == null)
            {
                return false;
            }
            _chase = best;
            _chaseUntil = now + ChaseSeconds;
            victim = best.transform.position;
            return true;
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
