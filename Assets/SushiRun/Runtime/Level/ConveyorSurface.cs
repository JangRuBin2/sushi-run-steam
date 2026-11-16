// 컨베이어 벨트 (정지한 상자 + 표면 속도). 위에 선 캐릭터를 LocalDirection으로 실어 날라요.
// 줄무늬 자식들을 같은 속도로 흘려서 움직이는 게 보이게 해요 (보이기 전용).
using System.Collections.Generic;
using UnityEngine;

namespace SushiRun.Runtime
{
    [DisallowMultipleComponent]
    public sealed class ConveyorSurface : MonoBehaviour, IPlatformMotion
    {
        public Vector3 LocalDirection = Vector3.forward;
        public float Speed = 2.5f;

        [Tooltip("줄무늬가 흐르는 길이 (로컬 축 방향 m) — 끝에 닿으면 처음으로")]
        public float StripeSpan = 10f;

        public readonly List<Transform> Stripes = new List<Transform>();

        public Vector3 GetPointVelocity(Vector3 worldPoint)
        {
            return transform.TransformDirection(LocalDirection.normalized) * Speed;
        }

        public float YawRateDeg => 0f;

        private void Update()
        {
            if (Stripes.Count == 0 || StripeSpan <= 0f)
            {
                return;
            }
            // 부모 스케일이 1이라고 가정 (GreyboxKit이 그렇게 만듦)
            Vector3 dir = LocalDirection.normalized;
            float half = StripeSpan * 0.5f;
            for (int i = 0; i < Stripes.Count; i++)
            {
                Transform s = Stripes[i];
                if (s == null)
                {
                    continue;
                }
                Vector3 p = s.localPosition + dir * (Speed * Time.deltaTime);
                float along = Vector3.Dot(p, dir);
                if (along > half)
                {
                    p -= dir * StripeSpan;
                }
                else if (along < -half)
                {
                    p += dir * StripeSpan;
                }
                s.localPosition = p;
            }
        }
    }
}
