using DG.Tweening.Plugins.Core.PathCore;
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
        [SerializeField]
        private UnityEvent _onBindingSet;
        [SerializeField]
        private InputProfileData _defaultKeyboard;
        [SerializeField]
        private InputProfileData _defaultPS;
        [SerializeField]
        private InputProfileData _defaultXBox;

        private string _profileName;
        private bool _creatingNewProfile;
        private static string _saveLoadPath;

        public string ProfileName { get => _profileName; set => _profileName = value; }

        public string ProfilePath
        {
            get
            {
                return _saveLoadPath + "/" + ProfileName + "_" + DeviceID + "_InputProfile.txt";
            }
        }

        public string DeviceID
        {
            get
            {
                if (_profileData.DeviceData[0].description.manufacturer == "Sony Interactive Entertainment")
                    return _profileData.DeviceData[0].description.manufacturer;

                return _profileData.DeviceData[0].name;
            }
        }

        public string[] ProfileOptions { get; private set; }

        private void Awake()
        {
            _saveLoadPath = Application.persistentDataPath + "/InputProfiles";
            _anyAction = new InputAction(binding: "/*/<button>");
            _anyAction.performed += StoreBinding;
            _anyAction.Enable();

            Keyboard.current.onTextInput += StoreKeyPressed;
        }

        private void StoreKeyPressed(char  key)
        {
            if (SceneManagerBehaviour.Instance.P1Devices.Length == 0)
                return;

            if (DeviceID != "Keyboard" || !_isListening)
                return;

            string path = "Key:/Keyboard/";
            path.Append(key);
            
            _profileData.SetBinding(_currentBinding, path, char.ToUpper(key).ToString());
            _onBindingSet?.Invoke();
            _isListening = false;
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
            _isListening = isListening;
        }

        private void StoreBinding(InputAction.CallbackContext context)
        {
            if (SceneManagerBehaviour.Instance.P1Devices.Length == 0)
                return;

            if (context.control.device != SceneManagerBehaviour.Instance.P1Devices[0] || !_isListening)
                return;

            //InputBinding? binding = context.action.GetBindingForControl(context.control);

            //if (binding == null)
            //    return;

            if (_profileData.DeviceData[0].name == "Keyboard")
                return;

            _profileData.SetBinding(_currentBinding, context.control.path, context.control.displayName);
            _onBindingSet?.Invoke();
            _isListening = false;
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
            if (!Directory.Exists(_saveLoadPath))
                Directory.CreateDirectory(_saveLoadPath);

            string uniquePath = ProfilePath;
            int num = 0;

            while (File.Exists(uniquePath))
            {
                num++;
                ProfileName = ProfileName + " " + num.ToString();
                uniquePath = _saveLoadPath + "/" + ProfileName + "_" + DeviceID + "_InputProfile.txt";
            }

            ProfileName = uniquePath;

            FileStream stream = File.Create(uniquePath);
            stream.Close();

            return uniquePath;
        }

        public void ClearBindings()
        {
            _profileData.ClearBindings();
        }

        //public void SaveCharacter()
        //{
        //    if (CustomCharacter?.ArmorReplacements == null)
        //        return;

        //    string path = _creatingNewCharacter ? CreateUniqueArmorPath() : ArmorPath;

        //    StreamWriter writer = new StreamWriter(path);

        //    foreach (ArmorData data in CustomCharacter.ArmorReplacements)
        //        writer.WriteLine(data.name);

        //    writer.WriteLine("EndArmor");

        //    string colorJson = JsonConvert.SerializeObject((Vector4)CustomCharacter.HairColor);

        //    writer.WriteLine(colorJson);

        //    colorJson = JsonConvert.SerializeObject((Vector4)CustomCharacter.FaceColor);

        //    writer.WriteLine(colorJson);

        //    writer.Close();
        //}

        public void DeleteInputProfile()
        {
            string armorPath = ProfilePath;

            File.Delete(armorPath);
        }

        public static void LoadProfile(string profileName, string deviceID, out InputProfileData data)
        {
            string[] files = Directory.GetFiles(Application.persistentDataPath + "/InputProfiles", profileName + "*");

            data = null;

            if (files.Length == 0)
                return;

            string fileName = "";

            for (int i = 0; i < files.Length; i++)
            {
                files[i] = System.IO.Path.GetFileName(files[i]);
                
                if (files[i].Contains(deviceID))
                {
                    fileName = files[i];
                    break;
                }
            }

            if (fileName == "")
                return;

            string profilePath = _saveLoadPath + "/" + fileName;

            StreamReader reader = new StreamReader(profilePath);

            RebindData[] rebindData = JsonConvert.DeserializeObject<RebindData[]>(reader.ReadToEnd());

            data = InputProfileData.CreateInstance(rebindData);

            reader.Close();
        }

        public void LoadProfile(string profileName)
        {

            string[] files = Directory.GetFiles(Application.persistentDataPath + "/InputProfiles", profileName + "*");


            if (files.Length == 0)
                return;

            string fileName = "";

            for (int i = 0; i < files.Length; i++)
            {
                files[i] = System.IO.Path.GetFileName(files[i]);

                if (files[i].Contains(DeviceID))
                {
                    fileName = files[i];
                    break;
                }
            }

            if (fileName == "")
            {
                ClearBindings();
                return;
            }

            string profilePath = _saveLoadPath + "/" + fileName;

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

            string[] files = Directory.GetFiles(Application.persistentDataPath + "/InputProfiles");

            if (files.Length == 0)
                return;

            ProfileOptions = new string[files.Length];

            for (int i = 0; i < files.Length; i++)
            {
                files[i] = System.IO.Path.GetFileName(files[i]);
                string deckName = files[i].Split('_')[0];

                ProfileOptions[i] = deckName;
            }
        }

        public void SetCreatingNewProfile(bool value)
        {
            _creatingNewProfile = value;
        }

    }
}