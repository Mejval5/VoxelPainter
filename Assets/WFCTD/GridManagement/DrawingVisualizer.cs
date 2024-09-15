using System;
using UnityEditor;
using UnityEngine;

namespace WFCTD.GridManagement
{
    [ExecuteAlways]
    public class DrawingVisualizer : MarchingCubeRendererBase
    {
        [SerializeField] private bool _allowRegeneration = false;
        [field: Range(0.1f, 10f)] [field: SerializeField] public float InitScale { get; set; } = 2f;
        [SerializeField] private MeshCollider _meshCollider;
        [Range(1f,100f)]
        [SerializeField] private float _mouseRadius = 5f;
        [Range(0f,1f)]
        [SerializeField] private float _valueToAdd = 1f;
        [SerializeField] private bool _draw = true;
        [Range(0f,0.5f)]
        [SerializeField] private float _offsetOfSphereDraw = 0.3f;
        [Range(0f,5f)]
        [SerializeField] private float _fuzziness = 2f;
        [Range(10f, 500f)]
        [SerializeField] private float _timeDrawDelayMs = 50f;
        [SerializeField] private float _changeSizeSpeed = 0.1f;
        [SerializeField] private float _cursorSizePixels = 50f;
        
        
        
        private float[,,] _drawing;
        
        private bool _isUserDrawing;
        private bool _isUserInteractingWithBg;
        private bool _isUserChangingSize;
        private float _mouseRadiusChangeSizeStart;
        private float _changeSizeScreenSpaceDistance;
        private Vector2 _changeSizeStartPosition;
        private Vector3 _changeSizeAnchor;
        
        private long _lastTimeDraw;
        private bool _generating;

        [SerializeField] private float[] _serializedDrawing;
        
        protected override void GenerateMesh()
        {
            try
            {
                _generating = true;

                bool didChange = false;
                if (_drawing == null
                    || _drawing.GetLength(0) != VertexAmountX
                    || _drawing.GetLength(1) != VertexAmountY
                    || _drawing.GetLength(2) != VertexAmountZ)
                {
                    _drawing = new float[VertexAmountX, VertexAmountY, VertexAmountZ];
                    didChange = true;
                }

                if (_allowRegeneration || didChange)
                {
                    InitDrawing();
                }

                base.GenerateMesh();

                _meshCollider.sharedMesh = GridMeshFilter.sharedMesh;

                if (_serializedDrawing == null || _serializedDrawing.Length != VertexAmountX * VertexAmountY * VertexAmountZ)
                {
                    _serializedDrawing = new float[VertexAmountX * VertexAmountY * VertexAmountZ];
                }

                int index = 0;
                for (int x = 0; x < VertexAmountX; x++)
                {
                    for (int y = 0; y < VertexAmountY; y++)
                    {
                        for (int z = 0; z < VertexAmountZ; z++)
                        {
                            _serializedDrawing[index] = _drawing[x, y, z];
                            index++;
                        }
                    }
                }
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
        
#if UNITY_EDITOR
        private void OnEnable()
        {
            EditorApplication.update -= SceneView.RepaintAll;
            EditorApplication.update += SceneView.RepaintAll;
            SceneView.duringSceneGui -= DuringSceneGUI;
            SceneView.duringSceneGui += DuringSceneGUI;

            if (_serializedDrawing == null || _serializedDrawing.Length != VertexAmountX * VertexAmountY * VertexAmountZ || _allowRegeneration)
            {
                return;
            }

            _drawing = new float[VertexAmountX, VertexAmountY, VertexAmountZ];
            int index = 0;
            for (int x = 0; x < VertexAmountX; x++)
            {
                for (int y = 0; y < VertexAmountY; y++)
                {
                    for (int z = 0; z < VertexAmountZ; z++)
                    {
                        _drawing[x, y, z] = _serializedDrawing[index];
                        index++;
                    }
                }
            }
        }

        private void DuringSceneGUI(SceneView sceneView)
        {
            if (_draw == false)
            {
                return;
            }
            
            Event currentEvent = Event.current;
            
            Ray ray = HandleUtility.GUIPointToWorldRay(currentEvent.mousePosition);
            Physics.Raycast(ray, out RaycastHit hitInfo);
            
            // Consume mouse left down event
            bool isLeftMouse = currentEvent.button == 0;
            bool isRightMouse = currentEvent.button == 1;
            bool isMiddleMouse = currentEvent.button == 2;
            
            if (isLeftMouse && isRightMouse)
            {
                return;
            }

            if (currentEvent.type is EventType.MouseDown && (isLeftMouse || isRightMouse || isMiddleMouse))
            {
                if (hitInfo.collider == _meshCollider)
                {
                    if (isMiddleMouse)
                    {
                        _isUserChangingSize = true;
                        _mouseRadiusChangeSizeStart = _mouseRadius;
                        _changeSizeScreenSpaceDistance = _mouseRadius * _cursorSizePixels;
                        _changeSizeAnchor = hitInfo.point - Vector3.right * _mouseRadius / 2f;
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
            
            Vector3 hitInfoPoint = hitInfo.point;
            hitInfoPoint += ray.direction.normalized * _mouseRadius * _offsetOfSphereDraw;
            
            if (_isUserChangingSize)
            {
                float changeSize = ((currentEvent.mousePosition - _changeSizeStartPosition).magnitude - _changeSizeScreenSpaceDistance) * _changeSizeSpeed;
                _mouseRadius = Mathf.Clamp(_mouseRadiusChangeSizeStart + changeSize, 0f , 100f);
            }
            
            if (_isUserInteractingWithBg == false && hitInfo.collider == _meshCollider)
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
                return; 
            }
            
            // Only draw every 100ms
            float timeDrawDelay = _timeDrawDelayMs * 1000f;
            if (DateTime.Now.Ticks - _lastTimeDraw > timeDrawDelay && _generating == false)
            {
                _lastTimeDraw = DateTime.Now.Ticks;
            
                if (_isUserDrawing && currentEvent.button == 0)
                {
                    if (hitInfo.collider == _meshCollider)
                    {
                        AddValue(hitInfoPoint, _mouseRadius, _valueToAdd);
                        GenerateMesh();
                    }
                }
            
                // Consume mouse right down event
                if (_isUserDrawing && currentEvent.button == 1)
                {
                    if (hitInfo.collider == _meshCollider)
                    {
                        AddValue(hitInfoPoint, _mouseRadius, _valueToAdd * -1);
                        GenerateMesh();
                    }
                }
            }
        }
#endif
        private void AddValue(Vector3 position, float radius, float value)
        {
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
                        Vector3 pos = new (x, y, z);
                        float distance = Vector3.Distance(pos, position);
                        float effectiveRadius = radius * (1 + _fuzziness);
                        float additionAmount = Mathf.Clamp(value * (1 - Mathf.Pow(distance / effectiveRadius, 4)), -1, 1);
                        if (distance <= effectiveRadius)
                        {
                            _drawing[x, y, z] = Mathf.Clamp(_drawing[x, y, z] + additionAmount, 0, 1);
                        }
                    }
                }
            }
        }

        private void InitDrawing()
        {
            for (int x = 0; x < VertexAmountX; x++)
            {
                for (int y = 0; y < VertexAmountY; y++)
                {
                    for (int z = 0; z < VertexAmountZ; z++)
                    {
                        float sineOffset = InitScale * Mathf.PerlinNoise(z * GenerationProperties.Frequency / 51.164658416f, x * GenerationProperties.Frequency / 87.1777416f);
                        _drawing[x, y, z] = y < (VertexAmountY / 2f + sineOffset) ? 1 : 0;
                    }
                }
            }
        }

        public override float GetGridValue(int i, Vector3 position, GenerationProperties generationProperties)
        {
            return _drawing[(int)position.x, (int)position.y, (int)position.z];
        }
    }
}