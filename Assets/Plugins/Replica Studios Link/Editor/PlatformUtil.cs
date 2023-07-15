using System.Linq;
using UnityEditor;
using UnityEngine;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace Packages.Replica.Bridge.Editor
{
    public static class PlatformUtil
    {
        public static PackageInfo ThisPackageInfo()
        {
            try
            {
                var packageJsons = AssetDatabase.FindAssets("package")
                    .Select(AssetDatabase.GUIDToAssetPath)
                    .Where(x => AssetDatabase.LoadAssetAtPath<TextAsset>(x) != null)
                    .Select(PackageInfo.FindForAssetPath).ToList();

                return packageJsons.First(p => p.name == "com.replicastudios.bridge");
            }
            catch
            {
                return null;
            }
        }
    }
}