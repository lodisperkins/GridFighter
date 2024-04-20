using Photon.Deterministic;
using Quantum;
using System;
using UnityEngine;
using Assert = Quantum.Assert;

public static class QuantumGameGizmos {

  private static Color Desaturate(Color c, float t) {
    return Color.Lerp(new Color(c.grayscale, c.grayscale, c.grayscale), c, t);
  }

  public static bool ShouldDraw(QuantumEditorSettings.GizmosMode mode, bool selected, bool hasStateDrawer = true) {
    if (Application.isPlaying) {
      if (hasStateDrawer) {
        // state drawer will take over
        return false;
      } else if ((mode & QuantumEditorSettings.GizmosMode.OnApplicationPlaying) == default) {
        // needs to be set in order to get to OnDraw/OnSelected
        return false;
      }
    }

    if (selected) {
      return (mode & QuantumEditorSettings.GizmosMode.OnSelected) == QuantumEditorSettings.GizmosMode.OnSelected;
    } else {
      return (mode & QuantumEditorSettings.GizmosMode.OnDraw) == QuantumEditorSettings.GizmosMode.OnDraw;
    }
  }

  public static unsafe void OnDrawGizmos(QuantumGame game, QuantumEditorSettings editorSettings) {
#if UNITY_EDITOR
    if (editorSettings == null) {
      editorSettings = QuantumEditorSettings.Instance;
    }

    var frame = game.Frames.Predicted;
    
    if (frame != null) {

      #region Components 

      if ((editorSettings.DrawColliderGizmos & QuantumEditorSettings.GizmosMode.OnApplicationPlaying) != 0) {

        // ################## Components: PhysicsCollider2D ##################

        foreach (var (handle, collider) in frame.GetComponentIterator<PhysicsCollider2D>()) {
          DrawCollider2DGizmo(frame, handle, &collider, editorSettings.ColliderGizmosStyle);
        }

        // ################## Components: PhysicsCollider3D ##################

        foreach (var (handle, collider) in frame.GetComponentIterator<PhysicsCollider3D>()) {
          DrawCollider3DGizmo(frame, handle, &collider, editorSettings.ColliderGizmosStyle);
        }

        // ################## Components: CharacterController2D ##################

        foreach (var (entity, cc) in frame.GetComponentIterator<CharacterController2D>()) {
          if (frame.Unsafe.TryGetPointer(entity, out Transform2D* t) &&
              frame.TryFindAsset(cc.Config, out CharacterController2DConfig config)) {
            DrawCharacterController2DGizmo(t->Position.ToUnityVector3(), config, false, editorSettings.ColliderGizmosStyle);
          }
        }

        // ################## Components: CharacterController3D ##################

        foreach (var (entity, cc) in frame.GetComponentIterator<CharacterController3D>()) {
          if (frame.Unsafe.TryGetPointer(entity, out Transform3D* t) &&
              frame.TryFindAsset(cc.Config, out CharacterController3DConfig config)) {
            DrawCharacterController3DGizmo(t->Position.ToUnityVector3(), config, false, editorSettings.ColliderGizmosStyle);
          }
        }
      }

      // ################## Components: PhysicsJoints2D ##################

      if ((editorSettings.DrawJointGizmos & QuantumEditorSettings.GizmosMode.OnApplicationPlaying) != 0) {
        foreach(var (handle, jointsComponent) in frame.Unsafe.GetComponentBlockIterator<PhysicsJoints2D>()) {
          if (frame.Unsafe.TryGetPointer(handle, out Transform2D* transform) && jointsComponent->TryGetJoints(frame, out var jointsBuffer, out var jointsCount)) {
            for (var i = 0; i < jointsCount; i++) {
              var curJoint = jointsBuffer + i;
              frame.Unsafe.TryGetPointer(curJoint->ConnectedEntity, out Transform2D* connectedTransform);
              GizmoUtils.DrawGizmosJoint2D(curJoint, transform, connectedTransform, selected: false, editorSettings, editorSettings.JointGizmosStyle);
            }
          }
        }
      }

      // ################## Components: PhysicsJoints3D ##################

      if ((editorSettings.DrawJointGizmos & QuantumEditorSettings.GizmosMode.OnApplicationPlaying) != 0) {
        foreach(var (handle, jointsComponent) in frame.Unsafe.GetComponentBlockIterator<PhysicsJoints3D>()) {
          if (frame.Unsafe.TryGetPointer(handle, out Transform3D* transform) && jointsComponent->TryGetJoints(frame, out var jointsBuffer, out var jointsCount)) {
            for (var i = 0; i < jointsCount; i++) {
              var curJoint = jointsBuffer + i;
              frame.Unsafe.TryGetPointer(curJoint->ConnectedEntity, out Transform3D* connectedTransform);
              GizmoUtils.DrawGizmosJoint3D(curJoint, transform, connectedTransform, selected: false, editorSettings, editorSettings.JointGizmosStyle);
            }
          }
        }
      }


      // ################## Components: NavMeshSteeringAgent ##################
      NavMeshAsset currentNavmeshAsset = null;

      if (editorSettings.DrawNavMeshAgents) {
        foreach(var (entity, navmeshPathfinderAgent) in frame.GetComponentIterator<NavMeshPathfinder>()) {
          var position = Vector3.zero;

          if (frame.Has<Transform2D>(entity)) {
            position = frame.Unsafe.GetPointer<Transform2D>(entity)->Position.ToUnityVector3();
            if (frame.Has<Transform2DVertical>(entity)) {
              position.y = frame.Unsafe.GetPointer<Transform2DVertical>(entity)->Position.AsFloat;
            }
          }
          else if (frame.Has<Transform3D>(entity)) {
            position = frame.Unsafe.GetPointer<Transform3D>(entity)->Position.ToUnityVector3();
          }

          var config = frame.FindAsset<NavMeshAgentConfig>(navmeshPathfinderAgent.ConfigId);

          var agentRadius = 0.25f;
          if (currentNavmeshAsset == null || currentNavmeshAsset.Settings.Identifier.Guid != navmeshPathfinderAgent.NavMeshGuid) {
            // cache the asset, it's likely other agents use the same 
            currentNavmeshAsset = UnityDB.FindAsset<NavMeshAsset>(navmeshPathfinderAgent.NavMeshGuid);
          }

          if (currentNavmeshAsset != null) {
            agentRadius = currentNavmeshAsset.Settings.MinAgentRadius.AsFloat;
          }

          if (frame.Has<NavMeshSteeringAgent>(entity)) {
            var steeringAgent = frame.Get<NavMeshSteeringAgent>(entity);
            Gizmos.color = editorSettings.NavMeshAgentColor;
            GizmoUtils.DrawGizmoVector(
              position, position + steeringAgent.Velocity.XOY.ToUnityVector3().normalized * agentRadius * 3.0f, 
              GizmoUtils.DefaultArrowHeadLength * editorSettings.GizmoIconScale.AsFloat);
          }

          if (config.AvoidanceType != Navigation.AvoidanceType.None && frame.Has<NavMeshAvoidanceAgent>(entity)) {
            GizmoUtils.DrawGizmosCircle(position, config.AvoidanceRadius.AsFloat, editorSettings.NavMeshAvoidanceColor, style: editorSettings.ColliderGizmosStyle);
          }

          GizmoUtils.DrawGizmosCircle(position, agentRadius, 
            navmeshPathfinderAgent.IsActive ?
            editorSettings.NavMeshAgentColor : 
            Desaturate(editorSettings.NavMeshAgentColor, 0.25f),
            style: editorSettings.ColliderGizmosStyle);
        }

        foreach(var (entity, navmeshObstacles) in frame.GetComponentIterator<NavMeshAvoidanceObstacle>()) {
          var position = Vector3.zero;

          if (frame.Has<Transform2D>(entity)) {
            position = frame.Unsafe.GetPointer<Transform2D>(entity)->Position.ToUnityVector3();
          }
          else if (frame.Has<Transform3D>(entity)) {
            position = frame.Unsafe.GetPointer<Transform3D>(entity)->Position.ToUnityVector3();
          }

          GizmoUtils.DrawGizmosCircle(position, navmeshObstacles.Radius.AsFloat, editorSettings.NavMeshAvoidanceColor);

          if (navmeshObstacles.Velocity != FPVector2.Zero) {
            GizmoUtils.DrawGizmoVector(
              position, position + navmeshObstacles.Velocity.XOY.ToUnityVector3().normalized * navmeshObstacles.Radius.AsFloat * 3.0f,
              GizmoUtils.DefaultArrowHeadLength * editorSettings.GizmoIconScale.AsFloat);
          }
        }
      }

      #endregion

      #region Navmesh And Pathfinder

      // ################## NavMeshes ##################

      if (editorSettings.DrawNavMesh) {
        var navmeshes = frame.Map.NavMeshes.Values;
        foreach (var navmesh in navmeshes) {
          MapNavMesh.CreateAndDrawGizmoMesh(navmesh, *frame.NavMeshRegionMask);

          for (Int32 i = 0; i < navmesh.Triangles.Length; i++) {
            var t = navmesh.Triangles[i];

            if (editorSettings.DrawNavMeshRegionIds) {
              if (t.Regions.HasValidRegions) {
                var s = string.Empty;
                for (int r = 0; r < frame.Map.Regions.Length; r++) {
                  if (t.Regions.IsRegionEnabled(r)) {
                    s += $"{frame.Map.Regions[r]} ({r})";
                  }
                }

                var vertex0 = navmesh.Vertices[t.Vertex0].Point.ToUnityVector3(true);
                var vertex1 = navmesh.Vertices[t.Vertex1].Point.ToUnityVector3(true);
                var vertex2 = navmesh.Vertices[t.Vertex2].Point.ToUnityVector3(true);
                UnityEditor.Handles.Label((vertex0 + vertex1 + vertex2) / 3.0f, s);
              }
            }
          }

          if (editorSettings.DrawNavMeshVertexNormals) {
            Gizmos.color = Color.blue;
            for (Int32 v = 0; v < navmesh.Vertices.Length; ++v) {
              if (navmesh.Vertices[v].Borders.Length >= 2) {
                var normal = NavMeshVertex.CalculateNormal(v, navmesh, *frame.NavMeshRegionMask);
                if (normal != FPVector3.Zero) {
                  GizmoUtils.DrawGizmoVector(navmesh.Vertices[v].Point.ToUnityVector3(true),
                                             navmesh.Vertices[v].Point.ToUnityVector3(true) +
                                             normal.ToUnityVector3(true) * editorSettings.GizmoIconScale.AsFloat * 0.33f,
                                             GizmoUtils.DefaultArrowHeadLength * editorSettings.GizmoIconScale.AsFloat * 0.33f);
                }
              }
            }
          }

          if (QuantumEditorSettings.Instance.DrawNavMeshLinks) {
            for (Int32 i = 0; i < navmesh.Links.Length; i++) {
              var color = Color.blue;
              var link = navmesh.Links[i];
              if (navmesh.Links[i].Region.IsSubset(*frame.NavMeshRegionMask) == false) {
                color = Color.gray;
              }

              Gizmos.color = color;
              GizmoUtils.DrawGizmoVector(
                navmesh.Links[i].Start.ToUnityVector3(), 
                navmesh.Links[i].End.ToUnityVector3(), 
                GizmoUtils.DefaultArrowHeadLength * editorSettings.GizmoIconScale.AsFloat);
              GizmoUtils.DrawGizmosCircle(navmesh.Links[i].Start.ToUnityVector3(), 0.1f * editorSettings.GizmoIconScale.AsFloat, color, style: editorSettings.ColliderGizmosStyle);
              GizmoUtils.DrawGizmosCircle(navmesh.Links[i].End.ToUnityVector3(), 0.1f * editorSettings.GizmoIconScale.AsFloat, color, style: editorSettings.ColliderGizmosStyle);
            }
          }
        }
      }

      // ################## NavMesh Borders ##################

      if (editorSettings.DrawNavMeshBorders) {
        Gizmos.color = Color.blue;
        var navmeshes = frame.Map.NavMeshes.Values;
        foreach (var navmesh in navmeshes) {
          for (Int32 i = 0; i < navmesh.Borders.Length; i++) {
            var b = navmesh.Borders[i];
            if (navmesh.IsBorderActive(i, *frame.NavMeshRegionMask) == false) { 
              // grayed out?
              continue;
            }

            Gizmos.color = Color.black;
            Gizmos.DrawLine(b.V0.ToUnityVector3(true), b.V1.ToUnityVector3(true));

            //// How to do a thick line? Multiple GizmoDrawLine also possible.
            //var color = QuantumEditorSettings.Instance.GetNavMeshColor(b.Regions);
            //UnityEditor.Handles.color = color;
            //UnityEditor.Handles.lighting = true;
            //UnityEditor.Handles.DrawAAConvexPolygon(
            //  b.V0.ToUnityVector3(true), 
            //  b.V1.ToUnityVector3(true), 
            //  b.V1.ToUnityVector3(true) + Vector3.up * 0.05f,
            //  b.V0.ToUnityVector3(true) + Vector3.up * 0.05f);
          }
        }
      }

      // ################## NavMesh Triangle Ids ##################

      if (editorSettings.DrawNavMeshTriangleIds) {
        UnityEditor.Handles.color = Color.white;
        var navmeshes = frame.Map.NavMeshes.Values;
        foreach (var navmesh in navmeshes) {
          for (Int32 i = 0; i < navmesh.Triangles.Length; i++) {
            UnityEditor.Handles.Label(navmesh.Triangles[i].Center.ToUnityVector3(true), i.ToString());
          }
        }
      }

      // ################## Pathfinder ##################

      if (frame.Navigation != null) {

        // Iterate though task contexts:
        var threadCount = frame.Context.TaskContext.ThreadCount;
        for (int t = 0; t < threadCount; t++) {

          // Iterate through path finders:
          var pf = frame.Navigation.GetDebugInformation(t).Item0;
          if (pf.RawPathSize >= 2) {
            if (editorSettings.DrawPathfinderRawPath) {
              for (Int32 i = 0; i < pf.RawPathSize; i++) {
                GizmoUtils.DrawGizmosCircle(pf.RawPath[i].Point.ToUnityVector3(true), 0.1f * editorSettings.GizmoIconScale.AsFloat, pf.RawPath[i].Link >= 0 ? Color.black : Color.magenta);
                if (i > 0) {
                  Gizmos.color = pf.RawPath[i].Link >= 0 && pf.RawPath[i].Link == pf.RawPath[i - 1].Link ? Color.black : Color.magenta;
                  Gizmos.DrawLine(pf.RawPath[i].Point.ToUnityVector3(true), pf.RawPath[i - 1].Point.ToUnityVector3(true));
                }
              }
            }

            if (editorSettings.DrawPathfinderRawTrianglePath) {
              var nmGuid = frame.Navigation.GetDebugInformation(t).Item1;
              if (!string.IsNullOrEmpty(nmGuid)) {
                var nm = UnityDB.FindAsset<NavMeshAsset>(nmGuid).Settings;
                for (Int32 i = 0; i < pf.RawPathSize; i++) {
                  var triangleIndex = pf.RawPath[i].Index;
                  if (triangleIndex >= 0) {
                    var vertex0 = nm.Vertices[nm.Triangles[triangleIndex].Vertex0].Point.ToUnityVector3(true);
                    var vertex1 = nm.Vertices[nm.Triangles[triangleIndex].Vertex1].Point.ToUnityVector3(true);
                    var vertex2 = nm.Vertices[nm.Triangles[triangleIndex].Vertex2].Point.ToUnityVector3(true);
                    var color = Color.magenta.Alpha(0.25f);
                    GizmoUtils.DrawGizmosTriangle(vertex0, vertex1, vertex2, true, color);
                    UnityEditor.Handles.color = color;
                    UnityEditor.Handles.lighting = true;
                    UnityEditor.Handles.DrawAAConvexPolygon(vertex0, vertex1, vertex2);
                  }
                }
              }
            }

            // Draw funnel on top of raw path
            if (editorSettings.DrawPathfinderFunnel) {
              for (Int32 i = 0; i < pf.PathSize; i++) {
                GizmoUtils.DrawGizmosCircle(pf.Path[i].Point.ToUnityVector3(true), 0.05f * editorSettings.GizmoIconScale.AsFloat, pf.Path[i].Link >= 0 ? Color.green * 0.5f : Color.green);
                if (i > 0) {
                  Gizmos.color = pf.Path[i].Link >= 0 && pf.Path[i].Link == pf.Path[i - 1].Link ? Color.green * 0.5f : Color.green;
                  Gizmos.DrawLine(pf.Path[i].Point.ToUnityVector3(true), pf.Path[i - 1].Point.ToUnityVector3(true));
                }
              }
            }
          }
        }
      }

      #endregion

      #region Various

      // ################## Prediction Area ##################

      if (editorSettings.DrawPredictionArea && frame.Context.Culling != null) {
        var context = frame.Context;
        if (context.PredictionAreaRadius != FP.UseableMax) {
#if QUANTUM_XY
          // The Quantum simulation does not know about QUANTUM_XY and always keeps the vector2 Y component in the vector3 Z component.
          var predictionAreaCenter = new UnityEngine.Vector3(context.PredictionAreaCenter.X.AsFloat, context.PredictionAreaCenter.Z.AsFloat, 0);
#else
          var predictionAreaCenter = context.PredictionAreaCenter.ToUnityVector3();
#endif
          GizmoUtils.DrawGizmosSphere(predictionAreaCenter, context.PredictionAreaRadius.AsFloat, editorSettings.PredictionAreaColor);
        }
      }

      #endregion
    }
#endif
  }

  public static unsafe void DrawCharacterController2DGizmo(Vector3 position, CharacterController2DConfig config, bool selected, QuantumGizmoStyle style) {
    var editorSettings = QuantumEditorSettings.Instance;
    GizmoUtils.DrawGizmosCircle(position + config.Offset.ToUnityVector3(),
      config.Radius.AsFloat, editorSettings.CharacterControllerColor, selected: selected, style: style);
    GizmoUtils.DrawGizmosCircle(position + config.Offset.ToUnityVector3(),
      config.Radius.AsFloat + config.Extent.AsFloat, editorSettings.AsleepColliderColor, selected: selected, style: style);
  }

  public static unsafe void DrawCharacterController3DGizmo(Vector3 position, CharacterController3DConfig config, bool selected, QuantumGizmoStyle style) {
    var editorSettings = QuantumEditorSettings.Instance;    
    GizmoUtils.DrawGizmosSphere(position + config.Offset.ToUnityVector3(),
      config.Radius.AsFloat, editorSettings.CharacterControllerColor, selected, style: style);
    GizmoUtils.DrawGizmosSphere(position + config.Offset.ToUnityVector3(),
      config.Radius.AsFloat + config.Extent.AsFloat, editorSettings.AsleepColliderColor, selected, style: style);
  }

  public static unsafe void DrawCollider3DGizmo(Frame frame, EntityRef handle, PhysicsCollider3D* collider, QuantumGizmoStyle style) {
    if (!frame.Unsafe.TryGetPointer(handle, out Transform3D* transform)) {
      return;
    }

    frame.Unsafe.TryGetPointer(handle, out PhysicsBody3D* body);

    var editorSettings = QuantumEditorSettings.Instance;
    
    Color color;
    if (body != null) {
      if (body->IsKinematic) {
        color = editorSettings.KinematicColliderColor;
      } else if (body->IsSleeping) {
        color = editorSettings.AsleepColliderColor;
      } else if (!body->Enabled) {
        color = editorSettings.DisabledColliderColor;
      } else {
        color = editorSettings.DynamicColliderColor;
      }
    } else {
      color = editorSettings.KinematicColliderColor;
    }

    if (collider->Shape.Type == Shape3DType.Compound) {
      DrawCompoundShape3D(frame, &collider->Shape, transform, color, style);
    } else {
      DrawShape3DGizmo(collider->Shape, transform->Position.ToUnityVector3(),
        transform->Rotation.ToUnityQuaternion(), color, style);
    }
  }

  public static unsafe void DrawCollider2DGizmo(Frame frame, EntityRef handle, PhysicsCollider2D* collider, QuantumGizmoStyle style) {
    if (!frame.Unsafe.TryGetPointer(handle, out Transform2D* t)) {
      return;
    }
    
    var hasBody              = frame.Unsafe.TryGetPointer<PhysicsBody2D>(handle, out var body);
    var hasTransformVertical = frame.Unsafe.TryGetPointer<Transform2DVertical>(handle, out var tVertical);

    var editorSettings = QuantumEditorSettings.Instance;
    
    Color color;
    if (hasBody) {
      if (body->IsKinematic) {
        color = editorSettings.KinematicColliderColor;
      } else if (body->IsSleeping) {
        color = editorSettings.AsleepColliderColor;
      } else if (!body->Enabled) {
        color = editorSettings.DisabledColliderColor;
      } else {
        color = editorSettings.DynamicColliderColor;
      }
    } else {
      color = editorSettings.KinematicColliderColor;
    }

    // Set 3d position of 2d object to simulate the vertical offset.
    var height = 0.0f;

#if QUANTUM_XY
    if (hasTransformVertical) {
      height = -tVertical->Height.AsFloat;
    }
#else
    if (hasTransformVertical) {
      height = tVertical->Height.AsFloat;
    }
#endif

    if (collider->Shape.Type == Shape2DType.Compound) {
      DrawCompoundShape2D(frame, &collider->Shape, t, tVertical, color, height, style);
    } else {
      var pos = t->Position.ToUnityVector3();
      var rot = t->Rotation.ToUnityQuaternion();

#if QUANTUM_XY
      if (hasTransformVertical) {
        pos.z = -tVertical->Position.AsFloat;
      }
#else
      if (hasTransformVertical) {
        pos.y = tVertical->Position.AsFloat;
      }
#endif

      DrawShape2DGizmo(collider->Shape, pos, rot, color, height, frame, style);
    }
  }

  public static unsafe void DrawShape3DGizmo(Shape3D s, Vector3 position, Quaternion rotation, Color color, QuantumGizmoStyle style = default) {

    var localOffset = s.LocalTransform.Position.ToUnityVector3();
    var localRotation = s.LocalTransform.Rotation.ToUnityQuaternion();

    position += rotation * localOffset;
    rotation *= localRotation;

    switch (s.Type) {
      case Shape3DType.Sphere:
        GizmoUtils.DrawGizmosSphere(position, s.Sphere.Radius.AsFloat, color, style: style);
        break;
      case Shape3DType.Box:
        GizmoUtils.DrawGizmosBox(position, s.Box.Extents.ToUnityVector3() * 2, color, style: style, rotation: rotation);
        break;
    }
  }

  public static unsafe void DrawShape2DGizmo(Shape2D s, Vector3 pos, Quaternion rot, Color color, float height, Frame currentFrame, QuantumGizmoStyle style = default) {

    var localOffset = s.LocalTransform.Position.ToUnityVector3();
    var localRotation = s.LocalTransform.Rotation.ToUnityQuaternion();

    pos += rot * localOffset;
    rot = rot * localRotation;

    switch (s.Type) {
      case Shape2DType.Circle:
        GizmoUtils.DrawGizmosCircle(pos, s.Circle.Radius.AsFloat, color, height: height, style: style);
        break;

      case Shape2DType.Box:
        var size = s.Box.Extents.ToUnityVector3() * 2.0f;
#if QUANTUM_XY
        size.z = height;
        pos.z += height * 0.5f;
#else
        size.y = height;
        pos.y += height * 0.5f;
#endif
        GizmoUtils.DrawGizmosBox(pos, size, color, rotation: rot, style: style);

        break;

      case Shape2DType.Polygon:
        PolygonCollider p;
        if (currentFrame != null) {
          p = currentFrame.FindAsset(s.Polygon.AssetRef);
        } else {
          p = (PolygonCollider)UnityDB.FindAsset<PolygonColliderAsset>(s.Polygon.AssetRef.Id)?.AssetObject;
        }

        if (p != null) {
          GizmoUtils.DrawGizmoPolygon2D(pos, rot, p.Vertices, height, color, style: style);
        }
        break;


      case Shape2DType.Edge:
        var extent = rot * Vector3.right * s.Edge.Extent.AsFloat;
        GizmoUtils.DrawGizmosEdge(pos - extent, pos + extent, height, false, color);
        break;
    }
  }
  
  private static unsafe void DrawCompoundShape2D(Frame f, Shape2D* compoundShape, Transform2D* transform, Transform2DVertical* transformVertical, Color color, float height, QuantumGizmoStyle style = default) {
    Debug.Assert(compoundShape->Type == Shape2DType.Compound);

    if (compoundShape->Compound.GetShapes(f, out var shapesBuffer, out var count)) {
      for (var i = 0; i < count; i++) {
        var shape = shapesBuffer + i;

        if (shape->Type == Shape2DType.Compound) {
          DrawCompoundShape2D(f, shape, transform, transformVertical, color, height, style);
        }
        else {
          var pos = transform->Position.ToUnityVector3();
          var rot = transform->Rotation.ToUnityQuaternion();
                  
      #if QUANTUM_XY
          if (transformVertical != null) {
            pos.z = -transformVertical->Position.AsFloat;
          }
      #else
          if (transformVertical != null) {
            pos.y = transformVertical->Position.AsFloat;
          }
      #endif
                  
          DrawShape2DGizmo(*shape, pos, rot, color, height, f, style);
        }
      }
    }
  }
  
  private static unsafe void DrawCompoundShape3D(Frame f, Shape3D* compoundShape, Transform3D* transform, Color color, QuantumGizmoStyle style = default) {
    Debug.Assert(compoundShape->Type == Shape3DType.Compound);

    if (compoundShape->Compound.GetShapes(f, out var shapesBuffer, out var count)) {
      for (var i = 0; i < count; i++) {
        var shape = shapesBuffer + i;

        if (shape->Type == Shape3DType.Compound) {
          DrawCompoundShape3D(f, shape, transform, color, style);
        } else {
          DrawShape3DGizmo(*shape, transform->Position.ToUnityVector3(), transform->Rotation.ToUnityQuaternion(), color, style);
        }
      }
    }
  }
}
