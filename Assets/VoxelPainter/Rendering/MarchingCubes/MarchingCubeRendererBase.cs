using System;
using System.Collections.Generic;
using System.Linq;
using Foxworks.Utils;
using Foxworks.Voxels;
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
    /// The GPU implementation is faster, but it requires a compute shader.
    /// The CPU implementation is slower, but it doesn't require a compute shader.
    /// </summary>
    [ExecuteAlways]
    public abstract class MarchingCubeRendererBase : MonoBehaviour
    {
        [SerializeField] private bool _updateEveryFrame;
        
        [SerializeField] private bool _lerp = true;
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
        
        [Range(0.01f,1f)] [SerializeField] public float _threshold  = 0.5f;
        
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
        
        public event Action<float> ThresholdChanged = delegate { };
        public event Action<bool> LerpChanged = delegate { };
        
        public bool Lerp
        {
            set
            {
                _lerp = value;
                LerpChanged.Invoke(value);
            }
            get => _lerp;
        }
        
        public float Threshold
        {
            set
            {
                ThresholdChanged.Invoke(value);
                _threshold = value;
            }
            get => _threshold;
        }

        protected virtual void Update()
        {
            if (_updateEveryFrame && Application.isEditor)
            {
                GenerateMesh();
            }
        }

#if UNITY_EDITOR
        protected void OnDrawGizmos()
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
            Dictionary<int, Vector3> positions = new ();
            for (int i = 0; i < MarchingCubesVisualizer.SubVertices.Length; i++)
            {
                positions[i] = MarchingCubesVisualizer.SubVertices[i];
            }
            IOrderedEnumerable<KeyValuePair<int, Vector3>> sortedPositions = positions.OrderByDescending(pair => Vector3.Distance(Camera.current.transform.position, pair.Value));
            positions = sortedPositions.ToDictionary(pair => pair.Key, pair => pair.Value);
            for (int i = 0; i < positions.Count; i++)
            {
                Vector3 pos = positions.ElementAt(i).Value;
                if (pos == Vector3.zero)
                {
                    continue;
                }
                Gizmos.color = Color.green;
                Gizmos.DrawSphere(pos, _gizmoSize);
                Handles.Label(pos + Vector3.down * 0.1f, positions.ElementAt(i).Key.ToString());
            }
        }

        /// <summary>
        /// Visualizes the base vertices.
        /// </summary>
        private void VisualizeBaseVertices()
        {
            NativeArray<Vector3> vertices = new();
            NativeArray<int> verticesValues = new();
            MarchingCubesVisualizer.GetBaseVerticesNative(ref vertices, VertexAmount);
            MarchingCubesVisualizer.GetVerticesValuesNative(ref verticesValues, VertexAmount);
            
            Dictionary<int, Vector3> positions = new ();
            for (int i = 0; i < vertices.Length; i++)
            {
                positions[i] = vertices[i];
            }
            IOrderedEnumerable<KeyValuePair<int, Vector3>> sortedPositions = positions.OrderByDescending(pair => Vector3.Distance(Camera.current.transform.position, pair.Value));
            positions = sortedPositions.ToDictionary(pair => pair.Key, pair => pair.Value);
            
            for (int i = 0; i < positions.Count; i++)
            {
                Vector3 pos = positions.ElementAt(i).Value;
                float val = VoxelDataUtils.UnpackValue(verticesValues[positions.ElementAt(i).Key]);
                Gizmos.color = Color.Lerp(Color.black, Color.white, val);
                if (val < Threshold)
                {
                    Gizmos.color *= Color.red;
                }
                Gizmos.DrawSphere(pos, _gizmoSize);
            }
        }

        protected virtual void OnValidate()
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
            if (SystemInfo.supportsComputeShaders == false && UseGpu)
            {
                Debug.LogWarning("Compute shaders are not supported on this device. Falling back to CPU implementation.");
                UseGpu = false;
            }
            
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
                    GetVertexValues, MarchingCubeComputeShader, _maxTriangles, _lerp, EnforceEmptyBorder);
            }
            else
            {
                _marchingCubesCpuVisualizer ??= new MarchingCubesCpuVisualizer();
                MarchingCubesVisualizer = _marchingCubesCpuVisualizer;
                _marchingCubesCpuVisualizer.MarchCubes(vertexAmount, Threshold, GridMeshFilter, 
                    GetVertexValues, _maxTriangles, _lerp, EnforceEmptyBorder);
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
        public abstract void GetVertexValues(NativeArray<int> verticesValues);

        protected virtual void OnDestroy()
        {
            _marchingCubesGpuVisualizer?.ReleaseBuffers();
            _marchingCubesCpuVisualizer?.ReleaseBuffers();
        }
    }
}