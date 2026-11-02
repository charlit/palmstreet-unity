using System.Collections.Generic;

namespace PalmStreet.Core
{
    public enum Slot { Head, Top, Legs, Shoes, Deck }

    public sealed class Item
    {
        public string Id, Name;
        public Slot Slot;
        public int Level;   // 0 : tenue de départ ; 1 à 12 : niveau qui le donne ; −1 : gagné avec les étoiles
        public int Stars;   // étoiles à réunir (objets à étoiles)
        public BossKind Boss;   // tenue de rugby (spec 004) : gagnée en battant ce boss, au niveau Level
    }

    /// <summary>Garde-robe gagnée en jouant (spec 002, FR-023 à FR-027) : la tenue de départ et un objet par niveau.</summary>
    public static class Items
    {
        public static readonly Item[] All =
        {
            I("start-head", Slot.Head, "TÊTE NUE", 0),
            I("start-top", Slot.Top, "T-SHIRT BLANC", 0),
            I("start-legs", Slot.Legs, "JEAN BLEU", 0),
            I("start-shoes", Slot.Shoes, "BASKETS BLANCHES", 0),
            I("start-deck", Slot.Deck, "PLANCHE BOIS", 0),
            I("cap-red", Slot.Head, "CASQUETTE ROUGE", 1),
            I("hoodie-ocean", Slot.Top, "SWEAT BLEU OCÉAN", 2),
            I("deck-crab", Slot.Deck, "PLANCHE FREESTILL", 3),
            I("shorts-surf", Slot.Legs, "SHORT DE SURF", 4),
            I("beanie-stripes", Slot.Head, "BONNET RAYÉ", 5),
            I("hoodie-gull", Slot.Top, "SWEAT GOÉLAND", 6),
            I("band-red", Slot.Head, "BANDEAU ROUGE", 7),
            I("pants-green", Slot.Legs, "PANTALON VERT", 8),
            I("deck-pepper", Slot.Deck, "PLANCHE KEKETSK8", 9),
            I("shoes-yellow", Slot.Shoes, "BASKETS JAUNES", 10),
            I("hoodie-purple", Slot.Top, "SWEAT VIOLET", 11),
            I("deck-neon", Slot.Deck, "PLANCHE GUIGUI", 12),
            S("cap-gold", Slot.Head, "CASQUETTE DORÉE", 6),
            S("hoodie-basque", Slot.Top, "SWEAT ROUGE ET VERT", 12),
            S("shoes-pink", Slot.Shoes, "BASKETS ROSES", 18),
            S("pants-white", Slot.Legs, "PANTALON BLANC", 24),
            S("deck-gold", Slot.Deck, "PLANCHE MAXWELL", 30),
            S("hoodie-gold", Slot.Top, "SWEAT DORÉ", 36),
            // tenues complètes des boss de rugby (spec 004) : après les objets des niveaux, pour que ForLevel les ignore
            R("band-pilier", Slot.Head, "BANDEAU DU PILIER", BossKind.Pilier),
            R("jersey-pilier", Slot.Top, "MAILLOT DU PILIER", BossKind.Pilier),
            R("shorts-pilier", Slot.Legs, "SHORT DU PILIER", BossKind.Pilier),
            R("boots-pilier", Slot.Shoes, "BASKETS DU PILIER", BossKind.Pilier),
            R("deck-pilier", Slot.Deck, "PLANCHE DU PILIER", BossKind.Pilier),
            R("scrum-cap", Slot.Head, "CASQUE DE MÊLÉE", BossKind.Talonneur),
            R("jersey-talonneur", Slot.Top, "MAILLOT DU TALONNEUR", BossKind.Talonneur),
            R("shorts-talonneur", Slot.Legs, "SHORT DU TALONNEUR", BossKind.Talonneur),
            R("boots-talonneur", Slot.Shoes, "BASKETS DU TALONNEUR", BossKind.Talonneur),
            R("deck-talonneur", Slot.Deck, "PLANCHE DU TALONNEUR", BossKind.Talonneur),
        };

        /// <summary>Tenue complète du boss de rugby du niveau n (5 pièces), vide pour les autres niveaux.</summary>
        public static IEnumerable<Item> OutfitFor(int n)
        {
            foreach (var i in All) if (i.Boss != BossKind.None && i.Level == n) yield return i;
        }

        /// <summary>Objets à étoiles débloqués en passant de before à after étoiles.</summary>
        public static IEnumerable<Item> ForStars(int before, int after)
        {
            foreach (var i in All) if (i.Stars > before && i.Stars <= after) yield return i;
        }

        public static Item Get(string id)
        {
            foreach (var i in All) if (i.Id == id) return i;
            return null;
        }

        public static Item ForLevel(int n)
        {
            foreach (var i in All) if (i.Level == n) return i;
            return null;
        }

        public static IEnumerable<Item> InSlot(Slot s)
        {
            foreach (var i in All) if (i.Slot == s) yield return i;
        }

        static Item I(string id, Slot slot, string name, int level) => new Item { Id = id, Slot = slot, Name = name, Level = level };
        static Item S(string id, Slot slot, string name, int stars) => new Item { Id = id, Slot = slot, Name = name, Level = -1, Stars = stars };
        static Item R(string id, Slot slot, string name, BossKind boss)
        {
            int level = System.Array.Find(Levels.All, l => l.Boss == boss).Number;
            return new Item { Id = id, Slot = slot, Name = name, Level = level, Boss = boss };
        }
    }
}
