using PalmStreet.Core;
using UnityEngine;
using static PalmStreet.Game.Paint;

namespace PalmStreet.Game
{
    public enum Decor { Largente, Btz, Arcangues, Neon, Bidart, Guethary, SaintJeanDeLuz, Hendaye, Bayonne, Espelette, Ascain, Cambo }

    /// <summary>Couleurs et position du soleil d'un décor (FR-015) : coucher de soleil sur l'océan, plein jour à BTZ, crépuscule basque.</summary>
    public sealed class ThemeLook
    {
        public string Name;
        public Color SkyTop, SkyMid, SkyLow, Sun, SunCore, Glow, Far, Sea, SeaLight, Mid, Near, Ground, GroundTop, Stripe;
        public Vector2 SunPos;   // position logique dans la zone de jeu (0,0 = sol à gauche)
        public float SunRadius;
        public bool HasSea;
        public Decor Decor;   // style des plans de décor

        ThemeLook With(string name) { var c = (ThemeLook)MemberwiseClone(); c.Name = name; return c; }

        /// <summary>Copie renommée avec son propre style de décor et ses couleurs (couleurs en hexadécimal, null = inchangée).</summary>
        ThemeLook City(string name, Decor d, bool sea, string skyTop, string skyMid, string skyLow, string far, string seaCol, string seaLight, string mid, string near, string groundTop, string sun = null, string stripe = null, string ground = null)
        {
            var c = With(name);
            c.Decor = d; c.HasSea = sea;
            c.SkyTop = Hex(skyTop); c.SkyMid = Hex(skyMid); c.SkyLow = Hex(skyLow); c.Far = Hex(far); c.Sea = Hex(seaCol); c.SeaLight = Hex(seaLight);
            c.Mid = Hex(mid); c.Near = Hex(near); c.GroundTop = Hex(groundTop);
            if (sun != null) c.Sun = Hex(sun);
            if (stripe != null) c.Stripe = Hex(stripe);
            if (ground != null) c.Ground = Hex(ground);
            return c;
        }

        /// <summary>Les 12 villes, dans l'ordre de Theme (spec 002). Les nouvelles villes partent d'une palette proche.</summary>
        public static ThemeLook[] All => all ??= Cities();   // Bases est déclaré plus bas : on construit à la demande
        static ThemeLook[] all;

        static ThemeLook[] Cities()
        {
            var b = Bases;
            return new[]
            {
                b[0].With("ANGLET"),
                b[1].With("BIARRITZ"),
                b[1].City("BIDART", Decor.Bidart, true, "#2E6FB0", "#7FB8D8", "#FFD9A0", "#4C6E8C", "#136E8F", "#BFF3FF", "#1E3F52", "#102A38", "#F2C14E", "#FFE7A0"),
                b[1].City("GUÉTHARY", Decor.Guethary, true, "#0F5E7A", "#5FB8C9", "#E8F4E0", "#3F7F8C", "#0B7A8C", "#CFFAFF", "#134652", "#0B2E36", "#6EE7B7"),
                b[0].City("SAINT-JEAN-DE-LUZ", Decor.SaintJeanDeLuz, true, "#3B2C6E", "#E07A8F", "#FFC7A1", "#6B4A7A", "#3E4C8A", "#FFD1B3", "#3A2348", "#24142E", "#FF8FA3"),
                b[0].City("HENDAYE", Decor.Hendaye, true, "#1F1A4A", "#D9534F", "#FFB067", "#5A2F4F", "#2B2A66", "#FF9E6B", "#2A1838", "#1A0F26", "#FF6B4A", "#FFDA8A"),
                b[2].City("BAYONNE", Decor.Bayonne, false, "#3D5A80", "#98C1D9", "#E0FBFC", "#5C6B7A", "#3E7CB1", "#9FD3F2", "#2E3A4A", "#1C2530", "#E63946", "#FFF6D5", "#F4F1EA", "#2B2F36"),
                b[2].With("ARCANGUES"),
                b[2].City("ESPELETTE", Decor.Espelette, false, "#2A7FBA", "#8ED1F2", "#FFF0C2", "#4F7A5A", "#5E9C5E", "#C7F0BD", "#2F4A35", "#1D2E22", "#E63946", "#FFF3B0"),
                b[2].City("ASCAIN", Decor.Ascain, false, "#1D2D50", "#6C5B7B", "#F8B195", "#355C7D", "#3E6B48", "#9BE15D", "#1F3A2B", "#13241B", "#9BE15D"),
                b[2].City("CAMBO-LES-BAINS", Decor.Cambo, false, "#3A7CA5", "#A9D6E5", "#F6F1D1", "#5B7F6A", "#4C8C4A", "#CDEAC0", "#2D4A35", "#1B2E21", "#B5E48C", "#FFF8DC"),
                b[3].With("BIARRITZ LA NUIT"),
            };
        }

        static readonly ThemeLook[] Bases =
        {
            new ThemeLook
            {
                Name = "LARGENTÉ", Decor = Decor.Largente, HasSea = true, SunPos = new Vector2(290, 185), SunRadius = 34,
                SkyTop = Hex("#2B1A5E"), SkyMid = Hex("#C8457E"), SkyLow = Hex("#FFA25E"),
                Sun = Hex("#FFD27A"), SunCore = Hex("#FFF4D6"), Glow = Hex("#FF8A4C", 0.55f),
                Far = Hex("#6E3B73"), Sea = Hex("#3A2C7C"), SeaLight = Hex("#FFB98A"),
                Mid = Hex("#2C1642"), Near = Hex("#1B0E2C"), Ground = Hex("#251A33"), GroundTop = Hex("#FF7A59"), Stripe = Hex("#FFD27A"),
            },
            new ThemeLook
            {
                Name = "BTZ", Decor = Decor.Btz, HasSea = true, SunPos = new Vector2(310, 380), SunRadius = 28,
                SkyTop = Hex("#1271C4"), SkyMid = Hex("#48B9E8"), SkyLow = Hex("#FFE9B0"),
                Sun = Hex("#FFF3B0"), SunCore = Hex("#FFFFFF"), Glow = Hex("#FFFFFF", 0.45f),
                Far = Hex("#2F6F99"), Sea = Hex("#0D8BB0"), SeaLight = Hex("#A8F2FF"),
                Mid = Hex("#174A66"), Near = Hex("#0C3346"), Ground = Hex("#2E3C4A"), GroundTop = Hex("#3FE0D0"), Stripe = Hex("#FFE14D"),
            },
            new ThemeLook
            {
                Name = "ARCANGUES", Decor = Decor.Arcangues, HasSea = false, SunPos = new Vector2(250, 165), SunRadius = 46,
                SkyTop = Hex("#150E30"), SkyMid = Hex("#5E2A7A"), SkyLow = Hex("#F27A4A"),
                Sun = Hex("#FF6A3D"), SunCore = Hex("#FFC58A"), Glow = Hex("#FF3D6E", 0.5f),
                Far = Hex("#3B2559"), Sea = Hex("#2F4A3A"), SeaLight = Hex("#9BE15D"),
                Mid = Hex("#1F3527"), Near = Hex("#11211A"), Ground = Hex("#272A20"), GroundTop = Hex("#9BE15D"), Stripe = Hex("#F2C94C"),
            },
            new ThemeLook
            {
                // niveau 2 (FR-029) : nuit, lune, néons roses et cyan
                Name = "NUIT NÉON", Decor = Decor.Neon, HasSea = true, SunPos = new Vector2(300, 430), SunRadius = 26,
                SkyTop = Hex("#04010D"), SkyMid = Hex("#1B0A3F"), SkyLow = Hex("#5A1A7A"),
                Sun = Hex("#DCE6FF"), SunCore = Hex("#FFFFFF"), Glow = Hex("#6FE8FF", 0.35f),
                Far = Hex("#120A2B"), Sea = Hex("#0A0524"), SeaLight = Hex("#FF4FD8"),
                Mid = Hex("#0E0820"), Near = Hex("#07040F"), Ground = Hex("#0B0716"), GroundTop = Hex("#FF2BD6"), Stripe = Hex("#35F2FF"),
            },
        };

        public static ThemeLook Of(Theme t) => All[(int)t];
    }

    public enum Hat { None, Cap, Beanie, Band }
    public enum DeckArt { Wood, Crab, Pepper, Neon }

    /// <summary>Apparence du skateur, telle que le moteur de dessin l'utilise.</summary>
    public sealed class RiderLook
    {
        public Color Skin, Hair, HatColor, Top, Accent, Pants, Shoes, Deck, Wheels;
        public Hat Hat;
        public bool Curly, Shorts, Stripes, Logo;
        public DeckArt Art;
    }

    /// <summary>
    /// Garde-robe (spec 002, FR-023 à FR-027) : l'apparence de chaque objet gagné, et la peau, la coiffure et la
    /// couleur des cheveux, qui restent au choix libre.
    /// </summary>
    public static class Wardrobe
    {
        public static readonly Color[] Skins = { Hex("#F6D2B3"), Hex("#E8B48A"), Hex("#C68B59"), Hex("#8D5524"), Hex("#5C3317") };
        public static readonly Color[] Hairs = { Hex("#E8C26A"), Hex("#B5502A"), Hex("#7A4B2A"), Hex("#3B2412"), Hex("#141414") };
        public static readonly string[] HairStyles = { "COURTS", "BOUCLES" };

        /// <summary>Couleur principale d'un objet (pastille du profil).</summary>
        public static Color ItemColor(string id) => id switch
        {
            "start-head" => Hex("#8A7F75"),
            "cap-red" => Hex("#E63946"),
            "beanie-stripes" => Hex("#23305C"),
            "band-red" => Hex("#E63946"),
            "start-top" => Hex("#F4F1EA"),
            "hoodie-ocean" => Hex("#1E88C8"),
            "hoodie-gull" => Hex("#E9EDF2"),
            "hoodie-purple" => Hex("#7B2CBF"),
            "start-legs" => Hex("#3D5A80"),
            "shorts-surf" => Hex("#2EC4B6"),
            "pants-green" => Hex("#2D6A4F"),
            "start-shoes" => Hex("#F4F1EA"),
            "shoes-yellow" => Hex("#FFD400"),
            "start-deck" => Hex("#C8A27A"),
            "deck-crab" => Hex("#E0452B"),
            "deck-pepper" => Hex("#C1121F"),
            "deck-neon" => Hex("#FF2BD6"),
            _ => Color.gray,
        };

        public static RiderLook Build(Progress p)
        {
            string head = p.WornIn(Slot.Head), top = p.WornIn(Slot.Top), legs = p.WornIn(Slot.Legs), shoes = p.WornIn(Slot.Shoes), deck = p.WornIn(Slot.Deck);
            var l = new RiderLook
            {
                Skin = Skins[Mathf.Clamp(p.Skin, 0, Skins.Length - 1)],
                Hair = Hairs[Mathf.Clamp(p.HairColor, 0, Hairs.Length - 1)],
                Curly = p.HairStyle == 1,
                Hat = head == "cap-red" ? Hat.Cap : head == "beanie-stripes" ? Hat.Beanie : head == "band-red" ? Hat.Band : Hat.None,
                HatColor = ItemColor(head),
                Stripes = head == "beanie-stripes",
                Top = ItemColor(top),
                Accent = top == "start-top" ? Hex("#C9CED6") : top == "hoodie-purple" ? Hex("#C77DFF") : top == "hoodie-gull" ? Hex("#9AA3B5") : Color.white,
                Logo = top == "hoodie-gull",
                Pants = ItemColor(legs),
                Shorts = legs == "shorts-surf",
                Shoes = ItemColor(shoes),
                Deck = ItemColor(deck),
                Wheels = deck == "deck-crab" ? Color.white : deck == "deck-pepper" ? Hex("#3BB273") : deck == "deck-neon" ? Hex("#35F2FF") : Hex("#FFF4D6"),
                Art = deck == "deck-crab" ? DeckArt.Crab : deck == "deck-pepper" ? DeckArt.Pepper : deck == "deck-neon" ? DeckArt.Neon : DeckArt.Wood,
            };
            return l;
        }
    }
}
