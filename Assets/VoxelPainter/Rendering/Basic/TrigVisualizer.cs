using Foxworks.Voxels;
using Unity.Collections;
using UnityEngine;
using VoxelPainter.GridManagement;
using VoxelPainter.Rendering.Utils;

namespace VoxelPainter.VoxelVisualization
{
    /// <summary>
    /// This class is used to visualize a trigonometric function.
    /// </summary>
    public class TrigVisualizer : MarchingCubeRendererBase
    {
        [SerializeField] private GenerationProperties _generationProperties;
        
        public override void GetVertexValues(NativeArray<int> verticesValues)
        {
            int floorSize = VertexAmountX * VertexAmountZ;
            Vector3Int vertexAmount = VertexAmount;

            for (int i = 0; i < verticesValues.Length; i++)
            {
                Vector3Int position = MarchingCubeUtils.ConvertIndexToPosition(i, floorSize, vertexAmount);
                float x = (position.x + _generationProperties.Origin.x) * _generationProperties.Frequency / 1000f;
                float y = (position.y + _generationProperties.Origin.y) * _generationProperties.Frequency / 1000f;
                float z = (position.z + _generationProperties.Origin.z) * _generationProperties.Frequency / 1000f;
            
                verticesValues[i] = VoxelDataUtils.PackValueAndVertexId((Mathf.Sin(x) + Mathf.Cos(y) + Mathf.Cos(z) + 3) / 6f);
            }
        }
    }
}