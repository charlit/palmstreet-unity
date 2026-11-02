using System.Linq;
using NUnit.Framework;
using PalmStreet.Core;

namespace PalmStreet.Tests
{
    /// <summary>Spec 004 : boss de rugby (Pilier, Talonneur), ballons, tenues.</summary>
    public class RugbyTests
    {
        /// <summary>Niveau à boss de rugby, boss arrivé et en train de trottiner ; vie très haute.</summary>
        static Run Fight(int n)
        {
            var r = LevelTests.Story(n);
            r.Skater.X = Levels.Get(n).Finish - 2;
            for (int i = 0; i < 400 && (r.Boss == null || r.Boss.State != BossState.Ground); i++) { Clean(r); r.Step(); }
            Assert.IsNotNull(r.Boss, "le boss est arrivé");
            r.Boss.Hp = 100000;
            return r;
        }

        /// <summary>Retire tout sauf les ballons du boss.</summary>
        static void Clean(Run r)
        {
            r.Obstacles.RemoveAll(o => o.Kind != ObstacleKind.Ball);
            r.Bolts.Clear();
        }

        /// <summary>Joue l'attaque n° attack du boss du niveau n ; react est appelé à chaque pas de l'attaque. Vrai si le skateur a chuté.</summary>
        static bool Face(int n, int attack, System.Action<Run> react)
        {
            var r = Fight(n);
            for (int i = 0; i < 600 && r.Boss.Attack != attack; i++)   // les attaques précédentes passent sans toucher
            {
                r.Obstacles.Clear(); r.Bolts.Clear();
                if (r.Boss.Tackling) r.Boss.Offset = 300;
                r.Step(); r.Phase = Phase.Playing;
            }
            while (r.Boss.State == BossState.Rising && r.Boss.Attack != attack) { r.Obstacles.Clear(); r.Step(); }
            for (int i = 0; i < 400 && r.Boss.State != BossState.Rising; i++) { r.Obstacles.Clear(); r.Bolts.Clear(); r.Step(); }
            Assert.AreEqual(attack, r.Boss.Attack, "attaque attendue");
            for (int i = 0; i < 260 && r.Phase == Phase.Playing; i++) { Clean(r); react(r); r.Step(); }
            return r.Phase == Phase.Over;
        }

        [Test] public void Boss_AuxNiveaux2Et7()
        {
            Assert.AreEqual(BossKind.Pilier, Levels.Get(2).Boss);
            Assert.AreEqual(BossKind.Talonneur, Levels.Get(7).Boss);
            Assert.AreEqual(BossKind.Pilier, Fight(2).Boss.Kind);
            Assert.AreEqual(BossKind.Talonneur, Fight(7).Boss.Kind);
        }

        [Test] public void Boss_ViesDansLOrdreDesNiveaux()
        {
            int Hp(BossKind k) => new Boss(k).MaxHp;
            var order = new[] { BossKind.Pilier, BossKind.Crab, BossKind.Gull, BossKind.Talonneur, BossKind.Boar, BossKind.Purple };
            for (int i = 1; i < order.Length; i++) Assert.That(Hp(order[i]), Is.GreaterThan(Hp(order[i - 1])), order[i].ToString());
        }

        [Test] public void Attaques_EnRotationEtAnnoncees()
        {
            var r = Fight(2);
            var seen = new System.Collections.Generic.List<int>();
            int rising = -1;
            for (int i = 0; i < 2400 && seen.Count < 4; i++)
            {
                r.Obstacles.Clear(); r.Bolts.Clear();
                if (r.Boss.State == BossState.Rising && rising < 0) { rising = 0; seen.Add(r.Boss.Attack); }
                if (r.Boss.State == BossState.Rising) rising++; else rising = -1;
                if (r.Boss.State == BossState.Rising && rising == 30) Assert.IsTrue(r.Boss.Windup, "au moins 0,5 s d'annonce");
                if (r.Boss.Tackling) r.Boss.Offset = 300;   // le plaquage ne touche pas : on teste la rotation
                r.Step(); r.Phase = Phase.Playing;
            }
            CollectionAssert.AreEqual(new[] { 0, 1, 2, 0 }, seen, "passe, chandelle, plaquage, puis on recommence");
        }

        [Test] public void Passe_SeBaisserLEvite()
        {
            Assert.IsTrue(Face(2, 0, r => { }), "debout, la passe le touche");
            bool held = false;
            Assert.IsFalse(Face(2, 0, r => { if (!held && r.Boss.Windup) { r.Press(); held = true; } }), "baissé, la passe passe au-dessus");
        }

        [Test] public void Chandelle_SauterLEvite()
        {
            Assert.IsTrue(Face(2, 1, r => { }), "au sol, le rebond le touche");
            Assert.IsFalse(Face(2, 1, r =>
            {
                var ball = r.Obstacles.FirstOrDefault(o => o.Kind == ObstacleKind.Ball && o.Bounces < 3);
                if (ball != null && r.Skater.Grounded && ball.X - r.Skater.X < 40 && ball.X > r.Skater.X) { r.Press(); r.Release(); }
            }), "en sautant au rebond, il passe");
        }

        [Test] public void Plaquage_SauterLEvite()
        {
            Assert.IsTrue(Face(2, 2, r => { }), "au sol, le plaquage le renverse");
            Assert.IsFalse(Face(2, 2, r => { if (r.Skater.Grounded && r.Boss.Tackling && r.Boss.Offset < 80) { r.Press(); r.Release(); } }), "en sautant, il passe");
        }

        [Test] public void Talonneur_MemesAttaques()
        {
            Assert.IsTrue(Face(7, 0, r => { }));
            Assert.IsTrue(Face(7, 2, r => { }));
            Assert.IsFalse(Face(7, 2, r => { if (r.Skater.Grounded && r.Boss.Tackling && r.Boss.Offset < 80) { r.Press(); r.Release(); } }));
        }

        [Test] public void Pilier_BattuParLesSorts()
        {
            var r = Fight(2);
            r.Spells = new System.Collections.Generic.List<int> { 0, 2 };
            r.Boss.Hp = 50;
            for (int i = 0; i < 400 && r.Phase == Phase.Playing; i++)
            {
                r.Obstacles.Clear(); r.Bolts.Clear();
                r.Cast(2);   // foudre : touche où qu'il soit
                r.Step();
                if (r.Phase == Phase.Over) r.Phase = Phase.Playing;
            }
            Assert.AreEqual(Phase.Cleared, r.Phase, "à zéro, le niveau 2 est réussi");
        }

        [Test] public void Rugby_PasDeSort()
        {
            Assert.AreEqual(-1, Levels.SpellOf(BossKind.Pilier));
            Assert.AreEqual(-1, Levels.SpellOf(BossKind.Talonneur));
            var p = new Progress();
            p.Clear(2, 100); p.Clear(7, 100);
            Assert.IsEmpty(p.Spells);
            p.Clear(3, 100);
            CollectionAssert.AreEqual(new[] { 0 }, p.Spells, "le crabe donne toujours VAGUE");
        }

        [Test] public void Tenues_CinqPiecesParBoss()
        {
            foreach (var (n, boss) in new[] { (2, BossKind.Pilier), (7, BossKind.Talonneur) })
            {
                var outfit = Items.OutfitFor(n).ToList();
                Assert.AreEqual(5, outfit.Count, $"tenue du niveau {n}");
                CollectionAssert.AreEquivalent(new[] { Slot.Head, Slot.Top, Slot.Legs, Slot.Shoes, Slot.Deck }, outfit.Select(i => i.Slot));
                Assert.IsTrue(outfit.All(i => i.Boss == boss));
                var p = new Progress();
                Assert.IsFalse(outfit.Any(i => p.Owns(i.Id)), "pas avant d'avoir battu le boss");
                p.Clear(n, 100);
                Assert.IsTrue(outfit.All(i => p.Owns(i.Id)), "toutes après");
                Assert.IsTrue(p.Wear(outfit[0].Id));
            }
            Assert.AreEqual("hoodie-ocean", Items.ForLevel(2).Id, "l'objet habituel du niveau 2 reste");
            Assert.AreEqual("band-red", Items.ForLevel(7).Id, "l'objet habituel du niveau 7 reste");
            Assert.IsEmpty(Items.OutfitFor(3).ToList(), "les autres boss ne donnent pas de tenue");
            Assert.IsEmpty(Items.OutfitFor(6).ToList());
        }

        [Test] public void BallonVolant_CommeUneMouette()
        {
            var r = LevelTests.Story(2);
            for (int i = 0; i < 200; i++) { r.Obstacles.Clear(); r.Bolts.Clear(); r.Step(); }
            r.Obstacles.Add(new Obstacle { Kind = ObstacleKind.Ball, X = r.Skater.X + 60, W = Tuning.GullWidth, H = Tuning.GullBody, Y = r.Ground.Floor(r.Skater.X + 60) + Tuning.GullHeight });
            for (int i = 0; i < 40 && r.Phase == Phase.Playing; i++) { r.Obstacles.RemoveAll(o => o.Kind != ObstacleKind.Ball); r.Bolts.Clear(); r.Step(); }
            Assert.AreEqual(Phase.Over, r.Phase, "debout, le ballon le touche");

            r = LevelTests.Story(2);
            for (int i = 0; i < 200; i++) { r.Obstacles.Clear(); r.Bolts.Clear(); r.Step(); }
            r.Press();   // maintenu : ollie, puis il se baisse à la retombée
            for (int i = 0; i < 70; i++) { r.Obstacles.Clear(); r.Bolts.Clear(); r.Step(); }
            Assert.IsTrue(r.Skater.Ducking, "baissé avant l'arrivée du ballon");
            r.Obstacles.Add(new Obstacle { Kind = ObstacleKind.Ball, X = r.Skater.X + 60, W = Tuning.GullWidth, H = Tuning.GullBody, Y = r.Ground.Floor(r.Skater.X + 60) + Tuning.GullHeight });
            for (int i = 0; i < 40 && r.Phase == Phase.Playing; i++) { r.Obstacles.RemoveAll(o => o.Kind != ObstacleKind.Ball); r.Bolts.Clear(); r.Step(); }
            Assert.IsTrue(r.Skater.Ducking);
            Assert.AreEqual(Phase.Playing, r.Phase, "baissé, il passe dessous");
        }

        [Test] public void BallonsVolantsABiarritz()
        {
            var k2 = LevelTests.KindsIn(2, out _, out _);
            Assert.IsTrue(k2.Contains(ObstacleKind.Ball), "niveau 2 : ballons");
            Assert.IsFalse(k2.Contains(ObstacleKind.Gull), "niveau 2 : pas de mouette");
            var k12 = LevelTests.KindsIn(12, out _, out _);
            Assert.IsTrue(k12.Contains(ObstacleKind.Ball) && k12.Contains(ObstacleKind.Gull), "niveau 12 : ballons et mouettes");
        }
    }
}
