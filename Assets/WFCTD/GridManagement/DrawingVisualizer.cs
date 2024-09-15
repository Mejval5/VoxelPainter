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
        [SerializeField] private float _mouseRadius = 5f;
        [Range(0f,1f)]
        [SerializeField] private float _valueToAdd = 1f;
        [SerializeField] private bool _draw = true;
        
        private float[,,] _drawing;
        private bool _isUserDrawing;

        [SerializeField] private float[] _serializedDrawing;
        
        protected override void GenerateMesh()
        {
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
            RaycastHit hitInfo;
            if (Physics.Raycast(ray, out hitInfo))
            {
            }

            if (currentEvent.type is EventType.MouseDown && hitInfo.collider == _meshCollider)
            {
                _isUserDrawing = true;
            }
            
            if (currentEvent.type is EventType.MouseUp)
            {
                _isUserDrawing = false;
            }
            
            // Consume mouse left down event
            bool isMouseTryingToDraw = currentEvent.type is EventType.MouseDrag && _isUserDrawing;
            bool isLeftMouse = currentEvent.button == 0;
            bool isRightMouse = currentEvent.button == 1;
            
            if (isLeftMouse && isRightMouse)
            {
                return;
            }
            
            if (currentEvent.type is not EventType.MouseDrag || _isUserDrawing)
            {
                Vector3 pos = hitInfo.point;
                Handles.color = _isUserDrawing ? isLeftMouse ? Color.green : Color.red : Color.white;
                Handles.color *= new Color(1, 1, 1, 0.5f);
                Handles.SphereHandleCap(0, pos, Quaternion.identity, _mouseRadius, EventType.Repaint);
            }
            
            if (isMouseTryingToDraw && currentEvent.button == 0)
            {
                if (hitInfo.collider == _meshCollider)
                {
                    currentEvent.Use();
                    AddValue(hitInfo.point, _mouseRadius, _valueToAdd);
                    GenerateMesh();
                    return;
                }
            }
            
            // Consume mouse right down event
            if (isMouseTryingToDraw && currentEvent.button == 1)
            {
                if (hitInfo.collider == _meshCollider)
                {
                    currentEvent.Use();
                    AddValue(hitInfo.point, _mouseRadius, _valueToAdd * -1);
                    GenerateMesh();
                    return;
                }
            }

            if (currentEvent.type is EventType.MouseDrag && _isUserDrawing)
            {
                currentEvent.Use();
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
                        Vector3 pos = new Vector3(x, y, z);
                        float distance = Vector3.Distance(pos, position);
                        if (distance <= radius)
                        {
                            _drawing[x, y, z] = Mathf.Clamp(_drawing[x, y, z] + value, 0, 1);
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