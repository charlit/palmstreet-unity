using System.Collections.Generic;
using PalmStreet.Core;
using UnityEngine;
using static PalmStreet.Game.Paint;

namespace PalmStreet.Game
{
    /// <summary>
    /// Décor (FR-015, FR-024) : ciel en dégradé, soleil et son halo, puis des plans en parallaxe de plus en plus
    /// proches (montagnes, océan et surfeurs ou collines, silhouettes du décor, premier plan) et la route.
    /// Chaque décor a ses plans ; on passe de l'un à l'autre en fondu.
    /// </summary>
    public sealed partial class Scenery : MonoBehaviour
    {
        public const float Horizon = 130, Tile = 640;
        static readonly float[] Parallax = { 0.06f, 0.2f, 0.36f, 0.62f };

        sealed class Layer { public Transform A, B; public Fader Fade; public float Factor; }

        readonly List<Layer>[] layers = System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Select(ThemeLook.All, _ => new List<Layer>()));
        readonly float[] weight = new float[ThemeLook.All.Length];
        readonly Paint p = new Paint();
        Transform cameraFixed;
        Mesh sky, sun, shine, ground, rain, fog;

        /// <summary>Centre et rayon du soleil, en coordonnées monde (pour le raccourci du menu).</summary>
        public Vector3 SunWorld { get; private set; }
        public float SunWorldRadius { get; private set; }

        /// <summary>À l'accueil le soleil monte à gauche du titre, où l'on peut le toucher (FR-020).</summary>
        public bool MenuSun, HideSun;   // HideSun : carte et profil, où il passait coupé sous l'en-tête
        public static readonly Vector2 MenuSunPos = new Vector2(38, 505);   // x : distance au bord gauche

        public void Init()
        {
            cameraFixed = new GameObject("Ciel").transform;
            cameraFixed.SetParent(transform, false);
            sky = Renderer("Dégradé du ciel", cameraFixed, -100).GetComponent<MeshFilter>().mesh = new Mesh();
            sun = Renderer("Soleil", cameraFixed, -98).GetComponent<MeshFilter>().mesh = new Mesh();
            shine = Renderer("Reflet du soleil", cameraFixed, -92).GetComponent<MeshFilter>().mesh = new Mesh();
            ground = Renderer("Route", transform, 0).GetComponent<MeshFilter>().mesh = new Mesh();
            fog = Renderer("Brume", cameraFixed, -84).GetComponent<MeshFilter>().mesh = new Mesh();     // devant le décor, derrière la route
            rain = Renderer("Pluie", cameraFixed, 450).GetComponent<MeshFilter>().mesh = new Mesh();    // devant tout le monde, sous l'interface

            var rng = new System.Random(20261030);
            for (int t = 0; t < ThemeLook.All.Length; t++)
            {
                var look = ThemeLook.All[t];
                AddLayer(t, 0, -96, Far(look.Decor, look, rng));
                AddLayer(t, 1, -94, Back(look.Decor, look, rng));
                AddLayer(t, 2, -90, Mid(look.Decor, look, rng));
                AddLayer(t, 3, -86, Near(look.Decor, look, rng));
                if (look.Decor == Decor.Btz) { Scoreboard(layers[t][2].A); Scoreboard(layers[t][2].B); }
            }
        }

        /// <summary>
        /// Tableau lumineux du stade Aguiléra (spec 004) : « BIARRITZ vs BAYONNE », jamais de sigle de club (principe V).
        /// Texte en Rubik, enfant de la tuile : il défile avec elle et s'éteint avec son fondu.
        /// </summary>
        static void Scoreboard(Transform tile)
        {
            var font = Resources.Load<Font>("Fonts/Rubik-Bold");
            if (font == null) return;
            var go = new GameObject("Tableau du stade");
            go.transform.SetParent(tile, false);
            go.transform.localPosition = W(StadiumBoard.x, StadiumBoard.y);
            var tm = go.AddComponent<TextMesh>();
            tm.font = font;
            tm.richText = true;
            tm.text = "<color=#FF4A5A>BIARRITZ</color> <color=#F7F4EC>vs</color> <color=#8FD3FF>BAYONNE</color>";
            tm.anchor = TextAnchor.MiddleCenter;
            tm.fontSize = 64;
            tm.characterSize = 0.0145f;
            var mr = go.GetComponent<MeshRenderer>();
            mr.sharedMaterial = font.material;
            mr.sortingOrder = -89;   // juste devant le plan du stade
        }

        static readonly Vector2 StadiumBoard = new Vector2(452, 112);

        void AddLayer(int theme, int depth, int order, Paint content)
        {
            var mesh = content.Build();
            var a = Renderer($"{ThemeLook.All[theme].Name} plan {depth + 1}", transform, order);
            var b = Renderer($"{ThemeLook.All[theme].Name} plan {depth + 1} (suite)", transform, order);
            a.GetComponent<MeshFilter>().sharedMesh = mesh;
            b.GetComponent<MeshFilter>().sharedMesh = mesh;
            var fader = new Fader(mesh, a.gameObject, b.gameObject);
            fader.SetAlpha(theme == 0 ? 1 : 0);
            layers[theme].Add(new Layer { A = a.transform, B = b.transform, Fade = fader, Factor = Parallax[depth] });
        }

        /// <summary>Décor actif tout de suite (début de partie) ou en fondu.</summary>
        public void Draw(Theme theme, Ground g, Camera cam, float time, bool instant)
        {
            float k = instant ? 1 : 1 - Mathf.Exp(-Time.unscaledDeltaTime * 2.2f);
            for (int t = 0; t < ThemeLook.All.Length; t++)
            {
                weight[t] = Mathf.Lerp(weight[t], t == (int)theme ? 1 : 0, k);
                if (weight[t] < 0.002f) weight[t] = 0;
                if (weight[t] > 0.998f) weight[t] = 1;
            }

            float cx = cam.transform.position.x * Ppu, half = cam.orthographicSize * cam.aspect * Ppu;
            float top = (cam.transform.position.y + cam.orthographicSize) * Ppu, bottom = (cam.transform.position.y - cam.orthographicSize) * Ppu;
            cameraFixed.position = new Vector3(cx / Ppu, 0, 0);

            // ciel et soleil (couleurs mélangées entre décors)
            Color C(System.Func<ThemeLook, Color> f) { var col = Color.clear; for (int t = 0; t < ThemeLook.All.Length; t++) col += f(ThemeLook.All[t]) * weight[t]; return col; }
            float sea = 0;
            for (int t = 0; t < ThemeLook.All.Length; t++) if (ThemeLook.All[t].HasSea) sea += weight[t];
            p.Clear();
            p.Gradient(-half - 2, 330, half * 2 + 4, Mathf.Max(top - 330, 1) + 2, C(l => l.SkyMid), C(l => l.SkyTop));
            p.Gradient(-half - 2, Horizon - 10, half * 2 + 4, 340 - Horizon, C(l => l.SkyLow), C(l => l.SkyMid));
            p.Rect(-half - 2, bottom - 2, half * 2 + 4, Horizon - bottom, C(l => l.SkyLow));
            p.Build(sky);

            var sp = Vector2.zero; float sr = 0;
            for (int t = 0; t < ThemeLook.All.Length; t++) { sp += ThemeLook.All[t].SunPos * weight[t]; sr += ThemeLook.All[t].SunRadius * weight[t]; }
            sp.x += half - 390;   // le soleil garde sa distance au bord droit de l'écran (x relatif au centre de la caméra)
            if (MenuSun) sp = new Vector2(-half + MenuSunPos.x, MenuSunPos.y);   // à l'accueil, côté gauche, loin du bouton PROFIL
            float pulse = 1 + Mathf.Sin(time * 1.3f) * 0.03f;
            var glow = C(l => l.Glow);
            p.Clear();
            if (!HideSun)
            {
                p.Glow(sp.x, sp.y, sr * 4.2f * pulse, glow * 0.6f, new Color(glow.r, glow.g, glow.b, 0));
                p.Glow(sp.x, sp.y, sr * 1.9f, glow, new Color(glow.r, glow.g, glow.b, 0));
                p.Circle(sp.x, sp.y, sr, C(l => l.Sun), 36);
                p.Glow(sp.x, sp.y, sr * 0.85f, C(l => l.SunCore), C(l => l.Sun), 36);
            }
            p.Build(sun);
            SunWorld = new Vector3((cx + sp.x) / Ppu, sp.y / Ppu, 0);
            SunWorldRadius = sr / Ppu;

            // reflet du soleil sur l'océan : traits qui scintillent
            p.Clear();
            if (sea > 0.01f)
            {
                var lightCol = C(l => l.SeaLight);
                for (int i = 0; i < 9; i++)
                {
                    float y = Horizon - 6 - i * 11, w = (60 - i * 4) * (0.7f + 0.3f * Mathf.Sin(time * 3 + i * 1.7f));
                    lightCol.a = sea * (0.75f - i * 0.06f);
                    p.Capsule(sp.x - w / 2 + Mathf.Sin(time * 2 + i) * 6, y, sp.x + w / 2 + Mathf.Sin(time * 2 + i) * 6, y, 2.5f, lightCol);
                }
            }
            p.Build(shine);

            // météo des villes : pluie à Espelette, brume à Hendaye et Ascain (en fondu avec le décor)
            float wet = weight[(int)Theme.Espelette], mist = weight[(int)Theme.Hendaye] + weight[(int)Theme.Ascain];
            p.Clear();
            if (wet > 0.01f)
            {
                p.Rect(-half - 2, bottom - 2, half * 2 + 4, top - bottom + 4, new Color(0.08f, 0.1f, 0.2f, 0.22f * wet));
                for (int i = 0; i < 90; i++)
                {
                    float x = Mathf.Repeat(i * 73.1f - time * 140, half * 2 + 40) - half - 20;
                    float y = Mathf.Repeat(i * 151.7f - time * 950, top - bottom + 40) + bottom - 20;
                    p.Line(x, y, x - 7, y - 26, 1.6f, new Color(0.85f, 0.9f, 1, 0.35f * wet));
                }
            }
            p.Build(rain);
            p.Clear();
            if (mist > 0.01f)
            {
                var white = new Color(0.95f, 0.93f, 0.97f, 0.5f * mist);
                p.Gradient(-half - 2, Horizon - 40, half * 2 + 4, 260, white, new Color(white.r, white.g, white.b, 0));
                p.Rect(-half - 2, bottom - 2, half * 2 + 4, Horizon - 38 - bottom, white);
            }
            p.Build(fog);

            // plans en parallaxe
            for (int t = 0; t < ThemeLook.All.Length; t++)
                foreach (var l in layers[t])
                {
                    l.Fade.SetAlpha(weight[t]);
                    if (weight[t] <= 0) continue;
                    float u = cx * l.Factor;   // distance parcourue dans ce plan
                    float s0 = Mathf.Floor((u - half) / Tile) * Tile;
                    l.A.position = new Vector3((cx + s0 - u) / Ppu, 0, 0);
                    l.B.position = new Vector3((cx + s0 + Tile - u) / Ppu, 0, 0);
                }

            DrawGround(g, cx - half - 20, cx + half + 20, bottom, C(l => l.Ground), C(l => l.GroundTop), C(l => l.Stripe));
        }

        void DrawGround(Ground g, float x0, float x1, float bottom, Color body, Color edge, Color stripe)
        {
            p.Clear();
            var deep = Color.Lerp(body, Color.black, 0.35f);
            var curb = Color.Lerp(body, edge, 0.18f);
            const float step = 6;
            float start = Mathf.Floor(x0 / step) * step;
            for (float x = start; x < x1; x += step)
            {
                if (g.IsGap(x) || g.IsGap(x + step)) continue;   // FR-030 : le vide, on voit le décor
                float ha = g.Height(x), hb = g.Height(x + step);
                p.Poly(deep, x, bottom - 10, x + step, bottom - 10, x + step, hb - 46, x, ha - 46);
                Grad(x, step, ha, hb, -46, -8, body, curb);
                Band(x, step, ha, hb, -50, -46, Color.Lerp(edge, Color.black, 0.45f));                    // bordure
                Band(x, step, ha, hb, -78, -50, Color.Lerp(body, edge, 0.1f));                              // trottoir
                Band(x, step, ha, hb, -80, -78, Color.Lerp(body, Color.black, 0.2f));
                Band(x, step, ha, hb, -8, -4, Color.Lerp(edge, Color.black, 0.25f));
                Band(x, step, ha, hb, -4, 0, edge);
                Band(x, step, ha, hb, 0, 1.5f, Color.Lerp(edge, Color.white, 0.5f));
            }
            // bords des trous : tranche de la route soulignée de néon
            foreach (var s in g.Segments)
            {
                if (s.Kind != SlopeKind.Gap || s.X1 < x0 || s.X0 > x1) continue;
                p.Gradient(s.X0, bottom - 10, s.Length, 10 - bottom, Color.black, new Color(0, 0, 0, 0.35f));   // le vide s'assombrit vers le bas
                foreach (float ex in new[] { s.X0, s.X1 })
                {
                    p.Rect(ex - 4, bottom - 10, 8, 10 - bottom, Color.Lerp(body, Color.black, 0.5f));
                    p.Line(ex, bottom - 10, ex, 0, 3, edge);
                    p.Line(ex, bottom - 10, ex, 0, 9, new Color(edge.r, edge.g, edge.b, 0.25f));
                }
            }
            // joints du trottoir et tirets de la route, accrochés au sol
            for (float x = Mathf.Floor(x0 / 44) * 44; x < x1; x += 44)
                if (!g.IsGap(x)) p.Line(x, g.Height(x) - 78, x, g.Height(x) - 50, 1.5f, Color.Lerp(body, Color.black, 0.25f));
            for (int row = 0; -110 - row * 34 > bottom - 120; row++)
                for (float x = Mathf.Floor(x0 / 70) * 70 + row % 2 * 35, y = -110 - row * 34; x < x1; x += 70)
                    if (!g.IsGap(x)) p.Circle(x, g.Height(x) + y, 2.2f, Color.Lerp(deep, edge, 0.12f), 6);
            for (float x = Mathf.Floor(x0 / 60) * 60; x < x1; x += 60)
                if (!g.IsGap(x) && !g.IsGap(x + 22)) p.Line(x, g.Height(x) - 26, x + 22, g.Height(x + 22) - 26, 3, new Color(stripe.r, stripe.g, stripe.b, 0.8f));
            p.Build(ground);
        }

        void Band(float x, float w, float ha, float hb, float d0, float d1, Color col) =>
            p.Poly(col, x, ha + d0, x + w, hb + d0, x + w, hb + d1, x, ha + d1);

        void Grad(float x, float w, float ha, float hb, float d0, float d1, Color low, Color high)
        {
            p.Poly(low, x, ha + d0, x + w, hb + d0, x + w, hb + d1, x, ha + d1);
            p.Poly(high, x, ha + d1 - 4, x + w, hb + d1 - 4, x + w, hb + d1, x, ha + d1);
        }

        // ---------- contenu des plans (une tuile de 640 qui se répète) ----------

        static float R(System.Random r, float a, float b) => a + (float)r.NextDouble() * (b - a);

        /// <summary>Ligne de crête périodique sur la tuile (somme de sinus), remplie jusqu'en bas.</summary>
        static void Ridge(Paint p, Color col, float baseY, float amp, int seed, float sharp = 0)
        {
            var r = new System.Random(seed);
            float a1 = R(r, 0.4f, 1), a2 = R(r, 0.2f, 0.6f), a3 = R(r, 0.1f, 0.3f), f1 = R(r, 0, 6), f2 = R(r, 0, 6), f3 = R(r, 0, 6);
            float H(float x)
            {
                float k = x / Tile * Mathf.PI * 2;
                float h = a1 * Mathf.Sin(k + f1) + a2 * Mathf.Sin(2 * k + f2) + a3 * Mathf.Sin(5 * k + f3);
                if (sharp > 0) h += sharp * Mathf.Abs(Mathf.Sin(7 * k + f2));
                return baseY + amp * h;
            }
            for (float x = 0; x < Tile; x += 8) p.Poly(col, x, -60, x + 8, -60, x + 8, H(x + 8), x, H(x));
        }

        static Paint Far(Decor t, ThemeLook l, System.Random r)
        {
            var city = FarCity(t, l, r);
            if (city != null) return city;
            var p = new Paint();
            switch (t)
            {
                case Decor.Largente:
                case Decor.Park:
                    Ridge(p, Color.Lerp(l.Far, l.SkyLow, 0.35f), 158, 16, 11);   // caps au loin, sous le soleil
                    Ridge(p, l.Far, 142, 12, 12);
                    break;
                case Decor.Btz:
                    Ridge(p, Color.Lerp(l.Far, l.SkyLow, 0.4f), 150, 14, 21);
                    // phare sur son rocher
                    p.Dome(470, 140, 60, 40, l.Far);
                    p.Poly(Color.Lerp(l.Far, Color.white, 0.75f), 462, 170, 478, 170, 475, 250, 465, 250);
                    for (int i = 0; i < 3; i++) p.Rect(463 + i * 0.6f, 182 + i * 22, 14 - i * 1.2f, 8, l.Mid);
                    p.Rect(462, 250, 16, 10, l.Mid);
                    p.Glow(470, 255, 26, new Color(1, 0.95f, 0.7f, 0.9f), new Color(1, 0.95f, 0.7f, 0));
                    p.Dome(470, 260, 9, 8, l.Mid);
                    RocherDeLaVierge(p, 150, l);
                    break;
                case Decor.Neon:
                    // nuit étoilée, grande roue et ville aux toits de néon
                    for (int i = 0; i < 70; i++) p.Circle(R(r, 0, Tile), R(r, 230, 640), R(r, 0.6f, 1.8f), new Color(1, 1, 1, R(r, 0.3f, 0.9f)), 6);
                    NeonWheel(p, 470, 190, 78, Hex("#35F2FF"), Hex("#FF4FD8"));
                    for (float x = 0; x < Tile - 20;)
                    {
                        float w = R(r, 34, 70), h = R(r, 70, 175);
                        var neon = r.NextDouble() < 0.5 ? Hex("#FF4FD8") : Hex("#35F2FF");
                        p.Rect(x, -60, w, h + 60, l.Far);
                        p.Rect(x - 1, h - 1, w + 2, 3, neon);
                        p.Rect(x - 4, h - 6, w + 8, 12, new Color(neon.r, neon.g, neon.b, 0.18f));
                        for (float wy = 12; wy < h - 12; wy += 16)
                            for (float wx = x + 6; wx < x + w - 8; wx += 11)
                                if (r.NextDouble() < 0.35) p.Rect(wx, wy, 5, 7, new Color(neon.r, neon.g, neon.b, 0.55f));
                        x += w + R(r, 4, 26);
                    }
                    break;
                default:
                    Ridge(p, Color.Lerp(l.Far, l.SkyLow, 0.45f), 230, 40, 31, 0.7f);
                    Ridge(p, Color.Lerp(l.Far, l.SkyMid, 0.2f), 190, 34, 32, 0.5f);
                    break;
            }
            return p;
        }

        static Paint Back(Decor t, ThemeLook l, System.Random r)
        {
            var city = BackCity(t, l, r);
            if (city != null) return city;
            var p = new Paint();
            if (!l.HasSea)
            {
                // collines du Pays basque, haies d'arbres ronds
                Ridge(p, Color.Lerp(l.Sea, l.SkyLow, 0.25f), 110, 22, 41);
                Ridge(p, l.Sea, 70, 20, 42);
                for (int i = 0; i < 14; i++) { float x = R(r, 10, Tile - 10); p.Circle(x, R(r, 60, 85), R(r, 6, 11), Color.Lerp(l.Sea, l.Mid, 0.5f), 12); }
                return p;
            }
            p.Gradient(0, -60, Tile, Horizon + 60, l.Sea, Color.Lerp(l.Sea, l.SkyLow, 0.35f));
            var crest = Color.Lerp(l.SeaLight, Color.white, 0.4f);
            for (int i = 0; i < 26; i++)
            {
                float y = R(r, 10, Horizon - 8), w = R(r, 14, 40) * (0.5f + y / Horizon), x = R(r, 0, Tile - w);
                p.Capsule(x, y, x + w, y, 1.6f, new Color(l.SeaLight.r, l.SeaLight.g, l.SeaLight.b, 0.35f));
            }
            if (t == Decor.Neon)
            {
                // reflets de néon qui ondulent sur l'eau noire
                for (int i = 0; i < 30; i++)
                {
                    var c = i % 2 == 0 ? Hex("#FF4FD8", 0.5f) : Hex("#35F2FF", 0.5f);
                    float y = R(r, 8, Horizon - 10), w = R(r, 10, 34), x = R(r, 0, Tile - w);
                    p.Capsule(x, y, x + w, y, 2, c);
                }
                return p;
            }
            // surfeurs sur leur vague
            int surfers = t == Decor.Btz ? 4 : 3;
            for (int i = 0; i < surfers; i++)
            {
                float x = 60 + i * (Tile - 120) / surfers + R(r, -20, 20), y = R(r, 40, 95), s = 0.6f + (Horizon - y) / Horizon * 0.6f;
                p.Curve(new Vector2(x - 40 * s, y - 2), new Vector2(x - 6 * s, y + 4 * s), new Vector2(x + 18 * s, y + 16 * s), 4 * s, 1.5f * s, new Color(crest.r, crest.g, crest.b, 0.85f));
                p.Ellipse(x, y + 2 * s, 11 * s, 2 * s, l.SeaLight, 12, 0.1f);
                p.Capsule(x - 2 * s, y + 4 * s, x, y + 14 * s, 3.2f * s, l.Mid);
                p.Capsule(x - 2 * s, y + 4 * s, x - 6 * s, y + 4 * s, 2.4f * s, l.Mid);
                p.Capsule(x, y + 12 * s, x + 6 * s, y + 15 * s, 2 * s, l.Mid);
                p.Capsule(x, y + 12 * s, x - 6 * s, y + 10 * s, 2 * s, l.Mid);
                p.Circle(x + 1 * s, y + 17 * s, 2.6f * s, l.Mid, 10);
            }
            return p;
        }

        static void Palm(Paint p, float x, float h, float lean, Color col, float scale = 1)
        {
            var top = new Vector2(x + lean, h);
            p.Curve(new Vector2(x, -10), new Vector2(x + lean * 0.2f, h * 0.55f), top, 9 * scale, 5 * scale, col, 10);
            for (int i = 0; i < 7; i++)
            {
                float a = Mathf.PI * (0.05f + i / 6f * 0.9f), len = (46 + (i % 2) * 10) * scale;
                var end = top + new Vector2(Mathf.Cos(a) * len, Mathf.Sin(a) * len * 0.45f - 18 * scale);
                var ctrl = top + new Vector2(Mathf.Cos(a) * len * 0.55f, Mathf.Sin(a) * len * 0.55f + 10 * scale);
                p.Curve(top, ctrl, end, 7 * scale, 1.5f * scale, col, 7);
            }
            p.Circle(top.x - 3 * scale, top.y - 5 * scale, 4 * scale, col, 10);
            p.Circle(top.x + 4 * scale, top.y - 6 * scale, 4 * scale, col, 10);
        }

        static void NeonPalm(Paint p, float x, float h, float lean, Color neon, Color dark, float scale)
        {
            Palm(p, x, h, lean, new Color(neon.r, neon.g, neon.b, 0.35f), scale * 1.15f);   // halo
            Palm(p, x, h, lean, neon, scale * 1.04f);
            Palm(p, x, h, lean, dark, scale);
        }

        /// <summary>Grande roue de néon : cercle, rayons et nacelles.</summary>
        static void NeonWheel(Paint p, float cx, float cy, float rad, Color ring, Color cabins)
        {
            p.Line(cx - 30, -60, cx, cy, 4, ring * 0.6f + new Color(0, 0, 0, 0.4f));
            p.Line(cx + 30, -60, cx, cy, 4, ring * 0.6f + new Color(0, 0, 0, 0.4f));
            for (int i = 0; i < 32; i++)
            {
                float a0 = i * Mathf.PI * 2 / 32, a1 = (i + 1) * Mathf.PI * 2 / 32;
                p.Line(cx + Mathf.Cos(a0) * rad, cy + Mathf.Sin(a0) * rad, cx + Mathf.Cos(a1) * rad, cy + Mathf.Sin(a1) * rad, 2.5f, ring);
                if (i % 4 == 0)
                {
                    p.Line(cx, cy, cx + Mathf.Cos(a0) * rad, cy + Mathf.Sin(a0) * rad, 1.2f, new Color(ring.r, ring.g, ring.b, 0.5f));
                    p.Circle(cx + Mathf.Cos(a0) * rad, cy + Mathf.Sin(a0) * rad - 6, 5, cabins, 10);
                }
            }
            p.Glow(cx, cy, rad + 20, new Color(ring.r, ring.g, ring.b, 0.12f), new Color(ring.r, ring.g, ring.b, 0));
        }

        /// <summary>
        /// Skatepark de la Barre (spec 006) : mur en béton couvert de graffitis colorés (formes, sans marque ni mot),
        /// quarters et bowl en béton à coping métallique, lampadaires.
        /// </summary>
        static void SkateparkBack(Paint p, ThemeLook l, System.Random r)
        {
            Color concrete = Hex("#AEB6BE"), shade = Hex("#8C959E"), coping = Hex("#E8EDF2"), dark = Hex("#2A3540");
            Color[] spray = { Hex("#FF4D8D"), Hex("#35F2FF"), Hex("#FFE14D"), Hex("#7CE38B"), Hex("#FF7A1A"), Hex("#B86BFF") };
            // mur à graffitis
            p.Rect(20, -10, 300, 90, concrete).Rect(20, 76, 300, 4, coping);
            for (int i = 0; i < 9; i++)
            {
                var c = spray[i % spray.Length]; float x = 34 + i * 32 + R(r, -6, 6), y = R(r, 14, 52);
                p.Ellipse(x, y, R(r, 14, 22), R(r, 9, 16), c, 16, R(r, -0.5f, 0.5f));
                p.Ellipse(x + R(r, -6, 6), y + R(r, -4, 4), R(r, 5, 9), R(r, 4, 7), spray[(i + 3) % spray.Length], 12);
                p.Line(x - 12, y - 12, x + 14, y + R(r, -6, 10), 2.2f, dark);
            }
            // quarter pipe et bowl
            p.Poly(concrete, 350, -10, 470, -10, 470, 70, 440, 64, 410, 46, 380, 24, 360, 8);
            p.Poly(shade, 350, -10, 380, -10, 380, 24, 360, 8);
            p.Rect(466, -10, 8, 80, shade).Line(440, 66, 476, 72, 3, coping);
            p.Poly(concrete, 500, -10, 630, -10, 630, 30, 600, 18, 565, 10, 530, 18, 500, 30);
            p.Line(500, 30, 528, 19, 3, coping).Line(602, 19, 630, 30, 3, coping);
            // lampadaires
            foreach (float lx in new[] { 335f, 485f })
            {
                p.Rect(lx, -10, 4, 150, dark).Capsule(lx + 2, 138, lx + 18, 142, 4, dark);
                p.Glow(lx + 20, 138, 18, new Color(1, 0.95f, 0.7f, 0.7f), new Color(1, 0.95f, 0.7f, 0));
            }
        }

        /// <summary>Rocher de la Vierge (spec 004) : rocher dans l'océan, statue blanche, passerelle en treillis reliée à la falaise.</summary>
        static void RocherDeLaVierge(Paint p, float x, ThemeLook l)
        {
            var rock = Color.Lerp(l.Far, Color.black, 0.15f); var light = Color.Lerp(l.Far, Color.white, 0.15f); var iron = Color.Lerp(l.Far, Color.black, 0.45f);
            p.Poly(rock, x - 100, 120, x - 66, 120, x - 64, 196, x - 84, 204, x - 100, 200);                 // la falaise
            p.Poly(rock, x - 20, 118, x + 34, 118, x + 26, 178, x + 8, 196, x - 12, 186, x - 22, 160);       // le rocher
            p.Poly(light, x - 12, 186, x + 8, 196, x + 4, 170, x - 10, 168);
            // passerelle en treillis, en arc, de la falaise au rocher
            float y = 188;
            p.Rect(x - 66, y, 60, 2.2f, iron);
            for (int i = 0; i <= 10; i++)
            {
                float t = i / 10f, bx = x - 66 + t * 60, dip = Mathf.Sin(t * Mathf.PI) * 9;
                p.Line(bx, y, bx, y - 4 - dip, 1, iron);
                if (i < 10) p.Line(bx, y - 4 - dip, bx + 6, y, 0.8f, iron);
            }
            // la statue blanche sur son socle
            var white = Hex("#F7F4EC");
            p.Rect(x + 4, 196, 8, 6, light);
            p.Poly(white, x + 5, 202, x + 11, 202, x + 10, 218, x + 6, 218).Circle(x + 8, 220, 2.2f, white, 10);
            p.Glow(x + 8, 214, 14, new Color(1, 1, 0.9f, 0.35f), new Color(1, 1, 0.9f, 0));
            p.Ellipse(x + 6, 120, 34, 3, new Color(1, 1, 1, 0.35f), 16);                                     // écume au pied
        }

        /// <summary>Fronton d'Arcangues (spec 005) : mur ocre au sommet en arc, liseré rouge, lignes rouges, bande de métal.</summary>
        static void Fronton(Paint p, float x0)
        {
            Color ocre = Hex("#C9A46A"), shade = Hex("#B08B54"), red = Hex("#B8202E"), metal = Hex("#8A949E");
            const float w = 220, h = 100, arch = 42;
            float cx = x0 + w / 2;
            p.Rect(x0, -10, w, h + 10, ocre);
            p.Dome(cx, h, w / 2, arch, ocre, 24);
            p.Rect(x0, -10, 14, h + 10, shade);                                                        // ombre du pilier
            for (int i = 0; i < 24; i++)                                                                // liseré rouge qui suit l'arc
            {
                float a0 = Mathf.PI * i / 24, a1 = Mathf.PI * (i + 1) / 24;
                p.Line(cx + Mathf.Cos(a0) * (w / 2 - 6), h + Mathf.Sin(a0) * (arch - 6), cx + Mathf.Cos(a1) * (w / 2 - 6), h + Mathf.Sin(a1) * (arch - 6), 2.4f, red);
            }
            p.Rect(x0 + 6, h - 2, 2.4f, 4, red).Rect(x0 + w - 8.4f, h - 2, 2.4f, 4, red);
            p.Rect(x0, 62, w, 2.6f, red).Rect(x0, 16, w, 2.6f, red);                                    // lignes rouges du jeu
            p.Rect(x0, -10, w, 14, metal).Rect(x0, 2, w, 1.6f, Hex("#C9CED6"));                        // bande de métal
        }

        /// <summary>Hôtel du Palais : palace brique et pierre, trois pavillons aux toits d'ardoise très pentus, terrasse.</summary>
        static void Palais(Paint p, float x0)
        {
            Color brick = Hex("#A0583F"), stone = Hex("#EFE3CC"), slate = Hex("#3C4A5E"), slateLight = Hex("#56667D"), win = Hex("#FFE7A8"), dark = Hex("#2A2F3A");
            void Wing(float x, float w, float h, float roof)
            {
                p.Rect(x, -10, w, h + 10, brick);
                for (float y = 14; y < h; y += 24) p.Rect(x, y, w, 3, stone);                       // bandeaux de pierre
                p.Rect(x, -10, 4, h + 10, stone).Rect(x + w - 4, -10, 4, h + 10, stone);            // chaînes d'angle
                p.Poly(slate, x - 3, h, x + w + 3, h, x + w - 6, h + roof, x + 6, h + roof);         // toit d'ardoise très pentu
                p.Line(x + 8, h + roof * 0.5f, x + w - 8, h + roof * 0.5f, 1.2f, slateLight);
                for (float wx = x + 9; wx < x + w - 12; wx += 15)
                {
                    for (float y = 20; y < h - 6; y += 24) p.Rect(wx, y, 7, 11, win).Dome(wx + 3.5f, y + 11, 3.5f, 3, win, 6);
                    p.Rect(wx + 1, h + 6, 5, 8, stone);                                              // lucarnes
                }
            }
            Wing(x0, 72, 88, 30);
            Wing(x0 + 168, 72, 88, 30);
            Wing(x0 + 64, 112, 74, 24);                                                                  // corps central, en retrait
            Wing(x0 + 92, 56, 110, 44);                                                                  // pavillon central, plus haut
            p.Line(x0 + 120, 154, x0 + 120, 172, 1.4f, dark).Poly(Hex("#F7F4EC"), x0 + 120, 172, x0 + 132, 168, x0 + 120, 164);   // drapeau blanc
            // terrasse sur la plage : balustrade
            p.Rect(x0 - 6, -10, 252, 14, stone);
            for (float bx = x0 - 2; bx < x0 + 244; bx += 7) p.Rect(bx, 4, 3, 8, stone);
            p.Rect(x0 - 6, 12, 252, 3, stone);
        }

        /// <summary>Stade Aguiléra : tribune couverte aux sièges rouges et blancs, toit en porte-à-faux, pylônes allumés, poteaux.</summary>
        static void Stadium(Paint p, float x0)
        {
            Color red = Hex("#D7263D"), white = Hex("#F2EEE6"), concrete = Hex("#9AA3B0"), dark = Hex("#2A2F3A"), glow = new Color(1, 0.97f, 0.8f, 0.85f);
            p.Rect(x0 + 6, -10, 250, 66, concrete);
            for (int i = 0; i < 8; i++) p.Rect(x0 + 10, -2 + i * 7, 242, 4.5f, i % 2 == 0 ? red : white);   // rangées de sièges
            for (float px = x0 + 10; px < x0 + 256; px += 40) p.Rect(px, -10, 3, 84, dark);                 // poteaux de la tribune
            p.Poly(dark, x0, 76, x0 + 266, 76, x0 + 272, 86, x0 - 6, 82);                                    // toit en porte-à-faux
            p.Rect(x0, 74, 266, 3, red);
            foreach (float lx in new[] { x0 - 14f, x0 + 300f })                                             // pylônes d'éclairage
            {
                p.Rect(lx, -10, 4, 196, dark);
                p.Glow(lx + 2, 192, 34, glow, new Color(glow.r, glow.g, glow.b, 0));
                p.Rect(lx - 11, 184, 26, 16, dark);
                for (int i = 0; i < 3; i++) for (int j = 0; j < 2; j++) p.Rect(lx - 9 + i * 8, 186 + j * 7, 6, 5, Hex("#FFF7D6"));
            }
            // poteaux de rugby en H, sur la pelouse à droite
            float gx = x0 + 276;
            p.Rect(gx - 6, -10, 40, 14, Hex("#5FAE58"));
            p.Rect(gx, -4, 2.5f, 74, white).Rect(gx + 26, -4, 2.5f, 74, white).Rect(gx, 22, 28.5f, 2.5f, white);
            // tableau d'affichage (le texte est un TextMesh : Scoreboard)
            float bx = StadiumBoard.x, by = StadiumBoard.y;
            p.Rect(bx - 3, 76, 3, by - 76, dark).Rect(bx, 76, 3, by - 76, dark);
            p.Rect(bx - 72, by - 14, 144, 28, Hex("#3A4252")).Rect(bx - 70, by - 12, 140, 24, Hex("#141826"));
        }

        static void House(Paint p, float x, float w, float h, float roof, Color wall, Color window, bool basque = false)
        {
            p.Rect(x, -10, w, h + 10, wall);
            if (basque) p.Poly(wall, x - 6, h, x + w * 0.62f, h + roof, x + w + 6, h);   // toit dissymétrique
            else p.Poly(wall, x - 4, h, x + w / 2, h + roof, x + w + 4, h);
            int cols = Mathf.Max(1, (int)(w / 18));
            for (int i = 0; i < cols; i++)
                for (int j = 0; j < Mathf.Max(1, (int)(h / 26)); j++)
                {
                    float wx = x + (w - cols * 18) / 2 + i * 18 + 5, wy = 10 + j * 26;
                    if ((i * 7 + j * 3 + (int)x) % 3 != 0) p.Rect(wx, wy, 8, 10, window);
                }
        }

        static Paint Mid(Decor t, ThemeLook l, System.Random r)
        {
            var city = MidCity(t, l, r);
            if (city != null) return city;
            var p = new Paint();
            var lit = new Color(l.SunCore.r, l.SunCore.g, l.SunCore.b, 0.75f);
            switch (t)
            {
                case Decor.Largente:
                    for (int i = 0; i < 3; i++) House(p, 40 + i * 210 + R(r, -10, 10), R(r, 44, 60), R(r, 26, 40), 18, l.Mid, lit);
                    for (int i = 0; i < 5; i++) Palm(p, 20 + i * 128 + R(r, -12, 12), R(r, 150, 215), R(r, -25, 30), l.Mid, R(r, 0.8f, 1));
                    break;
                case Decor.Park:   // spec 006 : le skatepark de la Barre
                    SkateparkBack(p, l, r);
                    break;
                case Decor.Btz:   // spec 004 : l'Hôtel du Palais et le stade Aguiléra
                    Palais(p, 12);
                    Palm(p, 268, 190, 14, l.Mid, 0.9f);
                    Stadium(p, 296);
                    break;
                case Decor.Neon:
                    // palmiers et enseignes de néon
                    for (int i = 0; i < 4; i++)
                    {
                        float x = 60 + i * 160 + R(r, -15, 15);
                        var c = i % 2 == 0 ? Hex("#FF4FD8") : Hex("#35F2FF");
                        NeonPalm(p, x, R(r, 160, 220), R(r, -25, 25), c, l.Mid, R(r, 0.85f, 1));
                    }
                    for (int i = 0; i < 2; i++)
                    {
                        float x = 130 + i * 320, y = R(r, 70, 110);
                        var c = i == 0 ? Hex("#FFE14D") : Hex("#FF4FD8");
                        p.Rect(x + 18, -10, 4, y + 10, l.Mid);
                        p.Rect(x - 6, y - 6, 52, 34, new Color(c.r, c.g, c.b, 0.2f));
                        p.Rect(x, y, 40, 22, l.Mid);
                        p.Rect(x + 3, y + 3, 34, 3, c); p.Rect(x + 3, y + 16, 34, 3, c);
                        p.Rect(x + 3, y + 3, 3, 16, c); p.Rect(x + 34, y + 3, 3, 16, c);
                        p.Line(x + 10, y + 8, x + 28, y + 14, 2.5f, c);
                    }
                    break;
                default:
                    // village basque : maisons, église et fronton
                    House(p, 40, 60, 46, 26, l.Mid, lit, true);
                    House(p, 112, 48, 38, 22, l.Mid, lit, true);
                    p.Rect(250, -10, 70, 80, l.Mid);                                   // nef
                    p.Poly(l.Mid, 246, 70, 285, 98, 324, 70);
                    p.Rect(318, -10, 26, 180, l.Mid);                                  // clocher (spec 005 : plus haut)
                    p.Poly(l.Mid, 314, 170, 331, 214, 348, 170);
                    p.Rect(322, 142, 18, 14, lit).Rect(330, 142, 2, 14, l.Mid);        // abat-son éclairé
                    Fronton(p, 380);
                    break;
            }
            return p;
        }

        static Paint Near(Decor t, ThemeLook l, System.Random r)
        {
            var city = NearCity(t, l, r);
            if (city != null) return city;
            var p = new Paint();
            switch (t)
            {
                case Decor.Largente:
                    Palm(p, 140, 290, 40, l.Near, 1.35f);
                    for (float x = 0; x < Tile; x += 32) p.Rect(x, -10, 4, 34, l.Near);           // garde-corps de la promenade
                    p.Rect(0, 22, Tile, 4, l.Near);
                    p.Rect(0, 8, Tile, 3, l.Near);
                    break;
                case Decor.Park:   // spec 006 : grillage du skatepark et un palmier de la plage
                    Palm(p, 560, 270, -30, l.Near, 1.2f);
                    for (float x = 0; x < Tile; x += 64) p.Rect(x, -10, 4, 46, l.Near);
                    for (float x = 0; x < Tile; x += 12) { p.Line(x, 0, x + 12, 34, 0.8f, l.Near); p.Line(x + 12, 0, x, 34, 0.8f, l.Near); }
                    p.Rect(0, 34, Tile, 3, l.Near);
                    break;
                case Decor.Btz:
                    for (int i = 0; i < 2; i++)
                    {
                        float x = 120 + i * 320;
                        p.Rect(x, -10, 5, 120, l.Near);                                             // réverbère
                        p.Capsule(x + 2, 108, x + 22, 116, 4, l.Near);
                        p.Glow(x + 24, 110, 22, new Color(1, 0.94f, 0.6f, 0.8f), new Color(1, 0.94f, 0.6f, 0));
                        p.Circle(x + 24, 112, 5, l.SunCore, 12);
                    }
                    Palm(p, 300, 260, -30, l.Near, 1.25f);
                    break;
                case Decor.Neon:
                    // réverbères à boules roses, garde-corps lumineux
                    for (int i = 0; i < 3; i++)
                    {
                        float x = 90 + i * 210;
                        p.Rect(x, -10, 4, 110, l.Near);
                        p.Glow(x + 2, 112, 26, Hex("#FF4FD8", 0.7f), Hex("#FF4FD8", 0));
                        p.Circle(x + 2, 112, 6, Hex("#FFC2F2"), 14);
                    }
                    for (float x = 0; x < Tile; x += 32) p.Rect(x, -10, 3, 30, l.Near);
                    p.Rect(0, 20, Tile, 3, Hex("#35F2FF", 0.85f));
                    p.Rect(0, 17, Tile, 9, Hex("#35F2FF", 0.15f));
                    break;
                default:
                    // platanes taillés (spec 005) : tronc tacheté, moignons de branches, couronne plate et compacte
                    var bark = Color.Lerp(l.Near, Color.white, 0.25f);
                    for (int i = 0; i < 4; i++)
                    {
                        float x = 60 + i * 160 + R(r, -20, 20), h = R(r, 80, 120);
                        p.Rect(x - 4, -10, 8, h * 0.72f, l.Near);
                        for (int k = 0; k < 4; k++) p.Ellipse(x + R(r, -2, 2), R(r, 0, h * 0.6f), 2.2f, 3, bark, 8);
                        p.Line(x, h * 0.62f, x - 16, h * 0.8f, 4, l.Near).Line(x, h * 0.62f, x + 16, h * 0.8f, 4, l.Near).Line(x, h * 0.66f, x, h * 0.86f, 4, l.Near);
                        p.Circle(x - 16, h * 0.82f, 4, l.Near, 10).Circle(x + 16, h * 0.82f, 4, l.Near, 10);   // moignons taillés
                        p.Ellipse(x, h * 0.92f, h * 0.42f, h * 0.13f, l.Near, 18);
                    }
                    for (float x = 0; x < Tile; x += 40) p.Rect(x, -10, 3, 26, l.Near);
                    p.Rect(0, 12, Tile, 3, l.Near);
                    break;
            }
            return p;
        }
    }
}
