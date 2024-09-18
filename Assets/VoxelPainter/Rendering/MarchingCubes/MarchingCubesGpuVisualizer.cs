using System;
using System.Runtime.InteropServices;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using VoxelPainter.GridManagement;

namespace VoxelPainter.VoxelVisualization
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
        private static readonly int AmountOfCubes = Shader.PropertyToID("AmountOfCubes");
        private static readonly int EnforceEmptyBorder = Shader.PropertyToID("EnforceEmptyBorder");

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

        private Array _subVerticesCleaningArray;
        
        public void GetVerticesValuesNative(ref NativeArray<float> verticesValues, Vector3Int vertexAmount)
        {
            SetupVerticesArrays(vertexAmount);
            verticesValues = VerticesValuesNative;
        }

        private void SetupVerticesArrays(Vector3Int vertexAmount)
        {
            int preAllocatedBaseVertices = vertexAmount.x * vertexAmount.y * vertexAmount.z;
            bool recalculateVertexValues = VerticesValuesNative.IsCreated == false || preAllocatedBaseVertices != VerticesValuesNative.Length;
            if (!recalculateVertexValues)
            {
                return;
            }

            BaseVerticesNative = new NativeArray<Vector3>(preAllocatedBaseVertices, Allocator.Persistent);
            VerticesValuesNative = new NativeArray<float>(preAllocatedBaseVertices, Allocator.Persistent);
            Profiler.BeginSample("MarchingCubesVisualizer.SetUpVertices");
            int floorSize = vertexAmount.x * vertexAmount.z;
            for (int i = 0; i < preAllocatedBaseVertices; i++)
            {
                Vector3Int pos = Vector3Int.zero;
                pos.x = i % vertexAmount.x;
                pos.z  = (i % floorSize) / vertexAmount.x;
                pos.y  = i / floorSize;

                BaseVerticesNative[i] = pos;
            }
            Profiler.EndSample();
        }
            
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
            int cubeFloorSize = cubeAmountX * cubeAmountZ;
            int preAllocatedBaseVertices = vertexAmount.x * vertexAmount.y * vertexAmount.z;
            int floorSize = vertexAmount.x * vertexAmount.z;
            int frontFaceSize = vertexAmount.x * vertexAmount.y;
            int sideFaceSize = vertexAmount.z * vertexAmount.y;
            
            int preAllocatedSubVertices = preAllocatedBaseVertices + cubeAmountY * floorSize + cubeAmountZ * frontFaceSize + cubeAmountX * sideFaceSize;

            SetupVerticesArrays(vertexAmount);
            Profiler.EndSample();
            
            Profiler.BeginSample("MarchingCubesVisualizer.GetVertexValues");
            getVertexValues(VerticesValuesNative);
            Profiler.EndSample();
            
            Profiler.BeginSample("MarchingCubesVisualizer.SetUpStructuredBuffers");
            SetupTriangleBuffer(amountOfCubes);
            SetupSubVerticesBuffer(preAllocatedSubVertices);
            SetupBaseVerticesValuesBuffer(preAllocatedBaseVertices);
            SetupStaticBuffers();
            Profiler.EndSample();
            
            Profiler.BeginSample("MarchingCubesVisualizer.PrecomputeValues");
            int middleOffset = vertexAmount.x * cubeAmountZ + vertexAmount.z * cubeAmountX;
            int topOffset = vertexAmount.x * vertexAmount.z + middleOffset;
            Profiler.EndSample();
            
            Profiler.BeginSample("MarchingCubesVisualizer.InjectDataIntoComputeShader");
            InjectDataIntoComputeShader(vertexAmount, threshold, useLerp, floorSize, cubeAmountX, cubeAmountY, 
                cubeAmountZ, cubeFloorSize, middleOffset, topOffset, computeShader, amountOfCubes, enforceEmptyBorder);
            Profiler.EndSample();
            
            Profiler.BeginSample("MarchingCubesVisualizer.MarchCubes");
            DispatchComputeShader(amountOfCubes, computeShader);
            Profiler.EndSample();

            Profiler.BeginSample("MarchingCubesVisualizer.GetResults");
                
            Profiler.BeginSample("MarchingCubesVisualizer.CalculateSize");
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
            }

            if (maxTriangles < 0)
            {
                maxTriangles = triangleCount;
            }
            triangleCount = Math.Min(triangleCount, maxTriangles);
            Profiler.EndSample();
            Profiler.BeginSample("MarchingCubesVisualizer.GetTriangles");
            int[] triangleIndices = new int[triangleCount * 3]; // hacky way to get all the items in single array, technically the buffer contains triplets of ints
            _appendedTrianglesBuffer.GetData(triangleIndices, 0, 0,  triangleCount * 3);
            Profiler.EndSample();
            
            Profiler.BeginSample("MarchingCubesVisualizer.GetVertices");
            SubVertices = new Vector3[preAllocatedSubVertices];
            _subVerticesBuffer.GetData(SubVertices, 0, 0, preAllocatedSubVertices);
            Profiler.EndSample();
            
            Profiler.EndSample();
            
            Profiler.BeginSample("MarchingCubesVisualizer.FillMesh");
            Mesh sharedMesh = gridMeshFilter.sharedMesh;
            if (sharedMesh == null)
            {
                sharedMesh = new Mesh
                {
                    indexFormat = SubVertices.Length > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16
                };
                gridMeshFilter.sharedMesh = sharedMesh;
            }

            sharedMesh.indexFormat = SubVertices.Length > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16;
            sharedMesh.MarkDynamic();
            sharedMesh.Clear();
            sharedMesh.vertices = SubVertices;
            sharedMesh.triangles = triangleIndices;
            sharedMesh.RecalculateBounds();
            Profiler.EndSample();
        }

        private void InjectDataIntoComputeShader(Vector3Int vertexAmount, float threshold, bool useLerp, int floorSize, int cubeAmountX, int cubeAmountY, int cubeAmountZ, int cubeFloorSize, int middleOffset,
            int topOffset, ComputeShader computeShader, int amountOfCubes, bool enforceEmptyBorder)
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
            computeShader.SetInt(AmountOfCubes, amountOfCubes);
            computeShader.SetBool(EnforceEmptyBorder, enforceEmptyBorder);
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

        private void SetupBaseVerticesValuesBuffer(int preAllocatedBaseVertices)
        {
            const int sizeOfFloat = sizeof(float);

            if (_baseVerticesValuesBuffer == null || _baseVerticesValuesBuffer.count != preAllocatedBaseVertices)
            {
                _baseVerticesValuesBuffer?.Dispose();
                _baseVerticesValuesBuffer = new ComputeBuffer(preAllocatedBaseVertices, sizeOfFloat);
            }
            
            _baseVerticesValuesBuffer.SetData(VerticesValuesNative);
        }

        private void SetupSubVerticesBuffer(int preAllocatedSubVertices)
        {
            const int sizeOfFloat = sizeof(float);
            
            if (_subVerticesCleaningArray == null || _subVerticesCleaningArray.Length != preAllocatedSubVertices)
            {
                _subVerticesCleaningArray = new float[preAllocatedSubVertices * 3];
            
                _subVerticesBuffer?.Dispose();
                _subVerticesBuffer = new ComputeBuffer(preAllocatedSubVertices, sizeOfFloat * 3);
            }
            
            // Clear the buffer
            _subVerticesBuffer.SetData(_subVerticesCleaningArray);
        }

        private void SetupTriangleBuffer(int amountOfCubes)
        {
            // Define the stride (size) of an integer in bytes
            const int strideInt = sizeof(int);

            // Initialize the AppendStructuredBuffer with an initial capacity
            int preAllocatedTriangles = amountOfCubes * MarchingCubeUtils.MaximumTrianglesPerCube;
            if (_appendedTrianglesBuffer == null || _appendedTrianglesBuffer.count != preAllocatedTriangles)
            {
                _appendedTrianglesBuffer?.Dispose();
                _appendedTrianglesBuffer = new ComputeBuffer(preAllocatedTriangles, strideInt * 3, ComputeBufferType.Append);
            }
            
            _appendedTrianglesBuffer.SetCounterValue(0);
        }

        private void SetupStaticBuffers()
        {
            if (_cubeEdgeFlagsBuffer == null)
            {
                _cubeEdgeFlagsBuffer?.Dispose();
                _cubeEdgeFlagsBuffer = new ComputeBuffer(MarchingCubeUtils.CubeEdgeFlags.Length, sizeof(int));
                _cubeEdgeFlagsBuffer.SetData(MarchingCubeUtils.CubeEdgeFlags);
            }

            if (_triangleConnectionTableBuffer != null)
            {
                return;
            }

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