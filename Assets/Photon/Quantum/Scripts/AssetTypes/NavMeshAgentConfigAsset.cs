using UnityEngine;

[CreateAssetMenu(menuName = "Quantum/Navigation/NavMesh Agent Config", order = Quantum.EditorDefines.AssetMenuPriorityStart + 13 * 26 + 13)]
public partial class NavMeshAgentConfigAsset : AssetBase {
  public Quantum.NavMeshAgentConfig Settings;

  public override Quantum.AssetObject AssetObject => Settings;

  public override void Reset() {
    if (Settings == null) {
      Settings = new Quantum.NavMeshAgentConfig();
    }

    base.Reset();
  }
}

public static partial class NavMeshAgentConfigAssetExts {
  public static NavMeshAgentConfigAsset GetUnityAsset(this Quantum.NavMeshAgentConfig data) {
    return data == null ? null : UnityDB.FindAsset<NavMeshAgentConfigAsset>(data);
  }
}
