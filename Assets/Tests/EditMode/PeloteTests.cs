using System.Linq;
using NUnit.Framework;
using PalmStreet.Core;

namespace PalmStreet.Tests
{
    /// <summary>Spec 005 : LE PELOTARI, le mode « planche en main » (bouclier, parade, boomerang, saut) et la pelote volante.</summary>
    public class PeloteTests
    {
        /// <summary>Niveau 8, Pelotari arrivé et en train de prendre son élan ; vie très haute.</summary>
        static Run Fight()
        {
            var r = LevelTests.Story(8);
            r.Skater.X = Levels.Get(8).Finish - 2;
            for (int i = 0; i < 400 && (r.Boss == null || r.Boss.State != BossState.Ground); i++) { Clean(r); r.Step(); }
            Assert.IsNotNull(r.Boss, "le Pelotari est arrivé");
            r.Boss.Hp = 100000;
            return r;
        }

        static void Clean(Run r) { r.Obstacles.RemoveAll(o => o.Kind != ObstacleKind.Pelote); r.Bolts.Clear(); }

        /// <summary>Joue l'attaque attack (0 lancer, 1 rebond) ; react à chaque pas depuis l'annonce. Vrai si le skateur a chuté.</summary>
        static bool Face(int attack, System.Action<Run> react, out Run run)
        {
            var r = run = Fight();
            for (int i = 0; i < 800 && !(r.Boss.Attack == attack && r.Boss.State == BossState.Rising); i++) { r.Obstacles.Clear(); r.Bolts.Clear(); r.Step(); }
            Assert.AreEqual(attack, r.Boss.Attack);
            for (int i = 0; i < 200 && r.Phase == Phase.Playing; i++) { Clean(r); react(r); r.Step(); }
            return r.Phase == Phase.Over;
        }

        static Obstacle Pelote(Run r) => r.Obstacles.FirstOrDefault(o => o.Kind == ObstacleKind.Pelote && o.FromBoss && !o.Reflected && o.X > r.Skater.X);

        [Test] public void Pelotari_AuNiveau8()
        {
            Assert.AreEqual(BossKind.Pelotari, Levels.Get(8).Boss);
            Assert.AreEqual(-1, Levels.SpellOf(BossKind.Pelotari), "pas de sort");
            int Hp(BossKind k) => new Boss(k).MaxHp;
            Assert.That(Hp(BossKind.Pelotari), Is.GreaterThan(Hp(BossKind.Talonneur)).And.LessThan(Hp(BossKind.Boar)));
            Assert.AreEqual(BossKind.Pelotari, Fight().Boss.Kind);
        }

        [Test] public void Attaques_AlterneesEtAnnoncees()
        {
            var r = Fight();
            var seen = new System.Collections.Generic.List<int>();
            int rising = -1;
            for (int i = 0; i < 1500 && seen.Count < 3; i++)
            {
                r.Obstacles.Clear(); r.Bolts.Clear();
                if (r.Boss.State == BossState.Rising && rising < 0) { rising = 0; seen.Add(r.Boss.Attack); }
                if (r.Boss.State == BossState.Rising) rising++; else rising = -1;
                if (rising == 30) Assert.IsTrue(r.Boss.Windup, "au moins 0,5 s d'annonce");
                r.Step();
            }
            CollectionAssert.AreEqual(new[] { 0, 1, 0 }, seen, "lancer, rebond, lancer");
        }

        [Test] public void AuDebutDuCombat_APied_PuisSurSaPlanche()
        {
            var r = Fight();
            Assert.IsTrue(r.OnFoot && r.Skater.OnFoot, "à pied pendant le combat");
            r.Spells = new System.Collections.Generic.List<int> { 2 };
            r.Boss.Hp = 10;
            Assert.IsTrue(r.Cast(2), "les sorts marchent à pied");
            for (int i = 0; i < 200 && r.Phase == Phase.Playing; i++) { r.Obstacles.Clear(); r.Step(); }
            Assert.AreEqual(Phase.Cleared, r.Phase);
            Assert.IsFalse(r.OnFoot);
        }

        [Test] public void Lancer_ChuteSansReaction_EviteBaisse()
        {
            Assert.IsTrue(Face(0, r => { }, out _), "debout, le lancer le touche");
            Assert.IsFalse(Face(0, r => { if (r.Boss.Windup && !r.Skater.Ducking) r.Swipe(0, -1); }, out _), "baissé, il passe dessous");
        }

        [Test] public void Bouclier_BloqueSansDegats()
        {
            bool pressed = false;
            Assert.IsFalse(Face(0, r => { if (!pressed && r.Boss.Windup) { r.Press(); pressed = true; } }, out var run), "bouclier levé tôt : bloqué");
            Assert.AreEqual(100000, run.Boss.Hp, "un simple blocage ne blesse pas le boss");
        }

        [Test] public void Parade_RenvoieLaPeloteEtBlesse()
        {
            bool pressed = false;
            Assert.IsFalse(Face(0, r =>
            {
                var p = Pelote(r);
                if (!pressed && p != null && p.X - r.Skater.X < 40) { r.Press(); pressed = true; }
            }, out var run), "parade : pas de chute");
            Assert.AreEqual(100000 - Tuning.ParryDamage, run.Boss.Hp, "la pelote renvoyée blesse le boss");
        }

        [Test] public void Rebond_ChuteSansReaction_EviteEnSautant()
        {
            Assert.IsTrue(Face(1, r => { }, out _), "au sol, le rebond le touche");
            bool jumped = false;
            Assert.IsFalse(Face(1, r =>
            {
                var p = Pelote(r);
                if (!jumped && p != null && p.Bounces < 6 && r.Skater.Grounded && p.X - r.Skater.X < 45) { r.Press(); r.Step(); r.Release(); jumped = true; }
            }, out _), "en sautant, il passe");
        }

        [Test] public void Toucher_Saute_Maintenir_NeSautePas()
        {
            var r = Fight();
            r.Press(); r.Step(); r.Release();
            Assert.IsFalse(r.Skater.Grounded, "toucher bref : saut");
            for (int i = 0; i < 80; i++) { r.Obstacles.Clear(); r.Step(); }
            r.Press();
            for (int i = 0; i < 20; i++) { r.Obstacles.Clear(); r.Step(); }
            Assert.IsTrue(r.Skater.Shield && r.Skater.Grounded, "doigt maintenu : bouclier, pas de saut");
            r.Release();
            Assert.IsTrue(r.Skater.Grounded, "relâché après un appui long : pas de saut");
        }

        [Test] public void Boomerang_BlesseUneFoisEtRevient()
        {
            var r = Fight();
            r.Swipe(1, 0);
            Assert.IsNotNull(r.Skater.Board);
            var first = r.Skater.Board;
            r.Swipe(1, 0);
            Assert.AreSame(first, r.Skater.Board, "un seul boomerang à la fois");
            r.Press();
            Assert.IsFalse(r.Skater.Shield, "pas de bouclier pendant le vol");
            r.Release();
            int steps = 0;
            while (r.Skater.Board != null && steps < 200) { r.Obstacles.Clear(); r.Step(); steps++; }
            Assert.IsNull(r.Skater.Board, "la planche revient");
            Assert.That(steps, Is.LessThan(90), "en moins de 1,5 s");
            Assert.AreEqual(100000 - Tuning.BoomerangDamage, r.Boss.Hp, "un coup par lancer");
        }

        [Test] public void APied_PasDeFigure()
        {
            var r = Fight();
            r.Press(); r.Step(); r.Release();
            for (int i = 0; i < 6; i++) { r.Obstacles.Clear(); r.Step(); }
            r.Swipe(0.7f, 0.7f);
            r.Swipe(-1, 0.3f);
            Assert.AreEqual(0, r.TricksDone, "à pied, les glissés ne font pas de figure");
            Assert.IsNull(r.Skater.Board, "en diagonale vers le haut, pas de boomerang");
        }

        [Test] public void PeloteVolante_InterieurEtRapide()
        {
            foreach (int n in new[] { 8, 9, 10, 11 })
                Assert.IsTrue(LevelTests.KindsIn(n, out _, out _).Contains(ObstacleKind.Pelote), $"pelotes au niveau {n}");
            Assert.IsFalse(LevelTests.KindsIn(2, out _, out _).Contains(ObstacleKind.Pelote), "pas sur la côte");
            Assert.That(Tuning.PelotaFlySpeed, Is.GreaterThan(Tuning.GullSpeed), "plus rapide que la mouette");

            var r = LevelTests.Story(8);
            for (int i = 0; i < 200; i++) { r.Obstacles.Clear(); r.Bolts.Clear(); r.Step(); }
            var o = new Obstacle { Kind = ObstacleKind.Pelote, X = r.Skater.X + 120, W = Tuning.PelotaSize, H = Tuning.PelotaSize, Y = r.Ground.Floor(r.Skater.X + 120) + Tuning.GullHeight, Vx = -Tuning.PelotaFlySpeed };
            r.Obstacles.Add(o);
            for (int i = 0; i < 60 && r.Phase == Phase.Playing; i++) { r.Obstacles.RemoveAll(x => x != o); r.Bolts.Clear(); r.Step(); }
            Assert.AreEqual(Phase.Over, r.Phase, "debout, la pelote le touche");

            r = LevelTests.Story(8);
            for (int i = 0; i < 200; i++) { r.Obstacles.Clear(); r.Bolts.Clear(); r.Step(); }
            r.Press();
            for (int i = 0; i < 70; i++) { r.Obstacles.Clear(); r.Bolts.Clear(); r.Step(); }
            Assert.IsTrue(r.Skater.Ducking);
            o = new Obstacle { Kind = ObstacleKind.Pelote, X = r.Skater.X + 120, W = Tuning.PelotaSize, H = Tuning.PelotaSize, Y = r.Ground.Floor(r.Skater.X + 120) + Tuning.GullHeight, Vx = -Tuning.PelotaFlySpeed };
            r.Obstacles.Add(o);
            for (int i = 0; i < 60 && r.Phase == Phase.Playing; i++) { r.Obstacles.RemoveAll(x => x != o); r.Bolts.Clear(); r.Step(); }
            Assert.AreEqual(Phase.Playing, r.Phase, "baissé, elle passe au-dessus");
        }
    }
}
