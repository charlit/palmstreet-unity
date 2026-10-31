using System;
using System.Collections.Generic;

namespace PalmStreet.Core
{
    public enum BossKind { None, Crab, Gull, Boar, Purple }

    /// <summary>Types d'obstacles qu'un niveau peut contenir (spec 002, FR-007).</summary>
    [Flags]
    public enum Allow { None = 0, Rails = 1, BigCones = 2, Hills = 4, Gulls = 8, Pads = 16, Gaps = 32, All = 63 }

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
    }

    /// <summary>Les 12 niveaux du mode histoire (spec 002, data-model.md), un par ville de la côte basque.</summary>
    public static class Levels
    {
        const Allow Base = Allow.Rails;
        const Allow Mid = Allow.Rails | Allow.BigCones | Allow.Hills;

        public static readonly LevelDef[] All =
        {
            L(1, "ANGLET", Theme.Anglet, 10500, 2.5f, 3.2f, Allow.None, BossKind.None, 0, null),
            L(2, "BIARRITZ", Theme.Biarritz, 10000, 2.6f, 3.3f, Base, BossKind.None, -1, "Retombe sur un rail pour grinder"),
            L(3, "BIDART", Theme.Bidart, 10500, 2.7f, 3.4f, Base, BossKind.Crab, 1, null),
            L(4, "GUÉTHARY", Theme.Guethary, 12000, 2.8f, 3.5f, Mid, BossKind.None, -1, "Grands plots et bosses : saute plus tôt"),
            L(5, "SAINT-JEAN-DE-LUZ", Theme.SaintJeanDeLuz, 13000, 2.9f, 3.6f, Mid, BossKind.None, -1, null),
            L(6, "HENDAYE", Theme.Hendaye, 12000, 3.0f, 3.7f, Mid, BossKind.Gull, 2, null),
            L(7, "BAYONNE", Theme.Bayonne, 14000, 3.1f, 3.9f, Mid | Allow.Gulls, BossKind.None, -1, "Mouettes : garde le doigt appuyé pour t'accroupir"),
            L(8, "ARCANGUES", Theme.Arcangues, 15000, 3.2f, 4.1f, Mid | Allow.Gulls | Allow.Pads, BossKind.None, -1, "Tremplin : MÉGA AIR et deux figures"),
            L(9, "ESPELETTE", Theme.Espelette, 14000, 3.4f, 4.3f, Mid | Allow.Gulls | Allow.Pads, BossKind.Boar, 3, null),
            L(10, "ASCAIN", Theme.Ascain, 16000, 3.6f, 4.5f, Allow.All, BossKind.None, -1, "Trous dans la route : saute-les"),
            L(11, "CAMBO-LES-BAINS", Theme.Cambo, 17000, 3.8f, 4.7f, Allow.All, BossKind.None, -1, null),
            L(12, "BIARRITZ LA NUIT", Theme.BiarritzNuit, 16000, 4.0f, 5.0f, Allow.All, BossKind.Purple, -1, null),
        };

        public static int Count => All.Length;

        public static LevelDef Get(int n) => All[n - 1];

        static LevelDef L(int n, string city, Theme theme, float finish, float start, float max, Allow allowed, BossKind boss, int unlocks, string tip) =>
            new LevelDef { Number = n, City = city, Theme = theme, Finish = finish, StartSpeed = start, MaxSpeed = max, Allowed = allowed, Boss = boss, Unlocks = unlocks, NewObstacle = tip };
    }
}
