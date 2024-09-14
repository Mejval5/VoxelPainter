
using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;

namespace WFCTD.GridManagement
{
    [ExecuteAlways]
    public class OneMarchedCubeVisualizer : MonoBehaviour
    {
        [SerializeField] private float _surface;

        [field: SerializeField] public Color ActiveColor { get; private set; }
        [field: SerializeField] public Color InactiveColor { get; private set; }
        [field: SerializeField] public float GizmoSize { get; private set; } = 0.5f;

        [field: SerializeField] public Cube Cube { get; private set; }

        [SerializeField] private MeshFilter _meshFilter;

        private MarchingCubesVisualizer _marchingCubesVisualizer;

#pragma warning disable CS0414 // Field is assigned but its value is never used
        [SerializeField] private bool _regenerateMesh;
#pragma warning restore CS0414 // Field is assigned but its value is never used

#if UNITY_EDITOR
        private void OnValidate()
        {
            EditorApplication.delayCall -= UpdateMesh;
            EditorApplication.delayCall += UpdateMesh;
        }
#endif

        private void Start()
        {
            UpdateMesh();
        }

        private void UpdateMesh()
        {
            _regenerateMesh = false;
         
            _marchingCubesVisualizer ??= new MarchingCubesVisualizer();
            
            if (Cube == null || Cube.Corners.Length != MarchingCubeUtils.CornersPerCube)
            {
                Cube = new Cube
                {
                    Corners = new GridPoint[MarchingCubeUtils.CornersPerCube]
                };
                
                for (int i = 0; i < MarchingCubeUtils.CornersPerCube; i++)
                {
                    int x = i % 2;
                    int z = (i % 4) / 2;
                    int y = i / 4;
                    Cube.Corners[i].position = new Vector3(x, y, z);
                }
            }
            
            Vector3Int vertexAmount = new (2, 2, 2);
            GenerationProperties generationProperties = new ();
            _marchingCubesVisualizer.MarchCubes(generationProperties, vertexAmount, _surface, _meshFilter, GetValue);
        }
        
        private float GetValue(int i, Vector3 position, GenerationProperties generationProperties)
        {
            return Cube.Corners[i].value;
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
                
                for (int i = 0; i < MarchingCubeUtils.CornersPerCube; i++)
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