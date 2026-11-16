// 캐릭터 조작 입력 — 사람(PlayerInputReader)과 봇(WanderBot)이 같은 컨트롤러를 쓰게 해요.
using UnityEngine;

namespace SushiRun.Runtime
{
    public interface ISushiInput
    {
        /// <summary>월드 기준 이동 방향 (x = 월드 X, y = 월드 Z), 길이 0~1. 카메라 기준 변환은 입력 쪽에서 끝내요.</summary>
        Vector2 MoveWorld { get; }

        /// <summary>눌림 플래그를 읽고 지워요 (Update에서 눌린 걸 FixedUpdate가 놓치지 않게 쌓아 둠).</summary>
        bool ConsumeJump();

        bool ConsumeDive();

        bool ConsumeAttack();

        /// <summary>쌓인 눌림을 모두 버려요 (부활할 때).</summary>
        void ClearPresses();
    }
}
