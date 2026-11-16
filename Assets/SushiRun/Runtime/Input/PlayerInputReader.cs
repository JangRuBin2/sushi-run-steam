// 사람 입력 — Input System 장치를 직접 읽어요 (액션 에셋 없이, 시제품용).
// 키보드: WASD/방향키 이동, Space 점프, 왼쪽 Shift·E 다이브, F 공격, 마우스 시점(커서 잠금일 때만)
// 패드: 왼쪽 스틱 이동, South(A) 점프, West(X) 다이브, LT·North(Y) 공격, 오른쪽 스틱 시점
using UnityEngine;
using UnityEngine.InputSystem;

namespace SushiRun.Runtime
{
    public sealed class PlayerInputReader : MonoBehaviour, ISushiInput
    {
        [Tooltip("이동 방향 기준이 되는 카메라 (없으면 월드 기준)")]
        public FollowCamera View;

        [Tooltip("마우스 1픽셀당 회전 (도)")]
        public float MouseSensitivity = 0.12f;

        [Tooltip("오른쪽 스틱 끝까지 밀었을 때 초당 회전 (도)")]
        public float StickLookSpeed = 170f;

        public float StickDeadZone = 0.15f;

        public bool InputEnabled = true;

        private Vector2 _moveWorld;
        private Vector2 _lookAccum;
        private bool _jump;
        private bool _dive;
        private bool _attack;

        public Vector2 MoveWorld => _moveWorld;

        /// <summary>지난 읽기 뒤로 쌓인 시점 회전 (도). x = 좌우(yaw), y = 위아래 (위로 밀면 +).</summary>
        public Vector2 ConsumeLook()
        {
            Vector2 look = _lookAccum;
            _lookAccum = Vector2.zero;
            return look;
        }

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
            bool v = _attack;
            _attack = false;
            return v;
        }

        public void ClearPresses()
        {
            _jump = false;
            _dive = false;
            _attack = false;
        }

        private void Update()
        {
            if (!InputEnabled)
            {
                _moveWorld = Vector2.zero;
                return;
            }

            Vector2 move = Vector2.zero;
            Keyboard kb = Keyboard.current;
            Mouse mouse = Mouse.current;
            Gamepad pad = Gamepad.current;

            if (kb != null)
            {
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed)
                {
                    move.y += 1f;
                }
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed)
                {
                    move.y -= 1f;
                }
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed)
                {
                    move.x += 1f;
                }
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed)
                {
                    move.x -= 1f;
                }
                if (kb.spaceKey.wasPressedThisFrame)
                {
                    _jump = true;
                }
                if (kb.leftShiftKey.wasPressedThisFrame || kb.eKey.wasPressedThisFrame)
                {
                    _dive = true;
                }
                if (kb.fKey.wasPressedThisFrame)
                {
                    _attack = true;
                }
            }

            if (pad != null)
            {
                Vector2 stick = pad.leftStick.ReadValue();
                if (stick.magnitude > StickDeadZone && stick.sqrMagnitude > move.sqrMagnitude)
                {
                    move = stick;
                }
                if (pad.buttonSouth.wasPressedThisFrame)
                {
                    _jump = true;
                }
                if (pad.buttonWest.wasPressedThisFrame)
                {
                    _dive = true;
                }
                if (pad.leftTrigger.wasPressedThisFrame || pad.buttonNorth.wasPressedThisFrame)
                {
                    _attack = true;
                }
                Vector2 look = pad.rightStick.ReadValue();
                if (look.magnitude > StickDeadZone)
                {
                    _lookAccum += look * (StickLookSpeed * Time.unscaledDeltaTime);
                }
            }

            if (mouse != null && Cursor.lockState == CursorLockMode.Locked)
            {
                _lookAccum += mouse.delta.ReadValue() * MouseSensitivity;
            }

            if (move.sqrMagnitude > 1f)
            {
                move.Normalize();
            }

            // 카메라가 보는 방향 기준 → 월드
            float yaw = View != null ? View.Yaw : 0f;
            Vector3 world = Quaternion.Euler(0f, yaw, 0f) * new Vector3(move.x, 0f, move.y);
            _moveWorld = new Vector2(world.x, world.z);
        }
    }
}
