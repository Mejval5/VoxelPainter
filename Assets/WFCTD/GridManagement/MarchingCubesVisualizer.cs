
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;

namespace WFCTD.GridManagement
{
    public class MarchingCubesVisualizer
    {
        public Vector3[] BaseVertices { get; private set; }
        public float[] VerticesValues { get; private set; }

        public Vector3[] SubVertices { get; private set; }
        public Vector3[] Normals { get; private set; }
        public int[] Triangles { get; private set; }
        public int[] ValidTriangles { get; private set; }

        public void MarchCubes(
            GenerationProperties generationProperties, 
            Vector3Int vertexAmount, 
            float surface, 
            MeshFilter gridMeshFilter,
            Func<int, Vector3, GenerationProperties, float> getVertexValue,
            int maxTriangles = int.MaxValue,
            bool useLerp = true)
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
            
            bool recalculateVertexValues = VerticesValues == null || preAllocatedVerticesValues != VerticesValues.Length;
            if (recalculateVertexValues)
            {
                BaseVertices = new Vector3[preAllocatedVerticesValues];
                VerticesValues = new float[preAllocatedVerticesValues];
            }
            
            for (int i = 0; i < preAllocatedVerticesValues; i++)
            {
                Vector3 pos;
                pos.x = i % vertexAmount.x;
                // ReSharper disable once PossibleLossOfFraction
                pos.z  = (i % floorSize) / vertexAmount.x;
                // ReSharper disable once PossibleLossOfFraction
                pos.y  = i / floorSize;
                
                BaseVertices[i] = pos;
                VerticesValues[i] = getVertexValue(i, pos, generationProperties);
            }

            if (Normals == null || preAllocatedVertices != Normals.Length)
            {
                Normals = new Vector3[preAllocatedVertices];
            }
            else
            {
                Array.Fill(Normals, Vector3.zero);
            }
            
            // Triangle has 3 vertices
            int preAllocatedTriangles = amountOfCubes * MarchingCubeUtils.MaximumTrianglesPerCube * 3;
            if (Triangles == null || preAllocatedTriangles != Triangles.Length)
            {
                Triangles = new int[preAllocatedTriangles];
            }
            
            Array.Fill(Triangles, -1);
            
            Profiler.EndSample();
            
            Profiler.BeginSample("MarchingCubesVisualizer.ConstructVertices");

            int[] baseVerticesOffsets =
            {
                0, 1, floorSize + 1, floorSize, vertexAmount.x, vertexAmount.x + 1, vertexAmount.x + floorSize + 1, vertexAmount.x + floorSize
            };

            int middleOffset = vertexAmount.x * cubeAmountZ + vertexAmount.z * cubeAmountX;
            int topOffset = vertexAmount.x * vertexAmount.z + middleOffset;
            
            int[] verticesOffsets = new int[12];

            int cubeFloor = cubeAmountX * cubeAmountZ;
            int triangleOffset = 0;
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
                    VerticesValues, 
                    verticesOffsets, 
                    surface, 
                    SubVertices, 
                    Triangles, 
                    Normals,
                    baseIndexOffset,
                    triangleOffset,
                    xIndex,
                    yIndex,
                    zIndex,
                    useLerp);
                
                triangleOffset += MarchingCubeUtils.MaximumTrianglesPerCube * 3;
            }
            
            Profiler.EndSample();
            Profiler.BeginSample("MarchingCubesVisualizer.AssignMesh");

            Profiler.BeginSample("MarchingCubesVisualizer.PruneTriangles");
            int maxItemsToPick = maxTriangles * 3;
            if (maxItemsToPick < 0)
            {
                maxItemsToPick = Triangles.Length;
            }
            ValidTriangles = Triangles.Where(value => value != -1).Take(maxItemsToPick).ToArray();
            Profiler.EndSample();
            
            Mesh mesh = new()
            {
                indexFormat = SubVertices.Length > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16,
                vertices = SubVertices,
                triangles = ValidTriangles,
                normals = Normals
            };
            Profiler.EndSample();
            
            
            Profiler.BeginSample("MarchingCubesVisualizer.RecalculateMesh");
            
            // Profiler.BeginSample("MarchingCubesVisualizer.RecalculateNormals");
            // mesh.RecalculateNormals();
            // Profiler.EndSample();
            
            Profiler.BeginSample("MarchingCubesVisualizer.RecalculateBounds");
            mesh.RecalculateBounds();
            Profiler.EndSample();
            
            // Profiler.BeginSample("MarchingCubesVisualizer.RecalculateTangents");
            // mesh.RecalculateTangents();
            // Profiler.EndSample();
            
            Profiler.EndSample();
            
            Profiler.BeginSample("MarchingCubesVisualizer.AssignMesh");
            gridMeshFilter.sharedMesh = mesh;
            gridMeshFilter.sharedMesh.hideFlags = HideFlags.DontSave;
            Profiler.EndSample();
        }
    }
}