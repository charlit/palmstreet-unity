using System.Collections;
using System.IO;
using NUnit.Framework;
using PalmStreet.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace PalmStreet.Tests
{
    /// <summary>
    /// Captures d'écran à des moments choisis (Tools/capture.sh), pour vérifier le rendu sans téléphone.
    /// Images dans le dossier PALMSTREET_CAPTURES (sinon Temp/Captures).
    /// </summary>
    public class CaptureTests
    {
        const int W = 780, H = 1688;   // iPhone en portrait, moitié de la résolution native

        static string Dir => System.Environment.GetEnvironmentVariable("PALMSTREET_CAPTURES") ?? Path.Combine(Application.dataPath, "../Temp/Captures");

        [UnityTest, Timeout(600000)]   // plus de 3 min : une centaine de captures
        public IEnumerator CaptureKeyMoments()
        {
            yield return SceneManager.LoadSceneAsync("Game");
            yield return null;
            var game = Object.FindAnyObjectByType<PalmStreet.Game.Game>();
            Assert.IsNotNull(game, "le jeu n'a pas démarré dans la scène Game");
            game.Manual = true;
            Directory.CreateDirectory(Dir);
            var run = game.Run;

            // animation de lancement : arrivée sur le titre, grind sur STREET, kickflip de sortie, puis on la passe
            while (game.LaunchAnimation.Running && game.LaunchAnimation.Elapsed < 0.55f) yield return null;
            yield return Shot("00-lancement-arrivee");
            while (game.LaunchAnimation.Running && game.LaunchAnimation.Elapsed < 1.2f) yield return null;
            yield return Shot("00-lancement-grind");
            while (game.LaunchAnimation.Running && game.LaunchAnimation.Elapsed < 2.05f) yield return null;
            yield return Shot("00-lancement-pst");
            game.LaunchAnimation.Skip();

            for (int i = 0; i < 10; i++) yield return null;
            yield return Shot("1-accueil");
            // spec 002 : carte neuve, puis après 5 niveaux ; départ, arrivée et réussite d'Anglet
            var saved = game.Progress;
            game.Progress = new Progress();
            game.OpenMap();
            var hud = Object.FindAnyObjectByType<PalmStreet.Game.Hud>();
            hud.ShowTutorial();
            for (int i = 0; i < 30; i++) yield return null;
            yield return Shot("0-tutoriel");
            hud.HideTutorial();
            for (int i = 0; i < 3; i++) yield return null;
            yield return Shot("0-carte-neuve");
            var advanced = new Progress();
            for (int n = 1; n <= 5; n++) advanced.Clear(n, 1000 * n);
            advanced.Stars[0] = advanced.Stars[2] = advanced.Stars[3] = true;
            advanced.ScoreStars[0] = advanced.ScoreStars[3] = true;
            game.Progress = advanced;
            game.OpenMap();
            for (int i = 0; i < 40; i++) yield return null;
            yield return Shot("0-carte-avancee");
            hud.ShowPreview(6);
            yield return null;
            yield return Shot("0-carte-apercu");
            game.Progress = new Progress();
            game.StartLevel(1);
            yield return Steps(run, 30, false);
            yield return Shot("0-anglet-depart");
            run.Skater.X = Levels.Get(1).Finish - 180;
            run.Obstacles.Clear(); run.Bolts.Clear();
            yield return Steps(run, 30, true);
            yield return Shot("0-anglet-arrivee");
            for (int i = 0; i < 400 && run.Phase == Phase.Playing; i++) { run.Obstacles.Clear(); run.Step(); }
            for (int i = 0; i < 5; i++) yield return null;
            yield return Shot("0-anglet-reussi");
            // les 12 villes, au départ de leur niveau
            for (int n = 1; n <= 12; n++)
            {
                game.StartLevel(n);
                yield return Steps(run, 140, true);
                yield return Shot($"0-ville-{n:00}");
            }
            // spec 003 : textes du milieu de l'écran sur un ciel de jour (Biarritz : sombres) et du soir (Anglet : couleurs)
            foreach (int n in new[] { 2, 1 })
            {
                game.StartLevel(n);
                yield return Steps(run, 140, true);
                hud.Banner("FULL SPEED", Levels.Get(n).City, new Color(1, 0.88f, 0.3f));
                hud.Shout("KICKFLIP", 4);
                hud.Float("+100 KICKFLIP", new Vector3(run.Skater.X / 100f, (run.Skater.Y + 70) / 100f, 0), new Color(1, 0.88f, 0.3f));
                for (int i = 0; i < 6; i++) yield return null;
                yield return Shot($"003-textes-{Levels.Get(n).City.ToLowerInvariant()}");
            }

            // les 4 boss : au sol, puis en pleine attaque
            foreach (int n in new[] { 3, 6, 9, 12 })
            {
                game.StartLevel(n);
                run.Skater.X = Levels.Get(n).Finish - 2;
                for (int i = 0; i < 200; i++) { run.Obstacles.RemoveAll(o => !o.Falling || o.X < run.Skater.X + 30); run.Step(); run.Phase = Phase.Playing; if (i % 3 == 0) yield return null; }   // les plots tombés trop près sont retirés : il ne chute pas avant la photo
                yield return Shot($"0-boss-{n}");
                for (int i = 0; i < 900 && !(run.Boss.State == BossState.Rising && run.Boss.Timer > 25) && !(run.Boss.State == BossState.Air && run.Boss.Drops > 1); i++)
                {
                    run.Obstacles.RemoveAll(o => o.X < run.Skater.X + 40); run.Step(); run.Phase = Phase.Playing;
                    if (i % 4 == 0) yield return null;
                }
                yield return Shot($"0-boss-{n}-attaque");
                if (n == 3 || n == 12)   // sorts et dent lancés pendant le combat, boutons de sorts à droite
                {
                    run.Boss.Hp = 100000;
                    for (int i = 0; i < 200 && run.Boss.State != BossState.Ground; i++) { run.Obstacles.RemoveAll(o => !o.Falling); run.Step(); run.Phase = Phase.Playing; }
                    run.Cast(0); run.Cast(1); run.Cast(3);
                    for (int i = 0; i < 14; i++) { run.Obstacles.RemoveAll(o => !o.Falling); run.Step(); run.Phase = Phase.Playing; if (i % 3 == 0) yield return null; }
                    run.Cast(2);
                    yield return null;
                    yield return Shot($"0-boss-{n}-sorts");
                }
            }
            // spec 004 : Biarritz (stade, palace, rocher), ballons et mouette de jour, boss de rugby et leurs attaques
            game.StartLevel(2);
            yield return Steps(run, 120, true);
            for (int k = 1; k <= 3; k++)
            {
                if (k == 2)
                {
                    run.Obstacles.RemoveAll(o => !o.IsRail);
                    run.Obstacles.Add(new Obstacle { Kind = ObstacleKind.Ball, X = run.Skater.X + 150, W = Tuning.GullWidth, H = Tuning.GullBody, Y = run.Ground.Floor(run.Skater.X + 150) + Tuning.GullHeight });
                    run.Obstacles.Add(new Obstacle { Kind = ObstacleKind.Gull, X = run.Skater.X + 250, W = Tuning.GullWidth, H = Tuning.GullBody, Y = run.Ground.Floor(run.Skater.X + 250) + Tuning.GullHeight + 20 });
                    for (int i = 0; i < 4; i++) yield return null;
                }
                yield return Shot($"004-biarritz-{k}");
                yield return Steps(run, 170, true);
            }
            foreach (int n in new[] { 2, 7 })
            {
                game.StartLevel(n);
                run.Skater.X = Levels.Get(n).Finish - 2;
                for (int i = 0; i < 200 && (run.Boss == null || run.Boss.State != BossState.Ground); i++) { run.Obstacles.RemoveAll(o => o.Kind != ObstacleKind.Ball); run.Step(); run.Phase = Phase.Playing; if (i % 3 == 0) yield return null; }
                run.Boss.Hp = 100000;
                yield return Shot($"004-boss-{n}");
                for (int attack = 0; attack < 3; attack++)
                {
                    for (int i = 0; i < 600 && !(run.Boss.Attack == attack && run.Boss.State == BossState.Rising && run.Boss.Timer >= Tuning.RugbyWindup - 6); i++)
                    { run.Obstacles.RemoveAll(o => o.Kind != ObstacleKind.Ball || o.X < run.Skater.X + 40); if (run.Boss.Tackling && run.Boss.Offset < 60) run.Boss.Offset = 60; run.Step(); run.Phase = Phase.Playing; if (i % 3 == 0) yield return null; }
                    yield return Shot($"004-boss-{n}-annonce-{attack}");
                    int after = attack == 1 ? 50 : 14;
                    for (int i = 0; i < after; i++) { run.Obstacles.RemoveAll(o => o.Kind != ObstacleKind.Ball || o.X < run.Skater.X + 40); run.Step(); run.Phase = Phase.Playing; if (i % 2 == 0) yield return null; }
                    yield return Shot($"004-boss-{n}-attaque-{attack}");
                }
            }
            game.StartLevel(12);
            yield return Steps(run, 140, true);
            run.Obstacles.RemoveAll(o => !o.IsRail);
            run.Obstacles.Add(new Obstacle { Kind = ObstacleKind.Ball, X = run.Skater.X + 150, W = Tuning.GullWidth, H = Tuning.GullBody, Y = run.Ground.Floor(run.Skater.X + 150) + Tuning.GullHeight });
            run.Obstacles.Add(new Obstacle { Kind = ObstacleKind.Gull, X = run.Skater.X + 250, W = Tuning.GullWidth, H = Tuning.GullBody, Y = run.Ground.Floor(run.Skater.X + 250) + Tuning.GullHeight + 20 });
            for (int i = 0; i < 4; i++) yield return null;
            yield return Shot("004-nuit-volants");
            foreach (var (lvl, ids) in new[] { (2, new[] { "band-pilier", "jersey-pilier", "shorts-pilier", "boots-pilier", "deck-pilier" }), (7, new[] { "scrum-cap", "jersey-talonneur", "shorts-talonneur", "boots-talonneur", "deck-talonneur" }) })
            {
                var rugby = new Progress();
                rugby.Clear(lvl, 1000);
                foreach (var id in ids) Assert.IsTrue(rugby.Wear(id), id);
                game.Progress = rugby;
                game.OpenProfile();
                for (int i = 0; i < 3; i++) yield return null;
                yield return Shot($"004-tenue-{lvl}");
            }
            game.ToMenu();

            // profil : tenue de départ, puis après 6 niveaux avec des objets portés
            game.Progress = new Progress();
            game.OpenProfile();
            for (int i = 0; i < 3; i++) yield return null;
            yield return Shot("0-profil-neuf");
            var six = new Progress();
            for (int n = 1; n <= 6; n++) six.Clear(n, 1000);
            six.Wear("cap-red"); six.Wear("hoodie-gull"); six.Wear("shorts-surf"); six.Wear("deck-crab");
            game.Progress = six;
            game.OpenProfile();
            for (int i = 0; i < 3; i++) yield return null;
            yield return Shot("0-profil-six");
            var all = new Progress();
            for (int n = 1; n <= 12; n++) { all.Clear(n, 1); all.Stars[n - 1] = all.ScoreStars[n - 1] = true; }
            foreach (var id in new[] { "cap-gold", "hoodie-basque", "pants-white", "shoes-pink", "deck-gold" }) all.Wear(id);
            game.Progress = all;
            game.OpenProfile();
            for (int i = 0; i < 3; i++) yield return null;
            yield return Shot("0-profil-etoiles");
            foreach (var id in new[] { "band-red", "hoodie-ocean", "shorts-surf", "shoes-yellow", "deck-gold" }) all.Wear(id);
            game.Progress = all;
            game.OpenProfile();
            for (int i = 0; i < 3; i++) yield return null;
            yield return Shot("0-profil-tenue2");
            // spec 003 : le reste de la garde-robe, peaux et cheveux sur le nouveau skateur
            foreach (var id in new[] { "beanie-stripes", "hoodie-purple", "pants-green", "start-shoes" }) all.Wear(id);
            all.Skin = 3; all.HairStyle = 0; all.HairColor = 4;
            game.Progress = all;
            game.OpenProfile();
            for (int i = 0; i < 3; i++) yield return null;
            yield return Shot("003-garde-robe-1");
            foreach (var id in new[] { "start-head", "hoodie-gold", "start-legs", "shoes-yellow" }) all.Wear(id);
            all.Skin = 1; all.HairStyle = 1; all.HairColor = 1;
            game.Progress = all;
            game.OpenProfile();
            for (int i = 0; i < 3; i++) yield return null;
            yield return Shot("003-garde-robe-2");
            game.Progress = six;
            Object.FindAnyObjectByType<PalmStreet.Game.Hud>().ShowTab(4);
            yield return null;
            yield return Shot("0-profil-planches");
            Object.FindAnyObjectByType<PalmStreet.Game.Hud>().ShowTab(0);
            Object.FindAnyObjectByType<PalmStreet.Game.Hud>().ShowSection(1);
            for (int i = 0; i < 40; i++) yield return null;   // le skateur de démonstration est en plein FLIP
            yield return Shot("0-profil-figures");
            Object.FindAnyObjectByType<PalmStreet.Game.Hud>().ShowSection(0);
            game.Progress = saved;
            game.ToMenu();

            game.OpenProfile();
            for (int i = 0; i < 3; i++) yield return null;
            yield return Shot("1-profil");
            Object.FindAnyObjectByType<PalmStreet.Game.Hud>().ShowSection(2);
            yield return null;
            yield return Shot("1-reglages");
            Object.FindAnyObjectByType<PalmStreet.Game.Hud>().ShowSection(0);
            game.ToMenu();

            game.StartRun(false);
            yield return Steps(run, 100, false);
            yield return Shot("2-depart");
            game.OpenGameSettings(true);
            yield return Shot("2-reglages-partie");
            game.OpenGameSettings(false);
            yield return Shot("2-depart-zoom", new Vector2(run.Skater.X, run.Skater.Y + 30));

            // un ollie image par image, cadré au même endroit pour voir monter la planche
            run.Press(); run.Release();
            for (int i = 0; i <= 36; i += 6)
            {
                yield return Shot($"2-ollie-{i:00}", new Vector2(run.Skater.X, 70));
                for (int k = 0; k < 6; k++) run.Step();
            }

            // le grand rail arc-en-ciel du départ
            var rail = run.Obstacles.Find(o => o.Kind == ObstacleKind.BigRail);
            while (rail != null && rail.X - run.Skater.X > 62) { run.Step(); if (run.Skater.X % 8 < 2) yield return null; }
            run.Press(); run.Release();
            for (int i = 0; i < 70 && run.Skater.Rail == null; i++) { run.Step(); yield return null; }
            yield return Steps(run, 30, false);
            yield return Shot("3-grind");
            yield return Shot("3-grind-zoom", new Vector2(run.Skater.X, run.Skater.Y + 30));
            // figures sur rail, sur un long rail posé exprès
            for (int g = 0; g < 900 && (!run.Skater.Grounded || run.Skater.Rail != null); g++) { run.Step(); run.Bolts.Clear(); }
            run.Obstacles.Clear();
            run.Obstacles.Add(new Obstacle { Kind = ObstacleKind.Rail, X = run.Skater.X + 40, W = 900, H = Tuning.RailHeight });
            run.Press(); run.Release();
            for (int i = 0; i < 70 && run.Skater.Rail == null; i++) run.Step();
            for (int style = 1; style < Tuning.RailNames.Length && run.Skater.Rail != null; style++)
            {
                run.Skater.GrindStyle = style;
                yield return null;
                yield return Shot($"3-rail-{style}", new Vector2(run.Skater.X, run.Skater.Y + 30));
            }
            if (run.Skater.Rail != null) run.Skater.GrindStyle = 0;
            // manual et BAIL, au sol (sans obstacle : il ne doit pas chuter avant)
            run.Obstacles.Clear();
            for (int i = 0; i < 600 && (!run.Skater.Grounded || run.Skater.Rail != null); i++) { run.Step(); run.Bolts.Clear(); run.Obstacles.Clear(); }
            run.Phase = Phase.Playing;
            run.Skater.Manual = true; run.Skater.ManualSteps = 0;
            yield return Steps(run, 10, false);
            yield return Shot("3-manual", new Vector2(run.Skater.X, run.Skater.Y + 30));
            run.Skater.Manual = false; run.Skater.Wobble = 22;
            yield return null;
            yield return Shot("3-bail", new Vector2(run.Skater.X, run.Skater.Y + 30));
            run.Skater.Wobble = 0;

            yield return Steps(run, 200, true);
            for (int k = 0; k < Tuning.TrickNames.Length; k++)   // les figures (glissé dans leur direction), saisies à 5 moments
            {
                for (int g = 0; g < 600 && !run.Skater.Grounded; g++) { run.Step(); yield return null; }
                run.Obstacles.Clear(); run.Bolts.Clear();
                run.Press(); run.Release();
                yield return Steps(run, 12, false);
                float a = Tuning.TrickSwipe[k] * Mathf.PI / 4;
                run.Swipe(Mathf.Cos(a), Mathf.Sin(a));
                Assert.AreEqual(k, run.Skater.Trick, "figure attendue");
                for (int moment = 1; moment <= 5; moment++)
                {
                    int target = run.Skater.TrickLength * moment / 6;
                    for (int g = 0; g < 600 && run.Skater.TrickStep < target && !run.Skater.Grounded; g++) { run.Step(); run.Bolts.Clear(); }
                    yield return Shot($"4-figure-{k}-{moment}", new Vector2(run.Skater.X, run.Skater.Y + 30));
                }
            }

            // 180 à deux doigts : demi-tour en l'air, puis il roule en fakie
            for (int g = 0; g < 600 && !run.Skater.Grounded; g++) { run.Step(); yield return null; }
            run.Obstacles.Clear(); run.Bolts.Clear();
            run.HalfTurn();
            for (int g = 0; g < 600 && run.Skater.TrickStep < run.Skater.TrickLength / 2; g++) { run.Step(); run.Bolts.Clear(); }
            yield return Shot("4-fakie-180", new Vector2(run.Skater.X, run.Skater.Y + 30));
            for (int g = 0; g < 600 && !run.Skater.Grounded; g++) { run.Step(); run.Bolts.Clear(); }
            yield return Steps(run, 20, false);
            yield return Shot("4-fakie-roule", new Vector2(run.Skater.X, run.Skater.Y + 30));
            run.HalfTurn();
            for (int g = 0; g < 600 && !run.Skater.Grounded; g++) { run.Step(); run.Bolts.Clear(); }

            run.Score = 6000;
            yield return Steps(run, 240, true);
            yield return Shot("5-btz");

            run.Score = 11000;
            yield return Steps(run, 240, true);
            yield return Shot("6-arcangues");

            game.StartRun(true);
            yield return Steps(run, 900, true);
            yield return Shot("7-boss");

            run.Boss.Hp = 100;
            for (int i = 0; i < 200 && run.Boss != null && run.Boss.State != BossState.Dying; i++) { run.Step(); yield return null; }
            yield return Steps(run, 12, true);
            yield return Shot("8-boom");

            // niveau 2 : NUIT NÉON, après le plat du nose grind
            yield return Steps(run, 700, true);
            run.Obstacles.RemoveAll(o => o.X > run.Skater.X + 20);
            float gx = run.Skater.X + 150;
            run.Obstacles.Add(new Obstacle { Kind = ObstacleKind.Pad, X = run.Skater.X + 60, W = Tuning.PadWidth, H = Tuning.PadHeight });
            run.Obstacles.Add(new Obstacle { Kind = ObstacleKind.Gull, X = run.Skater.X + 240, W = Tuning.GullWidth, H = Tuning.GullBody, Y = Tuning.GullHeight });
            for (int i = 0; i < 4; i++) yield return null;
            yield return Shot("8-neon");
            var hole = run.Ground.GapIn(run.Skater.X, run.Skater.X + 3000);
            if (hole != null)
            {
                while (run.Skater.X < hole.Value.X0 - 120 && run.Phase == Phase.Playing) { run.Obstacles.RemoveAll(o => !o.IsRail); run.Step(); if (run.StepCount % 3 == 0) yield return null; }
                yield return Shot("8-neon-trou");
            }

            // spec 003 : genou très plié, baissé (glissé vers le bas)
            game.StartRun(false);
            yield return Steps(run, 60, true);
            run.Press(); run.Swipe(0, -1);
            for (int i = 0; i < 20; i++) { run.Step(); run.Obstacles.Clear(); yield return null; }
            yield return Shot("003-baisse-zoom", new Vector2(run.Skater.X, run.Skater.Y + 30));
            run.Release();

            game.StartRun(false);
            yield return Steps(run, 30, true);
            run.Obstacles.Add(new Obstacle { Kind = ObstacleKind.SmallCone, X = run.Skater.X + 20, W = 22, H = 28 });
            for (int i = 0; i < 20 && run.Phase == Phase.Playing; i++) { run.Step(); yield return null; }
            for (int i = 0; i < 60; i++) yield return null;
            yield return Shot("9-wipeout");
        }

        /// <summary>Avance la partie ; invincible : saute tout seul et ne chute pas.</summary>
        static IEnumerator Steps(Run run, int n, bool invincible)
        {
            for (int i = 0; i < n && run.Phase == Phase.Playing; i++)
            {
                if (invincible) run.Obstacles.RemoveAll(o => !o.IsRail && o.X < run.Skater.X + 40 && o.X + o.W > run.Skater.X - 20 && !o.Falling);
                if (invincible && run.Skater.Grounded && run.Obstacles.Exists(o => o.IsRail && o.X - run.Skater.X is > 30 and < 70)) run.Press();
                if (invincible && run.Skater.Grounded && run.Ground.IsGap(run.Skater.X + 45)) run.Press();   // saute les trous
                run.Step();
                if (invincible && run.Phase != Phase.Playing) run.Phase = Phase.Playing;
                if (i % 2 == 0) yield return null;   // l'affichage suit (décor, particules)
            }
            yield return null;
        }

        /// <summary>zoom : gros plan carré sur ce point du monde (unités logiques), pour regarder le skateur.</summary>
        static IEnumerator Shot(string name, Vector2? zoom = null)
        {
            var cam = Camera.main;
            int w = zoom == null ? W : 600, h = zoom == null ? H : 600;
            var rt = new RenderTexture(w, h, 24);
            cam.targetTexture = rt;
            yield return null;   // le décor et l'interface se calent sur ce format
            yield return null;
            var pos = cam.transform.position; float size = cam.orthographicSize;
            if (zoom != null) { cam.transform.position = new Vector3(zoom.Value.x / 100, zoom.Value.y / 100, pos.z); cam.orthographicSize = 0.55f; }
            cam.Render();
            cam.transform.position = pos; cam.orthographicSize = size;
            RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply();
            RenderTexture.active = null;
            cam.targetTexture = null;
            File.WriteAllBytes(Path.Combine(Dir, name + ".png"), tex.EncodeToPNG());
            Object.Destroy(tex);
            rt.Release();
        }
    }
}
