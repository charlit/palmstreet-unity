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

        static Run Story(int n, params int[] tricks)
        {
            var r = new Run(5);
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
                Assert.AreEqual(n % 3 == 0, d.Boss != BossKind.None, "boss aux niveaux 3, 6, 9, 12");
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
        static System.Collections.Generic.HashSet<ObstacleKind> KindsIn(int n, out bool hills, out bool gaps)
        {
            var r = Story(n);
            var kinds = new System.Collections.Generic.HashSet<ObstacleKind>();
            hills = gaps = false;
            for (int i = 0; i < 8000 && r.Phase == Phase.Playing; i++)
            {
                foreach (var o in r.Obstacles) kinds.Add(o.Kind);
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
            var r = Story(7);
            Assert.IsTrue(r.Events.Any(e => e.Cue == Cue.NewObstacle && e.Text.Contains("Mouettes")));
            Assert.IsFalse(Story(1).Events.Any(e => e.Cue == Cue.NewObstacle));
        }

        // ---------- US3 : figures à débloquer ----------

        [Test] public void Figures_AucuneAuDepart()
        {
            var r = Story(1);   // aucune figure connue
            Clean(r, 2);
            r.Press(); Clean(r, 5); r.Press(); Clean(r, 5); r.Press();
            Assert.IsFalse(r.Events.Any(e => e.Cue == Cue.Trick), "un toucher en l'air ne fait rien");
            Assert.AreEqual(1, r.Events.Count(e => e.Cue == Cue.Ollie));
        }

        [Test] public void Figures_SeulementCellesConnues()
        {
            var r = Story(1, 0, 2);   // FLIP et 360°
            var names = new System.Collections.Generic.List<string>();
            for (int k = 0; k < 4; k++)
            {
                Clean(r, 2);
                r.Press(); Clean(r, 4); r.Press();
                names.AddRange(r.Events.Where(e => e.Cue == Cue.Trick).Select(e => e.Text));
                r.Events.Clear();
                while (!r.Skater.Grounded) Clean(r, 1);
            }
            CollectionAssert.AreEqual(new[] { "FLIP !", "360° !", "FLIP !", "360° !" }, names);
        }

        [Test] public void Figures_DebloqueesParLesNiveaux()
        {
            var p = new Progress();
            Assert.IsEmpty(p.Tricks);
            Assert.AreEqual(0, p.Clear(1, 100).trick, "niveau 1 : FLIP");
            Assert.AreEqual(-1, p.Clear(1, 200).trick, "annoncée une seule fois");
            Assert.AreEqual(-1, p.Clear(2, 100).trick);
            Assert.AreEqual(1, p.Clear(3, 100).trick, "niveau 3 : SHOVE-IT");
            p.Clear(4, 1); p.Clear(5, 1);
            Assert.AreEqual(2, p.Clear(6, 100).trick, "niveau 6 : 360°");
            p.Clear(7, 1); p.Clear(8, 1);
            Assert.AreEqual(3, p.Clear(9, 100).trick, "niveau 9 : 360° INVERSÉ");
            CollectionAssert.AreEqual(new[] { 0, 1, 2, 3 }, p.Tricks);
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
            foreach (var (n, kind, hp) in new[] { (3, BossKind.Crab, 4000), (6, BossKind.Gull, 6000), (9, BossKind.Boar, 7000), (12, BossKind.Purple, 8000) })
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
            for (int n = 1; n <= 12; n++) Assert.AreEqual(1, Items.All.Count(i => i.Level == n), $"un objet au niveau {n}");
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
            var r = new Run(9);
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
                foreach (var o in r.Obstacles) kinds.Add(o.Kind);
                r.Obstacles.RemoveAll(o => o.X < r.Skater.X + 60);
                r.Step(); r.Phase = Phase.Playing;
            }
            Assert.IsTrue(kinds.All(k => k == ObstacleKind.SmallCone), "seulement le niveau 1 ouvert : petits plots");
        }
    }
}
