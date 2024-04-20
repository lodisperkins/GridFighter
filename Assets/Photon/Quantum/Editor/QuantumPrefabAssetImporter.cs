namespace Quantum.Editor {
  using System;
  using System.IO;
  using System.Linq;
  using UnityEditor;
#if UNITY_2020_2_OR_NEWER
  using UnityEditor.AssetImporters;
#else
  using UnityEditor.Experimental.AssetImporters;
#endif
  using UnityEngine;

#if (QUANTUM_ADDRESSABLES || QUANTUM_ENABLE_ADDRESSABLES) && !QUANTUM_DISABLE_ADDRESSABLES
  using UnityEngine.AddressableAssets;
  using UnityEditor.AddressableAssets;
#endif


  [ScriptedImporter(2, Extension, 100000)]
  public partial class QuantumPrefabAssetImporter : ScriptedImporter {
    public const string Extension = "qprefab";
    public const string ExtensionWithDot = ".qprefab";
    public const string Suffix = "_data";

    public static string GetPath(string prefabPath) {
      var directory = Path.GetDirectoryName(prefabPath);
      var name = Path.GetFileNameWithoutExtension(prefabPath);
      return PathUtils.MakeSane(Path.Combine(directory, name + Suffix + ExtensionWithDot));
    }

    partial void CreateRootAssetUser(ref QuantumPrefabAsset root);

    public override void OnImportAsset(AssetImportContext ctx) {
      var path = ctx.assetPath;

      var prefabGuid = File.ReadAllText(path);
      var prefabPath = AssetDatabase.GUIDToAssetPath(prefabGuid);
      var prefab = string.IsNullOrEmpty(prefabPath) ? null : AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
      if (prefab == null) {
        ctx.LogImportError($"Unable to load prefab: {prefabGuid}");
        return;
      }
      ctx.DependsOnSourceAsset(prefabPath);

      // sync paths
      var desiredPath = GetPath(prefabPath);
      if (PathUtils.MakeSane(path) != desiredPath) {
        EditorApplication.delayCall += () => {
          AssetDatabase.MoveAsset(path, desiredPath);
        };
      }

      // create root object
      QuantumPrefabAsset root = null;
      CreateRootAssetUser(ref root);
      if (root == null) {
#if (QUANTUM_ADDRESSABLES || QUANTUM_ENABLE_ADDRESSABLES) && !QUANTUM_DISABLE_ADDRESSABLES
#if QUANTUM_ENABLE_ADDRESSABLES_FIND_ASSET_ENTRY
        var addressableEntry = AddressableAssetSettingsDefaultObject.Settings.FindAssetEntry(prefabGuid, true);
#else
        var lookup = AssetDBGeneration.CreateAddressablesLookup();
        var addressableEntry = lookup[prefabGuid].SingleOrDefault();
#endif
        if (addressableEntry != null) {
          var entry = ScriptableObject.CreateInstance<QuantumPrefabAsset_Addressable>();
          entry.Address = new AssetReferenceGameObject(prefabGuid);
          root = entry;
        } else
#endif
      {
          var prefabBundle = AssetDatabase.GetImplicitAssetBundleName(prefabPath);
          if (!string.IsNullOrEmpty(prefabBundle)) {
            var entry = ScriptableObject.CreateInstance<QuantumPrefabAsset_AssetBundle>();
            entry.AssetBundle = prefabBundle;
            entry.AssetName = Path.GetFileName(prefabPath);
            root = entry;
          } else if (PathUtils.MakeRelativeToFolder(prefabPath, "Resources", out var resourcePath)) {
            var entry = ScriptableObject.CreateInstance<QuantumPrefabAsset_Resource>();
            entry.ResourcePath = PathUtils.GetPathWithoutExtension(resourcePath);
            root = entry;
          } else {
            ctx.LogImportError($"Unable to determine how the source prefab can be loaded. Assign Address, set Asset Bundle, move to Resources or implement " +
              $" QuantumPrefabAssetImporter.CreateRootAssetUser");
            return;
          }
        }
      }

      root.PrefabGuid = prefabGuid;
      root.name = prefab.name;
      ctx.AddObjectToAsset("root", root);

      // discover nested assets
      var components = prefab.GetComponents<MonoBehaviour>()
        .OfType<IQuantumPrefabNestedAssetHost>()
        .ToList();

      if (!components.Any()) {
        ctx.LogImportWarning($"Prefab {prefabPath} does not have any {nameof(IQuantumPrefabNestedAssetHost)} components, this qprefab is pointless");
      } else {
        foreach (var component in components) {
          var nestedAsset = NestedAssetBaseEditor.GetNested((Component)component, component.NestedAssetType);
          if (nestedAsset == null) {
            ctx.LogImportError($"Not found {component.NestedAssetType}");
            continue;
          } 

          var instance = (AssetBase)ScriptableObject.CreateInstance(component.SplitAssetType);
          instance.name = NestedAssetBaseEditor.GetName(instance, root) + Suffix;

          var bakedAsset = (IQuantumPrefabBakedAsset)instance;
          bakedAsset.Import(root, nestedAsset);

          // ideally we would like to hide these assets, but Resources/Bundles/Addressables stop working :(
          // instance.hideFlags = HideFlags.HideInHierarchy;

          ctx.AddObjectToAsset(component.GetType().Name, instance);
        }
      }
    }
  }
}