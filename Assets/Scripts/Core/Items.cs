using System.Collections.Generic;

namespace PalmStreet.Core
{
    public enum Slot { Head, Top, Legs, Shoes, Deck }

    public sealed class Item
    {
        public string Id, Name;
        public Slot Slot;
        public int Level;   // 0 : tenue de départ ; 1 à 12 : niveau qui le donne
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
            I("deck-crab", Slot.Deck, "PLANCHE CRABE", 3),
            I("shorts-surf", Slot.Legs, "SHORT DE SURF", 4),
            I("beanie-stripes", Slot.Head, "BONNET RAYÉ", 5),
            I("hoodie-gull", Slot.Top, "SWEAT GOÉLAND", 6),
            I("band-red", Slot.Head, "BANDEAU ROUGE", 7),
            I("pants-green", Slot.Legs, "PANTALON VERT", 8),
            I("deck-pepper", Slot.Deck, "PLANCHE PIMENT", 9),
            I("shoes-yellow", Slot.Shoes, "BASKETS JAUNES", 10),
            I("hoodie-purple", Slot.Top, "SWEAT VIOLET", 11),
            I("deck-neon", Slot.Deck, "PLANCHE NÉON", 12),
        };

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
    }
}
