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
        public GameObject PageRoot;
        public GameObject FirstSelected;
        public bool KeepRootVisible;
        [SerializeField]
        private Page[] _children;
        private bool _childrenInitialized;
        [HideInInspector]
        public Page PageParent;
        public UnityEvent OnActive;
        public UnityEvent OnInactive;

        public Page[] Children
        {
            get
            {
                if (!_childrenInitialized)
                {
                    foreach (Page child in _children)
                    {
                        child.PageParent = this;
                    }
                }

                return _children;
            }
            set => _children = value;
        }
    }

    public class PageManagerBehaviour : MonoBehaviour
    {
        [SerializeField]
        private Page _rootPage;
        private Page _currentPage;
        [SerializeField]
        private UnityEngine.EventSystems.EventSystem _eventSystem;
        [SerializeField]
        private bool _loadSceneOnFirstPage;
        [SerializeField]
        private bool _previousPageOnCancel = true;
        [SerializeField]
        private int _sceneIndex = -1;
        private int _currentChildIndex;
        private PlayerControls _controls;

        public UnityEngine.EventSystems.EventSystem EventManager { get => _eventSystem; set => _eventSystem = value; }

        public void Awake()
        {
            if (!_previousPageOnCancel)
                return;

            _controls = new PlayerControls();
            _controls.UI.Cancel.started += context => GoToPageParent();

            _currentPage = _rootPage;
        }
        public void OnEnable()
        {
            _controls?.Enable();
        }

        public void OnDisable()
        {
            _controls?.Disable();
        }

        public void GoToRootPage()
        {
            if (_rootPage == null || _currentPage.PageParent == null)
                return;

            _currentPage.PageRoot.SetActive(false);
            _currentPage.OnInactive?.Invoke();

            _currentPage = _rootPage;

            _currentPage.PageRoot.SetActive(true);
            EventManager.SetSelectedGameObject(_currentPage.FirstSelected);
            _currentPage.OnActive?.Invoke();
        }

        public void GoToPageChild(int index)
        {
            if (_currentPage == null || !_currentPage.Children.ContainsIndex(index))
                return;

            if (!_currentPage.KeepRootVisible)
                _currentPage.PageRoot?.SetActive(false);

            _currentPage.OnInactive?.Invoke();

            _currentChildIndex = index;
            _currentPage = _currentPage.Children[_currentChildIndex];

            _currentPage.PageRoot.SetActive(true);
            EventManager.SetSelectedGameObject(_currentPage.FirstSelected);
            _currentPage.OnActive?.Invoke();
        }

        public void GoToPageParent()
        {
            if (_rootPage == null)
                return;

            if (_currentPage == _rootPage && _loadSceneOnFirstPage)
            {
                if (_sceneIndex == -1)
                    SceneManagerBehaviour.Instance.LoadPreviousScene();
                else
                    SceneManagerBehaviour.Instance.LoadScene(_sceneIndex);

                return;
            }

            _currentPage.PageRoot.SetActive(false);
            _currentPage.OnInactive?.Invoke();

            _currentPage = _currentPage.PageParent;

            _currentPage.PageRoot.SetActive(true);
            EventManager.SetSelectedGameObject(_currentPage.FirstSelected);
            _currentPage.OnActive?.Invoke();
        }
    }
}