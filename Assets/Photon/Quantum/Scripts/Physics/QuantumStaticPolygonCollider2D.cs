using Photon.Deterministic;
using UnityEngine;
using System;
using Quantum;
using Quantum.Inspector;

public class QuantumStaticPolygonCollider2D : MonoBehaviour {
#if (!UNITY_2019_1_OR_NEWER || QUANTUM_ENABLE_PHYSICS2D) && !QUANTUM_DISABLE_PHYSICS2D
  public PolygonCollider2D SourceCollider;

  public bool BakeAsStaticEdges2D = false;

  [DrawIf("SourceCollider", 0)]
  public FPVector2[] Vertices = new FPVector2[3] {
    new FPVector2(0, 2),
    new FPVector2(-1, 0),
    new FPVector2(+1, 0)
  };

  [DrawIf("SourceCollider", 0)]
  [UnityEngine.Tooltip("Additional translation applied to transform position when baking")]
  public FPVector2 PositionOffset;

  [UnityEngine.Tooltip("Additional rotation (in degrees) applied to transform rotation when baking")]
  public FP RotationOffset;

  public FP Height;
  public QuantumStaticColliderSettings Settings = new QuantumStaticColliderSettings();

  protected virtual bool UpdateVerticesFromSourceOnBake => true;

  public void UpdateFromSourceCollider(bool updateVertices = true) {
    if (SourceCollider == null) {
      return;
    }

    Settings.Trigger = SourceCollider.isTrigger;
    PositionOffset   = SourceCollider.offset.ToFPVector2();

    if (updateVertices == false) {
      return;
    }

    Vertices = new FPVector2[SourceCollider.points.Length];

    for (var i = 0; i < SourceCollider.points.Length; i++) {
      Vertices[i] = SourceCollider.points[i].ToFPVector2();
    }
  }

  public virtual void BeforeBake() {
    UpdateFromSourceCollider(UpdateVerticesFromSourceOnBake);
  }

  void OnDrawGizmos() {
    if (Application.isPlaying == false) {
      UpdateFromSourceCollider(updateVertices: false);
    }

    DrawGizmo(false);
  }


  void OnDrawGizmosSelected() {
    if (Application.isPlaying == false) {
      UpdateFromSourceCollider(updateVertices: false);
    }

    DrawGizmo(true);
  }

  void DrawGizmo(Boolean selected) {
    if (!QuantumGameGizmos.ShouldDraw(QuantumEditorSettings.Instance.DrawColliderGizmos, selected, false)) {
      return;
    }

    if (BakeAsStaticEdges2D) {
      for (var i = 0; i < Vertices.Length; i++) {
        QuantumStaticEdgeCollider2D.GetEdgeGizmosSettings(transform, PositionOffset, RotationOffset, Vertices[i], Vertices[(i + 1) % Vertices.Length], Height, out var start, out var end, out var edgeHeight);
        GizmoUtils.DrawGizmosEdge(start, end, edgeHeight, selected, QuantumEditorSettings.Instance.StaticColliderColor, style: QuantumEditorSettings.Instance.StaticColliderGizmoStyle);
      }

      return;
    }

    var height = Height.AsFloat * transform.localScale.z;
#if QUANTUM_XY
    height *= -1.0f;
#endif

    var t = transform;
    var matrix = Matrix4x4.TRS(
      t.TransformPoint(PositionOffset.ToUnityVector3()),
      t.rotation * RotationOffset.FlipRotation().ToUnityQuaternionDegrees(),
      t.localScale);
    GizmoUtils.DrawGizmoPolygon2D(matrix, Vertices, height, selected, selected, QuantumEditorSettings.Instance.StaticColliderColor, QuantumEditorSettings.Instance.StaticColliderGizmoStyle);
  }
#endif
}
