using Unity.Collections;
using UnityEngine;

namespace VoxelPainter.Utils
{
    public struct HitMeshInfo
    {
        public Vector3 HitPoint;
        public int VertexIndex;
        public bool IsHit;
        public Ray Ray;
    }

    public static class VoxelRaycaster
    {
        public static HitMeshInfo RayMarch(Ray ray, float stepSize, float threshold, int vertexAmountX, int vertexAmountY, int vertexAmountZ, NativeArray<float> verticesValues, Vector3 gridOrigin)
        {
            HitMeshInfo hitInfo = new ();
            Vector3 gridSpacing = new (1f, 1f, 1f);
            float maxDistance = Mathf.Sqrt(Mathf.Pow((vertexAmountX * gridSpacing.x), 2) + Mathf.Pow((vertexAmountY * gridSpacing.y), 2) + Mathf.Pow((vertexAmountZ * gridSpacing.z), 2));
        
            // Define the voxel grid bounds
            Vector3 gridMin = gridOrigin;
            Vector3 gridMax = gridOrigin + new Vector3(vertexAmountX * gridSpacing.x, vertexAmountY * gridSpacing.y, vertexAmountZ * gridSpacing.z);

            // Check if the ray intersects the voxel grid bounds
            if (!RayIntersectsBox(ray, gridMin, gridMax, out float tMin, out float tMax))
            {
                return hitInfo; // No intersection with the grid
            }

            // Adjust maxDistance to not exceed the distance inside the grid
            maxDistance = Mathf.Min(maxDistance, tMax - tMin);

            // Calculate the entry point using tMin
            Vector3 currentPosition = ray.origin + ray.direction * tMin;
            float distanceTraveled = 0f;

            while (distanceTraveled <= maxDistance)
            {
                currentPosition += ray.direction * stepSize;
                distanceTraveled += stepSize;

                float currentValue = SampleScalarField(currentPosition, vertexAmountX, vertexAmountY, vertexAmountZ, verticesValues, gridOrigin, gridSpacing, detectSmallFeatures: true);

                // Check for crossing
                if (!(currentValue > threshold))
                {
                    continue;
                }

                // Crossing detected, refine intersection point
                Vector3 intersectionPoint = RefineIntersection(ray, currentPosition - ray.direction * stepSize, currentPosition, threshold, vertexAmountX, vertexAmountY, vertexAmountZ, verticesValues, gridOrigin, gridSpacing);

                hitInfo.HitPoint = intersectionPoint;
                hitInfo.IsHit = true;
                // You can calculate the voxel index if needed
                break;
            }

            return hitInfo;
        }
    
        private static bool RayIntersectsBox(Ray ray, Vector3 min, Vector3 max, out float tMin, out float tMax)
        {
            tMin = 0.0f;
            tMax = Mathf.Infinity;

            for (int i = 0; i < 3; i++)
            {
                float origin = ray.origin[i];
                float direction = ray.direction[i];
                float invD = 1.0f / direction;

                float t0 = (min[i] - origin) * invD;
                float t1 = (max[i] - origin) * invD;

                if (invD < 0.0f)
                {
                    // Swap t0 and t1
                    (t0, t1) = (t1, t0);
                }

                tMin = Mathf.Max(tMin, t0);
                tMax = Mathf.Min(tMax, t1);

                if (tMax <= tMin)
                {
                    return false; // No intersection
                }
            }

            return true;
        }

        private static float SampleScalarField(Vector3 position, int vertexAmountX, int vertexAmountY, int vertexAmountZ, NativeArray<float> verticesValues, Vector3 gridOrigin, Vector3 gridSpacing, bool detectSmallFeatures = false)
        {
            // Convert world position to grid indices
            Vector3 localPos = position - gridOrigin;
            localPos.x /= gridSpacing.x;
            localPos.y /= gridSpacing.y;
            localPos.z /= gridSpacing.z;
            int x = Mathf.FloorToInt(localPos.x);
            int y = Mathf.FloorToInt(localPos.y);
            int z = Mathf.FloorToInt(localPos.z);

            // Check bounds
            if (x < 0 || x >= vertexAmountX - 1 || y < 0 || y >= vertexAmountY - 1 || z < 0 || z >= vertexAmountZ - 1)
            {
                return 0f; // Or some default value
            }

            // Trilinear interpolation
            float xd = localPos.x - x;
            float yd = localPos.y - y;
            float zd = localPos.z - z;

            int index000 = x + z * vertexAmountX + y * vertexAmountX * vertexAmountY;
            int index100 = index000 + 1;
            int index010 = index000 + vertexAmountX;
            int index001 = index000 + vertexAmountX * vertexAmountY;
            int index110 = index010 + 1;
            int index101 = index100 + vertexAmountX * vertexAmountY;
            int index011 = index001 + vertexAmountX;
            int index111 = index011 + 1;

            float c000 = verticesValues[index000];
            float c100 = verticesValues[index100];
            float c010 = verticesValues[index010];
            float c001 = verticesValues[index001];
            float c110 = verticesValues[index110];
            float c101 = verticesValues[index101];
            float c011 = verticesValues[index011];
            float c111 = verticesValues[index111];

            float c00 = Mathf.Lerp(c000, c100, xd);
            float c01 = Mathf.Lerp(c001, c101, xd);
            float c10 = Mathf.Lerp(c010, c110, xd);
            float c11 = Mathf.Lerp(c011, c111, xd);

            float c0 = Mathf.Lerp(c00, c10, yd);
            float c1 = Mathf.Lerp(c01, c11, yd);

            float c = Mathf.Lerp(c0, c1, zd);

            if (!detectSmallFeatures)
            {
                return c;
            }

            float maxHit = Mathf.Max(c00, c100, c010, c001, c110, c101, c011, c111);
            return maxHit;

        }

        private static Vector3 RefineIntersection(Ray ray, Vector3 start, Vector3 end, float threshold, int vertexAmountX, int vertexAmountY, int vertexAmountZ, NativeArray<float> verticesValues, Vector3 gridOrigin, Vector3 gridSpacing)
        {
            const int maxIterations = 10;
            const float tolerance = 1e-4f;

            Vector3 a = start;
            Vector3 b = end;

            float valueA = SampleScalarField(a, vertexAmountX, vertexAmountY, vertexAmountZ, verticesValues, gridOrigin, gridSpacing) - threshold;
            float valueB = SampleScalarField(b, vertexAmountX, vertexAmountY, vertexAmountZ, verticesValues, gridOrigin, gridSpacing) - threshold;

            for (int i = 0; i < maxIterations; i++)
            {
                Vector3 mid = (a + b) * 0.5f;
                float valueMid = SampleScalarField(mid, vertexAmountX, vertexAmountY, vertexAmountZ, verticesValues, gridOrigin, gridSpacing) - threshold;

                if (Mathf.Abs(valueMid) < tolerance)
                {
                    return mid;
                }

                if (valueA * valueMid < 0)
                {
                    b = mid;
                    valueB = valueMid;
                }
                else
                {
                    a = mid;
                    valueA = valueMid;
                }
            }

            return (a + b) * 0.5f;
        }
    }
}