using Lodis.Gameplay;
using Lodis.GridScripts;
using Lodis.Movement;
using Lodis.ScriptableObjects;
using Lodis.Utility;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Lodis.Gameplay
{
    /// <summary>
    /// An object that stores a renderer and th color properties to adjust on that renderer.
    /// </summary>
    [System.Serializable]
    public class ColorObject
    {
        public Renderer ObjectRenderer;
        [HideInInspector]
        public Color[] DefaultColors;
        public string[] ShaderProperties;
        [Tooltip("Whether or not new colors can only change the hue of the original color. Keeping things like saturation the same.")]
        public bool OnlyChangeHue = true;
        [Tooltip("When receiving new colors under this value, it will always override everything including the hue. Happens even if OnlyChangeHue is set to true.")]
        public float SaturationThreshold = 10;

        public ColorObject(Renderer objectRenderer, string[] shaderProperties, bool onlyChangeHue, float saturationThreshold)
        {
            ObjectRenderer = objectRenderer;
            ShaderProperties = shaderProperties;
            OnlyChangeHue = onlyChangeHue;
            SaturationThreshold = saturationThreshold;
        }



        /// <summary>
        /// Store the current colors that are on this object.
        /// </summary>
        public void CacheColors()
        {
            DefaultColors = new Color[ShaderProperties.Length];

            for (int i = 0; i < ShaderProperties.Length; i++)
            {
                for (int j = 0; j < ObjectRenderer.materials.Length; j++)
                {
                    if (ObjectRenderer.materials[j].HasProperty(ShaderProperties[i]))
                        DefaultColors[i] = ObjectRenderer.materials[j].GetColor(ShaderProperties[i]);
                }
            }
        }

        /// <summary>
        /// Override the current colors on this object with the colors currently cached.
        /// </summary>
        public void SetColorsToCache()
        {
            for (int i = 0; i < ShaderProperties.Length; i++)
            {
                for (int j = 0; j < ObjectRenderer.materials.Length; j++)
                {
                    if (ObjectRenderer.materials[j].HasProperty(ShaderProperties[i]))
                        ObjectRenderer.materials[j].SetColor(ShaderProperties[i], DefaultColors[i]);
                }
            }
        }
    }

    public class ColorManagerBehaviour : MonoBehaviour
    {
        [Tooltip("The character alignment that these objects will match the color of.")]
        [SerializeField] private GridScripts.GridAlignment _alignment;
        [Tooltip("Select this if the grid alignment should be found using an attached movement component.")]
        [SerializeField] private bool _autoDetectAlignment;
        [SerializeField] private bool _manuallySetColors;
        [Tooltip("The objects that will have their colors changed to match the alignment.")]
        [SerializeField] private ColorObject[] _objectsToColor;
        [SerializeField] private ColorManagerBehaviour _linkedManager;
        [SerializeField] private ParticleColorManagerBehaviour _linkedParticleManager;

        private Light _specularLight;

        private Color _ownerColor;

        public ColorObject[] ObjectsToColor { get => _objectsToColor; set => _objectsToColor = value; }
        public Light SpecularLight { get => _specularLight; set => _specularLight = value; }

        private void SetHue(ColorObject objectToColor)
        {
            for (int i = 0; i < objectToColor.ShaderProperties.Length; i++)
            {
                if (objectToColor.ObjectRenderer == null)
                    return;

                for (int j = 0; j < objectToColor.ObjectRenderer.materials.Length; j++)
                {
                    if (objectToColor.ObjectRenderer.materials[j].HasProperty(objectToColor.ShaderProperties[i]))
                        objectToColor.ObjectRenderer.materials[j].ChangeHue(_ownerColor, objectToColor.ShaderProperties[i]);
                }
            }
        }

        private void SetColor(ColorObject objectToColor)
        {
            if (objectToColor.ObjectRenderer == null)
                return;

            for (int i = 0; i < objectToColor.ShaderProperties.Length; i++)
            {
                for (int j = 0; j < objectToColor.ObjectRenderer.materials.Length; j++)
                {
                    if (objectToColor.ObjectRenderer.materials[j].HasProperty(objectToColor.ShaderProperties[i]))
                        objectToColor.ObjectRenderer.materials[j].SetColor(objectToColor.ShaderProperties[i], _ownerColor);
                }
            }
        }

        private void TrySetAlignment()
        {
            GridMovementBehaviour gridMovementBehaviour = transform.root.GetComponentInChildren<GridMovementBehaviour>();

            if (!gridMovementBehaviour)
                gridMovementBehaviour = GetComponentInParent<GridMovementBehaviour>();

            if (gridMovementBehaviour)
            {
                _alignment = gridMovementBehaviour.Alignment;


                if (_linkedManager)
                    _linkedManager._alignment = gridMovementBehaviour.Alignment;

                if (_linkedParticleManager)
                    _linkedParticleManager.Alignment = gridMovementBehaviour.Alignment;

                return;
            }
            PanelBehaviour panel = null;
            if (!BlackBoardBehaviour.Instance.Grid.GetPanelAtLocationInWorld(transform.root.position, out panel))
                return;

            _alignment = panel.Alignment;

            if (_linkedManager)
                _linkedManager._alignment = panel.Alignment;

            if (_linkedParticleManager)
                _linkedParticleManager.Alignment = panel.Alignment;
        }

        /// <summary>
        /// Sets all objects and their color properties to match the alignment.
        /// </summary>
        public void SetColors()
        {
            if (_autoDetectAlignment)
                TrySetAlignment();

            _ownerColor = BlackBoardBehaviour.Instance.GetPlayerColorByAlignment(_alignment);

            foreach (ColorObject colorObject in ObjectsToColor)
            {
                if (colorObject.ObjectRenderer == null)
                    continue;

                if (SpecularLight)
                {
                    colorObject.ObjectRenderer.material.SetInt("_UseCustomLight", 1);
                    colorObject.ObjectRenderer.material.SetVector("_CustomLightDirection", SpecularLight.transform.forward);
                }

                Vector3 propertyHSV;

                Color.RGBToHSV(_ownerColor, out propertyHSV.x, out propertyHSV.y, out propertyHSV.z);

                if (colorObject.OnlyChangeHue && propertyHSV.y > colorObject.SaturationThreshold)
                    SetHue(colorObject);
                else
                    SetColor(colorObject);

                colorObject.CacheColors();
            }

            _linkedManager?.SetColors();
            _linkedParticleManager?.SetColors();
        }

        /// <summary>
        /// Sets all objects and their color properties to match the alignment.
        /// </summary>
        /// <param name="alignmentID">The ID to use when finding the new alignement. 
        /// 0 = None
        /// 1 = Left
        /// 2 = Right
        /// 3 = Any</param>
        public void SetColors(int alignmentID)
        {
            GridScripts.GridAlignment alignment = (GridScripts.GridAlignment)alignmentID;
            _ownerColor = BlackBoardBehaviour.Instance.GetPlayerColorByAlignment(alignment);

            foreach (ColorObject colorObject in ObjectsToColor)
            {
                Vector3 propertyHSV;

                Color.RGBToHSV(_ownerColor, out propertyHSV.x, out propertyHSV.y, out propertyHSV.z);

                if (colorObject.OnlyChangeHue && propertyHSV.y > colorObject.SaturationThreshold)
                    SetHue(colorObject);
                else
                    SetColor(colorObject);

                colorObject.CacheColors();
            }

            _linkedManager?.SetColors(alignmentID);
            _linkedParticleManager?.SetColors((GridAlignment)alignmentID);
        }

        public void AddObjectToColor(GameObject objectToColor, params string[] shaderProperties)
        {
            SkinnedMeshRenderer objectRenderer = objectToColor.GetComponentInChildren<SkinnedMeshRenderer>();
            ColorObject colorObject = new ColorObject(objectRenderer, shaderProperties, false, 0);
            
            _objectsToColor = _objectsToColor.Add(colorObject);
        }

        public void EmptyColorArray()
        {
            ObjectsToColor = new ColorObject[0];
        }

        // Start is called before the first frame update
        void Start()
        {
            if (!_manuallySetColors)
                SetColors();
        }

    }

#if UNITY_EDITOR

    [CustomEditor(typeof(ColorManagerBehaviour))]
    public class ColorManagerEditor : Editor
    {
        private string objectPrefix = "";
        private List<string> shaderProperties = new List<string>();
        private GameObject searchTarget;
        private bool onlyChangeHue;
        private bool mustBeActive;
        private bool overwrite;
        private string shaderPropertyToRemove = "";

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            
            ColorManagerBehaviour colorManager = (ColorManagerBehaviour)target;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Auto Add Objects to Color", EditorStyles.boldLabel);
            searchTarget = (GameObject)EditorGUILayout.ObjectField("Search Target", searchTarget, typeof(GameObject), true);
            objectPrefix = EditorGUILayout.TextField("Child Object Prefix", objectPrefix);
            onlyChangeHue = EditorGUILayout.Toggle("Only Change Hue", onlyChangeHue);
            EditorGUILayout.LabelField("Shader Properties", EditorStyles.boldLabel);

            if (shaderProperties.Count == 0)
                shaderProperties.Add(""); // Ensure at least one field is present
            
            for (int i = 0; i < shaderProperties.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                shaderProperties[i] = EditorGUILayout.TextField($"Property {i + 1}", shaderProperties[i]);
                if (GUILayout.Button("Remove", GUILayout.Width(70)))
                {
                    shaderProperties.RemoveAt(i);
                    break;
                }
                EditorGUILayout.EndHorizontal();
            }

            mustBeActive = EditorGUILayout.Toggle("Must Be Active In Hierarchy", mustBeActive);
            overwrite = EditorGUILayout.Toggle("Overwrite Existing Objects", overwrite);

            if (GUILayout.Button("Add Property"))
                shaderProperties.Add("");

            EditorGUILayout.Space();
            if (GUILayout.Button("Add Objects to Color"))
            {
                AddObjectsToColor(colorManager);
            }

            shaderPropertyToRemove = EditorGUILayout.TextField($"Property To Remove ", shaderPropertyToRemove);

            if (GUILayout.Button("Remove Shader Property"))
            {
                RemoveShaderProperty(shaderPropertyToRemove, colorManager);
            }
        }

        private void AddObjectsToColor(ColorManagerBehaviour colorManager)
        {
            List<ColorObject> newColorObjects = overwrite ? new List<ColorObject>() : colorManager.ObjectsToColor.ToList();
            Transform parentTransform = searchTarget == null ? colorManager.transform : searchTarget.transform;

            foreach (Transform child in parentTransform)
            {
                if (!string.IsNullOrEmpty(objectPrefix) && !child.name.StartsWith(objectPrefix))
                    continue;
                
                Renderer renderer = child.GetComponentInChildren<Renderer>();

                if (!child.gameObject.activeInHierarchy && mustBeActive)
                    continue;

                if (renderer != null)
                {
                    ColorObject colorObject = new ColorObject(renderer, shaderProperties.ToArray(), false, 0);
                    colorObject.OnlyChangeHue = onlyChangeHue;
                    colorObject.SaturationThreshold = 10;
                    newColorObjects.Add(colorObject);
                }
            }

            colorManager.ObjectsToColor = newColorObjects.ToArray();
            EditorUtility.SetDirty(colorManager);
        }

        private void RemoveShaderProperty(string shaderProperty, ColorManagerBehaviour colorManager)
        {
            foreach (ColorObject colorObject in colorManager.ObjectsToColor)
            {
                List<string> updatedProperties = colorObject.ShaderProperties.ToList();
                updatedProperties.RemoveAll(prop => prop == shaderProperty);
                colorObject.ShaderProperties = updatedProperties.ToArray();
            }
            EditorUtility.SetDirty(colorManager);
        }
    }
#endif
}