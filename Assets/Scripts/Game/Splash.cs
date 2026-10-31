using System;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using static PalmStreet.Game.Paint;

namespace PalmStreet.Game
{
    /// <summary>
    /// Animation de lancement (spec 002, FR-028) : sur un ciel de nuit avec la lune en haut à gauche, la planche
    /// arrive en sautant, grinde sur le mot STREET du titre de l'accueil (étincelles, les lettres s'allument à son
    /// passage), puis s'en échappe en kickflip (« P. ST. » visible) pendant que la nuit s'efface sur l'accueil.
    /// 2,4 s ; un toucher passe. Le ciel est dessiné sous l'interface (ordre 400), la planche au-dessus (900 et plus).
    /// </summary>
    public sealed class Splash : MonoBehaviour
    {
        public const float Duration = 2.4f;
        const float Arrive = 0.2f, GrindStart = 0.65f, PopStart = 1.75f;

        Camera cam;
        Text title;
        Action onDone;
        float t;
        int frames;
        Mesh bg, fx;
        Transform board, deck, gripFace, bottomFace;
        readonly Paint p = new Paint();

        public bool Running => onDone != null;
        public float Elapsed => t;

        public void Init(Camera camera, Text street, Action done)
        {
            cam = camera;
            title = street;
            onDone = done;
            bg = Renderer("Nuit du lancement", transform, 400).GetComponent<MeshFilter>().mesh = new Mesh();
            fx = Renderer("Étincelles", transform, 901).GetComponent<MeshFilter>().mesh = new Mesh();
            board = new GameObject("Planche P. ST.").transform;
            board.SetParent(transform, false);
            deck = new GameObject("Profil").transform;
            deck.SetParent(board, false);
            var wood = Hex("#FF7A1A"); var grip = Hex("#1E1E22"); var metal = Hex("#C9CED6"); var wheel = Hex("#FFF4D6");
            new Paint()
                .Capsule(-19, 0, 19, 0, 3.6f, wood)
                .Curve(new Vector2(19, 0), new Vector2(24, 0), new Vector2(28.5f, 4.2f), 3.6f, 3, wood, 6)
                .Curve(new Vector2(-19, 0), new Vector2(-24, 0), new Vector2(-28.5f, 4.2f), 3.6f, 3, wood, 6)
                .Capsule(-19, 1.6f, 19, 1.6f, 1.2f, grip)
                .Rect(-17.5f, -4.6f, 7, 2.6f, metal).Rect(10.5f, -4.6f, 7, 2.6f, metal)
                .Circle(-14, -6.2f, 3.6f, wheel, 16).Circle(14, -6.2f, 3.6f, wheel, 16)
                .Circle(-14, -6.2f, 1.4f, Hex("#7D8590"), 10).Circle(14, -6.2f, 1.4f, Hex("#7D8590"), 10)
                .Make("Profil", deck, 902);

            var top = new Paint().Capsule(-25, 0, 25, 0, 15, wood).Capsule(-24.5f, 0, 24.5f, 0, 13.2f, grip);
            foreach (float cx in new[] { -14f, 14f })
                for (int i = 0; i < 4; i++) top.Circle(cx + (i % 2 == 0 ? -1.6f : 1.6f), i < 2 ? -2.4f : 2.4f, 0.7f, metal, 6);
            gripFace = top.Make("Dessus", board, 902).transform;

            // dessous : orange, liserés rose et jaune, « P. ST. » en grandes lettres
            var under = new Paint().Capsule(-25, 0, 25, 0, 15, wood)
                .Capsule(-21, -5.6f, 21, -5.6f, 1.2f, Hex("#FF4D8D")).Capsule(-21, 5.6f, 21, 5.6f, 1.2f, Hex("#FFE14D"));
            Letters(under, Hex("#1B1028"));
            bottomFace = under.Make("Dessous P. ST.", board, 903).transform;
            gripFace.gameObject.SetActive(false);
            bottomFace.gameObject.SetActive(false);
        }

        /// <summary>« P. ST. » tracé au trait, centré, lettres de 9 de haut.</summary>
        static void Letters(Paint p, Color ink)
        {
            const float w = 1.7f, h = 4.5f;
            void S(float x0, params float[] xy)
            {
                for (int i = 2; i + 1 < xy.Length; i += 2) p.Capsule(x0 + xy[i - 2], xy[i - 1], x0 + xy[i], xy[i + 1], w, ink);
            }
            float x = -15;
            S(x, 0, -h, 0, h, 3.5f, h, 5, 3, 5, 1, 3.5f, 0, 0, 0);                                  // P
            p.Circle(x + 7.5f, -h + 0.6f, 1.1f, ink, 10);                                            // .
            x += 11;
            S(x, 5, 3.5f, 3.5f, h, 1.2f, h, 0, 3, 0.6f, 1, 2.6f, 0, 4.4f, -1, 5, -3, 3.8f, -h, 1.2f, -h, 0, -3.6f);   // S
            x += 8;
            S(x, 0, h, 6, h); S(x, 3, h, 3, -h);                                                    // T
            p.Circle(x + 8.5f, -h + 0.6f, 1.1f, ink, 10);                                            // .
        }

        public void Skip() => Finish();

        void Finish()
        {
            if (onDone == null) return;
            var done = onDone;
            onDone = null;
            title.text = "STREET";
            gameObject.SetActive(false);
            done();
        }

        // kickflip de sortie sur une seule courbe (temps, angle, vitesse) : vif au départ, ralenti le dessous face à
        // l'écran (« P. ST. » lisible), puis fin du tour en douceur
        static readonly float[] Keys =
        {
            0f,    0f,                       3.2f,
            0.9f,  1.5f * Mathf.PI,          0.35f,
            1.35f, 1.5f * Mathf.PI + 0.16f,  0.35f,
            1.8f,  2f * Mathf.PI,            0f,
        };

        static float Flip(float u)
        {
            if (u <= 0) return 0;
            for (int i = 0; i + 3 < Keys.Length; i += 3)
            {
                float t0 = Keys[i], t1 = Keys[i + 3];
                if (u > t1) continue;
                float h = t1 - t0, x = (u - t0) / h, x2 = x * x, x3 = x2 * x;
                return (2 * x3 - 3 * x2 + 1) * Keys[i + 1] + (x3 - 2 * x2 + x) * h * Keys[i + 2]
                     + (-2 * x3 + 3 * x2) * Keys[i + 4] + (x3 - x2) * h * Keys[i + 5];
            }
            return 2f * Mathf.PI;
        }

        static float EaseOut(float x) { x = 1 - Mathf.Clamp01(x); return 1 - x * x * x; }

        void Update()
        {
            if (onDone == null) return;
            // les 2 premières images du lancement sont lentes (chargement, mesuré sur iPhone : 2,4 s puis 0,23 s) :
            // l'animation démarre après elles, et un à-coup plus tard la ralentit au lieu de la faire sauter
            if (++frames > 2) t += Mathf.Min(Time.unscaledDeltaTime, 1f / 30);
            if (t >= Duration) { Finish(); return; }

            // ciel de nuit et lune en haut à gauche, sous l'interface : le titre reste visible ; ils s'effacent à la sortie
            float fade = 1 - Mathf.SmoothStep(0, 1, (t - PopStart) / (Duration - PopStart));
            float cx = cam.transform.position.x * Ppu, cy = cam.transform.position.y * Ppu;
            float half = cam.orthographicSize * cam.aspect * Ppu + 4, hh = cam.orthographicSize * Ppu + 4;
            p.Clear();
            p.Gradient(cx - half, cy - hh, half * 2, hh, Hex("#FF8A4C", fade), Hex("#8E2F6E", fade));
            p.Gradient(cx - half, cy, half * 2, hh, Hex("#8E2F6E", fade), Hex("#1B1028", fade));
            float mx = cx - half + 4 + Scenery.MenuSunPos.x, my = Scenery.MenuSunPos.y, mr = 26;   // à la place du soleil de l'accueil : elle devient soleil au fondu
            p.Glow(mx, my, mr * 3.2f, Hex("#FFF4D6", 0.25f * fade), Hex("#FFF4D6", 0));
            p.Circle(mx, my, mr, Hex("#FFF4D6", fade), 32);
            p.Circle(mx - mr * 0.35f, my + mr * 0.2f, mr * 0.22f, Hex("#E6D8B8", fade), 14)
             .Circle(mx + mr * 0.3f, my - mr * 0.35f, mr * 0.15f, Hex("#E6D8B8", fade), 12)
             .Circle(mx + mr * 0.4f, my + mr * 0.4f, mr * 0.1f, Hex("#E6D8B8", fade), 10);
            p.Build(bg);

            // le rail : le haut des lettres de STREET (largeur du texte, mesurée sur le canevas)
            var rt = title.rectTransform;
            float tw = title.preferredWidth * rt.lossyScale.x * Ppu;
            float left = rt.position.x * Ppu - tw / 2, right = left + tw;
            float rail = rt.position.y * Ppu + title.fontSize * 0.17f * rt.lossyScale.y * Ppu;
            float k = half / 135, bs = tw * 0.5f / 57;   // k : pixel de la maquette (270 de large) ; bs : planche = moitié du titre

            float bx, by, rot = 0, a = 0;
            bool grinding = false;
            if (t < GrindStart)
            {
                float u = EaseOut((t - Arrive) / (GrindStart - Arrive));
                bx = Mathf.Lerp(cx - half - 40 * bs, left + 10 * k, u);
                by = Mathf.Lerp(rail - 90 * k, rail, u) + Mathf.Sin(u * Mathf.PI) * 30 * k;
                rot = 0.2f * (1 - u);
            }
            else if (t < PopStart)
            {
                float u = (t - GrindStart) / (PopStart - GrindStart);
                bx = Mathf.Lerp(left + 10 * k, right - 6 * k, u);
                by = rail;
                rot = 0.06f;
                grinding = true;
            }
            else
            {
                float u = (t - PopStart) / (Duration - PopStart);
                bx = right - 6 * k + u * 45 * k;
                by = rail + Mathf.Sin(u * Mathf.PI * 0.9f) * 22 * k - u * u * 40 * k;
                a = Flip(u * 1.8f);
                rot = -0.2f * u;
            }

            // lettres : éteintes, allumées au passage des roues, toutes allumées après le grind
            var sb = new StringBuilder();
            const string word = "STREET";
            for (int i = 0; i < word.Length; i++)
            {
                float lx = left + (i + 0.5f) * tw / word.Length;
                float on = grinding ? Mathf.Clamp01((bx - lx + 12 * k) / (24 * k)) : t >= PopStart ? 1 : 0.15f;
                sb.Append("<color=#").Append(ColorUtility.ToHtmlStringRGB(Color.Lerp(Hex("#6E3C50"), Hex("#FF7A1A"), on)))
                  .Append('>').Append(word[i]).Append("</color>");
            }
            title.text = sb.ToString();

            // étincelles derrière le truck arrière pendant le grind
            p.Clear();
            if (grinding)
                for (int i = 0; i < 9; i++)
                {
                    float q = (t * 3.1f + i / 9f) % 1, ang = Mathf.PI * (1.05f + 0.5f * (i * 37 % 10) / 10f);
                    float r = (4 + q * 26) * k, sx = bx - 14 * bs, sy = rail + q * 6 * k;
                    var col = (i % 2 == 1 ? Hex("#FFE14D") : Color.white) * new Color(1, 1, 1, 1 - q);
                    p.Capsule(sx + Mathf.Cos(ang) * r, sy - Mathf.Sin(ang) * r * 0.6f,
                              sx + Mathf.Cos(ang) * (r + 5 * k), sy - Mathf.Sin(ang) * (r + 5 * k) * 0.6f, 0.8f * k, col);
                }
            p.Build(fx);

            // planche vue de côté ; les trucks posés sur le rail ; kickflip de sortie
            board.position = W(bx, by + 7 * bs);
            float end = Mathf.Clamp01((t - PopStart) / (Duration - PopStart));
            board.localScale = Vector3.one * bs * (1 - end * end * end);   // rapetisse jusqu'à disparaître : pas de coupure à la fin
            board.rotation = Quaternion.Euler(0, 0, rot * Mathf.Rad2Deg);
            float c = Mathf.Cos(a), s = Mathf.Sin(a);
            deck.localScale = new Vector3(1, c, 1);
            bool face = Mathf.Abs(s) > 0.02f, warm = t < 0.1f;   // les deux faces dessinées d'abord, hors écran : chargées avant le tour
            gripFace.gameObject.SetActive(warm || face && s > 0);
            bottomFace.gameObject.SetActive(warm || face && s < 0);
            var f = s > 0 ? gripFace : bottomFace;
            f.localScale = new Vector3(1, Mathf.Abs(s), 1);
        }
    }
}
