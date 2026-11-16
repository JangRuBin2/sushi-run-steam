// 결승선 트리거.
using UnityEngine;

namespace SushiRun.Runtime
{
    [DisallowMultipleComponent]
    public sealed class FinishLine : MonoBehaviour
    {
        public PrototypeLevel Level;

        private void OnTriggerEnter(Collider other)
        {
            SushiCharacter ch = other.GetComponentInParent<SushiCharacter>();
            if (ch != null && Level != null)
            {
                Level.ReachFinish(ch);
            }
        }
    }
}
