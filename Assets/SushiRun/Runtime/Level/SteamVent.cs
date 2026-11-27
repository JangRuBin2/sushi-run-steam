// 증기구 분사: 가장자리 증기구가 잠깐 빨갛게 달아오른 뒤(경고) 한 줄로 뜨거운 증기를 뿜어
// 그 줄 안의 캐릭터를 뿜는 방향으로 밀어내요. 판정은 여기서 직접 (권위 쪽).
using System.Collections.Generic;
using UnityEngine;

namespace SushiRun.Runtime
{
    [DisallowMultipleComponent]
    public sealed class SteamVent : MonoBehaviour
    {
        [Tooltip("뿜는 방향 (월드, 수평)")]
        public Vector3 Direction = Vector3.left;

        [Tooltip("증기 줄 길이 (m)")]
        public float Length = 18f;

        [Tooltip("증기 줄 폭 (m)")]
        public float Width = 3f;

        [Tooltip("분사 사이 간격 (초)")]
        public float Interval = 12f;

        [Tooltip("첫 분사까지 (초) — 증기구마다 엇갈리게")]
        public float FirstDelay = 6f;

        public float Warning = 1.2f;
        public float BlastSeconds = 2f;

        public float PushHorizontal = 7f;
        public float PushUp = 2.5f;
        public float Stun = 0.3f;

        [Tooltip("같은 캐릭터를 다시 밀 때까지 (초)")]
        public float RepeatDelay = 0.5f;

        public Renderer Nozzle;
        public Material SteamMat;

        public bool Blowing => _blowUntil > Time.time;

        private float _next;
        private float _blowUntil = -1f;
        private Color _nozzleBase;
        private GameObject _steam;
        private readonly Dictionary<SushiCharacter, float> _last = new Dictionary<SushiCharacter, float>();

        private void Start()
        {
            ResetCycle();
            if (Nozzle != null && Nozzle.sharedMaterial != null && Nozzle.sharedMaterial.HasProperty("_BaseColor"))
            {
                Nozzle.material = new Material(Nozzle.sharedMaterial); // 달아오르는 색은 이 증기구만
                _nozzleBase = Nozzle.material.GetColor("_BaseColor");
            }
        }

        public void ResetCycle()
        {
            _next = Time.time + FirstDelay;
            _blowUntil = -1f;
            if (_steam != null)
            {
                _steam.SetActive(false);
            }
        }

        private void Update()
        {
            float now = Time.time;
            Vector3 dir = Direction;
            dir.y = 0f;
            dir.Normalize();

            // 경고: 분사 Warning초 전부터 빨갛게
            float warmup = Mathf.Clamp01(1f - (_next - now) / Warning);
            if (Nozzle != null && Nozzle.material != null && Nozzle.material.HasProperty("_BaseColor"))
            {
                Color hot = new Color(1f, 0.3f, 0.2f);
                Nozzle.material.SetColor("_BaseColor", Blowing ? hot : Color.Lerp(_nozzleBase, hot, warmup));
            }

            if (now >= _next)
            {
                _next = now + Interval;
                _blowUntil = now + BlastSeconds;
                ShowSteam(dir, true);
            }
            if (!Blowing)
            {
                ShowSteam(dir, false);
                return;
            }
            if (_steam != null)
            {
                float wob = 1f + 0.08f * Mathf.Sin(now * 25f);
                _steam.transform.localScale = new Vector3(Width * wob, 1.8f * wob, Length);
            }
            List<SushiCharacter> all = SushiCharacter.All;
            Vector3 origin = transform.position;
            for (int i = 0; i < all.Count; i++)
            {
                SushiCharacter ch = all[i];
                if (ch == null || ch.IsDespawned || ch.IsDown)
                {
                    continue;
                }
                Vector3 v = ch.transform.position - origin;
                if (v.y < -1f || v.y > 3f)
                {
                    continue;
                }
                v.y = 0f;
                float along = Vector3.Dot(v, dir);
                float side = Mathf.Abs(Vector3.Dot(v, new Vector3(-dir.z, 0f, dir.x)));
                if (along < 0f || along > Length || side > Width * 0.5f)
                {
                    continue;
                }
                float last;
                if (_last.TryGetValue(ch, out last) && now - last < RepeatDelay)
                {
                    continue;
                }
                _last[ch] = now;
                ch.Knock(dir * PushHorizontal + Vector3.up * PushUp, Stun);
            }
        }

        private void ShowSteam(Vector3 dir, bool on)
        {
            if (on && _steam == null && SteamMat != null)
            {
                _steam = ProtoMaterials.VisualPart(transform, "Steam", PrimitiveType.Sphere, Vector3.zero, Vector3.one, Quaternion.identity, SteamMat, false);
            }
            if (_steam == null)
            {
                return;
            }
            _steam.SetActive(on);
            if (on)
            {
                _steam.transform.position = transform.position + dir * (Length * 0.5f) + Vector3.up * 0.9f;
                _steam.transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
            }
        }
    }
}
