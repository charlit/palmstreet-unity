using PalmStreet.Core;
using UnityEngine;
using static PalmStreet.Game.Paint;

namespace PalmStreet.Game
{
    /// <summary>
    /// Un skateur en pantin articulé (FR-024, FR-025) : jambes, buste, tête, bras et planche en formes Unity.
    /// Poses : rouler et pousser, accroupi, ollie, figures (la planche tourne sur elle-même, ou tout le skateur),
    /// grind bras écartés, chute (le skateur tombe en avant, la planche s'échappe en rebondissant).
    /// Unités logiques : pieds en (0, 0), environ 60 de haut.
    /// </summary>
    public sealed class RiderView : MonoBehaviour
    {
        const float Center = 30, Thigh = 14.5f, Shin = 14f, Ankle = 4, DeckY = 6.5f, GripTop = 1.8f;
        const float BackFoot = -11, FrontFoot = 9;   // pieds sur les vis des trucks

        /// <summary>Jambe en deux segments (cuisse, tibia) et une chaussure, placée par cinématique inverse.</summary>
        sealed class LegRig { public Transform Thigh, Shin, Shoe; }

        Transform gripFace, bottomFace;
        LegRig legB, legF;
        Transform spin, board, deck, body, torso, armB, armF;
        Transform[] wheels;
        RiderLook look;

        bool wasAirborne;
        float landedAt = -9;

        // chute
        bool crashed;
        Vector2 bodyPos, bodyVel, boardPos, boardVel;
        float bodyRot, boardRot, boardSpin;

        public static RiderView Create(RiderLook look, Transform parent, int order, string layer = "Default")
        {
            var v = new GameObject("Skateur").AddComponent<RiderView>();
            v.transform.SetParent(parent, false);
            v.Build(look, order, layer);
            return v;
        }

        void Build(RiderLook l, int order, string layer)
        {
            look = l;
            spin = new GameObject("Rotation").transform;
            spin.SetParent(transform, false);

            // planche : vue de profil (plateau, grip, trucks, roues) et vues de dessus et de dessous pour les flips
            board = new GameObject("Planche").transform;
            board.SetParent(spin, false);
            deck = new GameObject("Plateau").transform;
            deck.SetParent(board, false);
            BuildBoard(l, order, layer);

            body = new GameObject("Corps").transform;
            body.SetParent(spin, false);

            // ordres de tri : planche 0-2, bras arrière 3-4, jambe arrière 5-8, buste 9-11, jambe avant 12-15, tête 16-17, bras avant 18-19
            // (chaque membre : contour d'encre puis couleurs, spec 003)
            torso = new GameObject("Buste").transform;
            torso.SetParent(body, false);
            armB = Arm("Bras arrière", torso, order + 3, layer, 0.85f);
            armB.localPosition = W(0.5f, 18);
            legB = MakeLeg("Jambe arrière", order + 5, layer, 0.8f);
            var bust = new Piece();
            bool hoodie = l.TopId.StartsWith("hoodie");
            if (hoodie) bust.Ink.Ellipse(-5.5f, 19.5f, 4.6f + Outline, 3.6f + Outline, Ink, 12);   // capuche (dessinée par TopDesign)
            bust.Ellipse(0, 0, 7.6f, 5.5f, l.Pants, 16)                      // bassin de la couleur du bas
                .Capsule(0, 2, 2.5f, 18, 15, l.Top);                         // épaules arrondies
            bust.Col.Capsule(-3.4f, 3.5f, -1.2f, 17.5f, 6.5f, Shade(l.Top))   // ombre côté dos
                .Capsule(4.2f, 6, 5.4f, 15.5f, 2.2f, Light(l.Top))             // lumière côté poitrine
                .Rect(-6.8f, 3, 13.6f, 3, l.Top)                              // le haut recouvre le haut du bassin
                .Line(-2.5f, 4.6f, 2.5f, 5.6f, 0.5f, Shade(l.Top))              // plis
                .Line(-1, 7.4f, 3.6f, 8.2f, 0.5f, Shade(l.Top))
                .Line(-6.6f, 3, 6.6f, 3, 0.7f, Shade(l.Pants))                 // taille
                .Line(-3, 10, 7.5f, 13, 3, l.Logo || l.TopId.StartsWith("jersey") ? new Color(0, 0, 0, 0) : l.Accent);   // pas de sangle sur un maillot
            bust.Make("Buste", torso, order + 9, layer);
            if (l.Logo) new Paint().Poly(l.Accent, -2, 10, 8, 14, 5, 9).Poly(l.Accent, -2, 10, 6, 8, 2, 6).Make("Logo", torso, order + 11, layer);   // aile de goéland
            TopDesign(l).Make("Motif du haut", torso, order + 11, layer);
            legF = MakeLeg("Jambe avant", order + 12, layer, 1);
            var head = Head(l, order + 16, layer);
            head.SetParent(torso, false);
            head.localPosition = W(3.5f, 28);
            armF = Arm("Bras avant", torso, order + 18, layer, 1);
            armF.localPosition = W(2.5f, 18);
        }

        void BuildBoard(RiderLook l, int order, string layer)
        {
            var grip = Hex("#1E1E22");
            var ply = Color.Lerp(l.Deck, Color.white, 0.5f);
            var under = Color.Lerp(l.Deck, Color.black, 0.3f);
            var metal = Hex("#C9CED6");
            var metalDark = Hex("#7D8590");

            // profil : plateau plat au milieu, kicktails relevés et arrondis, grip noir dessus, tranche du bois visible
            var p = new Paint();
            void Tails(float y, float w0, float w1, Color col)
            {
                p.Capsule(-18, y, 18, y, w0, col);
                p.Curve(new Vector2(18, y), new Vector2(24, y), new Vector2(28.5f, y + 4.2f), w0, w1, col, 6);
                p.Curve(new Vector2(-18, y), new Vector2(-24, y), new Vector2(-28.5f, y + 4.2f), w0, w1, col, 6);
            }
            Tails(-0.6f, 3.4f, 2.8f, under);
            Tails(-0.2f, 2.6f, 2.1f, l.Deck);
            p.Line(-18, -0.5f, 18, -0.5f, 0.7f, ply);
            Tails(1.1f, 1.2f, 0.9f, grip);
            p.Make("Plateau", deck, order + 1, layer);

            // trucks : semelle, axe (hanger) et kingpin
            var t = new Paint();
            foreach (float cx in new[] { -14f, 14f })
            {
                t.Rect(cx - 4.5f, -3, 9, 1.4f, metalDark);
                t.Poly(metal, cx - 2.5f, -1.8f, cx + 2.5f, -1.8f, cx + 4, -4.6f, cx - 4, -4.6f);
                t.Rect(cx - 1, -2.6f, 2, 1.2f, metalDark);
                t.Rect(cx - 4.6f, -4.4f, 9.2f, 1, Color.Lerp(metal, Color.white, 0.4f));
            }
            t.Make("Trucks", deck, order, layer);

            wheels = new Transform[2];
            for (int i = 0; i < 2; i++)
            {
                var w = new Paint()
                    .Circle(0, 0, 3.6f, Color.Lerp(l.Wheels, Color.black, 0.15f), 16)
                    .Circle(0, 0, 3, l.Wheels, 16)
                    .Circle(0, 0, 1.4f, metalDark, 10)
                    .Rect(-0.5f, 1.2f, 1, 1.6f, Color.Lerp(l.Wheels, Color.black, 0.45f))
                    .Make("Roue", deck, order + 2, layer).transform;
                w.localPosition = W(i == 0 ? -14 : 14, -3.1f);   // roues posées sur le sol
                wheels[i] = w;
            }

            // dessus (grip, vis des trucks) et dessous (graphisme, trucks et roues) : vus quand la planche se retourne
            var top = new Paint().Capsule(-25, 0, 25, 0, 14, l.Deck).Capsule(-24.5f, 0, 24.5f, 0, 12.4f, grip);
            var r = new System.Random(7);
            for (int i = 0; i < 40; i++) top.Circle((float)r.NextDouble() * 46 - 23, (float)r.NextDouble() * 9 - 4.5f, 0.35f, Hex("#4A4A52"), 4);
            foreach (float cx in new[] { -14f, 14f })
                for (int i = 0; i < 4; i++) top.Circle(cx + (i % 2 == 0 ? -1.6f : 1.6f), i < 2 ? -2.4f : 2.4f, 0.7f, metal, 6);
            gripFace = top.Make("Dessus", board, order + 1, layer).transform;

            var bottom = new Paint().Capsule(-25, 0, 25, 0, 14, l.Deck);
            var art = Color.Lerp(l.Deck, Color.black, 0.35f);
            switch (l.Art)   // dessous de la planche gagnée (spec 002, FR-025)
            {
                case DeckArt.Wood:
                    for (int i = 0; i < 4; i++) bottom.Line(-20, -4.5f + i * 3, 20, -4 + i * 3, 0.7f, art);
                    break;
                case DeckArt.Crab:
                    bottom.Ellipse(-6, 0, 7, 4, art, 16).Ellipse(7, 2.5f, 5, 2.2f, art, 12).Ellipse(7, -2.5f, 5, 2.2f, art, 12)
                          .Poly(Color.white, 10, 2.5f, 13, 3.5f, 10, 4).Poly(Color.white, 10, -2.5f, 13, -3.5f, 10, -4);
                    break;
                case DeckArt.Pepper:
                    for (int i = 0; i < 3; i++) { float x = -12 + i * 12; bottom.Ellipse(x, 0, 5, 2.2f, Hex("#FF3B30"), 12, 0.3f).Capsule(x + 4, 1.5f, x + 6, 2.5f, 1.2f, Hex("#3BB273")); }
                    break;
                case DeckArt.Gold:   // couronne et liserés blancs
                    bottom.Capsule(-21, -4.6f, 21, -4.6f, 0.8f, Color.white).Capsule(-21, 4.6f, 21, 4.6f, 0.8f, Color.white)
                          .Poly(Color.white, -6, -2.5f, 6, -2.5f, 6, 1).Poly(Color.white, -6, 1, -6, 3.5f, -3, 1).Poly(Color.white, -2, 1, 0, 4, 2, 1).Poly(Color.white, 3, 1, 6, 3.5f, 6, 1);
                    break;
                case DeckArt.Neon:
                    bottom.Capsule(-20, -3.5f, 20, -3.5f, 1.6f, l.Wheels).Capsule(-20, 3.5f, 20, 3.5f, 1.6f, l.Wheels).Capsule(-14, 0, 14, 0, 1.2f, Color.white);
                    break;
            }
            foreach (float cx in new[] { -14f, 14f })
            {
                bottom.Rect(cx - 1.6f, -7.5f, 3.2f, 15, metal);
                bottom.Capsule(cx, -8.2f, cx, -6.8f, 3.4f, l.Wheels).Capsule(cx, 6.8f, cx, 8.2f, 3.4f, l.Wheels);
            }
            bottomFace = bottom.Make("Dessous", board, order + 2, layer).transform;
            gripFace.gameObject.SetActive(false);
            bottomFace.gameObject.SetActive(false);
        }

        /// <summary>Motif de chaque haut, de la ceinture et du bas (spec 002 : chaque objet a son dessin).</summary>
        static Paint TopDesign(RiderLook l)
        {
            var p = new Paint();
            var dark = Color.Lerp(l.Top, Color.black, 0.25f);
            bool hoodie = l.TopId.StartsWith("hoodie");
            if (hoodie)
            {
                p.Ellipse(-5.5f, 19.5f, 4.6f, 3.6f, dark, 12);                                  // capuche dans le dos
                p.Rect(-4, 4.5f, 10, 4, dark);                                                   // poche kangourou
                p.Line(4.5f, 19, 5, 14.5f, 0.7f, Color.white).Line(6.5f, 18.5f, 7, 14.5f, 0.7f, Color.white);   // cordons
            }
            switch (l.TopId)
            {
                case "start-top":   // t-shirt : un petit palmier orange sur la poitrine
                    p.Line(4.5f, 9, 5, 14, 0.9f, Hex("#7A4B2A"));
                    for (int i = 0; i < 4; i++) { float a = 0.4f + i * 0.75f; p.Line(5, 14, 5 + Mathf.Cos(a) * 3, 14 + Mathf.Sin(a) * 1.6f, 0.9f, Hex("#FF7A1A")); }
                    break;
                case "hoodie-ocean":   // vague blanche
                    p.Curve(new Vector2(-4, 11), new Vector2(0, 15), new Vector2(3, 11), 1.1f, 1.1f, Color.white, 6)
                     .Curve(new Vector2(3, 11), new Vector2(5.5f, 9), new Vector2(8, 12), 1.1f, 0.6f, Color.white, 5);
                    break;
                case "hoodie-purple":   // ciel étoilé
                    foreach (var (x, y) in new[] { (-3f, 13f), (1f, 16f), (5f, 12f), (2f, 10f), (7f, 15.5f) }) p.Circle(x, y, 0.7f, Hex("#FFE14D"), 6);
                    break;
                case "hoodie-basque":   // drapeau basque : croix verte en X, croix blanche
                    p.Line(-3, 8, 7, 16, 1.4f, Hex("#009B48")).Line(-3, 16, 7, 8, 1.4f, Hex("#009B48"))
                     .Line(2, 7.5f, 2, 16.5f, 1.4f, Color.white).Line(-3.5f, 12, 7.5f, 12, 1.4f, Color.white);
                    break;
                case "hoodie-gold":   // reflets et chaîne
                    p.Line(-4, 16, 6, 7, 1.6f, new Color(1, 1, 1, 0.45f));
                    p.Curve(new Vector2(1, 18), new Vector2(4, 12), new Vector2(7, 18), 0.6f, 0.6f, Hex("#FFF4D6"), 6).Circle(4, 13.6f, 1.2f, Hex("#FFF4D6"), 8);
                    break;
                case "jersey-pilier": case "jersey-talonneur":   // maillot de rugby : cerceau et col blancs (spec 004)
                    p.Rect(-7.2f, 11, 15.6f, 3, Hex("#F7F4EC")).Capsule(-0.6f, 19.6f, 5.4f, 19, 2.4f, Hex("#F7F4EC"));
                    break;
            }
            switch (l.LegsId)
            {
                case "pants-white":   // tenue des fêtes : ceinture rouge nouée
                    p.Rect(-7, 1.5f, 14, 3, Hex("#D52B1E")).Poly(Hex("#D52B1E"), -6, 2, -9, -5, -6.5f, -5);
                    break;
                case "pants-green":
                    p.Rect(-6, 2.2f, 12, 1.2f, Hex("#1B1028"));   // ceinture
                    break;
            }
            return p;
        }

        // ---------- dessin des pièces (spec 003) ----------

        const float Outline = 0.7f;   // contour d'encre (unités du skateur) : il suit l'échelle du skateur
        static readonly Color Ink = Hex("#1B1028");
        static Color Shade(Color c) => Color.Lerp(c, Color.black, 0.25f);
        static Color Light(Color c) => Color.Lerp(c, Color.white, 0.2f);

        /// <summary>
        /// Une pièce : sa silhouette gonflée de Outline en encre (Ink), et ses couleurs par-dessus (Col).
        /// Le contour d'un membre est posé sous toutes ses couleurs : aucun trait aux articulations.
        /// </summary>
        internal sealed class Piece
        {
            public readonly Paint Ink = new Paint(), Col = new Paint();
            public Piece Capsule(float x0, float y0, float x1, float y1, float w, Color c)
            { Ink.Capsule(x0, y0, x1, y1, w + 2 * Outline, RiderView.Ink); Col.Capsule(x0, y0, x1, y1, w, c); return this; }
            public Piece Taper(float x0, float y0, float x1, float y1, float w0, float w1, Color c)
            { Ink.Taper(x0, y0, x1, y1, w0 + 2 * Outline, w1 + 2 * Outline, RiderView.Ink); Col.Taper(x0, y0, x1, y1, w0, w1, c); return this; }
            public Piece Ellipse(float cx, float cy, float rx, float ry, Color c, int seg = 16, float rot = 0)
            { Ink.Ellipse(cx, cy, rx + Outline, ry + Outline, RiderView.Ink, seg, rot); Col.Ellipse(cx, cy, rx, ry, c, seg, rot); return this; }
            public Piece Circle(float cx, float cy, float r, Color c, int seg = 14) => Ellipse(cx, cy, r, r, c, seg);
            public Piece Dome(float cx, float cy, float rx, float ry, Color c, int seg = 14)
            {
                Ink.Dome(cx, cy, rx + Outline, ry + Outline, RiderView.Ink, seg).Rect(cx - rx - Outline, cy - Outline, 2 * (rx + Outline), Outline, RiderView.Ink);
                Col.Dome(cx, cy, rx, ry, c, seg);
                return this;
            }
            /// <summary>Polygone convexe ; son contour suit chaque bord.</summary>
            public Piece Poly(Color c, params float[] xy)
            {
                Ink.Poly(RiderView.Ink, xy);
                for (int i = 0, n = xy.Length / 2; i < n; i++)
                {
                    int j = (i + 1) % n;
                    Ink.Capsule(xy[2 * i], xy[2 * i + 1], xy[2 * j], xy[2 * j + 1], 2 * Outline, RiderView.Ink);
                }
                Col.Poly(c, xy);
                return this;
            }
            public Piece Rect(float x, float y, float w, float h, Color c) => Poly(c, x, y, x + w, y, x + w, y + h, x, y + h);
            public void Make(string name, Transform t, int order, string layer)
            {
                Ink.Make(name + " contour", t, order, layer);
                Col.Make(name, t, order + 1, layer);
            }
        }

        /// <summary>
        /// Jambe : cuisse effilée, tibia et chaussure. Au genou, la cuisse finit par un demi-disque du rayon du haut du
        /// tibia, centré sur l'articulation ; le contour de toute la jambe est sous ses couleurs (ordres : encre de la jambe,
        /// couleurs de la jambe, encre de la chaussure, couleurs de la chaussure).
        /// </summary>
        LegRig MakeLeg(string name, int order, string layer, float tint)
        {
            var l = look;
            Color Tint(Color c) => Color.Lerp(Color.black, c, tint);
            Transform Part(string part, Piece p, int o)
            {
                var t = new GameObject(name + " " + part).transform;
                t.SetParent(body, false);
                p.Ink.Make(part + " contour", t, o, layer);
                p.Col.Make(part, t, o + 1, layer);
                return t;
            }
            Color pants = Tint(l.Pants), skin = Tint(l.Skin);
            float knee = l.Shorts ? 6 : 8;   // largeur au genou : la même pour la cuisse et le tibia
            var thigh = new Piece();
            if (l.Shorts)
            {
                thigh.Taper(0, 0, 0, -Thigh, 7.4f, knee, skin)
                     .Taper(0, 0.5f, 0, -Thigh * 0.62f, 10, 9, pants);
                thigh.Col.Line(-4.4f, -Thigh * 0.62f + 0.4f, 4.4f, -Thigh * 0.62f + 0.4f, 0.8f, Shade(pants))   // ourlet
                     .Capsule(-2.2f, -2, -1.4f, -Thigh * 0.55f, 2.4f, Shade(pants))
                     .Line(-1.2f, -Thigh + 1.2f, 1.6f, -Thigh + 0.6f, 0.5f, Shade(skin));                       // genou
            }
            else
            {
                thigh.Taper(0, 0, 0, -Thigh, 9.4f, knee, pants);
                thigh.Col.Taper(-2.6f, -0.5f, -2.2f, -Thigh + 1, 3.2f, 2.6f, Shade(pants))                   // ombre dessous
                     .Line(3.2f, -1.5f, 2.8f, -Thigh + 2.5f, 0.9f, Light(pants))                              // lumière dessus
                     .Line(-2.6f, -2.2f, 1.2f, -3.6f, 0.5f, Shade(pants))                                       // plis de l'aine
                     .Line(-1.6f, -Thigh + 2.6f, 2.4f, -Thigh + 1.6f, 0.5f, Shade(pants));                      // pli du genou
            }
            switch (l.LegsId)
            {
                case "start-legs":   // jean : genou usé et couture
                    thigh.Col.Rect(-3, -Thigh + 1.5f, 5.5f, 3.5f, Tint(Color.Lerp(l.Pants, Color.white, 0.35f)))
                         .Line(-3.2f, -Thigh + 3, 2.2f, -Thigh + 3.4f, 0.5f, Tint(Color.white)).Line(3.4f, -1, 3.2f, -Thigh + 1, 0.4f, Tint(Hex("#E9B44C")));
                    break;
                case "shorts-surf":   // hibiscus
                    foreach (var (x, y) in new[] { (-1.5f, -2.5f), (2f, -6.5f) })
                    {
                        for (int i = 0; i < 5; i++) { float a = i * Mathf.PI * 2 / 5; thigh.Col.Circle(x + Mathf.Cos(a) * 1.4f, y + Mathf.Sin(a) * 1.4f, 1.1f, Tint(Hex("#FF4D8D")), 8); }
                        thigh.Col.Circle(x, y, 0.7f, Tint(Hex("#FFE14D")), 6);
                    }
                    break;
                case "pants-green":   // cargo : grande poche à rabat
                    thigh.Col.Rect(-3.2f, -Thigh * 0.7f, 6, 5.5f, Tint(Color.Lerp(l.Pants, Color.black, 0.25f)))
                         .Rect(-3.4f, -Thigh * 0.7f + 4, 6.4f, 1.6f, Tint(Color.Lerp(l.Pants, Color.black, 0.4f)));
                    break;
            }
            var shin = new Piece();
            if (l.Shorts)
            {
                bool rugby = l.LegsId == "shorts-pilier" || l.LegsId == "shorts-talonneur";   // chaussettes hautes à bandes blanches
                float sock = rugby ? 6.5f : 3.5f;
                shin.Taper(0, 0, 0, -Shin + 1, knee, 5.2f, skin).Rect(-3.2f, -Shin + 0.5f, 6.4f, sock, Tint(l.Socks));   // chaussettes
                shin.Col.Line(-1.8f, -2, -1.6f, -Shin + 4.5f + (sock - 3.5f), 1.2f, Shade(skin)).Line(-3.2f, -Shin + 0.5f + sock - 0.1f, 3.2f, -Shin + 0.5f + sock - 0.1f, 0.5f, Shade(Tint(l.Socks)));
                if (rugby) shin.Col.Rect(-3.2f, -Shin + 3, 6.4f, 0.9f, Tint(Color.white)).Rect(-3.2f, -Shin + 4.6f, 6.4f, 0.9f, Tint(Color.white));
            }
            else
            {
                shin.Taper(0, 0, 0, -Shin + 1, knee, 7.4f, pants).Rect(-4, -Shin, 8, 2.4f, Shade(pants));          // bas du jean
                shin.Col.Taper(-2.3f, -1, -2, -Shin + 2.5f, 2.8f, 2.4f, Shade(pants))
                     .Line(2.6f, -2, 2.4f, -Shin + 3, 0.8f, Light(pants))
                     .Line(-2.4f, -Shin + 4, 2.6f, -Shin + 3.3f, 0.5f, Shade(pants));                           // pli au-dessus de la chaussure
            }
            // chaussure : la cheville est l'origine, la semelle 4 plus bas, la pointe vers l'avant
            Color shoes = Tint(l.Shoes), sole = Tint(l.ShoesId == "shoes-yellow" ? Hex("#1B1028") : Hex("#F4F1EA"));
            var shoe = new Piece()
                .Ellipse(3, -1.4f, 6.8f, 2.9f, shoes, 18)
                .Rect(-3.8f, -4, 13.6f, 1.9f, sole);
            shoe.Col.Ellipse(1.5f, -2, 4.8f, 1.6f, Shade(shoes), 14)                                                // ombre du flanc
                .Ellipse(3, -1.1f, 6.2f, 2.3f, shoes, 18)
                .Ellipse(8.2f, -1.9f, 2.5f, 1.8f, Tint(l.ShoesId == "start-shoes" ? Hex("#C9CED6") : Light(l.Shoes)), 12)   // embout
                .Curve(new Vector2(-2.6f, -2.3f), new Vector2(2.5f, 0.2f), new Vector2(6.8f, -2.4f), 0.8f, 0.8f, Tint(Light(l.Shoes)), 6)   // bande
                .Line(-3.6f, -2.5f, 9.6f, -2.5f, 0.45f, Shade(sole));                                              // semelle
            for (int i = 0; i < 3; i++) shoe.Col.Line(2.4f + i * 1.6f, 1 - i * 0.45f, 3.6f + i * 1.6f, 0.4f - i * 0.45f, 0.55f, Tint(Color.white));   // lacets
            switch (l.ShoesId)
            {
                case "shoes-yellow":   // bande noire
                    shoe.Col.Curve(new Vector2(-1, -2.4f), new Vector2(3, 0.5f), new Vector2(7, -2.6f), 1, 1, Tint(Hex("#1B1028")), 6);
                    break;
                case "shoes-pink":     // lacets blancs, bande violette
                    for (int i = 0; i < 3; i++) shoe.Col.Circle(1.5f + i * 2, 0.6f - i * 0.5f, 0.55f, Tint(Color.white), 6);
                    shoe.Col.Rect(-3, -2.8f, 11, 0.9f, Tint(Hex("#7B2CBF")));
                    break;
                case "boots-pilier": case "boots-talonneur":   // baskets noires à bande de la couleur du boss (spec 004)
                    shoe.Col.Curve(new Vector2(-1, -2.4f), new Vector2(3, 0.5f), new Vector2(7, -2.6f), 1.1f, 1.1f, Tint(l.ShoesId == "boots-pilier" ? Hex("#D7263D") : Hex("#5DADE2")), 6);
                    break;
            }
            return new LegRig { Thigh = Part("cuisse", thigh, order), Shin = Part("tibia", shin, order), Shoe = Part("chaussure", shoe, order + 2) };
        }

        /// <summary>Bras : manche, coude marqué, main avec pouce et doigts repliés. Ordres : encre puis couleurs.</summary>
        Transform Arm(string name, Transform parent, int order, string layer, float tint)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            var l = look;
            Color Tint(Color c) => Color.Lerp(Color.black, c, tint);
            Color top = Tint(l.Top), skin = Tint(l.Skin);
            var p = new Piece();
            bool jersey = l.TopId.StartsWith("jersey");
            if (l.TopId == "start-top" || jersey)   // t-shirt et maillot : manches courtes
            {
                p.Taper(0, -3, 0.5f, -9, 4.6f, 4.2f, skin).Taper(0.5f, -9, 0, -13.4f, 4.2f, 3.8f, skin)
                 .Capsule(0, 0, 0, -4.4f, 6.4f, top);
                p.Col.Capsule(-1.2f, -0.2f, -1.2f, -4.2f, 2.6f, Shade(top))
                     .Line(-3.1f, -5.5f, 3.1f, -5.5f, 0.6f, Ink);                       // ourlet de la manche
                if (jersey) p.Col.Line(-3, -3.9f, 3, -3.9f, 1.3f, Tint(Hex("#F7F4EC")));   // bande blanche de la manche
            }
            else
            {
                p.Taper(0, 0, 0.5f, -9, 6.2f, 5.6f, top).Taper(0.5f, -9, 0.2f, -11.6f, 5.6f, 5.2f, top);
                p.Col.Taper(-1.4f, -0.5f, -1, -10.5f, 2.8f, 2.2f, Shade(top))
                     .Rect(-2.6f, -12.4f, 5.6f, 1.6f, Shade(top));                    // poignet
            }
            p.Col.Line(-0.6f, -8.6f, 1.6f, -9.4f, 0.5f, Shade(l.TopId == "start-top" || jersey ? skin : top));   // coude
            // main : paume, doigts repliés vers l'avant, pouce
            p.Circle(0.2f, -15, 2.4f, skin, 12)
             .Capsule(-1.2f, -16.4f, 1.8f, -16.6f, 1.9f, skin)
             .Capsule(1.6f, -13.8f, 2.8f, -15.4f, 1.4f, skin);
            p.Col.Line(-0.3f, -15.6f, -0.3f, -17.2f, 0.35f, Shade(skin)).Line(0.8f, -15.7f, 0.8f, -17.3f, 0.35f, Shade(skin));
            p.Make(name, t, order, layer);
            return t;
        }

        /// <summary>Tête de profil (vers la droite) : front, nez, lèvres, menton, oreille, joue, œil, sourcil ; cheveux qui suivent le crâne.</summary>
        static Transform Head(RiderLook l, int order, string layer)
        {
            var p = new Piece();
            p.Capsule(0.2f, -8, 0.6f, -3, 4.4f, Shade(l.Skin));                    // cou
            p.Circle(-0.5f, 0.6f, 7.2f, l.Skin, 22);                                  // crâne
            p.Poly(l.Skin, 0, -7.2f, 5.2f, -7.2f, 6.9f, -5.6f, 7.3f, -4.6f, 7.4f, -3.2f, 7.35f, -2.6f, 7, 1.2f, 6.2f, 4.2f, 0, 5);   // visage
            p.Poly(l.Skin, 6.9f, 0.6f, 9.1f, -1.7f, 8.5f, -2.4f, 7.1f, -2.5f);         // nez
            p.Col.Line(7.3f, -2.4f, 8.4f, -2.3f, 0.4f, Shade(l.Skin))                // narine
                .Ellipse(2.4f, -5.6f, 3.6f, 1.2f, Shade(l.Skin), 12)                  // dessous de la mâchoire
                .Ellipse(3.4f, -5.2f, 3.4f, 1.1f, l.Skin, 12);
            // cheveux, puis couvre-chef gagné (spec 002)
            if (l.Hat == Hat.Cap || l.Hat == Hat.Beanie || l.Hat == Hat.Scrum)
                p.Ellipse(-6, -0.6f, 2.6f, 3.2f, l.Hair, 10);                         // le couvre-chef cache les cheveux : seule la nuque dépasse
            else if (l.Curly)
                for (int i = 0; i < 9; i++)
                {
                    float a = Mathf.PI * (0.05f + i / 8f * 1.05f);
                    p.Circle(-0.5f + Mathf.Cos(a) * 7, 1.8f + Mathf.Sin(a) * 6.5f, 3.6f, l.Hair, 10);
                }
            else
            {
                p.Ellipse(-1.6f, 3.2f, 7, 5.2f, l.Hair, 18, 0.2f)                    // calotte qui suit le crâne
                 .Ellipse(-5.4f, -0.4f, 3, 4.4f, l.Hair, 12)                          // nuque
                 .Ellipse(4.2f, 5.6f, 2.4f, 1.4f, l.Hair, 10, -0.5f);                 // mèche du front
                p.Col.Line(-4, 6.2f, 1.5f, 7.4f, 0.6f, Light(l.Hair)).Line(-5.6f, 3, -3.2f, 5.6f, 0.5f, Shade(l.Hair));
            }
            p.Col.Ellipse(-2, -0.4f, 2, 2.5f, Shade(l.Skin), 12).Ellipse(-1.8f, -0.4f, 1.4f, 1.9f, l.Skin, 10)   // oreille et son creux
                .Ellipse(-1.6f, -0.6f, 0.6f, 1, Shade(l.Skin), 8);
            switch (l.Hat)
            {
                case Hat.Cap:
                    p.Dome(-0.4f, 1.4f, 8.2f, 7.8f, l.HatColor, 14)
                     .Capsule(-7.4f, 2.2f, -13, 1.2f, 3, l.HatColor);                 // visière à l'envers
                    p.Col.Line(-7.8f, 1.6f, 7.6f, 1.6f, 0.6f, Shade(l.HatColor)).Circle(-0.4f, 9.2f, 1.3f, Shade(l.HatColor), 8);   // bouton
                    if (l.HeadId == "cap-gold")   // étoile blanche devant
                        for (int i = 0; i < 5; i++) { float a = Mathf.PI / 2 + i * Mathf.PI * 2 / 5; p.Col.Line(3.8f, 4.6f, 3.8f + Mathf.Cos(a) * 2.2f, 4.6f + Mathf.Sin(a) * 2.2f, 0.9f, Color.white); }
                    else p.Col.Rect(2.1f, 3.2f, 3.6f, 2.6f, Color.white).Rect(2.8f, 3.8f, 2.2f, 1.4f, l.HatColor);   // petit « P »
                    break;
                case Hat.Beanie:
                    p.Dome(-0.4f, 1, 8, 9.3f, l.HatColor, 14).Circle(-0.4f, 10.8f, 3, Color.white, 10);
                    if (l.Stripes) p.Col.Rect(-7.8f, 4.5f, 14.8f, 1.8f, Color.white).Rect(-6.4f, 7.5f, 12, 1.6f, Color.white);
                    p.Col.Rect(-8.4f, 0, 16, 3.4f, Light(l.HatColor)).Line(-8.4f, 0.2f, 7.6f, 0.2f, 0.5f, Shade(l.HatColor));
                    break;
                case Hat.Band:   // bandana à pois blancs ; bandeau du Pilier : blanc à liseré rouge
                    p.Rect(-7.8f, 2, 15, 3, l.HatColor).Capsule(-8, 3.5f, -12, 0, 2.4f, l.HatColor);
                    if (l.HeadId == "band-pilier") p.Col.Line(-7.8f, 2.6f, 7.2f, 2.6f, 0.6f, Hex("#D7263D")).Line(-7.8f, 4.4f, 7.2f, 4.4f, 0.6f, Hex("#D7263D"));
                    else for (int i = 0; i < 4; i++) p.Col.Circle(-5 + i * 3.5f, 3.5f, 0.5f, Color.white, 6);
                    break;
                case Hat.Scrum:   // casque de mêlée : calotte bleu ciel à bandes blanches, protège-oreille
                    p.Dome(-0.6f, 0.4f, 8.2f, 8.4f, l.HatColor, 16).Circle(-2, -0.6f, 3.2f, l.HatColor, 14);
                    p.Col.Line(-0.6f, 0.4f, -0.6f, 8.8f, 1.1f, Color.white).Line(-8.6f, 4, 7.4f, 4, 1.1f, Color.white)
                        .Circle(-2, -0.6f, 1.6f, Shade(l.HatColor), 10)
                        .Line(1, -1, 4.6f, -6.8f, 0.6f, Ink);   // jugulaire
                    break;
            }
            // visage : joue, œil (blanc, iris, reflet), paupière, sourcil, lèvres
            p.Col.Circle(4.4f, -3.2f, 1.6f, new Color(1, 0.45f, 0.5f, 0.35f), 10)
                .Ellipse(4.9f, 0.7f, 1.5f, 1.1f, Color.white, 12)
                .Circle(5.5f, 0.6f, 0.85f, Hex("#3A2A1E"), 10)
                .Circle(5.8f, 0.95f, 0.3f, Color.white, 6)
                .Line(3.3f, 1.8f, 6.4f, 1.6f, 0.5f, Ink);
            if (l.Hat != Hat.Beanie) p.Col.Line(3.4f, 3, 6.5f, 2.8f, 0.8f, Shade(l.Hair));   // sourcil
            p.Col.Line(6.4f, -4.1f, 7.35f, -4, 0.45f, Color.Lerp(Shade(l.Skin), Hex("#B23A48"), 0.5f))   // bouche
                .Line(6.7f, -4.6f, 7.2f, -4.6f, 0.5f, Light(l.Skin));                                         // lèvre
            var t = new GameObject("Tête").transform;
            p.Make("Tête", t, order, layer);
            return t;
        }


        // ---------- poses ----------

        /// <summary>
        /// Pose du skateur. Les semelles sont posées sur le grip (sur les vis des trucks), suivent l'inclinaison de la
        /// planche (tilt, °) et s'en écartent de lift pendant une figure ; les genoux se plient par cinématique inverse.
        /// crouch : 0 debout, 1 accroupi ; lean : buste penché en avant (°) ; bras en ° (0 = le long du corps, 90 = devant) ;
        /// push : phase de la poussée (0 à 1, le pied arrière va pousser sur le sol), négatif sans poussée.
        /// </summary>
        void Pose(float crouch, float lean, float armFront, float armBack, float tilt = 0, float lift = 0, float push = -1, float forward = 0)
        {
            body.localPosition = W(0, -Center);
            var rot = Quaternion.Euler(0, 0, tilt);
            Vector2 Top(float x) => new Vector2(0, DeckY + lift) + (Vector2)(rot * new Vector3(x, GripTop, 0));
            Vector2 back = Top(BackFoot), front = Top(FrontFoot);
            float pushWeight = push >= 0 ? Mathf.Sin(push * Mathf.PI) : 0;
            crouch += pushWeight * 0.5f;
            var hip = new Vector2((back.x + front.x) / 2 - 1 + lean * 0.05f + forward, (back.y + front.y) / 2 + Ankle + 25 * (1 - 0.5f * Mathf.Clamp01(crouch)));
            if (push >= 0) back = Vector2.Lerp(back, new Vector2(-4 - 10 * push, 0), pushWeight);   // le pied arrière pousse sur le sol
            Solve(legB, hip + new Vector2(-1, 0), back, push >= 0 ? tilt * (1 - pushWeight) : tilt);
            Solve(legF, hip + new Vector2(1, 0), front, tilt);
            torso.localPosition = W(hip.x, hip.y - 1);
            torso.localRotation = Quaternion.Euler(0, 0, -lean);
            armF.localRotation = Quaternion.Euler(0, 0, armFront);
            armB.localRotation = Quaternion.Euler(0, 0, armBack);
        }

        static void Solve(LegRig leg, Vector2 hip, Vector2 sole, float tilt)
        {
            var ankle = sole + (Vector2)(Quaternion.Euler(0, 0, tilt) * new Vector3(0, Ankle, 0));
            var d = ankle - hip;
            float reach = Thigh + Shin - 0.05f, dist = Mathf.Clamp(d.magnitude, 0.5f, reach);
            if (d.magnitude > reach) ankle = hip + d.normalized * reach;   // jambe tendue : le pied suit
            float bend = Mathf.Acos(Mathf.Clamp((Thigh * Thigh + dist * dist - Shin * Shin) / (2 * Thigh * dist), -1, 1));
            float a = Mathf.Atan2(d.y, d.x) + bend;   // genou vers l'avant
            var knee = hip + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * Thigh;
            static float Angle(Vector2 v) => Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg + 90;   // les segments sont dessinés vers le bas
            leg.Thigh.localPosition = W(hip.x, hip.y);
            leg.Thigh.localRotation = Quaternion.Euler(0, 0, Angle(knee - hip));
            leg.Shin.localPosition = W(knee.x, knee.y);
            leg.Shin.localRotation = Quaternion.Euler(0, 0, Angle(ankle - knee));
            leg.Shoe.localPosition = W(ankle.x, ankle.y);
            leg.Shoe.localRotation = Quaternion.Euler(0, 0, tilt);
        }

        /// <summary>
        /// Planche : tilt = nez relevé (°), flip = rotation dans sa longueur (rad, kickflip), shove = rotation à plat
        /// (rad, shove-it), drop = la planche descend sous les pieds pendant la figure.
        /// Le flip montre le grip puis le dessous ; le shove-it raccourcit la planche jusqu'à la voir par le bout.
        /// </summary>
        void Board(float tilt, float flip = 0, float shove = 0, float drop = 0)
        {
            board.localPosition = W(0, DeckY - Center - drop);
            float sx = Mathf.Cos(shove), sx2 = Mathf.Sign(sx) * Mathf.Max(Mathf.Abs(sx), 0.24f);
            float cy = Mathf.Cos(flip), sy = Mathf.Sin(flip);
            deck.localRotation = Quaternion.Euler(0, 0, tilt);
            deck.localScale = new Vector3(sx2, cy, 1);
            // shove-it : vu un peu d'en haut, le grip apparaît quand la planche est de travers
            float ss = Mathf.Abs(Mathf.Sin(shove));
            if (ss > Mathf.Abs(sy)) { sy = ss * 0.6f; cy = 1; }
            bool face = Mathf.Abs(sy) > 0.08f;
            gripFace.gameObject.SetActive(face && sy > 0);
            bottomFace.gameObject.SetActive(face && sy < 0);
            if (!face) return;
            var f = sy > 0 ? gripFace : bottomFace;
            f.localPosition = W(0, -1 * cy);
            f.localRotation = Quaternion.Euler(0, 0, tilt);
            f.localScale = new Vector3(shove != 0 ? Mathf.Max(Mathf.Abs(sx), 0.28f) : sx2, Mathf.Abs(sy) * (shove != 0 ? 0.55f : 1), 1);
        }

        void Wheels(float distance)
        {
            foreach (var w in wheels) w.localRotation = Quaternion.Euler(0, 0, -distance * 17);
        }

        /// <summary>Pose en partie, d'après l'état du skateur. feet : position monde des pieds.</summary>
        float duck, lastTime, drawY = float.NaN;   // duck : 0 debout → 1 baissé, en douceur ; drawY : hauteur des pieds affichée

        public void Ride(Skater s, Vector3 feet, float slope, float time, float speedFactor)
        {
            if (crashed) Reset();
            float dt = Mathf.Clamp(time - lastTime, 0, 0.1f);
            lastTime = time;
            // se baisser annule le saut qui venait de partir : les pieds redescendent en 0,1 s au lieu de sauter au sol
            if (s.Ducking && s.Grounded && !float.IsNaN(drawY) && drawY > feet.y) feet.y = Mathf.MoveTowards(drawY, feet.y, dt * 9);
            drawY = feet.y;
            duck = Mathf.MoveTowards(duck, s.Ducking && s.Grounded ? 1 : 0, dt * 9);   // ~0,11 s pour se baisser ou se relever
            float tilt = s.Grounded || s.Rail != null ? Mathf.Atan(slope) * Mathf.Rad2Deg : 0;
            spin.position = feet + W(0, Center);
            float current = spin.localEulerAngles.z;
            if (current > 180) current -= 360;
            float spinAngle = Mathf.Lerp(current, tilt, 0.35f);
            Wheels(s.X);
            bool airborne = !s.Grounded && s.Rail == null;
            if (wasAirborne && !airborne) landedAt = time;
            wasAirborne = airborne;

            float p = s.Trick >= 0 ? Mathf.Clamp01((float)s.TrickStep / s.TrickLength) : 0;
            float ease = 1 - (1 - p) * (1 - p) * (1 - p);   // rapide au départ, rattrapée en douceur
            float lift = Mathf.Sin(p * Mathf.PI);            // les pieds quittent la planche au milieu de la figure
            if (s.Rail != null)
            {
                // nose grind : le nez sur le rail, la queue levée ; le skateur penché sur l'avant, bras en balancier
                const float nose = -18, up = 4.5f;   // up : la roue avant reste posée sur le rail
                switch (s.GrindStyle)
                {
                    case 1:   // 50-50 : planche à plat, les deux trucks sur le rail, bras ouverts
                        Pose(0.4f, 12, 95, -80);
                        Board(0);
                        break;
                    case 2:   // BOARDSLIDE : planche en travers du rail, on la voit par le dessus, skateur de face
                        Pose(0.5f, 4, 110, -110);
                        Board(0, 0, Mathf.PI / 2, 2);
                        break;
                    case 3:   // CROOKED : nez sur le rail, planche de biais
                        Pose(0.5f, 30, 120, -40, nose, up, -1, 8);
                        Board(nose, 0, 0.7f, -up);
                        break;
                    case 4:   // 5-0 : sur le truck arrière, nez levé, penché en arrière
                        Pose(0.45f, -8, 70, -110, -nose * 0.8f, up, -1, -4);
                        Board(-nose * 0.8f, 0, 0, -up);
                        break;
                    case 5:   // LIPSLIDE : planche en travers, passée par-dessus le rail (l'autre sens du boardslide)
                        Pose(0.5f, -4, 100, -120);
                        Board(0, 0, -Mathf.PI / 2, 2);
                        break;
                    case 6:   // TAILSLIDE : la queue glisse, planche de biais, nez levé
                        Pose(0.5f, -6, 80, -100, -nose * 0.7f, up, -1, -3);
                        Board(-nose * 0.7f, 0, 0.8f, -up);
                        break;
                    case 7:   // FEEBLE : truck arrière sur le rail, l'avant passé de l'autre côté, un peu levé
                        Pose(0.45f, 8, 90, -70, 8, 2);
                        Board(8, 0, -0.5f, -2);
                        break;
                    case 8:   // SMITH : truck arrière sur le rail, le nez plonge du côté du skateur
                        Pose(0.55f, 20, 110, -60, -10, 0, -1, 4);
                        Board(-10, 0, 0.45f, 3);
                        break;
                    default:   // NOSE GRIND
                        Pose(0.45f, 26, 115, -45, nose, up, -1, 7);
                        Board(nose, 0, 0, -up);
                        break;
                }
            }
            else if (s.Grounded)
            {
                // rouler : petit balancement, et une poussée de temps en temps sur le plat
                float cycle = Mathf.Repeat(time, 2.6f), push = cycle < 0.7f && speedFactor < 1.05f && !s.Ducking ? cycle / 0.7f : -1;
                float squash = Mathf.Max(0, 1 - (time - landedAt) / 0.2f) * 0.6f;   // genoux qui amortissent la retombée
                if (s.Manual && s.Nose)   // nose manual : sur les roues avant, penché en avant, bras ouverts
                {
                    float nose = -(12 + Mathf.Sin(time * 9) * 2), up = Mathf.Sin(-nose * Mathf.Deg2Rad) * 22;
                    Pose(0.4f, 24, 100 + Mathf.Sin(time * 9) * 10, -60, nose, up, -1, 6);
                    Board(nose, 0, 0, -up);
                }
                else if (s.Manual)   // manual : sur les roues arrière, penché en arrière, bras en balancier
                {
                    float nose = 13 + Mathf.Sin(time * 9) * 2, up = Mathf.Sin(nose * Mathf.Deg2Rad) * 22;
                    Pose(0.35f, -6, 75 + Mathf.Sin(time * 9) * 10, -100, nose, up);
                    Board(nose, 0, 0, -up);
                }
                else
                {
                    if (duck > 0)   // se baisser / se relever : mélange de la pose qui roule et de la pose baissée
                    {
                        float k = duck * duck * (3 - 2 * duck);
                        Pose(Mathf.Lerp(0.15f + squash, 1, k), Mathf.Lerp(10, 32, k), Mathf.Lerp(-14, 55, k), Mathf.Lerp(22, 15, k));
                    }
                    else if (s.Wobble > 0)   // BAIL : il titube, bras en moulinet
                    {
                        float w = s.Wobble / 30f, a = Mathf.Sin(time * 22) * w;
                        Pose(0.5f + squash, 10 + a * 25, 130 + a * 50, 120 - a * 50);
                    }
                    else Pose(0.15f + squash + Mathf.Sin(time * 2.4f) * 0.04f, 10 + Mathf.Max(0, Mathf.Sin(push * Mathf.PI)) * 12, -14 + Mathf.Sin(time * 2.4f) * 6, 22, 0, 0, push);
                    Board(0);
                }
            }
            else
            {
                float rise = Mathf.Clamp(s.Vy / Tuning.OllieSpeed, -1, 1);
                Ollie(rise, out float nose, out float up);
                Pose(0.2f + (1 - Mathf.Abs(rise)) * 0.5f, 14, 60 + rise * 40, -70 - rise * 30, nose, up);
                Board(nose, 0, 0, -up);
            }

            switch (s.Trick)
            {
                case 0:   // FLIP : genoux remontés, pieds écartés de la planche qui tourne bien visible dessous, nez un peu levé
                    Pose(0.3f + lift * 0.45f, 12 + lift * 8, 75 + lift * 25, -85 - lift * 20, 0, lift * 10);
                    Board(lift * 14, p * p * (3 - 2 * p) * Mathf.PI * 2, 0, lift * 8);   // tour régulier : on voit chaque face
                    break;
                case 1:   // SHOVE-IT : même saut groupé, la planche fait un tour à plat sous les pieds
                    Pose(0.3f + lift * 0.35f, 6, 95 + lift * 20, -95 - lift * 15, 0, lift * 5);
                    Board(0, 0, p * p * (3 - 2 * p) * Mathf.PI * 2, lift * 2);   // rotation régulière : on la voit tourner
                    break;
                case 2:   // 360° : salto arrière groupé, pieds sur la planche, une main l'attrape
                    Pose(0.95f, 30 + lift * 15, -35, -70);
                    Board(0);
                    spinAngle = ease * 360;
                    break;
                case 3:   // 360° INVERSÉ : salto avant groupé
                    Pose(0.95f, 30 + lift * 15, -20, 60);
                    Board(0);
                    spinAngle = -ease * 360;
                    break;
                case Tuning.HalfTurn:   // 180 : saut groupé, le skateur tourne sur lui-même (voir le miroir plus bas)
                    Pose(0.35f + lift * 0.35f, 10, 70 + lift * 30, -70 - lift * 30, 0, lift * 4);
                    Board(0, 0, 0, lift * 2);
                    break;
                case 4:   // HEELFLIP : le flip dans l'autre sens (le dessous apparaît d'abord), bras ouverts
                    Pose(0.3f + lift * 0.45f, 8 + lift * 8, 95 + lift * 20, -60 - lift * 20, 0, lift * 10);
                    Board(lift * 14, -p * p * (3 - 2 * p) * Mathf.PI * 2, 0, lift * 8);
                    break;
                case 5:   // SHOVE-IT INVERSÉ : la planche tourne à plat dans l'autre sens
                    Pose(0.3f + lift * 0.35f, 6, 95 + lift * 20, -95 - lift * 15, 0, lift * 5);
                    Board(0, 0, -p * p * (3 - 2 * p) * Mathf.PI * 2, lift * 2);
                    break;
                case 6:   // VARIAL FLIP : un flip et un demi-tour à plat en même temps
                    Pose(0.35f + lift * 0.45f, 12 + lift * 8, 85 + lift * 25, -90 - lift * 20, 0, lift * 10);
                    Board(lift * 10, p * p * (3 - 2 * p) * Mathf.PI * 2, p * p * (3 - 2 * p) * Mathf.PI, lift * 9);
                    break;
                case 7:   // 360 FLIP : un flip et un tour complet à plat, saut très groupé
                    Pose(0.45f + lift * 0.45f, 14 + lift * 10, 100 + lift * 20, -100 - lift * 20, 0, lift * 12);
                    Board(lift * 10, p * p * (3 - 2 * p) * Mathf.PI * 2, p * p * (3 - 2 * p) * Mathf.PI * 2, lift * 10);
                    break;
            }
            // fakie : le skateur et sa planche vus en miroir (il regarde en arrière) ; le 180 passe d'un côté à l'autre
            float face = s.Fakie ? -1 : 1;
            if (s.Trick == Tuning.HalfTurn) { float c = -Mathf.Cos(Mathf.PI * ease) * face; face = Mathf.Sign(c) * Mathf.Max(Mathf.Abs(c), 0.12f); }
            if (s.Fakie && (s.Trick == 2 || s.Trick == 3)) spinAngle = -spinAngle;   // les saltos restent arrière / avant pour lui
            spin.localScale = new Vector3(face, 1, 1);
            spin.rotation = Quaternion.Euler(0, 0, spinAngle);
        }

        /// <summary>
        /// Mouvement de la planche pendant un ollie, selon rise (1 au décollage, 0 au sommet, −1 à la retombée) :
        /// la queue claque, le nez monte fort (pivot sur la queue, d'où up), la planche se remet à plat en montant,
        /// puis pique un peu du nez juste avant de toucher le sol.
        /// </summary>
        static void Ollie(float rise, out float nose, out float up)
        {
            float k = Mathf.Clamp01((rise - 0.1f) / 0.6f);
            nose = rise > 0.1f ? 32 * k * k * (3 - 2 * k) : rise < -0.5f ? -8 * (-0.5f - rise) / 0.5f : 0;
            up = Mathf.Max(0, Mathf.Sin(nose * Mathf.Deg2Rad) * 22);   // la queue reste en bas, le centre monte
        }

        Skater demo;
        readonly Obstacle demoRail = new Obstacle { Kind = ObstacleKind.Rail };

        /// <summary>
        /// Démonstration d'une figure en boucle (profil, onglet FIGURES) : roule, saute et fait la figure, retombe.
        /// id : index de Tuning.TrickNames, Tuning.HalfTurn (180), ou Hud.DemoOllie / DemoGrind / DemoManual.
        /// </summary>
        public void Demo(int id, Vector3 feet, float time, float scale)
        {
            if (crashed) Reset();
            transform.localScale = Vector3.one * scale;
            var s = demo ??= new Skater();
            const float cycle = 1.6f;
            int loop = (int)(time / cycle);
            float steps = (Mathf.Repeat(time, cycle) - 0.3f) * 60, h = 0;   // pas depuis le décollage
            s.X = time * 160;
            s.Rail = null; s.Manual = false; s.Ducking = false; s.Wobble = 0; s.GrindStyle = 0; s.Trick = -1; s.Grounded = true; s.Vy = 0;
            s.Fakie = false;
            if (id == Hud.DemoGrind) { s.Rail = demoRail; s.Grounded = false; s.GrindStyle = (int)(time / 0.9f) % Tuning.RailNames.Length; }
            else if (id == Hud.DemoManual) { s.Manual = true; s.Nose = false; }
            else if (id == Hud.DemoNoseManual) { s.Manual = true; s.Nose = true; }
            else
            {
                bool air = steps >= 0 && steps < 38;   // un ollie dure 38 pas
                if (air)
                {
                    s.Grounded = false;
                    s.Vy = Tuning.OllieSpeed - Tuning.Gravity * steps;
                    h = Tuning.OllieSpeed * steps - Tuning.Gravity / 2 * steps * steps;
                    if (id != Hud.DemoOllie && steps >= 3) { s.Trick = id; s.TrickStep = (int)(steps - 3); s.TrickLength = 30; }
                }
                if (id == Tuning.HalfTurn) s.Fakie = (loop % 2 == 1) != steps >= 3;   // un 180 par boucle : à l'endroit, puis en fakie
            }
            var at = feet + W(0, h * 0.55f) * scale;   // saut un peu réduit pour tenir dans le cadre
            Ride(s, at, 0, time, 0.9f);
            spin.localPosition = (at - transform.position) / scale + W(0, Center);
        }

        /// <summary>Pose d'accueil : debout sur la planche, qui respire ; le skateur choisi fait des petits ollies.</summary>
        public void Idle(Vector3 feet, float time, bool selected, float scale)
        {
            if (crashed) Reset();
            transform.localScale = Vector3.one * scale;
            // le skateur choisi enchaîne des petits ollies : même mouvement de planche qu'en partie
            float a = Mathf.Repeat(time * 3.2f, Mathf.PI * 2);
            bool air = selected && a < Mathf.PI;
            float hop = air ? Mathf.Sin(a) : 0, nose = 0, up = 0;
            if (air) Ollie(Mathf.Cos(a), out nose, out up);
            spin.localPosition = (feet - transform.position) / scale + W(0, Center + hop * 16);
            spin.localRotation = Quaternion.identity;
            spin.localScale = Vector3.one;
            Pose(0.15f + hop * 0.4f, 8, -10 + Mathf.Sin(time * 1.7f) * 5 + hop * 60, 18 - hop * 70, nose, up);
            Board(nose, 0, 0, -up);
        }

        // ---------- chute (FR-009) ----------

        public void Crash(Skater s, float speed)
        {
            crashed = true;
            bodyPos = new Vector2(s.X, s.Y);
            bodyVel = new Vector2(speed * 0.7f, 4.5f);
            bodyRot = 0;
            board.SetParent(transform, true);
            boardPos = new Vector2(s.X, s.Y + 6);
            boardVel = new Vector2(speed + 3.5f, 7.5f);
            boardRot = 0;
            boardSpin = -16;
        }

        public void Fall(Ground g, float dt)
        {
            if (!crashed) return;
            float k = dt * 60;
            bodyVel.y -= 0.5f * k;
            bodyPos += bodyVel * k;
            float gy = g.Height(bodyPos.x);
            if (bodyPos.y <= gy) { bodyPos.y = gy; bodyVel.y = 0; bodyVel.x *= Mathf.Pow(0.86f, k); }
            bodyRot = Mathf.MoveTowards(bodyRot, -95, 7 * k);
            spin.position = W(bodyPos.x, bodyPos.y + 8);
            spin.rotation = Quaternion.Euler(0, 0, bodyRot);
            Pose(0.3f, 20, 150, 120);
            body.localPosition = W(0, -8 - Thigh - Shin);

            boardVel.y -= 0.5f * k;
            boardPos += boardVel * k;
            boardRot += boardSpin * k;
            float by = g.Height(boardPos.x) + 6;
            if (boardPos.y <= by && boardVel.y < 0)
            {
                boardPos.y = by;
                boardVel.y = -boardVel.y * 0.5f;   // la planche rebondit
                boardVel.x *= 0.8f;
                boardSpin *= 0.6f;
                if (boardVel.y < 1) { boardVel.y = 0; boardRot = Mathf.MoveTowards(Mathf.Repeat(boardRot, 360), 180, 30); boardSpin = 0; }
            }
            board.position = W(boardPos.x, boardPos.y);
            board.rotation = Quaternion.Euler(0, 0, boardRot);
            deck.localRotation = Quaternion.identity;
            deck.localScale = Vector3.one;
            gripFace.gameObject.SetActive(false);
            bottomFace.gameObject.SetActive(false);
        }

        public Vector2 BoardPosition => boardPos;

        void Reset()
        {
            crashed = false;
            board.SetParent(spin, false);
            board.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
        }

        public void Show(bool on) => gameObject.SetActive(on);
    }
}
