using System;
using System.Collections.Generic;
using Foxworks.Persistence;
using UnityEngine;
using UnityEngine.UI;
using VoxelPainter.Rendering;

namespace VoxelPainter.UI
{
    [Serializable]
    public class ColorPickerSettings
    {
        public Color Color = Color.white;
        public PaintMode PaintMode = PaintMode.Addition;
    }
    
    public class ColorPickerPanel : MonoBehaviour
    {
        private const string ColorPickerSaveData = "ColorPickerSettings";
        
        [SerializeField] private ColorPicker _colorPicker;
        [SerializeField] private GameObject _colorPickerCover;
        
        [SerializeField] private Button _additionModeButton;
        [SerializeField] private Button _colorModeButton;
        [SerializeField] private Button _colorAndAdditionModeButton;
        
        [SerializeField] private List<Graphic> _colorModeIcons;
        
        [SerializeField] private Rendering.VoxelPainter _voxelPainter;
        
        public ColorPickerSettings ColorPickerSettings { get; private set; }
        
        private void Awake()
        {
            ColorPickerSettings = SaveManager.Load<ColorPickerSettings>(ColorPickerSaveData);
            ColorPickerSettings ??= new ColorPickerSettings();

            UpdateVoxelPainter();
            
            _colorPicker.onColorChanged += OnColorChanged;
            _additionModeButton.onClick.AddListener(OnChangeModeButtonClicked);
            _colorModeButton.onClick.AddListener(OnChangeModeButtonClicked);
            _colorAndAdditionModeButton.onClick.AddListener(OnChangeModeButtonClicked);

            UpdateVisuals();
        }

        private void Start()
        {
            // Needs to be done in start to let it initialize in Awake
            _colorPicker.color = ColorPickerSettings.Color;
        }

        private void UpdateVisuals()
        {
            _additionModeButton.gameObject.SetActive(ColorPickerSettings.PaintMode == PaintMode.Addition);
            _colorModeButton.gameObject.SetActive(ColorPickerSettings.PaintMode == PaintMode.Color);
            _colorAndAdditionModeButton.gameObject.SetActive(ColorPickerSettings.PaintMode == PaintMode.ColorAndAddition);
            
            _colorPickerCover.SetActive(ColorPickerSettings.PaintMode is PaintMode.Addition);
        }

        private void Update()
        {
            foreach (Graphic icon in _colorModeIcons)
            {
                icon.color = _colorPicker.color;
            }
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
            ColorPickerSettings.PaintMode = ColorPickerSettings.PaintMode switch
            {
                PaintMode.Addition => PaintMode.Color,
                PaintMode.Color => PaintMode.ColorAndAddition,
                PaintMode.ColorAndAddition => PaintMode.Addition,
                _ => throw new ArgumentOutOfRangeException()
            };
            
            UpdateVisuals();
        }
        
        private void OnChangeModeButtonClicked()
        {
            CyclePaintMode();
            SaveManager.Save(ColorPickerSaveData, ColorPickerSettings);

            UpdateVoxelPainter();
        }
    }
}