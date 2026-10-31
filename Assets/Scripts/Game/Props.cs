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

        Pool cones, bolts, teeth, shadows, rails, gulls, pads;
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

            // mouette (FR-031) : corps blanc, bec jaune, ailes qui battent, halo rose pour la nuit
            gulls = new Pool(() =>
            {
                var t = new GameObject("Mouette").transform;
                t.SetParent(transform, false);
                new Paint()
                    .Glow(15, 8, 26, Hex("#FF4FD8", 0.35f), Hex("#FF4FD8", 0))
                    .Ellipse(15, 8, 14, 6, Hex("#F4F6FA"), 18)
                    .Circle(4, 11, 5, Hex("#F4F6FA"), 14)
                    .Poly(Hex("#FFB21A"), -1, 11, -8, 9.5f, -1, 8.5f)
                    .Circle(3, 12.5f, 1.1f, Hex("#1B1028"), 6)
                    .Poly(Hex("#B9C0CC"), 26, 9, 34, 13, 34, 5)
                    .Make("Corps", t, 16);
                foreach (float side in new[] { -1f, 1f })
                {
                    var w = new GameObject(side < 0 ? "Aile arrière" : "Aile avant").transform;
                    w.SetParent(t, false);
                    w.localPosition = W(15, 10);
                    new Paint().Capsule(0, 0, 4, 14, 5, Hex(side < 0 ? "#C9CED6" : "#FFFFFF")).Capsule(4, 14, 12, 18, 3.5f, Hex("#3A3F4A")).Make("Aile", w, side < 0 ? 15 : 17);
                }
                return t;
            });

            // tremplin néon (FR-032) : socle sombre, bord cyan, chevrons vers le haut
            var padMesh = new Paint()
                .Glow(20, 6, 34, Hex("#35F2FF", 0.4f), Hex("#35F2FF", 0))
                .Poly(Hex("#1B1028"), 0, 0, 40, 0, 36, 6, 4, 6)
                .Rect(4, 5, 32, 2, Hex("#35F2FF"))
                .Poly(Hex("#35F2FF"), 14, 10, 20, 16, 26, 10, 26, 13, 20, 19, 14, 13)
                .Poly(Hex("#FF4FD8"), 14, 18, 20, 24, 26, 18, 26, 21, 20, 27, 14, 21)
                .Build();
            pads = new Pool(() =>
            {
                var mr = Renderer("Tremplin", transform, 11);
                mr.GetComponent<MeshFilter>().sharedMesh = padMesh;
                return mr.transform;
            });
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

        Transform finish, crab, crabClawL, crabClawR, gullBoss, gullWingB, gullWingF, boar, boarLegs;
        Mesh shellMesh, barrierMesh;
        Pool barriers;

        /// <summary>Le Crabe, le Goéland géant et le Sanglier (spec 002, FR-011), tournés vers le skateur (à gauche).</summary>
        void BuildBeasts()
        {
            var red = Hex("#E0452B"); var dark = Hex("#9E2A18"); var white = Hex("#FFFDF5"); var ink = Hex("#1B1028");
            // crabe : carapace, yeux sur pédoncules, pinces qui claquent, pattes
            crab = new GameObject("Crabe").transform; crab.SetParent(transform, false);
            var c = new Paint();
            for (int i = 0; i < 3; i++) { float x = -14 + i * 12; c.Line(x, -6, x - 8, -18, 3, dark); c.Line(x + 4, -6, x + 12, -18, 3, dark); }
            c.Line(-8, 10, -12, 22, 2.5f, dark).Line(4, 10, 2, 22, 2.5f, dark)
             .Ellipse(0, 0, 26, 15, red, 28).Ellipse(0, -6, 22, 8, dark, 24).Glow(-6, 6, 14, new Color(1, 1, 1, 0.25f), new Color(1, 1, 1, 0))
             .Circle(-12, 23, 5, white, 12).Circle(2, 23, 5, white, 12).Circle(-13.5f, 23, 2.2f, ink, 8).Circle(0.5f, 23, 2.2f, ink, 8)
             .Make("Carapace", crab, 20);
            Transform Claw(string name, float y, int order)
            {
                var t = new GameObject(name).transform; t.SetParent(crab, false); t.localPosition = W(-20, y);
                new Paint().Capsule(0, 0, -14, 4, 6, red).Ellipse(-20, 7, 9, 6, red, 16).Poly(white, -26, 6, -18, 9, -26, 11).Make(name, t, order);
                return t;
            }
            crabClawL = Claw("Pince arrière", 2, 19); crabClawR = Claw("Pince avant", -4, 21);
            crab.gameObject.SetActive(false);

            // goéland géant : corps blanc, tête et bec jaune à gauche, ailes qui battent
            gullBoss = new GameObject("Goéland géant").transform; gullBoss.SetParent(transform, false);
            Transform Wing(string name, int order, Color col)
            {
                var t = new GameObject(name).transform; t.SetParent(gullBoss, false); t.localPosition = W(4, 6);
                new Paint().Poly(col, 0, 0, 16, 0, 40, 34, 26, 38).Poly(Hex("#2A2D36"), 26, 38, 40, 34, 46, 44, 32, 46).Make(name, t, order);
                return t;
            }
            gullWingB = Wing("Aile arrière", 19, Hex("#C9CED6"));
            new Paint().Ellipse(4, 0, 30, 13, white, 28).Poly(Hex("#B9C0CC"), 30, 2, 46, 10, 46, -6)
                .Circle(-24, 6, 10, white, 18).Poly(Hex("#FFB21A"), -32, 6, -48, 2, -32, 1).Circle(-27, 9, 2, ink, 8)
                .Line(-30, 12, -22, 14, 1.6f, ink).Make("Corps", gullBoss, 20);
            gullWingF = Wing("Aile avant", 21, white);
            gullBoss.gameObject.SetActive(false);

            // sanglier : corps brun, groin, défenses, crête de poils, pattes
            boar = new GameObject("Sanglier").transform; boar.SetParent(transform, false);
            var brown = Hex("#6B4226"); var deep = Hex("#3E2414");
            boarLegs = new GameObject("Pattes").transform; boarLegs.SetParent(boar, false);
            new Paint().Rect(-18, -26, 7, 14, deep).Rect(-4, -26, 7, 14, deep).Rect(10, -26, 7, 14, deep).Rect(22, -26, 7, 14, deep).Make("Pattes", boarLegs, 19);
            var bp = new Paint().Ellipse(4, -2, 32, 18, brown, 30).Ellipse(-24, -2, 14, 12, brown, 20).Ellipse(-37, -5, 6, 5, Hex("#A8705A"), 14)
                .Circle(-39, -4, 1.2f, ink, 6).Circle(-36, -4, 1.2f, ink, 6).Circle(-26, 4, 2.2f, ink, 8)
                .Poly(deep, -20, 8, -12, 10, -18, 18)
                .Poly(white, -32, -8, -30, -9, -36, -1).Poly(white, -28, -9, -26, -10, -31, -2);
            for (int i = 0; i < 8; i++) bp.Poly(deep, -12 + i * 6, 14, -6 + i * 6, 14, -9 + i * 6, 22);
            bp.Make("Corps", boar, 20);
            boar.gameObject.SetActive(false);

            // coquillage (petit plot du goéland) et barrière basse (sanglier)
            shellMesh = new Paint().Dome(0.5f, 0, 0.5f, 1, Hex("#F4C7B0"), 14)
                .Line(0.5f, 0, 0.5f, 0.95f, 0.06f, Hex("#C98B70")).Line(0.5f, 0, 0.2f, 0.85f, 0.06f, Hex("#C98B70")).Line(0.5f, 0, 0.8f, 0.85f, 0.06f, Hex("#C98B70")).Build();
            barrierMesh = new Paint().Rect(2, 0, 4, 22, Hex("#2A2D36")).Rect(24, 0, 4, 22, Hex("#2A2D36"))
                .Rect(0, 11, 30, 9, Color.white).Rect(3, 11, 6, 9, Hex("#E63946")).Rect(15, 11, 6, 9, Hex("#E63946")).Rect(27, 11, 3, 9, Hex("#E63946")).Build();
            barriers = new Pool(() =>
            {
                var mr = Renderer("Barrière", transform, 12);
                mr.GetComponent<MeshFilter>().sharedMesh = barrierMesh;
                return mr.transform;
            });
        }

        /// <summary>Ligne d'arrivée (spec 002, FR-006) : portique à damier, poteaux et fanions.</summary>
        void BuildFinish()
        {
            var p = new Paint();
            var pole = Hex("#2A2D36");
            p.Rect(-46, 0, 6, 120, pole).Rect(40, 0, 6, 120, pole);
            for (int i = 0; i < 12; i++)
                for (int j = 0; j < 3; j++)
                    p.Rect(-46 + i * 7.67f, 100 + j * 7, 7.67f, 7, (i + j) % 2 == 0 ? Color.white : Hex("#1B1028"));
            for (int i = 0; i < 5; i++)
            {
                float x = -40 + i * 20;
                p.Poly(i % 2 == 0 ? Hex("#FF4D8D") : Hex("#FFE14D"), x, 98, x + 12, 98, x + 6, 86);
            }
            p.Rect(-48, -2, 96, 3, Color.white);   // ligne au sol
            finish = p.Make("Arrivée", transform, 9).transform;
            finish.gameObject.SetActive(false);
        }

        public void Draw(Run run, float time)
        {
            if (finish == null) { BuildFinish(); BuildBeasts(); }
            barriers.Begin();
            bool showFinish = run.Mode == GameMode.Story && run.Def != null && run.Def.Boss == BossKind.None && run.Phase != Phase.Menu;
            finish.gameObject.SetActive(showFinish);
            if (showFinish) finish.position = W(run.Def.Finish, run.Ground.Floor(run.Def.Finish));
            cones.Begin(); bolts.Begin(); teeth.Begin(); shadows.Begin(); rails.Begin(); gulls.Begin(); pads.Begin();
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
                if (o.Kind == ObstacleKind.Gull)
                {
                    var gt = gulls.Get(o, out _);
                    gt.position = W(o.X, o.Y + Mathf.Sin(time * 5 + o.X * 0.02f) * 3);
                    float flap = Mathf.Sin(time * 14 + o.X) * 40;
                    gt.GetChild(1).localRotation = Quaternion.Euler(0, 0, -20 + flap);
                    gt.GetChild(2).localRotation = Quaternion.Euler(0, 0, -10 + flap);
                    ShadowAt(o, o.X + o.W / 2, g.Floor(o.X + o.W / 2), 24, o.Y - g.Floor(o.X + o.W / 2));
                    continue;
                }
                if (o.Kind == ObstacleKind.Pad)
                {
                    var pt = pads.Get(o, out _);
                    pt.position = W(o.X, g.Height(o.X));
                    pt.localScale = new Vector3(1, 1 + (o.Used ? 0 : Mathf.Sin(time * 8) * 0.08f), 1);
                    continue;
                }
                if (o.Kind == ObstacleKind.Barrier)
                {
                    barriers.Get(o, out _).position = W(o.X, o.Y);
                    ShadowAt(o, o.X + o.W / 2, o.Y, o.W + 6, 0);
                    continue;
                }
                var c = cones.Get(o, out _);
                bool dropped = o.Kind == ObstacleKind.DroppedCone, shell = dropped && o.W < 20;
                c.GetChild(0).GetComponent<MeshFilter>().sharedMesh = shell ? shellMesh : dropped ? bossCone : coneMesh;
                c.GetChild(1).gameObject.SetActive(dropped && !shell);
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
            var kind = boss_?.Kind ?? BossKind.None;
            crab.gameObject.SetActive(kind == BossKind.Crab);
            gullBoss.gameObject.SetActive(kind == BossKind.Gull);
            boar.gameObject.SetActive(kind == BossKind.Boar);
            boss.gameObject.SetActive(kind == BossKind.Purple);
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
                if (kind != BossKind.Purple) hover = kind == BossKind.Gull ? hover * 0.6f : Mathf.Abs(Mathf.Sin(time * 9)) * 2;   // le crabe et le sanglier trottent
                var pos = new Vector2(boss_.X + enter * enter * 320, gy + boss_.Lift + boss_.Height / 2 + (kind == BossKind.Purple ? 6 : 2) + hover);
                float scale = 1;
                if (boss_.State == BossState.Dying)
                {
                    float k = boss_.Timer / 90f;
                    pos += new Vector2(Random.Range(-5f, 5f), Random.Range(-5f, 5f)) * (1 - k);
                    scale = 1 + k * 0.4f;
                    if (k > 0.75f) scale *= (1 - k) * 4;
                }
                var body = kind == BossKind.Crab ? crab : kind == BossKind.Gull ? gullBoss : kind == BossKind.Boar ? boar : boss;
                body.position = W(pos.x, pos.y);
                body.localScale = Vector3.one * scale * (1 + hitPunch * 0.08f);
                bool attacking = boss_.State == BossState.Rising;
                if (kind == BossKind.Crab)
                {
                    float snap = Mathf.Sin(time * (attacking ? 22 : 7)) * 18;
                    crabClawL.localRotation = Quaternion.Euler(0, 0, snap);
                    crabClawR.localRotation = Quaternion.Euler(0, 0, -snap);
                }
                if (kind == BossKind.Gull)
                {
                    float beat = Mathf.Sin(time * (attacking ? 5 : 12)) * (attacking ? 10 : 35);
                    gullWingB.localRotation = Quaternion.Euler(0, 0, 10 + beat);
                    gullWingF.localRotation = Quaternion.Euler(0, 0, beat);
                    gullBoss.rotation = Quaternion.Euler(0, 0, attacking ? 12 : 0);   // tête en bas quand il pique
                }
                if (kind == BossKind.Boar)
                {
                    boarLegs.localPosition = W(0, Mathf.Sin(time * (attacking ? 30 : 10)) * 2);
                    boar.rotation = Quaternion.Euler(0, 0, attacking ? 6 : 0);
                }
                hitPunch = Mathf.MoveTowards(hitPunch, 0, Time.deltaTime * 6);
                float flap = Mathf.Sin(time * (boss_.State == BossState.Ground ? 6 : 16)) * 25;
                bossWingL.localRotation = Quaternion.Euler(0, 0, -flap);
                bossWingR.localRotation = Quaternion.Euler(0, 0, flap);
                var look = new Vector2(run.Skater.X - pos.x, run.Skater.Y + 40 - pos.y).normalized;
                bossPupil.localPosition = W(-18 + look.x * 5, 12 + look.y * 5);
                float lift = pos.y - gy - boss_.Height / 2;
                bossShadow.position = W(pos.x, gy + 2);
                bossShadow.localScale = new Vector3(50, 9, 1) * Mathf.Lerp(1, 0.5f, lift / 160) * (1 - enter);
            }

            cones.End(); bolts.End(); teeth.End(); shadows.End(); rails.End(); gulls.End(); pads.End(); barriers.End();
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
