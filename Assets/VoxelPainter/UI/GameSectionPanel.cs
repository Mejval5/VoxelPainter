using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using VoxelPainter.Rendering;

namespace VoxelPainter.UI
{
    public class GameSectionPanel : MonoBehaviour
    {
        [Header("Buttons")]
        [SerializeField] private Button _newGameButton;
        [SerializeField] private Button _loadGameButton;
        [SerializeField] private Button _exitButton;

        [Header("Dependencies")]
        [SerializeField] private NewLevelDialog _newLevelDialog;
        [SerializeField] private DrawingVisualizer _drawingVisualizer;
        [SerializeField] private LevelSelectionPanel _levelSelectionPanel;
        
        private void Awake()
        {
            _newGameButton.onClick.AddListener(OnNewGameButtonClicked);
            _loadGameButton.onClick.AddListener(OnLoadGameButtonClicked);
            _exitButton.onClick.AddListener(OnExitButtonClicked);
        }
        
        private void OnNewGameButtonClicked()
        {
            _newLevelDialog.gameObject.SetActive(true);
        }
        
        private void OnLoadGameButtonClicked()
        {
            _levelSelectionPanel.gameObject.SetActive(true);
        }
        
        private async void OnExitButtonClicked()
        {
            await _drawingVisualizer.Save();
            
            if (Application.isEditor)
            {
#if UNITY_EDITOR
                EditorApplication.isPlaying = false;
#endif
            }
            else
            {
                Application.Quit();
            }
        }
    }
}