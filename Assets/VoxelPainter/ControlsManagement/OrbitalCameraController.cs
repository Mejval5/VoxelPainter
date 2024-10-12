using System;
using System.Collections.Generic;
using System.Linq;
using Cinemachine;
using Foxworks.Utils;
using UnityEngine;
using VoxelPainter.ControlsManagement;
using VoxelPainter.Rendering;

namespace Foxworks.Components.CameraUtils
{
    [RequireComponent(typeof(CinemachineVirtualCamera))]
    public class OrbitalCameraController : MonoBehaviour
    {
        [SerializeField] private Transform _centerPoint;
        [SerializeField] private Vector3 _centerPointSpeed = Vector3.one;
        
        [SerializeField] private float _startDistance = 70;
        [SerializeField] private float _downAngle = 45f;
        
        [SerializeField] private float _zoomSpeed = 1f;
        [SerializeField] private Vector2 _dragSpeed = Vector2.one;

        [SerializeField] private float _orthoRatio = 0.5f;

        [SerializeField] private DrawingVisualizer _drawingVisualizer;

        [SerializeField] private List<GameObject> _inputConsumers;
        
        private float _currentAngle = 0f;
        private float _distance = 10f;
        private bool _orbiting = false;
        private bool _dragging = false;
        
        private CinemachineVirtualCamera _virtualCamera;

        private void OnEnable()
        {
            _virtualCamera = GetComponent<CinemachineVirtualCamera>();
            _virtualCamera.LookAt = _centerPoint;
            
            _distance = _startDistance;
        }

        private void Update()
        {
            if (_centerPoint == null)
            {
                return;
            }
            
            if (_inputConsumers.Count > 0 && _inputConsumers.Any(consumer => consumer.activeSelf))
            {
                return;
            }
            
            HandleYAxis();
            
            HandleZooming();
            
            HandleOrbiting();

            HandleDragging();
            
            CalculatePosition();
        }

        private void HandleYAxis()
        {
            if (ScreenUtils.IsMouseInsideScreen == false)
            {
                return;
            }

            if (Controls.IsKeyPressed(VoxelControlKey.AltModifier) == false)
            {
                return;
            }
            
            if (Input.mouseScrollDelta.y != 0)
            {
            }

            float heightOffset = Input.mouseScrollDelta.y * _centerPointSpeed.y;

            Vector3 position = _centerPoint.position;
            float newPos = position.y + heightOffset;

            newPos = Mathf.Clamp(newPos, _drawingVisualizer.transform.position.y, _drawingVisualizer.VertexAmountY + 1.5f);
            
            position = new Vector3(position.x, newPos, position.z);
            _centerPoint.position = position;
        }
        
        private void CalculatePosition()
        {
            Vector3 position = _centerPoint.position;
            float distance = _distance;
            if (_virtualCamera.m_Lens.Orthographic)
            {
                distance = _distance / _orthoRatio;
            }
            position += Quaternion.Euler(_downAngle, _currentAngle, 0) * Vector3.back * distance;
            transform.position = position;
        }

        private void HandleZooming()
        {
            if (ScreenUtils.IsMouseInsideScreen == false)
            {
                return;
            }

            if (Controls.IsKeyPressed(VoxelControlKey.AltModifier))
            {
                return;
            }
            
            if (Input.mouseScrollDelta.y != 0)
            {
                _distance = Mathf.Clamp(_distance - Input.mouseScrollDelta.y * _zoomSpeed, 1.25f, 500f);
            }
            
            _virtualCamera.m_Lens.OrthographicSize = _distance * _orthoRatio;
        }

        private void HandleDragging()
        {
            if (Controls.IsKeyPressed(VoxelControlKey.RotateHeld) && ScreenUtils.IsMouseInsideScreen && Controls.IsKeyPressed(VoxelControlKey.AltModifier))
            {
                _dragging = true;
            }
            
            if (Controls.IsKeyPressed(VoxelControlKey.RotateHeld) == false || Controls.IsKeyPressed(VoxelControlKey.AltModifier) == false)
            {
                _dragging = false;
            }
            
            if (_dragging == false)
            {
                return;
            }

            Vector3 offset = new (Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"), 0);
            
            Vector3 worldSpaceOffset = transform.TransformDirection(- offset);
            
            _centerPoint.position += Vector3.Scale(worldSpaceOffset, _centerPointSpeed);
        }

        private void HandleOrbiting()
        {
            if (Controls.IsKeyPressed(VoxelControlKey.RotateHeld) && ScreenUtils.IsMouseInsideScreen && _orbiting == false)
            {
                _orbiting = true;
                return;
            }
            
            if (Controls.IsKeyPressed(VoxelControlKey.RotateHeld) == false || Controls.IsKeyPressed(VoxelControlKey.AltModifier))
            {
                _orbiting = false;
            }

            if (_orbiting == false)
            {
                return;
            }

            _currentAngle += Input.GetAxis("Mouse X") * _dragSpeed.x;
            _downAngle -= Input.GetAxis("Mouse Y") * _dragSpeed.y;
            _downAngle = Mathf.Clamp(_downAngle, -89.99f, 89.99f);
        }

        private void RecalculatePosition()
        {
            
        }
    }
}