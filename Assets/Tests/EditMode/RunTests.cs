using System.Linq;
using NUnit.Framework;
using PalmStreet.Core;

namespace PalmStreet.Tests
{
    /// <summary>Les règles de la spec, une par test (numéros FR-xxx de specs/001-portage-palmstreet/spec.md).</summary>
    public class RunTests
    {
        static Run NewRun(int score = 0)
        {
            var r = new Run(42);
            r.Start();
            r.Score = score;
            return r;
        }

        /// <summary>Avance sans obstacle ni éclair (pour tester une règle à la fois).</summary>
        static void Clear(Run r, int steps)
        {
            // les tests ne posent que des rails normaux : tout le reste est retiré
            for (int i = 0; i < steps; i++) { r.Obstacles.RemoveAll(o => o.Kind != ObstacleKind.Rail); r.Bolts.Clear(); r.Step(); }
        }

        static void Steps(Run r, int n) { for (int i = 0; i < n; i++) r.Step(); }

        static Obstacle Rail(Run r, float ahead, float len = Tuning.RailLength) =>
            new Obstacle { Kind = ObstacleKind.Rail, X = r.Skater.X + ahead, W = len, H = Tuning.RailHeight };

        [Test] public void Fr003_VitesseMonteAvecLeScore()
        {
            Assert.AreEqual(1.7f, Run.SpeedFor(0), 1e-4);
            Assert.AreEqual(1.745f, Run.SpeedFor(100), 1e-4);
            Assert.AreEqual(1.745f, Run.SpeedFor(199), 1e-4);
            Assert.AreEqual(4.3f, Run.SpeedFor(100000), 1e-4);
        }

        [Test] public void Fr004_UnPointParPas()
        {
            var r = NewRun();
            Clear(r, 60);
            Assert.AreEqual(60, r.Score);
        }

        [Test] public void Fr005_DepartPlatEtSegmentsDansLesBornes()
        {
            var g = new Ground(new System.Random(1), 300);
            Assert.IsTrue(g.IsFlat(-100, 720));
            g.EnsureUpTo(30000);
            var s = g.Segments;
            for (int i = 1; i < s.Count - 1; i++)
            {
                if (s[i].Kind == SlopeKind.Flat) Assert.That(s[i].Length, Is.InRange(Tuning.FlatMin, Tuning.FlatMax));
                if (s[i].Kind == SlopeKind.Up)
                {
                    Assert.That(s[i].Length, Is.InRange(Tuning.RampMin, Tuning.RampMax));
                    Assert.That(s[i].Top, Is.InRange(Tuning.HillMin, Tuning.HillMax));
                    Assert.AreEqual(SlopeKind.Down, s[i + 1].Kind);
                    Assert.AreEqual(s[i].Top, s[i + 1].Top);
                }
                if (s[i].Kind == SlopeKind.Down) Assert.AreEqual(0, g.Height(s[i].X1), 1e-3, "la descente revient au niveau de base");
            }
        }

        [Test] public void Fr005_VitesseEnPente()
        {
            var g = new Ground(new System.Random(3), 300);
            g.EnsureUpTo(20000);
            float max = 1, min = 1;
            for (float x = 0; x < 20000; x += 2) { max = System.Math.Max(max, g.SpeedFactor(x)); min = System.Math.Min(min, g.SpeedFactor(x)); }
            Assert.That(max, Is.GreaterThan(1.2f).And.LessThanOrEqualTo(1.55f + 1e-4f));
            Assert.That(min, Is.LessThan(0.92f).And.GreaterThanOrEqualTo(0.78f - 1e-4f));
        }

        [Test] public void Fr006_OllieEtRetombee()
        {
            var r = NewRun();
            Clear(r, 5);
            r.Press(); r.Release();
            Assert.IsFalse(r.Skater.Grounded);
            float top = 0; int air = 0;
            while (!r.Skater.Grounded && air < 200) { Clear(r, 1); top = System.Math.Max(top, r.Skater.Y - r.Ground.Height(r.Skater.X)); air++; }
            Assert.That(top, Is.InRange(85f, 95f), "hauteur d'un ollie : 9,6² / (2 × 0,5) ≈ 92");
            Assert.That(air, Is.InRange(36, 40));
        }

        [Test] public void Fr006_GarderLeDoigtAccroupit()
        {
            var r = NewRun();
            Clear(r, 5);
            r.Press();
            Clear(r, 1);   // le toucher fait un ollie ; on attend la retombée en gardant le doigt
            while (!r.Skater.Grounded) Clear(r, 1);
            Assert.IsTrue(r.Skater.Ducking);
            r.Release();
            Assert.IsFalse(r.Skater.Ducking);
        }

        [Test] public void Fr007_PlotsSeulementSous2000PuisRails()
        {
            seen.Clear();
            var r = NewRun();
            for (int i = 0; i < 1500; i++) InvincibleStep(r);
            Assert.IsFalse(seen.Any(o => o.Kind == ObstacleKind.Rail), "pas de rail sous 2 000 points");

            seen.Clear();
            r = NewRun(2000);
            for (int i = 0; i < 6000; i++) InvincibleStep(r);
            int rails = seen.Count(o => o.Kind == ObstacleKind.Rail), cones = seen.Count(o => !o.IsRail);
            Assert.That(rails, Is.GreaterThan(3));
            Assert.That(cones, Is.InRange(rails * 3 / 2, rails * 3), "2 plots pour 1 rail (les grands rails prennent aussi une place)");
            var sorted = seen.OrderBy(o => o.X).ToList();
            for (int i = 1; i < sorted.Count; i++) Assert.That(sorted[i].X, Is.GreaterThan(sorted[i - 1].X + sorted[i - 1].W), "jamais superposés");
        }

        static readonly System.Collections.Generic.HashSet<Obstacle> seen = new System.Collections.Generic.HashSet<Obstacle>();

        /// <summary>Un pas où le skateur ne peut pas chuter ; vérifie au passage que chaque rail apparaît sur le plat.</summary>
        static void InvincibleStep(Run r)
        {
            r.Step();
            foreach (var o in r.Obstacles)
                if (seen.Add(o) && o.IsRail) Assert.IsTrue(r.Ground.IsFlat(o.X - 10, o.X + o.W + 10), "rail près d'une pente");
            if (r.Phase != Phase.Playing) { r.Phase = Phase.Playing; r.Skater.Y = 500; r.Skater.Grounded = false; }
            r.Skater.Rail = null;
        }

        [Test] public void Fr008_PremierObstacleGrandRail()
        {
            var r = NewRun();
            r.Step();
            Assert.AreEqual(ObstacleKind.BigRail, r.Obstacles[0].Kind);
            Assert.AreEqual(Tuning.RailLength * 3, r.Obstacles[0].W);
        }

        [Test] public void Fr009_PlotTermineLaPartie()
        {
            var r = NewRun();
            Clear(r, 2);
            r.Obstacles.Add(new Obstacle { Kind = ObstacleKind.SmallCone, X = r.Skater.X + 20, W = 22, H = 28 });
            Steps(r, 20);
            Assert.AreEqual(Phase.Over, r.Phase);
            Assert.IsTrue(r.Events.Any(e => e.Cue == Cue.Crash));
            Assert.AreEqual(r.Score, r.Best);
        }

        [Test] public void Fr010_FiguresEnLairDansLOrdreEtMultiplieesParLeCombo()
        {
            var r = NewRun();
            int[] expected = { 30, 35 * 2, 45 * 3, 45 * 4 };
            for (int k = 0; k < 4; k++)
            {
                Clear(r, 2);
                r.Press(); Clear(r, 5);
                r.Events.Clear();
                r.Press();
                var t = r.Events.Single(e => e.Cue == Cue.Trick);
                Assert.AreEqual(expected[k], t.Points);
                Assert.AreEqual(Tuning.TrickNames[k] + " !", t.Text);
                r.Press(); Clear(r, 3); r.Press();
                Assert.AreEqual(1, r.Events.Count(e => e.Cue == Cue.Trick), "une seule figure par saut");
                while (!r.Skater.Grounded) Clear(r, 1);
            }
        }

        [Test] public void Fr010_UnToucherAuSolFaitSeulementUnOllie()
        {
            var r = NewRun();
            Clear(r, 2);
            r.Press();
            Assert.IsFalse(r.Events.Any(e => e.Cue == Cue.Trick));
            Assert.AreEqual(1, r.Events.Count(e => e.Cue == Cue.Ollie));
        }

        [Test] public void Fr010_DeuxTouchersFontSautPuisFigure()
        {
            var r = NewRun();
            Clear(r, 2);
            r.Press(); Clear(r, 20); r.Press();   // même tard dans le saut
            Assert.AreEqual(1, r.Events.Count(e => e.Cue == Cue.Ollie));
            Assert.AreEqual(1, r.Events.Count(e => e.Cue == Cue.Trick));
        }

        static Run OnRail(out Obstacle rail)
        {
            var r = NewRun();
            Clear(r, 2);
            r.Obstacles.Clear();
            rail = Rail(r, 40);
            r.Obstacles.Add(rail);
            r.Press();
            for (int i = 0; i < 60 && r.Skater.Rail == null; i++) r.Step();
            Assert.AreSame(rail, r.Skater.Rail, "le skateur retombe sur le rail");
            return r;
        }

        [Test] public void Fr011_GrindJusquauBoutPuisBonus()
        {
            var r = OnRail(out var rail);
            int combo = r.Combo, score = r.Score;
            int steps = 0;
            while (r.Skater.Rail != null) { r.Step(); steps++; }
            var end = r.Events.Single(e => e.Cue == Cue.GrindEnd);
            Assert.AreEqual(20 * combo, end.Points);
            Assert.AreEqual("GRIND!", end.Text);
            Assert.AreEqual(score + steps * 3 + 20 * combo, r.Score, "1 point de score + 2 de grind par pas, puis le bonus");
            Assert.AreEqual(combo + 1, r.Combo);
            Assert.AreEqual(Phase.Playing, r.Phase);
        }

        [Test] public void Fr012_SautPendantLeGrind()
        {
            var r = OnRail(out _);
            r.Events.Clear();
            int score = r.Score;
            r.Press();
            Assert.AreEqual(score + 15, r.Score);
            Assert.AreEqual("OUT!", r.Events.Single(e => e.Cue == Cue.GrindOut).Text);
            float y0 = r.Skater.Y, top = 0;
            while (r.Skater.Vy > 0 || top == 0) { r.Step(); top = System.Math.Max(top, r.Skater.Y - y0); }
            Assert.AreEqual(87.4f * 0.85f, top, 2f, "85 % de la hauteur d'un ollie (87,4 en pas de 1/60 s)");
        }

        [Test] public void Fr013_EclairRapporte10FoisLeCombo()
        {
            var r = NewRun();
            Clear(r, 2);
            r.Combo = 3;
            r.Bolts.Add(new Bolt { X = r.Skater.X + 4, Y = r.Skater.Y + 30 });
            r.Events.Clear();
            r.Step();
            Assert.AreEqual(30, r.Events.Single(e => e.Cue == Cue.Bolt).Points);
        }

        [Test] public void Fr013_EclairsEnRangeesHorsObstaclesEtSurLesMontees()
        {
            var r = NewRun();
            var lift = new System.Collections.Generic.Dictionary<Bolt, double>();
            seen.Clear();
            for (int i = 0; i < 14000; i++)   // avant le boss
            {
                InvincibleStep(r);
                foreach (var b in r.Bolts) if (!lift.ContainsKey(b)) lift[b] = System.Math.Round(b.Y - r.Ground.Height(b.X));
                r.Bolts.RemoveAll(b => b.X < r.Skater.X + 100);
            }
            Assert.That(lift.Count, Is.GreaterThan(30));
            foreach (var b in lift.Keys)
                foreach (var o in seen)
                    Assert.IsFalse(b.X > o.X - 10 && b.X < o.X + o.W + 10, "éclair sur un obstacle");
            Assert.That(lift.Values.Distinct().Count(), Is.EqualTo(3));
        }

        [Test] public void Fr014_ComboRetombeApres22Secondes()
        {
            var r = NewRun();
            r.Combo = 4; r.ComboTimer = 0;
            Clear(r, 131);
            Assert.AreEqual(4, r.Combo);
            Clear(r, 1);
            Assert.AreEqual(1, r.Combo);
        }

        [Test] public void Fr015_Fr016_DecorsEtFullSpeed()
        {
            Assert.AreEqual(Theme.Largente, Run.ThemeFor(4999));
            Assert.AreEqual(Theme.Btz, Run.ThemeFor(5000));
            Assert.AreEqual(Theme.Arcangues, Run.ThemeFor(10000));
            Assert.AreEqual(Theme.Arcangues, Run.ThemeFor(19999));
            Assert.AreEqual(Theme.Largente, Run.ThemeFor(20000));
            var r = NewRun(4998);
            Clear(r, 3);
            Assert.AreEqual(Theme.Btz, r.Theme);
            Assert.IsTrue(r.Events.Any(e => e.Cue == Cue.FullSpeed && e.Text == "FULL SPEED"));
        }

        [Test] public void Fr017_BossA15000()
        {
            var r = NewRun(14998);
            Clear(r, 3);
            Assert.IsNotNull(r.Boss);
            Assert.AreEqual(8000, r.Boss.Hp);
            Assert.IsTrue(r.Events.Any(e => e.Cue == Cue.BossIn && e.Text == "BOSS !"));
            int count = r.Obstacles.Count;
            for (int i = 0; i < 400; i++) { r.Step(); if (r.Phase != Phase.Playing) { r.Phase = Phase.Playing; } }
            Assert.IsFalse(r.Obstacles.Any(o => o.Kind != ObstacleKind.DroppedCone && o.X > r.Skater.X + Run.SpawnAhead), "plus d'obstacles normaux");
            Assert.IsTrue(r.Ground.IsFlat(r.Skater.X + Run.SpawnAhead + 300, r.Skater.X + 3000), "sol plat");
        }

        [Test] public void Fr018_DentsEtEnvol()
        {
            var r = new Run(7);
            r.Start(bossNow: true);
            int teeth = 0, drops = 0, hp = 0;
            for (int i = 0; i < Boss.ArriveSteps + 600 + 400; i++)
            {
                r.Obstacles.RemoveAll(o => !o.Falling && o.Kind == ObstacleKind.DroppedCone && o.X < r.Skater.X + 40);
                r.Step();
                r.Phase = Phase.Playing;
                teeth += r.Events.Count(e => e.Cue == Cue.Tooth);
                drops += r.Events.Count(e => e.Cue == Cue.ConeDrop);
                hp = r.Boss.Hp;
                r.Events.Clear();
            }
            Assert.AreEqual((600 + 400) / Tuning.ToothEvery, teeth, 1);
            Assert.AreEqual(4, drops, "le boss s'envole à 10 s et lâche 4 plots");
            Assert.AreEqual(8000 - 100 * (teeth - 2), hp, 300, "100 de dégâts par dent arrivée");
        }

        [Test] public void Fr019_BossBattu()
        {
            var r = new Run(7);
            r.Start(bossNow: true);
            r.Step();
            r.Boss.Hp = 100;
            int guard = 0;
            while (!r.Events.Any(e => e.Cue == Cue.Boom) && guard++ < 1000) { r.Events.Clear(); r.Step(); }
            var boom = r.Events.Single(e => e.Cue == Cue.Boom);
            Assert.AreEqual(5000, boom.Points);
            var nose = r.Obstacles.Single(o => o.Kind == ObstacleKind.NoseRail);
            Assert.AreEqual(Tuning.RailLength * 15, nose.W);
            Assert.IsTrue(r.Ground.IsFlat(r.Skater.X, r.Skater.X + 2500));
            Assert.AreEqual(r.Score + 15000, r.NextBossScore);
            Clear(r, 100);
            Assert.IsNull(r.Boss);
        }

        [Test] public void Fr020_RaccourciDuSoleil()
        {
            var r = new Run(1);
            r.Start(bossNow: true);
            r.Step();
            Assert.IsNotNull(r.Boss);
            Assert.AreEqual(Theme.Arcangues, r.Theme);
        }

        [Test] public void LePlotLacheTombeSurLaPente()
        {
            var r = NewRun();
            r.Ground.EnsureUpTo(5000);
            var up = r.Ground.Segments.First(s => s.Kind == SlopeKind.Up);
            float x = up.X0 + up.Length / 2;
            var o = new Obstacle { Kind = ObstacleKind.DroppedCone, X = x - 13, W = 26, H = 34, Y = 300, Falling = true };
            r.Obstacles.Add(o);
            r.Skater.Y = 0;
            for (int i = 0; i < 80; i++) { r.Step(); r.Phase = Phase.Playing; }
            Assert.IsFalse(o.Falling);
            Assert.AreEqual(r.Ground.Height(x), o.Y, 1e-3);
        }

        [Test] public void Fr027_VetementsDebloquesParLesPoints()
        {
            int total = Unlocks.Colors.Length + Unlocks.Heads.Length + Unlocks.Legs.Length;
            Assert.That(Unlocks.CountOpen(0), Is.GreaterThan(0).And.LessThan(total), "une tenue de départ, le reste à gagner");
            Assert.AreEqual(total, Unlocks.CountOpen(int.MaxValue));
            int prev = 0;
            for (int pts = 0; pts <= 30000; pts += 500) { int n = Unlocks.CountOpen(pts); Assert.That(n, Is.GreaterThanOrEqualTo(prev)); prev = n; }
            Assert.IsFalse(Unlocks.Open(Unlocks.Heads[1], Unlocks.Heads[1] - 1));
            Assert.IsTrue(Unlocks.Open(Unlocks.Heads[1], Unlocks.Heads[1]));
        }

        /// <summary>Partie au niveau 2 avec la route d'aujourd'hui : plat jusqu'à x0, trou de x0 à x1, puis plat.</summary>
        static Run NeonWithGap(float x0, float x1)
        {
            var r = NewRun();
            Clear(r, 2);
            r.Level = 2;
            r.Score = 16000; r.NextBossScore = int.MaxValue;   // vitesse du niveau 2, pas de boss
            var segs = r.Ground.Segments;
            segs.Clear();
            segs.Add(new Segment { Kind = SlopeKind.Flat, X0 = -1000, X1 = x0 });
            segs.Add(new Segment { Kind = SlopeKind.Gap, X0 = x0, X1 = x1 });
            segs.Add(new Segment { Kind = SlopeKind.Flat, X0 = x1, X1 = x1 + 5000 });
            r.Obstacles.Clear(); r.Bolts.Clear();
            return r;
        }

        [Test] public void Fr029_LeBossBattuOuvreLeNiveauNeon()
        {
            var r = new Run(7);
            r.Start(bossNow: true);
            r.Step();
            r.Boss.Hp = 100;
            var all = new System.Collections.Generic.List<GameEvent>();
            for (int i = 0; i < 400; i++) { all.AddRange(r.Events); r.Events.Clear(); r.Obstacles.RemoveAll(o => !o.IsRail); r.Step(); r.Phase = Phase.Playing; }
            Assert.IsTrue(all.Any(e => e.Cue == Cue.Level && e.Text == "NIVEAU 2"));
            Assert.AreEqual(2, r.Level);
            Assert.AreEqual(Theme.Neon, r.Theme);
            Assert.AreEqual(5.0f, Run.SpeedFor(100000, 2), 1e-4);
            Assert.IsTrue(r.Ground.Gaps);
        }

        [Test] public void Fr030_TrousDansLaRoute()
        {
            var g = new Ground(new System.Random(5), 300) { Gaps = true };
            g.EnsureUpTo(40000);
            var gaps = g.Segments.Where(s => s.Kind == SlopeKind.Gap).ToList();
            Assert.That(gaps.Count, Is.GreaterThan(5));
            foreach (var s in gaps) Assert.That(s.Length, Is.InRange(Tuning.GapMin, Tuning.GapMax));
            Assert.AreEqual(Ground.Abyss, g.Height(gaps[0].X0 + 10));
            Assert.AreEqual(0, g.Floor(gaps[0].X0 + 10));

            var fall = NeonWithGap(100, 180);   // il roule sans sauter : il tombe dans le trou
            for (int i = 0; i < 200 && fall.Phase == Phase.Playing; i++) { fall.Bolts.Clear(); fall.Step(); }
            Assert.AreEqual(Phase.Over, fall.Phase);
            Assert.That(fall.Skater.X, Is.InRange(100f, 200f));

            var jump = NeonWithGap(100, 180);   // il saute juste avant le bord : il passe
            while (jump.Skater.X < 100 - 25) { jump.Bolts.Clear(); jump.Step(); }
            jump.Press(); jump.Release();
            for (int i = 0; i < 120; i++) { jump.Bolts.Clear(); jump.Obstacles.Clear(); jump.Step(); }
            Assert.AreEqual(Phase.Playing, jump.Phase);
            Assert.That(jump.Skater.X, Is.GreaterThan(180f));
        }

        [Test] public void Fr031_MouetteAHauteurDeTete()
        {
            var r = NeonWithGap(5000, 5100);
            r.Obstacles.Add(new Obstacle { Kind = ObstacleKind.Gull, X = r.Skater.X + 60, W = Tuning.GullWidth, H = Tuning.GullBody, Y = Tuning.GullHeight });
            for (int i = 0; i < 40 && r.Phase == Phase.Playing; i++) { r.Bolts.Clear(); r.Step(); }
            Assert.AreEqual(Phase.Over, r.Phase, "debout, la mouette le touche");

            r = NeonWithGap(5000, 5100);
            r.Obstacles.Add(new Obstacle { Kind = ObstacleKind.Gull, X = r.Skater.X + 60, W = Tuning.GullWidth, H = Tuning.GullBody, Y = Tuning.GullHeight });
            r.Press(); r.Release(); while (!r.Skater.Grounded) r.Step();   // on attend d'être au sol
            r.Obstacles.Clear();
            r.Obstacles.Add(new Obstacle { Kind = ObstacleKind.Gull, X = r.Skater.X + 60, W = Tuning.GullWidth, H = Tuning.GullBody, Y = Tuning.GullHeight });
            r.Press();   // maintenu : il s'accroupit
            for (int i = 0; i < 12; i++) r.Step();
            r.Events.Clear();
            for (int i = 0; i < 60; i++) { r.Bolts.Clear(); r.Step(); }
            Assert.IsTrue(r.Skater.Ducking);
            Assert.AreEqual(Phase.Playing, r.Phase, "accroupi, il passe dessous");
        }

        [Test] public void Fr032_TremplinMegaAirEtDeuxFigures()
        {
            var r = NeonWithGap(5000, 5100);
            r.Obstacles.Add(new Obstacle { Kind = ObstacleKind.Pad, X = r.Skater.X + 10, W = Tuning.PadWidth, H = Tuning.PadHeight });
            r.Events.Clear();
            for (int i = 0; i < 10 && r.Skater.Grounded; i++) r.Step();
            var mega = r.Events.Single(e => e.Cue == Cue.MegaAir);
            Assert.AreEqual(Tuning.MegaAirPoints, mega.Points);
            r.Press(); r.Release();
            while (r.Skater.Trick >= 0) r.Step();
            r.Press(); r.Release();
            Assert.AreEqual(2, r.Events.Count(e => e.Cue == Cue.Trick), "deux figures pendant un méga air");
            r.Press(); r.Release();
            Assert.AreEqual(2, r.Events.Count(e => e.Cue == Cue.Trick));
        }

        [Test] public void Fr030_NiveauNeonLongtempsSansBloquer()
        {
            // bug corrigé : un obstacle posé juste après un trou retrouvait le même trou et bouclait sans fin
            var r = new Run(11);
            r.Start();
            r.Level = 2; r.Score = 16000; r.NextBossScore = int.MaxValue; r.Ground.Gaps = true;
            seen.Clear();
            for (int i = 0; i < 20000; i++)
            {
                if (r.Skater.Grounded && r.Ground.IsGap(r.Skater.X + 40)) r.Press();
                InvincibleStep(r);
                if (r.Skater.Y < 0) { r.Skater.Y = 0; r.Skater.Vy = 0; r.Skater.Grounded = !r.Ground.IsGap(r.Skater.X); }
            }
            Assert.That(seen.Count(o => o.Kind == ObstacleKind.Gull), Is.GreaterThan(3));
            Assert.That(seen.Count(o => o.Kind == ObstacleKind.Pad), Is.GreaterThan(3));
            foreach (var o in seen.Where(o => o.Kind != ObstacleKind.Gull))
                Assert.IsNull(r.Ground.GapIn(o.X, o.X + o.W) is Segment h && h.X0 > r.Skater.X - 300 ? (Segment?)h : null, "obstacle posé sur un trou");
        }
    }
}
