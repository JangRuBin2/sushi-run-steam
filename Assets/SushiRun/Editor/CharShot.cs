// 캐릭터 확인용 스크린샷 — 배치 모드(창 없이)로 초밥 6종을 나란히 세워 찍어요. 사용법: docs/ASSET-PIPELINE.md
//   Unity.exe -batchmode -projectPath <프로젝트> -executeMethod SushiRun.Editor.CharShot.Lineup -shotDir <폴더> -logFile <로그>
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using SushiRun.Runtime;

namespace SushiRun.Editor
{
    public static class CharShot
    {
        public static void Lineup()
        {
            string dir = "shots";
            string[] args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == "-shotDir")
                {
                    dir = args[i + 1];
                }
            }
            Directory.CreateDirectory(dir);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.shadows = LightShadows.Soft;
            RenderSettings.sun = sun;
            GreyboxKit.SetupLighting(sun);
            RenderSettings.fog = false;

            GreyboxKit.Box(null, "Floor", new Vector3(0f, -0.05f, 0f), new Vector3(20f, 0.1f, 8f), new Color(0.93f, 0.90f, 0.85f), 0.3f, false);
            SushiPalette[] pals = { SushiPalette.Tamago, SushiPalette.Salmon, SushiPalette.Tuna, SushiPalette.Mackerel, SushiPalette.Ebi, SushiPalette.Hamachi };
            for (int i = 0; i < pals.Length; i++)
            {
                var c = new GameObject("Char" + i);
                c.transform.position = new Vector3(-3.75f + i * 1.5f, 0f, 0f);
                c.transform.rotation = Quaternion.Euler(0f, 180f, 0f); // 카메라(-Z 쪽)를 보게
                Transform topping;
                SushiToyBuilder.Build(c.transform, pals[i], out topping);
            }
            Shot(new Vector3(0f, 1.6f, -6.5f), new Vector3(0f, 0.5f, 0f), 40f, Path.Combine(dir, "chars_front.png"));
            Shot(new Vector3(-1.2f, 1.3f, -2.2f), new Vector3(-2.2f, 0.55f, 0f), 35f, Path.Combine(dir, "chars_close.png"));
            Debug.Log("CHARSHOT_DONE");
            EditorApplication.Exit(0);
        }

        private static void Shot(Vector3 pos, Vector3 look, float fov, string file)
        {
            var cam = new GameObject("ShotCam").AddComponent<Camera>();
            cam.transform.position = pos;
            cam.transform.LookAt(look);
            cam.fieldOfView = fov;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.93f, 0.90f, 0.86f);
            var rt = new RenderTexture(1600, 900, 24);
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(1600, 900, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0);
            tex.Apply();
            File.WriteAllBytes(file, tex.EncodeToPNG());
            RenderTexture.active = null;
            cam.targetTexture = null;
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(tex);
            Object.DestroyImmediate(cam.gameObject);
        }
    }
}
