using System.Collections.Generic;
using PalmStreet.Core;
using PalmStreet.Services;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using static PalmStreet.Game.Paint;

namespace PalmStreet.Game
{
    /// <summary>
    /// Le jeu : crée tout à l'ouverture de la scène « Game », fait avancer la partie à pas fixes de 1/60 s,
    /// lit les touchers (FR-001, FR-006, FR-010), joue les événements (sons, particules, textes), met en pause en
    /// arrière-plan (FR-023) et garde meilleur score, skateur choisi et son coupé (FR-021, FR-022).
    /// </summary>
    public sealed class Game : MonoBehaviour
    {
        const float StepTime = 1f / Tuning.StepsPerSecond;

        public Run Run { get; private set; }
        /// <summary>Les tests font avancer la partie eux-mêmes.</summary>
        public bool Manual;

        Camera cam;
        Scenery scenery;
        Props props;
        Fx fx;
        Sound sound;
        Hud hud;
        RiderView rider, preview;   // preview : le skateur de l'accueil et du profil
        Ground menuGround;
        Outfit outfit;
        int total;   // points cumulés de toutes les parties (FR-027)
        float acc, time, menuX, overAt = -1, overShownAt;
        bool paused, instantScenery = true;
        readonly List<RaycastResult> uiHits = new List<RaycastResult>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Hook()
        {
            SceneManager.sceneLoaded -= OnLoaded;
            SceneManager.sceneLoaded += OnLoaded;
        }

        static void OnLoaded(Scene s, LoadSceneMode mode)
        {
            if (s.name == "Game" && FindAnyObjectByType<Game>() == null) new GameObject("PalmStreet").AddComponent<Game>();
        }

        void Awake()
        {
            Application.targetFrameRate = 60;
            cam = Camera.main;
            cam.orthographic = true;
            var probe = new GameObject("Sonde").AddComponent<SpriteRenderer>();
            Mat = probe.sharedMaterial;   // matériau sprite du pipeline, réutilisé par toutes les formes
            Destroy(probe.gameObject);

            Run = new Run { Best = PlayerPrefs.GetInt("best", 0) };
            outfit = LoadOutfit();
            total = Mathf.Max(PlayerPrefs.GetInt("total", 0), Run.Best);
            menuGround = new Ground(new System.Random(1), 0) { FlatUntil = float.PositiveInfinity };

            scenery = Child<Scenery>("Décor"); scenery.Init();
            props = Child<Props>("Objets"); props.Init();
            fx = Child<Fx>("Particules"); fx.Init();
            sound = Child<Sound>("Son"); sound.Init();
            hud = Child<Hud>("Interface");
            hud.Init(cam, outfit, () => total, () => StartRun(false), OpenProfile, ToMenu, () => Leaderboard.Show(hud.Toast), ToggleMute, () => !sound.Muted, () => Haptics.Enabled, ToggleVibrate, OutfitChanged, () => OpenGameSettings(true), () => OpenGameSettings(false), ToMenu);
            sound.Muted = PlayerPrefs.GetInt("mute", 0) == 1;
            Haptics.Enabled = PlayerPrefs.GetInt("vibrate", 1) == 1;
            preview = RiderView.Create(Wardrobe.Build(outfit), transform, 600);   // au-dessus du canevas (500)
            Leaderboard.SignIn();
            ToMenu();
        }

        T Child<T>(string name) where T : Component
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            return go.AddComponent<T>();
        }

        // ---------- écrans ----------

        public void StartRun(bool boss)
        {
            if (rider != null) Destroy(rider.gameObject);
            rider = RiderView.Create(Wardrobe.Build(outfit), transform, 20);
            Run.Start(boss);
            hud.ShowGame();
            sound.StartRun();
            paused = false; acc = 0; overAt = -1;
            instantScenery = true;
            preview.Show(false);
        }

        public void ToMenu()
        {
            Run.BackToMenu();
            Run.Obstacles.Clear(); Run.Bolts.Clear(); Run.Boss = null; Run.Events.Clear();
            if (rider != null) rider.Show(false);
            sound.StopMusic();
            hud.ShowMenu(Run.Best);
            instantScenery = true;
            preview.Show(true);
        }

        public void OpenProfile()
        {
            ToMenu();
            hud.ShowProfile();
        }

        // ---------- tenue (FR-026), gardée dans PlayerPrefs `outfit` ----------

        static Outfit LoadOutfit()
        {
            try { return JsonUtility.FromJson<Outfit>(PlayerPrefs.GetString("outfit", "")) ?? new Outfit(); }
            catch (System.ArgumentException) { return new Outfit(); }   // valeur abîmée : tenue par défaut
        }

        void OutfitChanged()
        {
            PlayerPrefs.SetString("outfit", JsonUtility.ToJson(outfit));
            PlayerPrefs.Save();
            Destroy(preview.gameObject);
            preview = RiderView.Create(Wardrobe.Build(outfit), transform, 600);
        }

        /// <summary>Bouton RÉGLAGES en partie : pause, son et vibrations ; REPRENDRE relance.</summary>
        public void OpenGameSettings(bool open)
        {
            if (Run.Phase != Phase.Playing) return;
            paused = open;
            sound.Pause(open);
            acc = 0;
            hud.ShowGameSettings(open);
        }

        void ToggleVibrate()
        {
            Haptics.Enabled = !Haptics.Enabled;
            PlayerPrefs.SetInt("vibrate", Haptics.Enabled ? 1 : 0);
            PlayerPrefs.Save();
            Haptics.Play(Buzz.Medium);   // on sent tout de suite ce qu'on vient d'activer
        }

        void ToggleMute()
        {
            sound.Muted = !sound.Muted;
            PlayerPrefs.SetInt("mute", sound.Muted ? 1 : 0);
            PlayerPrefs.Save();
        }


        void SetPaused(bool on)
        {
            if (Run.Phase != Phase.Playing || paused == on) return;
            paused = on;
            hud.ShowPause(on);
            sound.Pause(on);
            acc = 0;
        }

        void OnApplicationPause(bool p) { if (p) SetPaused(true); }
        void OnApplicationFocus(bool f) { if (!f) SetPaused(true); }

        // ---------- boucle ----------

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            time += dt;
            ReadInput();
            if (Run.Phase == Phase.Playing && !paused && !Manual)
            {
                acc += Mathf.Min(dt, 0.1f);
                while (acc >= StepTime && Run.Phase == Phase.Playing) { acc -= StepTime; Run.Step(); }
            }
            PlayEvents();
            if (overAt > 0 && time >= overAt) { overAt = -1; overShownAt = time; hud.ShowOver(Run.Score, Run.Best, Run.NewBest); }
            Draw(dt);
        }

        void ReadInput()
        {
            bool down = false, up = false, key = false;
            var pos = Vector2.zero;
            var pointer = Pointer.current;
            if (pointer != null)
            {
                if (pointer.press.wasPressedThisFrame) { down = true; pos = pointer.position.ReadValue(); }
                if (pointer.press.wasReleasedThisFrame) up = true;
            }
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.spaceKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame) { down = true; key = true; }
                if (kb.spaceKey.wasReleasedThisFrame || kb.upArrowKey.wasReleasedThisFrame) up = true;
                if (kb.escapeKey.wasPressedThisFrame) SetPaused(!paused);
            }
            if (down && !key && OverUi(pos)) down = false;   // les boutons gardent leur toucher
            if (down) Press(pos, key);
            if (up) Run.Release();
        }

        bool OverUi(Vector2 screen)
        {
            var es = EventSystem.current;
            if (es == null) return false;
            uiHits.Clear();
            es.RaycastAll(new PointerEventData(es) { position = screen }, uiHits);
            return uiHits.Count > 0;
        }

        void Press(Vector2 screen, bool key)
        {
            switch (Run.Phase)
            {
                case Phase.Menu:
                    if (!hud.MenuVisible) break;   // dans le profil, seuls les boutons comptent
                    if (key) { StartRun(false); break; }
                    var w = cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, -cam.transform.position.z));
                    if (Vector2.Distance(w, scenery.SunWorld) < scenery.SunWorldRadius * 1.4f) StartRun(true);
                    break;
                case Phase.Playing:
                    if (paused) SetPaused(false);
                    else Run.Press();
                    break;
                case Phase.Over:
                    if (overAt < 0 && time - overShownAt > 0.35f) StartRun(false);
                    break;
            }
        }

        void PlayEvents()
        {
            var yellow = Hex("#FFE14D");
            foreach (var e in Run.Events)
            {
                sound.Play(e.Cue);
                switch (e.Cue)   // FR-028 : vibrations courtes sur les moments forts
                {
                    case Cue.Trick: case Cue.GrindEnd: case Cue.MegaAir: Haptics.Play(Buzz.Medium); break;
                    case Cue.GrindStart: case Cue.GrindOut: Haptics.Play(Buzz.Light); break;
                    case Cue.Crash: case Cue.Boom: Haptics.Play(Buzz.Heavy); break;
                }
                var at = W(e.X, e.Y);
                switch (e.Cue)
                {
                    case Cue.Ollie:
                    case Cue.Land:
                        fx.Puff(e.X, e.Y, DustColor());
                        break;
                    case Cue.Trick:
                        hud.Float($"+{e.Points} {e.Text}", at, yellow);
                        fx.Burst(e.X, e.Y, Hex("#FF4D8D"), 8, 2.5f);
                        break;
                    case Cue.Bolt:
                        hud.Float("+" + e.Points, at, yellow);
                        fx.Burst(e.X, e.Y, yellow, 10, 3);
                        break;
                    case Cue.GrindEnd:
                        hud.Float($"+{e.Points} {e.Text}", at, Hex("#3FE0D0"));
                        break;
                    case Cue.GrindOut:
                        hud.Float($"+{e.Points} {e.Text}", at, Hex("#3FE0D0"));
                        fx.Puff(e.X, e.Y - Tuning.SkaterHeight, Color.white);
                        break;
                    case Cue.FullSpeed:
                        hud.Banner("FULL SPEED", ThemeLook.Of(Run.Theme).Name, yellow);
                        break;
                    case Cue.BossIn:
                        hud.Banner("BOSS !", "Il arrive… saute ses plots !", Hex("#FF4D8D"));
                        break;
                    case Cue.BossHit:
                        props.BossHit();
                        fx.Burst(e.X, e.Y, Color.white, 4, 2, 4);
                        break;
                    case Cue.Level:
                        hud.Banner("NIVEAU 2", "NUIT NÉON · trous, mouettes, tremplins", Hex("#35F2FF"));
                        break;
                    case Cue.MegaAir:
                        hud.Float($"+{e.Points} {e.Text}", at, Hex("#35F2FF"));
                        fx.Burst(e.X, e.Y - 40, Hex("#35F2FF"), 18, 4);
                        break;
                    case Cue.Boom:
                        fx.Confetti(e.X, e.Y);
                        fx.Burst(e.X, e.Y, Hex("#A55BD6"), 40, 7, 10);
                        hud.Float("+" + e.Points, at, yellow);
                        hud.Banner("BOOM", "TU ES UN VRAI PST !", yellow);
                        break;
                    case Cue.Crash:
                        Crashed();
                        break;
                }
            }
            Run.Events.Clear();
        }

        void Crashed()
        {
            rider.Crash(Run.Skater, Run.CurrentSpeed);
            fx.Burst(Run.Skater.X + 12, Run.Skater.Y + 20, Hex("#FF7A1A"), 16, 4);
            sound.StopMusic();
            if (Run.NewBest) PlayerPrefs.SetInt("best", Run.Best);
            int before = Unlocks.CountOpen(total);
            total = (int)System.Math.Min(int.MaxValue, (long)total + Run.Score);
            PlayerPrefs.SetInt("total", total);
            PlayerPrefs.Save();
            int unlocked = Unlocks.CountOpen(total) - before;
            if (unlocked > 0) hud.Toast(unlocked == 1 ? "Nouveau vêtement débloqué dans le PROFIL !" : $"{unlocked} vêtements débloqués dans le PROFIL !");
            Leaderboard.Report(Run.Score);
            overAt = time + 0.7f;   // on voit d'abord la planche s'échapper
        }

        Color DustColor() => Color.Lerp(ThemeLook.Of(Run.Theme).GroundTop, Color.white, 0.5f);

        // ---------- affichage ----------

        /// <summary>Caméra : 390 unités logiques de large au moins, 620 de haut au moins ; le skateur à 90 du bord gauche.</summary>
        public void FitCamera(float x)
        {
            cam.orthographicSize = Mathf.Max(3.1f, 1.95f / cam.aspect);
            cam.transform.position = new Vector3((x + 195 - Tuning.SkaterScreenX) / Ppu, 1.6f, -10);
        }

        void Draw(float dt)
        {
            bool menu = Run.Phase == Phase.Menu;
            if (menu) menuX += dt * 45;
            FitCamera(menu ? menuX : Run.Skater.X);
            var ground = menu ? menuGround : Run.Ground;
            scenery.MenuSun = menu;
            scenery.Draw(menu ? Theme.Largente : Run.Theme, ground, cam, time, instantScenery);
            instantScenery = false;

            if (menu)
            {
                var feet = hud.PreviewFeet(out float scale);
                preview.Show(hud.PreviewVisible);   // caché dans PROFIL › RÉGLAGES
                preview.Idle(feet, time, true, scale);
            }
            else
            {
                var sk = Run.Skater;
                rider.Show(true);
                if (Run.Phase == Phase.Playing)
                {
                    float slope = sk.Rail != null ? 0 : Run.Ground.Slope(sk.X);
                    rider.Ride(sk, W(sk.X, sk.Y), slope, time, Run.Speed > 0 ? Run.CurrentSpeed / Run.Speed : 1);
                    if (!paused)
                    {
                        // NUIT NÉON : feux d'artifice dans le ciel
                        if (Run.Theme == Theme.Neon && Random.value < dt * 0.9f)
                            fx.Firework(sk.X + Random.Range(-60f, 280f), Random.Range(300f, 520f));
                        if (sk.Grounded) fx.Dust(sk.X - 12, sk.Y, DustColor(), 0.6f);
                        if (sk.Rail != null) fx.Sparks(sk.X + 14, sk.Y);   // sous le truck avant (nose grind)
                    }
                }
                else rider.Fall(Run.Ground, dt);
            }
            sound.Grinding(Run.Phase == Phase.Playing && !paused && Run.Skater.Rail != null);
            props.Draw(Run, time);
            hud.Tick(Run, dt, time);
        }
    }
}
