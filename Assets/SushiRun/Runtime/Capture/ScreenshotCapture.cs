// F12 스크린샷 — 플레이를 시작하면 어느 씬에서나 자동으로 붙어요.
// 에디터: 프로젝트 폴더의 Screenshots/, 빌드: persistentDataPath/Screenshots/ 에 PNG로 저장.
using System;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SushiRun.Runtime
{
    public sealed class ScreenshotCapture : MonoBehaviour
    {
        [Tooltip("해상도 배율 (1 = 화면 그대로, 2 = 가로세로 2배)")]
        [Range(1, 4)]
        public int SuperSize = 1;

        private const float NoticeSeconds = 2.5f;

        private static ScreenshotCapture _instance;

        private string _notice;
        private float _noticeUntil;
        private int _captureFrame = -1;
        private GUIStyle _style;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoCreate()
        {
            if (_instance != null)
            {
                return;
            }
            var go = new GameObject("ScreenshotCapture");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<ScreenshotCapture>();
        }

        public static string Folder
        {
            get
            {
#if UNITY_EDITOR
                // Assets/ 옆 (프로젝트 폴더) — .gitignore 대상
                return Path.Combine(Path.GetDirectoryName(Application.dataPath), "Screenshots");
#else
                return Path.Combine(Application.persistentDataPath, "Screenshots");
#endif
            }
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        private void Update()
        {
            Keyboard kb = Keyboard.current;
            if (kb != null && kb.f12Key.wasPressedThisFrame)
            {
                Capture();
            }
        }

        /// <summary>지금 화면을 PNG로 저장하고 경로를 돌려줘요 (파일은 이번 프레임 끝에 써져요).</summary>
        public string Capture()
        {
            string folder = Folder;
            Directory.CreateDirectory(folder);
            string file = "SushiRun_" + DateTime.Now.ToString("yyyyMMdd_HHmmss_fff") + ".png";
            string path = Path.Combine(folder, file);
            ScreenCapture.CaptureScreenshot(path, Mathf.Clamp(SuperSize, 1, 4));
            Debug.Log("[SushiRun] 스크린샷 저장: " + path);
            _notice = "스크린샷 저장: " + file;
            // 알림 글씨가 사진에 찍히지 않게 다음 프레임부터 보여요 (사진은 이번 프레임 끝에 찍혀요)
            _captureFrame = Time.frameCount;
            _noticeUntil = Time.unscaledTime + NoticeSeconds;
            return path;
        }

        private void OnGUI()
        {
            if (_notice == null || Time.frameCount <= _captureFrame || Time.unscaledTime > _noticeUntil)
            {
                return;
            }
            if (_style == null)
            {
                _style = new GUIStyle(GUI.skin.box) { fontSize = 15, alignment = TextAnchor.MiddleCenter };
                _style.normal.textColor = Color.white;
            }
            float scale = Mathf.Max(1f, Screen.height / 900f);
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1f));
            float w = Screen.width / scale;
            float h = Screen.height / scale;
            GUI.Label(new Rect((w - 380f) * 0.5f, h - 60f, 380f, 32f), _notice, _style);
        }
    }
}
