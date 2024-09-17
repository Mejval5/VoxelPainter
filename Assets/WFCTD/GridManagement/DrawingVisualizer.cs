using System;
using Unity.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;

namespace WFCTD.GridManagement
{
    public struct HitMeshInfo
    {
        public Vector3 HitPoint;
        public int VertexIndex;
        public bool IsHit;
    }

    [ExecuteAlways]
    public class DrawingVisualizer : MarchingCubeRendererBase
    {
        [SerializeField] private bool _allowRegeneration = false;
        [field: Range(0.1f, 10f)]
        [field: SerializeField]
        public float InitScale { get; set; } = 2f;
        [Range(1f, 100f)] [SerializeField] private float _mouseRadius = 5f;
        [Range(0f, 1f)] [SerializeField] private float _valueToAdd = 1f;
        [SerializeField] private bool _draw = true;
        [Range(0f, 0.5f)] [SerializeField] private float _offsetOfSphereDraw = 0.3f;
        [Range(0f, 5f)] [SerializeField] private float _fuzziness = 2f;
        [Range(10f, 500f)] [SerializeField] private float _timeDrawDelayMs = 50f;
        [SerializeField] private float _changeSizeSpeed = 0.1f;
        [SerializeField] private float _cursorSizePixels = 50f;
        [SerializeField] private float _rayMarchStepSize = 0.5f;

        private bool _isUserDrawing;
        private bool _isUserInteractingWithBg;
        private bool _isUserChangingSize;
        private float _mouseRadiusChangeSizeStart;
        private float _changeSizeScreenSpaceDistance;
        private Vector2 _changeSizeStartPosition;
        private Vector3 _changeSizeAnchor;

        private long _lastTimeDraw;
        private bool _generating;

        [HideInInspector] [SerializeField] private float[] _serializedDrawing;
        
        private NativeArray<float> _verticesValuesNative;
        private Vector3Int _cachedVertexAmount;

        protected override void GenerateMesh()
        {
            try
            {
                _generating = true;

                bool didChange = false;
                if (_verticesValuesNative.IsCreated == false
                    || _cachedVertexAmount.x != VertexAmountX
                    || _cachedVertexAmount.y != VertexAmountY
                    || _cachedVertexAmount.z != VertexAmountZ)
                {
                    MarchingCubesVisualizer.GetVerticesValuesNative(ref _verticesValuesNative, new Vector3Int(VertexAmountX, VertexAmountY, VertexAmountZ));
                    _cachedVertexAmount = new Vector3Int(VertexAmountX, VertexAmountY, VertexAmountZ);
                    didChange = true;
                }

                if (_allowRegeneration || didChange)
                {
                    InitDrawing();
                }

                base.GenerateMesh();

                if (_serializedDrawing == null || _serializedDrawing.Length != VertexAmountX * VertexAmountY * VertexAmountZ)
                {
                    _serializedDrawing = new float[VertexAmountX * VertexAmountY * VertexAmountZ];
                }

                _verticesValuesNative.CopyTo(_serializedDrawing);
            }
            catch
            {
                // ignored0
            }
            finally
            {
                _generating = false;
            }
        }
        
        protected override void OnEnable()
        {
            base.OnEnable();
            
#if UNITY_EDITOR
            EditorOnEnable();
#endif

            if (_serializedDrawing == null || _allowRegeneration || _serializedDrawing.Length != VertexAmountX * VertexAmountY * VertexAmountZ)
            {
                GenerateMesh();
                return;
            }

            MarchingCubesVisualizer.GetVerticesValuesNative(ref _verticesValuesNative, new Vector3Int(VertexAmountX, VertexAmountY, VertexAmountZ));
            _cachedVertexAmount = new Vector3Int(VertexAmountX, VertexAmountY, VertexAmountZ);
            _verticesValuesNative.CopyFrom(_serializedDrawing);

            GenerateMesh();
        }

#if UNITY_EDITOR
        private void EditorOnEnable()
        {
            EditorApplication.update -= SceneView.RepaintAll;
            EditorApplication.update += SceneView.RepaintAll;
            SceneView.duringSceneGui -= DuringSceneGUI;
            SceneView.duringSceneGui += DuringSceneGUI;
        }
        
        protected override void OnDestroy()
        {
            base.OnDestroy();
            SceneView.duringSceneGui -= DuringSceneGUI;
            EditorApplication.update -= SceneView.RepaintAll;
        }

        private void DuringSceneGUI(SceneView sceneView)
        {
            if (_draw == false)
            {
                return;
            }
            Profiler.BeginSample("DrawingVisualizer.DuringSceneGUI");

            Event currentEvent = Event.current;
            
            // Check if mouse is over the scene view using position
            HitMeshInfo hitInfo = new ();
            Ray ray = HandleUtility.GUIPointToWorldRay(currentEvent.mousePosition);
            if (currentEvent.mousePosition is { x: >= 0, y: >= 0 }
                && currentEvent.mousePosition.x <= sceneView.position.width
                && currentEvent.mousePosition.y <= sceneView.position.height)
            {
                Profiler.BeginSample("Raycast");
                hitInfo = VoxelRaycaster.RayMarch(ray, _rayMarchStepSize, Threshold, VertexAmountX, VertexAmountY, VertexAmountZ, MarchingCubesVisualizer.ReadOnlyVerticesValuesNative, transform.position);
                Profiler.EndSample();
            }
            
            // Consume mouse left down event
            bool isLeftMouse = currentEvent.button == 0;
            bool isRightMouse = currentEvent.button == 1;
            bool isMiddleMouse = currentEvent.button == 2;

            if (isLeftMouse && isRightMouse)
            {
                Profiler.EndSample();
                return;
            }

            if (currentEvent.type is EventType.MouseDown && (isLeftMouse || isRightMouse || isMiddleMouse))
            {
                if (hitInfo.IsHit)
                {
                    if (isMiddleMouse)
                    {
                        _isUserChangingSize = true;
                        _mouseRadiusChangeSizeStart = _mouseRadius;
                        _changeSizeScreenSpaceDistance = _mouseRadius * _cursorSizePixels;
                        _changeSizeAnchor = hitInfo.HitPoint - Vector3.right * _mouseRadius / 2f;
                        _changeSizeStartPosition = HandleUtility.WorldToGUIPoint(_changeSizeAnchor);
                        _changeSizeScreenSpaceDistance = (currentEvent.mousePosition - _changeSizeStartPosition).magnitude;
                    }

                    if (isLeftMouse || isRightMouse)
                    {
                        _isUserDrawing = true;
                    }

                    currentEvent.Use();
                }
                else
                {
                    _isUserInteractingWithBg = true;
                }
            }

            if (currentEvent.type is EventType.MouseUp or EventType.MouseLeaveWindow &&
                (_isUserDrawing || _isUserInteractingWithBg || _isUserChangingSize))
            {
                if (currentEvent.type is EventType.MouseUp && _isUserInteractingWithBg == false)
                {
                    currentEvent.Use();
                }

                _isUserDrawing = false;
                _isUserInteractingWithBg = false;
                _isUserChangingSize = false;
            }

            Vector3 hitInfoPoint = hitInfo.HitPoint;
            hitInfoPoint += ray.direction.normalized * _mouseRadius * _offsetOfSphereDraw;

            if (_isUserChangingSize)
            {
                float changeSize = ((currentEvent.mousePosition - _changeSizeStartPosition).magnitude - _changeSizeScreenSpaceDistance) * _changeSizeSpeed;
                _mouseRadius = Mathf.Clamp(_mouseRadiusChangeSizeStart + changeSize, 0f, 100f);
            }

            if (_isUserInteractingWithBg == false && hitInfo.IsHit)
            {
                Handles.color = _isUserDrawing ? isLeftMouse ? Color.green : Color.red : _isUserChangingSize ? Color.blue : Color.white;
                Handles.color *= new Color(1, 1, 1, 0.5f);
                if (_isUserChangingSize)
                {
                    hitInfoPoint = _changeSizeAnchor;
                }

                Handles.SphereHandleCap(0, hitInfoPoint, Quaternion.identity, _mouseRadius, EventType.Repaint);
            }

            if (currentEvent.type is EventType.MouseDrag && _isUserDrawing)
            {
                currentEvent.Use();
            }

            if (currentEvent.type is not EventType.Layout)
            {
                Profiler.EndSample();
                return;
            }

            // Only draw every 100ms
            float timeDrawDelay = _timeDrawDelayMs * 1000f;
            if (DateTime.Now.Ticks - _lastTimeDraw > timeDrawDelay && _generating == false)
            {
                _lastTimeDraw = DateTime.Now.Ticks;

                if (_isUserDrawing && currentEvent.button == 0)
                {
                    if (hitInfo.IsHit)
                    {
                        AddValue(hitInfoPoint, _mouseRadius, _valueToAdd);
                        GenerateMesh();
                    }
                }

                // Consume mouse right down event
                if (_isUserDrawing && currentEvent.button == 1)
                {
                    if (hitInfo.IsHit)
                    {
                        AddValue(hitInfoPoint, _mouseRadius, _valueToAdd * -1);
                        GenerateMesh();
                    }
                }
            }
            Profiler.EndSample();
        }
#endif

        private void AddValue(Vector3 position, float radius, float value)
        {
            Profiler.BeginSample("DrawingVisualizer.AddValue");
            // Only do update near the center to optimize
            int minX = Mathf.Max(0, (int)(position.x - radius));
            int minY = Mathf.Max(0, (int)(position.y - radius));
            int minZ = Mathf.Max(0, (int)(position.z - radius));
            int maxX = Mathf.Min(VertexAmountX, (int)(position.x + radius));
            int maxY = Mathf.Min(VertexAmountY, (int)(position.y + radius));
            int maxZ = Mathf.Min(VertexAmountZ, (int)(position.z + radius));

            for (int x = minX; x < maxX; x++)
            {
                for (int y = minY; y < maxY; y++)
                {
                    for (int z = minZ; z < maxZ; z++)
                    {
                        Vector3Int pos = new(x, y, z);
                        int index = MarchingCubeUtils.ConvertPositionToIndex(pos, VertexAmount);
                        float distance = Vector3.Distance(pos, position);
                        float effectiveRadius = radius * (1 + _fuzziness);
                        float additionAmount = Mathf.Clamp(value * (1 - Mathf.Pow(distance / effectiveRadius, 4)), -1, 1);
                        if (distance <= effectiveRadius)
                        {
                            _verticesValuesNative[index] = Mathf.Clamp(_verticesValuesNative[index] + additionAmount, 0, 1);
                        }
                    }
                }
            }
            Profiler.EndSample();
        }

        private void InitDrawing()
        {
            for (int x = 0; x < VertexAmountX; x++)
            {
                for (int y = 0; y < VertexAmountY; y++)
                {
                    for (int z = 0; z < VertexAmountZ; z++)
                    {
                        Vector3Int pos = new(x, y, z);
                        int index = MarchingCubeUtils.ConvertPositionToIndex(pos, VertexAmount);
                        float sineOffset = InitScale * Mathf.PerlinNoise(z * GenerationProperties.Frequency / 51.164658416f, x * GenerationProperties.Frequency / 87.1777416f);
                        _verticesValuesNative[index] = y < VertexAmountY / 2f + sineOffset ? 1 : 0;
                    }
                }
            }
        }

        public override void GetVertexValues(NativeArray<float> verticesValues)
        {
            // Do nothing
        }
    }
}