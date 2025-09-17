using UnityEngine;

namespace Game.Core
{
    public static class NoiseUtility
    {
        /// Перлин с масштабом и оффсетами (0..1).
        public static float Perlin(float x, float y, float scale, float offX, float offY)
        {
            return Mathf.PerlinNoise((x + offX) * scale, (y + offY) * scale);
        }

        /// Детерминированные оффсеты от seed (+salt для разных каналов).
        public static (float offX, float offY) MakeOffsetsFromSeed(int seed, int salt = 0)
        {
            unchecked
            {
                uint k = (uint)seed * 73856093u ^ (0x9E3779B9u + (uint)salt);
                var rng = new System.Random((int)k); // допустим любой int
                float ox = (float)rng.NextDouble() * 10000f + 123.456f;
                float oy = (float)rng.NextDouble() * 10000f + 789.012f;
                return (ox, oy);
            }
        }

        /// Быстрый детерминированный hash → [0..1].
        public static float Hash01(int v)
        {
            unchecked
            {
                uint x = (uint)v;
                x ^= x >> 17; x *= 0xED5AD4BBu;
                x ^= x >> 11; x *= 0xAC4C1B51u;
                x ^= x >> 15; x *= 0x31848BABu;
                x ^= x >> 14;
                return (x & 0x00FFFFFFu) / 16777215f;
            }
        }
    }
}