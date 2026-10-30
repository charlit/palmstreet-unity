using System;
using UnityEditor;
using UnityEditor.Build.Reporting;

namespace PalmStreet.EditorTools
{
    /// <summary>Builds en ligne de commande : Unity -batchmode -executeMethod PalmStreet.EditorTools.BuildTools.IOS -quit</summary>
    public static class BuildTools
    {
        static readonly string[] Scenes = { "Assets/Scenes/Game.unity" };

        public static void IOS() => Build(BuildTarget.iOS, "Builds/iOS", BuildOptions.Development);
        public static void Android() => Build(BuildTarget.Android, "Builds/Android/PalmStreet.apk", BuildOptions.Development);

        static void Build(BuildTarget target, string path, BuildOptions options)
        {
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = Scenes, locationPathName = path, target = target, options = options });
            if (report.summary.result != BuildResult.Succeeded) throw new Exception("Build " + target + " : " + report.summary.result);
        }
    }
}
