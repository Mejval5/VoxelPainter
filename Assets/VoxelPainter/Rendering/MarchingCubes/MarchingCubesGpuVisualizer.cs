using System;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using VoxelPainter.Utils;
using VoxelPainter.VoxelVisualization;

namespace VoxelPainter.Rendering.MarchingCubes
{
    public class MarchingCubesGpuVisualizer : IMarchingCubesVisualizer
    {
        private const string ComputeShaderProgram = "CSMain";
        private static bool CleanSubVerticesBuffer => false;
        
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
        
        public Vector3[] SubVertices { get; private set; }
        
        private int[] _triangleIndices;

        private NativeArray<float> _verticesValuesNative;
        private NativeArray<Vector3> _baseVerticesNative;
        
        private ComputeBuffer _appendedTrianglesBuffer;
        private ComputeBuffer _subVerticesBuffer;
        private ComputeBuffer _baseVerticesValuesBuffer;
        private ComputeBuffer _cubeEdgeFlagsBuffer;
        private ComputeBuffer _triangleConnectionTableBuffer;

        private Array _subVerticesCleaningArray;
                
        public void GetBaseVerticesNative(ref NativeArray<Vector3> vertices, Vector3Int vertexAmount)
        {
            SetupVerticesArrays(vertexAmount);
            vertices = _baseVerticesNative;
        }
        
        public void GetVerticesValuesNative(ref NativeArray<float> verticesValues, Vector3Int vertexAmount)
        {
            SetupVerticesArrays(vertexAmount);
            verticesValues = _verticesValuesNative;
        }

        private void SetupVerticesArrays(Vector3Int vertexAmount)
        {
            int preAllocatedBaseVertices = vertexAmount.x * vertexAmount.y * vertexAmount.z;
            bool recalculateVertexValues = _verticesValuesNative.IsCreated == false || preAllocatedBaseVertices != _verticesValuesNative.Length;
            if (!recalculateVertexValues)
            {
                return;
            }

            _baseVerticesNative = new NativeArray<Vector3>(preAllocatedBaseVertices, Allocator.Persistent);
            _verticesValuesNative = new NativeArray<float>(preAllocatedBaseVertices, Allocator.Persistent);
            Profiler.BeginSample("MarchingCubesVisualizer.SetUpVertices");
            int floorSize = vertexAmount.x * vertexAmount.z;
            for (int i = 0; i < preAllocatedBaseVertices; i++)
            {
                Vector3Int pos = Vector3Int.zero;
                pos.x = i % vertexAmount.x;
                pos.z  = (i % floorSize) / vertexAmount.x;
                pos.y  = i / floorSize;

                _baseVerticesNative[i] = pos;
            }
            Profiler.EndSample();
        }
            
        public void MarchCubes(
            Vector3Int vertexAmount, 
            float threshold, 
            MeshFilter gridMeshFilter,
            Action<NativeArray<float>> getVertexValues,
            ComputeShader computeShader,
            int maxTriangles = int.MaxValue,
            bool useLerp = true,
            bool enforceEmptyBorder = true)
        {
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
            getVertexValues(_verticesValuesNative);
            Profiler.EndSample();
            
            Profiler.BeginSample("MarchingCubesVisualizer.SetupTriangleBuffer");
            SetupTriangleBuffer(amountOfCubes);
            Profiler.EndSample();
            
            Profiler.BeginSample("MarchingCubesVisualizer.SetupSubVerticesBuffer");
            SetupSubVerticesBuffer(preAllocatedSubVertices);
            Profiler.EndSample();
            
            Profiler.BeginSample("MarchingCubesVisualizer.SetupBaseVerticesValuesBuffer");
            SetupBaseVerticesValuesBuffer(preAllocatedBaseVertices);
            Profiler.EndSample();
            
            Profiler.BeginSample("MarchingCubesVisualizer.SetupStaticBuffers");
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
                
            Profiler.BeginSample("MarchingCubesVisualizer.GetDataAndRender");
            GetDataAndRender(vertexAmount, gridMeshFilter, maxTriangles, preAllocatedSubVertices);
            Profiler.EndSample();
        }

        private void GetDataAndRender(Vector3Int vertexAmount, MeshFilter gridMeshFilter, int maxTriangles, int preAllocatedSubVertices)
        {
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
            int triangleBufferSize = triangleCount * 3;
            if (_triangleIndices == null || _triangleIndices.Length != triangleBufferSize)
            {
                _triangleIndices = new int[triangleBufferSize];
            }
            
            // The buffer contains triplets of ints, but we can inject it into a flat int array
            _appendedTrianglesBuffer.GetData(_triangleIndices, 0, 0,  triangleCount * 3);
            Profiler.EndSample();
            
            Profiler.BeginSample("MarchingCubesVisualizer.GetVertices");
            if (SubVertices == null || SubVertices.Length != preAllocatedSubVertices)
            {
                SubVertices = new Vector3[preAllocatedSubVertices];
            }

            _subVerticesBuffer.GetData(SubVertices, 0, 0, preAllocatedSubVertices);
            Profiler.EndSample();

            Profiler.BeginSample("MarchingCubesVisualizer.FillMesh.SetupSharedMesh");
            Mesh sharedMesh = gridMeshFilter.sharedMesh;
            if (sharedMesh == null)
            {
                sharedMesh = new Mesh
                {
                    indexFormat = SubVertices.Length > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16
                };
                gridMeshFilter.sharedMesh = sharedMesh;
            }
            Profiler.EndSample();

            Profiler.BeginSample("MarchingCubesVisualizer.SetupSharedMeshFormat");
            sharedMesh.indexFormat = SubVertices.Length > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16;
            sharedMesh.MarkDynamic();
            sharedMesh.Clear();
            Profiler.EndSample();
            
            Profiler.BeginSample("MarchingCubesVisualizer.FillMeshVertices");
            VertexAttributeDescriptor[] vertexAttributeDescriptors = { new (VertexAttribute.Position) };
            sharedMesh.SetVertexBufferParams(preAllocatedSubVertices, vertexAttributeDescriptors);
            sharedMesh.SetVertexBufferData(SubVertices, 0, 0, preAllocatedSubVertices, 0, MeshUpdateFlags.DontValidateIndices );
            Profiler.EndSample();
            
            Profiler.BeginSample("MarchingCubesVisualizer.FillMeshTriangles");
            sharedMesh.SetIndexBufferParams(triangleBufferSize, IndexFormat.UInt32);
            sharedMesh.SetIndexBufferData(_triangleIndices, 0, 0, triangleBufferSize, MeshUpdateFlags.DontValidateIndices);
            Profiler.EndSample();
            
            Profiler.BeginSample("MarchingCubesVisualizer.FillMesh.RecalculateBounds");
            Vector3 vertexSize = vertexAmount;
            sharedMesh.bounds = new Bounds(vertexSize / 2f, vertexAmount);
            Profiler.EndSample();
            
            Profiler.BeginSample("MarchingCubesVisualizer.FillMesh.SetSubMesh");
            sharedMesh.subMeshCount = 1;
            sharedMesh.SetSubMesh(0, new SubMeshDescriptor(0, triangleBufferSize, MeshTopology.Triangles));
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
            
            _baseVerticesValuesBuffer.SetData(_verticesValuesNative);
        }

        private void SetupSubVerticesBuffer(int preAllocatedSubVertices)
        {
            const int sizeOfFloat = sizeof(float);
            
            Profiler.BeginSample("MarchingCubesVisualizer.SetupSubVerticesBuffer");
            if (_subVerticesBuffer == null || _subVerticesBuffer.count != preAllocatedSubVertices)
            {
                _subVerticesBuffer?.Dispose();
                _subVerticesBuffer = new ComputeBuffer(preAllocatedSubVertices, sizeOfFloat * 3);
            }
            Profiler.EndSample();
                        
            // Clear the buffer
            if (!CleanSubVerticesBuffer)
            {
                return;
            }

            Profiler.BeginSample("MarchingCubesVisualizer.ClearBuffer");
            int cleaningArraySize = preAllocatedSubVertices * 3;
            if (_subVerticesCleaningArray == null || _subVerticesCleaningArray.Length != cleaningArraySize)
            {
                _subVerticesCleaningArray = new float[cleaningArraySize];
            }

            _subVerticesBuffer.SetData(_subVerticesCleaningArray);
            Profiler.EndSample();
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
            
            _verticesValuesNative.Dispose();
            _baseVerticesNative.Dispose();
        }
    }
}