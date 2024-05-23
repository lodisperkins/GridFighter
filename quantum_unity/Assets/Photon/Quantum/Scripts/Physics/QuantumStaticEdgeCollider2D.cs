using Photon.Deterministic;
using UnityEngine;
using System;
using Quantum;
using Quantum.Inspector;

public class QuantumStaticEdgeCollider2D : MonoBehaviour {
#if (!UNITY_2019_1_OR_NEWER || QUANTUM_ENABLE_PHYSICS2D) && !QUANTUM_DISABLE_PHYSICS2D
  public EdgeCollider2D SourceCollider;

  [DrawIf("SourceCollider", 0)]
  public FPVector2 VertexA = new FPVector2(2, 2);

  [DrawIf("SourceCollider", 0)]
  public FPVector2 VertexB = new FPVector2(-2, -2);

  [DrawIf("SourceCollider", 0)]
  public FPVector2 PositionOffset;

  public FP RotationOffset;
  public FP Height;
  public QuantumStaticColliderSettings Settings = new QuantumStaticColliderSettings();

  public void UpdateFromSourceCollider() {
    if (SourceCollider == null) {
      return;
    }

    Settings.Trigger = SourceCollider.isTrigger;
    PositionOffset   = SourceCollider.offset.ToFPVector2();

    VertexA = SourceCollider.points[0].ToFPVector2();
    VertexB = SourceCollider.points[1].ToFPVector2();
  }

  public virtual void BeforeBake() {
    UpdateFromSourceCollider();
  }

  void OnDrawGizmos() {
    if (Application.isPlaying == false) {
      UpdateFromSourceCollider();
    }

    DrawGizmos(false);
  }


  void OnDrawGizmosSelected() {
    if (Application.isPlaying == false) {
      UpdateFromSourceCollider();
    }

    DrawGizmos(true);
  }

  void DrawGizmos(Boolean selected) {
    if (!QuantumGameGizmos.ShouldDraw(QuantumEditorSettings.Instance.DrawColliderGizmos, selected, false)) {
      return;
    }

    GetEdgeGizmosSettings(transform, PositionOffset, RotationOffset, VertexA, VertexB, Height, out var start, out var end, out var height);
    GizmoUtils.DrawGizmosEdge(start, end, height, selected, QuantumEditorSettings.Instance.StaticColliderColor, style: QuantumEditorSettings.Instance.StaticColliderGizmoStyle);
  }

  public static void GetEdgeGizmosSettings(Transform t, FPVector2 posOffset, FP rotOffset, FPVector2 localStart, FPVector2 localEnd, FP localHeight, out Vector3 start, out Vector3 end, out float height) {
    var trs = Matrix4x4.TRS(t.TransformPoint(posOffset.ToUnityVector3()), t.rotation * rotOffset.FlipRotation().ToUnityQuaternionDegrees(), t.localScale);

    start = trs.MultiplyPoint(localStart.ToUnityVector3());
    end   = trs.MultiplyPoint(localEnd.ToUnityVector3());
    
#if QUANTUM_XY
    height = localHeight.AsFloat * t.localScale.z;
#else
    height = localHeight.AsFloat * t.localScale.y;
#endif
  }
#endif
}
