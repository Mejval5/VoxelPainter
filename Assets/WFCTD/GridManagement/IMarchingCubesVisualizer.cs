using Unity.Collections;
using UnityEngine;

namespace WFCTD.GridManagement
{
    public interface IMarchingCubesVisualizer
    {
        NativeArray<float> ReadOnlyVerticesValuesNative { get; }
        NativeArray<Vector3> ReadOnlyBaseVertices { get; }
        Vector3[] SubVertices { get; }

        void GetVerticesValuesNative(ref NativeArray<float> verticesValues, Vector3Int vertexAmount);
    }
}