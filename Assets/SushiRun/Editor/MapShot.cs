// 맵 확인용 스크린샷 — Unity를 창 없이(배치 모드) 실행해 맵을 만들고 몇 장 찍어요. 사용법: docs/ASSET-PIPELINE.md
//   Unity.exe -batchmode -projectPath <프로젝트> -executeMethod SushiRun.Editor.MapShot.Sink -shotDir <폴더> -logFile <로그>
// 컴파일 오류가 있으면 로그에 error CS가 남고 사진이 안 나와요. 끝나면 로그에 MAPSHOT_DONE.
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using SushiRun.Runtime;

namespace SushiRun.Editor
{
    public static class MapShot
    {
        public static void Sink()
        {
            string dir = Arg("-shotDir", "shots");
            Directory.CreateDirectory(dir);
            SinkRaceLevel lvl = NewScene().AddComponent<SinkRaceLevel>();
            lvl.Build();
            PlaceCharacters(lvl);
            Shot(new Vector3(0f, 6f, -9f), new Vector3(0f, 0f, 22f), Path.Combine(dir, "sink_start.png"));
            Shot(new Vector3(8f, 8f, 44f), new Vector3(0f, 0.5f, 57f), Path.Combine(dir, "sink_sponges.png"));
            Shot(new Vector3(-7f, 7f, 70f), new Vector3(0f, 0f, 90f), Path.Combine(dir, "sink_cups.png"));
            Shot(new Vector3(0f, 5f, 98f), new Vector3(0f, 1f, 118f), Path.Combine(dir, "sink_finish.png"));
            Shot(new Vector3(-30f, 45f, -20f), new Vector3(0f, 0f, 60f), Path.Combine(dir, "sink_overview.png"));
            Debug.Log("MAPSHOT_DONE");
            EditorApplication.Exit(0);
        }

        public static void Space()
        {
            string dir = Arg("-shotDir", "shots");
            Directory.CreateDirectory(dir);
            SpaceRaceLevel lvl = NewScene().AddComponent<SpaceRaceLevel>();
            lvl.Build();
            PlaceCharacters(lvl);
            Shot(new Vector3(0f, 6f, -9f), new Vector3(0f, 0f, 24f), Path.Combine(dir, "space_start.png"));
            Shot(new Vector3(9f, 9f, 32f), new Vector3(0f, 0f, 55f), Path.Combine(dir, "space_rails.png"));
            Shot(new Vector3(-8f, 8f, 80f), new Vector3(0f, 0f, 100f), Path.Combine(dir, "space_meteors.png"));
            Shot(new Vector3(0f, 5f, 106f), new Vector3(0f, 1.5f, 126f), Path.Combine(dir, "space_finish.png"));
            Shot(new Vector3(-35f, 45f, -15f), new Vector3(0f, 0f, 65f), Path.Combine(dir, "space_overview.png"));
            Debug.Log("MAPSHOT_DONE");
            EditorApplication.Exit(0);
        }

        public static void Cooker()
        {
            string dir = Arg("-shotDir", "shots");
            Directory.CreateDirectory(dir);
            RiceCookerLevel lvl = NewScene().AddComponent<RiceCookerLevel>();
            lvl.Build();
            PlaceCharacters(lvl);
            Shot(new Vector3(0f, 9f, -16f), new Vector3(0f, 0f, 0f), Path.Combine(dir, "cooker_front.png"));
            Shot(new Vector3(-5f, 4f, -7f), new Vector3(3f, 0.5f, 3f), Path.Combine(dir, "cooker_close.png"));
            Shot(new Vector3(-25f, 22f, -25f), new Vector3(0f, -3f, 0f), Path.Combine(dir, "cooker_overview.png"));
            Debug.Log("MAPSHOT_DONE");
            EditorApplication.Exit(0);
        }

        public static void Garden()
        {
            string dir = Arg("-shotDir", "shots");
            Directory.CreateDirectory(dir);
            TeaGardenLevel lvl = NewScene().AddComponent<TeaGardenLevel>();
            lvl.Build();
            PlaceCharacters(lvl);
            Shot(new Vector3(0f, 10f, -20f), new Vector3(0f, 0f, 0f), Path.Combine(dir, "garden_front.png"));
            Shot(new Vector3(-6f, 4f, -8f), new Vector3(1f, 0.8f, 1f), Path.Combine(dir, "garden_close.png"));
            Shot(new Vector3(-28f, 26f, -28f), new Vector3(0f, -2f, 0f), Path.Combine(dir, "garden_overview.png"));
            Debug.Log("MAPSHOT_DONE");
            EditorApplication.Exit(0);
        }

        public static void FishMarket()
        {
            string dir = Arg("-shotDir", "shots");
            Directory.CreateDirectory(dir);
            FishMarketLevel lvl = NewScene().AddComponent<FishMarketLevel>();
            lvl.Build();
            PlaceCharacters(lvl);
            Shot(new Vector3(0f, 10f, -17f), new Vector3(0f, 0f, 0f), Path.Combine(dir, "fish_front.png"));
            Shot(new Vector3(-26f, 24f, -26f), new Vector3(0f, -2f, 0f), Path.Combine(dir, "fish_overview.png"));
            Debug.Log("MAPSHOT_DONE");
            EditorApplication.Exit(0);
        }

        public static void Onsen()
        {
            string dir = Arg("-shotDir", "shots");
            Directory.CreateDirectory(dir);
            OnsenLevel lvl = NewScene().AddComponent<OnsenLevel>();
            lvl.Build();
            PlaceCharacters(lvl);
            Shot(new Vector3(0f, 9f, -15f), new Vector3(0f, 1.5f, 0f), Path.Combine(dir, "onsen_front.png"));
            Shot(new Vector3(-22f, 20f, -22f), new Vector3(0f, 1f, 0f), Path.Combine(dir, "onsen_overview.png"));
            Debug.Log("MAPSHOT_DONE");
            EditorApplication.Exit(0);
        }

        private static string Arg(string name, string fallback)
        {
            string[] args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == name)
                {
                    return args[i + 1];
                }
            }
            return fallback;
        }

        /// <summary>빈 씬 + 해 하나, 레벨을 붙일 오브젝트를 돌려줘요.</summary>
        private static GameObject NewScene()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.shadows = LightShadows.Soft;
            RenderSettings.sun = sun;
            return new GameObject("Level");
        }

        /// <summary>크기 비교용 캐릭터 (출발점 4명).</summary>
        private static void PlaceCharacters(PrototypeLevel lvl)
        {
            SushiPalette[] pals = SushiPalette.BotPalettes;
            for (int i = 0; i < 4; i++)
            {
                var c = new GameObject("Char" + i);
                float yaw;
                c.transform.position = lvl.SpawnPoint(i, out yaw);
                Transform topping;
                SushiToyBuilder.Build(c.transform, pals[i % pals.Length], out topping);
            }
        }

        private static void Shot(Vector3 pos, Vector3 look, string file)
        {
            var cam = new GameObject("ShotCam").AddComponent<Camera>();
            cam.transform.position = pos;
            cam.transform.LookAt(look);
            cam.fieldOfView = 50f;
            cam.farClipPlane = 800f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = RenderSettings.fogColor;
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
