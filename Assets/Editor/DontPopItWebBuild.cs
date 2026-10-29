using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Profile;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DontPopIt.Editor
{
    public static class DontPopItWebBuild
    {
        public const string Template = "PROJECT:PortraitMinimal";

        [MenuItem("Tools/Don't Pop It/Build itch.io Web Release")]
        public static void BuildRelease()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play mode before building.");
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty)
                    throw new InvalidOperationException("Save open scene edits before building.");
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.WebGL, BuildTarget.WebGL))
                throw new InvalidOperationException("Install Web Build Support for this Unity Editor in Unity Hub.");

            string[] scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray();
            if (!scenes.SequenceEqual(new[]
                {
                    "Assets/V.1/Scene/MainMenuV.1.unity",
                    "Assets/V.1/Scene/GameSceneV.1.unity"
                }) || scenes.Any(scene => !File.Exists(scene)))
                throw new InvalidOperationException("Build Settings must contain MainMenuV.1 followed by GameSceneV.1.");

            // Use the same explicit release settings as Chain Reaction's web build.
            BuildProfile.SetActiveBuildProfile(null);
            Configure();

            string project = Directory.GetParent(Application.dataPath).FullName;
            string outputRoot = Environment.GetEnvironmentVariable("DONTPOPIT_WEB_OUTPUT_ROOT");
            if (string.IsNullOrWhiteSpace(outputRoot)) outputRoot = Path.Combine(project, "Builds", "ItchWeb");
            outputRoot = Path.GetFullPath(outputRoot);
            string output = Path.Combine(outputRoot, "DontPopIt-Web-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss"));
            Directory.CreateDirectory(output);
            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                target = BuildTarget.WebGL,
                locationPathName = output,
                options = BuildOptions.None
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException("Web build failed: " + report.summary.result + ". Check the build log.");
            if (!File.Exists(Path.Combine(output, "index.html")))
                throw new InvalidOperationException("The web build has no index.html.");

            string archive = output + ".zip";
            ZipFile.CreateFromDirectory(output, archive, System.IO.Compression.CompressionLevel.Optimal, false);
            var receipt = new Receipt
            {
                buildFolder = output, zip = archive, unity = Application.unityVersion,
                width = 1080, height = 1920, sceneCount = scenes.Length,
                developmentBuild = false, bytes = new FileInfo(archive).Length,
                errors = report.summary.totalErrors, warnings = report.summary.totalWarnings,
                buildSeconds = report.summary.totalTime.TotalSeconds
            };
            File.WriteAllText(Path.Combine(outputRoot, "latest.json"), JsonUtility.ToJson(receipt, true));
            Debug.Log("ITCH_WEB_BUILD_SUCCEEDED " + archive + " errors=" + receipt.errors + " scenes=" + scenes.Length);
        }

        public static void Configure()
        {
            EditorUserBuildSettings.development = false;
            EditorUserBuildSettings.allowDebugging = false;
            EditorUserBuildSettings.connectProfiler = false;
            PlayerSettings.defaultWebScreenWidth = 1080;
            PlayerSettings.defaultWebScreenHeight = 1920;
            PlayerSettings.WebGL.template = Template;
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = false;
            PlayerSettings.WebGL.dataCaching = true;
            PlayerSettings.WebGL.showDiagnostics = false;
            PlayerSettings.WebGL.debugSymbolMode = WebGLDebugSymbolMode.Off;
            PlayerSettings.WebGL.initialMemorySize = 128;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.WebGL, ScriptingImplementation.IL2CPP);
            AssetDatabase.SaveAssets();
        }

        [Serializable]
        private sealed class Receipt
        {
            public string buildFolder, zip, unity;
            public int width, height, sceneCount;
            public bool developmentBuild;
            public long bytes;
            public int errors, warnings;
            public double buildSeconds;
        }
    }
}
