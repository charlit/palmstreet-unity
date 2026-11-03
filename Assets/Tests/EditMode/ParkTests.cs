using System.Linq;
using NUnit.Framework;
using PalmStreet.Core;

namespace PalmStreet.Tests
{
    /// <summary>Spec 006 : le skatepark de la Barre (niveau libre, rampe immense, modules, pas de chute, 90 s, bonus de combo).</summary>
    public class ParkTests
    {
        static Run Park(int seed = 5)
        {
            var r = new Run(seed);
            r.StartLevel(Levels.Park, new[] { 0, 1, 2, 3 });
            return r;
        }

        [Test] public void HorsDeLaCarteEtDuModeInfini()
        {
            Assert.AreEqual(12, Levels.Count);
            Assert.IsFalse(Levels.All.Contains(Levels.Park));
            Assert.IsFalse(Levels.All.Any(l => l.Theme == Theme.Skatepark), "le décor du skatepark ne passe pas en infini");
            Assert.IsTrue(Levels.Park.Park);
        }

        [Test] public void RampeImmenseEtSortieRapide()
        {
            Assert.That(Tuning.ParkRampTop, Is.GreaterThanOrEqualTo(2 * Tuning.StartRampTop));
            float Exit(Run r, float rampLength)
            {
                for (int i = 0; i < 2000 && r.Skater.X < rampLength + 5; i++) { r.Obstacles.Clear(); r.Bolts.Clear(); r.Step(); }
                return r.CurrentSpeed;
            }
            var normal = new Run(5); normal.StartLevel(Levels.Get(1), new int[0]);
            float vNormal = Exit(normal, Tuning.StartRampLength) / Levels.Get(1).StartSpeed;
            float vPark = Exit(Park(), Tuning.ParkRampLength) / Levels.Park.StartSpeed;
            Assert.That(vPark, Is.GreaterThanOrEqualTo(1.5f * vNormal), $"sortie de rampe : {vPark:0.00} contre {vNormal:0.00}");
        }

        [Test] public void SessionDe90sSansChuteNiObstacleInterdit()
        {
            var r = Park();
            var kinds = new System.Collections.Generic.HashSet<ObstacleKind>();
            bool gap = false;
            int steps = 0;
            while (r.Phase == Phase.Playing && steps < 7000)
            {
                foreach (var o in r.Obstacles) kinds.Add(o.Kind);
                foreach (var s in r.Ground.Segments) gap |= s.Kind == SlopeKind.Gap;
                r.Step(); steps++;   // aucun toucher : il rentre dans tous les modules
            }
            Assert.AreEqual(Phase.Cleared, r.Phase, "la session se termine, sans chute");
            Assert.That(steps, Is.InRange(Tuning.ParkSteps - 60, Tuning.ParkSteps + 60), "90 s, à 1 s près");
            Assert.IsFalse(gap, "pas de trou");
            var allowed = new[] { ObstacleKind.Rail, ObstacleKind.BigRail, ObstacleKind.Curb, ObstacleKind.Funbox, ObstacleKind.Quarter };
            Assert.IsTrue(kinds.All(k => allowed.Contains(k)), "seulement des modules : " + string.Join(", ", kinds));
        }

        [Test] public void ChaqueModuleToutesLes30Secondes()
        {
            var r = Park(7);
            var window = new System.Collections.Generic.HashSet<ObstacleKind>();
            var seen = new System.Collections.Generic.HashSet<Obstacle>();
            bool bowl = false;
            for (int s = 1; r.Phase == Phase.Playing && s <= Tuning.ParkSteps; s++)
            {
                foreach (var o in r.Obstacles) if (seen.Add(o)) window.Add(o.Kind);
                foreach (var g in r.Ground.Segments) bowl |= g.Kind == SlopeKind.Up && g.Top < 0 && g.X0 > r.Skater.X;
                r.Step();
                if (s % 1800 == 0)
                {
                    foreach (var k in new[] { ObstacleKind.Rail, ObstacleKind.Curb, ObstacleKind.Funbox, ObstacleKind.Quarter })
                        Assert.IsTrue(window.Contains(k), $"{k} dans les 30 s finissant à {s / 60} s");
                    Assert.IsTrue(bowl, $"un bowl dans les 30 s finissant à {s / 60} s");
                    window.Clear(); bowl = false;
                }
            }
        }

        [Test] public void BonusDeCombo()
        {
            var r = Park();
            for (int i = 0; i < 1200; i++) { r.Obstacles.Clear(); r.Bolts.Clear(); r.Step(); }
            r.Press(); r.Release();
            for (int i = 0; i < 12; i++) { r.Obstacles.Clear(); r.Bolts.Clear(); r.Step(); }
            r.Combo = 8; r.ComboTimer = 0; r.Events.Clear();
            r.Swipe(1, 1);   // FLIP (figure 0)
            var trick = r.Events.First(e => e.Cue == Cue.Trick);
            Assert.AreEqual(Tuning.TrickPoints[0] * 8 + Tuning.ParkBonus * (8 - Tuning.ParkComboFrom), trick.Points, "points habituels + bonus de skatepark");
        }

        [Test] public void OuvertApresAngletEtEtoilesComptees()
        {
            var p = new Progress();
            Assert.IsFalse(p.ParkOpen);
            p.Clear(1, 100);
            Assert.IsTrue(p.ParkOpen);
            int before = p.TotalStars;
            p.ClearPark(Tuning.ParkScoreGoal, Tuning.ParkComboGoal);
            Assert.AreEqual(before + 3, p.TotalStars, "session finie, combo x8 et score : 3 étoiles");
            Assert.AreEqual(Levels.Count * 3 + 3, Progress.MaxStars);
            p.ClearPark(10, 2);
            Assert.AreEqual(Tuning.ParkScoreGoal, p.ParkBest, "le record reste");
        }
    }
}
