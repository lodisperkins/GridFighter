using Lodis.Utility;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Lodis.UI
{
    [System.Serializable]
    public class Page
    {
        public GameObject PageParent;
        public GameObject FirstSelected;
        public UnityEvent OnActive;
        public UnityEvent OnInactive;
    }

    public class PageManagerBehaviour : MonoBehaviour
    {
        [SerializeField]
        private Page[] _pages;
        [SerializeField]
        private EventSystem _eventSystem;
        [SerializeField]
        private bool _loadSceneOnFirstPage;
        [SerializeField]
        private int _sceneIndex = -1;
        private int _currentPage;
        private PlayerControls _controls;

        public EventSystem EventManager { get => _eventSystem; set => _eventSystem = value; }

        public void Awake()
        {
            _controls = new PlayerControls();
            _controls.UI.Cancel.started += context => GoToPreviousPage();
        }

        public void OnEnable()
        {
            _controls.Enable();
        }

        public void OnDisable()
        {
            _controls.Disable();
        }

        public void GoToNextPage()
        {
            _pages[_currentPage].PageParent.SetActive(false);
            _pages[_currentPage].OnInactive?.Invoke();

            _currentPage++;

            _pages[_currentPage].PageParent.SetActive(true);
            EventManager.SetSelectedGameObject(_pages[_currentPage].FirstSelected);
            _pages[_currentPage].OnActive?.Invoke();
        }

        public void GoToPreviousPage()
        {
            _pages[_currentPage].PageParent.SetActive(false);
            _pages[_currentPage].OnInactive?.Invoke();

            _currentPage--;

            if (_currentPage < 0 && _loadSceneOnFirstPage)
            {
                if (_sceneIndex == -1)
                    SceneManagerBehaviour.Instance.LoadPreviousScene();
                else
                    SceneManagerBehaviour.Instance.LoadScene(_sceneIndex);

                return;
            }

            _pages[_currentPage].PageParent.SetActive(true);
            EventManager.SetSelectedGameObject(_pages[_currentPage].FirstSelected);
            _pages[_currentPage].OnActive?.Invoke();
        }
    }
}