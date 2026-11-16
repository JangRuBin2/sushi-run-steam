// 3인칭 궤도 카메라: 시점 입력으로 yaw/pitch, 대상 위치는 축마다 임계 감쇠 스프링으로 따라가요.
// 벽을 뚫지 않게 SphereCast로 당기고, 다이브 때 시야각 킥, 큰 착지·넘어짐 때 살짝 흔들림.
using SushiRun.Core.Motion;
using SushiRun.Core.Movement;
using UnityEngine;

namespace SushiRun.Runtime
{
    [DisallowMultipleComponent]
    public sealed class FollowCamera : MonoBehaviour
    {
        public SushiCharacter Target;
        public PlayerInputReader LookInput;

        public float Distance = 6.2f;
        public float HeightOffset = 1.1f;
        public float Yaw;
        public float Pitch = 17f;
        public float MinPitch = -25f;
        public float MaxPitch = 70f;

        [Tooltip("수평 따라가기 스프링 (Hz). damping 1 = 지나치지 않음")]
        public float FollowFrequency = 3.2f;

        [Tooltip("세로는 조금 느슨하게 (점프할 때 화면이 덜 출렁)")]
        public float VerticalFrequency = 2.2f;

        public float FollowDamping = 1f;

        public float CollisionRadius = 0.25f;
        public float MinDistance = 0.8f;

        public float BaseFov = 60f;
        public float DiveFovKick = 7f;

        public float BigLandingSpeed = 9f;

        private Camera _cam;
        private Spring1D _fx;
        private Spring1D _fy;
        private Spring1D _fz;
        private Spring1D _fov;
        private Spring1D _dist;
        private Vector3 _lastFocus;
        private float _shake;
        private bool _snapped;
        private SushiCharacter _subscribed;

        public Camera Cam => _cam;

        private void Awake()
        {
            _cam = GetComponent<Camera>();
            _fov = new Spring1D(BaseFov);
            _dist = new Spring1D(Distance);
        }

        public void SetTarget(SushiCharacter target)
        {
            if (_subscribed != null)
            {
                _subscribed.Landed -= OnLanded;
                _subscribed.KnockedDown -= OnKnocked;
                _subscribed.Respawned -= SnapToTarget;
            }
            _subscribed = null;
            Target = target;
            if (target != null)
            {
                target.Landed += OnLanded;
                target.KnockedDown += OnKnocked;
                target.Respawned += SnapToTarget;
                _subscribed = target;
                Yaw = target.FacingDeg;
            }
            SnapToTarget();
        }

        private void OnDestroy()
        {
            if (_subscribed != null)
            {
                _subscribed.Landed -= OnLanded;
                _subscribed.KnockedDown -= OnKnocked;
                _subscribed.Respawned -= SnapToTarget;
            }
        }

        private void OnLanded(float impact)
        {
            if (impact >= BigLandingSpeed)
            {
                _shake = Mathf.Max(_shake, Mathf.Min(0.14f, impact * 0.008f));
            }
        }

        private void OnKnocked(Vector3 velocity)
        {
            _shake = Mathf.Max(_shake, 0.16f);
        }

        /// <summary>스프링 없이 바로 대상 뒤로 (부활·맵 바꿀 때).</summary>
        public void SnapToTarget()
        {
            Vector3 focus = FocusPoint();
            _fx = new Spring1D(focus.x);
            _fy = new Spring1D(focus.y);
            _fz = new Spring1D(focus.z);
            _dist = new Spring1D(Distance);
            _lastFocus = focus;
            _snapped = true;
        }

        private Vector3 FocusPoint()
        {
            if (Target != null && !Target.IsDespawned)
            {
                _lastFocus = Target.transform.position + Vector3.up * HeightOffset;
            }
            return _lastFocus;
        }

        private void LateUpdate()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f)
            {
                return;
            }
            if (!_snapped)
            {
                SnapToTarget();
            }

            if (LookInput != null)
            {
                Vector2 look = LookInput.ConsumeLook();
                Yaw = Mathf.Repeat(Yaw + look.x, 360f);
                Pitch = Mathf.Clamp(Pitch - look.y, MinPitch, MaxPitch);
            }

            Vector3 focus = FocusPoint();
            var hCoef = SpringCoefficients.For(FollowFrequency, FollowDamping, dt);
            _fx.Apply(focus.x, hCoef);
            _fz.Apply(focus.z, hCoef);
            _fy.Step(focus.y, VerticalFrequency, FollowDamping, dt);
            var smoothFocus = new Vector3(_fx.Value, _fy.Value, _fz.Value);

            Quaternion rot = Quaternion.Euler(Pitch, Yaw, 0f);
            Vector3 back = rot * Vector3.back;

            // 벽 뚫지 않기: 가까워질 때는 바로, 멀어질 때는 천천히
            float wanted = Distance;
            RaycastHit hit;
            if (Physics.SphereCast(smoothFocus, CollisionRadius, back, out hit, Distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                wanted = Mathf.Max(MinDistance, hit.distance - 0.05f);
            }
            if (wanted < _dist.Value)
            {
                _dist = new Spring1D(wanted);
            }
            else
            {
                _dist.Step(wanted, 1.5f, 1f, dt);
            }

            Vector3 pos = smoothFocus + back * _dist.Value;
            if (_shake > 0.0005f)
            {
                pos += Random.insideUnitSphere * _shake;
                _shake = Mathf.MoveTowards(_shake, 0f, dt * 0.6f);
            }
            transform.SetPositionAndRotation(pos, rot);

            if (_cam != null)
            {
                bool diving = Target != null && Target.Mode == MotorMode.Diving;
                _fov.Step(BaseFov + (diving ? DiveFovKick : 0f), 3.5f, 0.65f, dt);
                _cam.fieldOfView = _fov.Value;
            }
        }
    }
}
