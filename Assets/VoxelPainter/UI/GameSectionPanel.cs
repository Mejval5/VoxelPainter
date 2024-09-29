using UnityEngine;
using UnityEngine.UI;
using VoxelPainter.Rendering;

namespace VoxelPainter.UI
{
    public class GameSectionPanel : MonoBehaviour
    {
        [SerializeField] private Button _newGameButton;
        [SerializeField] private Button _loadGameButton;
        [SerializeField] private Button _settingsButton;
        [SerializeField] private Button _exitButton;

        [SerializeField] private DrawingVisualizer _drawingVisualizer;
        
        private void Awake()
        {
            _newGameButton.onClick.AddListener(OnNewGameButtonClicked);
            _loadGameButton.onClick.AddListener(OnLoadGameButtonClicked);
            _settingsButton.onClick.AddListener(OnSettingsButtonClicked);
            _exitButton.onClick.AddListener(OnExitButtonClicked);
        }
        
        private void OnNewGameButtonClicked()
        {
            _drawingVisualizer.NewDrawing();
        }
        
        private void OnLoadGameButtonClicked()
        {
            Debug.Log("Load Game Button Clicked");
        }
        
        private void OnSettingsButtonClicked()
        {
            Debug.Log("Settings Button Clicked");
        }
        
        private void OnExitButtonClicked()
        {
            _drawingVisualizer.Save();
            
            if (Application.isEditor)
            {
                UnityEditor.EditorApplication.isPlaying = false;
            }
            else
            {
                Application.Quit();
            }
        }
    }
}