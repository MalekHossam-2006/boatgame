using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Builds a Windows player into Builds/BoatGame/ (git-ignored), using the scenes in the build settings.
/// Menu: Tools/Boat Game/Build Windows
/// </summary>
public static class BuildGame
{
    const string OutputDir = "Builds/BoatGame";

    [MenuItem("Tools/Boat Game/Build Windows")]
    public static string BuildWindows()
    {
        var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
        if (Directory.Exists(OutputDir)) Directory.Delete(OutputDir, true);

        PlayerSettings.productName = "Boat Game";
        PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;

        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = OutputDir + "/BoatGame.exe",
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None,
        });
        var s = report.summary;
        return $"{s.result}: {s.totalErrors} errors, {s.totalSize / (1024 * 1024)} MB, {s.totalTime.TotalSeconds:0}s, scenes: {string.Join(", ", scenes)}";
    }
}
