using System;
using Foxworks.Components.UI;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace VoxelPainter.UI
{
    public class LevelPreviewButton : MonoBehaviour
    {
        [SerializeField] private JuicyButton _button;
        [SerializeField] private Button _deleteButton;
        [SerializeField] private GameObject _selectionBox;
        
        [SerializeField] private RawImage _rawImage;
        
        public RawImage RawImage => _rawImage;
        public JuicyButton JuicyButton => _button;

        [HideInInspector] public UnityEvent Clicked = new();
        [HideInInspector] public UnityEvent DeleteClicked = new ();
        
        protected void Awake()
        {
            _button.Clicked.AddListener(() => Clicked.Invoke());
            _deleteButton.onClick.AddListener(() => DeleteClicked.Invoke());
            _selectionBox.SetActive(false);
        }
        
        public void SetSelected(bool selected)
        {
            _selectionBox.SetActive(selected);
            
            _deleteButton.gameObject.SetActive(selected == false);
        }
    }
}