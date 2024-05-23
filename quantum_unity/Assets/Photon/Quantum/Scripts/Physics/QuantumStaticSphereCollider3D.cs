using Photon.Deterministic;
using Quantum;
using System;
using Quantum.Inspector;
using UnityEngine;

public class QuantumStaticSphereCollider3D : MonoBehaviour {
#if (!UNITY_2019_1_OR_NEWER || QUANTUM_ENABLE_PHYSICS3D) && !QUANTUM_DISABLE_PHYSICS3D
  public SphereCollider SourceCollider;

  [DrawIf("SourceCollider", 0)]
  public FP Radius;

  [DrawIf("SourceCollider", 0)]
  public FPVector3 PositionOffset;

  public QuantumStaticColliderSettings Settings = new QuantumStaticColliderSettings();

  public void UpdateFromSourceCollider() {
    if (SourceCollider == null) {
      return;
    }

    Radius           = SourceCollider.radius.ToFP();
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

    // the radius with which the sphere with be baked into the map
    var radius = Radius.AsFloat * transform.localScale.x;
    
    GizmoUtils.DrawGizmosSphere(transform.TransformPoint(PositionOffset.ToUnityVector3()), radius, QuantumEditorSettings.Instance.StaticColliderColor, selected, style: QuantumEditorSettings.Instance.StaticColliderGizmoStyle);
  }
#endif
}
