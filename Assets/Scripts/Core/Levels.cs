using System;
using System.Collections.Generic;

namespace PalmStreet.Core
{
    public enum BossKind { None, Crab, Gull, Boar, Purple, Pilier, Talonneur, Pelotari }   // les boss de rugby (spec 004) à la fin : les index des sorts ne bougent pas

    /// <summary>Types d'obstacles qu'un niveau peut contenir (spec 002, FR-007).</summary>
    [Flags]
    public enum Allow { None = 0, Rails = 1, BigCones = 2, Hills = 4, Gulls = 8, Pads = 16, Gaps = 32, All = 63 }

    /// <summary>Défi d'une ville : réussi en finissant le niveau avec au moins Goal (rapporte une étoile).</summary>
    public enum ChallengeKind { Tricks, Fakie, GrindSeconds, Combo, RailTricks, Bolts }

    public sealed class LevelDef
    {
        public int Number;
        public string City;
        public Theme Theme;
        public float Finish, StartSpeed, MaxSpeed;
        public Allow Allowed;
        public BossKind Boss;
        public int Unlocks = -1;        // figure débloquée (index de Tuning.TrickNames) ou −1
        public string NewObstacle;      // bulle d'aide au départ, ou null
        public ChallengeKind Challenge;
        public int Goal;
        public string Spot;             // lieu connu de la ville : un grand rail à grinder jusqu'au bout
        public int ScoreGoal;           // score de la troisième étoile
        public float FlyingRate = Tuning.FlyingRate;   // part des obstacles volants (spec 004 : peu au niveau 2)

        /// <summary>Le défi en clair, ex. « 3 figures en fakie ».</summary>
        public string ChallengeText => Challenge switch
        {
            ChallengeKind.Tricks => $"{Goal} figures",
            ChallengeKind.Fakie => $"{Goal} figures en fakie",
            ChallengeKind.GrindSeconds => $"{Goal} s de grind",
            ChallengeKind.Combo => $"combo x{Goal}",
            ChallengeKind.Bolts => $"{Goal} éclairs",
            _ => $"{Goal} figures sur rail",
        };
    }

    /// <summary>Les 12 niveaux du mode histoire (spec 002, data-model.md), un par ville de la côte basque.</summary>
    public static class Levels
    {
        const Allow Base = Allow.Rails;
        const Allow Mid = Allow.Rails | Allow.BigCones | Allow.Hills;

        public static readonly LevelDef[] All =
        {
            L(1, "ANGLET", Theme.Anglet, 10500, 2.5f, 3.2f, Allow.None, BossKind.None, 0, null, ChallengeKind.Bolts, 15, "LA BARRE"),
            L(2, "BIARRITZ", Theme.Biarritz, 11000, 2.6f, 3.3f, Base | Allow.Gulls, BossKind.Pilier, 4, "Ballons : glisse vers le bas pour te baisser", ChallengeKind.Tricks, 3, "ROCHER DE LA VIERGE", 0.12f),
            L(3, "BIDART", Theme.Bidart, 11200, 2.7f, 3.4f, Base, BossKind.Crab, 1, "Retombe sur un rail pour grinder", ChallengeKind.Combo, 4, "LA CHAPELLE"),
            L(4, "GUÉTHARY", Theme.Guethary, 12000, 2.8f, 3.5f, Mid, BossKind.None, -1, "Grands plots et bosses : saute plus tôt", ChallengeKind.RailTricks, 2, "LE PORT"),
            L(5, "SAINT-JEAN-DE-LUZ", Theme.SaintJeanDeLuz, 13000, 2.9f, 3.6f, Mid, BossKind.None, 5, null, ChallengeKind.Fakie, 2, "LA DIGUE"),
            L(6, "HENDAYE", Theme.Hendaye, 12400, 3.0f, 3.7f, Mid, BossKind.Gull, 2, null, ChallengeKind.Tricks, 8, "LES DEUX JUMEAUX"),
            L(7, "BAYONNE", Theme.Bayonne, 14000, 3.1f, 3.9f, Mid | Allow.Gulls, BossKind.Talonneur, -1, null, ChallengeKind.GrindSeconds, 5, "LES REMPARTS"),
            L(8, "ARCANGUES", Theme.Arcangues, 15000, 3.2f, 4.1f, Mid | Allow.Gulls | Allow.Pads, BossKind.Pelotari, 6, "Tremplin : MÉGA AIR et deux figures", ChallengeKind.Combo, 6, "LE FRONTON"),
            L(9, "ESPELETTE", Theme.Espelette, 14000, 3.4f, 4.3f, Mid | Allow.Gulls | Allow.Pads, BossKind.Boar, 3, null, ChallengeKind.Fakie, 4, "LE CHÂTEAU"),
            L(10, "ASCAIN", Theme.Ascain, 16000, 3.6f, 4.5f, Allow.All, BossKind.None, -1, "Trous dans la route : saute-les", ChallengeKind.RailTricks, 4, "LE PONT ROMAIN"),
            L(11, "CAMBO-LES-BAINS", Theme.Cambo, 17000, 3.8f, 4.7f, Allow.All, BossKind.None, 7, null, ChallengeKind.Tricks, 15, "VILLA ARNAGA"),
            L(12, "BIARRITZ LA NUIT", Theme.BiarritzNuit, 16500, 4.0f, 5.0f, Allow.All, BossKind.Purple, -1, null, ChallengeKind.Combo, 10, "LE CASINO"),
        };

        public static int Count => All.Length;

        public static LevelDef Get(int n) => All[n - 1];

        /// <summary>Sort donné par un boss (index de Tuning.SpellNames), ou −1 : les boss de rugby n'en donnent pas (spec 004).</summary>
        public static int SpellOf(BossKind k) => k >= BossKind.Crab && k <= BossKind.Purple ? (int)k - 1 : -1;

        /// <summary>Ciel de jour : le texte du milieu de l'écran s'y écrit en sombre (spec 003, FR-004).</summary>
        public static bool IsDay(Theme t) => t == Theme.Biarritz || t == Theme.Bidart || t == Theme.Guethary
            || t == Theme.Bayonne || t == Theme.Espelette || t == Theme.Cambo;

        static LevelDef L(int n, string city, Theme theme, float finish, float start, float max, Allow allowed, BossKind boss, int unlocks, string tip, ChallengeKind challenge, int goal, string spot, float flying = Tuning.FlyingRate)
        {
            float k = Tuning.SpeedScale;   // plus rapide, et plus long d'autant : la durée d'un niveau ne change pas
            finish *= k; start *= k; max *= k;
            return new LevelDef { Number = n, City = city, Theme = theme, Finish = finish, StartSpeed = start, MaxSpeed = max, Allowed = allowed, Boss = boss, Unlocks = unlocks, NewObstacle = tip, Challenge = challenge, Goal = goal, Spot = spot, FlyingRate = flying,
                // 3e étoile : 1 point par pas sur tout le niveau, × 1,5 avec figures et grinds (× 1,1 au niveau 1, sans figure)
                ScoreGoal = (int)Math.Round(finish / ((start + max) / 2) * (n == 1 ? 1.1 : 1.5) / 100) * 100 };
        }
    }
}
