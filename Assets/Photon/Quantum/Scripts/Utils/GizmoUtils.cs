using UnityEngine;
using Photon.Deterministic;
using System;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Quantum {
  public static class GizmoUtils {
    public static Color Alpha(this Color color, Single a) { 
      color.a = a; return color; 
    }

    public static Color Brightness(this Color color, float brightness) {
      Color.RGBToHSV(color, out var h, out var s, out var v);
      return Color.HSVToRGB(h, s, v * brightness).Alpha(color.a);
    }

    public const float DefaultArrowHeadLength = 0.25f;
    public const float DefaultArrowHeadAngle = 25.0f;

    public static void DrawGizmosBox(Transform transform, Vector3 size, Color color, Vector3 offset = default, bool selected = false, QuantumGizmoStyle style = default) {
#if UNITY_EDITOR
      var matrix = transform.localToWorldMatrix * Matrix4x4.Translate(offset);
      DrawGizmosBox(matrix, size, color, selected: selected, style: style);
#endif
    }

    public static void DrawGizmosBox(Vector3 center, Vector3 size, Color color, bool selected = false, Quaternion? rotation = null, QuantumGizmoStyle style = default) {
#if UNITY_EDITOR
      var matrix = Matrix4x4.TRS(center, rotation ?? Quaternion.identity, Vector3.one);
      DrawGizmosBox(matrix, size, color, selected: selected, style: style);
#endif
    }
   
    public static void DrawGizmosBox(Matrix4x4 matrix, Vector3 size, Color color, bool selected = false, QuantumGizmoStyle style = default) {
#if UNITY_EDITOR
      Gizmos.matrix = matrix;

      if (selected) {
        color = color.Brightness(QuantumEditorSettings.Instance.GizmoSelectedBrightness);
      }

      if (style.IsFillEnabled) {
        Gizmos.color = color;
        Gizmos.DrawCube(Vector3.zero, size);
      }

      if (style.IsWireframeEnabled) {
        Gizmos.color = color;
        Gizmos.DrawWireCube(Vector3.zero, size);
      }

      Gizmos.matrix = Matrix4x4.identity;
      Gizmos.color = Color.white;
#endif
    }

    public static void DrawGizmosCircle(Vector3 position, Single radius, Color color, Single height = 0.0f, Boolean selected = false, QuantumGizmoStyle style = default) {
#if UNITY_EDITOR
      var s = Vector3.one;
      Vector3 up;
      Quaternion rot;

#if QUANTUM_XY
      rot = Quaternion.Euler(0, 0, 0);
      s = new Vector3(radius + radius, radius + radius, 1.0f);
      up = Vector3.forward;
#else
      rot = Quaternion.Euler(-90, 0, 0);
      s = new Vector3(radius + radius, radius + radius, 1.0f);
      up = Vector3.up;
#endif

      var mesh = height != 0.0f ? DebugMesh.CylinderMesh : DebugMesh.CircleMesh;
      if (height != 0.0f) {
        s.z = height;
      }

      if (selected) {
        color = color.Brightness(QuantumEditorSettings.Instance.GizmoSelectedBrightness);
      }

      Gizmos.color = color;
      Handles.color = Gizmos.color;

      if (style.IsWireframeEnabled) {
        if (!style.IsFillEnabled) {
          // draw mesh as invisible; this still lets selection to work
          Gizmos.color = default;
          Gizmos.DrawMesh(mesh, 0, position, rot, s);
        }
        Handles.DrawWireDisc(position, up, radius);
      }

      if (style.IsFillEnabled) {
        Gizmos.DrawMesh(mesh, 0, position, rot, s);
      }

      Handles.color = Gizmos.color = Color.white;
#endif
    }

    public static void DrawGizmosSphere(Vector3 position, Single radius, Color color, Boolean selected = false, QuantumGizmoStyle style = default) {
#if UNITY_EDITOR
      if (selected) {
        color = color.Brightness(QuantumEditorSettings.Instance.GizmoSelectedBrightness);
      }

      Gizmos.color = color;
      if (style.IsFillEnabled) {
        Gizmos.DrawSphere(position, radius);
      } else {
        if (style.IsWireframeEnabled) {
          Gizmos.DrawWireSphere(position, radius);
        }
      }
      Gizmos.color = Color.white;
#endif
    }

    public static void DrawGizmosTriangle(Vector3 A, Vector3 B, Vector3 C, bool selected, Color color) {
#if UNITY_EDITOR
      if (selected) {
        color = color.Brightness(QuantumEditorSettings.Instance.GizmoSelectedBrightness);
      }

      Gizmos.color = color;
      Gizmos.DrawLine(A, B);
      Gizmos.DrawLine(B, C);
      Gizmos.DrawLine(C, A);
      Gizmos.color = Color.white;
#endif
    }

    public static void DrawGizmoGrid(FPVector2 bottomLeft, Int32 width, Int32 height, Int32 nodeSize, Color color) {
#if UNITY_EDITOR
      DrawGizmoGrid(bottomLeft.ToUnityVector3(), width, height, nodeSize, nodeSize, color);
#endif
    }

    public static void DrawGizmoGrid(Vector3 bottomLeft, Int32 width, Int32 height, Int32 nodeSize, Color color) {
#if UNITY_EDITOR
      DrawGizmoGrid(bottomLeft, width, height, nodeSize, nodeSize, color);
#endif
    }

    public static void DrawGizmoGrid(Vector3 bottomLeft, Int32 width, Int32 height, float nodeWidth, float nodeHeight, Color color) {
#if UNITY_EDITOR
        Gizmos.color = color;

#if QUANTUM_XY
        for (Int32 z = 0; z <= height; ++z) {
            Gizmos.DrawLine(bottomLeft + new Vector3(0.0f, nodeHeight * z, 0.0f), bottomLeft + new Vector3(width * nodeWidth, nodeHeight * z, 0.0f));
        }

        for (Int32 x = 0; x <= width; ++x) {
            Gizmos.DrawLine(bottomLeft + new Vector3(nodeWidth * x, 0.0f, 0.0f), bottomLeft + new Vector3(nodeWidth * x, height * nodeHeight, 0.0f));
        }
#else
        for (Int32 z = 0; z <= height; ++z) {
            Gizmos.DrawLine(bottomLeft + new Vector3(0.0f, 0.0f, nodeHeight * z), bottomLeft + new Vector3(width * nodeWidth, 0.0f, nodeHeight * z));
        }

        for (Int32 x = 0; x <= width; ++x) {
            Gizmos.DrawLine(bottomLeft + new Vector3(nodeWidth * x, 0.0f, 0.0f), bottomLeft + new Vector3(nodeWidth * x, 0.0f, height * nodeHeight));
        }
#endif
        
        Gizmos.color = Color.white;
#endif
    }

    public static void DrawGizmoPolygon2D(Vector3 position, Quaternion rotation, FPVector2[] vertices, Single height, Color color, QuantumGizmoStyle style = default) {
#if UNITY_EDITOR
      var matrix = Matrix4x4.TRS(position, rotation, Vector3.one);
      DrawGizmoPolygon2D(matrix, vertices, height, false, false, color, style: style);
#endif
    }

    public static void DrawGizmoPolygon2D(Vector3 position, Quaternion rotation, FPVector2[] vertices, Single height, bool drawNormals, bool selected, Color color, QuantumGizmoStyle style = default) {
#if UNITY_EDITOR
      var matrix = Matrix4x4.TRS(position, rotation, Vector3.one);
      DrawGizmoPolygon2D(matrix, vertices, height, drawNormals, selected, color, style: style);
#endif
    }

    public static void DrawGizmoPolygon2D(Transform transform, FPVector2[] vertices, Single height, bool drawNormals, bool selected, Color color, QuantumGizmoStyle style = default) {
#if UNITY_EDITOR
      var matrix = transform.localToWorldMatrix;
      DrawGizmoPolygon2D(matrix, vertices, height, drawNormals, selected, color, style: style);
#endif
    }

    public static void DrawGizmoPolygon2D(Matrix4x4 matrix, FPVector2[] vertices, Single height, bool drawNormals, bool selected, Color color, QuantumGizmoStyle style = default) {
#if UNITY_EDITOR
      if (vertices.Length < 3) return;

      FPMathUtils.LoadLookupTables();

      color = FPVector2.IsPolygonConvex(vertices) && FPVector2.PolygonNormalsAreValid(vertices) ? color : Color.red;

      var transformedVertices = vertices.Select(x => matrix.MultiplyPoint(x.ToUnityVector3())).ToArray();
      DrawGizmoPolygon2DInternal(transformedVertices, height, drawNormals, selected, color, style: style);
#endif
    }

    private static void DrawGizmoPolygon2DInternal(Vector3[] vertices, Single height, Boolean drawNormals, Boolean selected, Color color, QuantumGizmoStyle style = default) {
#if UNITY_EDITOR

#if QUANTUM_XY
      var upVector = Vector3.forward;
#else
      var upVector = Vector3.up;
#endif

      if (selected) {
        color = color.Brightness(QuantumEditorSettings.Instance.GizmoSelectedBrightness);
      }

      Gizmos.color = color;
      Handles.color = color;

      if (style.IsFillEnabled) {
        Handles.DrawAAConvexPolygon(vertices);

        if (height != 0.0f) {
          Handles.matrix = Matrix4x4.Translate(upVector * height);
          Handles.DrawAAConvexPolygon(vertices);
          Handles.matrix = Matrix4x4.identity;
        }
      }

      if (style.IsWireframeEnabled) {
        for (Int32 i = 0; i < vertices.Length; ++i) {
          var v1 = vertices[i];
          var v2 = vertices[(i + 1) % vertices.Length];

          Gizmos.DrawLine(v1, v2);

          if (height != 0.0f) {
            Gizmos.DrawLine(v1 + upVector * height, v2 + upVector * height);
            Gizmos.DrawLine(v1, v1 + upVector * height);
          }

          if (drawNormals) {
#if QUANTUM_XY
          var normal = Vector3.Cross(v2 - v1, upVector).normalized;
#else
            var normal = Vector3.Cross(v1 - v2, upVector).normalized;
#endif

            var center = Vector3.Lerp(v1, v2, 0.5f);
            DrawGizmoVector(center, center + (normal * 0.25f));
          }
        }
      }

      Gizmos.color = UnityEditor.Handles.color = Color.white;
#endif
    }

    public static void DrawGizmoDiamond(Vector3 center, Vector2 size) {
#if UNITY_EDITOR
      var DiamondWidth = size.x * 0.5f;
      var DiamondHeight = size.y * 0.5f;

#if QUANTUM_XY
      Gizmos.DrawLine(center + Vector3.right * DiamondWidth, center + Vector3.up * DiamondHeight);
      Gizmos.DrawLine(center + Vector3.left * DiamondWidth, center + Vector3.up * DiamondHeight);
      Gizmos.DrawLine(center + Vector3.right * DiamondWidth, center + Vector3.down * DiamondHeight);
      Gizmos.DrawLine(center + Vector3.left * DiamondWidth, center + Vector3.down * DiamondHeight);
#else 
      Gizmos.DrawLine(center + Vector3.right * DiamondWidth, center + Vector3.forward * DiamondHeight);
      Gizmos.DrawLine(center + Vector3.left * DiamondWidth, center + Vector3.forward * DiamondHeight);
      Gizmos.DrawLine(center + Vector3.right * DiamondWidth, center + Vector3.back * DiamondHeight);
      Gizmos.DrawLine(center + Vector3.left * DiamondWidth, center + Vector3.back * DiamondHeight);
#endif
#endif
    }

    public static void DrawGizmoVector3D(Vector3 start, Vector3 end, float arrowHeadLength = 0.25f, float arrowHeadAngle = 25.0f) {
#if UNITY_EDITOR
      Gizmos.DrawLine(start, end);
      var d = (end - start).normalized;
      Vector3 right = Quaternion.LookRotation(d) * Quaternion.Euler(0f, 180f + arrowHeadAngle, 0f) * new Vector3(0f, 0f, 1f);
      Vector3 left = Quaternion.LookRotation(d) * Quaternion.Euler(0f, 180f - arrowHeadAngle, 0f) * new Vector3(0f, 0f, 1f);
      Gizmos.DrawLine(end, end + right * arrowHeadLength);
      Gizmos.DrawLine(end, end + left * arrowHeadLength);
#endif
    }

    public static void DrawGizmoVector(Vector3 start, Vector3 end, float arrowHeadLength = DefaultArrowHeadLength, float arrowHeadAngle = DefaultArrowHeadAngle) {
#if UNITY_EDITOR
      Gizmos.DrawLine(start, end);

      var l = (start - end).magnitude;

      if (l < arrowHeadLength * 2) {
        arrowHeadLength = l / 2;
      }

      var d = (start - end).normalized;

      float cos = Mathf.Cos(arrowHeadAngle * Mathf.Deg2Rad);
      float sin = Mathf.Sin(arrowHeadAngle * Mathf.Deg2Rad);

      Vector3 left = Vector3.zero;
#if QUANTUM_XY
      left.x = d.x * cos - d.y * sin;
      left.y = d.x * sin + d.y * cos;
#else
      left.x = d.x * cos - d.z * sin;
      left.z = d.x * sin + d.z * cos;
#endif

      sin = -sin;

      Vector3 right = Vector3.zero;
#if QUANTUM_XY
      right.x = d.x * cos - d.y * sin;
      right.y = d.x * sin + d.y * cos;
#else
      right.x = d.x * cos - d.z * sin;
      right.z = d.x * sin + d.z * cos;
#endif

      Gizmos.DrawLine(end, end + left * arrowHeadLength);
      Gizmos.DrawLine(end, end + right * arrowHeadLength);
#endif
    }

    public static void DrawGizmoArc(Vector3 position, Vector3 normal, Vector3 from, float angle, float radius, Color color, float alphaRatio = 1.0f, QuantumGizmoStyle style = default) {
#if UNITY_EDITOR
      Handles.color = color;
      Gizmos.color  = color;

      if (style.IsWireframeEnabled) {
        Handles.DrawWireArc(position, normal, from, angle, radius);
        if (!style.IsFillEnabled) {
          var to = Quaternion.AngleAxis(angle, normal) * from;
          Gizmos.color = color.Alpha(color.a * alphaRatio);
          Gizmos.DrawRay(position, from * radius);
          Gizmos.DrawRay(position, to * radius);
        }
      }

      if (style.IsFillEnabled) {
        Handles.color = color.Alpha(color.a * alphaRatio);
        Handles.DrawSolidArc(position, normal, from, angle, radius);
      }

      Gizmos.color = UnityEditor.Handles.color = Color.white;
#endif
    }

    public static void DrawGizmoDisc(Vector3 position, Vector3 normal, float radius, Color color, float alphaRatio = 1.0f, QuantumGizmoStyle style = default) {
#if UNITY_EDITOR
      Handles.color = color;
      Gizmos.color  = color;

      if (style.IsWireframeEnabled) {
        Handles.DrawWireDisc(position, normal, radius);
      }

      if (style.IsFillEnabled) {
        Handles.color = Handles.color.Alpha(Handles.color.a * alphaRatio);
        Handles.DrawSolidDisc(position, normal, radius);
      }

      Gizmos.color = UnityEditor.Handles.color = Color.white;
#endif
    }

    public static void DrawGizmosEdge(Vector3 start, Vector3 end, float height, bool selected, Color color, QuantumGizmoStyle style = default) {
#if UNITY_EDITOR
      Gizmos.color = color;

      if (Math.Abs(height) > float.Epsilon) {
        var startToEnd = end - start;
        var edgeSize   = startToEnd.magnitude;
        var size       = new Vector3(edgeSize, 0);
        var center     = start + startToEnd / 2;
#if QUANTUM_XY
        size.z    = -height;
        center.z -= height / 2;
#else
        size.y    = height;
        center.y += height / 2;
#endif
        DrawGizmosBox(center, size, color, selected: selected, rotation: Quaternion.FromToRotation(Vector3.right, startToEnd), style: style);
      } else {
        Gizmos.DrawLine(start, end);
      }

      Gizmos.color = Color.white;
#endif
    }

    public static void DrawGizmosJoint2D(Quantum.Prototypes.Unity.Joint2D_Prototype prototype, Transform jointTransform, Transform connectedTransform, bool selected, QuantumEditorSettings editorSettings, QuantumGizmoStyle style = default) {
#if UNITY_EDITOR
      if (prototype.JointType == Physics2D.JointType.None) {
        return;
      }

      GizmosJointParams param;

      switch (prototype.JointType) {
        case Physics2D.JointType.DistanceJoint:
          param.Type = GizmosJointParams.GizmosJointType.DistanceJoint2D;
          param.MinDistance = prototype.MinDistance.AsFloat;
          break;

        case Physics2D.JointType.SpringJoint:
          param.Type = GizmosJointParams.GizmosJointType.SpringJoint2D;
          param.MinDistance = prototype.Distance.AsFloat;
          break;

        case Physics2D.JointType.HingeJoint:
          param.Type = GizmosJointParams.GizmosJointType.HingeJoint2D;
          param.MinDistance = prototype.Distance.AsFloat;
          break;

        default:
          throw new ArgumentOutOfRangeException();
      }
      
      param.Selected       = selected;
      param.JointRot       = jointTransform.rotation;
      param.RelRotRef      = Quaternion.Inverse(param.JointRot);
      param.AnchorPos      = jointTransform.position + param.JointRot * prototype.Anchor.ToUnityVector3();
      param.MaxDistance    = prototype.MaxDistance.AsFloat;
      param.UseAngleLimits = prototype.UseAngleLimits;
      param.LowerAngle     = prototype.LowerAngle.AsFloat;
      param.UpperAngle     = prototype.UpperAngle.AsFloat;

      if (connectedTransform == null) {
        param.ConnectedRot = Quaternion.identity;
        param.ConnectedPos = prototype.ConnectedAnchor.ToUnityVector3();
      } else {
        param.ConnectedRot = connectedTransform.rotation;
        param.ConnectedPos = connectedTransform.position + param.ConnectedRot * prototype.ConnectedAnchor.ToUnityVector3();
        param.RelRotRef    = param.ConnectedRot * param.RelRotRef;
      }

#if QUANTUM_XY
      param.Axis = Vector3.back;
#else
      param.Axis = Vector3.up;
#endif
      
      DrawGizmosJointInternal(ref param, editorSettings, style);
#endif
    }

    public static unsafe void DrawGizmosJoint2D(Physics2D.Joint* joint, Transform2D* jointTransform, Transform2D* connectedTransform, bool selected, QuantumEditorSettings editorSettings, QuantumGizmoStyle style = default) {
#if UNITY_EDITOR
      if (joint->Type == Physics2D.JointType.None) {
        return;
      }

      var param = default(GizmosJointParams);
      param.Selected  = selected;
      param.JointRot  = jointTransform->Rotation.ToUnityQuaternion();
      param.AnchorPos = jointTransform->TransformPoint(joint->Anchor).ToUnityVector3();

      switch (joint->Type) {
        case Physics2D.JointType.DistanceJoint:
          param.Type = GizmosJointParams.GizmosJointType.DistanceJoint2D;
          param.MinDistance = joint->DistanceJoint.MinDistance.AsFloat;
          param.MaxDistance = joint->DistanceJoint.MaxDistance.AsFloat;
          break;

        case Physics2D.JointType.SpringJoint:
          param.Type = GizmosJointParams.GizmosJointType.SpringJoint2D;
          param.MinDistance = joint->SpringJoint.Distance.AsFloat;
          break;

        case Physics2D.JointType.HingeJoint:
          param.Type = GizmosJointParams.GizmosJointType.HingeJoint2D;
          param.RelRotRef      = Quaternion.Inverse(param.JointRot);
          param.UseAngleLimits = joint->HingeJoint.UseAngleLimits;
          param.LowerAngle     = (joint->HingeJoint.LowerLimitRad * FP.Rad2Deg).AsFloat;
          param.UpperAngle     = (joint->HingeJoint.UpperLimitRad * FP.Rad2Deg).AsFloat;
          break;
      }

      if (connectedTransform == null) {
        param.ConnectedRot = Quaternion.identity;
        param.ConnectedPos = joint->ConnectedAnchor.ToUnityVector3();
      } else {
        param.ConnectedRot = connectedTransform->Rotation.ToUnityQuaternion();
        param.ConnectedPos = connectedTransform->TransformPoint(joint->ConnectedAnchor).ToUnityVector3();
        param.RelRotRef    = (param.ConnectedRot * param.RelRotRef).normalized;
      }

#if QUANTUM_XY
      param.Axis = Vector3.back;
#else
      param.Axis = Vector3.up;
#endif

      DrawGizmosJointInternal(ref param, editorSettings, style);
#endif
    }

    public static void DrawGizmosJoint3D(Quantum.Prototypes.Unity.Joint3D_Prototype prototype, Transform jointTransform, Transform connectedTransform, bool selected, QuantumEditorSettings editorSettings, QuantumGizmoStyle style = default) {
#if UNITY_EDITOR
      if (prototype.JointType == Physics3D.JointType3D.None) {
        return;
      }

      GizmosJointParams param;

      switch (prototype.JointType) {
        case Physics3D.JointType3D.DistanceJoint:
          param.Type = GizmosJointParams.GizmosJointType.DistanceJoint3D;
          param.MinDistance = prototype.MinDistance.AsFloat;
          break;

        case Physics3D.JointType3D.SpringJoint:
          param.Type = GizmosJointParams.GizmosJointType.SpringJoint3D;
          param.MinDistance = prototype.Distance.AsFloat;
          break;

        case Physics3D.JointType3D.HingeJoint:
          param.Type = GizmosJointParams.GizmosJointType.HingeJoint3D;
          param.MinDistance = prototype.Distance.AsFloat;
          break;

        default:
          throw new ArgumentOutOfRangeException();
      }
      
      param.Selected       = selected;
      param.JointRot       = jointTransform.rotation;
      param.RelRotRef      = Quaternion.Inverse(param.JointRot);
      param.AnchorPos      = jointTransform.position + param.JointRot * prototype.Anchor.ToUnityVector3();
      param.MaxDistance    = prototype.MaxDistance.AsFloat;
      param.Axis           = prototype.Axis.ToUnityVector3();
      param.UseAngleLimits = prototype.UseAngleLimits;
      param.LowerAngle     = prototype.LowerAngle.AsFloat;
      param.UpperAngle     = prototype.UpperAngle.AsFloat;

      if (connectedTransform == null) {
        param.ConnectedRot = Quaternion.identity;
        param.ConnectedPos = prototype.ConnectedAnchor.ToUnityVector3();
      } else {
        param.ConnectedRot = connectedTransform.rotation;
        param.ConnectedPos = connectedTransform.position + param.ConnectedRot * prototype.ConnectedAnchor.ToUnityVector3();
        param.RelRotRef    = param.ConnectedRot * param.RelRotRef;
      }

      DrawGizmosJointInternal(ref param, editorSettings, style);
#endif
    }

    public static unsafe void DrawGizmosJoint3D(Physics3D.Joint3D* joint, Transform3D* jointTransform, Transform3D* connectedTransform, bool selected, QuantumEditorSettings editorSettings, QuantumGizmoStyle style = default) {
#if UNITY_EDITOR
      if (joint->Type == Physics3D.JointType3D.None) {
        return;
      }

      var param = default(GizmosJointParams);
      param.Selected  = selected;
      param.JointRot  = jointTransform->Rotation.ToUnityQuaternion();
      param.AnchorPos = jointTransform->TransformPoint(joint->Anchor).ToUnityVector3();

      switch (joint->Type) {
        case Physics3D.JointType3D.DistanceJoint:
          param.Type = GizmosJointParams.GizmosJointType.DistanceJoint3D;
          param.MinDistance = joint->DistanceJoint.MinDistance.AsFloat;
          param.MaxDistance = joint->DistanceJoint.MaxDistance.AsFloat;
          break;

        case Physics3D.JointType3D.SpringJoint:
          param.Type = GizmosJointParams.GizmosJointType.SpringJoint3D;
          param.MinDistance = joint->SpringJoint.Distance.AsFloat;
          break;

        case Physics3D.JointType3D.HingeJoint:
          param.Type = GizmosJointParams.GizmosJointType.HingeJoint3D;
          param.RelRotRef      = joint->HingeJoint.RelativeRotationReference.ToUnityQuaternion();
          param.Axis           = joint->HingeJoint.Axis.ToUnityVector3();
          param.UseAngleLimits = joint->HingeJoint.UseAngleLimits;
          param.LowerAngle     = (joint->HingeJoint.LowerLimitRad * FP.Rad2Deg).AsFloat;
          param.UpperAngle     = (joint->HingeJoint.UpperLimitRad * FP.Rad2Deg).AsFloat;
          break;
      }

      if (connectedTransform == null) {
        param.ConnectedRot = Quaternion.identity;
        param.ConnectedPos = joint->ConnectedAnchor.ToUnityVector3();
      } else {
        param.ConnectedRot = connectedTransform->Rotation.ToUnityQuaternion();
        param.ConnectedPos = connectedTransform->TransformPoint(joint->ConnectedAnchor).ToUnityVector3();
      }

      DrawGizmosJointInternal(ref param, editorSettings, style);
#endif
    }

    private struct GizmosJointParams {
      public enum GizmosJointType {
        None = 0,

        DistanceJoint2D = 1,
        DistanceJoint3D = 2,

        SpringJoint2D = 3,
        SpringJoint3D = 4,

        HingeJoint2D = 5,
        HingeJoint3D = 6,
      }
      
      public GizmosJointType Type;
      public bool Selected;

      public Vector3 AnchorPos;
      public Vector3 ConnectedPos;

      public Quaternion JointRot;
      public Quaternion ConnectedRot;
      public Quaternion RelRotRef;

      public float MinDistance;
      public float MaxDistance;

      public Vector3 Axis;

      public bool  UseAngleLimits;
      public float LowerAngle;
      public float UpperAngle;
    }

    private static void DrawGizmosJointInternal(ref GizmosJointParams p, QuantumEditorSettings editorSettings, QuantumGizmoStyle style = default) {
#if UNITY_EDITOR
      const float anchorRadiusFactor           = 0.1f;
      const float barHalfLengthFactor          = 0.1f;
      const float hingeRefAngleBarLengthFactor = 0.5f;

      // how much weaker the alpha of the color of hinge disc is relative to the its rim's alpha
      const float solidDiscAlphaRatio = 0.25f;

      if (p.Type == GizmosJointParams.GizmosJointType.None) {
        return;
      }

      if (editorSettings == null) {
        editorSettings = QuantumEditorSettings.Instance;
      }

      var gizmosScale    = editorSettings.GizmoIconScale.AsFloat;

      var primColor    = editorSettings.JointGizmosPrimaryColor;
      var secColor     = editorSettings.JointGizmosSecondaryColor;
      var warningColor = editorSettings.JointGizmosWarningColor;

      if (p.Selected) {
        primColor    = primColor.Brightness(QuantumEditorSettings.Instance.GizmoSelectedBrightness);
        secColor     = secColor.Brightness(QuantumEditorSettings.Instance.GizmoSelectedBrightness);
        warningColor = warningColor.Brightness(QuantumEditorSettings.Instance.GizmoSelectedBrightness);
      }

      DrawGizmosSphere(p.AnchorPos, gizmosScale    * anchorRadiusFactor, secColor, style: style);
      DrawGizmosSphere(p.ConnectedPos, gizmosScale * anchorRadiusFactor, secColor, style: style);

      Gizmos.color = secColor;
      Gizmos.DrawLine(p.AnchorPos, p.ConnectedPos);

      switch (p.Type) {
        case GizmosJointParams.GizmosJointType.DistanceJoint2D:
        case GizmosJointParams.GizmosJointType.DistanceJoint3D: {
          var connectedToAnchorDir = Vector3.Normalize(p.AnchorPos - p.ConnectedPos);
          var minDistanceMark      = p.ConnectedPos + connectedToAnchorDir * p.MinDistance;
          var maxDistanceMark      = p.ConnectedPos + connectedToAnchorDir * p.MaxDistance;

          Gizmos.color = Handles.color = primColor;

          Gizmos.DrawLine(minDistanceMark, maxDistanceMark);
          DrawGizmoDisc(minDistanceMark, connectedToAnchorDir, barHalfLengthFactor, primColor, style: style);
          DrawGizmoDisc(maxDistanceMark, connectedToAnchorDir, barHalfLengthFactor, primColor, style: style);

          Gizmos.color = Handles.color = Color.white;

          break;
        }

        case GizmosJointParams.GizmosJointType.SpringJoint2D:
        case GizmosJointParams.GizmosJointType.SpringJoint3D: {
          var connectedToAnchorDir = Vector3.Normalize(p.AnchorPos - p.ConnectedPos);
          var distanceMark         = p.ConnectedPos + connectedToAnchorDir * p.MinDistance;

          Gizmos.color = Handles.color = primColor;

          Gizmos.DrawLine(p.ConnectedPos, distanceMark);
          DrawGizmoDisc(distanceMark, connectedToAnchorDir, barHalfLengthFactor, primColor, style: style);

          Gizmos.color = Handles.color = Color.white;

          break;
        }

        case GizmosJointParams.GizmosJointType.HingeJoint2D: {
          var hingeRefAngleBarLength = hingeRefAngleBarLengthFactor * editorSettings.GizmoIconScale.AsFloat;
          var connectedAnchorRight =  p.ConnectedRot * Vector3.right;
          var anchorRight =  p.JointRot * Vector3.right;
          
          Gizmos.color  = secColor;
          Gizmos.DrawRay(p.AnchorPos, anchorRight * hingeRefAngleBarLength);

          Gizmos.color  = primColor;
          Gizmos.DrawRay(p.ConnectedPos, connectedAnchorRight * hingeRefAngleBarLength);

#if QUANTUM_XY
          var planeNormal = -Vector3.forward;
#else
          var planeNormal = Vector3.up;
#endif

          if (p.UseAngleLimits) {
            var fromDir    = Quaternion.AngleAxis(p.LowerAngle, planeNormal) * connectedAnchorRight;
            var angleRange = p.UpperAngle - p.LowerAngle;
            var arcColor = angleRange < 0.0f ? warningColor : primColor;
            DrawGizmoArc(p.ConnectedPos, planeNormal, fromDir, angleRange, hingeRefAngleBarLength, arcColor, solidDiscAlphaRatio, style: style);
          } else {
            // Draw full disc
            DrawGizmoDisc(p.ConnectedPos, planeNormal, hingeRefAngleBarLength, primColor, solidDiscAlphaRatio, style: style);
          }

          Gizmos.color = Handles.color = Color.white;

          break;
        }

        case GizmosJointParams.GizmosJointType.HingeJoint3D: {
          var hingeRefAngleBarLength = hingeRefAngleBarLengthFactor * editorSettings.GizmoIconScale.AsFloat;

          var hingeAxisLocal = p.Axis.sqrMagnitude > float.Epsilon ? p.Axis.normalized : Vector3.right;
          var hingeAxisWorld = p.JointRot * hingeAxisLocal;
          var hingeOrtho     = Vector3.Cross(hingeAxisWorld, p.JointRot * Vector3.up);

          hingeOrtho = hingeOrtho.sqrMagnitude > float.Epsilon ? hingeOrtho.normalized : Vector3.Cross(hingeAxisWorld, p.JointRot * Vector3.forward).normalized;
          
          Gizmos.color = Handles.color = primColor;

          Gizmos.DrawRay(p.AnchorPos, hingeOrtho * hingeRefAngleBarLength);
          Handles.ArrowHandleCap(0, p.ConnectedPos, Quaternion.FromToRotation(Vector3.forward, hingeAxisWorld), hingeRefAngleBarLengthFactor * 1.5f, EventType.Repaint);

          if (p.UseAngleLimits) {
            var refAngle   = ComputeRelativeAngleHingeJoint(hingeAxisWorld, p.JointRot, p.ConnectedRot, p.RelRotRef);
            var refOrtho   = Quaternion.AngleAxis(refAngle, hingeAxisWorld) * hingeOrtho;
            var fromDir    = Quaternion.AngleAxis(-p.LowerAngle, hingeAxisWorld) * refOrtho;
            var angleRange = p.UpperAngle - p.LowerAngle;
            var arcColor = angleRange < 0.0f ? warningColor : primColor;
            DrawGizmoArc(p.ConnectedPos, hingeAxisWorld, fromDir, -angleRange, hingeRefAngleBarLength, arcColor, solidDiscAlphaRatio, style: style);
          } else {
            // Draw full disc
            DrawGizmoDisc(p.ConnectedPos, hingeAxisWorld, hingeRefAngleBarLength, primColor, solidDiscAlphaRatio, style: style);
          }

          Gizmos.color = Handles.color = Color.white;

          break;
        }
      }
#endif
    }

    private static float ComputeRelativeAngleHingeJoint(Vector3 hingeAxis, Quaternion rotJoint, Quaternion rotConnectedAnchor, Quaternion relRotRef) {
      var rotDiff = rotConnectedAnchor * Quaternion.Inverse(rotJoint);
      var relRot  = rotDiff            * Quaternion.Inverse(relRotRef);

      var rotVector     = new Vector3(relRot.x, relRot.y, relRot.z);
      var sinHalfRadAbs = rotVector.magnitude;
      var cosHalfRad    = relRot.w;

      var hingeAngleRad = 2 * Mathf.Atan2(sinHalfRadAbs, Mathf.Sign(Vector3.Dot(rotVector, hingeAxis)) * cosHalfRad);

      // clamp to range [-Pi, Pi]
      if (hingeAngleRad < -Mathf.PI) {
        hingeAngleRad += 2 * Mathf.PI;
      }

      if (hingeAngleRad > Mathf.PI) {
        hingeAngleRad -= 2 * Mathf.PI;
      }

      return hingeAngleRad * Mathf.Rad2Deg;
    }
  }

  [Serializable]
  public struct QuantumGizmoStyle {

    public static QuantumGizmoStyle FillDisabled => new QuantumGizmoStyle() {
      DisableFill = true
    };

    public bool DisableFill;

    public bool IsFillEnabled => !DisableFill;
    public bool IsWireframeEnabled => true;
  }
}