using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using PalmStreet.Core;
using PalmStreet.Game;
using UnityEngine;

namespace PalmStreet.Tests
{
    /// <summary>
    /// Essai du skateur PixelLab : quelle que soit la tenue, aucun morceau du pantin ne doit apparaître en roulant, pendant
    /// les figures, les grinds, les manuals ou la chute ; seul le dessin (« Dessin ») est visible.
    /// </summary>
    public class RiderOutfitTests
    {
        static IEnumerable<(string name, System.Action<RiderView, Skater> act)> States()
        {
            var rail = new Obstacle { Kind = ObstacleKind.Rail };
            var at = new Vector3(0, 0, 0);
            yield return ("roule", (v, s) => v.Ride(s, at, 0, 1.0f, 1.2f));
            yield return ("pousse", (v, s) => v.Ride(s, at, 0, 2.7f, 0.9f));
            yield return ("baissé", (v, s) => { s.Ducking = true; for (int i = 0; i < 10; i++) v.Ride(s, at, 0, 3 + i * 0.05f, 1.2f); });
            yield return ("bail", (v, s) => { s.Wobble = 20; v.Ride(s, at, 0, 1, 1.2f); });
            yield return ("manual", (v, s) => { s.Manual = true; v.Ride(s, at, 0, 1, 1.2f); });
            yield return ("nose manual", (v, s) => { s.Manual = true; s.Nose = true; v.Ride(s, at, 0, 1, 1.2f); });
            yield return ("saut", (v, s) => { s.Grounded = false; s.Vy = 3; v.Ride(s, at, 0, 1, 1.2f); });
            yield return ("fakie", (v, s) => { s.Fakie = true; v.Ride(s, at, 0, 1, 1.2f); });
            for (int t = 0; t < Tuning.TrickNames.Length; t++)
            {
                int trick = t;
                foreach (int step in new[] { 0, 6, 12, 18, 23 })
                    yield return ($"figure {trick} pas {step}", (v, s) => { s.Grounded = false; s.Trick = trick; s.TrickStep = step; v.Ride(s, at, 0, 1, 1.2f); });
            }
            yield return ("180", (v, s) => { s.Grounded = false; s.Trick = Tuning.HalfTurn; s.TrickStep = 10; v.Ride(s, at, 0, 1, 1.2f); });
            for (int g = 0; g < Tuning.RailNames.Length; g++)
            {
                int style = g;
                yield return ($"grind {style}", (v, s) => { s.Grounded = false; s.Rail = rail; s.GrindStyle = style; for (int i = 0; i < 20; i++) v.Ride(s, at, 0, 1 + i * 0.05f, 1.2f); });
            }
            yield return ("chute", (v, s) =>
            {
                v.Ride(s, at, 0, 1, 1.2f);
                v.Crash(s, 3);
                var ground = new Ground(new System.Random(1), -200);
                for (int i = 0; i < 40; i++) v.Fall(ground, 1 / 60f);
            });
            yield return ("relevé après chute", (v, s) =>
            {
                v.Crash(s, 3); v.Fall(new Ground(new System.Random(1), -200), 1 / 60f);
                v.Ride(new Skater(), at, 0, 2, 1.2f);
            });
            yield return ("accueil", (v, s) => v.Idle(at, 0.2f, true, 1));
            for (int t = 0; t < Tuning.TrickNames.Length; t++) { int trick = t; yield return ($"démo {trick}", (v, s) => v.Demo(trick, at, 0.8f, 1)); }
        }

        [Test]
        public void ChaqueTenueGardeLeDessinDansChaqueMouvement()
        {
            if (!RiderView.Pixel) Assert.Ignore("dessin PixelLab désactivé (riderpix = 0)");
            var problems = new List<string>();
            foreach (var item in Items.All)
            {
                var p = new Progress();
                p.Worn[(int)item.Slot] = item.Id;   // on porte l'objet sans le gagner
                var look = Wardrobe.Build(p);
                foreach (var (name, act) in States())
                {
                    var v = RiderView.Create(look, null, 20);
                    act(v, new Skater());
                    var shown = v.GetComponentsInChildren<Renderer>(false).Where(r => r.enabled && r.gameObject.name != "Dessin").ToList();
                    var drawing = v.GetComponentsInChildren<SpriteRenderer>(true).FirstOrDefault(r => r.gameObject.name == "Dessin");
                    if (shown.Count > 0) problems.Add($"{item.Id} / {name} : pantin visible ({string.Join(", ", shown.Select(r => r.gameObject.name).Distinct().Take(4))})");
                    else if (drawing == null || !drawing.enabled || drawing.sprite == null) problems.Add($"{item.Id} / {name} : pas de dessin");
                    Object.DestroyImmediate(v.gameObject);
                }
            }
            Assert.IsEmpty(problems, string.Join("\n", problems.Take(30)));
        }
    }
}
