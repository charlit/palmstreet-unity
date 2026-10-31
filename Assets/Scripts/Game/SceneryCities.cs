using UnityEngine;
using static PalmStreet.Game.Paint;

namespace PalmStreet.Game
{
    /// <summary>
    /// Plans des 8 villes ajoutées par la spec 002 (research.md §6), chacune avec son élément propre :
    /// Bidart (falaise, église blanche), Guéthary (port, grosse vague), Saint-Jean-de-Luz (baie et digue, maisons
    /// à colombages), Hendaye (les deux rochers jumeaux), Bayonne (cathédrale à deux flèches, fleuve, façades
    /// colorées), Espelette (maisons blanches et piments), Ascain (la Rhune et son petit train), Cambo (villa et
    /// jardin à la française). Renvoient null pour les décors existants.
    /// </summary>
    public sealed partial class Scenery
    {
        static readonly Color White = Hex("#F4F1EA"), Beam = Hex("#B3261E"), Green = Hex("#2E7D32");

        static void Sea(Paint p, ThemeLook l, System.Random r, int lines = 24)
        {
            p.Gradient(0, -60, Tile, Horizon + 60, l.Sea, Color.Lerp(l.Sea, l.SkyLow, 0.35f));
            for (int i = 0; i < lines; i++)
            {
                float y = R(r, 10, Horizon - 8), w = R(r, 14, 40) * (0.5f + y / Horizon), x = R(r, 0, Tile - w);
                p.Capsule(x, y, x + w, y, 1.6f, new Color(l.SeaLight.r, l.SeaLight.g, l.SeaLight.b, 0.35f));
            }
        }

        static void Hills(Paint p, ThemeLook l, System.Random r)
        {
            Ridge(p, Color.Lerp(l.Sea, l.SkyLow, 0.25f), 110, 22, 41);
            Ridge(p, l.Sea, 70, 20, 42);
        }

        /// <summary>Maison basque claire : murs blancs, colombages et volets rouges, toit dissymétrique.</summary>
        static void BasqueHouse(Paint p, float x, float w, float h, Color roof, Color beam)
        {
            p.Rect(x, -10, w, h + 10, White);
            p.Poly(roof, x - 6, h, x + w * 0.62f, h + 26, x + w + 6, h);
            p.Rect(x, h - 3, w, 3, beam);
            p.Rect(x + 4, h * 0.55f, w - 8, 2.5f, beam);
            for (float bx = x + 8; bx < x + w - 10; bx += 16)
            {
                p.Rect(bx, 12, 8, 12, beam);
                if (h > 44) p.Rect(bx, h * 0.55f + 8, 8, 10, beam);
            }
        }

        static Paint FarCity(Decor t, ThemeLook l, System.Random r)
        {
            var p = new Paint();
            switch (t)
            {
                case Decor.Bidart:
                    Ridge(p, Color.Lerp(l.Far, l.SkyLow, 0.4f), 150, 12, 61);
                    p.Poly(l.Far, 330, -60, 640, -60, 640, 190, 380, 175);   // la falaise
                    p.Poly(Color.Lerp(l.Far, Color.white, 0.12f), 380, 175, 640, 190, 640, 196, 420, 184);
                    return p;
                case Decor.Guethary:
                case Decor.SaintJeanDeLuz:
                case Decor.Hendaye:
                    // la côte espagnole et les montagnes au loin
                    Ridge(p, Color.Lerp(l.Far, l.SkyLow, 0.45f), 175, 26, t == Decor.Hendaye ? 71 : 72, 0.3f);
                    Ridge(p, l.Far, 150, 16, 73);
                    return p;
                case Decor.Bayonne:
                    Ridge(p, Color.Lerp(l.Far, l.SkyLow, 0.5f), 120, 10, 81);
                    for (float x = 0; x < Tile; x += 34) p.Rect(x, -60, 32, 90 + (x * 7 % 40) + 60, Color.Lerp(l.Far, l.SkyLow, 0.2f));
                    // cathédrale à deux flèches
                    p.Rect(250, -60, 140, 210, l.Far);
                    p.Poly(l.Far, 245, 150, 320, 190, 395, 150);
                    foreach (float sx in new[] { 262f, 360f })
                    {
                        p.Rect(sx, 150, 24, 90, l.Far);
                        p.Poly(l.Far, sx - 2, 240, sx + 12, 330, sx + 26, 240);
                    }
                    p.Circle(320, 120, 14, Color.Lerp(l.Far, l.SunCore, 0.35f), 20);
                    return p;
                case Decor.Espelette:
                case Decor.Cambo:
                    Ridge(p, Color.Lerp(l.Far, l.SkyLow, 0.45f), 210, 34, 91, 0.4f);
                    Ridge(p, l.Far, 170, 26, 92);
                    return p;
                case Decor.Ascain:
                    Ridge(p, Color.Lerp(l.Far, l.SkyLow, 0.5f), 170, 20, 101);
                    // la Rhune et son petit train rouge
                    p.Poly(l.Far, 120, -60, 560, -60, 420, 330, 360, 345, 300, 320);
                    p.Line(250, 120, 395, 330, 2, Color.Lerp(l.Far, Color.black, 0.3f));
                    p.Rect(300, 168, 18, 10, Hex("#C0392B"));
                    p.Rect(318, 172, 6, 6, Hex("#C0392B"));
                    p.Rect(303, 172, 4, 4, Hex("#FFE7A0")); p.Rect(310, 172, 4, 4, Hex("#FFE7A0"));
                    return p;
            }
            return null;
        }

        static Paint BackCity(Decor t, ThemeLook l, System.Random r)
        {
            var p = new Paint();
            var crest = Color.Lerp(l.SeaLight, Color.white, 0.4f);
            switch (t)
            {
                case Decor.Bidart:
                    Sea(p, l, r);
                    for (int i = 0; i < 3; i++)
                    {
                        float x = 80 + i * 180, y = R(r, 40, 90);
                        p.Curve(new Vector2(x - 30, y - 2), new Vector2(x, y + 5), new Vector2(x + 16, y + 14), 4, 1.5f, new Color(crest.r, crest.g, crest.b, 0.85f));
                        p.Ellipse(x, y + 2, 9, 1.8f, l.SeaLight, 12);
                        p.Capsule(x - 2, y + 4, x, y + 13, 3, l.Mid);
                        p.Circle(x + 1, y + 16, 2.4f, l.Mid, 10);
                    }
                    return p;
                case Decor.Guethary:
                    Sea(p, l, r, 16);
                    // la grosse vague de Guéthary, et un surfeur dans le tube
                    var wave = Color.Lerp(l.Sea, l.SeaLight, 0.35f);
                    p.Poly(wave, 160, 0, 520, 0, 520, 60, 300, 70);
                    p.Curve(new Vector2(300, 70), new Vector2(400, 150), new Vector2(470, 95), 22, 10, wave, 12);
                    p.Curve(new Vector2(300, 74), new Vector2(400, 156), new Vector2(472, 100), 5, 3, crest, 12);
                    p.Curve(new Vector2(470, 95), new Vector2(480, 80), new Vector2(455, 72), 8, 4, crest, 6);
                    p.Ellipse(420, 64, 10, 2, crest, 12, -0.3f);
                    p.Capsule(418, 67, 421, 79, 3, l.Mid);
                    p.Circle(422, 82, 2.6f, l.Mid, 10);
                    return p;
                case Decor.SaintJeanDeLuz:
                    Sea(p, l, r);
                    // la baie calme et sa digue
                    p.Rect(40, 92, 560, 6, Color.Lerp(l.Far, Color.black, 0.2f));
                    p.Rect(40, 98, 560, 2, crest);
                    for (int i = 0; i < 4; i++) p.Poly(White, 120 + i * 120, 40, 132 + i * 120, 40, 126 + i * 120, 58);   // voiles
                    return p;
                case Decor.Hendaye:
                    Sea(p, l, r);
                    // les deux rochers jumeaux
                    var rock = Color.Lerp(l.Mid, l.Far, 0.4f);
                    p.Poly(rock, 330, 0, 380, 0, 372, 96, 344, 108, 336, 70);
                    p.Poly(rock, 395, 0, 450, 0, 440, 82, 418, 94, 402, 60);
                    p.Capsule(320, 4, 460, 4, 4, crest);
                    return p;
                case Decor.Bayonne:
                    Hills(p, l, r);
                    // l'Adour, avec ses reflets
                    p.Gradient(0, -60, Tile, 100, l.Sea, Color.Lerp(l.Sea, l.SkyLow, 0.4f));
                    for (int i = 0; i < 14; i++) { float x = R(r, 0, Tile - 30), y = R(r, 0, 34); p.Capsule(x, y, x + 24, y, 1.5f, new Color(1, 1, 1, 0.35f)); }
                    return p;
                case Decor.Espelette:
                case Decor.Ascain:
                case Decor.Cambo:
                    Hills(p, l, r);
                    for (int i = 0; i < 12; i++) { float x = R(r, 10, Tile - 10); p.Circle(x, R(r, 60, 85), R(r, 6, 11), Color.Lerp(l.Sea, l.Mid, 0.5f), 12); }
                    return p;
            }
            return null;
        }

        static Paint MidCity(Decor t, ThemeLook l, System.Random r)
        {
            var p = new Paint();
            var lit = new Color(l.SunCore.r, l.SunCore.g, l.SunCore.b, 0.75f);
            switch (t)
            {
                case Decor.Bidart:
                    // l'église blanche sur la falaise, et quelques maisons
                    p.Rect(380, -10, 90, 90, White);
                    p.Poly(Hex("#8E3B2E"), 374, 80, 425, 104, 476, 80);
                    p.Rect(470, -10, 34, 150, White);
                    p.Poly(Hex("#8E3B2E"), 466, 140, 487, 170, 508, 140);
                    p.Rect(482, 104, 10, 16, l.Mid);
                    House(p, 60, 54, 40, 20, l.Mid, lit, true);
                    House(p, 150, 48, 34, 18, l.Mid, lit, true);
                    Palm(p, 260, 180, 20, l.Mid, 0.9f);
                    return p;
                case Decor.Guethary:
                    // le petit port : barques et maisons
                    for (int i = 0; i < 3; i++)
                    {
                        float x = 60 + i * 70;
                        var hull = i % 2 == 0 ? Hex("#C0392B") : Hex("#1E6FA8");
                        p.Poly(hull, x, 12, x + 44, 12, x + 38, 0, x + 6, 0);
                        p.Rect(x + 20, 12, 2, 30, l.Mid);
                    }
                    House(p, 300, 60, 46, 24, l.Mid, lit, true);
                    House(p, 380, 50, 38, 20, l.Mid, lit, true);
                    House(p, 470, 64, 50, 26, l.Mid, lit, true);
                    return p;
                case Decor.SaintJeanDeLuz:
                    for (int i = 0; i < 5; i++) BasqueHouse(p, 20 + i * 124, R(r, 70, 96), R(r, 50, 80), Hex("#8E3B2E"), Beam);
                    return p;
                case Decor.Hendaye:
                    for (int i = 0; i < 3; i++) House(p, 40 + i * 200, R(r, 50, 70), R(r, 40, 70), R(r, 16, 26), l.Mid, lit);
                    for (int i = 0; i < 4; i++) Palm(p, 120 + i * 150 + R(r, -15, 15), R(r, 150, 200), R(r, -20, 25), l.Mid, 0.9f);
                    return p;
                case Decor.Bayonne:
                    // façades étroites aux volets de couleur
                    Color[] shutters = { Hex("#C0392B"), Green, Hex("#1E6FA8") };
                    for (int i = 0, x = 6; x < Tile - 50; i++)
                    {
                        float w = R(r, 46, 60), h = R(r, 90, 140);
                        p.Rect(x, -10, w, h + 10, White);
                        p.Poly(Hex("#8E3B2E"), x - 3, h, x + w / 2, h + 16, x + w + 3, h);
                        var c = shutters[i % 3];
                        for (float wy = 20; wy < h - 16; wy += 30)
                            for (float wx = x + 6; wx < x + w - 14; wx += 18) { p.Rect(wx, wy, 4, 14, c); p.Rect(wx + 4, wy, 6, 14, l.Mid); p.Rect(wx + 10, wy, 4, 14, c); }
                        x += (int)(w + 4);
                    }
                    return p;
                case Decor.Espelette:
                    for (int i = 0; i < 4; i++)
                    {
                        float x = 30 + i * 155, w = R(r, 80, 100), h = R(r, 56, 70);
                        BasqueHouse(p, x, w, h, Hex("#8E3B2E"), Beam);
                        // guirlandes de piments qui sèchent sur la façade
                        for (int k = 0; k < 3; k++)
                        {
                            float gx = x + 14 + k * (w - 28) / 2, top = h - 6;
                            p.Line(gx, top, gx, top - 34, 1, Hex("#6B4226"));
                            for (int j = 0; j < 6; j++) p.Ellipse(gx + (j % 2 == 0 ? -2.5f : 2.5f), top - 4 - j * 5.5f, 2.2f, 4, Hex("#D62828"), 8);
                        }
                    }
                    return p;
                case Decor.Ascain:
                    House(p, 40, 60, 46, 26, l.Mid, lit, true);
                    House(p, 112, 48, 38, 22, l.Mid, lit, true);
                    p.Rect(250, -10, 64, 76, l.Mid);
                    p.Poly(l.Mid, 246, 66, 282, 92, 318, 66);
                    p.Rect(310, -10, 24, 120, l.Mid);   // clocher-porche basque
                    p.Poly(l.Mid, 308, 110, 322, 132, 336, 110);
                    House(p, 430, 56, 44, 24, l.Mid, lit, true);
                    House(p, 520, 60, 40, 22, l.Mid, lit, true);
                    return p;
                case Decor.Cambo:
                    // la villa et son jardin à la française
                    p.Rect(250, -10, 200, 104, White);
                    p.Poly(Hex("#8E3B2E"), 240, 94, 350, 132, 460, 94);
                    for (float wx = 266; wx < 440; wx += 30) { p.Rect(wx, 20, 12, 22, l.Mid); p.Rect(wx, 58, 12, 20, l.Mid); p.Rect(wx - 2, 20, 2, 22, Beam); p.Rect(wx + 12, 20, 2, 22, Beam); }
                    for (int i = 0; i < 6; i++)
                    {
                        float x = 20 + i * 40 + (i > 2 ? 420 : 0);
                        p.Rect(x - 2, -10, 4, 20, Hex("#5B3A1E"));
                        p.Poly(Green, x - 12, 10, x + 12, 10, x, 44);   // ifs taillés en cône
                    }
                    return p;
            }
            return null;
        }

        static Paint NearCity(Decor t, ThemeLook l, System.Random r)
        {
            var p = new Paint();
            switch (t)
            {
                case Decor.Bidart:
                case Decor.Guethary:
                case Decor.SaintJeanDeLuz:
                case Decor.Hendaye:
                    if (t == Decor.Hendaye || t == Decor.Guethary) Palm(p, 200, 270, -30, l.Near, 1.25f);
                    for (float x = 0; x < Tile; x += 32) p.Rect(x, -10, 4, 32, l.Near);   // garde-corps du front de mer
                    p.Rect(0, 20, Tile, 4, l.Near);
                    return p;
                case Decor.Bayonne:
                    for (int i = 0; i < 3; i++)
                    {
                        float x = 100 + i * 210;
                        p.Rect(x, -10, 5, 110, l.Near);
                        p.Rect(x - 8, 100, 21, 6, l.Near);
                        p.Glow(x + 2, 112, 20, new Color(1, 0.94f, 0.6f, 0.7f), new Color(1, 0.94f, 0.6f, 0));
                    }
                    p.Rect(0, 14, Tile, 3, l.Near);
                    return p;
                case Decor.Espelette:
                case Decor.Ascain:
                case Decor.Cambo:
                    for (int i = 0; i < 4; i++)
                    {
                        float x = 60 + i * 160 + R(r, -20, 20), h = R(r, 60, 110);
                        p.Rect(x - 3, -10, 6, h * 0.6f, l.Near);
                        p.Circle(x, h * 0.7f, h * 0.3f, l.Near, 16);
                        p.Circle(x - h * 0.18f, h * 0.55f, h * 0.2f, l.Near, 14);
                    }
                    for (float x = 0; x < Tile; x += 40) p.Rect(x, -10, 3, 24, l.Near);
                    p.Rect(0, 10, Tile, 3, l.Near);
                    return p;
            }
            return null;
        }
    }
}
