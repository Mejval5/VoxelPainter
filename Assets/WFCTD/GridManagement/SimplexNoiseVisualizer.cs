using System;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

namespace WFCTD.GridManagement
{
    [ExecuteAlways]
    public class SimplexNoiseVisualizer : MonoBehaviour
    {
        [FormerlySerializedAs("_gridProperties")] [SerializeField] private GenerationProperties _generationProperties;
        [SerializeField] private MeshFilter _gridMeshFilter;        
        
        [field: Range(0.01f,1f)]
        [field: SerializeField] private float Threshold { get; set; } = 0.5f;
        [field: Range(2, 100)]
        [field: SerializeField] private int VertexAmountX { get; set; } = 2;
        [field: Range(2, 100)]
        [field: SerializeField] private int VertexAmountY { get; set; } = 2;
        [field: Range(2, 100)]
        [field: SerializeField] private int VertexAmountZ { get; set; } = 2;

        [SerializeField] private bool _useLerp = true;
        
        [SerializeField] private bool _visualizeBaseVertices;
        [SerializeField] private bool _visualizeSubVertices;
        [SerializeField] private float _gizmoSize = 0.1f;
        [SerializeField] private int _maxTriangles = 100000;
        

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
            for (int i = 0; i < MarchingCubesVisualizer.BaseVertices.Length; i++)
            {
                Vector3 pos = MarchingCubesVisualizer.BaseVertices[i];
                Gizmos.color = Color.Lerp(Color.black, Color.white, MarchingCubesVisualizer.VerticesValues[i]);
                if (MarchingCubesVisualizer.VerticesValues[i] < Threshold)
                {
                    Gizmos.color *= Color.red;
                }
                Gizmos.DrawSphere(pos, _gizmoSize);
            }
        }

#pragma warning disable CS0414 // Field is assigned but its value is never used
        [SerializeField] private bool _regenerateMesh;
#pragma warning restore CS0414 // Field is assigned but its value is never used
        
#if UNITY_EDITOR
        private void OnValidate()
        {
            EditorApplication.delayCall -= Setup;
            EditorApplication.delayCall += Setup;
        }
#endif

        private void Setup()
        {
            _regenerateMesh = false;
            Vector3Int vertexAmount = new (VertexAmountX, VertexAmountY, VertexAmountZ);
            MarchingCubesVisualizer.MarchCubes(_generationProperties, vertexAmount, Threshold, _gridMeshFilter, GetNoiseValue, _maxTriangles, _useLerp);
        }

        private static float GetNoiseValue(int i, Vector3 position, GenerationProperties generationProperties)
        {
            float x = (position.x + generationProperties.Origin.x) * generationProperties.Frequency / 1000f;
            float y = (position.y + generationProperties.Origin.y) * generationProperties.Frequency / 1000f;
            float z = (position.z + generationProperties.Origin.z) * generationProperties.Frequency / 1000f;
            
            return CustomNoiseSimplex(x, y, z);
        }

        private static float CustomNoiseSimplex(float x, float y, float z)
        {
            return Mathf.Clamp01(Mathf.Pow(SimplexNoise.Generate(x, y, z), 2));
        }
    }
}