using UnityEngine;
using static PalmStreet.Game.Paint;

namespace PalmStreet.Game
{
    /// <summary>Particules (FR-024) : poussière des roues, étincelles de grind, éclats d'éclair, chute, explosion du boss.</summary>
    public sealed class Fx : MonoBehaviour
    {
        ParticleSystem dust, sparks, bits;

        public void Init()
        {
            var tex = new Texture2D(64, 64, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < 64; y++)
                for (int x = 0; x < 64; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(31.5f, 31.5f)) / 31.5f;
                    float a = Mathf.Clamp01(1 - d);
                    tex.SetPixel(x, y, new Color(1, 1, 1, a * a * (3 - 2 * a)));
                }
            tex.Apply();
            var mat = new Material(Mat) { mainTexture = tex };
            dust = Make("Poussière", mat, 8, 0.7f, -0.04f, 2.2f);
            sparks = Make("Étincelles", mat, 15, 0.35f, 0.7f, 0.2f);
            bits = Make("Éclats", mat, 17, 0.8f, 0.5f, 0.1f);
        }

        /// <summary>Vitesse d'émission en unités logiques par pas de 1/60 s.</summary>
        static Vector3 V(float vx, float vy) => new Vector3(vx * 60 / Ppu, vy * 60 / Ppu, 0);

        public void Dust(float x, float y, Color c, float amount = 1)
        {
            int n = Random.value < amount ? 1 : 0;
            for (int i = 0; i < n; i++) Emit(dust, x + Random.Range(-14f, 6f), y + 2, V(Random.Range(-1.4f, -0.4f), Random.Range(0.2f, 0.9f)), Random.Range(5f, 9f), c);
        }

        public void Puff(float x, float y, Color c)
        {
            for (int i = 0; i < 10; i++) Emit(dust, x + Random.Range(-16f, 16f), y + 2, V(Random.Range(-2f, 2f), Random.Range(0.3f, 1.2f)), Random.Range(6f, 11f), c);
        }

        public void Sparks(float x, float y)
        {
            for (int i = 0; i < 3; i++)
                Emit(sparks, x + Random.Range(-10f, 10f), y, V(Random.Range(-4f, -1f), Random.Range(1f, 4.5f)), Random.Range(2.5f, 4.5f),
                     Color.Lerp(Hex("#FFD24D"), Color.white, Random.value * 0.7f));
        }

        public void Burst(float x, float y, Color c, int count, float speed, float size = 6)
        {
            for (int i = 0; i < count; i++)
            {
                var d = Random.insideUnitCircle.normalized * Random.Range(0.3f, 1f) * speed;
                Emit(bits, x, y, V(d.x, d.y + speed * 0.3f), Random.Range(size * 0.5f, size * 1.4f), Color.Lerp(c, Color.white, Random.value * 0.4f));
            }
        }

        public void Confetti(float x, float y)
        {
            for (int i = 0; i < 70; i++)
            {
                var d = Random.insideUnitCircle * 9;
                Emit(bits, x, y, V(d.x, d.y + 4), Random.Range(4f, 9f), Color.HSVToRGB(Random.value, 0.75f, 1));
            }
        }

        /// <summary>Feu d'artifice : un anneau d'étincelles d'une couleur néon qui retombe.</summary>
        public void Firework(float x, float y)
        {
            var c = Color.HSVToRGB(Random.value, 0.65f, 1);
            int n = 36;
            for (int i = 0; i < n; i++)
            {
                float a = i * Mathf.PI * 2 / n;
                Emit(bits, x, y, V(Mathf.Cos(a) * 3.2f, Mathf.Sin(a) * 3.2f), Random.Range(4f, 7f), Color.Lerp(c, Color.white, Random.value * 0.3f));
            }
        }

        static void Emit(ParticleSystem ps, float x, float y, Vector3 vel, float size, Color c)
        {
            ps.Emit(new ParticleSystem.EmitParams { position = W(x, y), velocity = vel, startSize = size / Ppu, startColor = c, applyShapeToPosition = false }, 1);
        }

        ParticleSystem Make(string name, Material mat, int order, float life, float gravity, float sizeEnd)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.playOnAwake = false;
            main.startLifetime = life;
            main.gravityModifier = gravity;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 800;
            var emission = ps.emission; emission.enabled = false;
            var shape = ps.shape; shape.enabled = false;
            var size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1, AnimationCurve.Linear(0, 1, 1, sizeEnd));
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                      new[] { new GradientAlphaKey(0.9f, 0), new GradientAlphaKey(0.7f, 0.5f), new GradientAlphaKey(0, 1) });
            col.color = g;
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = mat;
            r.sortingOrder = order;
            ps.Play();
            return ps;
        }
    }
}
