using System;
using System.Collections.Generic;

namespace PalmStreet.Core
{
    public enum Phase { Menu, Playing, Over }
    public enum Rider { Freestill, Keketsk8, Guigui, Maxwell }
    public enum Theme { Largente, Btz, Arcangues }
    public enum ObstacleKind { SmallCone, BigCone, Rail, BigRail, NoseRail, DroppedCone }
    public enum Cue { Ollie, Land, Trick, Bolt, GrindStart, GrindEnd, GrindOut, Crash, Theme, FullSpeed, BossIn, BossFly, ConeDrop, Tooth, BossHit, Boom }

    public sealed class Obstacle
    {
        public ObstacleKind Kind;
        public float X, W, H;   // X : bord gauche
        public float Y;         // hauteur du bas de l'obstacle
        public float Vy;
        public bool Falling;
        public bool IsRail => Kind == ObstacleKind.Rail || Kind == ObstacleKind.BigRail || Kind == ObstacleKind.NoseRail;
        public float Top => Y + H;
    }

    public sealed class Bolt { public float X, Y; }

    public struct GameEvent
    {
        public Cue Cue;
        public int Points;
        public float X, Y;
        public string Text;
    }

    public sealed class Skater
    {
        public float X, Y, Vy;          // Y : hauteur des pieds
        public bool Grounded = true, Ducking, TrickUsed;
        public Obstacle Rail;           // rail en cours de grind
        public int Trick = -1, TrickStep, TrickLength = 24;   // TrickLength : durée de l'animation, finie avant la retombée
        public float Height => Ducking ? Tuning.DuckHeight : Tuning.SkaterHeight;
    }

    /// <summary>
    /// Une partie de PalmStreet : toutes les règles de la spec, sans Unity. Avancer d'un pas de 1/60 s avec Step(),
    /// les touchers arrivent par Press() et Release(). Les événements (sons, textes, particules) s'accumulent
    /// dans Events ; l'affichage les vide après lecture.
    /// </summary>
    public sealed class Run
    {
        public const float SpawnAhead = 330;   // les obstacles naissent juste après le bord droit de l'écran

        public Phase Phase = Phase.Menu;
        public Rider Rider;
        public int Score, Best, Combo = 1, ComboTimer, StepCount, NextBossScore;
        public bool NewBest;
        public float Speed, CurrentSpeed;
        public Theme Theme;
        public Ground Ground;
        public Skater Skater = new Skater();
        public Boss Boss;
        public readonly List<Obstacle> Obstacles = new List<Obstacle>();
        public readonly List<Bolt> Bolts = new List<Bolt>();
        public readonly List<GameEvent> Events = new List<GameEvent>();

        readonly int seed;
        Random rng;
        float nextObstacleX, speedFactor = 1;
        bool bigRailPending, holding, lastPressInAir;
        int pattern, trickIndex, holdSteps, lastPressStep, milestone;

        public Run(int seed = 0) { this.seed = seed; }

        public static float SpeedFor(int score) => Math.Min(Tuning.MaxSpeed, Tuning.StartSpeed + Tuning.SpeedPer100Points * (score / 100));

        public static Theme ThemeFor(int score)
        {
            int s = score % Tuning.ThemeCycle;
            return s < 5000 ? Theme.Largente : s < 10000 ? Theme.Btz : Theme.Arcangues;
        }

        /// <summary>Nouvelle partie. bossNow : raccourci du soleil (FR-020), le boss arrive tout de suite.</summary>
        public void Start(Rider rider, bool bossNow = false)
        {
            rng = new Random(seed != 0 ? seed : Environment.TickCount);
            Rider = rider;
            Skater = new Skater();
            Ground = new Ground(rng, 300);   // choix : le départ plat de 420 commence au bord droit de l'écran
            Obstacles.Clear(); Bolts.Clear(); Events.Clear();
            Boss = null;
            Score = bossNow ? Tuning.BossAt : 0;
            NextBossScore = Tuning.BossAt;
            milestone = Score / Tuning.MilestoneEvery;
            Theme = ThemeFor(Score);
            Combo = 1; ComboTimer = 0; StepCount = 0; NewBest = false;
            Speed = SpeedFor(Score); CurrentSpeed = Speed; speedFactor = 1;
            nextObstacleX = SpawnAhead;
            bigRailPending = true;   // FR-008 : le premier obstacle est un grand rail
            pattern = 0; trickIndex = 0; holding = false; lastPressInAir = false; lastPressStep = -999;
            Phase = Phase.Playing;
        }

        public void BackToMenu() => Phase = Phase.Menu;

        // ---------- touchers ----------

        public void Press()
        {
            if (Phase != Phase.Playing) return;
            holding = true; holdSteps = 0;
            var sk = Skater;
            if (sk.Rail != null)
            {
                // FR-012 : sortie de grind
                sk.Rail = null;
                sk.Vy = Tuning.OllieSpeed * (float)Math.Sqrt(Tuning.OutJump);
                AddPoints(Cue.GrindOut, Tuning.OutBonus, "OUT!");
                Combo++; ComboTimer = 0;
                lastPressInAir = false;
            }
            else if (sk.Grounded)
            {
                sk.Grounded = false; sk.Ducking = false;
                sk.Vy = Tuning.OllieSpeed;
                Add(Cue.Ollie);
                lastPressInAir = false;
            }
            else if (lastPressInAir && StepCount - lastPressStep <= Tuning.DoubleTapWindow && !sk.TrickUsed)
            {
                Trick();
                lastPressInAir = false;
            }
            else lastPressInAir = true;
            lastPressStep = StepCount;
        }

        public void Release()
        {
            holding = false;
            Skater.Ducking = false;
        }

        void Trick()
        {
            var sk = Skater;
            int k = trickIndex++ % Tuning.TrickPoints.Length;
            sk.Trick = k; sk.TrickStep = 0; sk.TrickUsed = true;
            // pas restants avant de retomber au niveau du sol actuel : y(n) = Y + (Vy - 0,25) n - 0,25 n²
            float b = sk.Vy - Tuning.Gravity / 2, drop = Math.Max(0, sk.Y - Ground.Height(sk.X));
            float n = (b + (float)Math.Sqrt(b * b + 4 * (Tuning.Gravity / 2) * drop)) / Tuning.Gravity;
            sk.TrickLength = (int)Math.Max(10, Math.Min(24, n - 2));
            AddPoints(Cue.Trick, Tuning.TrickPoints[k] * Combo, Tuning.TrickNames[k] + " !");
            Combo++; ComboTimer = 0;
        }

        // ---------- un pas de simulation ----------

        public void Step()
        {
            if (Phase != Phase.Playing) return;
            StepCount++;
            Score += 1;   // FR-004
            var sk = Skater;
            if (holding && ++holdSteps >= Tuning.DuckAfter && sk.Grounded) sk.Ducking = true;

            Speed = SpeedFor(Score);
            if (sk.Grounded) speedFactor = Ground.SpeedFactor(sk.X);
            CurrentSpeed = Speed * speedFactor;
            float prevY = sk.Y;
            sk.X += CurrentSpeed;
            Ground.EnsureUpTo(sk.X + 800);

            MoveSkater(prevY);
            if (sk.Trick >= 0 && ++sk.TrickStep > sk.TrickLength) sk.Trick = -1;
            if (sk.Rail == null && ++ComboTimer >= Tuning.ComboTimeout) Combo = 1;   // FR-014

            FallingCones();
            UpdateBoss();
            if (Boss == null && Score >= NextBossScore) StartBoss();
            Spawn();
            Collect();
            Collide();
            Milestones();
            Cleanup();
        }

        void MoveSkater(float prevY)
        {
            var sk = Skater;
            if (sk.Rail != null)
            {
                var r = sk.Rail;
                sk.Y = r.Top; sk.Vy = 0;
                Score += Tuning.GrindPerStep;   // FR-011
                ComboTimer = 0;
                if (sk.X - Tuning.SkaterHalfWidth > r.X + r.W)
                {
                    sk.Rail = null;
                    AddPoints(Cue.GrindEnd, Tuning.GrindBonus * Combo, "GRIND!");
                    Combo++;
                }
                return;
            }
            if (sk.Grounded) { sk.Y = Ground.Height(sk.X); return; }

            sk.Vy -= Tuning.Gravity;
            sk.Y += sk.Vy;
            if (sk.Vy <= 0)
                foreach (var o in Obstacles)
                    if (o.IsRail && OverlapsX(o.X, o.X + o.W) && sk.Y <= o.Top && prevY >= o.Top - 8)
                    {
                        sk.Rail = o; sk.Y = o.Top; sk.Vy = 0; sk.TrickUsed = false; sk.Trick = -1;
                        Add(Cue.GrindStart);
                        return;
                    }
            float g = Ground.Height(sk.X);
            if (sk.Y <= g)
            {
                sk.Y = g; sk.Vy = 0; sk.Grounded = true; sk.TrickUsed = false; sk.Trick = -1;
                if (holding && holdSteps >= Tuning.DuckAfter) sk.Ducking = true;
                Add(Cue.Land);
            }
        }

        bool OverlapsX(float x0, float x1) => Skater.X + Tuning.SkaterHalfWidth > x0 && Skater.X - Tuning.SkaterHalfWidth < x1;

        void Collide()
        {
            var sk = Skater;
            float top = sk.Y + sk.Height;
            foreach (var o in Obstacles)
            {
                if (o == sk.Rail) continue;
                float x0 = o.X, x1 = o.X + o.W, y1 = o.Top;
                if (!o.IsRail) { x0 += o.W * 0.2f; x1 -= o.W * 0.2f; y1 = o.Y + o.H * 0.85f; }   // un plot est pointu
                if (OverlapsX(x0, x1) && sk.Y < y1 && top > o.Y) { Crash(); return; }
            }
        }

        void Crash()
        {
            Phase = Phase.Over;
            Skater.Rail = null;
            Add(Cue.Crash);
            if (Score > Best) { Best = Score; NewBest = true; }
        }

        void Collect()
        {
            var sk = Skater;
            for (int i = Bolts.Count - 1; i >= 0; i--)
            {
                var b = Bolts[i];
                if (b.X + 10 > sk.X - Tuning.SkaterHalfWidth && b.X - 10 < sk.X + Tuning.SkaterHalfWidth && b.Y + 10 > sk.Y && b.Y - 10 < sk.Y + sk.Height)
                {
                    Bolts.RemoveAt(i);
                    AddPoints(Cue.Bolt, Tuning.BoltPoints * Combo, null, b.X, b.Y);   // FR-013
                }
            }
        }

        void Milestones()
        {
            int m = Score / Tuning.MilestoneEvery;
            if (m > milestone)
            {
                milestone = m;
                bigRailPending = true;   // FR-008
                Add(Cue.FullSpeed, "FULL SPEED");   // FR-016
            }
            var t = ThemeFor(Score);
            if (t != Theme) { Theme = t; Add(Cue.Theme); }
        }

        void Cleanup()
        {
            float behind = Skater.X - 300;
            Obstacles.RemoveAll(o => o.X + o.W < behind);
            Bolts.RemoveAll(b => b.X < behind);
            Ground.DropBefore(Skater.X - 600);
        }

        // ---------- obstacles et éclairs (FR-007, FR-008, FR-013) ----------

        void Spawn()
        {
            if (Boss != null) return;   // FR-017 : plus d'obstacles normaux pendant le boss
            while (nextObstacleX < Skater.X + SpawnAhead) PlaceObstacle();
        }

        void PlaceObstacle()
        {
            float x = nextObstacleX;
            if (Ground.InDescentStart(x, out float after)) x = after;
            bool rails = Score >= Tuning.RailsAfterScore;
            bool wantRail = bigRailPending || (rails && pattern % 3 == 2);
            if (rails) pattern++;
            Obstacle o = null;
            if (wantRail)
            {
                float len = Tuning.RailLength * (bigRailPending ? Tuning.BigRailFactor : 1);
                if (!bigRailPending && !Ground.IsFlat(x - 10, x + len + 10)) x = NextFlat(x, len + 20) ?? x;   // un rail normal attend le plat suivant
                if (Ground.IsFlat(x - 10, x + len + 10))
                    o = new Obstacle { Kind = bigRailPending ? ObstacleKind.BigRail : ObstacleKind.Rail, X = x, W = len, H = Tuning.RailHeight };
                bigRailPending = false;
            }
            if (o == null)
            {
                bool big = rails && rng.NextDouble() < 0.4;
                o = big ? new Obstacle { Kind = ObstacleKind.BigCone, X = x, W = 30, H = 42 } : new Obstacle { Kind = ObstacleKind.SmallCone, X = x, W = 22, H = 28 };
                o.Y = Ground.Height(x + o.W / 2);
            }
            Obstacles.Add(o);
            float end = o.X + o.W, gap = Range(200, 400) + Speed * 40;
            PlaceBolts(end + 50, end + gap - 50);
            nextObstacleX = end + gap;
        }

        /// <summary>Début (+10) du prochain plat assez long dans les 600 unités, ou null.</summary>
        float? NextFlat(float x, float len)
        {
            Ground.EnsureUpTo(x + 600 + len);
            foreach (var s in Ground.Segments)
                if (s.Kind == SlopeKind.Flat && s.X1 - Math.Max(s.X0, x) >= len && s.X0 < x + 600) return Math.Max(s.X0 + 10, x);
            return null;
        }

        void PlaceBolts(float from, float to)
        {
            int n = rng.Next(Tuning.BoltRowMin, Tuning.BoltRowMax + 1);
            const float spacing = 24;
            float width = (n - 1) * spacing;
            if (to - from < width) return;
            Ground.EnsureUpTo(to);
            // toujours sur une montée ou un début de descente s'il y en a une dans l'intervalle, sinon 38 % du temps
            float? onSlope = null;
            foreach (var s in Ground.Segments)
            {
                float a = s.X0, b = s.Kind == SlopeKind.Down ? s.X0 + s.Length / 2 : s.X1;
                if (s.Kind == SlopeKind.Flat || b < from || a > to) continue;
                onSlope = Math.Max(from, Math.Min(to - width, (a + b) / 2 - width / 2));
                break;
            }
            if (onSlope == null && rng.NextDouble() >= Tuning.BoltChance) return;
            float x0 = onSlope ?? from + (float)rng.NextDouble() * (to - from - width);
            float lift = Tuning.BoltHeights[rng.Next(Tuning.BoltHeights.Length)];
            for (int i = 0; i < n; i++)
            {
                float bx = x0 + i * spacing;
                Bolts.Add(new Bolt { X = bx, Y = Ground.Height(bx) + lift });
            }
        }

        void FallingCones()
        {
            foreach (var o in Obstacles)
            {
                if (!o.Falling) continue;
                o.Vy -= Tuning.Gravity;
                o.Y += o.Vy;
                float g = Ground.Height(o.X + o.W / 2);   // se pose sur le sol à cet endroit, même en pente
                if (o.Y <= g) { o.Y = g; o.Vy = 0; o.Falling = false; }
            }
        }

        // ---------- boss (FR-017 à FR-019) ----------

        void StartBoss()
        {
            Boss = new Boss();
            Ground.FlatUntil = float.PositiveInfinity;
            Ground.TrimAfter(Skater.X + SpawnAhead);
            Add(Cue.BossIn, "BOSS !");
        }

        void UpdateBoss()
        {
            var b = Boss;
            if (b == null) return;
            b.Timer++;
            b.X = Skater.X + Boss.Distance;
            switch (b.State)
            {
                case BossState.Arriving:
                    if (b.Timer >= Boss.ArriveSteps) b.Next(BossState.Ground);
                    break;
                case BossState.Ground:
                    if (b.Timer >= Tuning.BossFlyEvery) { b.Next(BossState.Rising); Add(Cue.BossFly); }
                    break;
                case BossState.Rising:
                    b.Lift = Boss.FlyHeight * b.Timer / Boss.MoveSteps;
                    if (b.Timer >= Boss.MoveSteps) { b.Next(BossState.Air); b.Drops = 0; }
                    break;
                case BossState.Air:
                    b.Lift = Boss.FlyHeight;
                    if (b.Drops < Tuning.BossDrops && b.Timer >= (b.Drops + 1) * Boss.DropEvery) DropCone(b);
                    else if (b.Drops == Tuning.BossDrops && b.Timer >= (b.Drops + 1) * Boss.DropEvery) b.Next(BossState.Falling);
                    break;
                case BossState.Falling:
                    b.Lift = Boss.FlyHeight * (1 - (float)b.Timer / Boss.MoveSteps);
                    if (b.Timer >= Boss.MoveSteps) { b.Lift = 0; b.Next(BossState.Ground); }
                    break;
                case BossState.Dying:
                    if (b.Timer >= 90) Boss = null;
                    return;
            }
            if (b.State == BossState.Arriving) return;

            // FR-018 : une dent toutes les 0,37 s, droit sur le boss
            if (++b.ToothTimer >= Tuning.ToothEvery)
            {
                b.ToothTimer = 0;
                b.Teeth.Add(new Tooth { X = Skater.X + 14, Y = Skater.Y + 40 });
                Add(Cue.Tooth);
            }
            float tx = b.X, ty = Ground.Height(b.X) + b.Lift + Boss.Size / 2;
            for (int i = b.Teeth.Count - 1; i >= 0; i--)
            {
                var t = b.Teeth[i];
                float dx = tx - t.X, dy = ty - t.Y, d = (float)Math.Sqrt(dx * dx + dy * dy);
                if (d < 30)
                {
                    b.Teeth.RemoveAt(i);
                    b.Hp -= Tuning.ToothDamage;
                    Add(Cue.BossHit, null, t.X, t.Y);
                    if (b.Hp <= 0) { Defeat(b); return; }
                    continue;
                }
                t.X += CurrentSpeed + dx / d * 9;
                t.Y += dy / d * 9;
            }
        }

        void DropCone(Boss b)
        {
            b.Drops++;
            var o = new Obstacle { Kind = ObstacleKind.DroppedCone, W = 26, H = 34, Falling = true };
            o.X = b.X - o.W / 2;
            o.Y = Ground.Height(b.X) + b.Lift;
            Obstacles.Add(o);
            Add(Cue.ConeDrop, null, b.X, o.Y);
        }

        void Defeat(Boss b)
        {
            b.Next(BossState.Dying);
            b.Hp = 0;
            b.Teeth.Clear();
            AddPoints(Cue.Boom, Tuning.BossBonus, "BOOM", b.X, Ground.Height(b.X) + b.Lift + Boss.Size / 2);
            // FR-019 : long plat avec une barre de nose grind, puis reprise
            float x = Skater.X;
            Ground.FlatUntil = x + Tuning.AfterBossFlat;
            Obstacles.Add(new Obstacle { Kind = ObstacleKind.NoseRail, X = x + 400, W = Tuning.RailLength * Tuning.NoseRailFactor, H = Tuning.RailHeight });
            nextObstacleX = x + Tuning.AfterBossFlat;
            NextBossScore = Score + Tuning.BossAt;
        }

        // ---------- utilitaires ----------

        float Range(float a, float b) => a + (float)rng.NextDouble() * (b - a);

        void Add(Cue cue, string text = null, float? x = null, float? y = null) =>
            Events.Add(new GameEvent { Cue = cue, Text = text, X = x ?? Skater.X, Y = y ?? Skater.Y });

        void AddPoints(Cue cue, int points, string text, float? x = null, float? y = null)
        {
            Score += points;
            Events.Add(new GameEvent { Cue = cue, Points = points, Text = text, X = x ?? Skater.X, Y = y ?? Skater.Y + Tuning.SkaterHeight });
        }
    }
}
