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

        // FR-006 saut et accroupi
        public const float OllieSpeed = 9.6f, Gravity = 0.5f;
        public const int DuckAfter = 13;              // 0,22 s

        // FR-007 / FR-008 obstacles
        public const int RailsAfterScore = 2000;
        public const float RailLength = 100, RailHeight = 30;          // choix
        public const float BigRailFactor = 3, NoseRailFactor = 15;
        public const int BigRailEvery = 5000;

        // FR-010 à FR-014 points
        public static readonly int[] TrickPoints = { 30, 35, 45, 45 };
        public static readonly string[] TrickNames = { "FLIP", "SHOVE-IT", "360°", "360° INVERSÉ" };
        public const int GrindPerStep = 2, GrindBonus = 20, OutBonus = 15;
        public const float OutJump = 0.85f;           // part de la hauteur d'un ollie
        public const int BoltPoints = 10, BoltRowMin = 3, BoltRowMax = 5;
        public static readonly float[] BoltHeights = { 30, 75, 120 };   // choix : 3 hauteurs au-dessus du sol
        public const float BoltChance = 0.38f;
        public const int ComboTimeout = 132;          // 2,2 s

        // FR-015 / FR-016 décors
        public const int ThemeCycle = 20000, MilestoneEvery = 5000;

        // FR-017 à FR-019 boss
        public const int BossAt = 15000, BossHp = 8000, ToothDamage = 100, BossBonus = 5000;
        public const int ToothEvery = 22;             // 0,37 s
        public const int BossFlyEvery = 600;          // 10 s
        public const int BossDrops = 4;
        public const float AfterBossFlat = 2500;

        // Niveau 2 « NUIT NÉON » après le premier boss (FR-029 à FR-032, choix)
        public const float NeonMaxSpeed = 5.0f;
        public const float GapMin = 50, GapMax = 90, GapChance = 0.4f, FallCrash = -45;
        public const float GullHeight = 46, GullWidth = 30, GullBody = 16, GullSpeed = 1.6f;
        public const float PadWidth = 40, PadHeight = 6, PadJump = 13.5f;
        public const int MegaAirPoints = 50;

        // Mode infini (spec 002, FR-018, FR-019)
        public const float EndlessStart = 1.7f, EndlessSlopePerSecond = 0.012f, EndlessMax = 5.5f;
        public const int EndlessThemeEvery = 4000;

        // Skateur (choix) : position à l'écran et boîte de collision
        public const float SkaterScreenX = 90, SkaterHalfWidth = 12, SkaterHeight = 60, DuckHeight = 40;
    }
}
