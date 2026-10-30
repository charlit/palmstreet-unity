using System.Collections.Generic;

namespace PalmStreet.Core
{
    public enum BossState { Arriving, Ground, Rising, Air, Falling, Dying }

    public sealed class Tooth { public float X, Y; }

    /// <summary>Le boss (FR-017 à FR-019) : il garde sa distance devant le skateur, s'envole toutes les 10 s et lâche 4 plots.</summary>
    public sealed class Boss
    {
        public const float Distance = 230, Size = 90, FlyHeight = 150;   // choix
        public const int ArriveSteps = 120, MoveSteps = 50, DropEvery = 60;

        public BossState State = BossState.Arriving;
        public int Hp = Tuning.BossHp, Timer, ToothTimer, Drops;
        public float X, Lift;   // Lift : hauteur au-dessus du sol
        public readonly List<Tooth> Teeth = new List<Tooth>();

        public void Next(BossState s) { State = s; Timer = 0; }
    }
}
