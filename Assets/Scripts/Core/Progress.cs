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
        public bool[] Stars = new bool[12];        // 2e étoile : défi de la ville réussi
        public bool[] ScoreStars = new bool[12];   // 3e étoile : score de la ville atteint
        public List<int> Equipped = new List<int>();   // figures en l'air équipées (4 au plus), choisies dans le profil
        public const int MaxEquipped = 4;
        public int EndlessBest;
        public List<int> SeenObstacles = new List<int>();
        public string[] Worn = { "start-head", "start-top", "start-legs", "start-shoes", "start-deck" };
        public int Skin, HairStyle, HairColor;

        /// <summary>Test sur téléphone : tous les niveaux, figures et objets ouverts, sans toucher à la sauvegarde.</summary>
        public static bool UnlockAll;
        public bool IsCleared(int n) => UnlockAll || Cleared[n - 1];
        public bool IsOpen(int n) => UnlockAll || n == 1 || Cleared[n - 2];

        /// <summary>Étoiles d'une ville (0 à 3) : niveau fini, défi réussi, score atteint.</summary>
        public int StarCount(int n) => (Cleared[n - 1] ? 1 : 0) + (Stars[n - 1] ? 1 : 0) + (ScoreStars[n - 1] ? 1 : 0);

        public int TotalStars
        {
            get { int t = 0; for (int n = 1; n <= Levels.Count; n++) t += StarCount(n); return t; }
        }

        /// <summary>Premier niveau ouvert non réussi (12 si tout est réussi).</summary>
        public int NextLevel
        {
            get
            {
                for (int n = 1; n <= Levels.Count; n++) if (!IsCleared(n)) return n;
                return Levels.Count;
            }
        }

        /// <summary>Figures connues, dans l'ordre de Tuning.TrickNames (niveaux 1, 2, 3, 5, 6, 8, 9, 11).</summary>
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

        /// <summary>Sorts connus : un par boss battu (crabe → VAGUE, goéland → TORNADE, sanglier → FOUDRE, violet → BOULE DE FEU).</summary>
        public List<int> Spells
        {
            get
            {
                var list = new List<int>();
                foreach (var l in Levels.All) if (Levels.SpellOf(l.Boss) >= 0 && IsCleared(l.Number)) list.Add(Levels.SpellOf(l.Boss));
                list.Sort();
                return list;
            }
        }

        /// <summary>
        /// Figures utilisables en partie : les équipées encore connues ; si aucune n'est équipée, les 4 premières
        /// connues (nouvelle partie, ancienne sauvegarde).
        /// </summary>
        public List<int> ActiveTricks
        {
            get
            {
                var known = Tricks;
                var list = Equipped.FindAll(known.Contains);
                return list.Count > 0 ? list : known.GetRange(0, Math.Min(MaxEquipped, known.Count));
            }
        }

        /// <summary>Équipe ou retire une figure ; faux si elle n'est pas connue ou si 4 sont déjà équipées.</summary>
        public bool ToggleEquip(int trick)
        {
            if (Equipped.Count == 0) Equipped = ActiveTricks;   // on part des figures utilisées jusqu'ici
            if (Equipped.Remove(trick)) return true;
            if (!Tricks.Contains(trick) || Equipped.Count >= MaxEquipped) return false;
            Equipped.Add(trick);
            return true;
        }

        public bool Owns(string id)
        {
            var item = Items.Get(id);
            if (item == null) return false;
            if (item.Stars > 0) return UnlockAll || TotalStars >= item.Stars;
            return item.Level == 0 || IsCleared(item.Level);
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
            int unlocks = Levels.Get(n).Unlocks;
            if (unlocks >= 0 && Equipped.Count > 0 && Equipped.Count < MaxEquipped && !Equipped.Contains(unlocks)) Equipped.Add(unlocks);   // une place libre : la nouvelle figure s'y met
            return (unlocks, Items.ForLevel(n));
        }

        /// <summary>Remet d'aplomb une progression lue (tableaux de la bonne taille, objets portés possédés).</summary>
        public void Repair()
        {
            if (Cleared == null || Cleared.Length != Levels.Count) Array.Resize(ref Cleared, Levels.Count);
            if (Best == null || Best.Length != Levels.Count) Array.Resize(ref Best, Levels.Count);
            if (Stars == null || Stars.Length != Levels.Count) Array.Resize(ref Stars, Levels.Count);
            if (ScoreStars == null || ScoreStars.Length != Levels.Count) Array.Resize(ref ScoreStars, Levels.Count);
            SeenObstacles ??= new List<int>();
            Equipped ??= new List<int>();
            Equipped = Equipped.FindAll(k => k >= 0 && k < Tuning.TrickNames.Length);
            if (Equipped.Count > MaxEquipped) Equipped.RemoveRange(MaxEquipped, Equipped.Count - MaxEquipped);
            var start = new Progress().Worn;
            if (Worn == null || Worn.Length != start.Length) Worn = start;
            for (int i = 0; i < Worn.Length; i++)
                if (!Owns(Worn[i]) || Items.Get(Worn[i]).Slot != (Slot)i) Worn[i] = start[i];
        }
    }
}
