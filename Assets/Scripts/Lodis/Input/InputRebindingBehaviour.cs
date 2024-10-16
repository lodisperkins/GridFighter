﻿using DG.Tweening.Plugins.Core.PathCore;
using DG.Tweening.Plugins.Options;
using Lodis.ScriptableObjects;
using Lodis.Utility;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Policy;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using UnityEngine.InputSystem.Utilities;

namespace Lodis.Input
{
    public enum BindingType
    {
        MoveUp,
        MoveDown,
        MoveLeft,
        MoveRight,
        WeakAttack,
        StrongAttack,
        Special1,
        Special2,
        Burst,
        Shuffle
    }

    [Serializable]
    public class RebindData
    {
        public BindingType Binding;
        public string Path = "";
        public string DisplayName = "";

        public RebindData(BindingType binding, string path, string displayName)
        {
            Binding = binding;
            Path = path;
            DisplayName = displayName;
        }

        public void Init(RebindData data)
        {
            Binding = data.Binding;
            Path = data.Path;
            DisplayName = data.DisplayName;
        }
    }

    public class InputRebindingBehaviour : MonoBehaviour
    {
        private InputAction _anyAction;
        [SerializeField]
        private InputProfileData _profileData;
        [SerializeField]
        private BindingType _currentBinding;
        [SerializeField]
        private bool _isListening;

        [Header("Binding Events")]

        [SerializeField]
        private UnityEvent _onListen;
        [SerializeField]
        private UnityEvent _onBindingSet;
        [SerializeField]
        private UnityEvent _onStopListening;

        [Header("Default Input Bindings")]
        [SerializeField]
        private InputProfileData _defaultKeyboard;
        [SerializeField]
        private InputProfileData _defaultPS;
        [SerializeField]
        private InputProfileData _defaultXBox;

        private string _profileName;
        private bool _creatingNewProfile;
        private static string _saveLoadPath;
        private TimedAction _timedAction;

        public string ProfileName { get => _profileName; set => _profileName = value; }

        public string ProfileFolderPath
        {
            get
            {
                return _saveLoadPath + "/" + ProfileName + "/";
            }
        }

        public string ProfilePath
        {
            get
            {
                return ProfileFolderPath + ProfileName + "_" + DeviceID + "_InputProfile.txt";
            }
        }

        public string DeviceID
        {
            get
            {
                if (_profileData.DeviceData.Length == 0)
                    return "";

                if (_profileData.DeviceData[0].description.manufacturer == "Sony Interactive Entertainment")
                    return _profileData.DeviceData[0].description.manufacturer;

                return _profileData.DeviceData[0].name;
            }
        }

        public string[] ProfileOptions { get; private set; }
        public bool IsListening
        {
            get => _isListening;
            set
            {
                if (_isListening != value && value)
                    _onListen?.Invoke();
                else if (_isListening != value && !value)
                    _onStopListening?.Invoke();

                _isListening = value;
            }
        }

        private void Awake()
        {
            _saveLoadPath = Application.persistentDataPath + "/InputProfiles";
            _anyAction = new InputAction(binding: "/*/<button>", type: InputActionType.Button);
            _anyAction.performed += context =>
            {
                if (DeviceID != "Keyboard")
                    StoreBinding(context.control);
            };

            _anyAction.Enable();
        }

        private void StoreKeyPressed(char  key)
        {
            if (SceneManagerBehaviour.Instance.P1Devices.Length == 0)
                return;

            if (DeviceID != "Keyboard" || !IsListening)
                return;

            string path = "Key:/Keyboard/";
            path.Append(key);
            
            _profileData.SetBinding(_currentBinding, path, char.ToUpper(key).ToString());
            _onBindingSet?.Invoke();
            IsListening = false;
        }

        public void SetProfileName(Text text)
        {
            ProfileName = text.text;   
        }

        public void SetCurrentKey(string key)
        {
            Enum.TryParse(key, out _currentBinding);
        }

        public void SetIsListening(bool isListening)
        {
            RoutineBehaviour.Instance.StopAction(_timedAction);
            _timedAction = RoutineBehaviour.Instance.StartNewTimedAction(args => IsListening = isListening, TimedActionCountType.FRAME, 3);
        }

        private void StoreBinding(InputControl control)
        {
            if (SceneManagerBehaviour.Instance.P1Devices.Length == 0)
                return;

            if (control.device != SceneManagerBehaviour.Instance.P1Devices[0] || !IsListening)
                return;

            if (control.displayName.ToLower() == "system" || control.displayName.ToLower() == "select"|| control.displayName.ToLower() == "start")
                return;

            _profileData.SetBinding(_currentBinding, control.path, control.displayName);
            _profileData.RemoveDuplicates(_currentBinding);

            _onBindingSet?.Invoke();

            SetIsListening(false);
        }

        public void ResetToDefault()
        {
            if (DeviceID == "Sony Interactive Entertainment")
                _profileData.Init(_defaultPS);
            else if (DeviceID == "Keyboard" || DeviceID == "Mouse")
                _profileData.Init(_defaultKeyboard);
            else
                _profileData.Init(_defaultXBox);
        }

        public void SaveBindings()
        {
            if (_profileData?.DeviceData == null)
                return;

            string path = _creatingNewProfile ? CreateUniqueProfilePath() : ProfilePath;

            StreamWriter writer = new StreamWriter(path);

            string bindingJson = JsonConvert.SerializeObject(_profileData.Value);

            writer.Write(bindingJson);

            writer.Close();
        }

        private string CreateUniqueProfilePath()
        {
            //Create the input profile folder if it doesn't exists.
            if (!Directory.Exists(_saveLoadPath))
                Directory.CreateDirectory(_saveLoadPath);


            //Build complete path.
            string uniquePath = ProfileFolderPath + ProfileName + "_" + DeviceID + "_InputProfile.txt";

            //Create the player profile folder if it doesn't exists.
            if (!Directory.Exists(ProfileFolderPath))
            {
                Directory.CreateDirectory(ProfileFolderPath);
                return uniquePath;
            }

            int num = 0;

            //While there is still a folder with the name
            while (Directory.Exists(ProfileFolderPath))
            {
                //..increment the name number.
                num++;
                ProfileName = ProfileName + " " + num.ToString();
            }

            Directory.CreateDirectory(ProfileFolderPath);

            uniquePath = ProfileFolderPath + ProfileName + "_" + DeviceID + "_InputProfile.txt";

            FileStream stream = File.Create(uniquePath);
            stream.Close();

            return uniquePath;
        }

        public void ClearBindings()
        {
            _profileData.ClearBindings();
        }

        public void DeleteInputProfile()
        {
            //Check if the name is valid before deleting.
            if (ProfileName == "" || ProfileOptions.Length == 0)
                return;

            //---Update profile options collection---

            //Create a new string array thats one less.
            string[] temp = new string[ProfileOptions.Length - 1];

            //Copy over all values except the one we don't want.
            int j = 0;
            for (int i = 0; i < ProfileOptions.Length; i++)
            {
                if (ProfileOptions[i] == ProfileName)
                {
                    continue;
                }
                else
                {
                    temp[j] = ProfileOptions[i];
                    j++;
                }
            }

            ProfileOptions = temp;  

            //Delete the entire folder and all of its contents.
            Directory.Delete(ProfileFolderPath, true);
        }

        public static void LoadProfile(string profileName, string deviceID, out InputProfileData data)
        {
            string[] files = Directory.GetFiles(Application.persistentDataPath + "/InputProfiles/" + profileName + "/", profileName + "*");

            data = null;

            //Leave if the folder didn't contain any files.
            if (files.Length == 0)
                return;

            string fileName = "";

            //Get the file for the specific device being used.
            for (int i = 0; i < files.Length; i++)
            {
                files[i] = System.IO.Path.GetFileName(files[i]);
                
                if (files[i].Contains(deviceID))
                {
                    fileName = files[i];
                    break;
                }
            }

            //Leave if the device file couldn't be found.
            if (fileName == "")
                return;


            //Load the profiles rebind data.
            string profilePath = Application.persistentDataPath + "/InputProfiles/" + profileName + "/" + fileName;

            StreamReader reader = new StreamReader(profilePath);

            RebindData[] rebindData = JsonConvert.DeserializeObject<RebindData[]>(reader.ReadToEnd());

            data = InputProfileData.CreateInstance(rebindData);

            reader.Close();
        }

        public void LoadProfile(string profileName)
        {

            string[] files = Directory.GetFiles(Application.persistentDataPath + "/InputProfiles/" + profileName + "/", profileName + "*");


            if (files.Length == 0)
                return;

            string fileName = "";
            bool fileFound = false;

            for (int i = 0; i < files.Length; i++)
            {
                files[i] = System.IO.Path.GetFileName(files[i]);

                if (files[i].Contains(DeviceID))
                {
                    fileName = files[i];
                    fileFound = true;
                    break;
                }
            }

            if (!fileFound)
            {
                ResetToDefault();
                return;
            }

            string profilePath = Application.persistentDataPath + "/InputProfiles/" + profileName + "/" + fileName;

            StreamReader reader = new StreamReader(profilePath);

            RebindData[] rebindData = JsonConvert.DeserializeObject<RebindData[]>(reader.ReadToEnd());

            _profileData.Init(rebindData);
            ProfileName = profileName;

            reader.Close();
        }

        public void LoadProfileNames()
        {
            if (!Directory.Exists(Application.persistentDataPath + "/InputProfiles"))
                return;

            string[] files = Directory.GetDirectories(Application.persistentDataPath + "/InputProfiles").Select(System.IO.Path.GetFileName).ToArray();

            if (files.Length == 0)
                return;

            ProfileOptions = new string[files.Length];

            for (int i = 0; i < files.Length; i++)
            {
                ProfileOptions[i] = files[i];
            }
        }

        public void SetCreatingNewProfile(bool value)
        {
            _creatingNewProfile = value;
        }


        private void Update()
        {
            if (IsListening && DeviceID == "Keyboard")
                InputSystem.onAnyButtonPress.CallOnce(StoreBinding);
        }
    }
}