// 에디터 메뉴 — 씬·에셋을 YAML 손작업 없이 코드로 만들어요.
//   SushiRun → Create Feel Prototype Scene : 빈 씬 + FeelPrototype 하나 → Assets/SushiRun/Scenes/FeelPrototype.unity
//   SushiRun → Create Tuning Profile       : TuningProfile 에셋 (열린 씬의 FeelPrototype에 연결)
using SushiRun.Runtime;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SushiRun.Editor
{
    public static class FeelPrototypeMenu
    {
        private const string SceneFolder = "Assets/SushiRun/Scenes";
        private const string ScenePath = SceneFolder + "/FeelPrototype.unity";
        private const string TuningFolder = "Assets/SushiRun/Tuning";

        [MenuItem("SushiRun/Create Feel Prototype Scene")]
        public static void CreateScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null
                && !EditorUtility.DisplayDialog("SushiRun", ScenePath + " 이(가) 이미 있어요. 새로 만들어 덮어쓸까요?", "덮어쓰기", "취소"))
            {
                return;
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var go = new GameObject("FeelPrototype");
            var proto = go.AddComponent<FeelPrototype>();

            // 이미 만든 튜닝 프로필이 있으면 첫 번째 것을 연결
            string[] guids = AssetDatabase.FindAssets("t:" + typeof(TuningProfile).Name);
            if (guids.Length > 0)
            {
                proto.Profile = AssetDatabase.LoadAssetAtPath<TuningProfile>(AssetDatabase.GUIDToAssetPath(guids[0]));
            }

            EnsureFolder(SceneFolder);
            if (EditorSceneManager.SaveScene(scene, ScenePath))
            {
                AddToBuildSettings(ScenePath);
                Debug.Log("[SushiRun] 씬을 만들었어요: " + ScenePath + " — Play를 누르세요.");
            }
            else
            {
                Debug.LogError("[SushiRun] 씬 저장 실패: " + ScenePath);
            }
        }

        [MenuItem("SushiRun/Create Tuning Profile")]
        public static void CreateTuningProfile()
        {
            EnsureFolder(TuningFolder);
            string path = AssetDatabase.GenerateUniqueAssetPath(TuningFolder + "/TuningProfile.asset");
            var profile = ScriptableObject.CreateInstance<TuningProfile>();
            AssetDatabase.CreateAsset(profile, path);
            AssetDatabase.SaveAssets();

            // 열린 씬의 FeelPrototype에 연결
            var proto = Object.FindFirstObjectByType<FeelPrototype>();
            if (proto != null && !EditorApplication.isPlaying)
            {
                Undo.RecordObject(proto, "Assign Tuning Profile");
                proto.Profile = profile;
                EditorUtility.SetDirty(proto);
                EditorSceneManager.MarkSceneDirty(proto.gameObject.scene);
                if (!string.IsNullOrEmpty(proto.gameObject.scene.path))
                {
                    EditorSceneManager.SaveScene(proto.gameObject.scene); // 저장된 적 없는 씬이면 사용자가 저장
                }
            }

            Selection.activeObject = profile;
            EditorGUIUtility.PingObject(profile);
            Debug.Log("[SushiRun] 튜닝 프로필을 만들었어요: " + path + (proto != null ? " (FeelPrototype에 연결)" : ""));
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }
            int slash = path.LastIndexOf('/');
            string parent = path.Substring(0, slash);
            string name = path.Substring(slash + 1);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }

        private static void AddToBuildSettings(string scenePath)
        {
            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
            foreach (EditorBuildSettingsScene s in scenes)
            {
                if (s.path == scenePath)
                {
                    return;
                }
            }
            var list = new System.Collections.Generic.List<EditorBuildSettingsScene>(scenes);
            list.Insert(0, new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = list.ToArray();
        }
    }
}
