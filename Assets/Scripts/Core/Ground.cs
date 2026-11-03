using System;
using System.Collections.Generic;

namespace PalmStreet.Core
{
    public enum SlopeKind { Flat, Up, Down, Gap }

    public struct Segment
    {
        public SlopeKind Kind;
        public float X0, X1, Top;   // Top : hauteur de la bosse (montée et descente) ou du palier de départ
        public float Length => X1 - X0;
    }

    /// <summary>
    /// Sol en segments (FR-005) : plat, montée, descente qui revient au niveau 0, et ainsi de suite.
    /// Pentes en demi-cosinus pour un relief doux. Hauteurs vers le haut à partir du niveau de base 0.
    /// </summary>
    public sealed class Ground
    {
        public readonly List<Segment> Segments = new List<Segment>();
        readonly Random rng;

        /// <summary>Le générateur ne fait que du plat jusqu'à cette abscisse (boss et plat d'après boss).</summary>
        public float FlatUntil = float.NegativeInfinity;

        /// <summary>Niveau 2 : des trous dans la route (FR-030). Hauteur du vide : Abyss.</summary>
        public bool Gaps;

        /// <summary>Skatepark (spec 006) : du plat et des bowls (creux), ni bosse ni trou.</summary>
        public bool Park;
        public const float Abyss = -1000;

        // la pente la plus raide possible : sert d'échelle aux bonus et freins de vitesse
        static readonly float MaxSlope = Tuning.HillMax * (float)Math.PI / (2 * Tuning.RampMin);

        public Ground(Random rng, float start, bool startRamp = false, float rampTop = Tuning.StartRampTop, float rampLength = Tuning.StartRampLength)
        {
            this.rng = rng;
            if (!startRamp)
            {
                Segments.Add(new Segment { Kind = SlopeKind.Flat, X0 = start - 600, X1 = start + Tuning.StartFlat });
                return;
            }
            // départ en haut d'une rampe : palier, descente jusqu'au niveau 0, puis le plat de départ habituel
            float top = rampTop, len = rampLength;
            Segments.Add(new Segment { Kind = SlopeKind.Flat, X0 = -600, X1 = 0, Top = top });
            Segments.Add(new Segment { Kind = SlopeKind.Down, X0 = 0, X1 = len, Top = top });
            Segments.Add(new Segment { Kind = SlopeKind.Flat, X0 = len, X1 = Math.Max(start, len) + Tuning.StartFlat });
        }

        public float End => Segments[Segments.Count - 1].X1;

        public void EnsureUpTo(float x)
        {
            while (End < x) Grow();
        }

        void Grow()
        {
            var last = Segments[Segments.Count - 1];
            float x = last.X1;
            if (last.Kind == SlopeKind.Up)
            {
                float down = last.Top < 0 ? Tuning.BowlHalf : Range(Tuning.RampMin, Tuning.RampMax);   // bowl : remontée symétrique
                Segments.Add(new Segment { Kind = SlopeKind.Down, X0 = x, X1 = x + down, Top = last.Top });
                return;
            }
            if (Park)   // plat, puis un bowl de temps en temps : une « montée » de hauteur négative (on descend) et sa remontée
            {
                if (last.Kind == SlopeKind.Flat && last.Length > 250 && rng.NextDouble() < Tuning.BowlChance)
                    Segments.Add(new Segment { Kind = SlopeKind.Up, X0 = x, X1 = x + Tuning.BowlHalf, Top = -Tuning.BowlDepth });
                else Segments.Add(new Segment { Kind = SlopeKind.Flat, X0 = x, X1 = x + Range(Tuning.FlatMin, Tuning.FlatMax) });
                return;
            }
            if (last.Kind == SlopeKind.Flat && x >= FlatUntil && Gaps && rng.NextDouble() < Tuning.GapChance)
            {
                Segments.Add(new Segment { Kind = SlopeKind.Gap, X0 = x, X1 = x + Range(Tuning.GapMin, Tuning.GapMax) });
                return;
            }
            if (last.Kind == SlopeKind.Flat && x >= FlatUntil)
            {
                Segments.Add(new Segment { Kind = SlopeKind.Up, X0 = x, X1 = x + Range(Tuning.RampMin, Tuning.RampMax), Top = Range(Tuning.HillMin, Tuning.HillMax) });
                return;
            }
            float len = Range(Tuning.FlatMin, Tuning.FlatMax);
            if (x < FlatUntil) len = Math.Min(len, Math.Max(FlatUntil - x, 1));   // le plat forcé se termine pile à FlatUntil
            Segments.Add(new Segment { Kind = SlopeKind.Flat, X0 = x, X1 = x + len });
        }

        /// <summary>Oublie les segments qui commencent après x (pas encore à l'écran) : le relief à venir devient plat.</summary>
        public void TrimAfter(float x)
        {
            while (Segments.Count > 1 && Segments[Segments.Count - 1].X0 > x) Segments.RemoveAt(Segments.Count - 1);
            // une bosse coupée après sa montée garde sa descente
            if (Segments[Segments.Count - 1].Kind == SlopeKind.Up) Grow();
        }

        public void DropBefore(float x)
        {
            while (Segments.Count > 2 && Segments[1].X1 < x) Segments.RemoveAt(0);
        }

        public Segment At(float x)
        {
            for (int i = Segments.Count - 1; i >= 0; i--)
                if (Segments[i].X0 <= x) return Segments[i];
            return Segments[0];
        }

        public float Height(float x)
        {
            var s = At(x);
            if (s.Kind == SlopeKind.Flat) return s.Top;   // 0, sauf le palier de la rampe de départ
            if (s.Kind == SlopeKind.Gap) return Abyss;
            float t = Clamp01((x - s.X0) / s.Length);
            float k = (1 - (float)Math.Cos(Math.PI * t)) / 2;
            return s.Kind == SlopeKind.Up ? s.Top * k : s.Top * (1 - k);
        }

        public float Slope(float x)
        {
            var s = At(x);
            if (s.Kind == SlopeKind.Flat || s.Kind == SlopeKind.Gap) return 0;
            float t = Clamp01((x - s.X0) / s.Length);
            float d = s.Top * (float)Math.PI / (2 * s.Length) * (float)Math.Sin(Math.PI * t);
            return s.Kind == SlopeKind.Up ? d : -d;
        }

        /// <summary>FR-005 : jusqu'à +55 % en descente, jusqu'à −22 % en montée, selon la raideur.</summary>
        public float SpeedFactor(float x)
        {
            float s = Slope(x), k = Math.Min(1, Math.Abs(s) / MaxSlope);
            return s < 0 ? 1 + Tuning.DownhillBoost * k : 1 - Tuning.UphillDrag * k;
        }

        public bool IsGap(float x) => At(x).Kind == SlopeKind.Gap;

        /// <summary>Hauteur du sol, ou 0 au-dessus d'un trou (pour poser éclairs et mouettes).</summary>
        public float Floor(float x) => IsGap(x) ? 0 : Height(x);

        /// <summary>Premier trou qui touche [x0, x1], s'il y en a un.</summary>
        public Segment? GapIn(float x0, float x1)
        {
            EnsureUpTo(x1);
            foreach (var s in Segments)
                if (s.Kind == SlopeKind.Gap && s.X1 > x0 && s.X0 < x1) return s;
            return null;
        }

        /// <summary>Montée qui touche [x0, x1], ou null : on n'y pose pas de plot.</summary>
        public Segment? UpIn(float x0, float x1)
        {
            EnsureUpTo(x1);
            foreach (var s in Segments)
                if (s.Kind == SlopeKind.Up && s.X1 > x0 && s.X0 < x1) return s;
            return null;
        }

        public bool IsFlat(float x0, float x1)
        {
            EnsureUpTo(x1);
            foreach (var s in Segments)
                if (s.Kind != SlopeKind.Flat && s.X1 > x0 && s.X0 < x1) return false;
            return true;
        }

        /// <summary>FR-007 : premier tiers d'une descente, où l'on ne pose pas d'obstacle.</summary>
        public bool InDescentStart(float x, out float after)
        {
            var s = At(x);
            after = s.X0 + s.Length / 3;
            return s.Kind == SlopeKind.Down && x < after;
        }

        float Range(float a, float b) => a + (float)rng.NextDouble() * (b - a);
        static float Clamp01(float v) => v < 0 ? 0 : v > 1 ? 1 : v;
    }
}
