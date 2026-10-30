using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PalmStreet.EditorTools
{
    /// <summary>
    /// Ouvre la scène du jeu, affiche la vue en format téléphone (simulateur) et lance le mode Play.
    /// Ligne de commande : Unity -projectPath … -executeMethod PalmStreet.EditorTools.OpenGame.Play
    /// Éditeur déjà ouvert : créer le fichier Temp/palmstreet-play, Unity le fait à la prochaine recompilation.
    /// </summary>
    [InitializeOnLoad]
    public static class OpenGame
    {
        const string Request = "Temp/palmstreet-play";

        static OpenGame()
        {
            if (!File.Exists(Request)) return;
            File.Delete(Request);
            EditorApplication.delayCall += Play;
        }

        [MenuItem("PalmStreet/Jouer dans l'éditeur (format téléphone)")]
        public static void Play()
        {
            if (EditorApplication.isPlaying)
            {
                // on sort d'abord du mode Play, puis on relance une fois revenu en édition
                EditorApplication.playModeStateChanged += Relaunch;
                EditorApplication.isPlaying = false;
                return;
            }
            PhoneView();
            EditorSceneManager.OpenScene("Assets/Scenes/Game.unity");
            EditorApplication.EnterPlaymode();
        }

        static void Relaunch(PlayModeStateChange change)
        {
            if (change != PlayModeStateChange.EnteredEditMode) return;
            EditorApplication.playModeStateChanged -= Relaunch;
            Play();
        }

        [MenuItem("PalmStreet/Format téléphone")]
        public static void PhoneView()
        {
            try
            {
                PlayModeWindow.SetViewType(PlayModeWindow.PlayModeViewTypes.SimulatorView);   // téléphone avec encoche et zones sûres
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("Simulateur indisponible, vue Game en 1179 × 2556 : " + e.Message);
                PlayModeWindow.SetViewType(PlayModeWindow.PlayModeViewTypes.GameView);
                PlayModeWindow.SetCustomRenderingResolution(1179, 2556, "iPhone portrait");
            }
        }
    }
}
