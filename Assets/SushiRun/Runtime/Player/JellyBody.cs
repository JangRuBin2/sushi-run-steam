// "쫀득쫀득" 보이는 몸 — Core/Motion/JellyMotion 결과를 Visual(발바닥 피벗)·Topping에 입혀요.
// 화면 연출만: 물리·히트박스(SushiCharacter 콜라이더)는 절대 바꾸지 않아요.
using SushiRun.Core.Motion;
using SushiRun.Core.Movement;
using UnityEngine;

namespace SushiRun.Runtime
{
    [DisallowMultipleComponent]
    public sealed class JellyBody : MonoBehaviour
    {
        public JellyTuning Tuning = new JellyTuning();

        [Tooltip("속도 평활 (클수록 가속도 추정이 민감)")]
        public float VelocitySmoothing = 14f;

        [Tooltip("가속도 추정 상한 (m/s²) — 부활·넉백 순간 튀는 값 자르기")]
        public float MaxAccel = 45f;

        [Tooltip("다이브·엎드림 때 앞으로 눕는 각도")]
        public float ProneAngle = 80f;

        [Tooltip("엎드렸을 때 몸 가운데를 땅 위로 띄우는 높이")]
        public float ProneLift = 0.32f;

        private SushiCharacter _ch;
        private AttackAbility _attack;
        private Transform _visual;
        private Transform _topping;
        private Vector3 _visualBaseScale = Vector3.one;
        private Vector3 _toppingBasePos;
        private Vector3 _toppingBaseScale = Vector3.one;
        private JellyMotion _jelly;
        private JellyTuning _jellyTuningRef;
        private Vector3 _smoothVel;
        private Vector3 _prevSmoothVel;
        private bool _hasPrev;
        private Spring1D _extraPitch; // 다이브·엎드림·넘어짐 기울기
        private Spring1D _extraRoll;
        private float _downSpinPhase;

        public JellyPose Pose => _jelly != null ? _jelly.Pose : JellyPose.Rest;

        /// <summary>캐릭터·보이는 몸·토핑을 연결 (SushiToyBuilder 다음에 한 번).</summary>
        public void Bind(SushiCharacter character, Transform visual, Transform topping)
        {
            Unbind();
            _ch = character;
            _visual = visual;
            _topping = topping;
            if (_visual != null)
            {
                _visualBaseScale = _visual.localScale;
            }
            if (_topping != null)
            {
                _toppingBasePos = _topping.localPosition;
                _toppingBaseScale = _topping.localScale;
            }
            ResetMotion();
            if (_ch != null)
            {
                _ch.Jumped += OnJumped;
                _ch.Landed += OnLanded;
                _ch.Dived += OnDived;
                _ch.KnockedDown += OnKnocked;
                _ch.GotUp += OnGotUp;
                _ch.Respawned += ResetMotion;
                _attack = _ch.GetComponent<AttackAbility>();
                if (_attack != null)
                {
                    _attack.Attacked += OnAttacked;
                }
            }
        }

        private void Unbind()
        {
            if (_ch != null)
            {
                _ch.Jumped -= OnJumped;
                _ch.Landed -= OnLanded;
                _ch.Dived -= OnDived;
                _ch.KnockedDown -= OnKnocked;
                _ch.GotUp -= OnGotUp;
                _ch.Respawned -= ResetMotion;
            }
            if (_attack != null)
            {
                _attack.Attacked -= OnAttacked;
            }
            _attack = null;
        }

        private void OnDestroy()
        {
            Unbind();
        }

        /// <summary>튜닝 묶음을 바꿔 끼워요 (같은 객체 안의 값 변경은 바로 반영돼서 이건 필요 없음).</summary>
        public void SetTuning(JellyTuning tuning)
        {
            Tuning = tuning ?? new JellyTuning();
            ResetMotion();
        }

        private void ResetMotion()
        {
            if (Tuning == null)
            {
                Tuning = new JellyTuning();
            }
            _jelly = new JellyMotion(Tuning);
            _jellyTuningRef = Tuning;
            _hasPrev = false;
            _smoothVel = Vector3.zero;
            _prevSmoothVel = Vector3.zero;
            _extraPitch = new Spring1D(0f);
            _extraRoll = new Spring1D(0f);
            _downSpinPhase = 0f;
        }

        private void OnJumped()
        {
            _jelly?.Jump();
        }

        private void OnLanded(float impact)
        {
            _jelly?.Land(impact);
        }

        private void OnDived()
        {
            _jelly?.Dive();
        }

        private void OnKnocked(Vector3 velocity)
        {
            _jelly?.Hit(Mathf.Clamp01(velocity.magnitude / 10f));
            _extraPitch.Kick(-260f); // 뒤로 벌렁
            _extraRoll.Kick(Random.Range(-1f, 1f) * 200f);
        }

        private void OnGotUp()
        {
            _jelly?.GetUp();
        }

        private void OnAttacked(int hits)
        {
            _jelly?.Hit(0.4f); // 공격하는 쪽도 "꾹" 눌렸다 튀어요
        }

        private void LateUpdate()
        {
            if (_ch == null || _visual == null || _ch.IsDespawned)
            {
                return;
            }
            float dt = Time.deltaTime;
            if (dt <= 0f)
            {
                return;
            }
            if (_jelly == null || _jellyTuningRef != Tuning)
            {
                ResetMotion();
            }

            // 가속도 추정: 발판 기준 속도를 평활 → 차이 / dt → 캐릭터 로컬 축으로
            Vector3 vel = _ch.RelativeVelocity;
            float a = 1f - Mathf.Exp(-VelocitySmoothing * dt);
            _smoothVel = _hasPrev ? Vector3.Lerp(_smoothVel, vel, a) : vel;
            Vector3 worldAccel = _hasPrev ? (_smoothVel - _prevSmoothVel) / dt : Vector3.zero;
            _prevSmoothVel = _smoothVel;
            _hasPrev = true;
            worldAccel.y = 0f;
            worldAccel = Vector3.ClampMagnitude(worldAccel, MaxAccel);
            Vector3 localAccel = _ch.transform.InverseTransformDirection(worldAccel);

            JellyPose pose = _jelly.Step(new JellyInput
            {
                Dt = dt,
                LocalAccelX = localAccel.x,
                LocalAccelZ = localAccel.z,
                VerticalVelocity = vel.y,
                HorizontalSpeed = new Vector2(vel.x, vel.z).magnitude,
                Grounded = _ch.IsGrounded,
            });

            // 상태별 추가 자세: 다이브·엎드림 = 앞으로 눕기, 넘어짐 = 뒤로 벌렁 + 데굴
            MotorMode mode = _ch.Mode;
            float pitchTarget = 0f;
            float rollTarget = 0f;
            if (mode == MotorMode.Diving || mode == MotorMode.Prone)
            {
                pitchTarget = ProneAngle;
            }
            else if (mode == MotorMode.Down)
            {
                _downSpinPhase += dt * 9f;
                pitchTarget = -70f;
                rollTarget = Mathf.Sin(_downSpinPhase) * 22f;
            }
            _extraPitch.Step(pitchTarget, 3.4f, 0.55f, dt);
            _extraRoll.Step(rollTarget, 3.0f, 0.4f, dt);

            float pitch = pose.LeanPitch + _extraPitch.Value;
            float roll = pose.LeanRoll + _extraRoll.Value;
            _visual.localRotation = Quaternion.Euler(pitch, 0f, -roll); // +roll = 오른쪽(+X)으로 기울기

            // 발바닥 피벗으로 크게 누우면 몸이 앞(뒤)으로 삐져나가니, 눕는 만큼 가운데로 당기고 살짝 띄워요
            float lieAngle = Mathf.Clamp(_extraPitch.Value, -100f, 100f) * Mathf.Deg2Rad;
            float lie = Mathf.Abs(Mathf.Sin(lieAngle));
            _visual.localPosition = new Vector3(0f, ProneLift * lie, -Mathf.Sin(lieAngle) * 0.45f);

            _visual.localScale = Vector3.Scale(_visualBaseScale, new Vector3(pose.ScaleX, pose.ScaleY, pose.ScaleZ));

            if (_topping != null)
            {
                // 토핑 오프셋은 m 단위 → 부모(Visual) 배율로 나눠 로컬로
                Vector3 s = _visual.localScale;
                var offset = new Vector3(
                    pose.ToppingX / Mathf.Max(0.01f, s.x),
                    pose.ToppingY / Mathf.Max(0.01f, s.y),
                    pose.ToppingZ / Mathf.Max(0.01f, s.z));
                _topping.localPosition = _toppingBasePos + offset;
                float localY = pose.ToppingScaleY / Mathf.Max(0.01f, pose.ScaleY); // 토핑은 몸보다 덜 눌림
                _topping.localScale = new Vector3(_toppingBaseScale.x, _toppingBaseScale.y * localY, _toppingBaseScale.z);
            }
        }
    }
}
