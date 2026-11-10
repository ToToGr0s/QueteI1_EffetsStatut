using System;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace QueteI1.StatusEffects.EditorTools
{
    /// <summary>Réglages du projet et build. Utilisable depuis le menu ou en ligne de commande (-executeMethod).</summary>
    public static class ProjectSetup
    {
        public const string BuildPath = "Builds/Windows/QueteI1_EffetsStatut.exe";

        [MenuItem("Quête I1/Tout régénérer (effets, scène, réglages)", priority = 0)]
        public static void SetupAll()
        {
            ApplyPlayerSettings();
            TestSceneBuilder.Build();
        }

        public static void ApplyPlayerSettings()
        {
            PlayerSettings.companyName = "DefaultCompany";
            PlayerSettings.productName = "Quete I1 - Effets de statut";
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.defaultScreenWidth = 1600;
            PlayerSettings.defaultScreenHeight = 900;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = true;
        }

        [MenuItem("Quête I1/Build Windows", priority = 20)]
        public static void BuildWindows()
        {
            ApplyPlayerSettings();
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { TestSceneBuilder.ScenePath },
                locationPathName = BuildPath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            });

            if (report.summary.result != BuildResult.Succeeded)
                throw new Exception($"Build échoué : {report.summary.result} ({report.summary.totalErrors} erreurs)");
            Debug.Log($"Build OK : {BuildPath} ({report.summary.totalSize / (1024 * 1024)} Mo)");
        }

        /// <summary>Ligne de commande : génère tout puis construit l'exécutable.</summary>
        public static void SetupAndBuild()
        {
            SetupAll();
            BuildWindows();
        }
    }
}
