
using System;
using Unity.Collections;
using UnityEditor;
using UnityEngine;

namespace WFCTD.GridManagement
{
    public abstract class MarchingCubeRendererBase : MonoBehaviour
    {
        [field: SerializeField] public GenerationProperties GenerationProperties { get; private set; }
        [SerializeField] private bool _useLerp = true;
        [SerializeField] private bool _visualizeBaseVertices;
        [SerializeField] private bool _visualizeSubVertices;
        [SerializeField] private float _gizmoSize = 0.075f;
        [SerializeField] private int _maxTriangles = -1;
        
        protected MarchingCubesCpuVisualizer _marchingCubesCpuVisualizer;
        protected MarchingCubesGpuVisualizer _marchingCubesGpuVisualizer;
        
        protected IMarchingCubesVisualizer MarchingCubesVisualizer { get; private set; }
        
        [field: SerializeField] public bool UseGpu { get; private set; }
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

        private void VisualizeBaseVertices()
        {
            for (int i = 0; i < MarchingCubesVisualizer.ReadOnlyBaseVertices.Length; i++)
            {
                Vector3 pos = MarchingCubesVisualizer.ReadOnlyBaseVertices[i];
                Gizmos.color = Color.Lerp(Color.black, Color.white, MarchingCubesVisualizer.ReadOnlyVerticesValuesNative[i]);
                if (MarchingCubesVisualizer.ReadOnlyVerticesValuesNative[i] < Threshold)
                {
                    Gizmos.color *= Color.red;
                }
                Gizmos.DrawSphere(pos, _gizmoSize);
            }
        }

        private void OnValidate()
        {
            EditorApplication.delayCall -= GenerateMesh;
            EditorApplication.delayCall += GenerateMesh;
        }

        protected virtual void GenerateMesh()
        {
            if (GridMeshFilter == null)
            {
                return;
            }
            
            Vector3Int vertexAmount = new (VertexAmountX, VertexAmountY, VertexAmountZ);
            
            if (UseGpu)
            {
                _marchingCubesGpuVisualizer ??= new MarchingCubesGpuVisualizer();
                MarchingCubesVisualizer = _marchingCubesGpuVisualizer;
                _marchingCubesGpuVisualizer.MarchCubes(GenerationProperties, vertexAmount, Threshold, GridMeshFilter, 
                    GetVertexValues, MarchingCubeComputeShader, _maxTriangles, _useLerp, EnforceEmptyBorder);
            }
            else
            {
                _marchingCubesCpuVisualizer ??= new MarchingCubesCpuVisualizer();
                MarchingCubesVisualizer = _marchingCubesCpuVisualizer;
                _marchingCubesCpuVisualizer.MarchCubes(GenerationProperties, vertexAmount, Threshold, GridMeshFilter, 
                    GetVertexValues, _maxTriangles, _useLerp, EnforceEmptyBorder);
            }
            
        }

        public abstract void GetVertexValues(NativeArray<float> verticesValues);

        protected void OnDestroy()
        {
            _marchingCubesGpuVisualizer?.ReleaseBuffers();
            _marchingCubesCpuVisualizer?.ReleaseBuffers();
        }
    }
}