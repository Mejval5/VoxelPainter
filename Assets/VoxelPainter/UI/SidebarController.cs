
using System.Collections;
using Foxworks.Utils;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UI;

namespace VoxelPainter.UI
{
    public class SidebarController : MonoBehaviour
    {
        [SerializeField] private RectTransform _sideBar;
        
        [SerializeField] private Button _openButton;
        [SerializeField] private Button _closeButton;

        [SerializeField] private float _fullDuration = 0.5f;
        
        [SerializeField] private float _openPosX = 5f;

        [NaughtyAttributes.Button]
        public void ToggleSidebar()
        {
            MoveSideBar(_sideBar.anchoredPosition.x > _openPosX, instant: true);
        }
        
        private void Awake()
        {
            _openButton.onClick.AddListener(OnOpenButtonClicked);
            _closeButton.onClick.AddListener(OnCloseButtonClicked);

            MoveSideBar(false, instant: true);
        }
        
        private void OnOpenButtonClicked()
        {
            MoveSideBar(true);
        }
        
        private void OnCloseButtonClicked()
        {
            MoveSideBar(false);
        }
        
        private void MoveSideBar(bool open, bool instant = false)
        {
            _openButton.gameObject.SetActive(!open);
            _closeButton.gameObject.SetActive(open);
            
            float closedPosX = _openPosX + _sideBar.rect.width;
            
            float targetPosition = open ? _openPosX : closedPosX;

            float duration = _fullDuration;
            
            if (open)
            {
                duration *= Mathf.Abs(_sideBar.anchoredPosition.x - _openPosX) / _sideBar.rect.width;
            }
            else
            {
                duration *= Mathf.Abs(_sideBar.anchoredPosition.x - closedPosX) / _sideBar.rect.width;
            }

            if (instant)
            {
                duration = 0;
            }

            StopAllCoroutines();
            StartCoroutine(MoveSideBarCoroutine(targetPosition, duration));
        }
        
        private IEnumerator MoveSideBarCoroutine(float targetPosition, float duration)
        {
            duration = Mathf.Clamp(duration, 0f, 10f);
            
            float startPosition = _sideBar.anchoredPosition.x;
            float time = 0;
            
            while (time <= duration)
            {
                time += Time.deltaTime;
                float t = 1f;
                if (duration > 0)
                {
                    t = time / duration;
                }

                t = EasingUtils.CubicOut(t);

                Vector2 position = _sideBar.anchoredPosition;
                position.x = Mathf.Lerp(startPosition, targetPosition, t);
                _sideBar.anchoredPosition = position;
                yield return null;
            }
        }
    }
}