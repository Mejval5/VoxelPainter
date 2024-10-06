using Unity.Collections;
using UnityEngine;

namespace VoxelPainter.VoxelVisualization
{
    public interface IMarchingCubesVisualizer
    {
        /// <summary>
        /// The base vertices of the mesh.
        /// </summary>
        Vector3[] SubVertices { get; }
        
        /// <summary>
        /// Buffer for the base vertices.
        /// </summary>
        ComputeBuffer VerticesValuesBuffer { get; }
        
        /// <summary>
        /// This method is used to get the base vertices.
        /// Sets up the vertices arrays if they are not created or if the amount of vertices is different.
        /// </summary>
        /// <param name="vertices"></param>
        /// <param name="vertexAmount"></param>
        void GetBaseVerticesNative(ref NativeArray<Vector3> vertices, Vector3Int vertexAmount);
        
        /// <summary>
        /// This method is used to get the vertices values.
        /// Sets up the vertices arrays if they are not created or if the amount of vertices is different.
        /// </summary>
        /// <param name="verticesValues"></param>
        /// <param name="vertexAmount"></param>
        void GetVerticesValuesNative(ref NativeArray<int> verticesValues, Vector3Int vertexAmount);
    }
}