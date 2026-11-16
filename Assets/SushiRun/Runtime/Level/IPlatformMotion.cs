// 움직이는 발판 (회전 접시·벨트). 위에 선 캐릭터는 이 속도를 더해서 같이 실려 가요.
using UnityEngine;

namespace SushiRun.Runtime
{
    public interface IPlatformMotion
    {
        /// <summary>월드 점 p에서 발판 표면 속도 (m/s).</summary>
        Vector3 GetPointVelocity(Vector3 worldPoint);

        /// <summary>발판의 Y축 회전 속도 (도/초) — 탄 캐릭터가 바라보는 방향도 같이 돌아요.</summary>
        float YawRateDeg { get; }
    }
}
