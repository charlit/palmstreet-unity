using System;
using System.Collections.Generic;

namespace PalmStreet.Core
{
    public enum Phase { Menu, Playing, Over, Cleared }
    public enum GameMode { Story, Endless }
    /// <summary>Les décors : une ville par niveau, dans l'ordre de la carte (spec 002, FR-002).</summary>
    public enum Theme { Anglet, Biarritz, Bidart, Guethary, SaintJeanDeLuz, Hendaye, Bayonne, Arcangues, Espelette, Ascain, Cambo, BiarritzNuit }
    public enum ObstacleKind { SmallCone, BigCone, Rail, BigRail, NoseRail, DroppedCone, Gull, Pad, Barrier }
    public enum Cue { Ollie, Land, Trick, Bolt, GrindStart, GrindEnd, GrindOut, Crash, Theme, FullSpeed, BossIn, BossFly, ConeDrop, Tooth, BossHit, Boom, MegaAir, Finish, Cleared, NewObstacle }

    public sealed class Obstacle
    {
        public ObstacleKind Kind;
        public float X, W, H;   // X : bord gauche
        public float Y;         // hauteur du bas de l'obstacle
        public float Vy;
        public bool Falling, Used;   // Used : tremplin déjà pris
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
        public bool Grounded = true, Ducking;
        public int TricksLeft = 1;      // figures encore possibles dans ce saut (2 après un tremplin)
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
        public int Score, Best, Combo = 1, ComboTimer, StepCount, NextBossScore;
        public GameMode Mode = GameMode.Endless;
        public LevelDef Def;                                  // niveau en cours (histoire)
        public List<int> Tricks = new List<int> { 0, 1, 2, 3 };   // figures connues (spec 002, FR-013)
        public Allow Allowed = Allow.All;                     // obstacles permis
        public List<Theme> Cities = new List<Theme> { Theme.Anglet };
        public List<BossKind> Bosses = new List<BossKind>();
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
        bool bigRailPending, holding, bossDone;
        int pattern, trickIndex, holdSteps, milestone;

        public Run(int seed = 0) { this.seed = seed; }

        /// <summary>Départ en haut d'une rampe (vrai en jeu) ; les tests de règles partent à plat.</summary>
        public bool StartRamp = true;

        /// <summary>Vitesse en histoire : de la vitesse de départ à la maximale du niveau sur sa distance (spec 002).</summary>
        public static float StorySpeed(LevelDef d, float x) => d.StartSpeed + (d.MaxSpeed - d.StartSpeed) * Math.Min(1, Math.Max(0, x) / d.Finish);

        /// <summary>Vitesse en infini : monte avec le temps de jeu, pas avec le score (spec 002, FR-018).</summary>
        public static float EndlessSpeed(int steps) => Math.Min(Tuning.EndlessMax, Tuning.EndlessStart + Tuning.EndlessSlopePerSecond * steps / Tuning.StepsPerSecond);

        Theme CurrentTheme => Mode == GameMode.Story ? Def.Theme : Cities[(Score / Tuning.EndlessThemeEvery) % Cities.Count];

        bool Has(Allow a) => (Allowed & a) != 0;

        /// <summary>Mode histoire : un niveau de la carte, avec les figures connues.</summary>
        public void StartLevel(LevelDef def, IList<int> tricks)
        {
            Begin(GameMode.Story, def.Allowed, tricks);
            Def = def;
            Theme = def.Theme;
            NextBossScore = int.MaxValue;   // le boss d'un niveau vient à son arrivée, pas au score
            if (def.NewObstacle != null) Add(Cue.NewObstacle, def.NewObstacle);
        }

        /// <summary>Mode infini : villes ouvertes en décor, boss déjà rencontrés, obstacles des niveaux ouverts.</summary>
        public void StartEndless(IList<Theme> cities, IList<BossKind> bosses, IList<int> tricks, Allow allowed, bool bossNow = false)
        {
            Begin(GameMode.Endless, allowed, tricks);
            Cities = cities.Count > 0 ? new List<Theme>(cities) : new List<Theme> { Theme.Anglet };
            Bosses = new List<BossKind>(bosses);
            Theme = CurrentTheme;
            NextBossScore = bossNow ? 0 : Tuning.BossAt;
        }

        /// <summary>Partie infinie avec tout débloqué (raccourci du soleil, tests).</summary>
        public void Start(bool bossNow = false) =>
            StartEndless(new[] { Theme.Anglet, Theme.Biarritz, Theme.Arcangues }, new[] { BossKind.Purple }, new[] { 0, 1, 2, 3 }, Allow.All & ~Allow.Gaps & ~Allow.Gulls & ~Allow.Pads, bossNow);

        void Begin(GameMode mode, Allow allowed, IList<int> tricks)
        {
            Mode = mode;
            Allowed = allowed;
            Tricks = new List<int>(tricks);
            Def = null;
            bossDone = false;
            rng = new Random(seed != 0 ? seed : Environment.TickCount);
            Skater = new Skater();
            Ground = new Ground(rng, 300, StartRamp) { Gaps = (allowed & Allow.Gaps) != 0 };   // choix : le départ plat de 420 commence au bord droit de l'écran
            if ((allowed & Allow.Hills) == 0) Ground.FlatUntil = float.PositiveInfinity;   // pas de bosses
            Obstacles.Clear(); Bolts.Clear(); Events.Clear();
            Boss = null;
            Score = 0;
            milestone = 0;
            Combo = 1; ComboTimer = 0; StepCount = 0; NewBest = false;
            Skater.Y = Ground.Height(0);   // en haut de la rampe de départ, s'il y en a une
            Speed = mode == GameMode.Endless ? EndlessSpeed(0) : 0; speedFactor = StartRamp ? Tuning.StartRampFrom : 1; CurrentSpeed = Speed * speedFactor;
            nextObstacleX = StartRamp ? Math.Max(SpawnAhead, Tuning.StartRampLength + 150) : SpawnAhead;   // rien sur la rampe
            bigRailPending = (allowed & Allow.Rails) != 0;   // FR-008 : le premier obstacle est un grand rail
            pattern = 0; trickIndex = 0; holding = false;
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
            }
            else if (sk.Grounded)
            {
                sk.Grounded = false; sk.Ducking = false;
                sk.Vy = Tuning.OllieSpeed;
                Add(Cue.Ollie);
            }
            else if (sk.TricksLeft > 0 && sk.Trick < 0 && Tricks.Count > 0) Trick();   // spec 002 : seulement les figures connues   // FR-010 : un toucher en l'air fait la figure (2 touchers en tout)
        }

        public void Release()
        {
            holding = false;
            Skater.Ducking = false;
        }

        void Trick()
        {
            var sk = Skater;
            int k = Tricks[trickIndex++ % Tricks.Count];
            sk.Trick = k; sk.TrickStep = 0; sk.TricksLeft--;
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

            Speed = Mode == GameMode.Story ? StorySpeed(Def, sk.X) : EndlessSpeed(StepCount);
            if (StartRamp && sk.X < Tuning.StartRampLength)
            {
                // rampe de départ : la vitesse monte avec la hauteur descendue, même en l'air
                float down = 1 - Ground.Height(sk.X) / Tuning.StartRampTop;
                speedFactor = Tuning.StartRampFrom + (Tuning.StartRampTo - Tuning.StartRampFrom) * down;
            }
            else if (sk.Grounded)
            {
                float target = Ground.SpeedFactor(sk.X);
                // après la rampe, l'élan retombe peu à peu à la vitesse du niveau au lieu de couper net
                if (StartRamp && sk.X < Tuning.StartRampLength + Tuning.StartRunOut) target = Math.Max(target, speedFactor - Tuning.StartRampDecay);
                speedFactor = target;
            }
            CurrentSpeed = Speed * speedFactor;
            float prevY = sk.Y;
            sk.X += CurrentSpeed;
            Ground.EnsureUpTo(sk.X + 800);

            MoveSkater(prevY);
            if (Phase != Phase.Playing) return;   // chute pendant le mouvement (bord d'un trou)
            if (sk.Trick >= 0 && ++sk.TrickStep > sk.TrickLength) sk.Trick = -1;
            if (sk.Rail == null && ++ComboTimer >= Tuning.ComboTimeout) Combo = 1;   // FR-014

            FallingCones();
            UpdateBoss();
            if (Phase != Phase.Playing) return;   // boss du niveau battu : niveau réussi
            if (Mode == GameMode.Story && sk.X >= Def.Finish && !bossDone && Boss == null)
            {
                if (Def.Boss == BossKind.None) { Add(Cue.Finish); Clear(); return; }   // ligne d'arrivée
                StartBoss(Def.Boss);
            }
            if (Mode == GameMode.Endless && Boss == null && Score >= NextBossScore)
                StartBoss(Bosses.Count > 0 ? Bosses[rng.Next(Bosses.Count)] : BossKind.Crab);
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
            if (sk.Grounded)
            {
                if (!Ground.IsGap(sk.X)) { sk.Y = Ground.Height(sk.X); return; }
                sk.Grounded = false; sk.Vy = 0;   // FR-030 : la route s'arrête, il tombe
            }

            sk.Vy -= Tuning.Gravity;
            sk.Y += sk.Vy;
            if (sk.Vy <= 0)
                foreach (var o in Obstacles)
                    if (o.IsRail && OverlapsX(o.X, o.X + o.W) && sk.Y <= o.Top && prevY >= o.Top - 8)
                    {
                        sk.Rail = o; sk.Y = o.Top; sk.Vy = 0; sk.TricksLeft = 1; sk.Trick = -1;
                        Add(Cue.GrindStart);
                        return;
                    }
            float g = Ground.Height(sk.X);
            if (sk.Y <= g)
            {
                if (prevY < g - 2) { Crash(); return; }   // arrivé par le côté : il heurte le bord du trou
                sk.Y = g; sk.Vy = 0; sk.Grounded = true; sk.TricksLeft = 1; sk.Trick = -1;
                if (holding && holdSteps >= Tuning.DuckAfter) sk.Ducking = true;
                Add(Cue.Land);
            }
        }

        bool OverlapsX(float x0, float x1) => Skater.X + Tuning.SkaterHalfWidth > x0 && Skater.X - Tuning.SkaterHalfWidth < x1;

        void Collide()
        {
            var sk = Skater;
            if (sk.Y < Tuning.FallCrash) { Crash(); return; }   // tombé dans un trou
            float top = sk.Y + sk.Height;
            if (Boss != null && Boss.State != BossState.Dying && Boss.State != BossState.Arriving)
            {
                var (bw, bh) = Boss.Body;
                float by = Ground.Floor(Boss.X) + Boss.Lift;
                if (bw > 0 && OverlapsX(Boss.X - bw / 2, Boss.X + bw / 2) && sk.Y < by + bh && top > by) { Crash(); return; }
            }
            foreach (var o in Obstacles)
            {
                if (o == sk.Rail) continue;
                if (o.Kind == ObstacleKind.Pad)
                {
                    // FR-032 : tremplin néon, saut géant et deux figures possibles
                    if (!o.Used && sk.Grounded && OverlapsX(o.X, o.X + o.W))
                    {
                        o.Used = true;
                        sk.Grounded = false; sk.Ducking = false;
                        sk.Vy = Tuning.PadJump;
                        sk.TricksLeft = 2;
                        AddPoints(Cue.MegaAir, Tuning.MegaAirPoints * Combo, "MÉGA AIR !");
                    }
                    continue;
                }
                float x0 = o.X, x1 = o.X + o.W, y1 = o.Top;
                if (!o.IsRail) { x0 += o.W * 0.2f; x1 -= o.W * 0.2f; y1 = o.Y + o.H * 0.85f; }   // un plot est pointu
                if (OverlapsX(x0, x1) && sk.Y < y1 && top > o.Y) { Crash(); return; }
            }
        }

        void Clear()
        {
            Phase = Phase.Cleared;
            Add(Cue.Cleared);
            if (Score > Best) { Best = Score; NewBest = true; }
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
            if (Mode == GameMode.Endless && m > milestone)
            {
                milestone = m;
                bigRailPending = Has(Allow.Rails);   // FR-008 (s'il y a des rails dans ce mode)
                Add(Cue.FullSpeed, "FULL SPEED");   // FR-016
            }
            var t = CurrentTheme;
            if (t != Theme) { Theme = t; Add(Cue.Theme); }
        }

        void Cleanup()
        {
            float behind = Skater.X - 300;
            Obstacles.RemoveAll(o => o.X + o.W < behind);
            foreach (var o in Obstacles) if (o.Kind == ObstacleKind.Gull) o.X -= Tuning.GullSpeed;   // FR-031 : les mouettes foncent sur le skateur
            Bolts.RemoveAll(b => b.X < behind);
            Ground.DropBefore(Skater.X - 600);
        }

        // ---------- obstacles et éclairs (FR-007, FR-008, FR-013) ----------

        void Spawn()
        {
            if (Boss != null) return;   // FR-017 : plus d'obstacles normaux pendant le boss
            float limit = Mode == GameMode.Story ? Def.Finish - 300 : float.MaxValue;   // la ligne d'arrivée reste dégagée
            while (nextObstacleX < Skater.X + SpawnAhead && nextObstacleX < limit) PlaceObstacle();
        }

        void PlaceObstacle()
        {
            float x = nextObstacleX;
            if (Ground.InDescentStart(x, out float after)) x = after;
            // jamais d'obstacle juste avant, sur ou juste après un trou : on le pose bien après
            for (var hole = Ground.GapIn(x - 150, x + 150); hole != null; hole = Ground.GapIn(x - 150, x + 150)) x = hole.Value.X1 + 160;   // plus loin que la marge de 150, sinon on retrouve le même trou
            if ((Has(Allow.Gulls) || Has(Allow.Pads)) && !bigRailPending && PlaceNeon(x)) return;
            bool rails = Has(Allow.Rails) && (Mode == GameMode.Story || Score >= Tuning.RailsAfterScore);
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
                bool big = Has(Allow.BigCones) && rng.NextDouble() < 0.4;
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

        /// <summary>Niveau 2 : mouette (FR-031) ou tremplin (FR-032), sinon false pour un obstacle normal.</summary>
        bool PlaceNeon(float x)
        {
            double r = rng.NextDouble();
            Obstacle o;
            if (r < 0.28 && Has(Allow.Gulls))
            {
                float gx = x + 140;   // elle vole vers le skateur : on la fait naître plus loin
                o = new Obstacle { Kind = ObstacleKind.Gull, X = gx, W = Tuning.GullWidth, H = Tuning.GullBody, Y = Ground.Floor(gx) + Tuning.GullHeight };
            }
            else if (r >= 0.28 && r < 0.45 && Has(Allow.Pads))
            {
                // le tremplin se met juste avant le prochain trou s'il y en a un : saut géant par-dessus
                var gap = Ground.GapIn(x, x + 400);
                float px = gap != null ? gap.Value.X0 - 70 : x;
                if (!Ground.IsFlat(px - 5, px + Tuning.PadWidth + 5)) return false;
                o = new Obstacle { Kind = ObstacleKind.Pad, X = px, W = Tuning.PadWidth, H = Tuning.PadHeight };
                if (gap != null) { Obstacles.Add(o); nextObstacleX = gap.Value.X1 + 160 + Speed * 40; return true; }
            }
            else return false;
            Obstacles.Add(o);
            nextObstacleX = o.X + o.W + Range(220, 380) + Speed * 40;
            return true;
        }

        void PlaceBolts(float from, float to)
        {
            int n = rng.Next(Tuning.BoltRowMin, Tuning.BoltRowMax + 1);
            const float spacing = 24;
            float width = (n - 1) * spacing;
            if (to - from < width) return;
            Ground.EnsureUpTo(to);
            var hole = Ground.GapIn(from, to);
            if (hole != null)
            {
                // un arc d'éclairs au-dessus du trou
                var h = hole.Value;
                float cx = (h.X0 + h.X1) / 2;
                for (int i = 0; i < n; i++)
                {
                    float k = n == 1 ? 0.5f : (float)i / (n - 1);
                    Bolts.Add(new Bolt { X = cx - width / 2 + i * spacing, Y = 45 + 45 * (float)Math.Sin(Math.PI * k) });
                }
                return;
            }
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
                Bolts.Add(new Bolt { X = bx, Y = Ground.Floor(bx) + lift });
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

        void StartBoss(BossKind kind)
        {
            Boss = new Boss(kind);
            Ground.FlatUntil = float.PositiveInfinity;
            Ground.TrimAfter(Skater.X + SpawnAhead);
            Add(Cue.BossIn, "BOSS !");
        }

        void UpdateBoss()
        {
            var b = Boss;
            if (b == null) return;
            b.Timer++;
            b.X = Skater.X + b.Offset;
            if (b.State == BossState.Dying)
            {
                if (b.Timer >= 90) { Boss = null; if (bossDone) Clear(); }
                return;
            }
            if (b.Kind != BossKind.Purple) UpdateBeast(b);
            else switch (b.State)
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
                    if (b.Timer >= 90) { Boss = null; if (bossDone) Clear(); }
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
            if (b.State == BossState.Dying) return;
            float tx = b.X, ty = Ground.Floor(b.X) + b.Lift + b.Height / 2;
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

        /// <summary>Crabe, Goéland, Sanglier (spec 002, FR-011) : attaques au ras du sol ou à hauteur de tête.</summary>
        void UpdateBeast(Boss b)
        {
            bool gull = b.Kind == BossKind.Gull;
            switch (b.State)
            {
                case BossState.Arriving:
                    if (gull) b.Lift = Boss.FlyHeight;
                    if (b.Timer >= Boss.ArriveSteps) b.Next(BossState.Ground);
                    break;
                case BossState.Ground:
                    if (gull && (b.Timer == 100 || b.Timer == 160 || b.Timer == 220)) DropThing(b, ObstacleKind.DroppedCone, 18, 16);   // coquillages
                    if (b.Kind == BossKind.Boar && (b.Timer == 120 || b.Timer == 220)) DropThing(b, ObstacleKind.Barrier, 30, 22);   // barrières
                    if (b.Timer >= b.AttackEvery) { b.Next(BossState.Rising); Add(Cue.BossFly); }
                    break;
                case BossState.Rising:   // l'attaque : il fonce et passe sous (ou sur) le skateur
                {
                    float k = Math.Min(1, (float)b.Timer / b.AttackSteps);
                    b.Offset = Boss.Distance + (Boss.BehindSkater - Boss.Distance) * k;
                    if (gull) b.Lift = Boss.FlyHeight + (Boss.GullDive - Boss.FlyHeight) * Math.Min(1, k * 1.6f);
                    if (b.Timer >= b.AttackSteps) b.Next(BossState.Falling);
                    break;
                }
                case BossState.Falling:   // retour devant
                {
                    float k = Math.Min(1, (float)b.Timer / Boss.ReturnSteps);
                    b.Offset = Boss.BehindSkater + (Boss.Distance - Boss.BehindSkater) * k;
                    if (gull) b.Lift = Boss.GullDive + (Boss.FlyHeight - Boss.GullDive) * k;
                    else b.Lift = (float)Math.Sin(Math.PI * k) * 140;   // repart devant d'un grand bond par-dessus le skateur
                    if (b.Timer >= Boss.ReturnSteps) b.Next(BossState.Ground);
                    break;
                }
            }
            b.X = Skater.X + b.Offset;
        }

        void DropThing(Boss b, ObstacleKind kind, float w, float h)
        {
            var o = new Obstacle { Kind = kind, W = w, H = h, X = b.X - w / 2 };
            o.Y = Ground.Floor(b.X) + b.Lift;
            o.Falling = o.Y > Ground.Floor(b.X);
            Obstacles.Add(o);
            Add(Cue.ConeDrop, null, b.X, o.Y);
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
            AddPoints(Cue.Boom, Tuning.BossBonus, "BOOM", b.X, Ground.Floor(b.X) + b.Lift + b.Height / 2);
            if (Mode == GameMode.Story) { bossDone = true; return; }   // le niveau est réussi quand l'explosion finit
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
