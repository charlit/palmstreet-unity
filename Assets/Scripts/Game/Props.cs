using System.Collections.Generic;
using PalmStreet.Core;
using UnityEngine;
using static PalmStreet.Game.Paint;

namespace PalmStreet.Game
{
    /// <summary>
    /// Obstacles, éclairs, boss et dents à l'écran. Chaque objet de la partie reçoit un objet Unity tiré d'une
    /// réserve, rendu à la réserve quand il quitte la partie.
    /// </summary>
    public sealed class Props : MonoBehaviour
    {
        sealed class Pool
        {
            readonly System.Func<Transform> make;
            readonly Stack<Transform> free = new Stack<Transform>();
            readonly Dictionary<object, Transform> used = new Dictionary<object, Transform>();
            readonly HashSet<object> seen = new HashSet<object>();
            readonly List<object> gone = new List<object>();
            public Pool(System.Func<Transform> make) { this.make = make; }

            public void Begin() => seen.Clear();

            public Transform Get(object key, out bool fresh)
            {
                seen.Add(key);
                fresh = !used.TryGetValue(key, out var t);
                if (fresh)
                {
                    t = free.Count > 0 ? free.Pop() : make();
                    t.gameObject.SetActive(true);
                    used[key] = t;
                }
                return t;
            }

            public void End()
            {
                gone.Clear();
                foreach (var k in used.Keys) if (!seen.Contains(k)) gone.Add(k);
                foreach (var k in gone) { var t = used[k]; used.Remove(k); t.gameObject.SetActive(false); free.Push(t); }
            }
        }

        Pool cones, bolts, teeth, shadows, rails;
        Transform boss, bossWingL, bossWingR, bossPupil, bossShadow;
        static readonly Color Shadow = new Color(0.05f, 0.02f, 0.08f, 0.35f);

        public void Init()
        {
            var coneMesh = Cone(Hex("#FF7A1A"), Hex("#FFF4E0"));
            var bossCone = Cone(Hex("#B04BFF"), Hex("#FFE14D"));
            cones = new Pool(() =>
            {
                var t = new GameObject("Plot").transform;
                t.SetParent(transform, false);
                var mr = Renderer("Plot", t, 12);
                mr.GetComponent<MeshFilter>().sharedMesh = coneMesh;
                mr.name = "Forme";
                var glow = new Paint().Glow(0.5f, 0.45f, 0.9f, new Color(0.8f, 0.3f, 1, 0.55f), new Color(0.8f, 0.3f, 1, 0)).Make("Lueur", t, 11);
                glow.gameObject.SetActive(false);
                return t;
            });
            this.bossCone = bossCone;
            this.coneMesh = coneMesh;

            var boltMesh = new Paint()
                .Glow(0, 0, 20, new Color(1, 0.88f, 0.3f, 0.55f), new Color(1, 0.88f, 0.3f, 0))
                .Poly(Hex("#B8860B"), 3, 13.5f, 9.5f, 13.5f, 4, 0.5f, -3.5f, 0.5f)
                .Poly(Hex("#B8860B"), 0.5f, 1.5f, 8.5f, 1.5f, -5.5f, -14.5f)
                .Poly(Hex("#FFE14D"), 3, 12, 8, 12, 3, 1.5f, -2.5f, 1.5f)
                .Poly(Hex("#FFE14D"), 1, 1, 7, 1, -4, -12.5f)
                .Poly(Hex("#FFFBE0"), 4, 11, 6, 11, 2.5f, 4, 1, 4)
                .Build();
            bolts = new Pool(() =>
            {
                var mr = Renderer("Éclair", transform, 14);
                mr.GetComponent<MeshFilter>().sharedMesh = boltMesh;
                return mr.transform;
            });

            var toothMesh = new Paint()
                .Glow(0, 0, 11, new Color(1, 1, 1, 0.5f), new Color(1, 1, 1, 0))
                .Ellipse(0, 2, 6, 4.5f, Hex("#FFFDF5"), 16)
                .Poly(Hex("#FFFDF5"), -6, 2, -1, 2, -2, -6, -5, -6)
                .Poly(Hex("#FFFDF5"), 1, 2, 6, 2, 5, -6, 2, -6)
                .Ellipse(-2, 4, 2, 1.2f, Hex("#FFFFFF"), 8)
                .Build();
            teeth = new Pool(() =>
            {
                var mr = Renderer("Dent", transform, 16);
                mr.GetComponent<MeshFilter>().sharedMesh = toothMesh;
                return mr.transform;
            });

            var shadowMesh = new Paint().Glow(0, 0, 1, Shadow, new Color(Shadow.r, Shadow.g, Shadow.b, 0), 20).Build();
            shadows = new Pool(() =>
            {
                var mr = Renderer("Ombre", transform, 5);
                mr.GetComponent<MeshFilter>().sharedMesh = shadowMesh;
                return mr.transform;
            });

            rails = new Pool(() =>
            {
                var mr = Renderer("Rail", transform, 10);
                mr.GetComponent<MeshFilter>().mesh = new Mesh();
                return mr.transform;
            });

            BuildBoss();
        }

        Mesh coneMesh, bossCone;

        static Mesh Cone(Color body, Color stripe)
        {
            // plot de 1 × 1 (mis à l'échelle de chaque obstacle) : socle, corps, bandes réfléchissantes, reflet
            var dark = Color.Lerp(body, Color.black, 0.45f);
            var light = Color.Lerp(body, Color.white, 0.35f);
            return new Paint()
                .Rect(-0.08f, 0, 1.16f, 0.1f, dark)
                .Poly(body, 0.1f, 0.1f, 0.9f, 0.1f, 0.58f, 1, 0.42f, 1)
                .Poly(stripe, 0.24f, 0.42f, 0.76f, 0.42f, 0.69f, 0.6f, 0.31f, 0.6f)
                .Poly(stripe, 0.33f, 0.72f, 0.67f, 0.72f, 0.62f, 0.84f, 0.38f, 0.84f)
                .Poly(light, 0.16f, 0.12f, 0.3f, 0.12f, 0.47f, 0.97f, 0.43f, 0.97f)
                .Build();
        }

        void BuildBoss()
        {
            // le boss : une grosse boule violette ailée, un œil, une bouche pleine de dents
            boss = new GameObject("Boss").transform;
            boss.SetParent(transform, false);
            var purple = Hex("#6A2C91"); var deep = Hex("#3E1659"); var belly = Hex("#A55BD6");
            Transform Wing(string name, float dir)
            {
                var t = new GameObject(name).transform;
                t.SetParent(boss, false);
                t.localPosition = W(-10 * dir, 18);
                var p = new Paint();
                p.Ellipse(-dir * 34, 10, 38, 16, deep, 18, dir * 0.4f);
                for (int i = 0; i < 3; i++) p.Capsule(0, 0, -dir * (46 + i * 8), 30 - i * 18, 3, purple);
                p.Make(name, t, 18);
                return t;
            }
            bossWingL = Wing("Aile arrière", -1);
            bossWingR = Wing("Aile avant", 1);
            new Paint()
                .Poly(deep, -26, 34, -14, 44, -10, 70)
                .Poly(deep, 14, 44, 26, 34, 10, 70)
                .Circle(0, 0, Boss.Size / 2, purple, 40)
                .Ellipse(-6, -14, 30, 24, belly, 30)
                .Glow(-14, 18, 22, new Color(1, 1, 1, 0.25f), new Color(1, 1, 1, 0))
                .Circle(-18, 12, 13, Hex("#FFFDF5"), 24)
                .Poly(deep, -34, 30, -4, 22, -4, 26, -32, 34)
                .Ellipse(-14, -16, 22, 11, Hex("#26081F"), 24)
                .Poly(Hex("#FFFDF5"), -32, -12, -26, -12, -29, -20)
                .Poly(Hex("#FFFDF5"), -24, -9, -17, -9, -20.5f, -18)
                .Poly(Hex("#FFFDF5"), -15, -8, -8, -8, -11.5f, -17)
                .Poly(Hex("#FFFDF5"), -6, -9, 0, -10, -3, -18)
                .Poly(Hex("#FFFDF5"), -27, -24, -21, -21, -24, -15)
                .Poly(Hex("#FFFDF5"), -12, -24, -6, -23, -9, -16)
                .Make("Corps", boss, 20);
            bossPupil = new Paint().Circle(0, 0, 5.5f, Hex("#1B1028"), 16).Circle(1.5f, 1.8f, 1.6f, Color.white, 8).Make("Pupille", boss, 21).transform;
            bossShadow = new Paint().Glow(0, 0, 1, Shadow, new Color(Shadow.r, Shadow.g, Shadow.b, 0), 20).Make("Ombre du boss", transform, 5).transform;
            boss.gameObject.SetActive(false);
            bossShadow.gameObject.SetActive(false);
        }

        public void Draw(Run run, float time)
        {
            cones.Begin(); bolts.Begin(); teeth.Begin(); shadows.Begin(); rails.Begin();
            var g = run.Ground;
            foreach (var o in run.Obstacles)
            {
                if (o.IsRail)
                {
                    var t = rails.Get(o, out bool fresh);
                    if (fresh) BuildRail(t.GetComponent<MeshFilter>().sharedMesh, o);
                    t.position = W(o.X, o.Y);
                    ShadowAt(o, o.X + o.W / 2, o.Y, o.W + 16, 0);
                    continue;
                }
                var c = cones.Get(o, out _);
                bool dropped = o.Kind == ObstacleKind.DroppedCone;
                c.GetChild(0).GetComponent<MeshFilter>().sharedMesh = dropped ? bossCone : coneMesh;
                c.GetChild(1).gameObject.SetActive(dropped);
                c.position = W(o.X, o.Y);
                c.localScale = new Vector3(o.W, o.H, 1);   // le plot est dessiné en 1 × 1
                c.rotation = Quaternion.Euler(0, 0, o.Falling ? Mathf.Sin(time * 14 + o.X) * 12 : 0);
                ShadowAt(o, o.X + o.W / 2, g.Height(o.X + o.W / 2), o.W * 0.75f, o.Y - g.Height(o.X + o.W / 2));
            }
            foreach (var b in run.Bolts)
            {
                var t = bolts.Get(b, out _);
                t.position = W(b.X, b.Y + Mathf.Sin(time * 4 + b.X * 0.05f) * 3);
                t.localScale = new Vector3(0.8f + 0.2f * Mathf.Cos(time * 5 + b.X * 0.1f), 1, 1);
            }

            var sk = run.Skater;
            if (run.Phase == Phase.Playing)
            {
                float sg = sk.Rail != null ? sk.Rail.Top : g.Height(sk.X);
                ShadowAt(sk, sk.X, sg, 40, sk.Y - sg);
            }

            var boss_ = run.Boss;
            boss.gameObject.SetActive(boss_ != null);
            bossShadow.gameObject.SetActive(boss_ != null);
            if (boss_ != null)
            {
                foreach (var tooth in boss_.Teeth)
                {
                    var t = teeth.Get(tooth, out _);
                    t.position = W(tooth.X, tooth.Y);
                    t.rotation = Quaternion.Euler(0, 0, -time * 720 + tooth.X);
                }
                float enter = boss_.State == BossState.Arriving ? 1 - (float)boss_.Timer / Boss.ArriveSteps : 0;
                float gy = g.Height(boss_.X), hover = Mathf.Sin(time * 3) * 4;
                var pos = new Vector2(boss_.X + enter * enter * 320, gy + boss_.Lift + Boss.Size / 2 + 6 + hover);
                float scale = 1;
                if (boss_.State == BossState.Dying)
                {
                    float k = boss_.Timer / 90f;
                    pos += new Vector2(Random.Range(-5f, 5f), Random.Range(-5f, 5f)) * (1 - k);
                    scale = 1 + k * 0.4f;
                    if (k > 0.75f) scale *= (1 - k) * 4;
                }
                boss.position = W(pos.x, pos.y);
                boss.localScale = Vector3.one * scale * (1 + hitPunch * 0.08f);
                hitPunch = Mathf.MoveTowards(hitPunch, 0, Time.deltaTime * 6);
                float flap = Mathf.Sin(time * (boss_.State == BossState.Ground ? 6 : 16)) * 25;
                bossWingL.localRotation = Quaternion.Euler(0, 0, -flap);
                bossWingR.localRotation = Quaternion.Euler(0, 0, flap);
                var look = new Vector2(run.Skater.X - pos.x, run.Skater.Y + 40 - pos.y).normalized;
                bossPupil.localPosition = W(-18 + look.x * 5, 12 + look.y * 5);
                float lift = pos.y - gy - Boss.Size / 2;
                bossShadow.position = W(pos.x, gy + 2);
                bossShadow.localScale = new Vector3(50, 9, 1) * Mathf.Lerp(1, 0.5f, lift / 160) * (1 - enter);
            }

            cones.End(); bolts.End(); teeth.End(); shadows.End(); rails.End();
        }

        float hitPunch;
        public void BossHit() => hitPunch = 1;

        /// <summary>Ombre douce au sol, plus petite quand l'objet est en l'air.</summary>
        public void ShadowAt(object key, float x, float groundY, float width, float lift)
        {
            var t = shadows.Get(key, out _);
            float k = Mathf.Clamp01(1 - lift / 180);
            t.position = W(x, groundY + 1);
            t.localScale = new Vector3(width / 2 * (0.5f + 0.5f * k), 4 * k, 1);   // disque de rayon 1
        }

        static void BuildRail(Mesh mesh, Obstacle o)
        {
            var p = new Paint();
            float h = o.H, w = o.W;
            var post = Hex("#2A2D36");
            for (float x = 4; x <= w - 4 + 0.1f; x += Mathf.Max(40, (w - 8) / Mathf.Max(1, Mathf.Round((w - 8) / 70))))
            {
                p.Rect(x - 2.5f, 0, 5, h - 2, post);
                p.Rect(x - 5, 0, 10, 2.5f, post);
            }
            switch (o.Kind)
            {
                case ObstacleKind.BigRail:
                    // grand rail arc-en-ciel (FR-008)
                    for (float x = 0; x < w; x += 6)
                    {
                        var col = Color.HSVToRGB(Mathf.Repeat(x / 140f, 1), 0.75f, 1);
                        p.Rect(x, h - 5.5f, Mathf.Min(6.5f, w - x), 7, col);
                    }
                    p.Rect(0, h + 0.4f, w, 1.4f, new Color(1, 1, 1, 0.65f));
                    break;
                case ObstacleKind.NoseRail:
                    p.Capsule(0, h - 2, w, h - 2, 7, Hex("#F2B705"));
                    p.Rect(0, h - 0.5f, w, 1.6f, Hex("#FFF2B0"));
                    for (float x = 30; x < w; x += 90) p.Rect(x, h - 4, 30, 2, Hex("#C28E00"));
                    break;
                default:
                    p.Capsule(0, h - 2, w, h - 2, 6, Hex("#9AA3B5"));
                    p.Rect(0, h - 0.5f, w, 1.5f, Hex("#E6ECF5"));
                    break;
            }
            p.Build(mesh);
        }
    }
}
