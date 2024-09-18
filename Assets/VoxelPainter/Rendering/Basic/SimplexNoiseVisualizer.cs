using Unity.Collections;
using UnityEngine;
using VoxelPainter.GridManagement;

namespace VoxelPainter.VoxelVisualization
{
    [ExecuteAlways]
    public class SimplexNoiseVisualizer : MarchingCubeRendererBase
    {
        public override void GetVertexValues(NativeArray<float> verticesValues)
        {
            int floorSize = VertexAmountX * VertexAmountZ;
            Vector3Int vertexAmount = VertexAmount;

            for (int i = 0; i < verticesValues.Length; i++)
            {
                Vector3Int position =MarchingCubeUtils.ConvertIndexToPosition(i, floorSize, vertexAmount);
                
                float x = (position.x + GenerationProperties.Origin.x) * GenerationProperties.Frequency / 1000f;
                float y = (position.y + GenerationProperties.Origin.y) * GenerationProperties.Frequency / 1000f;
                float z = (position.z + GenerationProperties.Origin.z) * GenerationProperties.Frequency / 1000f;

                verticesValues[i] = CustomNoiseSimplex(x, y, z);
            }
        }

        private static float CustomNoiseSimplex(float x, float y, float z)
        {
            return Mathf.Clamp01(Mathf.Pow(SimplexNoise.Generate(x, y, z), 2));
        }
    }
}