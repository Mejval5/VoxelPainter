
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;

namespace WFCTD.GridManagement
{
    public class MarchingCubesVisualizer : MonoBehaviour
    {
        [SerializeField] private GridProperties _gridProperties;
        [SerializeField] private MeshFilter _gridMeshFilter;        
        
        [field: SerializeField] private float Threshold { get; set; } = 0.5f;
        [field: SerializeField] private Vector3Int CubeAmount { get; set; } = Vector3Int.one;
      
#if UNITY_EDITOR
        private void OnValidate()
        {
            EditorApplication.delayCall -= Setup;
            EditorApplication.delayCall += Setup;
        }
#endif
        
        private Vector3[] _vertices;
        private Vector3[] _normals;
        private int[] _triangles;
        
        private void Setup()
        {
            if (_gridProperties == null)
            {
                return;
            }
            
            Profiler.BeginSample("MarchingCubesVisualizer.Setup");

            int preAllocatedVertices = CubeAmount.x * CubeAmount.y * CubeAmount.z * MarchingCubeUtils.CubeEdgesCount;
            if (_vertices == null || preAllocatedVertices != _vertices.Length)
            {
                _vertices = new Vector3[preAllocatedVertices];
            }
            
            int preAllocatedNormals = preAllocatedVertices;
            if (_normals == null || preAllocatedNormals != _normals.Length)
            {
                _normals = new Vector3[preAllocatedNormals];
            }
            
            int preAllocatedTriangles = preAllocatedVertices * 3;
            if (_triangles == null || preAllocatedTriangles != _triangles.Length)
            {
                _triangles = new int[preAllocatedTriangles];
                Array.Fill(_triangles, -1);
            }
            Profiler.EndSample();
            
            Profiler.BeginSample("MarchingCubesVisualizer.MarchCubes");
            Vector3 scale = _gridProperties.Scale;
            int index = 0;
            const float maxValueOfInt = int.MaxValue;
            Cube cube = new ();
            cube.Corners ??= new GridPoint[MarchingCubeUtils.CubeCornersCount];
            
            for (int x = 0; x < CubeAmount.x; x++)
            {
                for (int y = 0; y < CubeAmount.y; y++)
                {
                    for (int z = 0; z < CubeAmount.z; z++)
                    {
                        for (int i = 0; i < MarchingCubeUtils.CubeCornersCount; i++)
                        {
                            cube.Corners[i].position.x = (MarchingCubeUtils.CubeCornersPositions[i, 0] + x) * scale.x;
                            cube.Corners[i].position.y = (MarchingCubeUtils.CubeCornersPositions[i, 1] + y) * scale.y;
                            cube.Corners[i].position.z = (MarchingCubeUtils.CubeCornersPositions[i, 2] + z) * scale.z;
                            cube.Corners[i].value = Mathf.Abs(cube.Corners[i].position.GetHashCode() / maxValueOfInt);
                        }

                        MarchingCubeUtils.GetMarchedCube(cube, scale, Threshold, _vertices, _triangles, _normals, index);
                        
                        index += MarchingCubeUtils.CubeEdgesCount;
                    }
                }
            }
            
            Profiler.EndSample();
            Profiler.BeginSample("MarchingCubesVisualizer.AssignMesh");

            Mesh mesh = new()
            {
                indexFormat = _vertices.Length > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16,
                vertices = _vertices,
                triangles = _triangles.Where(value => value != -1).ToArray(),
                normals = _normals
            };
            
            Profiler.EndSample();
            
            Profiler.BeginSample("MarchingCubesVisualizer.RecalculateMesh");
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();
            Profiler.EndSample();
            
            Profiler.BeginSample("MarchingCubesVisualizer.AssignMesh");
            _gridMeshFilter.sharedMesh = mesh;
            _gridMeshFilter.sharedMesh.hideFlags = HideFlags.DontSave;
            Profiler.EndSample();
        }

        private float CustomNoiseSimplex(Vector3 position)
        {
            return Mathf.Clamp01(Mathf.Pow(SimplexNoise.Generate(position / _gridProperties.Frequency), 2));
        }
    }
}