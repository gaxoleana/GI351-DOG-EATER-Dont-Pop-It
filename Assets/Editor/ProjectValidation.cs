using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DontPopIt.Editor
{
    /// <summary>Checks the on-disk build scenes without changing the open scene setup.</summary>
    public static class ProjectValidation
    {
        [Serializable]
        private sealed class Report
        {
            public string unityVersion;
            public int scenesChecked;
            public int prefabsChecked;
            public List<string> errors = new List<string>();
        }

        [MenuItem("Tools/Don't Pop It/Validate Project")]
        public static void Run()
        {
            Validate();
        }

        [MenuItem("Tools/Don't Pop It/Validate Project", true)]
        private static bool CanRun()
        {
            return !EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isCompiling;
        }

        // For batch mode: -executeMethod DontPopIt.Editor.ProjectValidation.RunBatch
        public static void RunBatch()
        {
            if (!Validate())
                throw new BuildFailedException("Project validation failed. See Logs/ProjectValidation.json.");
        }

        private static bool Validate()
        {
            var report = new Report { unityVersion = Application.unityVersion };
            string[] scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled)
                .Select(scene => scene.path).ToArray();

            if (scenes.Length == 0 || scenes[0] != "Assets/V.1/Scene/MainMenuV.1.unity")
                report.errors.Add("MainMenuV.1 must be the first enabled build scene.");
            if (!scenes.Contains("Assets/V.1/Scene/GameSceneV.1.unity"))
                report.errors.Add("GameSceneV.1 must be enabled in the build scene list.");

            foreach (string path in scenes)
            {
                if (!File.Exists(path))
                {
                    report.errors.Add("Missing build scene: " + path);
                    continue;
                }

                Scene preview = default;
                try
                {
                    // Preview scenes leave the developer's open and unsaved scenes untouched.
                    preview = EditorSceneManager.OpenPreviewScene(path);
                    foreach (GameObject root in preview.GetRootGameObjects())
                        CheckHierarchy(root, path, report);
                    report.scenesChecked++;
                }
                catch (Exception exception)
                {
                    report.errors.Add(path + ": " + exception.Message);
                }
                finally
                {
                    if (preview.IsValid())
                        EditorSceneManager.ClosePreviewScene(preview);
                }
            }

            if (scenes.Length > 0)
            {
                foreach (string path in AssetDatabase.GetDependencies(scenes, true)
                    .Where(path => path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase)))
                {
                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (prefab == null)
                        report.errors.Add("Cannot load prefab: " + path);
                    else
                        CheckHierarchy(prefab, path, report);
                    report.prefabsChecked++;
                }
            }

            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/ProjectValidation.json", JsonUtility.ToJson(report, true));
            foreach (string error in report.errors)
                Debug.LogError("[Project Validation] " + error);

            string summary = $"[Project Validation] {(report.errors.Count == 0 ? "PASSED" : "FAILED")}: " +
                $"{report.scenesChecked} scenes, {report.prefabsChecked} prefabs, {report.errors.Count} errors. " +
                "Report: Logs/ProjectValidation.json";
            if (report.errors.Count == 0)
                Debug.Log(summary);
            else
                Debug.LogError(summary);
            return report.errors.Count == 0;
        }

        private static void CheckHierarchy(GameObject root, string path, Report report)
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                int missing = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject);
                if (missing > 0)
                    report.errors.Add($"{path}: {child.name} has {missing} missing script(s).");
            }
        }
    }
}
