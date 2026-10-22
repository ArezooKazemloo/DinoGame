using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>Builds the current enabled scenes as a Windows 64-bit player.</summary>
public static class BuildWindowsPlayer
{
    [MenuItem("Dino Game/Build Windows 64-bit")]
    public static void BuildWindows()
    {
        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        string outputDirectory = Path.Combine(projectRoot, "Builds");
        Directory.CreateDirectory(outputDirectory);

        string[] scenes = EditorBuildSettings.scenes
            .Where(scene => scene.enabled)
            .Select(scene => scene.path)
            .ToArray();
        if (scenes.Length == 0)
            throw new InvalidOperationException("No enabled scenes were found in Build Profiles.");

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = Path.Combine(outputDirectory, "DinoGame.exe"),
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result != BuildResult.Succeeded)
            throw new InvalidOperationException($"Windows build failed: {report.summary.result}");

        Debug.Log($"Windows build completed: {options.locationPathName}");
    }
}
