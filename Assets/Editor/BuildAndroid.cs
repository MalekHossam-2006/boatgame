using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Builds an Android APK into Builds/Android/BoatGame.apk (git-ignored), using the scenes
/// in the build settings. Landscape only, IL2CPP + ARM64 (required by modern phones).
/// Menu: Tools/Boat Game/Build Android APK
/// Batch mode: Unity -batchmode -quit -buildTarget Android -executeMethod BuildAndroid.Build
/// </summary>
public static class BuildAndroid
{
    const string OutputDir = "Builds/Android";

    [MenuItem("Tools/Boat Game/Build Android APK")]
    public static string Build()
    {
        ApplySettings();
        var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
        Directory.CreateDirectory(OutputDir);

        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = OutputDir + "/BoatGame.apk",
            target = BuildTarget.Android,
            options = BuildOptions.None,
        });
        var s = report.summary;
        string result = $"{s.result}: {s.totalErrors} errors, {s.totalSize / (1024 * 1024)} MB, {s.totalTime.TotalSeconds:0}s";
        Debug.Log("[BuildAndroid] " + result);
        if (Application.isBatchMode && s.result != BuildResult.Succeeded) EditorApplication.Exit(1);
        return result;
    }

    /// <summary>Android player settings for the game (safe to run more than once).</summary>
    public static void ApplySettings()
    {
        PlayerSettings.productName = "Boat Game";
        PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android, "com.boatgame.midterm");
        PlayerSettings.bundleVersion = "0.1";
        PlayerSettings.Android.bundleVersionCode = 1;

        // landscape only (left or right)
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
        PlayerSettings.allowedAutorotateToLandscapeLeft = true;
        PlayerSettings.allowedAutorotateToLandscapeRight = true;
        PlayerSettings.allowedAutorotateToPortrait = false;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;

        PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        EditorUserBuildSettings.buildAppBundle = false;   // a plain .apk that can be installed directly
    }
}
