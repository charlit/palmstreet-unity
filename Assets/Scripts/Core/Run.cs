using System;
using System.Collections.Generic;

namespace PalmStreet.Core
{
    public enum Phase { Menu, Playing, Over, Cleared }
    public enum GameMode { Story, Endless }
    /// <summary>Les décors : une ville par niveau, dans l'ordre de la carte (spec 002, FR-002).</summary>
    public enum Theme { Anglet, Biarritz, Bidart, Guethary, SaintJeanDeLuz, Hendaye, Bayonne, Arcangues, Espelette, Ascain, Cambo, BiarritzNuit }
    public enum ObstacleKind { SmallCone, BigCone, Rail, BigRail, NoseRail, DroppedCone, Gull, Pad, Barrier, Ball }
    public enum Cue { Ollie, Land, Trick, Bolt, GrindStart, GrindEnd, GrindOut, Bail, Boost, Spell, Spot, SpotAhead, Crash, Theme, FullSpeed, BossIn, BossFly, ConeDrop, Tooth, BossHit, Boom, MegaAir, Finish, Cleared, NewObstacle }

    public sealed class Obstacle
    {
        public ObstacleKind Kind;
        public float X, W, H;   // X : bord gauche
        public float Y;         // hauteur du bas de l'obstacle
        public float Vx, Vy;         // Vx : vitesse propre (ballons)
        public int Bounces = -1;     // chandelle : rebonds restants ; −1 : vole en ligne droite
        public bool Falling, Used;   // Used : tremplin déjà pris
        public bool IsRail => Kind == ObstacleKind.Rail || Kind == ObstacleKind.BigRail || Kind == ObstacleKind.NoseRail;
        public bool IsFlying => Kind == ObstacleKind.Gull || Kind == ObstacleKind.Ball;
        public float Top => Y + H;
        public string Spot;     // nom du spot de la ville (grand rail), ou null
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
        public bool Grounded = true, Ducking, Fakie;   // Fakie : roule à l'envers, la queue devant
        public int TricksLeft = 1;      // figures encore possibles dans ce saut (2 après un tremplin)
        public Obstacle Rail;           // rail en cours de grind
        public int GrindStyle;          // index de Tuning.RailNames ; repart à 0 sur chaque rail
        public int RailUsed;            // variantes déjà faites sur ce rail (un bit par variante)
        public bool Manual, ManualQueued;   // manual en cours ; demandé pour la retombée
        public bool Nose;                   // le manual (en cours ou demandé) est un NOSE MANUAL, sur les roues avant
        public int ManualSteps;
        public int Wobble;              // pas restants de déséquilibre après un BAIL (pour l'affichage)
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
        // pour les défis des villes
        public int TricksDone, FakieTricks, GrindSteps, RailTricks, BoltsTaken, MaxCombo = 1, Spots, Bails;
        bool spotPlaced, boosted;
        int ollieAge = 999;   // pas depuis le dernier ollie parti du sol
        /// <summary>Sorts connus (index de Tuning.SpellNames) et leur recharge restante, en pas.</summary>
        public List<int> Spells = new List<int> { 0, 1, 2, 3 };
        public readonly int[] SpellCooldown = new int[4];
        public GameMode Mode = GameMode.Endless;
        public LevelDef Def;                                  // niveau en cours (histoire)
        public List<int> Tricks = new List<int> { 0, 1, 2, 3, 4, 5, 6, 7 };   // figures connues (spec 002, FR-013)
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
        int pattern, holdSteps, milestone;

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
            StartEndless(new[] { Theme.Anglet, Theme.Biarritz, Theme.Arcangues }, new[] { BossKind.Purple }, new[] { 0, 1, 2, 3, 4, 5, 6, 7 }, Allow.All & ~Allow.Gaps & ~Allow.Gulls & ~Allow.Pads, bossNow);

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
            TricksDone = FakieTricks = GrindSteps = RailTricks = BoltsTaken = Spots = Bails = 0; MaxCombo = 1; spotPlaced = boosted = false; ollieAge = 999;
            Array.Clear(SpellCooldown, 0, SpellCooldown.Length);
            Skater.Y = Ground.Height(0);   // en haut de la rampe de départ, s'il y en a une
            Speed = mode == GameMode.Endless ? EndlessSpeed(0) : 0; speedFactor = StartRamp ? Tuning.StartRampFrom : 1; CurrentSpeed = Speed * speedFactor;
            nextObstacleX = StartRamp ? Math.Max(SpawnAhead, Tuning.StartRampLength + 150) : SpawnAhead;   // rien sur la rampe
            bigRailPending = (allowed & Allow.Rails) != 0;   // FR-008 : le premier obstacle est un grand rail
            pattern = 0; holding = false;
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
                sk.Grounded = false; sk.Ducking = false; sk.Manual = false;   // un ollie depuis un manual garde le combo
                sk.Vy = Tuning.OllieSpeed;
                ollieAge = 0;
                Add(Cue.Ollie);
            }
        }

        /// <summary>Glissé du doigt (dx, dy, y vers le haut) : en l'air, fait la figure de cette direction si elle est connue.</summary>
        public void Swipe(float dx, float dy)
        {
            var sk = Skater;
            if (sk.Rail != null) { RailTrick(dx, dy); return; }
            int k = TrickFor(dx, dy);
            // vers le bas tout de suite après avoir posé le doigt : il se baisse au lieu de sauter (oiseaux, goéland)
            if (Phase == Phase.Playing && Tuning.TrickSwipe[k] == 6 && (sk.Grounded || ollieAge <= Tuning.DuckSwipeSteps && sk.Trick < 0))
            {
                Duck();
                return;
            }
            // vers le bas (manual) ou vers le haut (nose manual), trop près du sol pour finir une figure ou après la
            // figure du saut : il retombera sur deux roues
            int dir = Tuning.TrickSwipe[k];
            if (Phase == Phase.Playing && !sk.Grounded && (dir == 6 || dir == 2) && (sk.TricksLeft <= 0 || StepsToLand() - 2 < 10))
            {
                sk.ManualQueued = true;
                sk.Nose = dir == 2;
                return;
            }
            if (Phase == Phase.Playing && !sk.Grounded && sk.Rail == null && sk.TricksLeft > 0 && sk.Trick < 0 && Tricks.Contains(k)) Trick(k);
        }

        /// <summary>Se baisser : annule l'ollie qui vient de partir ; il reste baissé tant que le doigt est posé.</summary>
        void Duck()
        {
            var sk = Skater;
            if (!sk.Grounded)
            {
                if (Ground.IsGap(sk.X)) return;   // au-dessus d'un trou : on garde le saut
                sk.Y = Ground.Height(sk.X); sk.Vy = 0; sk.Grounded = true; sk.TricksLeft = 1;
            }
            sk.Manual = false;
            sk.Ducking = true;
            holdSteps = Math.Max(holdSteps, Tuning.DuckAfter);
            ollieAge = 999;
        }

        /// <summary>Toucher à 2 doigts : 180, le skateur fait un demi-tour en l'air et passe en fakie (ou en revient).</summary>
        public void HalfTurn()
        {
            if (Phase != Phase.Playing) return;
            var sk = Skater;
            if (sk.Grounded || sk.Rail != null) Press();   // les deux doigts arrivent ensemble : on saute d'abord
            if (sk.Grounded || sk.Rail != null || sk.TricksLeft <= 0 || sk.Trick >= 0) return;
            sk.Fakie = !sk.Fakie;
            Animate(Tuning.HalfTurn);
            TricksDone++;
            Shoot(1);
            AddPoints(Cue.Trick, Tuning.HalfTurnPoints * Combo, sk.Fakie ? "180 FAKIE !" : "180 !");
            Combo++; ComboTimer = 0;
        }

        /// <summary>Glissé pendant un grind : la variante de cette direction, si elle n'est pas déjà faite sur ce rail.</summary>
        void RailTrick(float dx, float dy)
        {
            var sk = Skater;
            int style = RailFor(dx, dy);
            if (Phase != Phase.Playing || style < 1 || (sk.RailUsed & 1 << style) != 0) return;
            sk.GrindStyle = style;
            sk.RailUsed |= 1 << style;
            RailTricks++;
            Shoot(1);
            AddPoints(Cue.Trick, Tuning.RailPoints[sk.GrindStyle] * Combo, Tuning.RailNames[sk.GrindStyle] + " !");
            Combo++; ComboTimer = 0;
        }

        /// <summary>Où en est le défi du niveau (même unité que LevelDef.Goal).</summary>
        public int ChallengeValue(LevelDef d) => d.Challenge switch
        {
            ChallengeKind.Tricks => TricksDone,
            ChallengeKind.Fakie => FakieTricks,
            ChallengeKind.GrindSeconds => GrindSteps / 60,
            ChallengeKind.Combo => Math.Max(MaxCombo, Combo),
            ChallengeKind.Bolts => BoltsTaken,
            _ => RailTricks,
        };

        public bool ChallengeDone(LevelDef d) => ChallengeValue(d) >= d.Goal;

        /// <summary>Variante de grind du glissé : la plus proche des 8 directions.</summary>
        public static int RailFor(float dx, float dy)
        {
            int dir = ((int)Math.Round(Math.Atan2(dy, dx) / (Math.PI / 4)) + 8) % 8;
            return Array.IndexOf(Tuning.RailSwipe, dir);
        }

        /// <summary>Figure du glissé : la plus proche des 8 directions.</summary>
        public static int TrickFor(float dx, float dy)
        {
            int dir = ((int)Math.Round(Math.Atan2(dy, dx) / (Math.PI / 4)) + 8) % 8;
            return Array.IndexOf(Tuning.TrickSwipe, dir);
        }

        public void Release()
        {
            holding = false;
            Skater.Ducking = false;
        }

        void Trick(int k)
        {
            Animate(k);
            bool fakie = Skater.Fakie;
            TricksDone++;
            if (fakie) FakieTricks++;
            Shoot(fakie ? Tuning.FakieBonus : 1);
            AddPoints(Cue.Trick, (int)(Tuning.TrickPoints[k] * Combo * (fakie ? Tuning.FakieBonus : 1)), (fakie ? "FAKIE " : "") + Tuning.TrickNames[k] + " !");
            Combo++; ComboTimer = 0;
        }

        void Animate(int k)
        {
            var sk = Skater;
            sk.Trick = k; sk.TrickStep = 0; sk.TricksLeft--;
            sk.TrickLength = (int)Math.Max(10, Math.Min(24, StepsToLand() - 2));   // trop tard : la figure ne finit pas, BAIL à la retombée
        }

        /// <summary>Pas restants avant de retomber au niveau du sol actuel : y(n) = Y + (Vy − g/2) n − g/2 n².</summary>
        float StepsToLand()
        {
            var sk = Skater;
            float b = sk.Vy - Tuning.Gravity / 2, drop = Math.Max(0, sk.Y - Ground.Height(sk.X));
            return (b + (float)Math.Sqrt(b * b + 4 * (Tuning.Gravity / 2) * drop)) / Tuning.Gravity;
        }

        /// <summary>Retombée (sol ou rail) : une figure pas finie fait perdre le combo ; un manual demandé démarre.</summary>
        void Landed(bool onRail)
        {
            var sk = Skater;
            if (sk.Trick >= 0)
            {
                Combo = 1; Bails++; sk.Wobble = 30;
                sk.ManualQueued = false;
                Add(Cue.Bail, "BAIL !", null, sk.Y + Tuning.SkaterHeight);
                return;
            }
            if (sk.ManualQueued && !onRail)
            {
                sk.Manual = true; sk.ManualSteps = 0;
                TricksDone++;
                Shoot(1);
                AddPoints(Cue.Trick, (sk.Nose ? Tuning.NoseManualPoints : Tuning.ManualPoints) * Combo, sk.Nose ? "NOSE MANUAL !" : "MANUAL !");
                Combo++; ComboTimer = 0;
            }
            sk.ManualQueued = false;
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
            if (Boss != null) Speed *= Tuning.BossSpeedBoost;
            if (StartRamp && sk.X < Tuning.StartRampLength)
            {
                // rampe de départ : la vitesse monte avec la hauteur descendue, même en l'air ;
                // en infini, boost : elle monte de plus en plus vite et bien plus haut
                float down = 1 - Ground.Height(sk.X) / Tuning.StartRampTop;
                bool endless = Mode == GameMode.Endless;
                float to = endless ? Tuning.EndlessRampTo : Tuning.StartRampTo;
                speedFactor = Tuning.StartRampFrom + (to - Tuning.StartRampFrom) * (endless ? down * down : down);
                if (endless && !boosted && down > 0.92f) { boosted = true; Add(Cue.Boost, "BOOST !"); }
            }
            else if (sk.Grounded)
            {
                float target = Ground.SpeedFactor(sk.X);
                // après la rampe, l'élan retombe peu à peu à la vitesse du niveau au lieu de couper net
                bool endlessRun = Mode == GameMode.Endless;
                float runOut = endlessRun ? Tuning.EndlessRunOut : Tuning.StartRunOut, decay = endlessRun ? Tuning.EndlessRampDecay : Tuning.StartRampDecay;
                if (StartRamp && sk.X < Tuning.StartRampLength + runOut) target = Math.Max(target, speedFactor - decay);
                speedFactor = target;
            }
            CurrentSpeed = Speed * speedFactor;
            float prevY = sk.Y;
            sk.X += CurrentSpeed;
            Ground.EnsureUpTo(sk.X + 800);

            MoveSkater(prevY);
            if (Phase != Phase.Playing) return;   // chute pendant le mouvement (bord d'un trou)
            if (sk.Trick >= 0 && ++sk.TrickStep > sk.TrickLength) sk.Trick = -1;
            MaxCombo = Math.Max(MaxCombo, Combo);
            if (sk.Wobble > 0) sk.Wobble--;
            if (ollieAge < 999) ollieAge++;
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
                GrindSteps++;
                if (sk.X - Tuning.SkaterHalfWidth > r.X + r.W)
                {
                    sk.Rail = null;
                    AddPoints(Cue.GrindEnd, Tuning.GrindBonus * Combo, "GRIND!");
                    Combo++;
                    if (r.Spot != null) { Spots++; AddPoints(Cue.Spot, Tuning.SpotPoints * Combo, r.Spot); }
                }
                return;
            }
            if (sk.Grounded)
            {
                if (sk.Manual)   // sur deux roues : le combo tient, 1 point de plus par pas, 2,5 s au plus
                {
                    Score += 1; ComboTimer = 0;
                    if (++sk.ManualSteps >= Tuning.ManualMax) sk.Manual = false;
                }
                if (!Ground.IsGap(sk.X)) { sk.Y = Ground.Height(sk.X); return; }
                sk.Manual = false;
                sk.Grounded = false; sk.Vy = 0;   // FR-030 : la route s'arrête, il tombe
            }

            sk.Vy -= Tuning.Gravity;
            sk.Y += sk.Vy;
            if (sk.Vy <= 0)
                foreach (var o in Obstacles)
                    if (o.IsRail && OverlapsX(o.X, o.X + o.W) && sk.Y <= o.Top && prevY >= o.Top - 8)
                    {
                        Landed(true);
                        sk.Rail = o; sk.Y = o.Top; sk.Vy = 0; sk.TricksLeft = 1; sk.Trick = -1; sk.GrindStyle = 0; sk.RailUsed = 0;
                        Add(Cue.GrindStart);
                        return;
                    }
            float g = Ground.Height(sk.X);
            if (sk.Y <= g)
            {
                if (prevY < g - 2) { Crash(); return; }   // arrivé par le côté : il heurte le bord du trou
                Landed(false);
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
                    BoltsTaken++;
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
            foreach (var o in Obstacles) if (o.IsFlying && o.Bounces < 0 && o.Vx == 0) o.X -= Tuning.GullSpeed;   // FR-031 : les mouettes (et ballons volants) foncent sur le skateur
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
            if (Mode == GameMode.Story && Def.Spot != null && !spotPlaced && x >= Def.Finish * 0.45f && PlaceSpot(x)) return;
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
        /// <summary>Le spot de la ville : un grand rail à son nom, annoncé ; le grinder jusqu'au bout rapporte un bonus.</summary>
        bool PlaceSpot(float x)
        {
            float len = Tuning.RailLength * Tuning.BigRailFactor;
            var at = NextFlat(x, len + 20);
            if (at == null || Ground.GapIn(at.Value - 150, at.Value + len + 150) != null) return false;
            spotPlaced = true;
            var o = new Obstacle { Kind = ObstacleKind.BigRail, X = at.Value, W = len, H = Tuning.RailHeight, Spot = Def.Spot };
            Obstacles.Add(o);
            Add(Cue.SpotAhead, Def.Spot);
            nextObstacleX = o.X + len + 300 + Speed * 40;
            return true;
        }

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
            float flying = Mode == GameMode.Story && Def != null ? Def.FlyingRate : Tuning.FlyingRate;
            if (r < flying && Has(Allow.Gulls))
            {
                float gx = x + 140;   // elle vole vers le skateur : on la fait naître plus loin
                // spec 004 : à Biarritz, des ballons de rugby volent aussi (seulement des ballons au niveau 2)
                bool biarritz = Theme == Theme.Biarritz || Theme == Theme.BiarritzNuit;
                bool ball = biarritz && (Theme == Theme.Biarritz || rng.NextDouble() < 0.5);
                o = new Obstacle { Kind = ball ? ObstacleKind.Ball : ObstacleKind.Gull, X = gx, W = Tuning.GullWidth, H = Tuning.GullBody, Y = Ground.Floor(gx) + Tuning.GullHeight };
            }
            else if (r >= Tuning.FlyingRate && r < 0.45 && Has(Allow.Pads))
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
                if (o.Kind == ObstacleKind.Ball && (o.Vx != 0 || o.Bounces >= 0)) { MoveBall(o); continue; }
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
            if (b.IsRugby) UpdateRugby(b);
            else if (b.Kind != BossKind.Purple) UpdateBeast(b);
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

            // tirs : droit devant, ils touchent le boss s'ils passent dans sa zone (il faut viser la bonne hauteur)
            for (int k = 0; k < SpellCooldown.Length; k++) if (SpellCooldown[k] > 0) SpellCooldown[k]--;
            if (b.State == BossState.Dying) return;
            float floor = Ground.Floor(b.X), x0 = b.X - b.HitWidth / 2, x1 = b.X + b.HitWidth / 2, y0 = floor + b.Lift, y1 = y0 + b.Height;
            for (int i = b.Teeth.Count - 1; i >= 0; i--)
            {
                var t = b.Teeth[i];
                if (t.Spell == 2) { if (--t.Life <= 0) b.Teeth.RemoveAt(i); continue; }   // éclair : dégâts déjà faits, il reste un instant à l'écran
                t.X += CurrentSpeed + t.Vx;
                t.Y += t.Vy;
                if (t.Spell == 0) t.Y = Ground.Floor(t.X) + 8;   // la vague roule au sol
                float r = t.Spell < 0 ? 10 : 18, top = t.Spell == 1 ? 70 : 0;   // la tornade est haute
                bool hit = t.X + r > x0 && t.X - r < x1 && t.Y + top + r > y0 && t.Y - r < y1 && (t.Spell != 0 || b.Lift < 30);
                if (hit)
                {
                    b.Teeth.RemoveAt(i);
                    b.Hp -= t.Damage;
                    Add(Cue.BossHit, t.Spell >= 0 ? Tuning.SpellNames[t.Spell] : null, t.X, t.Y);
                    if (b.Hp <= 0) { Defeat(b); return; }
                    continue;
                }
                if (--t.Life <= 0 || t.X > b.X + 400 || t.Y > floor + 500) b.Teeth.RemoveAt(i);
            }
        }

        /// <summary>Pendant un boss, une figure lance une dent droit devant, à hauteur du skateur.</summary>
        void Shoot(float power)
        {
            var b = Boss;
            if (b == null || b.State == BossState.Arriving || b.State == BossState.Dying) return;
            var sk = Skater;
            b.Teeth.Add(new Tooth { X = sk.X + 14, Y = sk.Y + 20, Vx = Tuning.ToothSpeed, Damage = (int)(Tuning.ToothDamage * power) });
            Add(Cue.Tooth);
        }

        /// <summary>Lance un sort débloqué (bouton pendant le boss) ; faux s'il n'est pas prêt.</summary>
        public bool Cast(int spell)
        {
            var b = Boss;
            if (Phase != Phase.Playing || b == null || b.State == BossState.Arriving || b.State == BossState.Dying) return false;
            if (!Spells.Contains(spell) || SpellCooldown[spell] > 0) return false;
            SpellCooldown[spell] = Tuning.SpellCooldown[spell];
            var sk = Skater;
            int dmg = Tuning.SpellDamage[spell];
            switch (spell)
            {
                case 0: b.Teeth.Add(new Tooth { Spell = 0, X = sk.X + 20, Y = Ground.Floor(sk.X) + 8, Vx = 7, Damage = dmg }); break;            // vague au sol
                case 1: b.Teeth.Add(new Tooth { Spell = 1, X = sk.X + 20, Y = Ground.Floor(sk.X), Vx = 6, Vy = 1.6f, Damage = dmg }); break;     // tornade qui monte
                case 2:   // foudre : frappe le boss où qu'il soit
                    b.Teeth.Add(new Tooth { Spell = 2, X = b.X, Y = Ground.Floor(b.X) + b.Lift + b.Height, Life = 14 });
                    b.Hp -= dmg;
                    Add(Cue.BossHit, Tuning.SpellNames[2], b.X, Ground.Floor(b.X) + b.Lift + b.Height / 2);
                    break;
                default: b.Teeth.Add(new Tooth { Spell = 3, X = sk.X + 16, Y = sk.Y + 26, Vx = 11, Damage = dmg }); break;                       // boule de feu droit devant
            }
            Add(Cue.Spell, Tuning.SpellNames[spell]);
            if (b.Hp <= 0) Defeat(b);
            return true;
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
                    // entre deux attaques, il ne tient pas en place : le goéland plonge et remonte, les autres bondissent
                    if (gull) { float w = (float)Math.Sin(Math.PI * 3 * b.Timer / b.AttackEvery); b.Lift = Boss.FlyHeight - 90 * w * w; }
                    else { int h = b.Timer % Boss.HopEvery; b.Lift = h < Boss.HopSteps ? (float)Math.Sin(Math.PI * h / Boss.HopSteps) * Boss.HopHeight : 0; }
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

        /// <summary>
        /// Boss de rugby (spec 004, FR-002 à FR-005b) : il trottine devant, puis passe (ballon à hauteur de tête : se
        /// baisser), chandelle (ballon qui retombe devant et rebondit vers le skateur : sauter), plaquage (plongeon au
        /// ras du sol : sauter). Chaque attaque commence par RugbyWindup pas de pose (State Rising, Windup).
        /// </summary>
        void UpdateRugby(Boss b)
        {
            int w = Tuning.RugbyWindup;
            switch (b.State)
            {
                case BossState.Arriving:
                    if (b.Timer >= Boss.ArriveSteps) b.Next(BossState.Ground);
                    break;
                case BossState.Ground:
                    b.Lift = 0;
                    b.Offset = Boss.Distance;
                    if (b.Timer >= Tuning.RugbyAttackEvery) { b.Next(BossState.Rising); Add(Cue.BossFly); }
                    break;
                case BossState.Rising:
                    if (b.Timer == w && b.Attack == 0) ThrowBall(b, false);
                    if (b.Timer == w && b.Attack == 1) ThrowBall(b, true);
                    if (b.Attack == 2 && b.Timer >= w)   // plaquage : il fonce au ras du sol et passe sous le skateur
                    {
                        float k = Math.Min(1, (float)(b.Timer - w) / Tuning.RugbyAttackSteps);
                        b.Offset = Boss.Distance + (Boss.BehindSkater - Boss.Distance) * k;
                    }
                    if (b.Attack < 2 && b.Timer >= w + 30 || b.Timer >= w + Tuning.RugbyAttackSteps)
                    {
                        b.Next(b.Attack == 2 ? BossState.Falling : BossState.Ground);
                        if (b.Attack < 2) b.Attack++;
                    }
                    break;
                case BossState.Falling:   // retour devant, d'un bond par-dessus le skateur
                {
                    float k = Math.Min(1, (float)b.Timer / Boss.ReturnSteps);
                    b.Offset = Boss.BehindSkater + (Boss.Distance - Boss.BehindSkater) * k;
                    b.Lift = (float)Math.Sin(Math.PI * k) * 140;
                    if (b.Timer >= Boss.ReturnSteps) { b.Lift = 0; b.Attack = 0; b.Next(BossState.Ground); }
                    break;
                }
            }
            b.X = Skater.X + b.Offset;
        }

        /// <summary>Passe (droite, à hauteur de tête) ou chandelle (en cloche, retombe devant le skateur puis rebondit vers lui).</summary>
        void ThrowBall(Boss b, bool chandelle)
        {
            float floor = Ground.Floor(b.X), v = CurrentSpeed;
            var o = new Obstacle { Kind = ObstacleKind.Ball, W = Tuning.GullWidth, H = Tuning.GullBody, X = b.X - 30 };
            if (chandelle)
            {
                int t = Tuning.ChandelleFlight;
                float y0 = 50;
                o.Y = floor + y0;
                o.Vx = v + (Tuning.ChandelleLand - Boss.Distance + 30) / t;   // retombe ChandelleLand devant le skateur
                o.Vy = (Tuning.BallGravity * t * t / 2 - y0) / t;
                o.Bounces = 3;
            }
            else
            {
                o.Y = floor + Tuning.GullHeight;
                o.Vx = v - Tuning.PassSpeed;
            }
            Obstacles.Add(o);
            Add(Cue.ConeDrop, null, o.X, o.Y);
        }

        /// <summary>Ballon du boss : vol droit, ou en cloche avec rebonds de plus en plus bas vers le skateur.</summary>
        void MoveBall(Obstacle o)
        {
            o.X += o.Vx;
            if (o.Bounces < 0) return;
            o.Vy -= Tuning.BallGravity;
            o.Y += o.Vy;
            float g = Ground.Floor(o.X + o.W / 2);
            if (o.Y > g) return;
            o.Y = g;
            if (o.Bounces == 0) { o.Vy = 0; return; }   // il roule
            o.Bounces--;
            o.Vx = CurrentSpeed - Tuning.ChandelleRoll;   // il revient vers le skateur
            o.Vy = (float)Math.Sqrt(2 * Tuning.BallGravity * Tuning.ChandelleBounce);
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
