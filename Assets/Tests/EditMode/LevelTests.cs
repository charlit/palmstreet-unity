using System.Linq;
using NUnit.Framework;
using PalmStreet.Core;

namespace PalmStreet.Tests
{
    /// <summary>Spec 002 : niveaux, progression, figures et objets à débloquer, boss, mode infini.</summary>
    public class LevelTests
    {
        /// <summary>Pas de simulation sans obstacle ni éclair, pour tester une règle à la fois.</summary>
        static void Clean(Run r, int steps)
        {
            for (int i = 0; i < steps && r.Phase == Phase.Playing; i++) { r.Obstacles.Clear(); r.Bolts.Clear(); r.Step(); }
        }

        /// <summary>Avance jusqu'à la retombée ; échoue (au lieu de boucler) si le skateur a chuté.</summary>
        internal static void Land(Run r)
        {
            for (int i = 0; i < 400 && !r.Skater.Grounded && r.Phase == Phase.Playing; i++) { r.Obstacles.Clear(); r.Bolts.Clear(); r.Step(); }
            Assert.IsTrue(r.Skater.Grounded, $"retombée attendue (phase {r.Phase})");
        }

        internal static Run Story(int n, params int[] tricks)
        {
            var r = new Run(5) { StartRamp = false };
            r.StartLevel(Levels.Get(n), tricks);
            return r;
        }

        // ---------- US1 : table des niveaux et progression ----------

        [Test] public void Table_DouzeVillesEtBossTousLes3Niveaux()
        {
            Assert.AreEqual(12, Levels.Count);
            for (int n = 1; n <= 12; n++)
            {
                var d = Levels.Get(n);
                Assert.AreEqual(n, d.Number);
                Assert.AreEqual((Theme)(n - 1), d.Theme, "un décor par ville, dans l'ordre de la carte");
                Assert.AreEqual(n % 3 == 0 || n == 2 || n == 7 || n == 8, d.Boss != BossKind.None, "boss aux niveaux 2, 3, 6, 7, 8, 9, 12 (rugby aux 2 et 7, Pelotari au 8)");
                float seconds = d.Finish / ((d.StartSpeed + d.MaxSpeed) / 2) / 60;
                Assert.That(seconds, Is.InRange(60f, 90f), $"durée du niveau {n} (SC-002)");
                if (n > 1)
                {
                    Assert.That(d.StartSpeed, Is.GreaterThan(Levels.Get(n - 1).StartSpeed));
                    Assert.That(d.MaxSpeed, Is.GreaterThan(Levels.Get(n - 1).MaxSpeed));
                }
            }
            Assert.AreEqual(BossKind.Crab, Levels.Get(3).Boss);
            Assert.AreEqual(BossKind.Gull, Levels.Get(6).Boss);
            Assert.AreEqual(BossKind.Boar, Levels.Get(9).Boss);
            Assert.AreEqual(BossKind.Purple, Levels.Get(12).Boss);
        }

        [Test] public void Progression_UnNiveauOuvreLeSuivant()
        {
            var p = new Progress();
            Assert.IsTrue(p.IsOpen(1));
            for (int n = 2; n <= 12; n++) Assert.IsFalse(p.IsOpen(n));
            Assert.AreEqual(1, p.NextLevel);
            p.Clear(1, 900);
            Assert.IsTrue(p.IsOpen(2));
            Assert.IsFalse(p.IsOpen(3));
            Assert.AreEqual(2, p.NextLevel);
            p.Clear(1, 500);
            Assert.AreEqual(900, p.Best[0], "le meilleur score est gardé");
            p.Clear(1, 1200);
            Assert.AreEqual(1200, p.Best[0]);
        }

        // ---------- US1 : jouer un niveau ----------

        [Test] public void Niveau_VitesseSelonLaDistance()
        {
            var d = Levels.Get(1);
            Assert.AreEqual(d.StartSpeed, Run.StorySpeed(d, 0), 1e-4);
            Assert.AreEqual((d.StartSpeed + d.MaxSpeed) / 2, Run.StorySpeed(d, d.Finish / 2), 1e-4);
            Assert.AreEqual(d.MaxSpeed, Run.StorySpeed(d, d.Finish * 3), 1e-4);
            var r = Story(1);
            r.Score = 50000;
            Clean(r, 10);
            Assert.AreEqual(Run.StorySpeed(d, r.Skater.X), r.Speed, 1e-3, "le score ne change pas la vitesse");
        }

        [Test] public void Niveau_RampeDeDepartDonneDeLElanPuisSeStabilise()
        {
            var r = new Run(5);
            r.StartLevel(Levels.Get(1), new int[0]);
            Assert.AreEqual(Tuning.StartRampTop, r.Skater.Y, 1e-3, "départ en haut de la rampe");
            Clean(r, 1);
            float first = r.CurrentSpeed / r.Speed;
            Assert.That(first, Is.LessThan(0.6f), "on part lentement");
            float peak = 0, prev = 0;
            while (r.Skater.X < Tuning.StartRampLength)
            {
                Clean(r, 1);
                Assert.That(r.CurrentSpeed, Is.GreaterThanOrEqualTo(prev - 1e-4f), "la vitesse monte sur toute la rampe");
                prev = r.CurrentSpeed; peak = System.Math.Max(peak, r.CurrentSpeed / r.Speed);
            }
            Assert.That(peak, Is.GreaterThan(1.4f), "en bas de la rampe, on va plus vite que le niveau");
            Assert.AreEqual(0, r.Ground.Height(r.Skater.X + 1), 1e-3);
            Clean(r, 20);
            Assert.That(r.CurrentSpeed / r.Speed, Is.GreaterThan(1.2f), "l'élan ne coupe pas net");
            for (int i = 0; i < 5000 && r.Skater.X < Tuning.StartRampLength + Tuning.StartRunOut && r.Phase == Phase.Playing; i++) Clean(r, 1);
            Clean(r, 1);
            Assert.AreEqual(r.Ground.SpeedFactor(r.Skater.X), r.CurrentSpeed / r.Speed, 1e-3, "puis la vitesse normale du niveau");
        }

        [Test] public void Niveau_LigneDArriveeReussitLeNiveau()
        {
            var r = Story(1);
            Assert.AreEqual(Theme.Anglet, r.Theme);
            for (int i = 0; i < 20000 && r.Phase == Phase.Playing; i++) { r.Obstacles.Clear(); r.Bolts.Clear(); r.Events.Clear(); r.Step(); }
            Assert.AreEqual(Phase.Cleared, r.Phase);
            Assert.That(r.Skater.X, Is.GreaterThanOrEqualTo(Levels.Get(1).Finish));
            Assert.IsTrue(r.Events.Any(e => e.Cue == Cue.Finish));
            Assert.IsTrue(r.Events.Any(e => e.Cue == Cue.Cleared));
        }

        [Test] public void Niveau_ChuteAvantLArrivee()
        {
            var r = Story(1);
            Clean(r, 5);
            r.Obstacles.Add(new Obstacle { Kind = ObstacleKind.SmallCone, X = r.Skater.X + 20, W = 22, H = 28 });
            for (int i = 0; i < 40 && r.Phase == Phase.Playing; i++) r.Step();
            Assert.AreEqual(Phase.Over, r.Phase);
        }

        /// <summary>Obstacles vus pendant un niveau joué sans chuter.</summary>
        internal static System.Collections.Generic.HashSet<ObstacleKind> KindsIn(int n, out bool hills, out bool gaps)
        {
            var r = Story(n);
            var kinds = new System.Collections.Generic.HashSet<ObstacleKind>();
            hills = gaps = false;
            for (int i = 0; i < 8000 && r.Phase == Phase.Playing; i++)
            {
                foreach (var o in r.Obstacles) if (o.Spot == null) kinds.Add(o.Kind);   // le spot de la ville est à part
                foreach (var s in r.Ground.Segments) { hills |= s.Kind == SlopeKind.Up; gaps |= s.Kind == SlopeKind.Gap; }
                r.Obstacles.RemoveAll(o => o.X < r.Skater.X + 60);
                r.Bolts.Clear();
                if (r.Skater.Grounded && r.Ground.IsGap(r.Skater.X + 40)) r.Press();
                r.Step();
                if (r.Phase == Phase.Over) { r.Phase = Phase.Playing; r.Skater.Y = 0; r.Skater.Vy = 0; r.Skater.Grounded = true; }
                if (r.Skater.Y < 0) { r.Skater.Y = 0; r.Skater.Vy = 0; r.Skater.Grounded = !r.Ground.IsGap(r.Skater.X); }
            }
            return kinds;
        }

        [Test] public void Niveau_ObstaclesIntroduitsUnParUn()
        {
            var k1 = KindsIn(1, out bool hills1, out bool gaps1);
            Assert.IsTrue(k1.All(k => k == ObstacleKind.SmallCone), "niveau 1 : petits plots seulement");
            Assert.IsFalse(hills1, "niveau 1 : sol plat");
            var k2 = KindsIn(2, out _, out _);
            Assert.IsTrue(k2.Contains(ObstacleKind.Rail) || k2.Contains(ObstacleKind.BigRail), "niveau 2 : rails");
            Assert.IsFalse(k2.Contains(ObstacleKind.BigCone));
            var k4 = KindsIn(4, out bool hills4, out _);
            Assert.IsTrue(k4.Contains(ObstacleKind.BigCone) && hills4, "niveau 4 : grands plots et bosses");
            var k7 = KindsIn(7, out _, out bool gaps7);
            Assert.IsTrue(k7.Contains(ObstacleKind.Gull), "niveau 7 : mouettes");
            Assert.IsFalse(gaps7);
            KindsIn(10, out _, out bool gaps10);
            Assert.IsTrue(gaps10, "niveau 10 : trous");
        }

        [Test] public void Niveau_BulleDAideAuDepart()
        {
            var r = Story(2);
            Assert.IsTrue(r.Events.Any(e => e.Cue == Cue.NewObstacle && e.Text.Contains("baisser")), "spec 004 : on apprend à se baisser au niveau 2");
            Assert.IsFalse(Story(7).Events.Any(e => e.Cue == Cue.NewObstacle), "le conseil n'est plus au niveau 7");
            Assert.IsFalse(Story(1).Events.Any(e => e.Cue == Cue.NewObstacle));
        }

        // ---------- US3 : figures à débloquer ----------

        [Test] public void Figures_AucuneAuDepart()
        {
            var r = Story(1);   // aucune figure connue
            Clean(r, 2);
            r.Press(); Clean(r, 5); r.Press(); Clean(r, 5); r.Swipe(60, 50);
            Assert.IsFalse(r.Events.Any(e => e.Cue == Cue.Trick), "ni toucher ni glissé en l'air ne fait de figure");
            Assert.AreEqual(1, r.Events.Count(e => e.Cue == Cue.Ollie));
        }

        [Test] public void Figures_SeulementCellesConnues()
        {
            var r = Story(1, 0, 2);   // FLIP et 360°
            var names = new System.Collections.Generic.List<string>();
            for (int k = 0; k < Tuning.TrickNames.Length; k++)
            {
                Clean(r, 2);
                r.Press(); Clean(r, 12); RunTests.SwipeFor(r, k);
                names.AddRange(r.Events.Where(e => e.Cue == Cue.Trick).Select(e => e.Text));
                r.Events.Clear();
                Land(r);
            }
            CollectionAssert.AreEqual(new[] { "FLIP !", "360° !" }, names, "les autres glissés ne font rien");
        }

        [Test] public void Figures_DebloqueesParLesNiveaux()
        {
            var p = new Progress();
            Assert.IsEmpty(p.Tricks);
            Assert.AreEqual(0, p.Clear(1, 100).trick, "niveau 1 : FLIP");
            Assert.AreEqual(-1, p.Clear(1, 200).trick, "annoncée une seule fois");
            Assert.AreEqual(4, p.Clear(2, 100).trick, "niveau 2 : HEELFLIP");
            Assert.AreEqual(1, p.Clear(3, 100).trick, "niveau 3 : SHOVE-IT");
            Assert.AreEqual(-1, p.Clear(4, 1).trick);
            Assert.AreEqual(5, p.Clear(5, 1).trick, "niveau 5 : SHOVE-IT INVERSÉ");
            Assert.AreEqual(2, p.Clear(6, 100).trick, "niveau 6 : 360°");
            p.Clear(7, 1);
            Assert.AreEqual(6, p.Clear(8, 1).trick, "niveau 8 : VARIAL FLIP");
            Assert.AreEqual(3, p.Clear(9, 100).trick, "niveau 9 : 360° INVERSÉ");
            p.Clear(10, 1);
            Assert.AreEqual(7, p.Clear(11, 1).trick, "niveau 11 : 360 FLIP");
            CollectionAssert.AreEqual(new[] { 0, 1, 2, 3, 4, 5, 6, 7 }, p.Tricks);
        }

        [Test] public void Figures_GrindEtOutSansFigure()
        {
            var r = Story(2);
            Clean(r, 2);
            var rail = new Obstacle { Kind = ObstacleKind.Rail, X = r.Skater.X + 40, W = 100, H = Tuning.RailHeight };
            r.Obstacles.Add(rail);
            r.Press();
            for (int i = 0; i < 60 && r.Skater.Rail == null; i++) r.Step();
            Assert.AreSame(rail, r.Skater.Rail, "grind sans aucune figure connue");
            r.Press();
            Assert.IsTrue(r.Events.Any(e => e.Cue == Cue.GrindOut));
        }

        // ---------- défis des villes (une étoile chacun) ----------

        [Test] public void Defis_UnParVilleEtLisible()
        {
            for (int n = 1; n <= Levels.Count; n++)
            {
                var d = Levels.Get(n);
                Assert.That(d.Goal, Is.GreaterThan(0), $"défi du niveau {n}");
                StringAssert.DoesNotContain("{", d.ChallengeText);
            }
            Assert.AreEqual("15 éclairs", Levels.Get(1).ChallengeText);
            Assert.AreEqual("2 figures en fakie", Levels.Get(5).ChallengeText);
        }

        [Test] public void Defis_Niveau1FaisableSansFigure()
        {
            var r = Story(1);   // aucune figure : seulement rouler et ramasser les éclairs
            for (int i = 0; i < 20000 && r.Phase == Phase.Playing; i++)
            {
                r.Obstacles.RemoveAll(o => o.X < r.Skater.X + 60);
                r.Step();
            }
            Assert.AreEqual(Phase.Cleared, r.Phase);
            Assert.IsTrue(r.ChallengeDone(Levels.Get(1)), $"{r.BoltsTaken} éclairs ramassés sans sauter");
        }

        [Test] public void Defis_FiguresSurRailEtFakieComptees()
        {
            var r = Story(5, 0);
            Clean(r, 2);
            var rail = new Obstacle { Kind = ObstacleKind.Rail, X = r.Skater.X + 40, W = 200, H = Tuning.RailHeight };
            r.Obstacles.Add(rail);
            r.Press();
            for (int i = 0; i < 60 && r.Skater.Rail == null; i++) r.Step();
            Assert.AreSame(rail, r.Skater.Rail);
            r.Events.Clear();
            int combo = r.Combo;
            r.Swipe(0, 60);   // vers le haut pendant le grind : BOARDSLIDE
            var t = r.Events.Single(e => e.Cue == Cue.Trick);
            Assert.AreEqual("BOARDSLIDE !", t.Text);
            Assert.AreEqual(40 * combo, t.Points);
            r.Swipe(0, 60);
            Assert.AreEqual(1, r.RailTricks, "chaque variante une fois par rail");
            r.Swipe(-50, -50);   // en bas à gauche : FEEBLE, enchaîné sur le même rail
            Assert.AreEqual("FEEBLE !", r.Events.Last(e => e.Cue == Cue.Trick).Text);
            Assert.AreEqual(2, r.RailTricks);
            Assert.AreSame(rail, r.Skater.Rail, "le glissé ne fait pas sortir du rail");
            r.Press();
            Assert.IsTrue(r.Events.Any(e => e.Cue == Cue.GrindOut), "toucher : sortie OUT! comme avant");

            Land(r);
            r.HalfTurn();
            Land(r);
            r.Press(); Clean(r, 4); RunTests.SwipeFor(r, 0);
            Assert.AreEqual(1, r.FakieTricks);
            Assert.AreEqual(2, r.TricksDone, "180 et FLIP");
            Assert.IsFalse(r.ChallengeDone(Levels.Get(5)), "2 figures en fakie : une seule faite");
        }

        [Test] public void Rail_HuitVariantesHuitDirections()
        {
            CollectionAssert.AreEquivalent(Enumerable.Range(0, 8), Tuning.RailSwipe.Skip(1), "une direction par variante");
            Assert.AreEqual(1, Run.RailFor(1, 0.1f), "à droite : 50-50");
            Assert.AreEqual(8, Run.RailFor(1, -1), "en bas à droite : SMITH");
            Assert.AreEqual(Tuning.RailNames.Length, Tuning.RailPoints.Length);
        }

        // ---------- atterrissage, manual, spots, étoiles ----------

        [Test] public void Bail_FigureTropTardFaitPerdreLeCombo()
        {
            var r = Story(5, 0);
            Clean(r, 2);
            r.Press(); Clean(r, 5); RunTests.SwipeFor(r, 0);   // FLIP tôt : il finit, combo x2
            Land(r);
            Assert.AreEqual(2, r.Combo);
            Assert.AreEqual(0, r.Bails);
            r.Press(); Clean(r, 30); RunTests.SwipeFor(r, 0);  // FLIP 8 pas avant le sol : il ne finit pas
            Land(r);
            Assert.AreEqual(1, r.Bails);
            Assert.AreEqual(1, r.Combo, "BAIL : combo perdu");
            Assert.AreEqual(Phase.Playing, r.Phase, "pas de chute");
            Assert.IsTrue(r.Events.Any(e => e.Cue == Cue.Bail));
        }

        [Test] public void Manual_GlisseVersLeBasAvantLaRetombee()
        {
            var r = Story(5, 0, 3);
            Clean(r, 2);
            r.Press(); Clean(r, 5); RunTests.SwipeFor(r, 0);   // FLIP
            Clean(r, 3);
            r.Swipe(0, -60);                                     // vers le bas après la figure : manual demandé
            Assert.IsFalse(r.Events.Any(e => e.Text == "360° INVERSÉ !"));
            Land(r);
            Assert.IsTrue(r.Skater.Manual);
            Assert.IsTrue(r.Events.Any(e => e.Text == "MANUAL !"));
            int combo = r.Combo;
            Clean(r, Tuning.ComboTimeout + 5);
            Assert.AreEqual(combo, r.Combo, "le manual garde le combo");
            Clean(r, Tuning.ManualMax);
            Assert.IsFalse(r.Skater.Manual, "2,5 s au plus");

            r.Press(); Clean(r, 31);
            r.Swipe(0, -60);                                     // tard, sans figure : manual plutôt que 360° INVERSÉ
            Assert.IsFalse(r.Events.Any(e => e.Text == "360° INVERSÉ !"));
            Land(r);
            Assert.IsTrue(r.Skater.Manual);
            r.Press();
            Assert.IsFalse(r.Skater.Manual, "un ollie sort du manual");

            Clean(r, 4); RunTests.SwipeFor(r, 0);   // FLIP tôt (il finit avant le sol), puis vers le haut : nose manual
            Clean(r, 3);
            r.Swipe(0, 60);
            Land(r);
            Assert.IsTrue(r.Skater.Manual && r.Skater.Nose, "sur les roues avant");
            Assert.IsTrue(r.Events.Any(e => e.Text == "NOSE MANUAL !"));
        }

        [Test] public void Spot_UnParVilleGrindeJusquAuBout()
        {
            Assert.IsTrue(Levels.All.All(d => !string.IsNullOrEmpty(d.Spot)));
            Assert.AreEqual("ROCHER DE LA VIERGE", Levels.Get(2).Spot);
            var r = Story(2);
            Obstacle spot = null;
            for (int i = 0; i < 20000 && r.Phase == Phase.Playing && spot == null; i++)
            {
                r.Obstacles.RemoveAll(o => o.Spot == null && o.X < r.Skater.X + 60);
                r.Step();
                spot = r.Obstacles.Find(o => o.Spot != null);
            }
            Assert.IsNotNull(spot, "le spot apparaît au milieu du niveau");
            Assert.IsTrue(r.Events.Any(e => e.Cue == Cue.SpotAhead && e.Text == "ROCHER DE LA VIERGE"));
            r.Skater.X = spot.X + 10; r.Skater.Rail = spot; r.Skater.Y = spot.Top; r.Skater.Grounded = false;
            for (int i = 0; i < 2000 && r.Skater.Rail != null; i++) r.Step();
            Assert.AreEqual(1, r.Spots);
            Assert.AreEqual("ROCHER DE LA VIERGE", r.Events.Single(e => e.Cue == Cue.Spot).Text);
        }

        [Test] public void Etoiles_TroisParVilleEtObjetsAGagner()
        {
            var p = new Progress();
            Assert.AreEqual(0, p.TotalStars);
            Assert.IsFalse(p.Owns("cap-gold"));
            for (int n = 1; n <= 3; n++) { p.Clear(n, 1); p.Stars[n - 1] = true; }
            Assert.AreEqual(2, p.StarCount(1));
            Assert.AreEqual(6, p.TotalStars);
            Assert.IsTrue(p.Owns("cap-gold"), "6 étoiles : casquette dorée");
            Assert.IsFalse(p.Owns("hoodie-basque"));
            CollectionAssert.AreEqual(new[] { "cap-gold" }, Items.ForStars(5, 6).Select(i => i.Id));
            for (int n = 1; n <= 12; n++)
            {
                Assert.That(Levels.Get(n).ScoreGoal, Is.GreaterThan(1000), $"score visé du niveau {n}");
                p.Clear(n, 1); p.Stars[n - 1] = p.ScoreStars[n - 1] = true;
            }
            Assert.AreEqual(36, p.TotalStars);
            Assert.IsTrue(Items.All.Where(i => i.Stars > 0).All(i => p.Owns(i.Id)));
        }

        [Test] public void Infini_RampeDeDepartAvecBoost()
        {
            var r = new Run(5);
            r.Start();   // infini, avec la rampe de départ
            float peak = 0, prev = 0, prevGain = 0;
            bool accelerates = true;
            for (int i = 0; i < 400 && r.Skater.X < Tuning.StartRampLength; i++)
            {
                r.Obstacles.Clear(); r.Bolts.Clear(); r.Step();
                float f = r.CurrentSpeed / r.Speed;
                if (i > 3 && f - prev < prevGain - 0.01f) accelerates = false;   // la vitesse gagnée par pas grandit
                prevGain = f - prev; prev = f; peak = System.Math.Max(peak, f);
            }
            Assert.IsTrue(accelerates, "boost : l'accélération s'emballe dans la descente");
            Assert.That(peak, Is.GreaterThan(2.1f));
            Assert.IsTrue(r.Events.Any(e => e.Cue == Cue.Boost));
            for (int i = 0; i < 200; i++) { r.Obstacles.Clear(); r.Bolts.Clear(); r.Step(); }
            Assert.That(r.CurrentSpeed / r.Speed, Is.GreaterThan(1.6f), "l'élan dure");
        }

        [Test] public void Figures_QuatreEquipeesAuPlus()
        {
            var p = new Progress();
            Assert.IsEmpty(p.ActiveTricks);
            foreach (int n in new[] { 1, 2, 3, 5, 6 }) p.Clear(n, 1);   // FLIP, HEELFLIP, SHOVE-IT, SHOVE-IT INVERSÉ, 360°
            CollectionAssert.AreEqual(new[] { 0, 1, 2, 4 }, p.ActiveTricks, "sans choix : les 4 premières connues");
            Assert.IsFalse(p.ToggleEquip(5), "déjà 4 équipées");
            Assert.IsTrue(p.ToggleEquip(1), "retirer SHOVE-IT");
            Assert.IsTrue(p.ToggleEquip(5));
            CollectionAssert.AreEquivalent(new[] { 0, 2, 4, 5 }, p.ActiveTricks);
            Assert.IsFalse(p.ToggleEquip(7), "360 FLIP pas encore débloqué");
            p.ToggleEquip(0);
            p.Clear(8, 1);   // VARIAL FLIP débloqué : une place est libre, il s'y met
            CollectionAssert.AreEquivalent(new[] { 2, 4, 5, 6 }, p.ActiveTricks);

            var r = Story(9, p.ActiveTricks.ToArray());
            Clean(r, 2);
            r.Press(); Clean(r, 4); RunTests.SwipeFor(r, 0);
            Assert.IsFalse(r.Events.Any(e => e.Cue == Cue.Trick), "FLIP connu mais pas équipé : rien");
        }

        [Test] public void Defis_EtoileGardeeDansLaProgression()
        {
            var p = new Progress { Stars = null };
            p.Repair();
            Assert.AreEqual(Levels.Count, p.Stars.Length, "ancienne sauvegarde sans étoiles");
        }

        // ---------- US2 : un boss différent tous les 3 niveaux ----------

        /// <summary>Niveau à boss, skateur posé juste avant l'arrivée : le boss arrive.</summary>
        static Run AtBoss(int n)
        {
            var r = Story(n);
            r.Skater.X = Levels.Get(n).Finish - 2;
            Clean(r, 3);
            return r;
        }

        [Test] public void Boss_ArriveALaFinDuNiveau()
        {
            foreach (var (n, kind, hp) in new[] { (3, BossKind.Crab, 4000), (6, BossKind.Gull, 5000), (9, BossKind.Boar, 6000), (12, BossKind.Purple, 8000) })
            {
                var r = AtBoss(n);
                Assert.IsNotNull(r.Boss, $"boss du niveau {n}");
                Assert.AreEqual(kind, r.Boss.Kind);
                Assert.AreEqual(hp, r.Boss.Hp);
                Assert.IsTrue(r.Events.Any(e => e.Cue == Cue.BossIn));
                Assert.AreEqual(Phase.Playing, r.Phase, "pas d'arrivée tant que le boss vit");
            }
        }

        [Test] public void Boss_BattuReussitLeNiveau()
        {
            var r = AtBoss(3);
            r.Boss.Hp = 100;
            var all = new System.Collections.Generic.List<GameEvent>();
            for (int i = 0; i < 600 && r.Phase == Phase.Playing; i++)
            {
                all.AddRange(r.Events); r.Events.Clear();
                r.Obstacles.Clear();
                if (r.Boss != null && r.Boss.State == BossState.Rising && r.Skater.Grounded) r.Press();   // saute le crabe
                r.Step();
                r.Cast(0);   // VAGUE au sol : le crabe posé la prend
            }
            all.AddRange(r.Events);
            Assert.IsTrue(all.Any(e => e.Cue == Cue.Boom));
            Assert.AreEqual(Phase.Cleared, r.Phase);
            Assert.IsFalse(all.Any(e => e.Cue == Cue.Finish), "pas de ligne d'arrivée dans un niveau à boss");
        }

        /// <summary>Joue jusqu'à la première attaque du boss ; renvoie si le skateur a chuté.</summary>
        static bool FaceAttack(int n, System.Action<Run> react)
        {
            var r = AtBoss(n);
            r.Boss.Hp = 100000;
            bool attacked = false;
            for (int i = 0; i < 1200 && r.Phase == Phase.Playing; i++)
            {
                r.Obstacles.Clear(); r.Bolts.Clear();
                if (r.Boss.State == BossState.Rising) { attacked = true; react(r); }
                r.Step();
                if (attacked && r.Boss.State == BossState.Ground) break;   // l'attaque est passée
            }
            Assert.IsTrue(attacked, "le boss a attaqué");
            return r.Phase == Phase.Over;
        }

        [Test] public void Boss_CrabeASauter()
        {
            Assert.IsTrue(FaceAttack(3, r => { }), "sans sauter, le crabe le renverse");
            Assert.IsFalse(FaceAttack(3, r => { if (r.Skater.Grounded && r.Boss.Offset < 80) r.Press(); }), "en sautant au bon moment, il passe");
        }

        [Test] public void Boss_GoelandAEsquiverAccroupi()
        {
            Assert.IsTrue(FaceAttack(6, r => { }), "debout, le goéland le touche");
            bool pressed = false;   // un toucher maintenu : ollie, puis accroupi à la retombée
            Assert.IsFalse(FaceAttack(6, r => { if (!pressed) { r.Press(); pressed = true; } }), "accroupi, il passe dessous");
        }

        [Test] public void Boss_SanglierChargeEtBarrieres()
        {
            Assert.IsTrue(FaceAttack(9, r => { }), "sans sauter, le sanglier le renverse");
            Assert.IsFalse(FaceAttack(9, r => { if (r.Skater.Grounded && r.Boss.Offset < 95) r.Press(); }), "en sautant au bon moment, il passe");
            var r = AtBoss(9);
            r.Boss.Hp = 100000;
            bool barrier = false;
            for (int i = 0; i < 360; i++) { r.Step(); r.Phase = Phase.Playing; barrier |= r.Obstacles.Any(o => o.Kind == ObstacleKind.Barrier && o.H == 22); }
            Assert.IsTrue(barrier, "il pose des barrières basses");
            var g = AtBoss(6);
            g.Boss.Hp = 100000;
            int shells = 0;
            for (int i = 0; i < 360; i++) { g.Events.Clear(); g.Step(); g.Phase = Phase.Playing; shells += g.Events.Count(e => e.Cue == Cue.ConeDrop); }
            Assert.AreEqual(3, shells, "le goéland lâche 3 coquillages entre deux piqués");
        }

        // ---------- US5 : vêtements gagnés en finissant les niveaux ----------

        [Test] public void Objets_TenueDeDepartEtUnParNiveau()
        {
            Assert.AreEqual(5, Items.All.Count(i => i.Level == 0), "tenue de départ : un objet par emplacement");
            for (int n = 1; n <= 12; n++) Assert.AreEqual(1, Items.All.Count(i => i.Level == n && i.Boss == BossKind.None), $"un objet au niveau {n} (hors tenues de rugby)");
            Assert.AreEqual(Items.All.Length, Items.All.Select(i => i.Id).Distinct().Count());
            Assert.AreEqual("cap-red", Items.ForLevel(1).Id);
            Assert.AreEqual("deck-neon", Items.ForLevel(12).Id);
        }

        [Test] public void Objets_GagnesEtPortes()
        {
            var p = new Progress();
            Assert.IsTrue(Items.All.Where(i => i.Level == 0).All(i => p.Owns(i.Id)));
            Assert.IsFalse(p.Owns("cap-red"));
            Assert.AreEqual("start-head", p.WornIn(Slot.Head));
            Assert.IsFalse(p.Wear("cap-red"), "pas encore gagnée");
            Assert.AreEqual("cap-red", p.Clear(1, 100).item.Id);
            Assert.IsNull(p.Clear(1, 200).item, "donnée une seule fois");
            Assert.IsTrue(p.Wear("cap-red"));
            Assert.AreEqual("cap-red", p.WornIn(Slot.Head));
            Assert.IsTrue(p.Wear("start-head"), "on peut revenir à la tenue de départ");
        }

        [Test] public void Progression_RepareLesDonneesLues()
        {
            var p = new Progress { Cleared = new bool[3], Best = null, Worn = new[] { "deck-neon" }, SeenObstacles = null };
            p.Repair();
            Assert.AreEqual(12, p.Cleared.Length);
            Assert.AreEqual(12, p.Best.Length);
            Assert.AreEqual("start-deck", p.WornIn(Slot.Deck), "un objet non gagné n'est pas porté");
            Assert.IsNotNull(p.SeenObstacles);
        }

        // ---------- US4 : mode infini ----------

        static Run Endless(Theme[] cities, BossKind[] bosses, Allow allowed)
        {
            var r = new Run(9) { StartRamp = false };
            r.StartEndless(cities, bosses, new int[0], allowed);
            return r;
        }

        [Test] public void Infini_VitesseSelonLeTemps()
        {
            var a = Endless(new[] { Theme.Anglet }, new BossKind[0], Allow.None);
            var b = Endless(new[] { Theme.Anglet }, new BossKind[0], Allow.None);
            b.Score = 50000; b.NextBossScore = int.MaxValue;
            a.NextBossScore = int.MaxValue;
            Clean(a, 7200); Clean(b, 7200);
            Assert.AreEqual(a.Speed, b.Speed, 1e-4, "le score ne change pas la vitesse");
            Assert.That(a.Speed, Is.GreaterThanOrEqualTo(1.5f * Tuning.EndlessStart), "après 2 min, au moins 1,5 fois le départ");
        }

        [Test] public void Infini_DecorsDesVillesOuvertes()
        {
            var r = Endless(new[] { Theme.Anglet, Theme.Bidart }, new BossKind[0], Allow.None);
            r.NextBossScore = int.MaxValue;
            Assert.AreEqual(Theme.Anglet, r.Theme);
            r.Score = 3998; Clean(r, 3);
            Assert.AreEqual(Theme.Bidart, r.Theme);
            r.Score = 7998; Clean(r, 3);
            Assert.AreEqual(Theme.Anglet, r.Theme, "on reboucle sur les villes ouvertes");
            var solo = Endless(new[] { Theme.Anglet }, new BossKind[0], Allow.None);
            solo.Score = 20000; solo.NextBossScore = int.MaxValue; Clean(solo, 3);
            Assert.AreEqual(Theme.Anglet, solo.Theme, "une seule ville ouverte : un seul décor");
        }

        [Test] public void Infini_BossDejaRencontres()
        {
            var r = Endless(new[] { Theme.Anglet }, new[] { BossKind.Gull }, Allow.None);
            r.Score = 14998; Clean(r, 3);
            Assert.AreEqual(BossKind.Gull, r.Boss?.Kind);
            var none = Endless(new[] { Theme.Anglet }, new BossKind[0], Allow.None);
            none.Score = 14998; Clean(none, 3);
            Assert.AreEqual(BossKind.Crab, none.Boss?.Kind, "aucun boss battu : le Crabe");
        }

        [Test] public void Infini_ObstaclesDesNiveauxOuverts()
        {
            var r = Endless(new[] { Theme.Anglet }, new BossKind[0], Allow.None);
            r.NextBossScore = int.MaxValue;
            var kinds = new System.Collections.Generic.HashSet<ObstacleKind>();
            for (int i = 0; i < 6000; i++)
            {
                foreach (var o in r.Obstacles) if (o.Spot == null) kinds.Add(o.Kind);   // le spot de la ville est à part
                r.Obstacles.RemoveAll(o => o.X < r.Skater.X + 60);
                r.Step(); r.Phase = Phase.Playing;
            }
            Assert.IsTrue(kinds.All(k => k == ObstacleKind.SmallCone), "seulement le niveau 1 ouvert : petits plots");
        }

        [Test] public void CielsDeJour()
        {
            var day = new[] { Theme.Biarritz, Theme.Bidart, Theme.Guethary, Theme.Bayonne, Theme.Espelette, Theme.Cambo };
            foreach (Theme t in System.Enum.GetValues(typeof(Theme)))
                Assert.AreEqual(day.Contains(t), Levels.IsDay(t), t.ToString());
        }
    }
}
