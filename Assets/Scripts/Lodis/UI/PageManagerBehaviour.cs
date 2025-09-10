using Lodis.ScriptableObjects;
using Lodis.Utility;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Lodis.UI
{
    [System.Serializable]
    public class Page
    {
        public GameObject PageRoot;
        public GameObject FirstSelected;
        public bool KeepRootVisible;
        public string PageName = "None";
        [SerializeField]
        private Page[] _children;
        private bool _childrenInitialized;
        [HideInInspector]
        public Page PageParent;
        public UnityEvent OnActive;
        public UnityEvent OnInactive;
        public UnityEvent OnGoToParent;
        public BoolVariable GoToParentCondition;
        public UnityEvent OnGoToChild;
        public BoolVariable GoToChildCondition;

        public Page GetChildByName(string name)
        {
            foreach (Page child in Children)
            {
                Page target;
                if (child.PageName == name)
                {
                    return child;
                }
                else if ((target = child.GetChildByName(name)) != null)
                {
                    return target;
                }
            }

            return null;
        }

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
        [SerializeField]
        [Tooltip("If true pages will no longer turn on/off automatically. Use this if you are going to handle page visuals manually.")]
        private bool _changePageManually;
        [SerializeField] private bool _useEventSystemScheme;
        [SerializeField] private UnityEvent _onPreviousPagePressed;

        //---
        private int _currentChildIndex;
        private PlayerControls _controls;
        private PlayerInput _playerInput;

        public UnityEngine.EventSystems.EventSystem EventManager { get => _eventSystem; set => _eventSystem = value; }
        public Page RootPage { get => _rootPage; private set => _rootPage = value; }
        public Page CurrentPage { get => _currentPage; private set => _currentPage = value; }
        public bool UseEventSystemScheme { get => _useEventSystemScheme; set => _useEventSystemScheme = value; }

        public void Awake()
        {
            if (!_previousPageOnCancel)
                return;

            _controls = new PlayerControls();

            _controls.UI.Cancel.started += context =>
            {
                // Return early if previous page navigation is disabled
                if (!_previousPageOnCancel)
                    return;

                _onPreviousPagePressed?.Invoke();

                // If UseEventSystemScheme is false, always allow going to the previous page
                if (!UseEventSystemScheme)
                {
                    GoToPageParent();
                    return;
                }

                // Get the event system's control scheme
                PlayerInput playerInput = _eventSystem.GetComponent<PlayerInput>();
                if (playerInput == null) return;

                string eventSystemScheme = playerInput.currentControlScheme; // The active scheme used by the EventSystem

                // Get control that triggered the context
                var control = context.control;

                // Get the binding for the control
                var binding = context.action.GetBindingForControl(control);
                if (!binding.HasValue) return;

                // Extract the control scheme name from the binding groups
                if (binding.Value.groups == null) return;

                string[] splitResult = binding.Value.groups.Split(';');
                string activePlayerControlScheme = string.Empty;

                if (splitResult.Length > 0) 
                    activePlayerControlScheme = splitResult[0]; // Take the first scheme if multiple

                // Only go back if both control schemes match
                if (!string.IsNullOrEmpty(activePlayerControlScheme) && activePlayerControlScheme == eventSystemScheme)
                {
                    GoToPageParent();
                }
            };


            //_playerInput = GetComponent<PlayerInput>();

            //_playerInput.actions.FindActionMap("UI").FindAction("Cancel").started += context => GoToPageParent();

            CurrentPage = RootPage;
        }

        /// <summary>
        /// Converts device layout names into proper control schemes.
        /// </summary>
        private string ConvertDeviceToScheme(string deviceLayout)
        {
            if (deviceLayout.Contains("Gamepad")) return "Gamepad";
            if (deviceLayout.Contains("Keyboard") || deviceLayout.Contains("Mouse")) return "Keyboard&Mouse";
            return deviceLayout; // Default return if no match found
        }

        public void OnEnable()
        {
            _controls?.Enable();
        }

        public void OnDisable()
        {
            _controls?.Disable();
        }

        private Page FindPage(Page current, string name)
        {
            if (current.PageName == name)
                return current;

            Page target = current.GetChildByName(name);

            if (target != null)
                return target;

            foreach (Page child in current.Children)
            {
                target = FindPage(child, name);

                if (target != null)
                    break;
            }


            return target;
        }

        public void GoToPage(string name)
        {
            Page targetPage = FindPage(RootPage, name);

            if (targetPage == null)
            {
                Debug.LogError("Couldn't find page with name " + name);
                return;
            }

            if (CurrentPage == null)
            {
                CurrentPage = targetPage;
            }
            else
            {
                if (!CurrentPage.KeepRootVisible || _changePageManually)
                    CurrentPage.PageRoot?.SetActive(false);

                CurrentPage.OnInactive?.Invoke();

                CurrentPage = targetPage;
            }

            if (!_changePageManually)
                CurrentPage.PageRoot.SetActive(true);

            if (EventManager)
            {
                EventManager.SetSelectedGameObject(CurrentPage.FirstSelected);
                EventManager.UpdateModules();
            }

            CurrentPage.OnActive?.Invoke();
        }

        public void GoToRootPage()
        {
            if (RootPage == null || CurrentPage == RootPage)
                return;

            if (CurrentPage != null)
            {
                if (!_changePageManually)
                    CurrentPage.PageRoot.SetActive(false);

                CurrentPage.OnInactive?.Invoke();
            }

            CurrentPage = RootPage;

            if (!_changePageManually)
                CurrentPage.PageRoot.SetActive(true);

            if (EventManager)
            {
                EventManager.SetSelectedGameObject(CurrentPage.FirstSelected);
                EventManager.UpdateModules();
            }
            CurrentPage.OnActive?.Invoke();
        }

        public void GoToPageChild(int index)
        {
            if (CurrentPage == null || !CurrentPage.Children.ContainsIndex(index))
                return;

            if (CurrentPage.GoToChildCondition?.Value == false)
            {
                return;
            }

            if (!CurrentPage.KeepRootVisible && !_changePageManually)
                CurrentPage.PageRoot?.SetActive(false);

            CurrentPage.OnInactive?.Invoke();
            CurrentPage.OnGoToChild?.Invoke();

            _currentChildIndex = index;
            CurrentPage = CurrentPage.Children[_currentChildIndex];

            if (!_changePageManually)
                CurrentPage.PageRoot.SetActive(true);

            if (EventManager)
            {
                EventManager.SetSelectedGameObject(CurrentPage.FirstSelected);
                EventManager.UpdateModules();
            }
            CurrentPage.OnActive?.Invoke();
        }

        public void GoToPageParent()
        {
            if (RootPage == null || (CurrentPage?.PageParent == null && CurrentPage != RootPage))
                return;

            if (CurrentPage.GoToParentCondition?.Value == false)
            {
                return;
            }

            if (CurrentPage == RootPage && _loadSceneOnFirstPage)
            {
                if (_sceneIndex == -1)
                    SceneManagerBehaviour.Instance.LoadPreviousScene();
                else
                    SceneManagerBehaviour.Instance.LoadScene(_sceneIndex);

                return;
            }
            else if (CurrentPage == _rootPage)
                return;

            if (!_changePageManually)
                CurrentPage.PageRoot.SetActive(false);

            CurrentPage.OnInactive?.Invoke();
            CurrentPage.OnGoToParent?.Invoke();

            CurrentPage = CurrentPage.PageParent;

            if (!_changePageManually)
                CurrentPage.PageRoot.SetActive(true);

            if (EventManager)
            {
                EventManager.SetSelectedGameObject(CurrentPage.FirstSelected);
                EventManager.UpdateModules();
            }
            CurrentPage.OnActive?.Invoke();
        }

        public void SetPreviousOnCancel(bool val)
        {
            _previousPageOnCancel = val;
        }

        private void Update()
        {
            //Debug.Log(_controls.UI.enabled);    
        }
    }
}