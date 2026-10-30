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
        };

        public static ThemeLook Of(Theme t) => All[(int)t];
    }

    public enum Headwear { Cap, Beanie, Curls, Headband }

    /// <summary>Tenue d'un skateur (FR-025) : chacun a sa silhouette et sa couleur de planche ; GUIGUI roule en jaune (FR-002).</summary>
    public sealed class RiderLook
    {
        public string Name;
        public Color Skin, Hair, Hat, Top, Accent, Pants, Shoes, Deck, Wheels;
        public Headwear Head;
        public bool Shorts;

        public static readonly RiderLook[] All =
        {
            new RiderLook { Name = "FREESTILL", Head = Headwear.Cap, Skin = Hex("#F1C7A1"), Hair = Hex("#E8C26A"), Hat = Hex("#E63946"),
                Top = Hex("#F4F1EA"), Accent = Hex("#E63946"), Pants = Hex("#23233B"), Shoes = Hex("#E63946"), Deck = Hex("#E63946"), Wheels = Hex("#FFF4D6") },
            new RiderLook { Name = "KEKETSK8", Head = Headwear.Beanie, Skin = Hex("#C68B59"), Hair = Hex("#2A1A10"), Hat = Hex("#2A9D8F"),
                Top = Hex("#FF8C1A"), Accent = Hex("#FFF4D6"), Pants = Hex("#3D5A80"), Shoes = Hex("#FFFFFF"), Deck = Hex("#222222"), Wheels = Hex("#FF8C1A") },
            new RiderLook { Name = "GUIGUI", Head = Headwear.Curls, Shorts = true, Skin = Hex("#E8B48A"), Hair = Hex("#3B2412"), Hat = Hex("#3B2412"),
                Top = Hex("#3BB273"), Accent = Hex("#FFE14D"), Pants = Hex("#D9C29C"), Shoes = Hex("#1D1D1D"), Deck = Hex("#FFD400"), Wheels = Hex("#FFFFFF") },
            new RiderLook { Name = "MAXWELL", Head = Headwear.Headband, Skin = Hex("#8D5524"), Hair = Hex("#141414"), Hat = Hex("#FF4D6D"),
                Top = Hex("#7B2CBF"), Accent = Hex("#C77DFF"), Pants = Hex("#5C6670"), Shoes = Hex("#FFE14D"), Deck = Hex("#3A86FF"), Wheels = Hex("#FFE14D") },
        };

        public static RiderLook Of(Rider r) => All[(int)r];
    }
}
