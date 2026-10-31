using System;
using UnityEngine;
using static PalmStreet.Game.Paint;

namespace PalmStreet.Game
{
    /// <summary>
    /// Animation de lancement (spec 002, FR-028) : sur un fond de coucher de soleil, une grande planche fait un
    /// kickflip au ralenti ; elle s'arrête un instant le dessous face à l'écran, où est écrit « P. ST. », finit son
    /// tour, puis le fond s'efface sur l'accueil et son titre PALM STREET. 2,4 s ; un toucher passe.
    /// Dessinée au-dessus de l'interface (ordres 900 et plus).
    /// </summary>
    public sealed class Splash : MonoBehaviour
    {
        public const float Duration = 2.4f;
        const float Scale = 3.4f;

        Camera cam;
        Action onDone;
        float t;
        Mesh bg;
        Transform board, deck, gripFace, bottomFace;
        readonly Paint p = new Paint();

        public bool Running => onDone != null;
        public float Elapsed => t;

        public void Init(Camera camera, Action done)
        {
            cam = camera;
            onDone = done;
            bg = Renderer("Fond du lancement", transform, 900).GetComponent<MeshFilter>().mesh = new Mesh();

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
            gameObject.SetActive(false);
            done();
        }

        void Update()
        {
            if (onDone == null) return;
            t += Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            if (t >= Duration) { Finish(); return; }

            // fond : nuit violette vers coucher de soleil, qui s'efface à la fin
            float fade = Mathf.Clamp01((Duration - t) / 0.45f);
            float cx = cam.transform.position.x * Ppu, cy = cam.transform.position.y * Ppu;
            float half = cam.orthographicSize * cam.aspect * Ppu + 4, hh = cam.orthographicSize * Ppu + 4;
            p.Clear();
            p.Gradient(cx - half, cy - hh, half * 2, hh, Hex("#FF8A4C", fade), Hex("#8E2F6E", fade));
            p.Gradient(cx - half, cy, half * 2, hh, Hex("#8E2F6E", fade), Hex("#1B1028", fade));
            p.Glow(cx, cy - 20, 150, Hex("#FFD27A", 0.35f * fade), Hex("#FFD27A", 0));
            p.Build(bg);

            // planche : saut, kickflip au ralenti jusqu'au dessous face à l'écran, pause, fin du tour
            float a;
            if (t < 1.0f) { float k = Mathf.SmoothStep(0, 1, t / 1.0f); a = k * 1.5f * Mathf.PI; }
            else if (t < 1.5f) a = 1.5f * Mathf.PI;                                                   // « P. ST. » bien lisible
            else { float k = Mathf.SmoothStep(0, 1, Mathf.Clamp01((t - 1.5f) / 0.4f)); a = (1.5f + 0.5f * k) * Mathf.PI; }
            float rise = Mathf.Sin(Mathf.Clamp01(t / 1.9f) * Mathf.PI) * 40;
            float shrink = 1 - Mathf.Clamp01((t - 1.9f) / 0.5f);
            board.position = W(cx, cy + 10 + rise);
            board.localScale = Vector3.one * Scale * shrink;
            board.rotation = Quaternion.Euler(0, 0, Mathf.Sin(t * 2.4f) * 6);
            float c = Mathf.Cos(a), s = Mathf.Sin(a);
            deck.localScale = new Vector3(1, c, 1);
            bool face = Mathf.Abs(s) > 0.08f;
            gripFace.gameObject.SetActive(face && s > 0);
            bottomFace.gameObject.SetActive(face && s < 0);
            var f = s > 0 ? gripFace : bottomFace;
            f.localScale = new Vector3(1, Mathf.Abs(s), 1);
        }
    }
}
