using Unity.Collections;
using UnityEngine;
using VoxelPainter.GridManagement;
using VoxelPainter.Utils;

namespace VoxelPainter.VoxelVisualization
{
    [ExecuteAlways]
    public class SimplexNoiseVisualizer : MarchingCubeRendererBase
    {
        [SerializeField] private GenerationProperties _generationProperties;
        
        public override void GetVertexValues(NativeArray<float> verticesValues)
        {
            int floorSize = VertexAmountX * VertexAmountZ;
            Vector3Int vertexAmount = VertexAmount;

            for (int i = 0; i < verticesValues.Length; i++)
            {
                Vector3Int position =MarchingCubeUtils.ConvertIndexToPosition(i, floorSize, vertexAmount);
                
                float x = (position.x + _generationProperties.Origin.x) * _generationProperties.Frequency / 1000f;
                float y = (position.y + _generationProperties.Origin.y) * _generationProperties.Frequency / 1000f;
                float z = (position.z + _generationProperties.Origin.z) * _generationProperties.Frequency / 1000f;

                verticesValues[i] = CustomNoiseSimplex(x, y, z);
            }
        }

        private static float CustomNoiseSimplex(float x, float y, float z)
        {
            return Mathf.Clamp01(Mathf.Pow(SimplexNoiseGenerator.Generate(x, y, z), 2));
        }
    }
}