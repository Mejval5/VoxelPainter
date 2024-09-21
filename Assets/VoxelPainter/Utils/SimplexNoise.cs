using UnityEngine;

namespace VoxelPainter.Utils
{
    public static class SimplexNoise
    {
        private static int[] perm =
        {
            151, 160, 137, 91, 90, 15, // Hash lookup table as defined by Ken Perlin. This is a randomly
            131, 13, 201, 95, 96, 53, 194, 233, 7, 225, 140, 36, // arranged array of all numbers from 0-255 inclusive.
            103, 30, 69, 142, 8, 99, 37, 240, 21, 10, 23,
            190, 6, 148, 247, 120, 234, 75, 0, 26, 197, 62, 94,
            252, 219, 203, 117, 35, 11, 32, 57, 177, 33,
            88, 237, 149, 56, 87, 174, 20, 125, 136, 171,
            168, 68, 175, 74, 165, 71, 134, 139, 48, 27,
            166, 77, 146, 158, 231, 83, 111, 229, 122, 60,
            211, 133, 230, 220, 105, 92, 41, 55, 46, 245,
            40, 244, 102, 143, 54, 65, 25, 63, 161, 1,
            216, 80, 73, 209, 76, 132, 187, 208, 89, 18,
            169, 200, 196, 135, 130, 116, 188, 159, 86,
            164, 100, 109, 198, 173, 186, 3, 64, 52, 217,
            226, 250, 124, 123, 5, 202, 38, 147, 118, 126,
            255, 82, 85, 212, 207, 206, 59, 227, 47, 16, 58,
            17, 182, 189, 28, 42, 223, 183, 170, 213, 119,
            248, 152, 2, 44, 154, 163, 70, 221, 153, 101,
            155, 167, 43, 172, 9, 129, 22, 39, 253, 19, 98,
            108, 110, 79, 113, 224, 232, 178, 185, 112, 104,
            218, 246, 97, 228, 251, 34, 242, 193, 238, 210,
            144, 12, 191, 179, 162, 241, 81, 51, 145, 235,
            249, 14, 239, 107, 49, 192, 214, 31, 181, 199,
            106, 157, 184, 84, 204, 176, 115, 121, 50, 45,
            127, 4, 150, 254, 138, 236, 205, 93, 222, 114,
            67, 29, 24, 72, 243, 141, 128, 195, 78, 66,
            215, 61, 156, 180
        };

        private static int[] permMod12 = new int[512];

        static SimplexNoise()
        {
            for (int i = 0; i < 512; i++)
            {
                permMod12[i] = perm[i % 256] % 12;
            }
        }

        private static readonly float[] grad3 =
        {
            1, 1, 0, -1, 1, 0, 1, -1, 0, -1, -1, 0,
            1, 0, 1, -1, 0, 1, 1, 0, -1, -1, 0, -1,
            0, 1, 1, 0, -1, 1, 0, 1, -1, 0, -1, -1
        };

        private static readonly float F3 = 1.0f / 3.0f;
        private static readonly float G3 = 1.0f / 6.0f;

        public static float Generate(Vector3 position)
        {
            return Generate(position.x, position.y, position.z);
        }
        
        public static float Generate(float x, float y, float z)
        {
            float s = (x + y + z) * F3; // Very nice and simple skew factor for 3D
            int i = Mathf.FloorToInt(x + s);
            int j = Mathf.FloorToInt(y + s);
            int k = Mathf.FloorToInt(z + s);

            float t = (i + j + k) * G3;
            float X0 = i - t; // Unskew the cell origin back to (x,y,z) space
            float Y0 = j - t;
            float Z0 = k - t;
            float x0 = x - X0; // The x,y,z distances from the cell origin
            float y0 = y - Y0;
            float z0 = z - Z0;

            // For the 3D case, the simplex shape is a slightly irregular tetrahedron.
            // Determine which simplex we are in.
            int i1, j1, k1; // Offsets for the second corner of simplex in (i,j,k) coords
            int i2, j2, k2; // Offsets for the third corner of simplex in (i,j,k) coords

            if (x0 >= y0)
            {
                if (y0 >= z0)
                {
                    i1 = 1;
                    j1 = 0;
                    k1 = 0;
                    i2 = 1;
                    j2 = 1;
                    k2 = 0;
                }
                else if (x0 >= z0)
                {
                    i1 = 1;
                    j1 = 0;
                    k1 = 0;
                    i2 = 1;
                    j2 = 0;
                    k2 = 1;
                }
                else
                {
                    i1 = 0;
                    j1 = 0;
                    k1 = 1;
                    i2 = 1;
                    j2 = 0;
                    k2 = 1;
                }
            }
            else
            {
                if (y0 < z0)
                {
                    i1 = 0;
                    j1 = 0;
                    k1 = 1;
                    i2 = 0;
                    j2 = 1;
                    k2 = 1;
                }
                else if (x0 < z0)
                {
                    i1 = 0;
                    j1 = 1;
                    k1 = 0;
                    i2 = 0;
                    j2 = 1;
                    k2 = 1;
                }
                else
                {
                    i1 = 0;
                    j1 = 1;
                    k1 = 0;
                    i2 = 1;
                    j2 = 1;
                    k2 = 0;
                }
            }

            float x1 = x0 - i1 + G3; // Offsets for the second corner in (x,y,z) coords
            float y1 = y0 - j1 + G3;
            float z1 = z0 - k1 + G3;
            float x2 = x0 - i2 + 2.0f * G3; // Offsets for the third corner in (x,y,z) coords
            float y2 = y0 - j2 + 2.0f * G3;
            float z2 = z0 - k2 + 2.0f * G3;
            float x3 = x0 - 1.0f + 3.0f * G3; // Offsets for the last corner in (x,y,z) coords
            float y3 = y0 - 1.0f + 3.0f * G3;
            float z3 = z0 - 1.0f + 3.0f * G3;

            // Wrap the indices to stay within 0-255 range for the permutation table
            int ii = i & 255;
            int jj = j & 255;
            int kk = k & 255;

            // Use modulo operation to wrap indices properly
            int gi0 = permMod12[(ii + perm[(jj + perm[kk]) % 256]) % 256] * 3;
            int gi1 = permMod12[(ii + i1 + perm[(jj + j1 + perm[(kk + k1) % 256]) % 256]) % 256] * 3;
            int gi2 = permMod12[(ii + i2 + perm[(jj + j2 + perm[(kk + k2) % 256]) % 256]) % 256] * 3;
            int gi3 = permMod12[(ii + 1 + perm[(jj + 1 + perm[(kk + 1) % 256]) % 256]) % 256] * 3;


            float t0 = 0.6f - x0 * x0 - y0 * y0 - z0 * z0;
            float n0;
            if (t0 < 0)
            {
                n0 = 0.0f;
            }
            else
            {
                t0 *= t0;
                n0 = t0 * t0 * Dot(grad3, gi0, x0, y0, z0);
            }

            float t1 = 0.6f - x1 * x1 - y1 * y1 - z1 * z1;
            float n1;
            if (t1 < 0)
            {
                n1 = 0.0f;
            }
            else
            {
                t1 *= t1;
                n1 = t1 * t1 * Dot(grad3, gi1, x1, y1, z1);
            }

            float t2 = 0.6f - x2 * x2 - y2 * y2 - z2 * z2;
            float n2;
            if (t2 < 0)
            {
                n2 = 0.0f;
            }
            else
            {
                t2 *= t2;
                n2 = t2 * t2 * Dot(grad3, gi2, x2, y2, z2);
            }

            float t3 = 0.6f - x3 * x3 - y3 * y3 - z3 * z3;
            float n3;
            if (t3 < 0)
            {
                n3 = 0.0f;
            }
            else
            {
                t3 *= t3;
                n3 = t3 * t3 * Dot(grad3, gi3, x3, y3, z3);
            }

            return 32.0f * (n0 + n1 + n2 + n3);
        }

        private static float Dot(float[] g, int i, float x, float y, float z)
        {
            return g[i] * x + g[i + 1] * y + g[i + 2] * z;
        }
    }
}