using System.Collections.Generic;

namespace PalmStreet.Core
{
    public enum BossState { Arriving, Ground, Rising, Air, Falling, Dying }

    /// <summary>
    /// Tir du skateur pendant un boss : une figure lance une dent droit devant (Spell = −1), un sort débloqué lance
    /// le sien (index de Tuning.SpellNames). Pas de tête chercheuse : il faut viser à la bonne hauteur.
    /// </summary>
    public sealed class Tooth
    {
        public float X, Y, Vx, Vy;
        public int Spell = -1, Damage, Life = 240;
    }

    /// <summary>
    /// Un boss. Violet (001, FR-017 à FR-019) : garde sa distance, s'envole toutes les 10 s et lâche 4 plots.
    /// Spec 002 (FR-011) : le Crabe et le Sanglier foncent au ras du sol vers le skateur (à sauter), le Goéland
    /// pique à hauteur de tête (s'accroupir ou sauter) ; entre deux attaques, coquillages ou barrières.
    /// Pendant une attaque, State vaut Rising (aller) puis Falling (retour).
    /// </summary>
    public sealed class Boss
    {
        public const float Distance = 230, Size = 90, FlyHeight = 150;   // choix
        public const int ArriveSteps = 120, MoveSteps = 25, DropEvery = 30;   // s'envole vite

        public readonly BossKind Kind;
        public readonly int MaxHp;
        public BossState State = BossState.Arriving;
        public int Hp, Timer, Drops;

        public Boss(BossKind kind = BossKind.Purple)
        {
            Kind = kind;
            MaxHp = Hp = kind switch { BossKind.Crab => 2500, BossKind.Gull => 3000, BossKind.Boar => 3500, _ => Tuning.BossHp };
        }
        public float X, Lift;   // Lift : hauteur au-dessus du sol
        public float Offset = Distance;   // distance devant le skateur (négative quand il est passé derrière)
        public const float GullDive = 46, BehindSkater = -90;

        /// <summary>Corps qui fait chuter le skateur (largeur, hauteur) ; (0, 0) pour le boss violet qui ne touche pas.</summary>
        public (float w, float h) Body => Kind switch { BossKind.Crab => (40, 28), BossKind.Gull => (60, 28), BossKind.Boar => (56, 34), _ => (0, 0) };

        /// <summary>Zone touchée par les tirs (largeur ; la hauteur est Height, depuis Lift).</summary>
        public float HitWidth => Kind == BossKind.Purple ? Size : Body.w;

        /// <summary>Taille dessinée (et cible des dents).</summary>
        public float Height => Kind switch { BossKind.Crab => 28, BossKind.Gull => 28, BossKind.Boar => 34, _ => Size };

        /// <summary>Pas entre deux attaques, durée de l'aller et du retour.</summary>
        public int AttackEvery => Kind == BossKind.Crab ? 200 : 240;   // il attaque souvent ; entre deux, il saute (Hop)
        public const int HopEvery = 80, HopSteps = 36;
        public const float HopHeight = 100;
        public int AttackSteps => Kind switch { BossKind.Crab => 60, BossKind.Gull => 80, _ => 50 };   // choix : laisse ~0,3 s pour sauter au bon moment
        public const int ReturnSteps = 80;
        public readonly List<Tooth> Teeth = new List<Tooth>();

        public void Next(BossState s) { State = s; Timer = 0; }
    }
}
