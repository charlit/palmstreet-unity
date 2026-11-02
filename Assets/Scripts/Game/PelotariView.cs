using PalmStreet.Core;
using UnityEngine;
using static PalmStreet.Game.Paint;

namespace PalmStreet.Game
{
    /// <summary>
    /// LE PELOTARI (spec 005) : joueur de cesta punta, maillot rouge numéroté, pantalon blanc, casque blanc à bande
    /// rouge, chistera (grand panier d'osier recourbé) au bras avant. Il regarde vers le skateur (à gauche).
    /// Poses : élan, lancer (chistera armé haut, puis fouetté devant), rebond (armé bas, puis lancé vers le sol), chute.
    /// </summary>
    public sealed class PelotariView
    {
        const float Thigh = 15, Shin = 15, Torso = 22, Arm = 18, Scale = 1.25f;
        public readonly Transform Root;
        readonly Transform spin, torso, armB, armF, pelota;
        readonly (Transform thigh, Transform shin) legB, legF;

        public PelotariView(Transform parent, int order)
        {
            Color red = Hex("#C8102E"), white = Hex("#F7F4EC"), skin = Hex("#C68B59"), shoe = Hex("#F2EEE6"), osier = Hex("#C99A5B"), osierDark = Hex("#8C6234");
            Root = new GameObject("Le Pelotari").transform;
            Root.SetParent(parent, false);
            spin = new GameObject("Corps").transform;
            spin.SetParent(Root, false);
            spin.localScale = new Vector3(-Scale, Scale, 1);   // tourné vers le skateur
            Color Dark(Color c, float k) => Color.Lerp(Color.black, c, k);

            (Transform, Transform) Leg(string name, int o, float k)
            {
                var th = new GameObject(name + " cuisse").transform; th.SetParent(spin, false);
                var sh = new GameObject(name + " tibia").transform; sh.SetParent(spin, false);
                var pt = new RiderView.Piece();
                pt.Taper(0, 0, 0, -Thigh, 10, 8.4f, Dark(white, k));
                pt.Col.Line(-2.5f, -1, -2, -Thigh + 1, 2.4f, Dark(Hex("#D9D4C8"), k));
                pt.Ink.Make("contour", th, order + o, "Default"); pt.Col.Make("cuisse", th, order + o + 1, "Default");
                var ps = new RiderView.Piece();
                ps.Taper(0, 0, 0, -Shin, 8.4f, 7, Dark(white, k)).Ellipse(2.5f, -Shin - 1.4f, 6.4f, 2.8f, Dark(shoe, k), 14);
                ps.Col.Rect(-4.4f, -Shin + 0.8f, 13, 1.2f, Dark(red, k));   // bande de la chaussure
                ps.Ink.Make("contour", sh, order + o, "Default"); ps.Col.Make("tibia", sh, order + o + 1, "Default");
                return (th, sh);
            }
            Transform ArmPart(string name, int o, float k, bool chistera)
            {
                var a = new GameObject(name).transform; a.SetParent(spin, false);
                var p = new RiderView.Piece();
                p.Taper(0, -3, 0, -Arm, 5.6f, 4.8f, Dark(skin, k)).Capsule(0, 0, 0, -6, 7.5f, Dark(red, k)).Circle(0, -Arm - 1, 3, Dark(Hex("#5A3A22"), k), 12);   // gant de cuir
                if (chistera)
                {
                    // le chistera : gouttière d'osier qui prolonge le bras et se recourbe vers l'avant
                    p.Taper(0, -Arm - 1, 3, -Arm - 20, 7, 6, osier).Taper(3, -Arm - 20, 10, -Arm - 30, 6, 4, osier);
                    for (int i = 1; i < 6; i++) p.Col.Line(-2.4f + i * 0.4f, -Arm - 2 - i * 4, 3.4f + i * 0.6f, -Arm - 3 - i * 4, 0.5f, osierDark);
                    p.Col.Line(-3, -Arm + 1, 3, -Arm - 2, 1, Hex("#5A3A22"));   // lanière au gant
                }
                p.Make(name, a, order + o, "Default");
                return a;
            }

            legB = Leg("Jambe arrière", 0, 0.8f);
            armB = ArmPart("Bras arrière", 2, 0.85f, false);
            torso = new GameObject("Buste").transform; torso.SetParent(spin, false);
            var b = new RiderView.Piece();
            b.Capsule(0, 1, 1.5f, Torso, 16, red).Ellipse(0, 0, 8.6f, 5.6f, white, 14);
            b.Col.Capsule(-4, 3, -2.5f, Torso - 1, 5.5f, Dark(red, 0.8f))
                 .Rect(-8, 2.5f, 16, 2.5f, red)
                 .Rect(-7.4f, 1, 15, 1.6f, Hex("#1B1028"));                               // ceinture
            // numéro 7 sur la poitrine
            b.Col.Line(1, Torso * 0.62f, 5, Torso * 0.62f, 1.2f, white).Line(5, Torso * 0.62f, 2.4f, Torso * 0.3f, 1.2f, white);
            b.Make("Maillot", torso, order + 4, "Default");

            var head = new GameObject("Tête").transform; head.SetParent(torso, false);
            head.localPosition = W(2, Torso + 11);
            var h = new RiderView.Piece();
            h.Capsule(0, -9, 0.5f, -4, 6, Dark(skin, 0.85f)).Circle(0, 0, 7.6f, skin, 20)
             .Poly(skin, 0, -7.6f, 5.4f, -7.4f, 7.6f, -4.5f, 7.8f, -2.4f, 7.2f, 1.5f, 6, 4.4f, 0, 5)
             .Poly(skin, 7, 0.4f, 9.6f, -2, 8.8f, -2.8f, 7.2f, -2.8f)
             .Dome(-0.4f, 1, 8.4f, 8.6f, white, 16);                                          // casque blanc
            h.Col.Rect(-8.8f, 3.4f, 16.8f, 2.4f, red)                                          // bande rouge
                .Line(-8.4f, 1.2f, 8, 1.2f, 0.7f, Hex("#C9CED6"))
                .Ellipse(5, -0.6f, 1.4f, 1.1f, Color.white, 10).Circle(5.6f, -0.7f, 0.8f, Hex("#2A1A10"), 8)
                .Line(3.6f, 0.9f, 6.8f, 0.6f, 0.8f, Hex("#2A1A10"))
                .Line(6.2f, -4.5f, 7.6f, -4.8f, 0.5f, Hex("#7A2E2E"));
            h.Make("Tête", head, order + 6, "Default");

            legF = Leg("Jambe avant", 8, 1);
            armF = ArmPart("Bras avant", 12, 1, true);
            pelota = new GameObject("Pelote en main").transform; pelota.SetParent(armF, false);
            PelotaPaint(false).Make("Pelote", pelota, order + 14, "Default");
            pelota.localPosition = W(6, -Arm - 26);
        }

        /// <summary>Pelote basque : petite balle blanche cousue (rayon 6, boîte 12 × 12), traits de vitesse en option.</summary>
        public static Paint PelotaPaint(bool trail)
        {
            var p = new Paint();
            if (trail) for (int i = 0; i < 3; i++) p.Capsule(8, -3 + i * 3, 20 + i * 4 - (i == 1 ? 0 : 4), -3 + i * 3, 1.4f, new Color(1, 1, 1, 0.55f - i * 0.1f));
            p.Circle(0, 0, 6.8f, Hex("#1B1028"), 18).Circle(0, 0, 6, Hex("#F7F4EC"), 18).Circle(-1.6f, 1.6f, 1.6f, Color.white, 10);
            p.Curve(new Vector2(-4, -4), new Vector2(0, 0), new Vector2(-4, 4), 0.6f, 0.6f, Hex("#B0A898"), 6)
             .Curve(new Vector2(4, -4), new Vector2(0, 0), new Vector2(4, 4), 0.6f, 0.6f, Hex("#B0A898"), 6);
            return p;
        }

        static void Seg(Transform t, Vector2 at, float deg) { t.localPosition = W(at.x, at.y); t.localRotation = Quaternion.Euler(0, 0, deg); }

        static void Leg((Transform thigh, Transform shin) leg, Vector2 hip, float thigh, float knee)
        {
            Seg(leg.thigh, hip, thigh);
            float a = (thigh - 90) * Mathf.Deg2Rad;
            var k = hip + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * Thigh;
            Seg(leg.shin, k, thigh - knee);
        }

        public void Pose(Boss b, Vector2 feet, float time, float scale)
        {
            Root.position = W(feet.x, feet.y);
            Root.localScale = Vector3.one * scale;
            float run = time * 8, s = Mathf.Sin(run);
            float lean = 10, bob = Mathf.Abs(Mathf.Cos(run)) * 1.5f, bodyRot = 0;
            float thB = -20 * s, thF = 20 * s, kB = 30 + 20 * Mathf.Max(0, s), kF = 30 + 20 * Mathf.Max(0, -s);
            float aB = 30 * s, aF = -20 - 30 * s;
            bool holding = true;
            if (b.State == BossState.Rising)
            {
                float k = Mathf.Clamp01((float)b.Timer / Tuning.RugbyWindup), after = b.Timer - Tuning.RugbyWindup;
                bob = 0; thB = -35; thF = 30; kB = 30; kF = 25;
                if (b.Attack == 0)   // lancer : chistera armé haut derrière, puis fouetté devant
                {
                    lean = after < 0 ? -12 * k : 22;
                    aF = after < 0 ? -150 * k : 70;
                    aB = after < 0 ? 60 : -30;
                }
                else                 // rebond : armé bas, puis lancé vers le sol
                {
                    lean = after < 0 ? 20 * k : 35;
                    aF = after < 0 ? -60 * k : 40;
                    aB = 50;
                }
                holding = after < 0;
            }
            if (b.State == BossState.Dying) bodyRot = Mathf.Min(90, b.Timer * 3);
            spin.localRotation = Quaternion.Euler(0, 0, bodyRot);
            var hip = new Vector2(0, Thigh + Shin + 4 + bob);
            Leg(legB, hip + new Vector2(-1.5f, 0), thB, kB);
            Leg(legF, hip + new Vector2(1.5f, 0), thF, kF);
            Seg(torso, hip, -lean);
            float r = -lean * Mathf.Deg2Rad;
            var shoulder = hip + new Vector2(-Mathf.Sin(r), Mathf.Cos(r)) * (Torso - 3);
            Seg(armB, shoulder + new Vector2(-1, 0), aB - lean);
            Seg(armF, shoulder + new Vector2(1, 0), aF - lean);
            pelota.gameObject.SetActive(holding && b.State != BossState.Dying);
        }
    }
}
