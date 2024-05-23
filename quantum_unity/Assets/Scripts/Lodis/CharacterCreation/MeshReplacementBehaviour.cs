using Lodis.Gameplay;
using Lodis.ScriptableObjects;
using Lodis.Utility;
using NaughtyAttributes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[System.Serializable]
public class Wearable
{
    public GameObject[] WearableItems;
    public string ID;
    public BodySection Section;
    public Sprite DisplayIcon;
    public bool IsActive;
    public bool IsHair;

    public Wearable(GameObject[] wearableItems, string iD, BodySection section, Sprite displayIcon)
    {
        WearableItems = wearableItems;
        ID = iD;
        Section = section;
        DisplayIcon = displayIcon;
    }

    public void SetAllItemsEnabled(bool enabled)
    {
        if (WearableItems == null)
            return;

        foreach (var item in WearableItems)
        {
            item.SetActive(enabled);
        }

        IsActive = enabled;
    }

    public T GetComponentInItems<T>() where T : Component
    {
        if (WearableItems == null)
            return null; 

        foreach(var item in WearableItems)
        {
            if (item.TryGetComponent(out T component))
                return component;
        }

        return default;
    }
}

public class MeshReplacementBehaviour : MonoBehaviour
{

    [SerializeField]
    private Wearable[] _defaultWearables;
    [SerializeField]
    private bool _setDefaultOnStart;
    [SerializeField]
    private Sprite _removeWearableSprite;

    [SerializeField]
    private Wearable[] _wearables;
    private Dictionary<int, Wearable> _wearableDictionary = new Dictionary<int, Wearable>();
    [SerializeField]
    private List<Wearable> _activeWearables = new List<Wearable>();
    [SerializeField]
    private SkinnedMeshRenderer _bodyRenderer;
    [SerializeField]
    private SkinnedMeshRenderer _noseRenderer;
    [SerializeField]
    private SkinnedMeshRenderer _lEarRenderer;
    [SerializeField]
    private SkinnedMeshRenderer _rEarRenderer;
    [SerializeField]
    private SkinnedMeshRenderer _hairRenderer;
    [SerializeField]
    private string[] _shaderColorProperties;


    private Wearable _currentHeadWearable;
    private Wearable _currentFaceWearable;
    private Wearable _currentChestWearable;
    private Wearable _currentArmWearable;
    private Wearable _currentWaistWearable;
    private Wearable _currentLegWearable;
    private Wearable _currentFeetWearable;

    private ColorManagerBehaviour _colorManager;

    private bool _hasDefaultOutfit;

    public Color FaceColor 
    {
        get => _bodyRenderer.material.GetColor("_Color");
        set
        { 
            _bodyRenderer.material.SetColor("_Color", value);
            _noseRenderer.material.SetVector("_Color", value);
            _lEarRenderer.material.SetVector("_Color", value);
            _rEarRenderer.material.SetVector("_Color", value);
        }
    }
    public Color HairColor
    {
        get
        {
            if (!_hairRenderer)
                return default;

            return _hairRenderer.material.GetColor("_Color");
        }
        set
        {
            if (_hairRenderer)
                _hairRenderer.material.SetColor("_Color", value);
        }
    }
    public bool HasDefaultOutfit { get => _hasDefaultOutfit; set => _hasDefaultOutfit = value; }
    public Dictionary<int, Wearable> WearableDictionary 
    {
        get => _wearableDictionary;
        private set
        {
            if (_wearableDictionary == null || _wearableDictionary.Count == 0)
                _wearableDictionary = value;
        }
    }

    private void Awake()
    {
        _colorManager = GetComponent<ColorManagerBehaviour>();
        GenerateDictionary();

        if (_setDefaultOnStart)
            SetOutfitToDefault();
    }

    private void GenerateDictionary()
    {
        for (int i = 0; i < _wearables.Length; i++)
        {
            Wearable current = _wearables[i];

            int cosmeticHash = current.ID.GetHashCode();

            current.SetAllItemsEnabled(false);
            WearableDictionary.Add(cosmeticHash, current);
        }

        for (int i = 0; i < 7; i++)
        {
            Wearable none = new Wearable(null, "None_" + (BodySection)i, (BodySection)i, _removeWearableSprite);
            int hash = none.ID.GetHashCode();

            _wearableDictionary.Add(hash, none);
        }
    }

    public Wearable GetCurrentWearable(BodySection section)
    {
        switch (section)
        {
            case BodySection.HEAD:
                return _currentHeadWearable;
            case BodySection.FACE:
                return _currentFaceWearable;
            case BodySection.CHEST:
                return _currentChestWearable;
            case BodySection.ARMS:
                return _currentArmWearable; 
            case BodySection.WAIST:
                return _currentWaistWearable; 
            case BodySection.LEGS:
                return _currentLegWearable;
            case BodySection.FEET:
                return _currentFeetWearable;
            default:
                return null;
        }
    }

    public void SetCurrentWearable(Wearable data)
    {
        switch (data.Section)
        {
            case BodySection.HEAD:
                _currentHeadWearable = data;
                break;
            case BodySection.FACE:
                _currentFaceWearable = data;
                break;
            case BodySection.CHEST:
                _currentChestWearable = data;
                break;
            case BodySection.ARMS:
                _currentArmWearable = data;
                break;
            case BodySection.WAIST:
                _currentWaistWearable = data;
                break;
            case BodySection.LEGS:
                _currentLegWearable = data;
                break;
            case BodySection.FEET:
                _currentFeetWearable = data;
                break;
        }
    }

    public void ReplaceMeshes(List<string> IDs)
    {
        foreach (string ID in IDs)
        {
            ReplaceWearable(ID);
        }
    }

    public void ReplaceMeshes(string[] IDs)
    {
        foreach (string ID in IDs)
        {
            ReplaceWearable(ID);
        }
    }

    public void ReplaceMeshes(Wearable[] wearables)
    {
        foreach (Wearable item in wearables)
        {
            ReplaceWearable(item.ID);
        }
    }

    public void RemoveCurrentWearables(List<BodySection> sections)
    {
        foreach (BodySection section in sections)
        {
            RemoveWearable(section);
        }
    }

    public void SetCurrentWearablesActive(bool active)
    {
        _currentHeadWearable.SetAllItemsEnabled(active);
        _currentFaceWearable.SetAllItemsEnabled(active);
        _currentChestWearable.SetAllItemsEnabled(active);
        _currentArmWearable.SetAllItemsEnabled(active);
        _currentWaistWearable.SetAllItemsEnabled(active);
        _currentLegWearable.SetAllItemsEnabled(active);
        _currentFeetWearable.SetAllItemsEnabled(active);
    }

    public void ReplaceWearable(string ID)
    {
        int IDHash = ID.GetHashCode();

        Wearable replacementWearable = WearableDictionary[IDHash];

        Wearable activeWearable = GetCurrentWearable(replacementWearable.Section);

        if (IDHash == activeWearable?.ID.GetHashCode())
            return;

        replacementWearable.SetAllItemsEnabled(true);

        if (activeWearable != null)
            activeWearable.SetAllItemsEnabled(false);

        SetCurrentWearable(replacementWearable);

        if (replacementWearable.Section == BodySection.HEAD && replacementWearable.IsHair)
            _hairRenderer = replacementWearable.GetComponentInItems<SkinnedMeshRenderer>();

        _colorManager.EmptyColorArray();

        if (_currentHeadWearable.IsHair)
            AddWearableToColorArray(_currentHeadWearable, new string[4] {"_ShadowColor", "_ShadowPatternColor", "_OutlineColor", "_EmissionColor"});
        else
            AddWearableToColorArray(_currentHeadWearable);

        AddWearableToColorArray(_currentFaceWearable);
        AddWearableToColorArray(_currentChestWearable);
        AddWearableToColorArray(_currentArmWearable);
        AddWearableToColorArray(_currentWaistWearable);
        AddWearableToColorArray(_currentLegWearable);
        AddWearableToColorArray(_currentFeetWearable);

        _colorManager.AddObjectToColor(_bodyRenderer.gameObject,new string[4] {"_ShadowColor", "_ShadowPatternColor", "_OutlineColor", "_EmissionColor"});
        _colorManager.SetColors();


        HasDefaultOutfit = false;
    }

    private void AddWearableToColorArray(Wearable wearable, string[] shaderProperties = null)
    {
        if (wearable == null || wearable.ID.Contains("None"))
            return;

        if (shaderProperties == null)
            shaderProperties = _shaderColorProperties;

        for (int i = 0; i < wearable.WearableItems.Length; i++)
        {
            _colorManager.AddObjectToColor(wearable.WearableItems[i], shaderProperties);
        }
    }

    public void RemoveWearable(BodySection section)
    {
        Wearable wearable = GetCurrentWearable(section);

        wearable.SetAllItemsEnabled(false);
    }

    [Button]
    public void SetOutfitToDefault()
    {
        if (!Application.isPlaying)
        {
            _colorManager = GetComponent<ColorManagerBehaviour>();
            _wearableDictionary = new Dictionary<int, Wearable>();
            GenerateDictionary();
        }

        ReplaceMeshes(_defaultWearables);
        HasDefaultOutfit = true;
    }

    public void SaveOutfit(StreamWriter writer)
    {
        writer.WriteLine(_currentHeadWearable.ID);
        writer.WriteLine(_currentFaceWearable.ID);
        writer.WriteLine(_currentChestWearable.ID);
        writer.WriteLine(_currentArmWearable.ID);
        writer.WriteLine(_currentWaistWearable.ID);
        writer.WriteLine(_currentLegWearable.ID);
        writer.WriteLine(_currentFeetWearable.ID);
    }

    public void LoadOutfit(StreamReader reader)
    {
        for (int i = 0; i < 7; i++)
        {
            string name = reader.ReadLine();
            ReplaceWearable(name);
        }
    }

    public bool GetIsWearingItem(Wearable wearable)
    {
        Wearable current = GetCurrentWearable(wearable.Section);
        return current == null || wearable.ID == current.ID;
    }

    public bool GetIsWearingItem(string ID)
    {
        int hash = ID.GetHashCode();
        return WearableDictionary[hash].IsActive;
    }
}
