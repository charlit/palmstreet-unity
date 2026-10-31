using UnityEngine;
#if UNITY_IOS && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

namespace PalmStreet.Services
{
    public enum Buzz { Light, Medium, Heavy }

    /// <summary>
    /// Vibrations courtes (FR-028). iOS : Taptic Engine (Assets/Plugins/iOS/PalmHaptics.mm).
    /// Android : Vibrator avec une impulsion de 15 à 60 ms. Ailleurs (éditeur) : rien.
    /// </summary>
    public static class Haptics
    {
        public static bool Enabled = true;

#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")] static extern void PalmHaptic(int style);
#endif

#if UNITY_ANDROID && !UNITY_EDITOR
        static AndroidJavaObject vibrator;
#endif

        public static void Play(Buzz b)
        {
            if (!Enabled) return;
#if UNITY_IOS && !UNITY_EDITOR
            PalmHaptic((int)b);
#elif UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                if (vibrator == null)
                    using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                    using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                        vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
                long ms = b == Buzz.Light ? 15 : b == Buzz.Medium ? 30 : 60;
                using (var version = new AndroidJavaClass("android.os.Build$VERSION"))
                {
                    if (version.GetStatic<int>("SDK_INT") >= 26)
                        using (var effect = new AndroidJavaClass("android.os.VibrationEffect"))
                        using (var oneShot = effect.CallStatic<AndroidJavaObject>("createOneShot", ms, b == Buzz.Heavy ? 255 : 120))
                            vibrator.Call("vibrate", oneShot);
                    else Handheld.Vibrate();   // vieux Android ; cette référence ajoute aussi la permission VIBRATE
                }
            }
            catch (AndroidJavaException) { Enabled = false; }   // pas de vibreur : on n'insiste pas
#endif
        }
    }
}
