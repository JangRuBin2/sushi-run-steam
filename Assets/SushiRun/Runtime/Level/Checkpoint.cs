// 체크포인트 트리거 — 지나가면 그 캐릭터의 부활 위치가 여기로 (번호가 큰 쪽만).
using UnityEngine;

namespace SushiRun.Runtime
{
    [DisallowMultipleComponent]
    public sealed class Checkpoint : MonoBehaviour
    {
        public int Index;
        public Vector3 RespawnPoint;
        public float RespawnYaw;
        public PrototypeLevel Level;

        private void OnTriggerEnter(Collider other)
        {
            SushiCharacter ch = other.GetComponentInParent<SushiCharacter>();
            if (ch != null && Level != null)
            {
                Level.ReachCheckpoint(ch, this);
            }
        }
    }
}
