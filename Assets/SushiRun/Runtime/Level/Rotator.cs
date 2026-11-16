// 키네마틱 회전체 (회전 접시·휘두르는 젓가락·꼬치). FixedUpdate에서 rb.MoveRotation으로 돌려야
// 위에 탄 캐릭터·부딪힌 캐릭터와 물리가 맞아요 (transform을 직접 돌리면 순간이동으로 처리됨).
using UnityEngine;

namespace SushiRun.Runtime
{
    [DisallowMultipleComponent]
    public sealed class Rotator : MonoBehaviour, IPlatformMotion
    {
        [Tooltip("돌리는 축 (로컬)")]
        public Vector3 LocalAxis = Vector3.up;

        [Tooltip("초당 회전 (도). 음수면 반대로")]
        public float SpeedDeg = 30f;

        [Tooltip("속도 크기가 초당 이만큼 늘어요 (도/초²) — 결승 꼬치처럼 점점 빨라지게")]
        public float AccelerationDeg = 0f;

        public float MaxSpeedDeg = 720f;

        private Rigidbody _rb;
        private float _startSpeed;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            if (_rb == null)
            {
                _rb = gameObject.AddComponent<Rigidbody>();
            }
            _rb.isKinematic = true;
            _rb.useGravity = false;
            _rb.interpolation = RigidbodyInterpolation.Interpolate;
            _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        }

        private void Start()
        {
            _startSpeed = SpeedDeg;
        }

        /// <summary>처음 속도로 되돌려요.</summary>
        public void ResetSpeed()
        {
            SpeedDeg = _startSpeed;
        }

        private void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            if (AccelerationDeg != 0f)
            {
                float sign = SpeedDeg < 0f ? -1f : 1f;
                float mag = Mathf.Min(Mathf.Abs(SpeedDeg) + AccelerationDeg * dt, MaxSpeedDeg);
                SpeedDeg = sign * Mathf.Max(0f, mag);
            }
            Quaternion step = Quaternion.AngleAxis(SpeedDeg * dt, LocalAxis);
            _rb.MoveRotation(_rb.rotation * step);
        }

        public Vector3 WorldAngularVelocity
        {
            get
            {
                Quaternion rot = _rb != null ? _rb.rotation : transform.rotation;
                return (rot * LocalAxis).normalized * (SpeedDeg * Mathf.Deg2Rad);
            }
        }

        public Vector3 GetPointVelocity(Vector3 worldPoint)
        {
            Vector3 pivot = _rb != null ? _rb.position : transform.position;
            return Vector3.Cross(WorldAngularVelocity, worldPoint - pivot);
        }

        public float YawRateDeg
        {
            get
            {
                Quaternion rot = _rb != null ? _rb.rotation : transform.rotation;
                return (rot * LocalAxis).normalized.y * SpeedDeg;
            }
        }
    }
}
