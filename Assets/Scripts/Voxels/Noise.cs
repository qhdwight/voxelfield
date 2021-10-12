using System.Collections.Generic;
using UnityEngine;

namespace Voxels
{
    public static class Noise
    {
        private static void Shuffle(int seed)
        {
            _permutations = new List<byte>(StandardPermutations);
            if (seed == 0) return;
            Random.InitState(seed);
            int n = _permutations.Count;
            while (n > 1)
            {
                n--;
                int k = Random.Range(0, n);
                (_permutations[k], _permutations[n]) = (_permutations[n], _permutations[k]);
            }
        }

        private static readonly List<byte> StandardPermutations = new()
        {
            151, 160, 137, 91, 90, 15,
            131, 13, 201, 95, 96, 53, 194, 233, 7, 225, 140, 36, 103, 30, 69, 142, 8, 99, 37, 240, 21, 10, 23,
            190, 6, 148, 247, 120, 234, 75, 0, 26, 197, 62, 94, 252, 219, 203, 117, 35, 11, 32, 57, 177, 33,
            88, 237, 149, 56, 87, 174, 20, 125, 136, 171, 168, 68, 175, 74, 165, 71, 134, 139, 48, 27, 166,
            77, 146, 158, 231, 83, 111, 229, 122, 60, 211, 133, 230, 220, 105, 92, 41, 55, 46, 245, 40, 244,
            102, 143, 54, 65, 25, 63, 161, 1, 216, 80, 73, 209, 76, 132, 187, 208, 89, 18, 169, 200, 196,
            135, 130, 116, 188, 159, 86, 164, 100, 109, 198, 173, 186, 3, 64, 52, 217, 226, 250, 124, 123,
            5, 202, 38, 147, 118, 126, 255, 82, 85, 212, 207, 206, 59, 227, 47, 16, 58, 17, 182, 189, 28, 42,
            223, 183, 170, 213, 119, 248, 152, 2, 44, 154, 163, 70, 221, 153, 101, 155, 167, 43, 172, 9,
            129, 22, 39, 253, 19, 98, 108, 110, 79, 113, 224, 232, 178, 185, 112, 104, 218, 246, 97, 228,
            251, 34, 242, 193, 238, 210, 144, 12, 191, 179, 162, 241, 81, 51, 145, 235, 249, 14, 239, 107,
            49, 192, 214, 31, 181, 199, 106, 157, 184, 84, 204, 176, 115, 121, 50, 45, 127, 4, 150, 254,
            138, 236, 205, 93, 222, 114, 67, 29, 24, 72, 243, 141, 128, 195, 78, 66, 215, 61, 156, 180
        };

        private static List<byte> _permutations = new();

        // F2 = (sqrt(3) - 1) / 2
        // G2 = (3 - sqrt(3)) / 6   = F2 / (1 + 2 * K)
        private const float F2 = 0.366025403f, G2 = 0.211324865f;

        private static byte Perm(int i)
        {
            int r = i % byte.MaxValue;
            return _permutations[r < 0 ? r + byte.MaxValue : i];
        }

        private static float Grad(int hash, float x, float y)
        {
            int h = hash & 0x3F;
            float u = h < 4 ? x : y,
                  v = h < 4 ? y : x;
            return ((h & 1) != 0 ? -u : u) + ((h & 2) != 0 ? -2.0f * v : 2.0f * v);
        }

        public static float RawSimplex(float x, float y)
        {
            float n0, n1, n2;
            float s = (x + y) * F2,
                  xs = x + s,
                  ys = y + s;
            int i = Mathf.FloorToInt(xs), j = Mathf.FloorToInt(ys);
            float t = (i + j) * G2, X0 = i - t, Y0 = j - t, x0 = x - X0, y0 = y - Y0;
            int i1, j1;
            if (x0 > y0)
            {
                i1 = 1;
                j1 = 0;
            }
            else
            {
                i1 = 0;
                j1 = 1;
            }
            float x1 = x0 - i1 + G2,
                  y1 = y0 - j1 + G2,
                  x2 = x0 - 1.0f + 2.0f * G2,
                  y2 = y0 - 1.0f + 2.0f * G2;
            int gi0 = Perm(i + Perm(j)),
                gi1 = Perm(i + i1 + Perm(j + j1)),
                gi2 = Perm(i + 1 + Perm(j + 1));
            float t0 = 0.5f - x0 * x0 - y0 * y0;
            if (t0 < 0.0f)
            {
                n0 = 0.0f;
            }
            else
            {
                t0 *= t0;
                n0 = t0 * t0 * Grad(gi0, x0, y0);
            }
            float t1 = 0.5f - x1 * x1 - y1 * y1;
            if (t1 < 0.0f)
            {
                n1 = 0.0f;
            }
            else
            {
                t1 *= t1;
                n1 = t1 * t1 * Grad(gi1, x1, y1);
            }
            float t2 = 0.5f - x2 * x2 - y2 * y2;
            if (t2 < 0.0f)
            {
                n2 = 0.0f;
            }
            else
            {
                t2 *= t2;
                n2 = t2 * t2 * Grad(gi2, x2, y2);
            }
            return 45.23065f * (n0 + n1 + n2);
        }

        public static void SetSeed(int seed) => Shuffle(seed);

        public static float Simplex(float x, float y, TerrainGenerationComponent terrainGeneration)
        {
            float output = 0.0f, denominator = 0.0f, frequency = 1.0f, amplitude = 1.0f;
            for (var i = 0; i < terrainGeneration.octaves; i++)
            {
                output += amplitude * RawSimplex(x * frequency / terrainGeneration.lateralScale, y * frequency / terrainGeneration.lateralScale);
                denominator += amplitude;
                frequency *= terrainGeneration.lacunarity;
                amplitude *= terrainGeneration.persistence;
            }
            return output / denominator * terrainGeneration.verticalScale;
        }
    }
}