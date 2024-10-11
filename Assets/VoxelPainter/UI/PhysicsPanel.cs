using System;
using Foxworks.Persistence;
using UnityEngine;
using UnityEngine.UI;
using VoxelPainter.Rendering;

namespace VoxelPainter.UI
{
    [Serializable]
    public class PhysicsSettings
    {
        public bool RunPhysics;
    }
    
    public class PhysicsPanel : MonoBehaviour
    {
        private const string PhysicsSettingsKey = "PhysicsSettings";
        
        [SerializeField] private DrawingVisualizer _drawingVisualizer;
        [SerializeField] private Button _physicsButton;
        [SerializeField] private GameObject _offVisuals;
        [SerializeField] private GameObject _onVisuals;
        
        private PhysicsSettings _physicsSettings;
        
        private void Awake()
        {
            _physicsSettings = SaveManager.Load<PhysicsSettings>(PhysicsSettingsKey);
            _physicsSettings ??= new PhysicsSettings();
            
            _physicsButton.onClick.AddListener(TogglePhysics);
            _drawingVisualizer.RunPhysics = _physicsSettings.RunPhysics;
            
            UpdateVisuals();
        }
        
        private void TogglePhysics()
        {
            _physicsSettings.RunPhysics = !_physicsSettings.RunPhysics;
            _drawingVisualizer.RunPhysics = _physicsSettings.RunPhysics;
            SaveManager.Save(PhysicsSettingsKey, _physicsSettings);

            UpdateVisuals();
        }

        private void UpdateVisuals()
        {
            _offVisuals.SetActive(!_physicsSettings.RunPhysics);
            _onVisuals.SetActive(_physicsSettings.RunPhysics);
        }
    }
}