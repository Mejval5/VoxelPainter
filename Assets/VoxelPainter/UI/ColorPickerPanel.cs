using System;
using Foxworks.Persistence;
using UnityEngine;
using UnityEngine.UI;
using VoxelPainter.Rendering;

namespace VoxelPainter.UI
{
    [Serializable]
    public class ColorPickerSettings
    {
        public Color Color;
        public PaintMode PaintMode;
    }
    
    public class ColorPickerPanel : MonoBehaviour
    {
        private const string ColorPickerSaveData = "ColorPickerSettings";
        
        [SerializeField] private ColorPicker _colorPicker;
        [SerializeField] private GameObject _colorPickerCover;
        
        [SerializeField] private Button _additionModeButton;
        [SerializeField] private Button _colorModeButton;
        [SerializeField] private Rendering.VoxelPainter _voxelPainter;
        
        public ColorPickerSettings ColorPickerSettings { get; private set; }
        
        private void Awake()
        {
            ColorPickerSettings = SaveManager.Load<ColorPickerSettings>(ColorPickerSaveData);
            ColorPickerSettings ??= new ColorPickerSettings();

            UpdateVoxelPainter();
            
            _colorPicker.color = ColorPickerSettings.Color;
            
            _colorPicker.onColorChanged += OnColorChanged;
            _additionModeButton.onClick.AddListener(OnAdditionModeButtonClicked);
            _colorModeButton.onClick.AddListener(OnColorModeButtonClicked);

            UpdateVisuals();
        }

        private void UpdateVisuals()
        {
            _additionModeButton.gameObject.SetActive(ColorPickerSettings.PaintMode == PaintMode.Addition);
            _colorModeButton.gameObject.SetActive(ColorPickerSettings.PaintMode == PaintMode.Color);
            
            _colorPickerCover.SetActive(ColorPickerSettings.PaintMode is not PaintMode.Color);
        }

        private void UpdateVoxelPainter()
        {
            _voxelPainter.PaintMode = ColorPickerSettings.PaintMode;
            _voxelPainter.CurrentPaintColor = ColorPickerSettings.Color;
        }
        
        private void OnColorChanged(Color color)
        {
            ColorPickerSettings.Color = color;
            SaveManager.Save(ColorPickerSaveData, ColorPickerSettings);

            UpdateVoxelPainter();
        }

        private void CyclePaintMode()
        {
            ColorPickerSettings.PaintMode = ColorPickerSettings.PaintMode == PaintMode.Color ? PaintMode.Addition : PaintMode.Color;
            UpdateVisuals();
        }
        
        private void OnAdditionModeButtonClicked()
        {
            CyclePaintMode();
            SaveManager.Save(ColorPickerSaveData, ColorPickerSettings);

            UpdateVoxelPainter();
        }
        
        private void OnColorModeButtonClicked()
        {
            CyclePaintMode();
            SaveManager.Save(ColorPickerSaveData, ColorPickerSettings);

            UpdateVoxelPainter();
        }
    }
}