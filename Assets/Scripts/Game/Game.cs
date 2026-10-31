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
        Splash splash;
        RiderView rider, preview;   // preview : le skateur de l'accueil et du profil
        Ground menuGround;
        Progress progress;   // spec 002 : niveaux, meilleurs scores, figures, tenue
        int level;           // niveau en cours (1 à 12), 0 en mode infini
        int mapFrom;         // le petit skateur de la carte part de ce niveau (après une réussite)
        float mapAnimAt;
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
            progress = LoadProgress();
            menuGround = new Ground(new System.Random(1), 0) { FlatUntil = float.PositiveInfinity };

            scenery = Child<Scenery>("Décor"); scenery.Init();
            props = Child<Props>("Objets"); props.Init();
            fx = Child<Fx>("Particules"); fx.Init();
            sound = Child<Sound>("Son"); sound.Init();
            hud = Child<Hud>("Interface");
            hud.Init(cam, new HudActions
            {
                Play = OpenMap, Endless = () => StartEndless(false), OpenProfile = OpenProfile, CloseProfile = ToMenu,
                Top10 = () => Leaderboard.Show(hud.Toast), Mute = ToggleMute, ToggleVibrate = ToggleVibrate, OutfitChanged = OutfitChanged,
                OpenSettings = () => OpenGameSettings(true), Resume = () => OpenGameSettings(false),
                Quit = () => { if (level > 0) OpenMap(); else ToMenu(); },
                Home = ToMenu, Map = OpenMap, Retry = () => StartLevel(level),
                Next = () => { if (level >= Levels.Count) StartEndless(false); else StartLevel(level + 1); },
                PickLevel = StartLevel, SoundOn = () => !sound.Muted, VibrateOn = () => Haptics.Enabled, Progress = () => progress,
            });
            sound.Muted = PlayerPrefs.GetInt("mute", 0) == 1;
            Haptics.Enabled = PlayerPrefs.GetInt("vibrate", 1) == 1;
            preview = RiderView.Create(Wardrobe.Build(progress), transform, 600);   // au-dessus du canevas (500)
            Leaderboard.SignIn();
            ToMenu();
            splash = Child<Splash>("Lancement");   // FR-028 : la planche P. ST. avant l'accueil
            splash.Init(cam, () => { });
        }

        T Child<T>(string name) where T : Component
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            return go.AddComponent<T>();
        }

        // ---------- écrans ----------

        /// <summary>Partie infinie avec tout débloqué (tests de capture).</summary>
        public void StartRun(bool boss)
        {
            level = 0;
            Run.Start(boss);
            BeginRun();
        }

        /// <summary>Mode histoire : un niveau de la carte (spec 002).</summary>
        public void StartLevel(int n)
        {
            level = n;
            Run.Best = progress.Best[n - 1];
            var def = Levels.Get(n);
            Run.StartLevel(def, progress.Tricks);
            BeginRun();
            hud.Banner($"NIVEAU {n}", def.City, Hex("#FFE14D"));
        }

        /// <summary>Mode infini : villes ouvertes, boss déjà battus, obstacles des niveaux ouverts (spec 002, FR-018 à FR-020).</summary>
        public void StartEndless(bool boss)
        {
            level = 0;
            Run.Best = progress.EndlessBest;
            var cities = new List<Theme>();
            var bosses = new List<BossKind>();
            var allowed = Allow.None;
            foreach (var d in Levels.All)
            {
                if (!progress.IsOpen(d.Number)) continue;
                cities.Add(d.Theme);
                allowed |= d.Allowed;
                if (d.Boss != BossKind.None && progress.IsCleared(d.Number)) bosses.Add(d.Boss);
            }
            Run.StartEndless(cities, bosses, progress.Tricks, allowed, boss);
            BeginRun();
        }

        void BeginRun()
        {
            if (rider != null) Destroy(rider.gameObject);
            rider = RiderView.Create(Wardrobe.Build(progress), transform, 20);
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
            hud.ShowMenu(progress.EndlessBest);
            instantScenery = true;
        }

        public Splash LaunchAnimation => splash;

        /// <summary>Progression du joueur (les tests de capture la remplacent).</summary>
        public Progress Progress { get => progress; set { progress = value; OutfitChanged(); } }

        public void OpenMap()
        {
            ToMenu();
            hud.ShowMap(progress);
            mapAnimAt = time;
        }

        // ---------- progression (spec 002), gardée dans PlayerPrefs `progress` ----------

        Progress LoadProgress()
        {
            Progress p = null;
            try { p = JsonUtility.FromJson<Progress>(PlayerPrefs.GetString("progress", "")); }
            catch (System.ArgumentException) { }   // valeur abîmée : on repart de zéro
            if (p == null)
            {
                p = new Progress { EndlessBest = PlayerPrefs.GetInt("best", 0) };   // ancien meilleur score = meilleur score infini
                try
                {
                    var old = JsonUtility.FromJson<OldOutfit>(PlayerPrefs.GetString("outfit", ""));
                    if (old != null) { p.Skin = old.Skin; p.HairColor = old.HairColor; p.HairStyle = old.Head == 2 ? 1 : 0; }
                }
                catch (System.ArgumentException) { }
            }
            p.Repair();
            return p;
        }

        void SaveProgress()
        {
            PlayerPrefs.SetString("progress", JsonUtility.ToJson(progress));
            PlayerPrefs.Save();
        }

        void LevelCleared()
        {
            sound.StopMusic();
            var def = Levels.Get(level);
            var (trick, item) = progress.Clear(level, Run.Score);
            SaveProgress();
            mapFrom = level;
            hud.ShowCleared(def, Run.Score, progress.Best[level - 1], trick >= 0 ? Tuning.TrickNames[trick] : null, item);
        }

        public void OpenProfile()
        {
            ToMenu();
            hud.ShowProfile();
        }

        // ---------- tenue (spec 002, FR-023 à FR-027), gardée dans `progress` ----------

        void OutfitChanged()
        {
            SaveProgress();
            Destroy(preview.gameObject);
            preview = RiderView.Create(Wardrobe.Build(progress), transform, 600);
        }

        /// <summary>Ancienne tenue (001) : on n'en garde que la peau et les cheveux.</summary>
        [System.Serializable] sealed class OldOutfit { public int Head, HairColor, Skin; }

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
            if (overAt > 0 && time >= overAt) { overAt = -1; overShownAt = time; hud.ShowOver(Run.Score, Run.Best, Run.NewBest, level > 0); }
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
            if (down && splash.Running) { splash.Skip(); return; }   // un toucher passe l'animation de lancement
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
                    if (Vector2.Distance(w, scenery.SunWorld) < scenery.SunWorldRadius * 1.4f) StartEndless(true);   // raccourci du soleil
                    break;
                case Phase.Playing:
                    if (paused) SetPaused(false);
                    else Run.Press();
                    break;
                case Phase.Over:
                    if (level == 0 && overAt < 0 && time - overShownAt > 0.35f) StartEndless(false);   // en histoire : RECOMMENCER ou CARTE
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
                        hud.Banner(BossName(Run.Boss?.Kind ?? BossKind.Purple), BossTip(Run.Boss?.Kind ?? BossKind.Purple), Hex("#FF4D8D"));
                        break;
                    case Cue.BossHit:
                        props.BossHit();
                        fx.Burst(e.X, e.Y, Color.white, 4, 2, 4);
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
                    case Cue.Cleared:
                        LevelCleared();
                        break;
                    case Cue.Finish:
                        fx.Confetti(e.X + 40, e.Y + 80);
                        hud.Banner("ARRIVÉE !", Levels.Get(level).City, yellow);
                        break;
                    case Cue.NewObstacle:
                        if (level > 0 && !progress.SeenObstacles.Contains(level))
                        {
                            hud.Tip(e.Text);   // bulle d'aide la première fois (FR-007)
                            progress.SeenObstacles.Add(level);
                            SaveProgress();
                        }
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
            if (level == 0 && Run.NewBest) { progress.EndlessBest = Run.Best; SaveProgress(); }
            if (level == 0) Leaderboard.Report(Run.Score);   // classement : mode infini seulement
            overAt = time + 0.7f;   // on voit d'abord la planche s'échapper
        }

        static string BossName(BossKind k) => k switch { BossKind.Crab => "LE CRABE GÉANT", BossKind.Gull => "LE GOÉLAND GÉANT", BossKind.Boar => "LE SANGLIER", _ => "LE GRAND BOSS" };
        static string BossTip(BossKind k) => k switch
        {
            BossKind.Crab => "Saute-le quand il fonce sur toi !",
            BossKind.Gull => "Accroupis-toi quand il pique !",
            BossKind.Boar => "Saute ses charges et ses barrières !",
            _ => "Saute ses plots !",
        };

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
            scenery.Draw(menu ? Theme.Anglet : Run.Theme, ground, cam, time, instantScenery);
            instantScenery = false;

            if (menu)
            {
                if (hud.MapVisible)
                {
                    // FR-022 : petit skateur posé sur le prochain niveau ; après une réussite, il y roule depuis le niveau réussi
                    int next = progress.NextLevel;
                    var to = hud.NodeWorld(next, out bool visible, out float scale);
                    float k = mapFrom > 0 && mapFrom != next ? Mathf.Clamp01((time - mapAnimAt - 0.3f) / 1.4f) : 1;
                    if (k >= 1) mapFrom = 0;
                    var from = mapFrom > 0 ? hud.NodeWorld(mapFrom, out _, out _) : to;
                    preview.Show(visible);
                    preview.Idle(Vector3.Lerp(from, to, k * k * (3 - 2 * k)), time, k >= 1, scale);
                }
                else
                {
                    var feet = hud.PreviewFeet(out float scale);
                    preview.Show(hud.PreviewVisible);   // profil, onglet TENUE
                    preview.Idle(feet, time, true, scale);
                }
            }
            else
            {
                var sk = Run.Skater;
                rider.Show(true);
                if (Run.Phase == Phase.Playing || Run.Phase == Phase.Cleared)
                {
                    float slope = sk.Rail != null ? 0 : Run.Ground.Slope(sk.X);
                    rider.Ride(sk, W(sk.X, sk.Y), slope, time, Run.Speed > 0 ? Run.CurrentSpeed / Run.Speed : 1);
                    if (!paused && Run.Phase == Phase.Playing)
                    {
                        // NUIT NÉON : feux d'artifice dans le ciel
                        if (ThemeLook.Of(Run.Theme).Decor == Decor.Neon && Random.value < dt * 0.9f)
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
