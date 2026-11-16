// 한 판의 규칙 계산 — 로블록스판 src/shared/Rules.luau의 C# 이식 (같은 입력 → 같은 답, 테스트로 고정).
// 판 계획(맵 고르기)은 2단계(네트워크 시제품)에서 이식해요.
using System;

namespace SushiRun.Core.Rules
{
    [Serializable] // Unity 인스펙터에서 바로 고칠 수 있게 (System 특성이라 엔진 참조 없음)
    public sealed class RoundRulesConfig
    {
        public int FourRoundMinPlayers = 9; // 시작 인원이 이 이상이면 4라운드
        public int MinQualifiers = 2; // 결승 전 라운드는 최소 이만큼 통과
        public int SkipToFinalAt = 2; // 결승 전 남은 인원이 이하이면 바로 결승
        public double[] ThreeRoundRatios = { 0.60, 0.50 };
        public double[] FourRoundRatios = { 0.65, 0.55, 0.50 };
        public int LargeMinPlayers = 25; // 시작 인원이 이 이상인 4라운드 판은 대형 비율
        public double[] LargeRatios = { 0.60, 0.45, 0.40 };

        public static readonly RoundRulesConfig Default = new RoundRulesConfig();
    }

    public static class RoundRules
    {
        public static int RoundCount(int startCount, RoundRulesConfig config = null)
        {
            config ??= RoundRulesConfig.Default;
            return startCount >= config.FourRoundMinPlayers ? 4 : 3;
        }

        public static double[] PassRatios(int roundCount, int? startCount = null, RoundRulesConfig config = null)
        {
            config ??= RoundRulesConfig.Default;
            if (roundCount == 4 && startCount.HasValue && startCount.Value >= config.LargeMinPlayers)
            {
                return config.LargeRatios;
            }
            if (roundCount == 3)
            {
                return config.ThreeRoundRatios;
            }
            if (roundCount == 4)
            {
                return config.FourRoundRatios;
            }
            return null;
        }

        /// <summary>
        /// roundIndex(1부터) 라운드의 통과 인원. 통과 = clamp(round(남은 × 비율), 2, 남은 - 1), 결승은 1명.
        /// </summary>
        public static int QualifyCount(int aliveCount, int roundCount, int roundIndex, int? startCount = null, RoundRulesConfig config = null)
        {
            config ??= RoundRulesConfig.Default;
            if (roundIndex >= roundCount)
            {
                return 1;
            }
            double[] ratios = PassRatios(roundCount, startCount, config);
            if (ratios == null || roundIndex < 1 || roundIndex > ratios.Length)
            {
                throw new ArgumentException($"no pass ratio for round {roundIndex} of {roundCount}");
            }
            int minimum = config.MinQualifiers;
            int target = CoreMath.RoundHalfAway(aliveCount * ratios[roundIndex - 1]);
            return CoreMath.Clamp(target, minimum, Math.Max(aliveCount - 1, minimum));
        }

        public static bool ShouldSkipToFinal(int aliveCount, RoundRulesConfig config = null)
        {
            config ??= RoundRulesConfig.Default;
            return aliveCount <= config.SkipToFinalAt;
        }

        /// <summary>방금 끝난 라운드 다음 번호. 결승이 끝났으면 null.</summary>
        public static int? NextRound(int finishedIndex, int roundCount, int aliveCount, RoundRulesConfig config = null)
        {
            if (finishedIndex >= roundCount)
            {
                return null;
            }
            if (ShouldSkipToFinal(aliveCount, config))
            {
                return roundCount;
            }
            return finishedIndex + 1;
        }

        public static int RoundToPlay(int plannedIndex, int roundCount, int aliveCount, RoundRulesConfig config = null)
        {
            if (plannedIndex < roundCount && ShouldSkipToFinal(aliveCount, config))
            {
                return roundCount;
            }
            return plannedIndex;
        }

        /// <summary>이번 라운드 eliminatedCount번째 탈락자의 매치 등수 (먼저 탈락할수록 큰 등수).</summary>
        public static int EliminationPlace(int totalRacers, int eliminatedCount)
        {
            return totalRacers - eliminatedCount + 1;
        }
    }
}
