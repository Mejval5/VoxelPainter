using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using VoxelPainter.GridManagement;

namespace VoxelPainter.VoxelVisualization
{
    public class MarchingCubesCpuVisualizer : IMarchingCubesVisualizer
    {
        public NativeArray<float> VerticesValuesNative;
        public NativeArray<Vector3> BaseVerticesNative;
        public Vector3[] SubVertices { get; private set; }
        public List<int> Triangles { get; private set; }
        public int[] ValidTriangles { get; private set; }

        public NativeArray<float> ReadOnlyVerticesValuesNative => VerticesValuesNative;
        public NativeArray<Vector3> ReadOnlyBaseVertices => BaseVerticesNative;
        
        public void GetVerticesValuesNative(ref NativeArray<float> verticesValues, Vector3Int vertexAmount)
        {
            SetupVerticesArrays(vertexAmount);
            verticesValues = VerticesValuesNative;
        }
        
        private void SetupVerticesArrays(Vector3Int vertexAmount)
        {
            int preAllocatedBaseVertices = vertexAmount.x * vertexAmount.y * vertexAmount.z;
            bool recalculateVertexValues = VerticesValuesNative.IsCreated == false || preAllocatedBaseVertices != VerticesValuesNative.Length;
            if (recalculateVertexValues)
            {
                BaseVerticesNative = new NativeArray<Vector3>(preAllocatedBaseVertices, Allocator.Persistent);
                VerticesValuesNative = new NativeArray<float>(preAllocatedBaseVertices, Allocator.Persistent);
                
                int floorSize = vertexAmount.x * vertexAmount.z;
                for (int i = 0; i < preAllocatedBaseVertices; i++)
                {
                    Vector3Int pos = Vector3Int.zero;
                    pos.x = i % vertexAmount.x;
                    pos.z = (i % floorSize) / vertexAmount.x;
                    pos.y = i / floorSize;

                    BaseVerticesNative[i] = pos;
                }
            }
        }
        
        public void MarchCubes(
            GenerationProperties generationProperties, 
            Vector3Int vertexAmount, 
            float threshold, 
            MeshFilter gridMeshFilter,
            Action<NativeArray<float>> getVertexValues,
            int maxTriangles = int.MaxValue,
            bool useLerp = true,
            bool enforceEmptyBorder = true)
        {
            if (generationProperties == null)
            {
                return;
            }
            
            Profiler.BeginSample("MarchingCubesVisualizer.Setup");

            int cubeAmountX = vertexAmount.x - 1;
            int cubeAmountY = vertexAmount.y - 1;
            int cubeAmountZ = vertexAmount.z - 1;
            int amountOfCubes = cubeAmountX * cubeAmountY * cubeAmountZ;
            int preAllocatedVerticesValues = vertexAmount.x * vertexAmount.y * vertexAmount.z;
            int floorSize = vertexAmount.x * vertexAmount.z;
            int frontFaceSize = vertexAmount.x * vertexAmount.y;
            int sideFaceSize = vertexAmount.z * vertexAmount.y;
            
            int preAllocatedVertices = preAllocatedVerticesValues + (vertexAmount.y - 1) * floorSize + (vertexAmount.z - 1) * frontFaceSize + (vertexAmount.x - 1) * sideFaceSize;
            bool recalculateVertices = SubVertices == null || preAllocatedVertices != SubVertices.Length;
            if (recalculateVertices)
            {
                SubVertices = new Vector3[preAllocatedVertices];
            }
            else
            {
                Array.Fill(SubVertices, Vector3.zero);
            }

            SetupVerticesArrays(vertexAmount);
            
            // Triangle has 3 vertices
            int preAllocatedTriangles = amountOfCubes * MarchingCubeUtils.MaximumTrianglesPerCube * 3;
            if (Triangles == null || preAllocatedTriangles < Triangles.Count)
            {
                Triangles = new List<int>(preAllocatedTriangles);
            }
            else
            {
                Triangles.Clear();
            }
            int[] baseVerticesOffsets =
            {
                0, 1, floorSize + 1, floorSize, vertexAmount.x, vertexAmount.x + 1, vertexAmount.x + floorSize + 1, vertexAmount.x + floorSize
            };

            int middleOffset = vertexAmount.x * cubeAmountZ + vertexAmount.z * cubeAmountX;
            int topOffset = vertexAmount.x * vertexAmount.z + middleOffset;
            
            int[] verticesOffsets = new int[12];

            int cubeFloor = cubeAmountX * cubeAmountZ;
            Profiler.EndSample();
            
            Profiler.BeginSample("MarchingCubesVisualizer.FillVertices");
            getVertexValues(VerticesValuesNative);
            
            for (int i = 0; i < preAllocatedVerticesValues; i++)
            {
                if (!enforceEmptyBorder)
                {
                    continue;
                }
                
                Vector3Int pos = Vector3Int.zero;
                pos.x = i % vertexAmount.x;
                pos.z = (i % floorSize) / vertexAmount.x;
                pos.y = i / floorSize;


                if (MarchingCubeUtils.IsBorder(pos, vertexAmount))
                {
                    VerticesValuesNative[i] = 0;
                }
            }
            Profiler.EndSample();

            Profiler.BeginSample("MarchingCubesVisualizer.ConstructVertices");
            MarchCubesOnCPU(vertexAmount, threshold, useLerp, amountOfCubes, cubeAmountX, cubeFloor, topOffset, floorSize, middleOffset, verticesOffsets, baseVerticesOffsets);
            Profiler.EndSample();

            Profiler.BeginSample("MarchingCubesVisualizer.PruneTriangles");
            int maxItemsToPick = maxTriangles * 3;
            if (maxItemsToPick < 0 || maxItemsToPick > Triangles.Count)
            {
                maxItemsToPick = Triangles.Count;
            }

            // Create and fill the ValidTriangles array
            ValidTriangles = new int[maxItemsToPick];
            for (int i = 0; i < maxItemsToPick; i++)
            {
                ValidTriangles[i] = Triangles[i];
            }
            
            Profiler.EndSample();

            if (gridMeshFilter == null)
            {
                return; // Probably the object was destroyed while waiting for the mesh to be generated
            }
            
            Profiler.BeginSample("MarchingCubesVisualizer.FillMesh");
            Mesh sharedMesh = gridMeshFilter.sharedMesh;
            if (sharedMesh == null)
            {
                sharedMesh = new Mesh()
                {
                    indexFormat = SubVertices.Length > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16
                };
            }

            sharedMesh.MarkDynamic();
            sharedMesh.Clear();
            sharedMesh.vertices = SubVertices;
            sharedMesh.triangles = ValidTriangles;

            Profiler.EndSample();
            
            sharedMesh.RecalculateBounds();
            
            Profiler.BeginSample("MarchingCubesVisualizer.AssignMesh");
            gridMeshFilter.sharedMesh = sharedMesh;
            gridMeshFilter.sharedMesh.hideFlags = HideFlags.DontSave;
            Profiler.EndSample();
        }

        private void MarchCubesOnCPU(Vector3Int vertexAmount, float threshold, bool useLerp, int amountOfCubes, int cubeAmountX, int cubeFloor, int topOffset, int floorSize, int middleOffset,
            int[] verticesOffsets, int[] baseVerticesOffsets)
        {
            for (int i = 0; i < amountOfCubes; i++)
            {
                int xIndex = i % cubeAmountX;
                int zIndex = (i % cubeFloor) / cubeAmountX;
                int yIndex = i / cubeFloor;
                
                int indexOffset = xIndex + zIndex * (cubeAmountX + vertexAmount.x) + yIndex * topOffset;
                int baseIndexOffset = xIndex + zIndex * vertexAmount.x + yIndex * floorSize;
                
                int middleIndexOffset = middleOffset - zIndex * cubeAmountX;
                
                // Front face
                verticesOffsets[0] = indexOffset;
                verticesOffsets[1] = indexOffset + middleIndexOffset + 1;
                verticesOffsets[2] = indexOffset + topOffset;
                verticesOffsets[3] = indexOffset + middleIndexOffset;
                
                // Back face
                verticesOffsets[4] = indexOffset + cubeAmountX + vertexAmount.x;
                verticesOffsets[5] = indexOffset + middleIndexOffset + vertexAmount.x + 1;
                verticesOffsets[6] = indexOffset + topOffset + vertexAmount.x + cubeAmountX;
                verticesOffsets[7] = indexOffset + middleIndexOffset + vertexAmount.x;
                
                // Middle face
                verticesOffsets[8] = indexOffset + cubeAmountX;
                verticesOffsets[9] = indexOffset + cubeAmountX + 1;
                verticesOffsets[10] = indexOffset + topOffset + cubeAmountX + 1;
                verticesOffsets[11] = indexOffset + topOffset + cubeAmountX;

                MarchingCubeUtils.GetMarchedCube(
                    baseVerticesOffsets, 
                    VerticesValuesNative, 
                    verticesOffsets, 
                    threshold, 
                    SubVertices, 
                    Triangles,
                    baseIndexOffset,
                    xIndex,
                    yIndex,
                    zIndex,
                    useLerp);
            }
        }
        
        public void ReleaseBuffers()
        {
            VerticesValuesNative.Dispose();
            BaseVerticesNative.Dispose();
        }
    }
}