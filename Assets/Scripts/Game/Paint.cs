using System.Collections.Generic;
using UnityEngine;

namespace PalmStreet.Game
{
    /// <summary>
    /// Dessin en formes Unity : polygones convexes, cercles, capsules et dégradés assemblés en un Mesh à couleurs
    /// de sommets. Coordonnées en unités logiques du jeu (y vers le haut, 100 unités = 1 m Unity).
    /// </summary>
    public sealed class Paint
    {
        public static Material Mat;   // matériau sprite du pipeline (fourni par Game)
        public const float Ppu = 100f;

        readonly List<Vector3> v = new List<Vector3>();
        readonly List<Color> c = new List<Color>();
        readonly List<int> t = new List<int>();

        public static Color Lin(Color col) => QualitySettings.activeColorSpace == ColorSpace.Linear ? col.linear : col;
        public static Color Hex(string hex, float a = 1) { ColorUtility.TryParseHtmlString(hex, out var col); col.a = a; return col; }
        public static Vector3 W(float x, float y) => new Vector3(x / Ppu, y / Ppu, 0);

        int Vert(float x, float y, Color col) { v.Add(W(x, y)); c.Add(Lin(col)); return v.Count - 1; }

        /// <summary>Polygone convexe (x0, y0, x1, y1, …).</summary>
        public Paint Poly(Color col, params float[] xy)
        {
            int b = v.Count, n = xy.Length / 2;
            for (int i = 0; i < n; i++) Vert(xy[2 * i], xy[2 * i + 1], col);
            for (int i = 1; i < n - 1; i++) { t.Add(b); t.Add(b + i); t.Add(b + i + 1); }
            return this;
        }

        public Paint Rect(float x, float y, float w, float h, Color col) => Poly(col, x, y, x + w, y, x + w, y + h, x, y + h);

        /// <summary>Rectangle en dégradé vertical.</summary>
        public Paint Gradient(float x, float y, float w, float h, Color bottom, Color top)
        {
            int b = Vert(x, y, bottom); Vert(x + w, y, bottom); Vert(x + w, y + h, top); Vert(x, y + h, top);
            t.Add(b); t.Add(b + 1); t.Add(b + 2); t.Add(b); t.Add(b + 2); t.Add(b + 3);
            return this;
        }

        public Paint Ellipse(float cx, float cy, float rx, float ry, Color col, int seg = 20, float rot = 0)
        {
            int b = Vert(cx, cy, col);
            float cr = Mathf.Cos(rot), sr = Mathf.Sin(rot);
            for (int i = 0; i <= seg; i++)
            {
                float a = i * Mathf.PI * 2 / seg, x = Mathf.Cos(a) * rx, y = Mathf.Sin(a) * ry;
                Vert(cx + x * cr - y * sr, cy + x * sr + y * cr, col);
                if (i > 0) { t.Add(b); t.Add(b + i); t.Add(b + i + 1); }
            }
            return this;
        }

        public Paint Circle(float cx, float cy, float r, Color col, int seg = 20) => Ellipse(cx, cy, r, r, col, seg);

        /// <summary>Disque en dégradé du centre au bord (halos, lueurs).</summary>
        public Paint Glow(float cx, float cy, float r, Color inner, Color outer, int seg = 32)
        {
            int b = Vert(cx, cy, inner);
            for (int i = 0; i <= seg; i++)
            {
                float a = i * Mathf.PI * 2 / seg;
                Vert(cx + Mathf.Cos(a) * r, cy + Mathf.Sin(a) * r, outer);
                if (i > 0) { t.Add(b); t.Add(b + i); t.Add(b + i + 1); }
            }
            return this;
        }

        /// <summary>Demi-disque (dômes, sommets d'arbres).</summary>
        public Paint Dome(float cx, float cy, float rx, float ry, Color col, int seg = 12)
        {
            int b = Vert(cx, cy, col);
            for (int i = 0; i <= seg; i++)
            {
                float a = i * Mathf.PI / seg;
                Vert(cx + Mathf.Cos(a) * rx, cy + Mathf.Sin(a) * ry, col);
                if (i > 0) { t.Add(b); t.Add(b + i); t.Add(b + i + 1); }
            }
            return this;
        }

        public Paint Line(float x0, float y0, float x1, float y1, float w, Color col)
        {
            var d = new Vector2(x1 - x0, y1 - y0);
            if (d.sqrMagnitude < 1e-6f) return this;
            var n = new Vector2(-d.y, d.x).normalized * (w / 2);
            return Poly(col, x0 + n.x, y0 + n.y, x1 + n.x, y1 + n.y, x1 - n.x, y1 - n.y, x0 - n.x, y0 - n.y);
        }

        /// <summary>Trait à bouts ronds (membres, palmes, barres).</summary>
        public Paint Capsule(float x0, float y0, float x1, float y1, float w, Color col)
        {
            Line(x0, y0, x1, y1, w, col);
            Circle(x0, y0, w / 2, col, 10);
            return Circle(x1, y1, w / 2, col, 10);
        }

        /// <summary>Courbe de Bézier quadratique tracée en capsules (palmes, câbles).</summary>
        public Paint Curve(Vector2 a, Vector2 ctrl, Vector2 b, float w0, float w1, Color col, int seg = 8)
        {
            Vector2 prev = a;
            for (int i = 1; i <= seg; i++)
            {
                float k = (float)i / seg;
                var p = (1 - k) * (1 - k) * a + 2 * (1 - k) * k * ctrl + k * k * b;
                Capsule(prev.x, prev.y, p.x, p.y, Mathf.Lerp(w0, w1, k), col);
                prev = p;
            }
            return this;
        }

        public int VertexCount => v.Count;

        public Mesh Build(Mesh m = null)
        {
            m ??= new Mesh();
            m.Clear();
            m.SetVertices(v);
            m.SetColors(c);
            m.SetTriangles(t, 0);
            m.RecalculateBounds();
            return m;
        }

        public void Clear() { v.Clear(); c.Clear(); t.Clear(); }

        /// <summary>Objet Unity qui affiche ces formes.</summary>
        public MeshRenderer Make(string name, Transform parent, int order, string layer = "Default")
        {
            var mr = Renderer(name, parent, order, layer);
            mr.GetComponent<MeshFilter>().sharedMesh = Build();
            return mr;
        }

        public static MeshRenderer Renderer(string name, Transform parent, int order, string layer = "Default")
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>();
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = Mat;
            mr.sortingLayerName = layer;
            mr.sortingOrder = order;
            var block = new MaterialPropertyBlock();
            block.SetTexture("_MainTex", Texture2D.whiteTexture);
            mr.SetPropertyBlock(block);
            return mr;
        }
    }

    /// <summary>Mesh dont on fait varier l'opacité (fondu entre décors) en réécrivant ses couleurs.</summary>
    public sealed class Fader
    {
        readonly Mesh mesh;
        readonly Color[] base_;
        readonly Color[] work;
        float alpha = -1;
        public readonly GameObject[] Objects;

        public Fader(Mesh mesh, params GameObject[] objects)
        {
            this.mesh = mesh;
            base_ = mesh.colors;
            work = new Color[base_.Length];
            Objects = objects;
        }

        public void SetAlpha(float a)
        {
            if (Mathf.Approximately(a, alpha)) return;
            alpha = a;
            foreach (var o in Objects) o.SetActive(a > 0.001f);
            if (a <= 0.001f) return;
            for (int i = 0; i < base_.Length; i++) { var col = base_[i]; col.a *= a; work[i] = col; }
            mesh.colors = work;
        }
    }
}
