using Unity.Collections;
using UnityEngine;

namespace VoxelPainter.VoxelVisualization
{
    public interface IMarchingCubesVisualizer
    {
        Vector3[] SubVertices { get; }

        void GetBaseVerticesNative(ref NativeArray<Vector3> vertices, Vector3Int vertexAmount);
        void GetVerticesValuesNative(ref NativeArray<float> verticesValues, Vector3Int vertexAmount);
    }
}