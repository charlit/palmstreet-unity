using PalmStreet.Core;
using UnityEngine;
using UnityEngine.UI;
using static PalmStreet.Game.Paint;

namespace PalmStreet.Game
{
    /// <summary>
    /// Fond de la carte du mode histoire (spec 002, FR-002) : le Pays basque dessiné en formes simples, en
    /// coordonnées 0..1 de la zone (u vers l'est, v vers le nord ; géographie un peu étirée pour l'écran) :
    /// océan, côte et plages, Adour et Nive, frontière espagnole, la Rhune, et la route d'un niveau à l'autre.
    /// </summary>
    public sealed class MapArt : MaskableGraphic
    {
        /// <summary>Villes des 12 niveaux, dans l'ordre (Biarritz la nuit : sur le rocher de la Vierge).</summary>
        public static readonly Vector2[] Towns =
        {
            new Vector2(0.60f, 0.86f),   // 1 Anglet
            new Vector2(0.50f, 0.74f),   // 2 Biarritz
            new Vector2(0.43f, 0.62f),   // 3 Bidart
            new Vector2(0.33f, 0.52f),   // 4 Guéthary
            new Vector2(0.27f, 0.40f),   // 5 Saint-Jean-de-Luz
            new Vector2(0.12f, 0.27f),   // 6 Hendaye
            new Vector2(0.84f, 0.88f),   // 7 Bayonne
            new Vector2(0.66f, 0.66f),   // 8 Arcangues
            new Vector2(0.70f, 0.24f),   // 9 Espelette
            new Vector2(0.40f, 0.29f),   // 10 Ascain
            new Vector2(0.86f, 0.42f),   // 11 Cambo-les-Bains
            new Vector2(0.27f, 0.80f),   // 12 Biarritz la nuit
        };

        // côte du nord au sud (v décroissant) ; au sud de Hendaye, l'Espagne
        static readonly Vector2[] Coast =
        {
            new Vector2(0.62f, 1.00f), new Vector2(0.57f, 0.92f), new Vector2(0.50f, 0.85f), new Vector2(0.41f, 0.79f),
            new Vector2(0.40f, 0.72f), new Vector2(0.36f, 0.63f), new Vector2(0.27f, 0.53f), new Vector2(0.22f, 0.45f),
            new Vector2(0.17f, 0.42f), new Vector2(0.19f, 0.38f), new Vector2(0.12f, 0.34f), new Vector2(0.04f, 0.30f),
            new Vector2(0.00f, 0.29f),
        };
        static readonly Vector2[] Adour = { new Vector2(0.60f, 0.97f), new Vector2(0.70f, 0.93f), new Vector2(0.84f, 0.90f), new Vector2(1.00f, 0.93f) };
        static readonly Vector2[] Nive = { new Vector2(0.84f, 0.90f), new Vector2(0.83f, 0.72f), new Vector2(0.88f, 0.55f), new Vector2(0.85f, 0.38f), new Vector2(0.92f, 0.20f), new Vector2(1.00f, 0.10f) };
        static readonly Vector2[] Border =
        {
            new Vector2(0.04f, 0.30f), new Vector2(0.12f, 0.20f), new Vector2(0.26f, 0.13f), new Vector2(0.42f, 0.10f),
            new Vector2(0.56f, 0.13f), new Vector2(0.70f, 0.09f), new Vector2(0.84f, 0.06f), new Vector2(1.00f, 0.09f),
        };

        /// <summary>Niveaux ouverts : route discrète entre eux, étape vers le dernier en pointillés jaunes.</summary>
        public int Open;
        /// <summary>Étoiles de chaque ville (0 à 3), dessinées à côté de la pastille.</summary>
        public int[] Stars;

        Rect r;
        VertexHelper vh;


        Vector2 P(Vector2 uv) => new Vector2(r.xMin + uv.x * r.width, r.yMin + uv.y * r.height);

        void Quad(Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color col)
        {
            int i = vh.currentVertCount;
            vh.AddVert(a, col, Vector4.zero); vh.AddVert(b, col, Vector4.zero);
            vh.AddVert(c, col, Vector4.zero); vh.AddVert(d, col, Vector4.zero);
            vh.AddTriangle(i, i + 1, i + 2); vh.AddTriangle(i, i + 2, i + 3);
        }

        void Dot(Vector2 c, float rad, Color col, int seg = 10)
        {
            int i = vh.currentVertCount;
            vh.AddVert(c, col, Vector4.zero);
            for (int k = 0; k <= seg; k++)
            {
                float a = k * Mathf.PI * 2 / seg;
                vh.AddVert(c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * rad, col, Vector4.zero);
                if (k > 0) vh.AddTriangle(i, i + k, i + k + 1);
            }
        }

        void Star(Vector2 c, float rad, Color col)
        {
            int b = vh.currentVertCount;
            vh.AddVert(c, col, Vector4.zero);
            for (int k = 0; k <= 10; k++)
            {
                float a = Mathf.PI / 2 + k * Mathf.PI / 5, rr = k % 2 == 0 ? rad : rad * 0.45f;
                vh.AddVert(c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * rr, col, Vector4.zero);
                if (k > 0) vh.AddTriangle(b, b + k, b + k + 1);
            }
        }

        /// <summary>Trait en pixels ; dash > 0 : pointillés de cette longueur.</summary>
        void Line(Vector2[] uv, float w, Color col, float dash = 0)
        {
            for (int i = 0; i + 1 < uv.Length; i++) Segment(P(uv[i]), P(uv[i + 1]), w, col, dash);
        }

        void Segment(Vector2 a, Vector2 b, float w, Color col, float dash = 0)
        {
            var d = b - a;
            float len = d.magnitude;
            if (len < 0.01f) return;
            var n = new Vector2(-d.y, d.x) / len * w / 2;
            if (dash <= 0)
            {
                Quad(a - n, b - n, b + n, a + n, col);
                Dot(b, w / 2, col, 8);   // jointure arrondie
                return;
            }
            for (float s = 0; s < len; s += dash * 2)
            {
                var p0 = a + d * (s / len); var p1 = a + d * (Mathf.Min(s + dash, len) / len);
                Quad(p0 - n, p1 - n, p1 + n, p0 + n, col);
            }
        }

        protected override void OnPopulateMesh(VertexHelper helper)
        {
            vh = helper;
            vh.Clear();
            r = rectTransform.rect;
            Vector2 Uv(float u, float v) => P(new Vector2(u, v));

            Quad(Uv(0, 0), Uv(1, 0), Uv(1, 1), Uv(0, 1), Hex("#3E6B47"));          // la terre
            for (int i = 0; i + 1 < Border.Length; i++)                               // l'Espagne, un peu plus sèche
                Quad(Uv(Border[i].x, 0), Uv(Border[i + 1].x, 0), P(Border[i + 1]), P(Border[i]), Hex("#5A6E3E"));
            Quad(Uv(0, 0), Uv(Border[0].x, 0), P(Border[0]), Uv(0, Border[0].y), Hex("#5A6E3E"));
            for (int i = 0; i + 1 < Coast.Length; i++)                                // l'océan, à gauche de la côte
                Quad(Uv(0, Coast[i + 1].y), P(Coast[i + 1]), P(Coast[i]), Uv(0, Coast[i].y), Hex("#1D5F8C"));
            Quad(Uv(0, Coast[0].y), P(Coast[0]), Uv(Coast[0].x, 1), Uv(0, 1), Hex("#1D5F8C"));
            for (int k = 1; k <= 3; k++)                                              // vagues au large
                for (int j = 0; j < 4; j++)
                {
                    var c = Uv(0.05f + 0.07f * k + (j % 2) * 0.04f, 0.45f + j * 0.13f + k * 0.03f);
                    Segment(c, c + new Vector2(12, 0), 2, Hex("#FFFFFF", 0.22f));
                }
            Line(Coast, 5, Hex("#E9D8A6"));                                           // plages
            Line(Adour, 5, Hex("#3C8DC0"));
            Line(Nive, 4, Hex("#3C8DC0"));
            Line(Border, 2, Hex("#FFF4E0", 0.55f), 5);

            // la Rhune, au-dessus d'Ascain
            var m = Uv(0.40f, 0.17f);
            int b = vh.currentVertCount;
            vh.AddVert(m + new Vector2(-30, -8), Hex("#2C4A33"), Vector4.zero);
            vh.AddVert(m + new Vector2(30, -8), Hex("#2C4A33"), Vector4.zero);
            vh.AddVert(m + new Vector2(0, 26), Hex("#2C4A33"), Vector4.zero);
            vh.AddTriangle(b, b + 1, b + 2);
            b = vh.currentVertCount;
            vh.AddVert(m + new Vector2(-9, 15.6f), Hex("#FFF4E0"), Vector4.zero);
            vh.AddVert(m + new Vector2(9, 15.6f), Hex("#FFF4E0"), Vector4.zero);
            vh.AddVert(m + new Vector2(0, 26), Hex("#FFF4E0"), Vector4.zero);
            vh.AddTriangle(b, b + 1, b + 2);

            // la route : les étapes déjà faites en discret, l'étape vers le prochain niveau bien visible
            for (int i = 0; i + 2 <= Open && i + 1 < Towns.Length; i++)
            {
                bool next = i + 2 == Open;
                Segment(P(Towns[i]), P(Towns[i + 1]), next ? 4 : 2, next ? Hex("#FFE14D") : Hex("#FFF4E0", 0.3f), next ? 7 : 0);
            }

            for (int i = 0; Stars != null && i < Stars.Length; i++)
                for (int k = 0; k < 3; k++)
                    Star(P(Towns[i]) + new Vector2((Levels.All[i].Boss != BossKind.None ? 31 : 27) + (k == 1 ? 5 : 0), 13 - k * 13), 6.5f, k < Stars[i] ? Hex("#FFE14D") : Hex("#1B1028", 0.45f));

            // liseré autour de la carte
            var cream = Hex("#FFF4E0", 0.8f);
            Segment(Uv(0, 0), Uv(1, 0), 3, cream); Segment(Uv(1, 0), Uv(1, 1), 3, cream);
            Segment(Uv(1, 1), Uv(0, 1), 3, cream); Segment(Uv(0, 1), Uv(0, 0), 3, cream);
        }
    }
}
