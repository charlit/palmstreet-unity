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
    public sealed class Scenery : MonoBehaviour
    {
        public const float Horizon = 130, Tile = 640;
        static readonly float[] Parallax = { 0.06f, 0.2f, 0.36f, 0.62f };

        sealed class Layer { public Transform A, B; public Fader Fade; public float Factor; }

        readonly List<Layer>[] layers = { new List<Layer>(), new List<Layer>(), new List<Layer>() };
        readonly float[] weight = { 1, 0, 0 };
        readonly Paint p = new Paint();
        Transform cameraFixed;
        Mesh sky, sun, shine, ground;

        /// <summary>Centre et rayon du soleil, en coordonnées monde (pour le raccourci du menu).</summary>
        public Vector3 SunWorld { get; private set; }
        public float SunWorldRadius { get; private set; }

        /// <summary>À l'accueil le soleil monte à côté du titre, où l'on peut le toucher (FR-020).</summary>
        public bool MenuSun;
        static readonly Vector2 MenuSunPos = new Vector2(335, 440);

        public void Init()
        {
            cameraFixed = new GameObject("Ciel").transform;
            cameraFixed.SetParent(transform, false);
            sky = Renderer("Dégradé du ciel", cameraFixed, -100).GetComponent<MeshFilter>().mesh = new Mesh();
            sun = Renderer("Soleil", cameraFixed, -98).GetComponent<MeshFilter>().mesh = new Mesh();
            shine = Renderer("Reflet du soleil", cameraFixed, -92).GetComponent<MeshFilter>().mesh = new Mesh();
            ground = Renderer("Route", transform, 0).GetComponent<MeshFilter>().mesh = new Mesh();

            var rng = new System.Random(20261030);
            for (int t = 0; t < 3; t++)
            {
                var look = ThemeLook.All[t];
                AddLayer(t, 0, -96, Far((Theme)t, look, rng));
                AddLayer(t, 1, -94, Back((Theme)t, look, rng));
                AddLayer(t, 2, -90, Mid((Theme)t, look, rng));
                AddLayer(t, 3, -86, Near((Theme)t, look, rng));
            }
        }

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
            for (int t = 0; t < 3; t++)
            {
                weight[t] = Mathf.Lerp(weight[t], t == (int)theme ? 1 : 0, k);
                if (weight[t] < 0.002f) weight[t] = 0;
                if (weight[t] > 0.998f) weight[t] = 1;
            }

            float cx = cam.transform.position.x * Ppu, half = cam.orthographicSize * cam.aspect * Ppu;
            float top = (cam.transform.position.y + cam.orthographicSize) * Ppu, bottom = (cam.transform.position.y - cam.orthographicSize) * Ppu;
            cameraFixed.position = new Vector3(cx / Ppu, 0, 0);

            // ciel et soleil (couleurs mélangées entre décors)
            Color C(System.Func<ThemeLook, Color> f) { var col = Color.clear; for (int t = 0; t < 3; t++) col += f(ThemeLook.All[t]) * weight[t]; return col; }
            float sea = weight[0] * 1 + weight[1] * 1;   // Arcangues n'a pas d'océan
            p.Clear();
            p.Gradient(-half - 2, 330, half * 2 + 4, Mathf.Max(top - 330, 1) + 2, C(l => l.SkyMid), C(l => l.SkyTop));
            p.Gradient(-half - 2, Horizon - 10, half * 2 + 4, 340 - Horizon, C(l => l.SkyLow), C(l => l.SkyMid));
            p.Rect(-half - 2, bottom - 2, half * 2 + 4, Horizon - bottom, C(l => l.SkyLow));
            p.Build(sky);

            var sp = Vector2.zero; float sr = 0;
            for (int t = 0; t < 3; t++) { sp += ThemeLook.All[t].SunPos * weight[t]; sr += ThemeLook.All[t].SunRadius * weight[t]; }
            if (MenuSun) sp = MenuSunPos;
            sp.x += half - 390;   // le soleil garde sa distance au bord droit de l'écran (x relatif au centre de la caméra)
            float pulse = 1 + Mathf.Sin(time * 1.3f) * 0.03f;
            var glow = C(l => l.Glow);
            p.Clear();
            p.Glow(sp.x, sp.y, sr * 4.2f * pulse, glow * 0.6f, new Color(glow.r, glow.g, glow.b, 0));
            p.Glow(sp.x, sp.y, sr * 1.9f, glow, new Color(glow.r, glow.g, glow.b, 0));
            p.Circle(sp.x, sp.y, sr, C(l => l.Sun), 36);
            p.Glow(sp.x, sp.y, sr * 0.85f, C(l => l.SunCore), C(l => l.Sun), 36);
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

            // plans en parallaxe
            for (int t = 0; t < 3; t++)
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
            // joints du trottoir et tirets de la route, accrochés au sol
            for (float x = Mathf.Floor(x0 / 44) * 44; x < x1; x += 44)
                p.Line(x, g.Height(x) - 78, x, g.Height(x) - 50, 1.5f, Color.Lerp(body, Color.black, 0.25f));
            for (int row = 0; -110 - row * 34 > bottom - 120; row++)
                for (float x = Mathf.Floor(x0 / 70) * 70 + row % 2 * 35, y = -110 - row * 34; x < x1; x += 70)
                    p.Circle(x, g.Height(x) + y, 2.2f, Color.Lerp(deep, edge, 0.12f), 6);
            for (float x = Mathf.Floor(x0 / 60) * 60; x < x1; x += 60)
                p.Line(x, g.Height(x) - 26, x + 22, g.Height(x + 22) - 26, 3, new Color(stripe.r, stripe.g, stripe.b, 0.8f));
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

        static Paint Far(Theme t, ThemeLook l, System.Random r)
        {
            var p = new Paint();
            switch (t)
            {
                case Theme.Largente:
                    Ridge(p, Color.Lerp(l.Far, l.SkyLow, 0.35f), 158, 16, 11);   // caps au loin, sous le soleil
                    Ridge(p, l.Far, 142, 12, 12);
                    break;
                case Theme.Btz:
                    Ridge(p, Color.Lerp(l.Far, l.SkyLow, 0.4f), 150, 14, 21);
                    // phare sur son rocher
                    p.Dome(470, 140, 60, 40, l.Far);
                    p.Poly(Color.Lerp(l.Far, Color.white, 0.75f), 462, 170, 478, 170, 475, 250, 465, 250);
                    for (int i = 0; i < 3; i++) p.Rect(463 + i * 0.6f, 182 + i * 22, 14 - i * 1.2f, 8, l.Mid);
                    p.Rect(462, 250, 16, 10, l.Mid);
                    p.Glow(470, 255, 26, new Color(1, 0.95f, 0.7f, 0.9f), new Color(1, 0.95f, 0.7f, 0));
                    p.Dome(470, 260, 9, 8, l.Mid);
                    break;
                default:
                    Ridge(p, Color.Lerp(l.Far, l.SkyLow, 0.45f), 230, 40, 31, 0.7f);
                    Ridge(p, Color.Lerp(l.Far, l.SkyMid, 0.2f), 190, 34, 32, 0.5f);
                    break;
            }
            return p;
        }

        static Paint Back(Theme t, ThemeLook l, System.Random r)
        {
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
            // surfeurs sur leur vague
            int surfers = t == Theme.Btz ? 4 : 3;
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

        static Paint Mid(Theme t, ThemeLook l, System.Random r)
        {
            var p = new Paint();
            var lit = new Color(l.SunCore.r, l.SunCore.g, l.SunCore.b, 0.75f);
            switch (t)
            {
                case Theme.Largente:
                    for (int i = 0; i < 3; i++) House(p, 40 + i * 210 + R(r, -10, 10), R(r, 44, 60), R(r, 26, 40), 18, l.Mid, lit);
                    for (int i = 0; i < 5; i++) Palm(p, 20 + i * 128 + R(r, -12, 12), R(r, 150, 215), R(r, -25, 30), l.Mid, R(r, 0.8f, 1));
                    break;
                case Theme.Btz:
                    for (int x = 10; x < Tile - 70;)
                    {
                        float w = R(r, 46, 80);
                        House(p, x, w, R(r, 50, 110), R(r, 12, 30), l.Mid, lit);
                        x += (int)(w + R(r, 14, 40));
                    }
                    Palm(p, 220, 190, 14, l.Mid, 0.9f);
                    break;
                default:
                    // village basque : maisons, église et fronton
                    House(p, 40, 60, 46, 26, l.Mid, lit, true);
                    House(p, 112, 48, 38, 22, l.Mid, lit, true);
                    p.Rect(250, -10, 70, 80, l.Mid);                                   // nef
                    p.Poly(l.Mid, 246, 70, 285, 98, 324, 70);
                    p.Rect(318, -10, 26, 150, l.Mid);                                  // clocher
                    p.Poly(l.Mid, 316, 140, 331, 172, 346, 140);
                    p.Rect(328, 112, 6, 12, lit);
                    p.Rect(420, -10, 110, 70, l.Mid);                                  // fronton
                    p.Dome(475, 70, 55, 26, l.Mid, 16);
                    House(p, 560, 56, 44, 24, l.Mid, lit, true);
                    break;
            }
            return p;
        }

        static Paint Near(Theme t, ThemeLook l, System.Random r)
        {
            var p = new Paint();
            switch (t)
            {
                case Theme.Largente:
                    Palm(p, 140, 290, 40, l.Near, 1.35f);
                    for (float x = 0; x < Tile; x += 32) p.Rect(x, -10, 4, 34, l.Near);           // garde-corps de la promenade
                    p.Rect(0, 22, Tile, 4, l.Near);
                    p.Rect(0, 8, Tile, 3, l.Near);
                    break;
                case Theme.Btz:
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
                default:
                    for (int i = 0; i < 4; i++)
                    {
                        float x = 60 + i * 160 + R(r, -20, 20), h = R(r, 60, 120);
                        p.Rect(x - 3, -10, 6, h * 0.6f, l.Near);
                        p.Circle(x, h * 0.7f, h * 0.32f, l.Near, 16);
                        p.Circle(x - h * 0.18f, h * 0.55f, h * 0.22f, l.Near, 14);
                        p.Circle(x + h * 0.2f, h * 0.58f, h * 0.24f, l.Near, 14);
                    }
                    for (float x = 0; x < Tile; x += 40) p.Rect(x, -10, 3, 26, l.Near);
                    p.Rect(0, 12, Tile, 3, l.Near);
                    break;
            }
            return p;
        }
    }
}
