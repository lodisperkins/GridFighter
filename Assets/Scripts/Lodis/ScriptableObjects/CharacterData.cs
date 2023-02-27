using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(menuName = "Character Data")]
public class CharacterData : ScriptableObject
{
    [SerializeField()]
    [Tooltip("The name of this character that will appear in the character selection screen.")]
    private string _displayName = "none";
    [SerializeField()]
    [Tooltip("The image that will display on the character selection screen.")]
    private GameObject _headShot;
    [SerializeField()]
    [Tooltip("The prefab that will be loaded when this character is spawned.")]
    private GameObject _characterReference;

    /// <summary>
    /// The name of this character that will appear in the character selection screen.
    /// </summary>
    public string DisplayName { get => _displayName; set => _displayName = value; }
    /// <summary>
    /// The image that will display on the character selection screen.
    /// </summary>
    public GameObject HeadShot { get => _headShot; set => _headShot = value; }
    /// <summary>
    /// he prefab that will be loaded when this character is spawned.
    /// </summary>
    public GameObject CharacterReference { get => _characterReference; set => _characterReference = value; }
}
