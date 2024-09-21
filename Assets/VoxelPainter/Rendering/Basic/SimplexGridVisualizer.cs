using UnityEngine;
using UnityEngine.Serialization;
using VoxelPainter.GridManagement;

namespace VoxelPainter.VoxelVisualization
{
    /// <summary>
    /// This class is used to visualize a simplex grid.
    /// Draws the grid and the vertices using GPU instancing.
    /// Doesn't render triangles.
    /// </summary>
    [ExecuteAlways]
    public class SimplexGridVisualizer : MonoBehaviour
    {
        [FormerlySerializedAs("_gridProperties")] [SerializeField] private GridProperties _generationProperties;
        [SerializeField] private MeshFilter _gridMeshFilter;        
        
        [Header("Draw Vertices")]
        [SerializeField] private bool _drawVerticesUsingGpuInstancing = true;
        [SerializeField] private Material _vertexMaterial;
        [SerializeField] private Mesh _vertexMesh;
        [SerializeField] private float _vertexSize = 0.1f;
        [SerializeField] private int _maxVerticesDrawn = 10;
        [SerializeField] private Mesh _vertexMeshInstance;
        [field: SerializeField] private float Threshold { get; set; } = 0.1f;
        
        private SimplexGrid _simplexGrid;
        private Matrix4x4[] _instData;
        
        private void Awake()
        {
            if (_generationProperties == null)
            {
                return;
            }
            
            _simplexGrid = new SimplexGrid(_generationProperties, Threshold);
        }

        private void OnValidate()
        {
            if (_generationProperties == null)
            {
                return;
            }
            
            _simplexGrid ??= new SimplexGrid(_generationProperties, Threshold);

            Vector3[] vertices = _vertexMesh.vertices;

            if (_vertexMeshInstance != null)
            {
                DestroyImmediate(_vertexMeshInstance);
            }
            
            _vertexMeshInstance = new Mesh
            {
                vertices = new Vector3[vertices.Length],
                triangles = _vertexMesh.triangles
            };

            for (int i = 0; i < vertices.Length; ++i)
            {
                vertices[i] *= _vertexSize;
            }
            
            _vertexMeshInstance.vertices = vertices;
            
            _vertexMeshInstance.RecalculateBounds();
            _vertexMeshInstance.RecalculateNormals();
            _vertexMeshInstance.RecalculateTangents();

            _simplexGrid.UpdateGridProperties(_generationProperties, Threshold);
        }

        private void DrawVerticesUsingGpuInstancing()
        {
            if (_simplexGrid == null)
            {
                return;
            }
            
            if (_vertexMaterial == null)
            {
                return;
            }
            
            if (_vertexMesh == null)
            {
                return;
            }
            
            if (_simplexGrid.GridMesh == null)
            {
                return;
            }
            
            if (_simplexGrid.GridMesh.vertices.Length == 0)
            {
                return;
            }

            int maxVertices = _maxVerticesDrawn <= 0 ? int.MaxValue : _maxVerticesDrawn;
            int vertexCount = Mathf.Min(_simplexGrid.GridMesh.vertices.Length, maxVertices);

            RenderParams rp = new(_vertexMaterial);
            if (_instData == null || _instData.Length != vertexCount)
            {
                _instData = new Matrix4x4[vertexCount];
            }
            
            if (_instData.Length == 0)
            {
                return;
            }
            
            for (int i = 0; i < vertexCount; ++i)
            {
                Vector3 vertex = _simplexGrid.GridMesh.vertices[i];
                _instData[i] = Matrix4x4.Translate(transform.TransformPoint(vertex));
            }

            Graphics.RenderMeshInstanced(rp, _vertexMeshInstance, 0, _instData);
        }

        private void Update()
        {
            if (_simplexGrid == null)
            {
                return;
            }

            if (_gridMeshFilter == null)
            {
                return;
            }

            _gridMeshFilter.mesh = _simplexGrid.GridMesh;
            
            if (_drawVerticesUsingGpuInstancing)
            {
                DrawVerticesUsingGpuInstancing();
            }
        }
    }
}