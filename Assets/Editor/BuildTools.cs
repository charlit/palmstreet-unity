using System;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
#if UNITY_IOS
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
#endif

namespace PalmStreet.EditorTools
{
    /// <summary>Builds en ligne de commande : Unity -batchmode -executeMethod PalmStreet.EditorTools.BuildTools.IOS -quit</summary>
    public static class BuildTools
    {
        static readonly string[] Scenes = { "Assets/Scenes/Game.unity" };
        const string Version = "1.0";
        static bool release;   // build App Store en cours (signature manuelle après le build)

        public static void IOS() => Build(BuildTarget.iOS, "Builds/iOS", BuildOptions.Development);
        public static void Android() => Build(BuildTarget.Android, "Builds/Android/PalmStreet.apk", BuildOptions.Development);

        /// <summary>
        /// Version publique pour l'App Store : pas de Development (donc rien de débloqué), numéro de build donné par
        /// la variable PALMSTREET_BUILD (doit augmenter à chaque envoi chez Apple).
        /// </summary>
        public static void IOSRelease()
        {
            PlayerSettings.iOS.buildNumber = Environment.GetEnvironmentVariable("PALMSTREET_BUILD") ?? "1";
            release = true;
            Build(BuildTarget.iOS, "Builds/iOS-release", BuildOptions.None);
        }

        static void Build(BuildTarget target, string path, BuildOptions options)
        {
            Prepare();
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = Scenes, locationPathName = path, target = target, options = options });
            if (report.summary.result != BuildResult.Succeeded) throw new Exception("Build " + target + " : " + report.summary.result);
        }

        /// <summary>Réglages communs : nom, version, icône (celle de l'app PalmStreet), iPhone seulement, portrait.</summary>
        static void Prepare()
        {
            PlayerSettings.productName = "PalmStreet";
            PlayerSettings.bundleVersion = Version;
            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Icon/AppIcon.png");
            if (icon != null) PlayerSettings.SetIcons(UnityEditor.Build.NamedBuildTarget.Unknown, new[] { icon }, IconKind.Any);
            PlayerSettings.iOS.targetDevice = iOSTargetDevice.iPhoneOnly;
            PlayerSettings.iOS.targetOSVersionString = "15.0";
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.SplashScreen.show = false;   // pas d'écran « Made with Unity » (facultatif depuis Unity 6) : l'animation PST ouvre le jeu
        }

#if UNITY_IOS
        /// <summary>Après le build iOS : pas de chiffrement à déclarer, et la capacité Game Center (classements).</summary>
        [PostProcessBuild]
        public static void AfterIOS(BuildTarget target, string path)
        {
            if (target != BuildTarget.iOS) return;
            var plistPath = path + "/Info.plist";
            var plist = new PlistDocument();
            plist.ReadFromFile(plistPath);
            plist.root.SetBoolean("ITSAppUsesNonExemptEncryption", false);
            plist.root.SetString("CFBundleDisplayName", "PalmStreet");
            plist.WriteToFile(plistPath);

            var projPath = PBXProject.GetPBXProjectPath(path);
            var proj = new PBXProject();
            proj.ReadFromFile(projPath);
            var caps = new ProjectCapabilityManager(projPath, "Unity-iPhone/PalmStreet.entitlements", null, proj.GetUnityMainTargetGuid());
            caps.AddGameCenter();
            caps.WriteToFile();
            if (release)   // version App Store : signature manuelle de l'app seule (le framework Unity est signé à l'export)
            {
                proj.ReadFromFile(projPath);
                var app = proj.GetUnityMainTargetGuid();
                proj.SetBuildProperty(app, "CODE_SIGN_STYLE", "Manual");
                proj.SetBuildProperty(app, "DEVELOPMENT_TEAM", "32AK299L44");
                proj.SetBuildProperty(app, "CODE_SIGN_IDENTITY", "Apple Distribution");
                proj.SetBuildProperty(app, "CODE_SIGN_IDENTITY[sdk=iphoneos*]", "Apple Distribution");   // Unity y met « iPhone Developer »
                proj.SetBuildProperty(app, "PROVISIONING_PROFILE_SPECIFIER", "PalmStreet App Store");
                proj.WriteToFile(projPath);
            }
        }
#endif
    }
}
