// 시제품 맵 공통: 캐릭터 등록, 낙하(Kill Y) → 3초 뒤 부활, 체크포인트, 봇 길, 화면 글씨.
using System.Collections.Generic;
using UnityEngine;

namespace SushiRun.Runtime
{
    [System.Serializable]
    public struct BotWaypoint
    {
        public Vector3 Position;
        public float Radius; // 이 안에 들어오면 다음 점으로
        public bool Jump; // 여기(가장자리)에 닿으면 점프

        public BotWaypoint(Vector3 position, float radius = 1.5f, bool jump = false)
        {
            Position = position;
            Radius = radius;
            Jump = jump;
        }
    }

    public abstract class PrototypeLevel : MonoBehaviour
    {
        public float KillY = -8f;
        public float RespawnDelay = 3f;

        /// <summary>봇이 따라갈 길 (비어 있으면 WanderCenter 둘레를 돌아다님).</summary>
        public readonly List<BotWaypoint> BotPath = new List<BotWaypoint>();

        /// <summary>true면 봇이 길 끝 낭떠러지에서 점프 (Race), false면 가장자리를 피함 (Final).</summary>
        public bool BotEdgeJumps = true;

        public Vector3 WanderCenter;
        public float WanderRadius = 5f;

        /// <summary>true인 동안 봇이 공격하지 않아요 (판 시작 직후 등).</summary>
        public virtual bool BotAttacksPaused => false;

        /// <summary>한 판 흐름의 라운드인가요. 켜면: 레이스는 완주하면 빠지고(다시 안 달림), 생존·결승은 떨어지면 탈락(부활 없음).</summary>
        public bool MatchMode;

        /// <summary>한 판 흐름: 완주(레이스) — 통과 순서대로 불려요.</summary>
        public event System.Action<SushiCharacter> MatchFinished;

        /// <summary>한 판 흐름: 떨어져서 탈락 (생존·결승).</summary>
        public event System.Action<SushiCharacter> MatchEliminated;

        /// <summary>떨어지면 탈락인 맵 (생존·결승). 레이스는 false — 체크포인트에서 다시.</summary>
        public virtual bool FallEliminates => false;

        /// <summary>시간 제한 라운드의 남은 초 (생존). 없으면 -1.</summary>
        public virtual float RoundTimeLeft => -1f;

        /// <summary>아직 남아 있는(빠지지 않은) 캐릭터 수.</summary>
        public int InCount
        {
            get
            {
                int n = 0;
                for (int i = 0; i < Runners.Count; i++)
                {
                    if (Runners[i].Ch != null && !Runners[i].Out)
                    {
                        n++;
                    }
                }
                return n;
            }
        }

        /// <summary>아직 남아 있는(빠지지 않은) 캐릭터들.</summary>
        public List<SushiCharacter> InCharacters()
        {
            var list = new List<SushiCharacter>();
            for (int i = 0; i < Runners.Count; i++)
            {
                if (Runners[i].Ch != null && !Runners[i].Out)
                {
                    list.Add(Runners[i].Ch);
                }
            }
            return list;
        }

        /// <summary>레이스 ReachFinish에서: 한 판 흐름이면 이 캐릭터를 빼고 알려요 (true면 처리 끝).</summary>
        protected bool MatchFinish(Runner r)
        {
            if (!MatchMode || r == null || r.Out)
            {
                return MatchMode;
            }
            r.Out = true;
            r.Finished = true;
            r.FinishTime = Time.time - r.StartTime;
            r.Ch.Despawn();
            MatchFinished?.Invoke(r.Ch);
            return true;
        }

        /// <summary>사람이 조종하는 캐릭터 (화면 글씨·대기 시간 기준).</summary>
        public SushiCharacter Player { get; set; }

        protected sealed class Runner
        {
            public SushiCharacter Ch;
            public int SpawnIndex;
            public Vector3 RespawnPos;
            public float RespawnYaw;
            public int Checkpoint = -1;
            public float RespawnAt = -1f; // >= 0이면 부활 대기 중
            public bool Fell;
            public float StartTime;
            public bool Finished;
            public float FinishTime;
            public float Best = -1f;
            public bool Out; // 한 판 흐름: 완주해서 빠졌거나 탈락 (더는 부활 안 함)
        }

        protected readonly List<Runner> Runners = new List<Runner>();

        public abstract string Title { get; }

        /// <summary>판정 지오메트리 + 장식을 이 오브젝트 아래에 만들어요.</summary>
        public abstract void Build();

        public abstract Vector3 SpawnPoint(int index, out float yaw);

        public void Register(SushiCharacter ch, int spawnIndex)
        {
            if (ch == null || Find(ch) != null)
            {
                return;
            }
            float yaw;
            Vector3 pos = SpawnPoint(spawnIndex, out yaw);
            var r = new Runner { Ch = ch, SpawnIndex = spawnIndex, RespawnPos = pos, RespawnYaw = yaw, StartTime = Time.time };
            Runners.Add(r);
            ch.Respawn(pos, yaw);
            OnRegistered(r);
        }

        protected Runner Find(SushiCharacter ch)
        {
            for (int i = 0; i < Runners.Count; i++)
            {
                if (Runners[i].Ch == ch)
                {
                    return Runners[i];
                }
            }
            return null;
        }

        protected virtual void Update()
        {
            float now = Time.time;
            for (int i = 0; i < Runners.Count; i++)
            {
                Runner r = Runners[i];
                if (r.Ch == null || r.Out)
                {
                    continue;
                }
                if (r.RespawnAt < 0f)
                {
                    if (!r.Ch.IsDespawned && r.Ch.transform.position.y < KillY)
                    {
                        r.Ch.Despawn();
                        r.Fell = true;
                        if (MatchMode && FallEliminates)
                        {
                            r.Out = true; // 한 판 흐름: 탈락 (부활 없음)
                            OnFell(r);
                            MatchEliminated?.Invoke(r.Ch);
                            continue;
                        }
                        r.RespawnAt = now + RespawnDelay;
                        OnFell(r);
                    }
                }
                else if (now >= r.RespawnAt)
                {
                    r.RespawnAt = -1f;
                    r.Fell = false;
                    Vector2 jitter = Random.insideUnitCircle * 0.9f;
                    r.Ch.Respawn(r.RespawnPos + new Vector3(jitter.x, 0.05f, jitter.y), r.RespawnYaw);
                    OnRespawned(r);
                }
            }
        }

        /// <summary>delay초 뒤 RespawnPos로 (hide면 그동안 안 보임).</summary>
        protected void ScheduleRespawn(Runner r, float delay, bool hide)
        {
            if (hide)
            {
                r.Ch.Despawn();
            }
            r.RespawnAt = Time.time + Mathf.Max(0f, delay);
        }

        /// <summary>마지막 체크포인트로 바로 (R 키).</summary>
        public void ForceRespawn(SushiCharacter ch)
        {
            Runner r = Find(ch);
            if (r == null || r.RespawnAt >= 0f || r.Out)
            {
                return;
            }
            ScheduleRespawn(r, 0f, true);
        }

        public void ReachCheckpoint(SushiCharacter ch, Checkpoint cp)
        {
            Runner r = Find(ch);
            if (r == null || ch.IsDespawned || cp == null)
            {
                return;
            }
            if (cp.Index > r.Checkpoint)
            {
                r.Checkpoint = cp.Index;
                r.RespawnPos = cp.RespawnPoint;
                r.RespawnYaw = cp.RespawnYaw;
                OnCheckpoint(r);
            }
        }

        public virtual void ReachFinish(SushiCharacter ch)
        {
        }

        /// <summary>부활까지 남은 초 (대기 중이 아니면 -1).</summary>
        public float RespawnCountdown(SushiCharacter ch)
        {
            Runner r = Find(ch);
            if (r == null || r.RespawnAt < 0f)
            {
                return -1f;
            }
            return Mathf.Max(0f, r.RespawnAt - Time.time);
        }

        /// <summary>왼쪽 위 상태 줄 (플레이어 기준).</summary>
        public virtual string StatusText(SushiCharacter player)
        {
            return "";
        }

        /// <summary>화면 가운데 큰 글씨 (없으면 빈 문자열).</summary>
        public virtual string CenterText(SushiCharacter player)
        {
            float c = RespawnCountdown(player);
            if (c >= 0f)
            {
                return "떨어졌어요!\n" + Mathf.CeilToInt(c);
            }
            return "";
        }

        protected virtual void OnRegistered(Runner r)
        {
        }

        protected virtual void OnFell(Runner r)
        {
        }

        protected virtual void OnRespawned(Runner r)
        {
        }

        protected virtual void OnCheckpoint(Runner r)
        {
        }
    }
}
