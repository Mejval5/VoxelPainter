
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace WFCTD.GridManagement
{
    [ExecuteAlways]
    public class OneMarchedCubeVisualizer : MonoBehaviour
    {
        [SerializeField] private float _surface;

        [field: SerializeField] public Vector3 Scale { get; private set; } = Vector3.one;
        [field: SerializeField] public Color ActiveColor { get; private set; }
        [field: SerializeField] public Color InactiveColor { get; private set; }
        [field: SerializeField] public float GizmoSize { get; private set; } = 0.5f;

        [field: SerializeField] public Cube Cube { get; private set; }

        [SerializeField] private MeshFilter _meshFilter;
        
#if UNITY_EDITOR
        private void OnValidate()
        {
            EditorApplication.delayCall -= Setup;
            EditorApplication.delayCall += Setup;
        }
#endif

        private void Start()
        {
            Setup();
        }

        private void Setup()
        {
            Cube = new Cube
            {
                Corners = new GridPoint[MarchingCubeUtils.CubeCornersCount]
            };

            for (int i = 0; i < MarchingCubeUtils.CubeCornersCount; i++)
            {
                float x = MarchingCubeUtils.CubeCornersPositions[i, 0] * Scale.x;
                float y = MarchingCubeUtils.CubeCornersPositions[i, 1] * Scale.y;
                float z = MarchingCubeUtils.CubeCornersPositions[i, 2] * Scale.z;
                Cube.Corners[i].position = new Vector3(x, y, z);
            }

            UpdateMesh();
        }

        private void UpdateMesh()
        {
            const int index = 0;
            Vector3[] vertices = new Vector3[MarchingCubeUtils.CubeEdgesCount];
            int[] triangles = new int[MarchingCubeUtils.CubeEdgesCount * 3];
            Vector3[] normals = new Vector3[MarchingCubeUtils.CubeEdgesCount];
            
            Array.Fill(triangles, -1);
            
            if (MarchingCubeUtils.GetMarchedCube(Cube, Scale, _surface, vertices, triangles, normals, index))
            {
                _meshFilter.sharedMesh = null;
                return;
            }

            Mesh mesh = new()
            {
                vertices = vertices,
                triangles = triangles.Where(value => value != -1).ToArray(),
                normals = normals
            };
            
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();
            _meshFilter.sharedMesh = mesh;
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(OneMarchedCubeVisualizer)), CanEditMultipleObjects]
        public sealed class OneMarchedCubeVisualizerEditor : Editor
        {
            private OneMarchedCubeVisualizer _visualizer;

            private void OnEnable()
            {
                if (_visualizer == null)
                {
                    _visualizer = (OneMarchedCubeVisualizer)target;
                }

                SceneView.duringSceneGui -= OnScene;
                SceneView.duringSceneGui += OnScene;
            }

            private void OnScene(SceneView sceneView)
            {
                if (_visualizer.gameObject.activeInHierarchy == false)
                {
                    return;
                }
                
                for (int i = 0; i < MarchingCubeUtils.CubeCornersCount; i++)
                {
                    DrawCorner(i);
                }

                if (Event.current.type == EventType.MouseMove)
                    HandleUtility.Repaint();
            }

            private void DrawCorner(int index)
            {
                bool isCornerActive = Mathf.Approximately(_visualizer.Cube.Corners[index].value, 1f);
                Color color = isCornerActive ? _visualizer.ActiveColor : _visualizer.InactiveColor;
                Handles.color = color;
                Vector3 position = _visualizer.transform.position + _visualizer.Cube.Corners[index].position;
                float size = _visualizer.GizmoSize;
                bool pressed = Handles.Button(position, Quaternion.identity, size, size / 2f, Handles.SphereHandleCap);
                if (pressed)
                {
                    OnCornerClicked(index);
                }
            }

            private void OnCornerClicked(int index)
            {
                _visualizer.Cube.Corners[index].value = _visualizer.Cube.Corners[index].value == 0f ? 1f : 0f;
                _visualizer.UpdateMesh();
            }
        }
#endif
    }
}