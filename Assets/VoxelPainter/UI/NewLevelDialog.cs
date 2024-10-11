using System;
using System.Threading;
using Foxworks.Components.UI;
using Foxworks.Utils;
using SFB;
using UnityEngine;
using UnityEngine.UI;
using VoxelPainter.Rendering;

namespace VoxelPainter.UI
{
    public class NewLevelDialog : MonoBehaviour
    {
        private const float MinLoadingTime = 0.0f;
        
        [Header("Basic References")]
        [SerializeField] private Button _bgCloseButton;
        [SerializeField] private Button _closeButton;
        
        [Header("UI Elements")]
        [SerializeField] private JuicyButton _newLevelPerlinButton;
        [SerializeField] private JuicyButton _newLevelDefaultButton;
        [SerializeField] private JuicyButton _newLevelPlayerHeightMapButton;
        
        [Header("Dependencies")]
        [SerializeField] private DrawingVisualizer _drawingVisualizer;

        private string _lastSelectedPath;
        
        protected void Awake()
        {
            _bgCloseButton.onClick.AddListener(Hide);
            _closeButton.onClick.AddListener(Hide);
            
            _newLevelPerlinButton.Clicked.AddListener(OnNewLevelPerlinButtonClicked);
            _newLevelDefaultButton.Clicked.AddListener(OnNewLevelDefaultButtonClicked);
            _newLevelPlayerHeightMapButton.Clicked.AddListener(OnNewLevelPlayerHeightMapButtonClicked);
        }
        
        private void OnNewLevelPerlinButtonClicked()
        {
            _drawingVisualizer.GenerateDrawingAndRender(HeightmapInitType.PerlinNoise);
            Hide();
        }
        
        private void OnNewLevelDefaultButtonClicked()
        {
            _drawingVisualizer.GenerateDrawingAndRender(HeightmapInitType.DefaultTexture);
            Hide();
        }
        
        private void OnNewLevelPlayerHeightMapButtonClicked()
        {
            string pathToOpen = string.IsNullOrEmpty(_lastSelectedPath) ? Application.dataPath : _lastSelectedPath;
            
            // Open file dialog for the user to select a JPG or PNG image
            ExtensionFilter[] filters = { new("Image files", "jpg", "png")};
            string[] paths = StandaloneFileBrowser.OpenFilePanel("Select a Heightmap Image", pathToOpen, filters, false);

            if (paths.Length <= 0 || string.IsNullOrEmpty(paths[0]))
            {
                return;
            }
            
            string path = paths[0];
            _lastSelectedPath = path;

            // Load the texture from the selected file path
            Texture2D loadedTexture = FileUtils.LoadTextureFromFile(path);

            if (loadedTexture != null)
            {
                _drawingVisualizer.GenerateDrawingAndRender(HeightmapInitType.Texture, loadedTexture);
            }
            else
            {
                Debug.LogError("Failed to load texture from the selected file.");
            }
            
            Hide();
        }

        private void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}