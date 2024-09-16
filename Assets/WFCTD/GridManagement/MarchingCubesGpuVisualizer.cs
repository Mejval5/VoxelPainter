using System;
using System.Runtime.InteropServices;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;

namespace WFCTD.GridManagement
{
    public class MarchingCubesGpuVisualizer : IMarchingCubesVisualizer
    {
        private const string ComputeShaderProgram = "CSMain";

        // Ensure sequential layout for memory alignment
        [StructLayout(LayoutKind.Sequential)]
        public struct Triangle
        {
            public int v1;
            public int v2;
            public int v3;
        }
        
        private static readonly int AppendedTriangles = Shader.PropertyToID("AppendedTriangles");
        private static readonly int VertexAmount = Shader.PropertyToID("VertexAmount");
        private static readonly int CubeAmount = Shader.PropertyToID("CubeAmount");
        private static readonly int Offsets = Shader.PropertyToID("Offsets");
        private static readonly int UseLerp = Shader.PropertyToID("UseLerp");
        private static readonly int Threshold = Shader.PropertyToID("Threshold");
        private static readonly int SubVerticesShaderKey = Shader.PropertyToID("SubVertices");
        private static readonly int BaseVerticesValues = Shader.PropertyToID("BaseVerticesValues");
        private static readonly int CubeEdgeFlags = Shader.PropertyToID("CubeEdgeFlags");
        private static readonly int TriangleConnectionTable = Shader.PropertyToID("TriangleConnectionTable");

        public NativeArray<float> VerticesValuesNative;
        public NativeArray<Vector3> BaseVerticesNative;
        public Vector3[] SubVertices { get; private set; }
        
        public NativeArray<float> ReadOnlyVerticesValuesNative => VerticesValuesNative;
        public NativeArray<Vector3> ReadOnlyBaseVertices => BaseVerticesNative;
        
        private ComputeBuffer _appendedTrianglesBuffer;
        private ComputeBuffer _subVerticesBuffer;
        private ComputeBuffer _baseVerticesValuesBuffer;
        private ComputeBuffer _cubeEdgeFlagsBuffer;
        private ComputeBuffer _triangleConnectionTableBuffer;
        
        public void MarchCubes(
            GenerationProperties generationProperties, 
            Vector3Int vertexAmount, 
            float threshold, 
            MeshFilter gridMeshFilter,
            Action<NativeArray<float>> getVertexValues,
            ComputeShader computeShader,
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
            int cubeFloorSize = vertexAmount.x * vertexAmount.z;
            int preAllocatedBaseVertices = vertexAmount.x * vertexAmount.y * vertexAmount.z;
            int floorSize = vertexAmount.x * vertexAmount.z;
            int frontFaceSize = vertexAmount.x * vertexAmount.y;
            int sideFaceSize = vertexAmount.z * vertexAmount.y;
            
            int preAllocatedSubVertices = preAllocatedBaseVertices + cubeAmountY * floorSize + cubeAmountZ * frontFaceSize + cubeAmountX * sideFaceSize;
            
            bool recalculateVertexValues = VerticesValuesNative.IsCreated == false || preAllocatedBaseVertices != VerticesValuesNative.Length;
            if (recalculateVertexValues)
            {
                BaseVerticesNative = new NativeArray<Vector3>(preAllocatedBaseVertices, Allocator.Persistent);
                VerticesValuesNative = new NativeArray<float>(preAllocatedBaseVertices, Allocator.Persistent);
            }
            Profiler.EndSample();
            
            Profiler.BeginSample("MarchingCubesVisualizer.GetVertexValues");
            getVertexValues(VerticesValuesNative);
            Profiler.EndSample();
            Profiler.BeginSample("MarchingCubesVisualizer.SetUpVertices");
            for (int i = 0; i < preAllocatedBaseVertices; i++)
            {
                Vector3Int pos = Vector3Int.zero;
                pos.x = i % vertexAmount.x;
                pos.z  = (i % floorSize) / vertexAmount.x;
                pos.y  = i / floorSize;

                BaseVerticesNative[i] = pos;

                if (!enforceEmptyBorder)
                {
                    continue;
                }

                if (MarchingCubeUtils.IsBorder(pos, vertexAmount))
                {
                    VerticesValuesNative[i] = 0;
                }
            }
            Profiler.EndSample();
            
            Profiler.BeginSample("MarchingCubesVisualizer.SetUpStructuredBuffers");
            SetupTriangleBuffer(amountOfCubes);
            SetupSubVerticesBuffer(preAllocatedSubVertices);
            SetupBaseVerticesValuesBuffer(preAllocatedBaseVertices, recalculateVertexValues);
            SetupStaticBuffers();
            Profiler.EndSample();
            
            Profiler.BeginSample("MarchingCubesVisualizer.PrecomputeValues");
            int middleOffset = vertexAmount.x * cubeAmountZ + vertexAmount.z * cubeAmountX;
            int topOffset = vertexAmount.x * vertexAmount.z + middleOffset;
            Profiler.EndSample();
            
            Profiler.BeginSample("MarchingCubesVisualizer.InjectDataIntoComputeShader");
            InjectDataIntoComputeShader(vertexAmount, threshold, useLerp, floorSize, cubeAmountX, cubeAmountY, 
                cubeAmountZ, cubeFloorSize, middleOffset, topOffset, computeShader);
            Profiler.EndSample();
            
            Profiler.BeginSample("MarchingCubesVisualizer.MarchCubes");
            DispatchComputeShader(amountOfCubes, computeShader);
            Profiler.EndSample();
            
            Profiler.BeginSample("MarchingCubesVisualizer.GetResults");
            // Create a buffer to hold the count (size of 4 bytes for an int)
            ComputeBuffer triangleCountBuffer = new(1, sizeof(int), ComputeBufferType.Raw);

            // Copy the counter value from the append buffer to the count buffer
            ComputeBuffer.CopyCount(_appendedTrianglesBuffer, triangleCountBuffer, 0);

            // Read back the count from the GPU
            int[] triangleCountArray = new int[1];
            triangleCountBuffer.GetData(triangleCountArray);
            int triangleCount = triangleCountArray[0];

            // Release the count buffer when done
            triangleCountBuffer.Release();
            
            if (triangleCount == 0)
            {
                Debug.LogWarning("No triangles appended.");
                Profiler.EndSample();
                return;
            }

            if (maxTriangles < 0)
            {
                maxTriangles = triangleCount;
            }
            triangleCount = Math.Min(triangleCount, maxTriangles);
            int[] triangleIndices = new int[triangleCount];
            _appendedTrianglesBuffer.GetData(triangleIndices, 0, 0, triangleCount);
            
            SubVertices = new Vector3[preAllocatedSubVertices];
            _subVerticesBuffer.GetData(SubVertices, 0, 0, preAllocatedSubVertices);
            Profiler.EndSample();
            
            Profiler.BeginSample("MarchingCubesVisualizer.FillMesh");
            Mesh sharedMesh = gridMeshFilter.sharedMesh;
            if (sharedMesh == null)
            {
                sharedMesh = new Mesh
                {
                    indexFormat = SubVertices.Length > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16
                };
            }

            sharedMesh.MarkDynamic();
            sharedMesh.Clear();
            sharedMesh.vertices = SubVertices;
            sharedMesh.triangles = triangleIndices;
            sharedMesh.RecalculateBounds();
            Profiler.EndSample();
        }

        private void InjectDataIntoComputeShader(Vector3Int vertexAmount, float threshold, bool useLerp, int floorSize, int cubeAmountX, int cubeAmountY, int cubeAmountZ, int cubeFloorSize, int middleOffset,
            int topOffset, ComputeShader computeShader)
        {
            // Set the buffer on the compute shader
            int kernelHandle = computeShader.FindKernel(ComputeShaderProgram);
            computeShader.SetBuffer(kernelHandle, AppendedTriangles, _appendedTrianglesBuffer);
            computeShader.SetBuffer(kernelHandle, SubVerticesShaderKey, _subVerticesBuffer);
            computeShader.SetBuffer(kernelHandle, BaseVerticesValues, _baseVerticesValuesBuffer);
            
            computeShader.SetBuffer(kernelHandle, CubeEdgeFlags, _cubeEdgeFlagsBuffer);
            computeShader.SetBuffer(kernelHandle, TriangleConnectionTable, _triangleConnectionTableBuffer);
            
            computeShader.SetInts(VertexAmount, vertexAmount.x, vertexAmount.y, vertexAmount.z, floorSize);
            computeShader.SetInts(CubeAmount, cubeAmountX, cubeAmountY, cubeAmountZ, cubeFloorSize);
            computeShader.SetInts(Offsets, middleOffset, topOffset);
            computeShader.SetBool(UseLerp, useLerp);
            computeShader.SetFloat(Threshold, threshold);
        }

        private void DispatchComputeShader(int amountOfCubes, ComputeShader computeShader)
        {
            int kernelHandle = computeShader.FindKernel(ComputeShaderProgram);
            // Dispatch the compute shader
            computeShader.GetKernelThreadGroupSizes(kernelHandle, out uint xSize, out _, out _);
            int threadSizeX = (int)xSize;
            int threadGroupsX = (amountOfCubes + threadSizeX - 1) / threadSizeX;
            computeShader.Dispatch(kernelHandle, threadGroupsX, 1, 1);
        }

        private void SetupBaseVerticesValuesBuffer(int preAllocatedBaseVertices, bool recalculateVertexValues)
        {
            const int sizeOfFloat = sizeof(float);

            _baseVerticesValuesBuffer?.Dispose();
            _baseVerticesValuesBuffer = new ComputeBuffer(preAllocatedBaseVertices, sizeOfFloat);
            _baseVerticesValuesBuffer.SetData(VerticesValuesNative);
        }

        private void SetupSubVerticesBuffer(int preAllocatedSubVertices)
        {
            const int sizeOfFloat = sizeof(float);
            
            _subVerticesBuffer?.Dispose();
            _subVerticesBuffer = new ComputeBuffer(preAllocatedSubVertices, sizeOfFloat * 3);
        }

        private void SetupTriangleBuffer(int amountOfCubes)
        {
            // Define the stride (size) of an integer in bytes
            const int strideInt = sizeof(int);

            // Initialize the AppendStructuredBuffer with an initial capacity
            // Here, assuming a maximum of 300 indices (100 triangles)
            int preAllocatedTriangles = amountOfCubes * MarchingCubeUtils.MaximumTrianglesPerCube * 3;
            _appendedTrianglesBuffer?.Dispose();
            _appendedTrianglesBuffer = new ComputeBuffer(preAllocatedTriangles, strideInt, ComputeBufferType.Append);
            _appendedTrianglesBuffer.SetCounterValue(0);
        }

        private void SetupStaticBuffers()
        {
            _cubeEdgeFlagsBuffer?.Dispose();
            _cubeEdgeFlagsBuffer = new ComputeBuffer(MarchingCubeUtils.CubeEdgeFlags.Length, sizeof(int));
            _cubeEdgeFlagsBuffer.SetData(MarchingCubeUtils.CubeEdgeFlags);
            
            _triangleConnectionTableBuffer?.Dispose();
            _triangleConnectionTableBuffer = new ComputeBuffer(MarchingCubeUtils.TriangleConnectionTable.Length, sizeof(int));
                
            // Get the dimensions
            int rows = MarchingCubeUtils.TriangleConnectionTable.GetLength(0); // 3 rows
            int cols = MarchingCubeUtils.TriangleConnectionTable.GetLength(1); // 3 columns
                
            // Create a 1D array
            int[] triangleConnectionTable1D = new int[rows * cols]; // 9 elements

            // Unroll the 2D array into the 1D array
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    triangleConnectionTable1D[i * cols + j] = MarchingCubeUtils.TriangleConnectionTable[i, j];
                }
            }
            _triangleConnectionTableBuffer.SetData(triangleConnectionTable1D);
        }
        
        public void ReleaseBuffers()
        {
            _appendedTrianglesBuffer?.Dispose();
            _subVerticesBuffer?.Dispose();
            _baseVerticesValuesBuffer?.Dispose();
            _cubeEdgeFlagsBuffer?.Dispose();
            _triangleConnectionTableBuffer?.Dispose();
            
            VerticesValuesNative.Dispose();
            BaseVerticesNative.Dispose();
        }
    }
}