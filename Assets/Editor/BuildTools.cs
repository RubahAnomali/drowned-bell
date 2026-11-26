using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using DrownedBell.Game;

/// <summary>Menu items and batch-mode entry points: create the main scene and build the Windows player.</summary>
public static class BuildTools
{
    const string ScenePath = "Assets/Scenes/Main.unity";
    const string ExePath = "Builds/Windows/DrownedBell.exe";

    [MenuItem("Drowned Bell/Create Main Scene")]
    public static void CreateScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 9f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.02f, 0.04f, 0.06f);
        camGo.AddComponent<AudioListener>();
        camGo.transform.position = new Vector3(16f, 45f, -10f);
        new GameObject("Drowned Bell").AddComponent<GameController>();
        Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        Debug.Log("Drowned Bell: main scene created at " + ScenePath);
    }

    [MenuItem("Drowned Bell/Build Windows")]
    public static void BuildWindows()
    {
        if (!File.Exists(ScenePath)) CreateScene();
        PlayerSettings.productName = "Drowned Bell";
        PlayerSettings.companyName = "RubahAnomali";
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        PlayerSettings.defaultScreenWidth = 1600;
        PlayerSettings.defaultScreenHeight = 900;
        PlayerSettings.resizableWindow = true;
        PlayerSettings.runInBackground = false;
        var opts = new BuildPlayerOptions
        {
            scenes = new[] { ScenePath },
            locationPathName = ExePath,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None,
        };
        BuildReport report = BuildPipeline.BuildPlayer(opts);
        Debug.Log("DROWNED BELL BUILD: " + report.summary.result + ", " + report.summary.totalErrors + " errors, " + (report.summary.totalSize / (1024 * 1024)) + " MB at " + ExePath);
        if (Application.isBatchMode) EditorApplication.Exit(report.summary.result == BuildResult.Succeeded ? 0 : 1);
    }

    /// <summary>
    /// Browser build for the portfolio site. Files are left uncompressed: static hosts (Vercel) compress them
    /// on the fly, while Unity's own .br/.gz files would need custom Content-Encoding headers to load.
    /// </summary>
    [MenuItem("Drowned Bell/Build Web")]
    public static void BuildWebGL()
    {
        const string WebPath = "Builds/WebGL";
        if (!File.Exists(ScenePath)) CreateScene();
        PlayerSettings.productName = "Drowned Bell";
        PlayerSettings.companyName = "RubahAnomali";
        PlayerSettings.WebGL.template = "PROJECT:DrownedBell";
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
        PlayerSettings.WebGL.decompressionFallback = false;
        PlayerSettings.WebGL.nameFilesAsHashes = false;
        PlayerSettings.WebGL.dataCaching = true;
        PlayerSettings.runInBackground = false;
        var opts = new BuildPlayerOptions
        {
            scenes = new[] { ScenePath },
            locationPathName = WebPath,
            target = BuildTarget.WebGL,
            options = BuildOptions.None,
        };
        BuildReport report = BuildPipeline.BuildPlayer(opts);
        Debug.Log("DROWNED BELL WEB BUILD: " + report.summary.result + ", " + report.summary.totalErrors + " errors, " + (report.summary.totalSize / (1024 * 1024)) + " MB at " + WebPath);
        if (Application.isBatchMode) EditorApplication.Exit(report.summary.result == BuildResult.Succeeded ? 0 : 1);
    }
}
