using PalmStreet.Core;
using UnityEngine;

namespace PalmStreet.Game
{
    /// <summary>
    /// Musique et bruitages synthétisés au lancement (FR-021) : une boucle à 132 bpm (basse, arpèges, batterie,
    /// mélodie) et les sons du jeu (saut, figure, éclair, grind, chute, boss). Aucun fichier son.
    /// </summary>
    public sealed class Sound : MonoBehaviour
    {
        const int Rate = 22050;
        AudioSource music, sfx, grind, tricks;   // tricks : sa hauteur monte avec le combo
        AudioClip[] flips;   // un son par figure de Tuning.TrickNames (T033)
        AudioClip ollie, land, trick, bolt, crash, boom, tooth, hit, banner, start;
        bool muted;

        public bool Muted
        {
            get => muted;
            set { muted = value; AudioListener.volume = value ? 0 : 1; }
        }

        public void Init()
        {
            music = gameObject.AddComponent<AudioSource>();
            music.loop = true; music.volume = 0.45f;
            sfx = gameObject.AddComponent<AudioSource>();
            grind = gameObject.AddComponent<AudioSource>();
            tricks = gameObject.AddComponent<AudioSource>();
            grind.loop = true; grind.volume = 0.35f;

            music.clip = Music();
            ollie = Make("ollie", 0.16f, (t, n) => Noise(n) * Env(t, 0.002f, 0.04f) * 0.6f + Sine(t, Sweep(t, 0.12f, 260, 620)) * Env(t, 0.005f, 0.12f) * 0.5f);
            land = Make("retombée", 0.12f, (t, n) => Sine(t, Sweep(t, 0.1f, 140, 55)) * Env(t, 0.002f, 0.1f) * 0.7f + Noise(n) * Env(t, 0.001f, 0.03f) * 0.3f);
            trick = Make("figure", 0.26f, (t, n) => Square(t, Sweep(t, 0.26f, 420, 1300)) * Env(t, 0.01f, 0.24f) * 0.22f + Noise(n) * Env(t, 0.05f, 0.2f) * 0.25f);
            // figures : même base, mais hauteur, montée, forme d'onde et tournoiement (les 360) propres à chacune
            flips = new AudioClip[Tuning.TrickNames.Length];
            for (int k = 0; k < flips.Length; k++)
            {
                int i = k;
                float f0 = 300 + 70 * i, f1 = (i % 2 == 0 ? 1500 : 700) + 60 * i, spin = Tuning.TrickNames[i].Contains("360") ? 14 + 4 * i : 0;
                flips[k] = Make(Tuning.TrickNames[i], 0.26f, (t, n) =>
                {
                    float f = Sweep(t, 0.26f, f0, f1) * (1 + (spin > 0 ? 0.12f * Mathf.Sin(t * 2 * Mathf.PI * spin) : 0));
                    float tone = i % 3 == 0 ? Square(t, f) : i % 3 == 1 ? Tri(t, f) * 1.6f : Sine(t, f) * 1.8f;
                    return tone * Env(t, 0.01f, 0.24f) * 0.22f + Noise(n) * Env(t, 0.03f, i % 2 == 1 ? 0.24f : 0.12f) * 0.25f;
                });
            }
            bolt = Make("éclair", 0.16f, (t, n) => Square(t, t < 0.06f ? 1320 : 1980) * Env(t, 0.002f, 0.15f) * 0.3f);
            crash = Make("chute", 0.7f, (t, n) => Noise(n) * Env(t, 0.002f, 0.55f) * 0.7f + Sine(t, Sweep(t, 0.4f, 160, 45)) * Env(t, 0.002f, 0.4f) * 0.6f + Square(t, 230) * Env(t, 0.001f, 0.12f) * 0.2f);
            boom = Make("boom", 1.4f, (t, n) => Noise(n) * Env(t, 0.005f, 1.3f) * 0.8f + Sine(t, Sweep(t, 1f, 90, 30)) * Env(t, 0.005f, 1.2f) * 0.8f);
            tooth = Make("dent", 0.06f, (t, n) => Square(t, Sweep(t, 0.06f, 1500, 800)) * Env(t, 0.001f, 0.05f) * 0.15f);
            hit = Make("touché", 0.09f, (t, n) => Square(t, 190) * Env(t, 0.001f, 0.08f) * 0.25f + Noise(n) * Env(t, 0.001f, 0.04f) * 0.3f);
            banner = Make("bandeau", 0.4f, (t, n) => Square(t, new[] { 523f, 659, 784, 1047 }[Mathf.Min(3, (int)(t / 0.08f))]) * Env(t % 0.08f, 0.002f, 0.1f) * 0.22f);
            start = Make("départ", 0.3f, (t, n) => Square(t, t < 0.1f ? 392 : t < 0.2f ? 523 : 784) * Env(t % 0.1f, 0.002f, 0.1f) * 0.22f);
            grind.clip = Make("grind", 0.5f, (t, n) => (Noise(n) * 0.5f + Sine(t, 1830) * 0.25f + Sine(t, 2470) * 0.2f) * (0.7f + 0.3f * Mathf.Sin(t * 2 * Mathf.PI * 18)) * 0.5f);
        }

        public void Play(Cue cue, int combo = 1, string text = null)
        {
            if (cue == Cue.Trick)
            {
                tricks.pitch = 1 + 0.06f * Mathf.Clamp(combo - 2, 0, 10);
                // « FAKIE 360 FLIP ! » → 360 FLIP ; grinds, 180 et manuals gardent le son commun
                string name = text?.Replace("FAKIE ", "").TrimEnd(' ', '!');
                int k = name == null ? -1 : System.Array.IndexOf(Tuning.TrickNames, name);
                tricks.PlayOneShot(k >= 0 ? flips[k] : trick);
                return;
            }
            AudioClip c = cue switch
            {
                Cue.Ollie => ollie, Cue.Land => land, Cue.Trick => trick, Cue.Bolt => bolt, Cue.GrindOut => ollie,
                Cue.GrindEnd => trick, Cue.Bail => land, Cue.Spot => banner, Cue.Boost => banner, Cue.Spell => boom, Cue.Crash => crash, Cue.Boom => boom, Cue.Tooth => tooth, Cue.BossHit => hit,
                Cue.FullSpeed => banner, Cue.BossIn => banner, Cue.BossFly => trick, Cue.MegaAir => trick, _ => null,
            };
            if (c != null) sfx.PlayOneShot(c, cue == Cue.Tooth ? 0.6f : 1);
        }

        public void StartRun() { sfx.PlayOneShot(start); if (!music.isPlaying) music.Play(); }
        public void StopMusic() { music.Stop(); grind.Stop(); }
        public void Pause(bool on) { if (on) { music.Pause(); grind.Pause(); } else music.UnPause(); }

        public void Grinding(bool on)
        {
            if (on && !grind.isPlaying) grind.Play();
            else if (!on && grind.isPlaying) grind.Stop();
        }

        // ---------- synthèse ----------

        static float Sine(float t, float f) => Mathf.Sin(2 * Mathf.PI * f * t);
        static float Square(float t, float f) => Mathf.Repeat(f * t, 1) < 0.5f ? 1 : -1;
        static float Tri(float t, float f) { float p = Mathf.Repeat(f * t, 1); return 4 * Mathf.Abs(p - 0.5f) - 1; }
        static float Env(float t, float attack, float decay) => t < attack ? t / attack : Mathf.Max(0, 1 - (t - attack) / decay);
        static float Sweep(float t, float dur, float f0, float f1) => f0 + (f1 - f0) * Mathf.Clamp01(t / dur) * 0.5f;   // fréquence moyenne : phase continue
        static uint seed = 22222;
        static float Noise(int n) { seed ^= seed << 13; seed ^= seed >> 17; seed ^= seed << 5; return (seed & 0xFFFF) / 32768f - 1; }

        static AudioClip Make(string name, float seconds, System.Func<float, int, float> f)
        {
            int n = (int)(seconds * Rate);
            var data = new float[n];
            for (int i = 0; i < n; i++) data[i] = Mathf.Clamp(f(i / (float)Rate, i), -1, 1);
            var clip = AudioClip.Create(name, n, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        static float Midi(int note) => 440 * Mathf.Pow(2, (note - 69) / 12f);

        /// <summary>Boucle de 8 mesures à 132 bpm : la mineur, fa, do, sol (deux fois, mélodie variée la seconde).</summary>
        static AudioClip Music()
        {
            const float bpm = 132, sixteenth = 60 / bpm / 4;
            int bars = 8, steps = bars * 16, n = (int)(steps * sixteenth * Rate);
            var buf = new float[n];
            int[] roots = { 57, 53, 48, 55 };                         // A, F, C, G
            int[][] chords = { new[] { 0, 3, 7 }, new[] { 0, 4, 7 }, new[] { 0, 4, 7 }, new[] { 0, 4, 7 } };
            int[] melodyA = { 12, -1, 15, -1, 19, -1, 17, 15, 12, -1, 10, -1, 12, -1, -1, -1 };
            int[] melodyB = { 19, -1, 17, -1, 15, -1, 12, -1, 15, 17, 19, -1, 22, -1, 19, -1 };

            void Note(int step, float len, float freq, float vol, int wave, float decay)
            {
                int s0 = (int)(step * sixteenth * Rate), count = (int)(len * Rate);
                for (int i = 0; i < count && s0 + i < n; i++)
                {
                    float t = i / (float)Rate, env = Env(t, 0.004f, decay);
                    float v = wave == 0 ? Square(t, freq) * 0.5f : wave == 1 ? Tri(t, freq) : Sine(t, freq);
                    buf[s0 + i] += v * env * vol;
                }
            }

            for (int bar = 0; bar < bars; bar++)
            {
                int chord = bar % 4, root = roots[chord], b0 = bar * 16;
                for (int s = 0; s < 16; s += 2) Note(b0 + s, sixteenth * 1.8f, Midi(root - 12 + (s % 4 == 2 ? 12 : 0)), 0.32f, 1, sixteenth * 1.7f);   // basse
                for (int s = 0; s < 16; s++) Note(b0 + s, sixteenth, Midi(root + 12 + chords[chord][s % 3]), 0.07f, 0, sixteenth * 0.9f);              // arpège
                var mel = bar < 4 ? melodyA : melodyB;
                for (int s = 0; s < 16; s++)
                    if (mel[s] >= 0 && (bar % 2 == 0 || s < 8)) Note(b0 + s, sixteenth * 2, Midi(57 + mel[s]), 0.11f, 0, sixteenth * 2.4f);   // mélodie
                // batterie
                for (int s = 0; s < 16; s++)
                {
                    int i0 = (int)((b0 + s) * sixteenth * Rate);
                    if (s % 8 == 0) for (int i = 0; i < Rate / 8 && i0 + i < n; i++) { float t = i / (float)Rate; buf[i0 + i] += Sine(t, 110 - t * 500) * Env(t, 0.001f, 0.12f) * 0.55f; }
                    if (s % 8 == 4) for (int i = 0; i < Rate / 10 && i0 + i < n; i++) { float t = i / (float)Rate; buf[i0 + i] += Noise(i) * Env(t, 0.001f, 0.09f) * 0.3f; }
                    if (s % 2 == 0) for (int i = 0; i < Rate / 40 && i0 + i < n; i++) { float t = i / (float)Rate; buf[i0 + i] += Noise(i) * Env(t, 0.001f, 0.02f) * 0.08f; }
                }
            }
            for (int i = 0; i < n; i++) buf[i] = (float)System.Math.Tanh(buf[i] * 1.2f) * 0.85f;
            var clip = AudioClip.Create("musique 132 bpm", n, 1, Rate, false);
            clip.SetData(buf, 0);
            return clip;
        }
    }
}
