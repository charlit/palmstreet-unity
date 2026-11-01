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
            var r = new Run(42) { StartRamp = false };
            r.Start();
            r.Score = score;
            return r;
        }

        /// <summary>Glissé dans la direction de la figure k.</summary>
        internal static void SwipeFor(Run r, int k)
        {
            double a = Tuning.TrickSwipe[k] * System.Math.PI / 4;
            r.Swipe((float)System.Math.Cos(a) * 80, (float)System.Math.Sin(a) * 80);
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
            // spec 002 (FR-018) remplace la vitesse par score de 001 : en infini, elle monte avec le temps
            float k = Tuning.SpeedScale;   // tout le jeu va plus vite d'autant
            Assert.AreEqual(1.7f * k, Run.EndlessSpeed(0), 1e-4);
            Assert.AreEqual((1.7f + 0.012f * 120) * k, Run.EndlessSpeed(7200), 1e-3);
            Assert.AreEqual(5.5f * k, Run.EndlessSpeed(10000000), 1e-4);
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
            LevelTests.Land(r);
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

        [Test] public void Fr010_ChaqueGlisseFaitSaFigureMultiplieeParLeCombo()
        {
            var r = NewRun();
            for (int k = 0; k < Tuning.TrickNames.Length; k++)
            {
                r.Obstacles.Clear();   // pas de rail : 8 sauts de suite finiraient par retomber dessus
                Clear(r, 2);
                r.Press(); Clear(r, 5);
                r.Events.Clear();
                int combo = r.Combo;
                SwipeFor(r, k);
                var t = r.Events.Single(e => e.Cue == Cue.Trick);
                Assert.AreEqual(Tuning.TrickPoints[k] * combo, t.Points);
                Assert.AreEqual(Tuning.TrickNames[k] + " !", t.Text);
                Clear(r, 3); SwipeFor(r, (k + 1) % Tuning.TrickNames.Length);
                Assert.AreEqual(1, r.Events.Count(e => e.Cue == Cue.Trick), "une seule figure par saut");
                LevelTests.Land(r);
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

        [Test] public void Fr010_ToucherPuisGlisserFontSautPuisFigure()
        {
            var r = NewRun();
            Clear(r, 2);
            r.Press(); Clear(r, 20); r.Press();   // un toucher en l'air ne fait plus de figure
            Assert.IsFalse(r.Events.Any(e => e.Cue == Cue.Trick));
            r.Swipe(60, 50);   // glissé vers le haut à droite, même tard dans le saut : FLIP
            Assert.AreEqual(1, r.Events.Count(e => e.Cue == Cue.Ollie));
            Assert.AreEqual("FLIP !", r.Events.Single(e => e.Cue == Cue.Trick).Text);
        }

        [Test] public void Fr010_HuitDirectionsHuitFigures()
        {
            Assert.AreEqual(0, Run.TrickFor(1, 1), "en haut à droite : FLIP");
            Assert.AreEqual(1, Run.TrickFor(1, 0.2f), "à droite : SHOVE-IT");
            Assert.AreEqual(2, Run.TrickFor(0.1f, 1), "en haut : 360°");
            Assert.AreEqual(3, Run.TrickFor(0, -1), "en bas : 360° INVERSÉ");
            Assert.AreEqual(4, Run.TrickFor(-1, 1), "en haut à gauche : HEELFLIP");
            CollectionAssert.AreEquivalent(Enumerable.Range(0, 8), Tuning.TrickSwipe, "une direction par figure");
        }

        [Test] public void Fakie_DeuxDoigtsFont180EtLesFiguresValentPlus()
        {
            var r = NewRun();
            r.Obstacles.Clear();
            Clear(r, 2);
            r.HalfTurn();   // au sol : saute et fait le 180
            Assert.AreEqual(1, r.Events.Count(e => e.Cue == Cue.Ollie));
            var half = r.Events.Single(e => e.Cue == Cue.Trick);
            Assert.AreEqual("180 FAKIE !", half.Text);
            Assert.AreEqual(Tuning.HalfTurnPoints, half.Points);
            Assert.IsTrue(r.Skater.Fakie);
            r.HalfTurn();
            Assert.AreEqual(1, r.Events.Count(e => e.Cue == Cue.Trick), "un seul 180 par saut");
            LevelTests.Land(r);
            Assert.IsTrue(r.Skater.Fakie, "il reste en fakie après la retombée");

            r.Events.Clear();
            r.Press(); Clear(r, 5);
            int combo = r.Combo;
            SwipeFor(r, 0);
            var flip = r.Events.Single(e => e.Cue == Cue.Trick);
            Assert.AreEqual("FAKIE FLIP !", flip.Text);
            Assert.AreEqual((int)(30 * combo * 1.5f), flip.Points, "figures ×1,5 en fakie");
            LevelTests.Land(r);

            r.Events.Clear();
            r.HalfTurn();
            Assert.AreEqual("180 !", r.Events.Single(e => e.Cue == Cue.Trick).Text);
            Assert.IsFalse(r.Skater.Fakie, "un deuxième 180 le remet à l'endroit");
        }

        [Test] public void Fr010_GlisserAuSolNeFaitRien()
        {
            var r = NewRun();
            Clear(r, 2);
            r.Swipe(60, 50);
            Assert.IsFalse(r.Events.Any(e => e.Cue == Cue.Trick || e.Cue == Cue.Ollie));
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
            while (r.Skater.Rail != null && steps < 2000) { r.Step(); steps++; }
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
            for (int g = 0; g < 400 && (r.Skater.Vy > 0 || top == 0); g++) { r.Step(); top = System.Math.Max(top, r.Skater.Y - y0); }
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
            // spec 002 (FR-019) : en infini, le décor change toutes les 4 000 points parmi les villes ouvertes
            var r = NewRun(3998);
            Clear(r, 3);
            Assert.AreEqual(Theme.Biarritz, r.Theme);
            Assert.IsTrue(r.Events.Any(e => e.Cue == Cue.Theme));
            r = NewRun(4998);
            Clear(r, 3);
            Assert.IsTrue(r.Events.Any(e => e.Cue == Cue.FullSpeed && e.Text == "FULL SPEED"));
        }

        [Test] public void Fr017_BossA15000()
        {
            var r = NewRun(14998);
            Clear(r, 3);
            Assert.IsNotNull(r.Boss);
            Assert.AreEqual(Tuning.BossHp, r.Boss.Hp);
            Assert.IsTrue(r.Events.Any(e => e.Cue == Cue.BossIn && e.Text == "BOSS !"));
            int count = r.Obstacles.Count;
            for (int i = 0; i < 400; i++) { r.Step(); if (r.Phase != Phase.Playing) { r.Phase = Phase.Playing; } }
            Assert.IsFalse(r.Obstacles.Any(o => o.Kind != ObstacleKind.DroppedCone && o.X > r.Skater.X + Run.SpawnAhead), "plus d'obstacles normaux");
            Assert.IsTrue(r.Ground.IsFlat(r.Skater.X + Run.SpawnAhead + 300, r.Skater.X + 3000), "sol plat");
        }

        [Test] public void Fr018_LesFiguresTirentDroitDevantEtLeBossSEnvoleSouvent()
        {
            var r = new Run(7) { StartRamp = false };
            r.Start(bossNow: true);
            int drops = 0, flights = 0;
            for (int i = 0; i < Boss.ArriveSteps + 600; i++)
            {
                r.Obstacles.RemoveAll(o => o.Kind == ObstacleKind.DroppedCone);
                r.Step();
                r.Phase = Phase.Playing;
                Assert.IsFalse(r.Events.Any(e => e.Cue == Cue.Tooth), "plus de tir automatique");
                drops += r.Events.Count(e => e.Cue == Cue.ConeDrop);
                flights += r.Events.Count(e => e.Cue == Cue.BossFly);
                r.Events.Clear();
            }
            Assert.That(flights, Is.GreaterThanOrEqualTo(2), "s'envole toutes les 6 s environ");
            Assert.That(drops, Is.GreaterThanOrEqualTo(4));

            // au sol, une figure envoie une dent droit devant qui touche le boss posé
            for (int g = 0; g < 2000 && r.Boss.State != BossState.Ground; g++) { r.Obstacles.Clear(); r.Step(); r.Phase = Phase.Playing; }
            int hp = r.Boss.Hp;
            r.Obstacles.Clear();
            r.Press(); Clear(r, 3); SwipeFor(r, 0);
            Assert.AreEqual(1, r.Boss.Teeth.Count);
            for (int i = 0; i < 60 && r.Boss.Teeth.Count > 0; i++) { r.Obstacles.Clear(); r.Step(); }
            Assert.AreEqual(hp - Tuning.ToothDamage, r.Boss.Hp, "une dent qui arrive à hauteur du boss le touche");
        }

        [Test] public void Sorts_DebloquesParLesBossEtRecharge()
        {
            var p = new Progress();
            Assert.IsEmpty(p.Spells);
            p.Clear(3, 1); p.Clear(9, 1);
            CollectionAssert.AreEqual(new[] { 0, 2 }, p.Spells, "crabe → VAGUE, sanglier → FOUDRE");

            var r = new Run(7) { StartRamp = false };
            r.Start(bossNow: true);
            r.Spells = new System.Collections.Generic.List<int> { 2 };
            Assert.IsFalse(r.Cast(2), "pas pendant l'arrivée du boss");
            for (int i = 0; i < Boss.ArriveSteps + 2; i++) { r.Obstacles.Clear(); r.Step(); }
            int hp = r.Boss.Hp;
            Assert.IsTrue(r.Cast(2));
            Assert.AreEqual(hp - Tuning.SpellDamage[2], r.Boss.Hp, "la foudre frappe le boss où qu'il soit");
            Assert.IsFalse(r.Cast(2), "en recharge");
            Assert.IsFalse(r.Cast(0), "VAGUE pas débloquée");
            for (int i = 0; i < Tuning.SpellCooldown[2]; i++) { r.Obstacles.Clear(); r.Step(); r.Phase = Phase.Playing; }
            Assert.IsTrue(r.Cast(2), "rechargé");
        }

        [Test] public void Fr019_BossBattu()
        {
            var r = new Run(7) { StartRamp = false };
            r.Start(bossNow: true);
            r.Step();
            r.Boss.Hp = 100;
            int guard = 0;
            while (!r.Events.Any(e => e.Cue == Cue.Boom) && guard++ < 1000) { r.Events.Clear(); r.Step(); r.Cast(2); }   // la foudre l'achève
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
            var r = new Run(1) { StartRamp = false };
            r.Start(bossNow: true);
            r.Step();
            Assert.IsNotNull(r.Boss);
            Assert.AreEqual(Theme.Anglet, r.Theme);   // spec 002 : le raccourci part en infini, à score 0
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

        /// <summary>Partie au niveau 2 avec la route d'aujourd'hui : plat jusqu'à x0, trou de x0 à x1, puis plat.</summary>
        static Run NeonWithGap(float x0, float x1)
        {
            var r = NewRun();
            Clear(r, 2);
            r.Allowed = Allow.All;
            r.StepCount = 18000; r.NextBossScore = int.MaxValue;   // vitesse de l'infini après 5 min, pas de boss
            var segs = r.Ground.Segments;
            segs.Clear();
            segs.Add(new Segment { Kind = SlopeKind.Flat, X0 = -1000, X1 = x0 });
            segs.Add(new Segment { Kind = SlopeKind.Gap, X0 = x0, X1 = x1 });
            segs.Add(new Segment { Kind = SlopeKind.Flat, X0 = x1, X1 = x1 + 5000 });
            r.Obstacles.Clear(); r.Bolts.Clear();
            return r;
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
            for (int s = 0; s < 2000 && jump.Skater.X < 100 - 25; s++) { jump.Bolts.Clear(); jump.Step(); }
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
            r.Press(); r.Release(); LevelTests.Land(r);   // on attend d'être au sol
            r.Obstacles.Clear();
            // assez loin pour qu'il ait le temps de sauter, retomber et s'accroupir (le doigt maintenu)
            r.Obstacles.Add(new Obstacle { Kind = ObstacleKind.Gull, X = r.Skater.X + r.CurrentSpeed * 62, W = Tuning.GullWidth, H = Tuning.GullBody, Y = Tuning.GullHeight });
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
            SwipeFor(r, 0);
            for (int g = 0; g < 400 && r.Skater.Trick >= 0; g++) r.Step();
            SwipeFor(r, 1);
            Assert.AreEqual(2, r.Events.Count(e => e.Cue == Cue.Trick), "deux figures pendant un méga air");
            SwipeFor(r, 2);
            Assert.AreEqual(2, r.Events.Count(e => e.Cue == Cue.Trick));
        }

        [Test] public void Fr030_NiveauNeonLongtempsSansBloquer()
        {
            // bug corrigé : un obstacle posé juste après un trou retrouvait le même trou et bouclait sans fin
            var r = new Run(11) { StartRamp = false };
            r.Start();
            r.Allowed = Allow.All; r.StepCount = 18000; r.NextBossScore = int.MaxValue; r.Ground.Gaps = true;
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
