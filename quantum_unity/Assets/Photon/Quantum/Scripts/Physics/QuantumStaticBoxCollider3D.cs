using Photon.Deterministic;
using Quantum;
using System;
using Quantum.Inspector;
using UnityEngine;

public class QuantumStaticBoxCollider3D : MonoBehaviour {
#if (!UNITY_2019_1_OR_NEWER || QUANTUM_ENABLE_PHYSICS3D) && !QUANTUM_DISABLE_PHYSICS3D
  public BoxCollider SourceCollider;

  [DrawIf("SourceCollider", 0)]
  public FPVector3 Size;

  [DrawIf("SourceCollider", 0)]
  public FPVector3 PositionOffset;

  public FPVector3 RotationOffset;
  public QuantumStaticColliderSettings Settings = new QuantumStaticColliderSettings();

  public void UpdateFromSourceCollider() {
    if (SourceCollider == null) {
      return;
    }

    Size             = SourceCollider.size.ToFPVector3();
    PositionOffset   = SourceCollider.center.ToFPVector3();
    Settings.Trigger = SourceCollider.isTrigger;
  }

  public virtual void BeforeBake() {
    UpdateFromSourceCollider();
  }

  void OnDrawGizmos() {
    if (Application.isPlaying == false) {
      UpdateFromSourceCollider();
    }

    DrawGizmo(false);
  }

  void OnDrawGizmosSelected() {
    if (Application.isPlaying == false) {
      UpdateFromSourceCollider();
    }

    DrawGizmo(true);
  }

  void DrawGizmo(Boolean selected) {
    if (!QuantumGameGizmos.ShouldDraw(QuantumEditorSettings.Instance.DrawColliderGizmos, selected, false)) {
      return;
    }

    var t = transform;
    var matrix = Matrix4x4.TRS(
      t.TransformPoint(PositionOffset.ToUnityVector3()),
      Quaternion.Euler(t.rotation.eulerAngles + RotationOffset.ToUnityVector3()),
      t.localScale);
    GizmoUtils.DrawGizmosBox(matrix, Size.ToUnityVector3(), QuantumEditorSettings.Instance.StaticColliderColor, selected: selected, style: QuantumEditorSettings.Instance.StaticColliderGizmoStyle);
  }
#endif
}
