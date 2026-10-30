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
        RiderView rider;
        readonly RiderView[] cardRiders = new RiderView[4];
        Ground menuGround;
        Rider selected;
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
            selected = (Rider)Mathf.Clamp(PlayerPrefs.GetInt("rider", 0), 0, 3);
            menuGround = new Ground(new System.Random(1), 0) { FlatUntil = float.PositiveInfinity };

            scenery = Child<Scenery>("Décor"); scenery.Init();
            props = Child<Props>("Objets"); props.Init();
            fx = Child<Fx>("Particules"); fx.Init();
            sound = Child<Sound>("Son"); sound.Init();
            hud = Child<Hud>("Interface");
            hud.Init(cam, () => StartRun(false), Select, () => Leaderboard.Show(hud.Toast), ToggleMute, ToMenu);
            sound.Muted = PlayerPrefs.GetInt("mute", 0) == 1;
            hud.SetMuted(sound.Muted);
            for (int i = 0; i < 4; i++) cardRiders[i] = RiderView.Create(RiderLook.All[i], transform, 600);   // au-dessus du canevas (500)
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
            var r = boss ? Rider.Freestill : selected;   // FR-020 : le raccourci du soleil part avec FREESTILL
            if (rider != null) Destroy(rider.gameObject);
            rider = RiderView.Create(RiderLook.Of(r), transform, 20);
            Run.Start(r, boss);
            hud.ShowGame();
            sound.StartRun();
            paused = false; acc = 0; overAt = -1;
            instantScenery = true;
            ShowCards(false);
        }

        public void ToMenu()
        {
            Run.BackToMenu();
            Run.Obstacles.Clear(); Run.Bolts.Clear(); Run.Boss = null; Run.Events.Clear();
            if (rider != null) rider.Show(false);
            sound.StopMusic();
            hud.ShowMenu(selected, Run.Best);
            instantScenery = true;
            ShowCards(true);
        }

        void Select(Rider r)
        {
            selected = r;
            hud.Select(r);
            PlayerPrefs.SetInt("rider", (int)r);
        }

        void ToggleMute()
        {
            sound.Muted = !sound.Muted;
            hud.SetMuted(sound.Muted);
            PlayerPrefs.SetInt("mute", sound.Muted ? 1 : 0);
            PlayerPrefs.Save();
        }

        void ShowCards(bool on) { foreach (var c in cardRiders) c.Show(on); }

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
            if (Run.NewBest) { PlayerPrefs.SetInt("best", Run.Best); PlayerPrefs.Save(); }
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
                for (int i = 0; i < 4; i++)
                {
                    var feet = hud.CardFeet(i, out float scale);
                    cardRiders[i].Idle(feet, time + i * 0.7f, i == (int)selected, scale);
                }
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
                        if (sk.Grounded) fx.Dust(sk.X - 12, sk.Y, DustColor(), 0.6f);
                        if (sk.Rail != null) fx.Sparks(sk.X, sk.Y);
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
