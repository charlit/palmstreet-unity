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

        [UnityTest]
        public IEnumerator CaptureKeyMoments()
        {
            yield return SceneManager.LoadSceneAsync("Game");
            yield return null;
            var game = Object.FindAnyObjectByType<PalmStreet.Game.Game>();
            Assert.IsNotNull(game, "le jeu n'a pas démarré dans la scène Game");
            game.Manual = true;
            Directory.CreateDirectory(Dir);
            var run = game.Run;

            for (int i = 0; i < 10; i++) yield return null;
            yield return Shot("1-accueil");

            game.StartRun(false);
            yield return Steps(run, 100, false);
            yield return Shot("2-depart");
            yield return Shot("2-depart-zoom", new Vector2(run.Skater.X, run.Skater.Y + 30));

            // le grand rail arc-en-ciel du départ
            var rail = run.Obstacles.Find(o => o.Kind == ObstacleKind.BigRail);
            while (rail != null && rail.X - run.Skater.X > 62) { run.Step(); if (run.Skater.X % 8 < 2) yield return null; }
            run.Press(); run.Release();
            for (int i = 0; i < 70 && run.Skater.Rail == null; i++) { run.Step(); yield return null; }
            yield return Steps(run, 30, false);
            yield return Shot("3-grind");
            yield return Shot("3-grind-zoom", new Vector2(run.Skater.X, run.Skater.Y + 30));

            yield return Steps(run, 200, true);
            for (int k = 0; k < 4; k++)   // les 4 figures, saisies au quart, à la moitié et aux trois quarts
            {
                while (!run.Skater.Grounded) { run.Step(); yield return null; }
                run.Obstacles.Clear(); run.Bolts.Clear();
                run.Press(); run.Release();
                yield return Steps(run, 4, false);
                run.Press(); run.Release();
                yield return Steps(run, 3, false);
                run.Press(); run.Release();
                Assert.AreEqual(k, run.Skater.Trick, "figure attendue");
                for (int moment = 1; moment <= 3; moment++)
                {
                    int target = run.Skater.TrickLength * moment / 4;
                    while (run.Skater.TrickStep < target) { run.Step(); run.Bolts.Clear(); }
                    yield return Shot($"4-figure-{k}-{moment}", new Vector2(run.Skater.X, run.Skater.Y + 30));
                }
            }

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
