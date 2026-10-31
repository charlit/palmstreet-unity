using PalmStreet.Core;
using UnityEngine;
using static PalmStreet.Game.Paint;

namespace PalmStreet.Game
{
    /// <summary>Couleurs et position du soleil d'un décor (FR-015) : coucher de soleil sur l'océan, plein jour à BTZ, crépuscule basque.</summary>
    public sealed class ThemeLook
    {
        public string Name;
        public Color SkyTop, SkyMid, SkyLow, Sun, SunCore, Glow, Far, Sea, SeaLight, Mid, Near, Ground, GroundTop, Stripe;
        public Vector2 SunPos;   // position logique dans la zone de jeu (0,0 = sol à gauche)
        public float SunRadius;
        public bool HasSea;

        public static readonly ThemeLook[] All =
        {
            new ThemeLook
            {
                Name = "LARGENTÉ", HasSea = true, SunPos = new Vector2(290, 185), SunRadius = 34,
                SkyTop = Hex("#2B1A5E"), SkyMid = Hex("#C8457E"), SkyLow = Hex("#FFA25E"),
                Sun = Hex("#FFD27A"), SunCore = Hex("#FFF4D6"), Glow = Hex("#FF8A4C", 0.55f),
                Far = Hex("#6E3B73"), Sea = Hex("#3A2C7C"), SeaLight = Hex("#FFB98A"),
                Mid = Hex("#2C1642"), Near = Hex("#1B0E2C"), Ground = Hex("#251A33"), GroundTop = Hex("#FF7A59"), Stripe = Hex("#FFD27A"),
            },
            new ThemeLook
            {
                Name = "BTZ", HasSea = true, SunPos = new Vector2(310, 380), SunRadius = 28,
                SkyTop = Hex("#1271C4"), SkyMid = Hex("#48B9E8"), SkyLow = Hex("#FFE9B0"),
                Sun = Hex("#FFF3B0"), SunCore = Hex("#FFFFFF"), Glow = Hex("#FFFFFF", 0.45f),
                Far = Hex("#2F6F99"), Sea = Hex("#0D8BB0"), SeaLight = Hex("#A8F2FF"),
                Mid = Hex("#174A66"), Near = Hex("#0C3346"), Ground = Hex("#2E3C4A"), GroundTop = Hex("#3FE0D0"), Stripe = Hex("#FFE14D"),
            },
            new ThemeLook
            {
                Name = "ARCANGUES", HasSea = false, SunPos = new Vector2(250, 165), SunRadius = 46,
                SkyTop = Hex("#150E30"), SkyMid = Hex("#5E2A7A"), SkyLow = Hex("#F27A4A"),
                Sun = Hex("#FF6A3D"), SunCore = Hex("#FFC58A"), Glow = Hex("#FF3D6E", 0.5f),
                Far = Hex("#3B2559"), Sea = Hex("#2F4A3A"), SeaLight = Hex("#9BE15D"),
                Mid = Hex("#1F3527"), Near = Hex("#11211A"), Ground = Hex("#272A20"), GroundTop = Hex("#9BE15D"), Stripe = Hex("#F2C94C"),
            },
            new ThemeLook
            {
                // niveau 2 (FR-029) : nuit, lune, néons roses et cyan
                Name = "NUIT NÉON", HasSea = true, SunPos = new Vector2(300, 430), SunRadius = 26,
                SkyTop = Hex("#04010D"), SkyMid = Hex("#1B0A3F"), SkyLow = Hex("#5A1A7A"),
                Sun = Hex("#DCE6FF"), SunCore = Hex("#FFFFFF"), Glow = Hex("#6FE8FF", 0.35f),
                Far = Hex("#120A2B"), Sea = Hex("#0A0524"), SeaLight = Hex("#FF4FD8"),
                Mid = Hex("#0E0820"), Near = Hex("#07040F"), Ground = Hex("#0B0716"), GroundTop = Hex("#FF2BD6"), Stripe = Hex("#35F2FF"),
            },
        };

        public static ThemeLook Of(Theme t) => All[(int)t];
    }

    public enum Headwear { Cap, Beanie, Curls, Headband }

    /// <summary>Apparence du skateur, telle que le moteur de dessin l'utilise.</summary>
    public sealed class RiderLook
    {
        public Color Skin, Hair, Hat, Top, Accent, Pants, Shoes, Deck, Wheels;
        public Headwear Head;
        public bool Shorts;
    }

    /// <summary>Tenue choisie dans le profil (FR-026) : des index dans les listes de Wardrobe, gardés dans PlayerPrefs.</summary>
    [System.Serializable]
    public sealed class Outfit
    {
        public int Head, HatColor = 0, HairColor, Skin, Top = 9, Accent, Legs, Pants = 6, Shoes, Deck, Wheels = 13;
    }

    /// <summary>Ce qu'on peut changer dans le profil : coiffures, peaux, cheveux et une palette commune aux vêtements et à la planche.</summary>
    public static class Wardrobe
    {
        public static readonly string[] Heads = { "CASQUETTE", "BONNET", "BOUCLES", "BANDEAU" };
        public static readonly string[] Legs = { "PANTALON", "SHORT" };
        public static readonly Color[] Skins = { Hex("#F6D2B3"), Hex("#E8B48A"), Hex("#C68B59"), Hex("#8D5524"), Hex("#5C3317") };
        public static readonly Color[] Hairs = { Hex("#E8C26A"), Hex("#B5502A"), Hex("#7A4B2A"), Hex("#3B2412"), Hex("#141414") };
        public static readonly Color[] Palette =
        {
            Hex("#E63946"), Hex("#FF8C1A"), Hex("#FFD400"), Hex("#3BB273"), Hex("#2A9D8F"), Hex("#3A86FF"), Hex("#23233B"),
            Hex("#7B2CBF"), Hex("#FF4D6D"), Hex("#F4F1EA"), Hex("#222222"), Hex("#5C6670"), Hex("#3D5A80"), Hex("#D9C29C"),
        };

        static int Wrap(int i, int n) => ((i % n) + n) % n;

        public static RiderLook Build(Outfit o) => new RiderLook
        {
            Head = (Headwear)Wrap(o.Head, Heads.Length),
            Shorts = Wrap(o.Legs, Legs.Length) == 1,
            Skin = Skins[Wrap(o.Skin, Skins.Length)],
            Hair = Hairs[Wrap(o.HairColor, Hairs.Length)],
            Hat = Palette[Wrap(o.HatColor, Palette.Length)],
            Top = Palette[Wrap(o.Top, Palette.Length)],
            Accent = Palette[Wrap(o.Accent, Palette.Length)],
            Pants = Palette[Wrap(o.Pants, Palette.Length)],
            Shoes = Palette[Wrap(o.Shoes, Palette.Length)],
            Deck = Palette[Wrap(o.Deck, Palette.Length)],
            Wheels = Palette[Wrap(o.Wheels, Palette.Length)],
        };
    }
}
