namespace PalmStreet.Core
{
    /// <summary>
    /// Toutes les valeurs chiffrées de la spec (specs/001-portage-palmstreet/spec.md).
    /// Temps en pas de simulation de 1/60 s, distances et hauteurs en unités logiques (écran de 390 × 620).
    /// Les valeurs marquées « choix » ne sont pas dans la spec : elles sont choisies pour le jeu.
    /// </summary>
    public static class Tuning
    {
        public const int StepsPerSecond = 60;

        // FR-003 vitesse
        public const float StartSpeed = 1.7f, SpeedPer100Points = 0.045f, MaxSpeed = 4.3f;

        // FR-005 terrain
        public const float StartFlat = 420, FlatMin = 240, FlatMax = 500;
        public const float RampMin = 130, RampMax = 240, HillMin = 45, HillMax = 100;
        public const float DownhillBoost = 0.55f, UphillDrag = 0.22f;
        // départ : une rampe à descendre, de 45 % à 155 % de la vitesse, puis retour en douceur à 100 %
        public const float StartRampTop = 110, StartRampLength = 360, StartRampFrom = 0.45f, StartRampTo = 1.55f;
        public const float StartRampDecay = 0.004f, StartRunOut = 700;
        // mode infini : la rampe donne un boost (accélération qui s'emballe, ×2,3 en bas) qui retombe lentement
        public const float EndlessRampTo = 2.3f, EndlessRampDecay = 0.0015f, EndlessRunOut = 1600;

        // FR-006 saut et accroupi
        public const float OllieSpeed = 9.6f, Gravity = 0.5f;
        public const int DuckAfter = 13;              // 0,22 s
        public const int DuckSwipeSteps = 9;          // glissé vers le bas dans les 0,15 s après l'ollie : il se baisse à la place

        // FR-007 / FR-008 obstacles
        public const int RailsAfterScore = 2000;
        public const float RailLength = 100, RailHeight = 30;          // choix
        public const float BigRailFactor = 3, NoseRailFactor = 15;
        public const int BigRailEvery = 5000;

        // FR-010 à FR-014 points
        public static readonly int[] TrickPoints = { 30, 35, 45, 45, 35, 35, 50, 60 };
        public static readonly string[] TrickNames = { "FLIP", "SHOVE-IT", "360°", "360° INVERSÉ", "HEELFLIP", "SHOVE-IT INVERSÉ", "VARIAL FLIP", "360 FLIP" };
        // geste de chaque figure : glissé du doigt dans une des 8 directions (0 = droite, puis tous les 45° vers le haut)
        public static readonly int[] TrickSwipe = { 1, 0, 2, 6, 3, 4, 7, 5 };
        public static readonly string[] SwipeWords = { "à droite", "en haut à droite", "vers le haut", "en haut à gauche", "à gauche", "en bas à gauche", "vers le bas", "en bas à droite" };
        // variantes de grind : glissé pendant le grind, dans une des 8 directions (chacune une fois par rail) ;
        // 0 = le nose grind de départ
        public static readonly string[] RailNames = { "NOSE GRIND", "50-50", "BOARDSLIDE", "CROOKED", "5-0", "LIPSLIDE", "TAILSLIDE", "FEEBLE", "SMITH" };
        public static readonly int[] RailPoints = { 0, 30, 40, 40, 30, 45, 45, 50, 50 };
        public static readonly int[] RailSwipe = { -1, 0, 2, 6, 1, 3, 4, 5, 7 };   // direction de chaque variante (comme TrickSwipe)
        // manual : glissé vers le bas juste avant de retomber (ou après la figure du saut) ; roule sur deux roues
        public const int ManualMax = 150, ManualPoints = 15, NoseManualPoints = 20;   // 2,5 s au plus ; points au départ, puis 1 de plus par pas
        public const int SpotPoints = 150;                     // spot d'une ville grindé jusqu'au bout
        public const int HalfTurn = 8, HalfTurnPoints = 25;   // le 180 (toucher à 2 doigts) : pas une figure à débloquer
        public const float FakieBonus = 1.5f;         // figures faites en fakie
        public const float SwipeMin = 0.05f;          // longueur d'un glissé, en part de la hauteur de l'écran
        public const int GrindPerStep = 2, GrindBonus = 20, OutBonus = 15;
        public const float OutJump = 0.85f;           // part de la hauteur d'un ollie
        public const int BoltPoints = 10, BoltRowMin = 3, BoltRowMax = 5;
        public static readonly float[] BoltHeights = { 30, 75, 120 };   // choix : 3 hauteurs au-dessus du sol
        public const float BoltChance = 0.38f;
        public const int ComboTimeout = 132;          // 2,2 s

        // FR-015 / FR-016 décors
        public const int ThemeCycle = 20000, MilestoneEvery = 5000;

        // FR-017 à FR-019 boss
        public const int BossAt = 15000, BossHp = 8000, ToothDamage = 250, BossBonus = 5000;   // ToothDamage : dent lancée par une figure
        public const float ToothSpeed = 9;
        public const int BossFlyEvery = 180;          // 3 s au sol entre deux envols
        // sorts débloqués en battant les boss (crabe, goéland, sanglier, violet) ; temps de recharge en pas
        public static readonly string[] SpellNames = { "VAGUE", "TORNADE", "FOUDRE", "BOULE DE FEU" };
        public static readonly int[] SpellDamage = { 800, 700, 1000, 900 };
        public static readonly int[] SpellCooldown = { 360, 420, 720, 480 };
        public const int BossDrops = 4;
        public const float AfterBossFlat = 2500;

        // Niveau 2 « NUIT NÉON » après le premier boss (FR-029 à FR-032, choix)
        public const float NeonMaxSpeed = 5.0f;
        public const float GapMin = 50, GapMax = 90, GapChance = 0.4f, FallCrash = -45;
        public const float GullHeight = 46, GullWidth = 30, GullBody = 16, GullSpeed = 1.6f;
        public const float FlyingRate = 0.28f;          // part des obstacles volants (mouettes, ballons)

        // boss de rugby (spec 004) : annonce, passe, chandelle
        public const int RugbyWindup = 36;              // 0,6 s de pose avant chaque attaque (SC-002 : ≥ 0,5 s)
        public const int RugbyAttackEvery = 150, RugbyAttackSteps = 60;
        public const float PassSpeed = 4;               // vitesse du ballon vers le skateur (en plus du défilement)
        public const int ChandelleFlight = 60;          // pas de vol de la chandelle
        public const float ChandelleLand = 140, ChandelleBounce = 26, ChandelleRoll = 3, BallGravity = 0.25f;

        // LE PELOTARI et le mode « planche en main » (spec 005)
        public const int PelotaAttackEvery = 130;
        public const float PelotaThrowSpeed = 5, PelotaFlySpeed = 3.2f, PelotaBounce = 22, PelotaSize = 12;
        public const int ParryWindow = 9;               // 0,15 s : bouclier levé juste avant l'impact = parade (clarification 3)
        public const int TapSteps = 10;                 // à pied : doigt posé moins de 0,17 s = saut
        public const int ParryDamage = 700, BoomerangDamage = 450;
        public const float BoomerangSpeed = 6;          // en plus du défilement, à l'aller comme au retour
        public const int BoomerangOut = 40;
        public const float PadWidth = 40, PadHeight = 6, PadJump = 13.5f;
        public const int MegaAirPoints = 50;

        // Mode infini (spec 002, FR-018, FR-019)
        /// <summary>Tout le jeu va 35 % plus vite qu'au départ ; les niveaux s'allongent d'autant (même durée).</summary>
        public const float SpeedScale = 1.35f;
        public const float BossSpeedBoost = 1.2f;   // pendant un boss, il roule encore plus vite
        public const float EndlessStart = 1.7f * SpeedScale, EndlessSlopePerSecond = 0.012f * SpeedScale, EndlessMax = 5.5f * SpeedScale;
        public const int EndlessThemeEvery = 4000;

        // Skateur (choix) : position à l'écran et boîte de collision
        public const float SkaterScreenX = 90, SkaterHalfWidth = 12, SkaterHeight = 60, DuckHeight = 40;
    }
}
