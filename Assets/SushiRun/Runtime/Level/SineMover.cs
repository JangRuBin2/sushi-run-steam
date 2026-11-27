// 키네마틱으로 왔다 갔다 하는 물체 (좌우로 미는 스펀지, 물에 떠서 출렁이는 컵).
// FixedUpdate에서 rb.MovePosition으로 옮겨야 위에 탄 캐릭터·부딪힌 캐릭터와 물리가 맞아요.
using UnityEngine;

namespace SushiRun.Runtime
{
    [DisallowMultipleComponent]
    public sealed class SineMover : MonoBehaviour, IPlatformMotion
    {
        [Tooltip("움직이는 방향 (월드)")]
        public Vector3 Axis = Vector3.right;

        [Tooltip("가운데에서 한쪽 끝까지 (m)")]
        public float Amplitude = 3f;

        [Tooltip("한 번 왕복하는 시간 (초)")]
        public float Period = 8f;

        [Tooltip("시작 위상 (0~1 = 한 주기 중 어디서 시작)")]
        public float Phase;

        private Rigidbody _rb;
        private Vector3 _origin;
        private Vector3 _velocity;

        public Vector3 Velocity => _velocity;

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
            _origin = transform.position; // 자리를 잡은 뒤에 붙여요 (SinkKit이 그렇게 함)
        }

        private void FixedUpdate()
        {
            float period = Mathf.Max(0.1f, Period);
            float w = Mathf.PI * 2f / period;
            float a = w * Time.fixedTime + Phase * Mathf.PI * 2f;
            Vector3 axis = Axis.normalized;
            _velocity = axis * (Amplitude * w * Mathf.Cos(a));
            _rb.MovePosition(_origin + axis * (Amplitude * Mathf.Sin(a)));
        }

        public Vector3 GetPointVelocity(Vector3 worldPoint)
        {
            return _velocity;
        }

        public float YawRateDeg => 0f;
    }
}
