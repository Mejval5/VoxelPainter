using UnityEngine;
using UnityEngine.UI;

namespace VoxelPainter.UI
{
    public class SideBarManager : MonoBehaviour
    {
        [SerializeField] private Button _newButton;
        [SerializeField] private Button _openButton;
        [SerializeField] private Button _quitButton;
        
        private void Awake()
        {
            _newButton.onClick.AddListener(OnNewButtonClicked);
            _openButton.onClick.AddListener(OnOpenButtonClicked);
            _quitButton.onClick.AddListener(OnQuitButtonClicked);
        }
        
        private void OnNewButtonClicked()
        {
            Debug.Log("New button clicked");
        }
        
        private void OnOpenButtonClicked()
        {
            Debug.Log("Open button clicked");
        }
        
        private void OnQuitButtonClicked()
        {
            Debug.Log("Quit button clicked");
        }
    }
}