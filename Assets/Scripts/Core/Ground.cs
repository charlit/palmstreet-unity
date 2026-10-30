using System;
using System.Collections.Generic;

namespace PalmStreet.Core
{
    public enum SlopeKind { Flat, Up, Down }

    public struct Segment
    {
        public SlopeKind Kind;
        public float X0, X1, Top;   // Top : hauteur de la bosse (montée et descente)
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

        // la pente la plus raide possible : sert d'échelle aux bonus et freins de vitesse
        static readonly float MaxSlope = Tuning.HillMax * (float)Math.PI / (2 * Tuning.RampMin);

        public Ground(Random rng, float start)
        {
            this.rng = rng;
            Segments.Add(new Segment { Kind = SlopeKind.Flat, X0 = start - 600, X1 = start + Tuning.StartFlat });
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
                Segments.Add(new Segment { Kind = SlopeKind.Down, X0 = x, X1 = x + Range(Tuning.RampMin, Tuning.RampMax), Top = last.Top });
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
            if (s.Kind == SlopeKind.Flat) return 0;
            float t = Clamp01((x - s.X0) / s.Length);
            float k = (1 - (float)Math.Cos(Math.PI * t)) / 2;
            return s.Kind == SlopeKind.Up ? s.Top * k : s.Top * (1 - k);
        }

        public float Slope(float x)
        {
            var s = At(x);
            if (s.Kind == SlopeKind.Flat) return 0;
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
