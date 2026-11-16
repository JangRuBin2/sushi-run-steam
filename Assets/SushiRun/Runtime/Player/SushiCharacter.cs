// 초밥 캐릭터 컨트롤러 (Rigidbody). 규칙은 순수 Core/Movement/MoveLogic, 여기서는 물리에 적용만 해요.
// 루트 = 발바닥 (콜라이더 중심은 Height/2 위). 보이는 몸은 자식 "Visual" (JellyBody가 움직임 — 히트박스와 무관).
using System.Collections.Generic;
using SushiRun.Core.Movement;
using UnityEngine;

namespace SushiRun.Runtime
{
    [DisallowMultipleComponent]
    public sealed class SushiCharacter : MonoBehaviour
    {
        /// <summary>캐릭터 레이어 = 내장 "Ignore Raycast"(2). 땅·카메라 검사가 캐릭터를 무시하게.</summary>
        public const int CharacterLayer = 2;

        public const int CharacterMask = 1 << CharacterLayer;

        /// <summary>살아 있는(켜진) 캐릭터 목록 — 공격 대상 찾기용 (매 프레임 FindObjects 안 함).</summary>
        public static readonly List<SushiCharacter> All = new List<SushiCharacter>();

        private static int s_nextId = 1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            All.Clear(); // 도메인 리로드를 끈 에디터에서도 깨끗하게
            s_nextId = 1;
        }

        public MoveTuning Tuning = new MoveTuning();

        [Tooltip("중력 배율 (Physics.gravity × 이 값). 통통 튀는 점프를 위해 1보다 크게")]
        public float GravityScale = 2.2f;

        public float Radius = 0.45f;
        public float Height = 0.95f;

        [Tooltip("발바닥 아래로 이 거리 안에 바닥이 있으면 땅에 선 것")]
        public float GroundProbe = 0.15f;

        [Tooltip("바닥 법선 y가 이보다 커야 땅 (0.6 ≈ 53°)")]
        public float MinGroundNormalY = 0.6f;

        [Tooltip("보이는 몸 (숨길 때·JellyBody용). SushiToyBuilder가 채워요")]
        public Transform Visual;

        public string DisplayName = "Sushi";

        public event System.Action Jumped;
        public event System.Action<float> Landed; // 착지 속도 (m/s, 양수)
        public event System.Action Dived;
        public event System.Action<Vector3> KnockedDown; // 튕긴 속도
        public event System.Action GotUp;
        public event System.Action Despawned;
        public event System.Action Respawned;

        private Rigidbody _rb;
        private CapsuleCollider _col;
        private MotorState _motor = new MotorState();
        private float _facingDeg;
        private float _groundIgnoreUntil;
        private Collider _lastGroundCollider;
        private IPlatformMotion _lastPlatform;

        public int Id { get; private set; }
        public ISushiInput Input { get; set; }
        public Rigidbody Body => _rb;
        public MotorMode Mode => _motor.Mode;
        public bool IsDown => _motor.Mode == MotorMode.Down;
        public bool IsGrounded { get; private set; }
        public bool IsDespawned { get; private set; }

        /// <summary>지금 밟고 있는 발판의 표면 속도 (땅이 아니면 0).</summary>
        public Vector3 PlatformVelocity { get; private set; }

        /// <summary>발판 기준 속도 (연출용 — 회전 접시 위에서 가만히 있으면 0).</summary>
        public Vector3 RelativeVelocity => _rb != null ? _rb.linearVelocity - PlatformVelocity : Vector3.zero;

        /// <summary>몸 가운데 (공격 거리 계산용).</summary>
        public Vector3 Center => (_rb != null ? _rb.position : transform.position) + Vector3.up * (Height * 0.5f);

        public float FacingDeg => _facingDeg;

        private void Awake()
        {
            Id = s_nextId++;
            gameObject.layer = CharacterLayer;

            _rb = GetComponent<Rigidbody>();
            if (_rb == null)
            {
                _rb = gameObject.AddComponent<Rigidbody>();
            }
            _rb.mass = 1f;
            _rb.linearDamping = 0f;
            _rb.angularDamping = 0.05f;
            _rb.useGravity = true;
            _rb.isKinematic = false;
            _rb.interpolation = RigidbodyInterpolation.Interpolate;
            _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            _rb.constraints = RigidbodyConstraints.FreezeRotation;

            _col = GetComponent<CapsuleCollider>();
            if (_col == null)
            {
                _col = gameObject.AddComponent<CapsuleCollider>();
            }
            _col.direction = 1; // Y축
            _col.radius = Radius;
            _col.height = Mathf.Max(Height, Radius * 2f);
            _col.center = new Vector3(0f, _col.height * 0.5f, 0f);
            _col.sharedMaterial = SlipMaterial();

            _facingDeg = transform.eulerAngles.y;
        }

        private void OnEnable()
        {
            if (!All.Contains(this))
            {
                All.Add(this);
            }
        }

        private void OnDisable()
        {
            All.Remove(this);
        }

        private static PhysicsMaterial s_slip;

        /// <summary>벽에 달라붙지 않게 마찰 0. 속도는 매 틱 코드가 정하니 미끄러지지 않아요.</summary>
        private static PhysicsMaterial SlipMaterial()
        {
            if (s_slip == null)
            {
                s_slip = new PhysicsMaterial("SushiSlip")
                {
                    dynamicFriction = 0f,
                    staticFriction = 0f,
                    bounciness = 0f,
                    frictionCombine = PhysicsMaterialCombine.Minimum,
                    bounceCombine = PhysicsMaterialCombine.Minimum,
                };
            }
            return s_slip;
        }

        private void FixedUpdate()
        {
            if (IsDespawned || _rb == null)
            {
                return;
            }
            float dt = Time.fixedDeltaTime;
            float now = Time.fixedTime;

            if (!Mathf.Approximately(GravityScale, 1f))
            {
                _rb.AddForce(Physics.gravity * (GravityScale - 1f), ForceMode.Acceleration);
            }

            float platformYawRate;
            Vector3 platformVel;
            IsGrounded = ProbeGround(now, out platformVel, out platformYawRate);
            PlatformVelocity = platformVel;

            Vector3 v = _rb.linearVelocity;
            Vector3 rel = v - platformVel;

            var input = new MoveInput();
            if (Input != null)
            {
                Vector2 m = Input.MoveWorld;
                input.MoveX = m.x;
                input.MoveZ = m.y;
                input.JumpPressed = Input.ConsumeJump();
                input.DivePressed = Input.ConsumeDive();
            }

            var ctx = new MotorContext
            {
                Time = now,
                Dt = dt,
                Grounded = IsGrounded,
                VelX = rel.x,
                VelY = rel.y,
                VelZ = rel.z,
                FacingDeg = _facingDeg,
            };
            MoveCommand cmd = MoveLogic.Step(_motor, input, ctx, Tuning);

            float velY = cmd.SetVelY.HasValue ? cmd.SetVelY.Value + platformVel.y : v.y;
            _rb.linearVelocity = new Vector3(cmd.VelX + platformVel.x, velY, cmd.VelZ + platformVel.z);

            // 회전 접시 위에서는 바라보는 방향도 같이 돌아요
            _facingDeg = Mathf.Repeat(cmd.FacingDeg + platformYawRate * dt, 360f);
            _rb.MoveRotation(Quaternion.Euler(0f, _facingDeg, 0f));

            if (cmd.Landed)
            {
                Landed?.Invoke(cmd.LandImpactSpeed);
            }
            if (cmd.Jumped)
            {
                _groundIgnoreUntil = now + 0.12f; // 뛰자마자 다음 틱에 다시 "땅"으로 보지 않게
                Jumped?.Invoke();
            }
            if (cmd.Dived)
            {
                _groundIgnoreUntil = now + 0.12f;
                Dived?.Invoke();
            }
            if (cmd.GotUp)
            {
                GotUp?.Invoke();
            }
        }

        private bool ProbeGround(float now, out Vector3 platformVel, out float platformYawRate)
        {
            platformVel = Vector3.zero;
            platformYawRate = 0f;
            if (now < _groundIgnoreUntil)
            {
                return false;
            }
            float castRadius = Radius * 0.9f;
            Vector3 origin = _rb.position + Vector3.up * (castRadius + 0.05f);
            float distance = 0.05f + GroundProbe;
            RaycastHit hit;
            if (!Physics.SphereCast(origin, castRadius, Vector3.down, out hit, distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                return false;
            }
            if (hit.normal.y <= MinGroundNormalY)
            {
                return false;
            }

            if (hit.collider != _lastGroundCollider)
            {
                _lastGroundCollider = hit.collider;
                _lastPlatform = hit.collider != null ? hit.collider.GetComponentInParent<IPlatformMotion>() : null;
            }
            Vector3 feet = new Vector3(_rb.position.x, hit.point.y, _rb.position.z);
            if (_lastPlatform != null)
            {
                platformVel = _lastPlatform.GetPointVelocity(feet);
                platformYawRate = _lastPlatform.YawRateDeg;
            }
            else if (hit.rigidbody != null && hit.rigidbody.isKinematic)
            {
                platformVel = hit.rigidbody.GetPointVelocity(feet);
            }
            platformVel.y = 0f; // 수평으로만 실어 날라요 (세로는 물리가)
            return true;
        }

        /// <summary>맞아서 날아감 + 넘어짐. velocity = 새 속도(월드), stun = 넘어져 있는 시간(초).</summary>
        public void Knock(Vector3 velocity, float stun)
        {
            if (IsDespawned || _rb == null)
            {
                return;
            }
            float now = Time.fixedTime;
            MoveLogic.KnockDown(_motor, now, Tuning);
            _motor.ModeUntil = now + Mathf.Max(0.1f, stun);
            _groundIgnoreUntil = now + 0.15f; // 바로 땅 마찰로 멈추지 않게
            IsGrounded = false;
            _rb.linearVelocity = velocity;
            KnockedDown?.Invoke(velocity);
        }

        /// <summary>안 보이고 멈춤 (떨어져서 부활 기다리는 동안).</summary>
        public void Despawn()
        {
            if (IsDespawned)
            {
                return;
            }
            IsDespawned = true;
            IsGrounded = false;
            if (_rb != null)
            {
                _rb.linearVelocity = Vector3.zero; // 키네마틱으로 바꾸기 전에 (키네마틱에 속도 쓰면 경고)
                _rb.angularVelocity = Vector3.zero;
                _rb.isKinematic = true;
            }
            if (_col != null)
            {
                _col.enabled = false;
            }
            if (Visual != null)
            {
                Visual.gameObject.SetActive(false);
            }
            Despawned?.Invoke();
        }

        /// <summary>pos(발바닥)에 다시 세워요. yawDeg가 NaN이면 지금 방향 유지.</summary>
        public void Respawn(Vector3 pos, float yawDeg = float.NaN)
        {
            if (_rb == null)
            {
                return;
            }
            if (!float.IsNaN(yawDeg))
            {
                _facingDeg = Mathf.Repeat(yawDeg, 360f);
            }
            Quaternion rot = Quaternion.Euler(0f, _facingDeg, 0f);
            IsDespawned = false;
            _rb.isKinematic = false;
            if (_col != null)
            {
                _col.enabled = true;
            }
            transform.SetPositionAndRotation(pos, rot);
            _rb.position = pos;
            _rb.rotation = rot;
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
            _motor = new MotorState();
            _groundIgnoreUntil = 0f;
            _lastGroundCollider = null;
            _lastPlatform = null;
            IsGrounded = false;
            PlatformVelocity = Vector3.zero;
            if (Input != null)
            {
                Input.ClearPresses();
            }
            if (Visual != null)
            {
                Visual.gameObject.SetActive(true);
            }
            Respawned?.Invoke();
        }
    }
}
