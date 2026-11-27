// 텍스처를 흘려 보여요 (벨트가 움직이는 것처럼 — 보이기 전용, 판정은 ConveyorSurface).
using UnityEngine;

namespace SushiRun.Runtime
{
    [DisallowMultipleComponent]
    public sealed class TextureScroller : MonoBehaviour
    {
        [Tooltip("초당 UV 이동 (반복 단위)")]
        public Vector2 Speed;

        private Material _mat;
        private Vector2 _offset;

        private void Start()
        {
            var r = GetComponent<Renderer>();
            if (r != null)
            {
                _mat = r.material; // 이 물체만의 복사본
            }
        }

        private void Update()
        {
            if (_mat == null)
            {
                return;
            }
            _offset += Speed * Time.deltaTime;
            _offset.x %= 1f;
            _offset.y %= 1f;
            if (_mat.HasProperty("_BaseMap"))
            {
                _mat.SetTextureOffset("_BaseMap", _offset);
            }
        }

        private void OnDestroy()
        {
            if (_mat != null)
            {
                Destroy(_mat);
            }
        }
    }
}
