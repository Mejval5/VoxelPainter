﻿using System;
using System.Collections.Generic;
using System.Linq;
using Foxworks.Voxels;
using Unity.Collections;
using UnityEditor;
using UnityEngine;
using VoxelPainter.GridManagement;

namespace VoxelPainter.VoxelVisualization
{
    /// <summary>
    /// This class is used to visualize a single cube.
    /// </summary>
    [ExecuteAlways]
    public class OneMarchedCubeVisualizer : MonoBehaviour
    {
        [Range(0.01f, 0.99f)] [SerializeField] private float _surface;

        [field: SerializeField] public Color ActiveColor { get; private set; }
        [field: SerializeField] public Color InactiveColor { get; private set; }
        [field: SerializeField] public Color SelectionColor { get; private set; }
        [field: SerializeField] public float GizmoSize { get; private set; } = 0.5f;

        [field: SerializeField] public Cube Cube { get; private set; }

        [SerializeField] private MeshFilter _meshFilter;

        private MarchingCubesCpuVisualizer _marchingCubesCpuVisualizer;

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

        private void OnDestroy()
        {
            _marchingCubesCpuVisualizer?.ReleaseBuffers();
        }

        private void Start()
        {
            UpdateMesh();
        }

        private void Update()
        {
            UpdateMesh();
        }

        private void UpdateMesh()
        {
            _regenerateMesh = false;
         
            _marchingCubesCpuVisualizer ??= new MarchingCubesCpuVisualizer();
            
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
            _marchingCubesCpuVisualizer.MarchCubes(vertexAmount, _surface, _meshFilter, GetVertexValues, enforceEmptyBorder: false);
        }
        
        private void GetVertexValues(NativeArray<int> verticesValues)
        {
            verticesValues.CopyFrom(Cube.Corners.Select(corner => VoxelDataUtils.PackValueAndVertexColor(corner.value)).ToArray());
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

            private void OnDestroy()
            {
                SceneView.duringSceneGui -= OnScene;
            }

            private void OnScene(SceneView sceneView)
            {
                if (_visualizer.gameObject.activeInHierarchy == false)
                {
                    return;
                }
                
                Dictionary<int, Vector3> positions = new ();
                for (int i = 0; i < MarchingCubeUtils.CornersPerCube; i++)
                {
                    positions[i] = GetCornerPosition(i);
                }
                IOrderedEnumerable<KeyValuePair<int, Vector3>> sortedPositions = positions.OrderByDescending(pair => Vector3.Distance(Camera.current.transform.position, pair.Value));
                
                foreach (KeyValuePair<int, Vector3> pair in sortedPositions)
                {
                    DrawCorner(pair.Key);
                }

                if (Event.current.type == EventType.MouseMove)
                    HandleUtility.Repaint();
            }
            
            private Vector3 GetCornerPosition(int index)
            {
                return _visualizer.transform.position + _visualizer.Cube.Corners[index].position;
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