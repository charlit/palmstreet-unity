using PalmStreet.Core;
using UnityEngine;
using static PalmStreet.Game.Paint;

namespace PalmStreet.Game
{
    /// <summary>
    /// Un skateur en pantin articulé (FR-024, FR-025) : jambes, buste, tête, bras et planche en formes Unity.
    /// Poses : rouler et pousser, accroupi, ollie, figures (la planche tourne sur elle-même, ou tout le skateur),
    /// grind bras écartés, chute (le skateur tombe en avant, la planche s'échappe en rebondissant).
    /// Unités logiques : pieds en (0, 0), environ 60 de haut.
    /// </summary>
    public sealed class RiderView : MonoBehaviour
    {
        const float Center = 30, Thigh = 14.5f, Shin = 14f, Ankle = 4, DeckY = 6.5f, GripTop = 1.8f;
        const float BackFoot = -11, FrontFoot = 9;   // pieds sur les vis des trucks

        /// <summary>Jambe en deux segments (cuisse, tibia) et une chaussure, placée par cinématique inverse.</summary>
        sealed class LegRig { public Transform Thigh, Shin, Shoe; }

        Transform gripFace, bottomFace;
        LegRig legB, legF;
        Transform spin, board, deck, body, torso, armB, armF;
        Transform[] wheels;
        RiderLook look;

        bool wasAirborne;
        float landedAt = -9;

        // chute
        bool crashed;
        Vector2 bodyPos, bodyVel, boardPos, boardVel;
        float bodyRot, boardRot, boardSpin;

        public static RiderView Create(RiderLook look, Transform parent, int order, string layer = "Default")
        {
            var v = new GameObject("Skateur").AddComponent<RiderView>();
            v.transform.SetParent(parent, false);
            v.Build(look, order, layer);
            return v;
        }

        void Build(RiderLook l, int order, string layer)
        {
            look = l;
            spin = new GameObject("Rotation").transform;
            spin.SetParent(transform, false);

            // planche : vue de profil (plateau, grip, trucks, roues) et vues de dessus et de dessous pour les flips
            board = new GameObject("Planche").transform;
            board.SetParent(spin, false);
            deck = new GameObject("Plateau").transform;
            deck.SetParent(board, false);
            BuildBoard(l, order, layer);

            body = new GameObject("Corps").transform;
            body.SetParent(spin, false);

            legB = MakeLeg("Jambe arrière", order + 3, layer, 0.8f);
            torso = new GameObject("Buste").transform;
            torso.SetParent(body, false);
            var dark = Color.Lerp(l.Top, Color.black, 0.18f);
            new Paint()
                .Capsule(0, 2, 2.5f, 18, 15, l.Top)
                .Rect(-6.5f, 1, 13, 3.5f, dark)
                .Line(-3, 10, 7.5f, 13, 3, l.Accent)
                .Make("Sweat", torso, order + 4, layer);
            var head = Head(l, order + 6, layer);
            head.SetParent(torso, false);
            head.localPosition = W(3.5f, 28);
            legF = MakeLeg("Jambe avant", order + 5, layer, 1);
            armB = Arm("Bras arrière", torso, order + 2, layer, 0.85f);
            armB.localPosition = W(0.5f, 18);
            armF = Arm("Bras avant", torso, order + 7, layer, 1);
            armF.localPosition = W(2.5f, 18);
        }

        void BuildBoard(RiderLook l, int order, string layer)
        {
            var grip = Hex("#1E1E22");
            var ply = Color.Lerp(l.Deck, Color.white, 0.5f);
            var under = Color.Lerp(l.Deck, Color.black, 0.3f);
            var metal = Hex("#C9CED6");
            var metalDark = Hex("#7D8590");

            // profil : plateau plat au milieu, kicktails relevés et arrondis, grip noir dessus, tranche du bois visible
            var p = new Paint();
            void Tails(float y, float w0, float w1, Color col)
            {
                p.Capsule(-18, y, 18, y, w0, col);
                p.Curve(new Vector2(18, y), new Vector2(24, y), new Vector2(28.5f, y + 4.2f), w0, w1, col, 6);
                p.Curve(new Vector2(-18, y), new Vector2(-24, y), new Vector2(-28.5f, y + 4.2f), w0, w1, col, 6);
            }
            Tails(-0.6f, 3.4f, 2.8f, under);
            Tails(-0.2f, 2.6f, 2.1f, l.Deck);
            p.Line(-18, -0.5f, 18, -0.5f, 0.7f, ply);
            Tails(1.1f, 1.2f, 0.9f, grip);
            p.Make("Plateau", deck, order + 1, layer);

            // trucks : semelle, axe (hanger) et kingpin
            var t = new Paint();
            foreach (float cx in new[] { -14f, 14f })
            {
                t.Rect(cx - 4.5f, -3, 9, 1.4f, metalDark);
                t.Poly(metal, cx - 2.5f, -1.8f, cx + 2.5f, -1.8f, cx + 4, -4.6f, cx - 4, -4.6f);
                t.Rect(cx - 1, -2.6f, 2, 1.2f, metalDark);
                t.Rect(cx - 4.6f, -4.4f, 9.2f, 1, Color.Lerp(metal, Color.white, 0.4f));
            }
            t.Make("Trucks", deck, order, layer);

            wheels = new Transform[2];
            for (int i = 0; i < 2; i++)
            {
                var w = new Paint()
                    .Circle(0, 0, 3.6f, Color.Lerp(l.Wheels, Color.black, 0.15f), 16)
                    .Circle(0, 0, 3, l.Wheels, 16)
                    .Circle(0, 0, 1.4f, metalDark, 10)
                    .Rect(-0.5f, 1.2f, 1, 1.6f, Color.Lerp(l.Wheels, Color.black, 0.45f))
                    .Make("Roue", deck, order + 2, layer).transform;
                w.localPosition = W(i == 0 ? -14 : 14, -3.1f);   // roues posées sur le sol
                wheels[i] = w;
            }

            // dessus (grip, vis des trucks) et dessous (graphisme, trucks et roues) : vus quand la planche se retourne
            var top = new Paint().Capsule(-25, 0, 25, 0, 14, l.Deck).Capsule(-24.5f, 0, 24.5f, 0, 12.4f, grip);
            var r = new System.Random(7);
            for (int i = 0; i < 40; i++) top.Circle((float)r.NextDouble() * 46 - 23, (float)r.NextDouble() * 9 - 4.5f, 0.35f, Hex("#4A4A52"), 4);
            foreach (float cx in new[] { -14f, 14f })
                for (int i = 0; i < 4; i++) top.Circle(cx + (i % 2 == 0 ? -1.6f : 1.6f), i < 2 ? -2.4f : 2.4f, 0.7f, metal, 6);
            gripFace = top.Make("Dessus", board, order + 1, layer).transform;

            var bottom = new Paint().Capsule(-25, 0, 25, 0, 14, l.Deck);
            bottom.Capsule(-20, -3.5f, 20, -3.5f, 2.2f, l.Accent).Capsule(-20, 3.5f, 20, 3.5f, 2.2f, l.Accent);
            bottom.Circle(0, 0, 4.6f, l.Wheels, 18).Circle(0, 0, 2.6f, l.Deck, 14);
            foreach (float cx in new[] { -14f, 14f })
            {
                bottom.Rect(cx - 1.6f, -7.5f, 3.2f, 15, metal);
                bottom.Capsule(cx, -8.2f, cx, -6.8f, 3.4f, l.Wheels).Capsule(cx, 6.8f, cx, 8.2f, 3.4f, l.Wheels);
            }
            bottomFace = bottom.Make("Dessous", board, order + 2, layer).transform;
            gripFace.gameObject.SetActive(false);
            bottomFace.gameObject.SetActive(false);
        }

        LegRig MakeLeg(string name, int order, string layer, float tint)
        {
            var l = look;
            Color Tint(Color c) => Color.Lerp(Color.black, c, tint);
            Transform Part(string part, Paint p, int o)
            {
                var t = new GameObject(name + " " + part).transform;
                t.SetParent(body, false);
                p.Make(part, t, o, layer);
                return t;
            }
            var thigh = new Paint().Capsule(0, 0, 0, -Thigh, l.Shorts ? 9.6f : 8.8f, Tint(l.Pants));
            var shin = new Paint();
            if (l.Shorts) shin.Capsule(0, 0, 0, -Shin, 6, Tint(l.Skin)).Rect(-3.3f, -Shin + 0.5f, 6.6f, 3.5f, Tint(l.Accent));   // chaussettes
            else shin.Capsule(0, 0, 0, -Shin + 1, 8, Tint(l.Pants)).Rect(-4.2f, -Shin, 8.4f, 2.2f, Tint(Color.Lerp(l.Pants, Color.black, 0.25f)));
            // chaussure : la cheville est l'origine, la semelle 4 plus bas, la pointe vers l'avant
            var shoe = new Paint()
                .Ellipse(3, -1.4f, 6.8f, 2.9f, Tint(l.Shoes), 16)
                .Rect(-3.8f, -4, 13.6f, 1.8f, Tint(Hex("#F4F1EA")))
                .Line(2, 0.4f, 6.5f, -0.6f, 0.8f, Tint(Color.Lerp(l.Shoes, Color.white, 0.6f)));
            return new LegRig { Thigh = Part("cuisse", thigh, order), Shin = Part("tibia", shin, order), Shoe = Part("chaussure", shoe, order + 1) };
        }

        Transform Arm(string name, Transform parent, int order, string layer, float tint)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            var l = look;
            Color Tint(Color c) => Color.Lerp(Color.black, c, tint);
            new Paint().Capsule(0, 0, 0, -10, 5.8f, Tint(l.Top)).Capsule(0, -10, 0, -15, 4.6f, Tint(l.Skin)).Make(name, t, order, layer);
            return t;
        }

        static Transform Head(RiderLook l, int order, string layer)
        {
            var p = new Paint();
            p.Rect(-2.2f, -9, 4.4f, 5, l.Skin);                       // cou
            p.Circle(0, 0, 7.5f, l.Skin, 20);
            p.Circle(-5.5f, -0.5f, 2, Color.Lerp(l.Skin, Color.black, 0.15f), 10);   // oreille
            switch (l.Head)
            {
                case Headwear.Cap:
                    p.Ellipse(-1, 2, 5, 4, l.Hair, 10);
                    p.Dome(0, 1.5f, 8, 7.5f, l.Hat, 14);
                    p.Capsule(-7, 2.2f, -13, 1.2f, 3, l.Hat);   // visière à l'envers
                    break;
                case Headwear.Beanie:
                    p.Dome(0, 1, 8.2f, 9.5f, l.Hat, 14);
                    p.Rect(-8.4f, 0, 16.8f, 3.4f, Color.Lerp(l.Hat, Color.white, 0.25f));
                    p.Circle(0, 11, 3, Color.Lerp(l.Hat, Color.white, 0.4f), 10);
                    break;
                case Headwear.Curls:
                    for (int i = 0; i < 9; i++)
                    {
                        float a = Mathf.PI * (0.05f + i / 8f * 1.05f);
                        p.Circle(Mathf.Cos(a) * 7, 1.5f + Mathf.Sin(a) * 6.5f, 3.6f, l.Hair, 10);
                    }
                    break;
                default:
                    p.Dome(0, 1, 8, 8.2f, l.Hair, 14);
                    p.Rect(-8, 2, 16, 3, l.Hat);
                    p.Capsule(-8, 3.5f, -12, 0, 2.4f, l.Hat);
                    break;
            }
            p.Circle(4.2f, 0.5f, 1.2f, Hex("#1B1028"), 8);           // œil
            p.Line(3.2f, -3.6f, 6, -3.2f, 1, Color.Lerp(l.Skin, Color.black, 0.4f));
            var t = new GameObject("Tête").transform;
            p.Make("Tête", t, order, layer);
            return t;
        }

        // ---------- poses ----------

        /// <summary>
        /// Pose du skateur. Les semelles sont posées sur le grip (sur les vis des trucks), suivent l'inclinaison de la
        /// planche (tilt, °) et s'en écartent de lift pendant une figure ; les genoux se plient par cinématique inverse.
        /// crouch : 0 debout, 1 accroupi ; lean : buste penché en avant (°) ; bras en ° (0 = le long du corps, 90 = devant) ;
        /// push : phase de la poussée (0 à 1, le pied arrière va pousser sur le sol), négatif sans poussée.
        /// </summary>
        void Pose(float crouch, float lean, float armFront, float armBack, float tilt = 0, float lift = 0, float push = -1, float forward = 0)
        {
            body.localPosition = W(0, -Center);
            var rot = Quaternion.Euler(0, 0, tilt);
            Vector2 Top(float x) => new Vector2(0, DeckY + lift) + (Vector2)(rot * new Vector3(x, GripTop, 0));
            Vector2 back = Top(BackFoot), front = Top(FrontFoot);
            float pushWeight = push >= 0 ? Mathf.Sin(push * Mathf.PI) : 0;
            crouch += pushWeight * 0.5f;
            var hip = new Vector2((back.x + front.x) / 2 - 1 + lean * 0.05f + forward, (back.y + front.y) / 2 + Ankle + 25 * (1 - 0.5f * Mathf.Clamp01(crouch)));
            if (push >= 0) back = Vector2.Lerp(back, new Vector2(-4 - 10 * push, 0), pushWeight);   // le pied arrière pousse sur le sol
            Solve(legB, hip + new Vector2(-1, 0), back, push >= 0 ? tilt * (1 - pushWeight) : tilt);
            Solve(legF, hip + new Vector2(1, 0), front, tilt);
            torso.localPosition = W(hip.x, hip.y - 1);
            torso.localRotation = Quaternion.Euler(0, 0, -lean);
            armF.localRotation = Quaternion.Euler(0, 0, armFront);
            armB.localRotation = Quaternion.Euler(0, 0, armBack);
        }

        static void Solve(LegRig leg, Vector2 hip, Vector2 sole, float tilt)
        {
            var ankle = sole + (Vector2)(Quaternion.Euler(0, 0, tilt) * new Vector3(0, Ankle, 0));
            var d = ankle - hip;
            float reach = Thigh + Shin - 0.05f, dist = Mathf.Clamp(d.magnitude, 0.5f, reach);
            if (d.magnitude > reach) ankle = hip + d.normalized * reach;   // jambe tendue : le pied suit
            float bend = Mathf.Acos(Mathf.Clamp((Thigh * Thigh + dist * dist - Shin * Shin) / (2 * Thigh * dist), -1, 1));
            float a = Mathf.Atan2(d.y, d.x) + bend;   // genou vers l'avant
            var knee = hip + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * Thigh;
            static float Angle(Vector2 v) => Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg + 90;   // les segments sont dessinés vers le bas
            leg.Thigh.localPosition = W(hip.x, hip.y);
            leg.Thigh.localRotation = Quaternion.Euler(0, 0, Angle(knee - hip));
            leg.Shin.localPosition = W(knee.x, knee.y);
            leg.Shin.localRotation = Quaternion.Euler(0, 0, Angle(ankle - knee));
            leg.Shoe.localPosition = W(ankle.x, ankle.y);
            leg.Shoe.localRotation = Quaternion.Euler(0, 0, tilt);
        }

        /// <summary>
        /// Planche : tilt = nez relevé (°), flip = rotation dans sa longueur (rad, kickflip), shove = rotation à plat
        /// (rad, shove-it), drop = la planche descend sous les pieds pendant la figure.
        /// Le flip montre le grip puis le dessous ; le shove-it raccourcit la planche jusqu'à la voir par le bout.
        /// </summary>
        void Board(float tilt, float flip = 0, float shove = 0, float drop = 0)
        {
            board.localPosition = W(0, DeckY - Center - drop);
            float sx = Mathf.Cos(shove), sx2 = Mathf.Sign(sx) * Mathf.Max(Mathf.Abs(sx), 0.24f);
            float cy = Mathf.Cos(flip), sy = Mathf.Sin(flip);
            deck.localRotation = Quaternion.Euler(0, 0, tilt);
            deck.localScale = new Vector3(sx2, cy, 1);
            // shove-it : vu un peu d'en haut, le grip apparaît quand la planche est de travers
            float ss = Mathf.Abs(Mathf.Sin(shove));
            if (ss > Mathf.Abs(sy)) { sy = ss * 0.6f; cy = 1; }
            bool face = Mathf.Abs(sy) > 0.08f;
            gripFace.gameObject.SetActive(face && sy > 0);
            bottomFace.gameObject.SetActive(face && sy < 0);
            if (!face) return;
            var f = sy > 0 ? gripFace : bottomFace;
            f.localPosition = W(0, -1 * cy);
            f.localRotation = Quaternion.Euler(0, 0, tilt);
            f.localScale = new Vector3(shove != 0 ? Mathf.Max(Mathf.Abs(sx), 0.28f) : sx2, Mathf.Abs(sy) * (shove != 0 ? 0.55f : 1), 1);
        }

        void Wheels(float distance)
        {
            foreach (var w in wheels) w.localRotation = Quaternion.Euler(0, 0, -distance * 17);
        }

        /// <summary>Pose en partie, d'après l'état du skateur. feet : position monde des pieds.</summary>
        public void Ride(Skater s, Vector3 feet, float slope, float time, float speedFactor)
        {
            if (crashed) Reset();
            float tilt = s.Grounded || s.Rail != null ? Mathf.Atan(slope) * Mathf.Rad2Deg : 0;
            spin.position = feet + W(0, Center);
            float current = spin.localEulerAngles.z;
            if (current > 180) current -= 360;
            float spinAngle = Mathf.Lerp(current, tilt, 0.35f);
            Wheels(s.X);
            bool airborne = !s.Grounded && s.Rail == null;
            if (wasAirborne && !airborne) landedAt = time;
            wasAirborne = airborne;

            float p = s.Trick >= 0 ? Mathf.Clamp01((float)s.TrickStep / s.TrickLength) : 0;
            float ease = 1 - (1 - p) * (1 - p) * (1 - p);   // rapide au départ, rattrapée en douceur
            float lift = Mathf.Sin(p * Mathf.PI);            // les pieds quittent la planche au milieu de la figure
            if (s.Rail != null)
            {
                // nose grind : le nez sur le rail, la queue levée ; le skateur penché sur l'avant, bras en balancier
                const float nose = -18, up = 4.5f;   // up : la roue avant reste posée sur le rail
                Pose(0.45f, 26, 115, -45, nose, up, -1, 7);
                Board(nose, 0, 0, -up);
            }
            else if (s.Grounded)
            {
                // rouler : petit balancement, et une poussée de temps en temps sur le plat
                float cycle = Mathf.Repeat(time, 2.6f), push = cycle < 0.7f && speedFactor < 1.05f && !s.Ducking ? cycle / 0.7f : -1;
                float squash = Mathf.Max(0, 1 - (time - landedAt) / 0.2f) * 0.6f;   // genoux qui amortissent la retombée
                if (s.Ducking) Pose(1, 32, 55, 15);
                else Pose(0.15f + squash + Mathf.Sin(time * 2.4f) * 0.04f, 10 + Mathf.Max(0, Mathf.Sin(push * Mathf.PI)) * 12, -14 + Mathf.Sin(time * 2.4f) * 6, 22, 0, 0, push);
                Board(0);
            }
            else
            {
                float rise = Mathf.Clamp(s.Vy / Tuning.OllieSpeed, -1, 1);
                Ollie(rise, out float nose, out float up);
                Pose(0.2f + (1 - Mathf.Abs(rise)) * 0.5f, 14, 60 + rise * 40, -70 - rise * 30, nose, up);
                Board(nose, 0, 0, -up);
            }

            switch (s.Trick)
            {
                case 0:   // FLIP : genoux remontés, pieds écartés de la planche qui tourne bien visible dessous, nez un peu levé
                    Pose(0.3f + lift * 0.45f, 12 + lift * 8, 75 + lift * 25, -85 - lift * 20, 0, lift * 10);
                    Board(lift * 14, p * p * (3 - 2 * p) * Mathf.PI * 2, 0, lift * 8);   // tour régulier : on voit chaque face
                    break;
                case 1:   // SHOVE-IT : même saut groupé, la planche fait un tour à plat sous les pieds
                    Pose(0.3f + lift * 0.35f, 6, 95 + lift * 20, -95 - lift * 15, 0, lift * 5);
                    Board(0, 0, p * p * (3 - 2 * p) * Mathf.PI * 2, lift * 2);   // rotation régulière : on la voit tourner
                    break;
                case 2:   // 360° : salto arrière groupé, pieds sur la planche, une main l'attrape
                    Pose(0.95f, 30 + lift * 15, -35, -70);
                    Board(0);
                    spinAngle = ease * 360;
                    break;
                case 3:   // 360° INVERSÉ : salto avant groupé
                    Pose(0.95f, 30 + lift * 15, -20, 60);
                    Board(0);
                    spinAngle = -ease * 360;
                    break;
            }
            spin.rotation = Quaternion.Euler(0, 0, spinAngle);
        }

        /// <summary>
        /// Mouvement de la planche pendant un ollie, selon rise (1 au décollage, 0 au sommet, −1 à la retombée) :
        /// la queue claque, le nez monte fort (pivot sur la queue, d'où up), la planche se remet à plat en montant,
        /// puis pique un peu du nez juste avant de toucher le sol.
        /// </summary>
        static void Ollie(float rise, out float nose, out float up)
        {
            float k = Mathf.Clamp01((rise - 0.1f) / 0.6f);
            nose = rise > 0.1f ? 32 * k * k * (3 - 2 * k) : rise < -0.5f ? -8 * (-0.5f - rise) / 0.5f : 0;
            up = Mathf.Max(0, Mathf.Sin(nose * Mathf.Deg2Rad) * 22);   // la queue reste en bas, le centre monte
        }

        /// <summary>Pose d'accueil : debout sur la planche, qui respire ; le skateur choisi fait des petits ollies.</summary>
        public void Idle(Vector3 feet, float time, bool selected, float scale)
        {
            if (crashed) Reset();
            transform.localScale = Vector3.one * scale;
            // le skateur choisi enchaîne des petits ollies : même mouvement de planche qu'en partie
            float a = Mathf.Repeat(time * 3.2f, Mathf.PI * 2);
            bool air = selected && a < Mathf.PI;
            float hop = air ? Mathf.Sin(a) : 0, nose = 0, up = 0;
            if (air) Ollie(Mathf.Cos(a), out nose, out up);
            spin.localPosition = (feet - transform.position) / scale + W(0, Center + hop * 16);
            spin.localRotation = Quaternion.identity;
            Pose(0.15f + hop * 0.4f, 8, -10 + Mathf.Sin(time * 1.7f) * 5 + hop * 60, 18 - hop * 70, nose, up);
            Board(nose, 0, 0, -up);
        }

        // ---------- chute (FR-009) ----------

        public void Crash(Skater s, float speed)
        {
            crashed = true;
            bodyPos = new Vector2(s.X, s.Y);
            bodyVel = new Vector2(speed * 0.7f, 4.5f);
            bodyRot = 0;
            board.SetParent(transform, true);
            boardPos = new Vector2(s.X, s.Y + 6);
            boardVel = new Vector2(speed + 3.5f, 7.5f);
            boardRot = 0;
            boardSpin = -16;
        }

        public void Fall(Ground g, float dt)
        {
            if (!crashed) return;
            float k = dt * 60;
            bodyVel.y -= 0.5f * k;
            bodyPos += bodyVel * k;
            float gy = g.Height(bodyPos.x);
            if (bodyPos.y <= gy) { bodyPos.y = gy; bodyVel.y = 0; bodyVel.x *= Mathf.Pow(0.86f, k); }
            bodyRot = Mathf.MoveTowards(bodyRot, -95, 7 * k);
            spin.position = W(bodyPos.x, bodyPos.y + 8);
            spin.rotation = Quaternion.Euler(0, 0, bodyRot);
            Pose(0.3f, 20, 150, 120);
            body.localPosition = W(0, -8 - Thigh - Shin);

            boardVel.y -= 0.5f * k;
            boardPos += boardVel * k;
            boardRot += boardSpin * k;
            float by = g.Height(boardPos.x) + 6;
            if (boardPos.y <= by && boardVel.y < 0)
            {
                boardPos.y = by;
                boardVel.y = -boardVel.y * 0.5f;   // la planche rebondit
                boardVel.x *= 0.8f;
                boardSpin *= 0.6f;
                if (boardVel.y < 1) { boardVel.y = 0; boardRot = Mathf.MoveTowards(Mathf.Repeat(boardRot, 360), 180, 30); boardSpin = 0; }
            }
            board.position = W(boardPos.x, boardPos.y);
            board.rotation = Quaternion.Euler(0, 0, boardRot);
            deck.localRotation = Quaternion.identity;
            deck.localScale = Vector3.one;
            gripFace.gameObject.SetActive(false);
            bottomFace.gameObject.SetActive(false);
        }

        public Vector2 BoardPosition => boardPos;

        void Reset()
        {
            crashed = false;
            board.SetParent(spin, false);
            board.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
        }

        public void Show(bool on) => gameObject.SetActive(on);
    }
}
