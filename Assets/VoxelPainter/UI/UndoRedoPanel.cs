using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VoxelPainter.Rendering;

namespace VoxelPainter.UI
{
    public class UndoRedoPanel : MonoBehaviour
    {
        [Header("Undo")]
        [SerializeField] private Button _undoButton;
        [SerializeField] private Button _undoButtonBg;
        [SerializeField] private TextMeshProUGUI _undoText;
        [SerializeField] private TextMeshProUGUI _undoNumberText;
        
        [Header("Redo")]
        [SerializeField] private Button _redoButton;
        [SerializeField] private Button _redoButtonBg;
        [SerializeField] private TextMeshProUGUI _redoText;
        [SerializeField] private TextMeshProUGUI _redoNumberText;
        
        [Header("Settings")]
        [SerializeField] private Color _bgColorActive;
        [SerializeField] private Color _textColorActive;
        [SerializeField] private Color _bgColorInactive;
        [SerializeField] private Color _textColorInactive;
        
        [Header("Reference")]
        [SerializeField] private DrawingVisualizer _drawingVisualizer;
        
        protected void Awake()
        {
            _undoButton.onClick.AddListener(OnUndoButtonClicked);
            _undoButtonBg.onClick.AddListener(OnUndoButtonClicked);
            _redoButton.onClick.AddListener(OnRedoButtonClicked);
            _redoButtonBg.onClick.AddListener(OnRedoButtonClicked);
            _drawingVisualizer.UndoRedoStateChanged += OnUndoRedoStateChanged;
            
            UpdateVisuals();
        }
        
        private void OnDestroy()
        {
            _drawingVisualizer.UndoRedoStateChanged -= OnUndoRedoStateChanged;
        }
        
        private void OnUndoRedoStateChanged()
        {
            UpdateVisuals();
        }

        private void UpdateVisuals()
        {
            UpdateUndoVisuals();
            UpdateRedoVisuals();
        }
        
        private void UpdateUndoVisuals()
        {
            _undoButton.interactable = _drawingVisualizer.UndoCount > 1;
            _undoButtonBg.interactable = _undoButton.interactable;
            _undoText.color = _undoButton.interactable ? _textColorActive : _textColorInactive;
            _undoNumberText.text = _undoButton.interactable ? Mathf.Max(0, _drawingVisualizer.UndoCount - 1).ToString() : "-";
        }

        private void UpdateRedoVisuals()
        {
            _redoButton.interactable = _drawingVisualizer.RedoCount > 0;
            _redoButtonBg.interactable = _redoButton.interactable;
            _redoText.color = _redoButton.interactable ? _textColorActive : _textColorInactive;
            _redoNumberText.text = _redoButton.interactable ? _drawingVisualizer.RedoCount.ToString() : "-";
        }
        
        private void OnUndoButtonClicked()
        {
            _drawingVisualizer.Undo();
        }
        
        private void OnRedoButtonClicked()
        {
            _drawingVisualizer.Redo();
        }
    }
}