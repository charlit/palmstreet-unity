using System;
using System.Collections.Generic;

namespace PalmStreet.Core
{
    /// <summary>
    /// Progression du joueur (spec 002, FR-021) : niveaux réussis, meilleurs scores, meilleur score infini, bulles
    /// d'aide vues, tenue portée. Figures et objets possédés se déduisent des niveaux réussis.
    /// Champs publics : sérialisable tel quel (JsonUtility côté Game).
    /// </summary>
    [Serializable]
    public sealed class Progress
    {
        public bool[] Cleared = new bool[12];
        public int[] Best = new int[12];
        public int EndlessBest;
        public List<int> SeenObstacles = new List<int>();
        public string[] Worn = { "start-head", "start-top", "start-legs", "start-shoes", "start-deck" };
        public int Skin, HairStyle, HairColor;

        public bool IsCleared(int n) => Cleared[n - 1];
        public bool IsOpen(int n) => n == 1 || Cleared[n - 2];

        /// <summary>Premier niveau ouvert non réussi (12 si tout est réussi).</summary>
        public int NextLevel
        {
            get
            {
                for (int n = 1; n <= Levels.Count; n++) if (!IsCleared(n)) return n;
                return Levels.Count;
            }
        }

        /// <summary>Figures connues, dans l'ordre FLIP, SHOVE-IT, 360°, 360° INVERSÉ (1 → FLIP, 3 → SHOVE-IT, 6 → 360°, 9 → 360° INVERSÉ).</summary>
        public List<int> Tricks
        {
            get
            {
                var list = new List<int>();
                foreach (var l in Levels.All) if (l.Unlocks >= 0 && IsCleared(l.Number)) list.Add(l.Unlocks);
                list.Sort();
                return list;
            }
        }

        public bool Owns(string id)
        {
            var item = Items.Get(id);
            return item != null && (item.Level == 0 || IsCleared(item.Level));
        }

        public string WornIn(Slot s) => Worn[(int)s];

        /// <summary>Porter un objet ; refusé s'il n'est pas possédé.</summary>
        public bool Wear(string id)
        {
            if (!Owns(id)) return false;
            Worn[(int)Items.Get(id).Slot] = id;
            return true;
        }

        /// <summary>Niveau réussi : garde le meilleur score, renvoie ce qui est gagné pour la première fois.</summary>
        public (int trick, Item item) Clear(int n, int score)
        {
            bool first = !Cleared[n - 1];
            Cleared[n - 1] = true;
            Best[n - 1] = Math.Max(Best[n - 1], score);
            if (!first) return (-1, null);
            return (Levels.Get(n).Unlocks, Items.ForLevel(n));
        }

        /// <summary>Remet d'aplomb une progression lue (tableaux de la bonne taille, objets portés possédés).</summary>
        public void Repair()
        {
            if (Cleared == null || Cleared.Length != Levels.Count) Array.Resize(ref Cleared, Levels.Count);
            if (Best == null || Best.Length != Levels.Count) Array.Resize(ref Best, Levels.Count);
            SeenObstacles ??= new List<int>();
            var start = new Progress().Worn;
            if (Worn == null || Worn.Length != start.Length) Worn = start;
            for (int i = 0; i < Worn.Length; i++)
                if (!Owns(Worn[i]) || Items.Get(Worn[i]).Slot != (Slot)i) Worn[i] = start[i];
        }
    }
}
