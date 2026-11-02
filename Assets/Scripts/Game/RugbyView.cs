using PalmStreet.Core;
using UnityEngine;
using static PalmStreet.Game.Paint;

namespace PalmStreet.Game
{
    /// <summary>
    /// Boss de rugby (spec 004) : un joueur en pantin articulé, dessiné comme le skateur (contour d'encre sous les
    /// couleurs). LE PILIER : rouge à cerceau blanc, bandeau ; LE TALONNEUR : bleu ciel, crâne rasé et barbe.
    /// Il regarde vers la gauche (vers le skateur). Poses : course, passe, chandelle, plaquage, retour en bond, chute.
    /// </summary>
    public sealed class RugbyView
    {
        const float Thigh = 15, Shin = 15, Torso = 22, Arm = 18, Scale = 1.25f;
        public readonly Transform Root;
        readonly Transform spin, torso, head, ball;
        readonly (Transform thigh, Transform shin) legB, legF;
        readonly Transform armB, armF;

        public RugbyView(Transform parent, bool talonneur, int order)
        {
            var jersey = talonneur ? Hex("#5DADE2") : Hex("#D7263D");
            var white = Hex("#F7F4EC"); var skin = Hex("#C68B59"); var boot = Hex("#1B1028"); var hair = Hex("#3B2412");
            Root = new GameObject(talonneur ? "Le Talonneur" : "Le Pilier").transform;
            Root.SetParent(parent, false);
            spin = new GameObject("Corps").transform;
            spin.SetParent(Root, false);
            spin.localScale = new Vector3(-Scale, Scale, 1);   // tourné vers le skateur
            Color Dark(Color c, float k) => Color.Lerp(Color.black, c, k);

            // ordres : jambe et bras arrière 0-3, buste 4-5, tête 6-7, jambe avant 8-11, bras avant 12-13
            (Transform, Transform) Leg(string name, int o, float k)
            {
                var th = new GameObject(name + " cuisse").transform; th.SetParent(spin, false);
                var sh = new GameObject(name + " tibia").transform; sh.SetParent(spin, false);
                var pt = new RiderView.Piece();
                pt.Taper(0, 0, 0, -Thigh, 9.5f, 7.5f, Dark(skin, k)).Taper(0, 0.5f, 0, -Thigh * 0.55f, 11, 10, Dark(white, k));   // short blanc
                pt.Col.Line(-5, -Thigh * 0.55f + 0.5f, 5, -Thigh * 0.55f + 0.5f, 0.8f, Dark(Hex("#C9CED6"), k));
                pt.Ink.Make("contour", th, order + o, "Default"); pt.Col.Make("cuisse", th, order + o + 1, "Default");
                var ps = new RiderView.Piece();
                ps.Taper(0, 0, 0, -Shin, 7.5f, 6, Dark(skin, k))
                  .Rect(-3.6f, -Shin + 1, 7.2f, 7.5f, Dark(jersey, k))                                               // chaussettes
                  .Ellipse(2.5f, -Shin - 1.5f, 6.5f, 2.8f, Dark(boot, k), 14);                                       // crampons
                ps.Col.Rect(-3.6f, -Shin + 3.2f, 7.2f, 1.2f, Dark(white, k)).Rect(-3.6f, -Shin + 5.4f, 7.2f, 1.2f, Dark(white, k));
                for (int i = 0; i < 3; i++) ps.Col.Rect(-1.5f + i * 3, -Shin - 5, 1.2f, 1.4f, Dark(boot, k));
                ps.Ink.Make("contour", sh, order + o, "Default"); ps.Col.Make("tibia", sh, order + o + 1, "Default");
                return (th, sh);
            }
            Transform ArmPart(string name, int o, float k)
            {
                var a = new GameObject(name).transform; a.SetParent(spin, false);
                var p = new RiderView.Piece();
                p.Taper(0, -3, 0, -Arm, 5.6f, 4.8f, Dark(skin, k)).Capsule(0, 0, 0, -6, 7.5f, Dark(jersey, k)).Circle(0, -Arm - 1, 3, Dark(skin, k), 12);
                p.Col.Line(-3.6f, -6.5f, 3.6f, -6.5f, 1.2f, Dark(white, k));   // poignet de la manche
                p.Make(name, a, order + o, "Default");
                return a;
            }

            legB = Leg("Jambe arrière", 0, 0.8f);
            armB = ArmPart("Bras arrière", 2, 0.85f);
            torso = new GameObject("Buste").transform; torso.SetParent(spin, false);
            var b = new RiderView.Piece();
            b.Capsule(0, 1, 1.5f, Torso, 17, jersey).Ellipse(0, 0, 9, 6, white, 14);
            b.Col.Capsule(-4, 3, -2.5f, Torso - 1, 6, Dark(jersey, 0.8f))                                       // ombre du dos
                 .Rect(-8.4f, Torso * 0.55f, 17.4f, 3.4f, white)                                                // cerceau
                 .Capsule(-1, Torso + 6, 5, Torso + 5.6f, 3, white)                                            // col
                 .Rect(-8.4f, 2.5f, 17, 2.5f, jersey);                                                          // le maillot sur le short
            b.Make("Maillot", torso, order + 4, "Default");

            head = new GameObject("Tête").transform; head.SetParent(torso, false);
            head.localPosition = W(2, Torso + 11);
            var h = new RiderView.Piece();
            h.Capsule(0, -9, 0.5f, -4, 6, Dark(skin, 0.85f)).Circle(0, 0, 7.6f, skin, 20)
             .Poly(skin, 0, -7.6f, 5.4f, -7.4f, 7.6f, -4.5f, 7.8f, -2.4f, 7.2f, 1.5f, 6, 4.4f, 0, 5)
             .Poly(skin, 7, 0.4f, 9.6f, -2, 8.8f, -2.8f, 7.2f, -2.8f);                                          // nez
            if (talonneur)
            {
                h.Col.Dome(0, 0.5f, 7.6f, 7.2f, Color.Lerp(skin, hair, 0.35f), 16);                              // crâne rasé
                h.Poly(hair, -2, -2.5f, 3, -4.2f, 7.6f, -4.4f, 6.8f, -8.6f, 2.6f, -10, -1.6f, -7);              // barbe
            }
            else
            {
                h.Ellipse(-1.4f, 3, 7.2f, 5.6f, hair, 18, 0.2f).Ellipse(-5.4f, -0.6f, 3, 4.4f, hair, 12);
                h.Rect(-8, 2.4f, 15.6f, 2.8f, white);                                                           // bandeau blanc
                h.Col.Line(-8, 2.6f, 7.6f, 2.6f, 0.6f, jersey);
                h.Capsule(-8, 3.6f, -12, 1, 2.2f, white);
            }
            h.Col.Ellipse(-1.8f, -0.6f, 1.8f, 2.4f, Dark(skin, 0.8f), 12)                                       // oreille
                .Ellipse(5, 0.6f, 1.4f, 1.1f, Color.white, 10).Circle(5.6f, 0.5f, 0.8f, Hex("#2A1A10"), 8)       // œil
                .Line(3.6f, 2.8f, 6.8f, 2.4f, 0.9f, hair)                                                        // sourcil froncé
                .Line(6.2f, -4.3f, 7.6f, -4.6f, 0.5f, Hex("#7A2E2E"));
            h.Make("Tête", head, order + 6, "Default");

            legF = Leg("Jambe avant", 8, 1);
            armF = ArmPart("Bras avant", 12, 1);
            ball = new GameObject("Ballon").transform; ball.SetParent(spin, false);
            BallPaint().Make("Ballon", ball, order + 14, "Default");
        }

        /// <summary>Ballon de rugby blanc à bandes rouges, centré, 30 × 16 (même boîte que la mouette).</summary>
        public static Paint BallPaint()
        {
            var red = Hex("#D7263D");
            return new Paint()
                .Ellipse(0, 0, 15.8f, 8.8f, Hex("#1B1028"), 24)
                .Ellipse(0, 0, 15, 8, Hex("#F7F4EC"), 24)
                .Ellipse(-6.5f, 0, 1.6f, 7.4f, red, 12).Ellipse(6.5f, 0, 1.6f, 7.4f, red, 12)
                .Line(-3, 2.8f, 3, 2.8f, 0.6f, Hex("#1B1028"))
                .Line(-2, 1.6f, -2, 4, 0.5f, Hex("#1B1028")).Line(0, 1.6f, 0, 4, 0.5f, Hex("#1B1028")).Line(2, 1.6f, 2, 4, 0.5f, Hex("#1B1028"));
        }

        static void Seg(Transform t, Vector2 at, float deg) { t.localPosition = W(at.x, at.y); t.localRotation = Quaternion.Euler(0, 0, deg); }

        /// <summary>Jambe : angle de la cuisse (0 = vers le bas, positif = vers l'avant) et pli du genou.</summary>
        static void Leg((Transform thigh, Transform shin) leg, Vector2 hip, float thigh, float knee)
        {
            Seg(leg.thigh, hip, thigh);
            float a = (thigh - 90) * Mathf.Deg2Rad;
            var k = hip + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * Thigh;
            Seg(leg.shin, k, thigh - knee);
        }

        /// <summary>
        /// Pose selon l'état du boss. feet : position monde des pieds (unités logiques).
        /// </summary>
        public void Pose(Boss b, Vector2 feet, float time, float scale)
        {
            Root.position = W(feet.x, feet.y);
            Root.localScale = Vector3.one * scale;
            float run = time * 11, s = Mathf.Sin(run);
            float lean = 14, bodyRot = 0, bob = Mathf.Abs(Mathf.Cos(run)) * 2;
            float thB = -30 * s, thF = 30 * s, kB = 40 + 30 * Mathf.Max(0, s), kF = 40 + 30 * Mathf.Max(0, -s);
            float aB = 40 * s, aF = -40 * s;
            bool holding = true;
            int windup = Tuning.RugbyWindup;
            if (b.State == BossState.Rising)
            {
                float k = Mathf.Clamp01((float)b.Timer / windup), after = b.Timer - windup;
                switch (b.Attack)
                {
                    case 0:   // passe : bras armés en arrière, puis lancés devant
                        bob = 0; thB = -20; thF = 25; kB = 20; kF = 15;
                        lean = after < 0 ? -10 * k : 18;
                        aB = aF = after < 0 ? -70 * k : 95;
                        holding = after < 0;
                        break;
                    case 1:   // chandelle : jambe armée en arrière, puis coup de pied vers le ciel
                        bob = 0; lean = -8; thB = 10; kB = 10;
                        thF = after < 0 ? -40 * k : 110; kF = after < 0 ? 90 * k : 0;
                        aB = 60; aF = after < 0 ? 40 : -50;
                        holding = after < 0;
                        break;
                    default:  // plaquage : buste penché, puis plongeon à l'horizontale
                        if (after < 0) { lean = 35 * k; bob = 0; thB = -35; thF = 20; kB = 60; kF = 70; aB = aF = 70 * k; }
                        else { bodyRot = 80; lean = 0; thB = thF = -10; kB = kF = 5; aB = aF = 170; bob = 0; }
                        holding = false;
                        break;
                }
            }
            else if (b.State == BossState.Falling) { thB = thF = 60; kB = kF = 100; aB = aF = 150; lean = 10; holding = false; }   // bond par-dessus
            if (b.State == BossState.Dying) bodyRot = Mathf.Min(90, b.Timer * 3);

            spin.localRotation = Quaternion.Euler(0, 0, bodyRot);   // spin est retourné : un angle positif le couche vers le skateur
            spin.localPosition = W(0, bodyRot > 45 ? 6 : 0);
            var hip = new Vector2(0, Thigh + Shin + 4 + bob);
            Leg(legB, hip + new Vector2(-1.5f, 0), thB, kB);
            Leg(legF, hip + new Vector2(1.5f, 0), thF, kF);
            Seg(torso, hip, -lean);
            float r = -lean * Mathf.Deg2Rad;
            var shoulder = hip + new Vector2(-Mathf.Sin(r), Mathf.Cos(r)) * (Torso - 3);
            Seg(armB, shoulder + new Vector2(-1, 0), aB - lean);
            Seg(armF, shoulder + new Vector2(1, 0), aF - lean);
            ball.gameObject.SetActive(holding && b.State != BossState.Dying);
            if (holding)
            {
                float a = (aF - lean - 90) * Mathf.Deg2Rad;
                var hand = shoulder + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * (Arm + 2);
                ball.localPosition = W(hand.x + 2, hand.y);
                ball.localRotation = Quaternion.Euler(0, 0, 20);
                ball.localScale = Vector3.one * 0.7f;
            }
        }
    }
}
