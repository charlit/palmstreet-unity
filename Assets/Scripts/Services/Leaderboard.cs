using System;
using UnityEngine;

namespace PalmStreet.Services
{
    /// <summary>
    /// Classement en ligne (FR-022). iOS : Game Center, classement palmstreet.top (celui de l'app PalmStreet existante).
    /// Android : Google Play Games n'est pas encore branché (il faut son plugin) ; le bouton le dit.
    /// Sans connexion, le jeu reste jouable et le bouton explique pourquoi le classement est indisponible.
    /// </summary>
    public static class Leaderboard
    {
        public const string Id = "palmstreet.top";
        static bool ready;

        public static void SignIn()
        {
#if UNITY_IOS && !UNITY_EDITOR
            Social.localUser.Authenticate(ok => ready = ok);
#endif
        }

        public static void Report(int score)
        {
            if (ready) Social.ReportScore(score, Id, _ => { });
        }

        /// <summary>Ouvre le classement, ou appelle unavailable avec la raison.</summary>
        public static void Show(Action<string> unavailable)
        {
#if UNITY_IOS && !UNITY_EDITOR
            if (ready) { Social.ShowLeaderboardUI(); return; }
            unavailable(Application.internetReachability == NetworkReachability.NotReachable
                ? "Hors ligne : le classement revient avec le réseau"
                : "Connecte-toi à Game Center pour voir le TOP 10");
            SignIn();
#elif UNITY_ANDROID && !UNITY_EDITOR
            unavailable("Classement Google Play bientôt disponible");
#else
            unavailable("Classement disponible sur le téléphone");
#endif
        }
    }
}
