using System;
using Foxworks.Utils;
using Unity.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
using VoxelPainter.GridManagement;
using VoxelPainter.Rendering.MarchingCubes;

namespace VoxelPainter.VoxelVisualization
{
    /// <summary>
    /// Base class for the Marching Cubes renderer.
    /// This class is used to generate the mesh using the Marching Cubes algorithm.
    /// It can be used to generate the mesh using the CPU or the GPU.
    /// The GPU implementation is faster but it requires a compute shader.
    /// The CPU implementation is slower but it doesn't require a compute shader.
    /// </summary>
    [ExecuteAlways]
    public abstract class MarchingCubeRendererBase : MonoBehaviour
    {
        [SerializeField] private bool _useLerp = true;
        [SerializeField] private bool _visualizeBaseVertices;
        [SerializeField] private bool _visualizeSubVertices;
        [SerializeField] private float _gizmoSize = 0.075f;
        [SerializeField] private int _maxTriangles = -1;
        
        protected MarchingCubesCpuVisualizer _marchingCubesCpuVisualizer;
        protected MarchingCubesGpuVisualizer _marchingCubesGpuVisualizer;
        
        protected IMarchingCubesVisualizer MarchingCubesVisualizer { get; private set; }

        [field: SerializeField] public bool UseGpu { get; private set; } = true;
        [field: SerializeField] public ComputeShader MarchingCubeComputeShader { get; private set; }
        
        [field: SerializeField] public MeshFilter GridMeshFilter { get; private set; }
        [field: Range(0.01f,1f)]
        [field: SerializeField] public float Threshold { get; private set; } = 0.5f;
        [field: Range(2, 300)]
        [field: SerializeField] public int VertexAmountX { get; private set; } = 50;
        [field: Range(2, 300)]
        [field: SerializeField] public int VertexAmountY { get; private set; } = 50;
        [field: Range(2, 300)]
        [field: SerializeField] public int VertexAmountZ { get; private set; } = 50;
        [field: SerializeField]  public virtual bool EnforceEmptyBorder { get; private set; } = true;
        
        public Vector3Int VertexAmount => new (VertexAmountX, VertexAmountY, VertexAmountZ);
        public bool AreComputeShadersSupported => SystemInfo.supportsComputeShaders;
        
        public event Action MeshGenerated = delegate { };

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (_visualizeBaseVertices)
            {
                VisualizeBaseVertices();
            }
            
            if (_visualizeSubVertices)
            {
                VisualizeSubVertices();
            }
        }

        /// <summary>
        /// Visualizes the sub vertices.
        /// </summary>
        private void VisualizeSubVertices()
        {
            for (int i = 0; i < MarchingCubesVisualizer.SubVertices.Length; i++)
            {
                Vector3 pos = MarchingCubesVisualizer.SubVertices[i];
                if (pos == Vector3.zero)
                {
                    continue;
                }
                Gizmos.color = Color.green;
                Gizmos.DrawSphere(pos, _gizmoSize);
                Handles.Label(pos + Vector3.down * 0.1f, i.ToString());
            }
        }

        /// <summary>
        /// Visualizes the base vertices.
        /// </summary>
        private void VisualizeBaseVertices()
        {
            NativeArray<Vector3> vertices = new();
            NativeArray<float> verticesValues = new();
            MarchingCubesVisualizer.GetBaseVerticesNative(ref vertices, VertexAmount);
            MarchingCubesVisualizer.GetVerticesValuesNative(ref verticesValues, VertexAmount);
            
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 pos = vertices[i];
                Gizmos.color = Color.Lerp(Color.black, Color.white, verticesValues[i]);
                if (verticesValues[i] < Threshold)
                {
                    Gizmos.color *= Color.red;
                }
                Gizmos.DrawSphere(pos, _gizmoSize);
            }
        }

        protected void OnValidate()
        {
            if (gameObject.activeInHierarchy == false)
            {
                return;
            }

            EditorApplication.delayCall -= GenerateMesh;
            EditorApplication.delayCall += GenerateMesh;
        }
#endif

        protected virtual void OnEnable()
        {
            if (UseGpu)
            {
                _marchingCubesGpuVisualizer ??= new MarchingCubesGpuVisualizer();
                MarchingCubesVisualizer = _marchingCubesGpuVisualizer;
            }
            else
            {
                _marchingCubesCpuVisualizer ??= new MarchingCubesCpuVisualizer();
                MarchingCubesVisualizer = _marchingCubesCpuVisualizer;
            }
            
            Debug.Log("AreComputeShadersSupported: " + AreComputeShadersSupported);
        }

        public virtual void GenerateMesh()
        {
            if (GridMeshFilter == null)
            {
                return;
            }
            
            Vector3Int vertexAmount = new (VertexAmountX, VertexAmountY, VertexAmountZ);
            
            Profiler.BeginSample("MarchCubes");
            if (UseGpu && AreComputeShadersSupported)
            {
                _marchingCubesGpuVisualizer ??= new MarchingCubesGpuVisualizer();
                MarchingCubesVisualizer = _marchingCubesGpuVisualizer;
                _marchingCubesGpuVisualizer.MarchCubes(vertexAmount, Threshold, GridMeshFilter, 
                    GetVertexValues, MarchingCubeComputeShader, _maxTriangles, _useLerp, EnforceEmptyBorder);
            }
            else
            {
                _marchingCubesCpuVisualizer ??= new MarchingCubesCpuVisualizer();
                MarchingCubesVisualizer = _marchingCubesCpuVisualizer;
                _marchingCubesCpuVisualizer.MarchCubes(vertexAmount, Threshold, GridMeshFilter, 
                    GetVertexValues, _maxTriangles, _useLerp, EnforceEmptyBorder);
            }
            Profiler.EndSample();
            
            Profiler.BeginSample("MeshGenerated");
            MeshGenerated.InvokeAndLogExceptions();
            Profiler.EndSample();
        }

        /// <summary>
        /// This method is used to get the vertices values.
        /// You can leave it empty if you don't need to set the vertices values or set them directly using the GetBaseVerticesNative method.
        /// </summary>
        /// <param name="verticesValues"></param>
        public abstract void GetVertexValues(NativeArray<float> verticesValues);

        protected virtual void OnDestroy()
        {
            _marchingCubesGpuVisualizer?.ReleaseBuffers();
            _marchingCubesCpuVisualizer?.ReleaseBuffers();
        }
    }
}