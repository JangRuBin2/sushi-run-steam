// 조작감 튜닝 묶음 에셋 — 실행 중 슬라이더로 맞춘 값을 저장해 두고 다음에도 써요.
// 만들기: 메뉴 SushiRun → Create Tuning Profile (또는 Project 창 Create → SushiRun → Tuning Profile).
using SushiRun.Core.Combat;
using SushiRun.Core.Motion;
using SushiRun.Core.Movement;
using UnityEngine;

namespace SushiRun.Runtime
{
    [CreateAssetMenu(menuName = "SushiRun/Tuning Profile", fileName = "TuningProfile")]
    public sealed class TuningProfile : ScriptableObject
    {
        public MoveTuning Move = new MoveTuning();
        public JellyTuning Jelly = new JellyTuning();
        public AttackConfig Attack = new AttackConfig();

        [Tooltip("중력 배율 (SushiCharacter.GravityScale)")]
        public float GravityScale = 2.2f;
    }
}
