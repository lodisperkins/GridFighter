using Quantum;
using Quantum.Inspector;
using UnityEngine;
using Photon.Deterministic;
#if UNITY_EDITOR
using UnityEditor;
#endif

public partial class EntityComponentNavMeshPathfinder {

  [LocalReference]
  [DrawIf("Prototype.InitialTargetNavMesh.Id.Value", 0)]
  public MapNavMeshDefinition InitialTargetNavMeshReference;
  public override void Refresh() {
    if (InitialTargetNavMeshReference != null) {
      Prototype.InitialTargetNavMeshName = InitialTargetNavMeshReference.name;
    }
  }

#if UNITY_EDITOR

  [ContextMenu("Migrate To EntityPrototype")]
  public void Migrate() {
    var parent = GetComponent<EntityPrototype>();
    UnityEditor.Undo.RecordObject(parent, "Migrate");
    parent.NavMeshPathfinder.IsEnabled = true;
    parent.NavMeshPathfinder.InitialTarget.IsEnabled = this.Prototype.InitialTarget.HasValue;
    if (Prototype.InitialTarget.HasValue) {
      parent.NavMeshPathfinder.InitialTarget.Position = Prototype.InitialTarget.Value;
      parent.NavMeshPathfinder.InitialTarget.NavMesh.Asset = Prototype.InitialTargetNavMesh;
      parent.NavMeshPathfinder.InitialTarget.NavMesh.Name = Prototype.InitialTargetNavMeshName;
    }
    UnityEditor.Undo.DestroyObjectImmediate(this);
  }
#endif
}

[RequireComponent(typeof(EntityComponentPhysicsCollider2D))]
public partial class EntityComponentPhysicsBody2D {

#if UNITY_EDITOR

  private void OnValidate() {
    Prototype.EnsureVersionUpdated();
  }

  [ContextMenu("Migrate To EntityPrototype")]
  public void Migrate() {
    var parent = GetComponent<EntityPrototype>();
    UnityEditor.Undo.RecordObject(parent, "Migrate");
    Prototype.EnsureVersionUpdated();
    parent.PhysicsBody.IsEnabled = true;
    parent.PhysicsBody.Version2D = Prototype.Version;
    parent.PhysicsBody.Config2D = Prototype.Config;
    parent.PhysicsBody.AngularDrag = Prototype.AngularDrag;
    parent.PhysicsBody.Drag = Prototype.Drag;
    parent.PhysicsBody.Mass = Prototype.Mass;
    parent.PhysicsBody.CenterOfMass2D = Prototype.CenterOfMass;
    parent.PhysicsBody.GravityScale = Prototype.GravityScale;
    UnityEditor.Undo.DestroyObjectImmediate(this);
  }

#endif
}

[RequireComponent(typeof(EntityComponentPhysicsCollider3D))]
public partial class EntityComponentPhysicsBody3D {

#if UNITY_EDITOR

  private void OnValidate() {
    Prototype.EnsureVersionUpdated();
  }

  [ContextMenu("Migrate To EntityPrototype")]
  public void Migrate() {
    var parent = GetComponent<EntityPrototype>();
    UnityEditor.Undo.RecordObject(parent, "Migrate");
    Prototype.EnsureVersionUpdated();
    parent.PhysicsBody.IsEnabled = true;
    parent.PhysicsBody.Version3D = Prototype.Version;
    parent.PhysicsBody.AngularDrag = Prototype.AngularDrag;
    parent.PhysicsBody.Drag = Prototype.Drag;
    parent.PhysicsBody.Mass = Prototype.Mass;
    parent.PhysicsBody.RotationFreeze = Prototype.RotationFreeze;
    parent.PhysicsBody.CenterOfMass3D = Prototype.CenterOfMass;
    parent.PhysicsBody.GravityScale = Prototype.GravityScale;
    UnityEditor.Undo.DestroyObjectImmediate(this);
  }

#endif
}

[RequireComponent(typeof(EntityComponentPhysicsCollider2D))]
public partial class EntityComponentPhysicsCallbacks2D {

#if UNITY_EDITOR

  [ContextMenu("Migrate To EntityPrototype")]
  public void Migrate() {
    var parent = GetComponent<EntityPrototype>();
    UnityEditor.Undo.RecordObject(parent, "Migrate");
    parent.PhysicsCollider.CallbackFlags = Prototype.CallbackFlags;
    UnityEditor.Undo.DestroyObjectImmediate(this);
  }

#endif
}

[RequireComponent(typeof(EntityComponentPhysicsCollider3D))]
public partial class EntityComponentPhysicsCallbacks3D {

#if UNITY_EDITOR

  [ContextMenu("Migrate To EntityPrototype")]
  public void Migrate() {
    var parent = GetComponent<EntityPrototype>();
    UnityEditor.Undo.RecordObject(parent, "Migrate");
    parent.PhysicsCollider.CallbackFlags = Prototype.CallbackFlags;
    UnityEditor.Undo.DestroyObjectImmediate(this);
  }

#endif
}

public partial class EntityComponentPhysicsCollider2D {
#if (!UNITY_2019_1_OR_NEWER || QUANTUM_ENABLE_PHYSICS2D) && !QUANTUM_DISABLE_PHYSICS2D
  [MultiTypeReference(typeof(BoxCollider2D), typeof(CircleCollider2D)
#if (!UNITY_2019_1_OR_NEWER || QUANTUM_ENABLE_PHYSICS3D) && !QUANTUM_DISABLE_PHYSICS3D
    , typeof(BoxCollider), typeof(SphereCollider)
#endif
    )]
  public Component SourceCollider;

  private void OnValidate() {
    if (EntityPrototypeUtils.TrySetShapeConfigFromSourceCollider2D(Prototype.ShapeConfig, transform, SourceCollider)) {
      Prototype.IsTrigger = SourceCollider.IsColliderTrigger();
      Prototype.Layer = SourceCollider.gameObject.layer;
    }
  }

  public override void Refresh() {
    if (EntityPrototypeUtils.TrySetShapeConfigFromSourceCollider2D(Prototype.ShapeConfig, transform, SourceCollider)) {
      Prototype.IsTrigger = SourceCollider.IsColliderTrigger();
      Prototype.Layer = SourceCollider.gameObject.layer;
    }
  }

#if UNITY_EDITOR

  [ContextMenu("Migrate To EntityPrototype")]
  public void Migrate() {
    var parent = GetComponent<EntityPrototype>();
    UnityEditor.Undo.RecordObject(parent, "Migrate");
    parent.PhysicsCollider.IsEnabled = true;
    parent.PhysicsCollider.IsTrigger = Prototype.IsTrigger;
    parent.PhysicsCollider.Layer = Prototype.Layer;
    parent.PhysicsCollider.Material = Prototype.PhysicsMaterial;
    parent.PhysicsCollider.Shape2D = Prototype.ShapeConfig;
    parent.PhysicsCollider.SourceCollider = SourceCollider;
    UnityEditor.Undo.DestroyObjectImmediate(this);
  }

  public override void OnInspectorGUI(SerializedObject so, IQuantumEditorGUI QuantumEditorGUI) {
    var sourceCollider = so.FindPropertyOrThrow(nameof(EntityComponentPhysicsCollider2D.SourceCollider));

    EditorGUILayout.PropertyField(sourceCollider);

    bool enterChildren = true;
    for (var p = so.FindPropertyOrThrow("Prototype"); p.Next(enterChildren) && p.depth >= 1; enterChildren = false) {
      using (new EditorGUI.DisabledScope(sourceCollider.objectReferenceValue != null &&
                                         (p.name == nameof(Quantum.Prototypes.PhysicsCollider2D_Prototype.Layer) ||
                                         p.name == nameof(Quantum.Prototypes.PhysicsCollider2D_Prototype.IsTrigger)))) {
        QuantumEditorGUI.PropertyField(p);
      }
    }

    try {
      // sync with Unity collider, if set
      ((EntityComponentPhysicsCollider2D)so.targetObject).Refresh();
    } catch (System.Exception ex) {
      EditorGUILayout.HelpBox(ex.Message, MessageType.Error);
    }
  }

#endif
#endif
}

public partial class EntityComponentPhysicsCollider3D {
#if (!UNITY_2019_1_OR_NEWER || QUANTUM_ENABLE_PHYSICS3D) && !QUANTUM_DISABLE_PHYSICS3D
  [MultiTypeReference(typeof(BoxCollider), typeof(SphereCollider))]
  public Collider SourceCollider3D;

  private void OnValidate() {
    if (EntityPrototypeUtils.TrySetShapeConfigFromSourceCollider3D(Prototype.ShapeConfig, transform, SourceCollider3D)) {
      Prototype.IsTrigger = SourceCollider3D.isTrigger;
      Prototype.Layer = SourceCollider3D.gameObject.layer;
    }
  }

  public override void Refresh() {
    if (EntityPrototypeUtils.TrySetShapeConfigFromSourceCollider3D(Prototype.ShapeConfig, transform, SourceCollider3D)) {
      Prototype.IsTrigger = SourceCollider3D.isTrigger;
      Prototype.Layer = SourceCollider3D.gameObject.layer;
    }
  }

#if UNITY_EDITOR

  [ContextMenu("Migrate To EntityPrototype")]
  public void Migrate() {
    var parent = GetComponent<EntityPrototype>();
    UnityEditor.Undo.RecordObject(parent, "Migrate");
    parent.PhysicsCollider.IsEnabled = true;
    parent.PhysicsCollider.IsTrigger = Prototype.IsTrigger;
    parent.PhysicsCollider.Layer = Prototype.Layer;
    parent.PhysicsCollider.Material = Prototype.PhysicsMaterial;
    parent.PhysicsCollider.Shape3D = Prototype.ShapeConfig;
    parent.PhysicsCollider.SourceCollider = SourceCollider3D;
    UnityEditor.Undo.DestroyObjectImmediate(this);
  }

  public override void OnInspectorGUI(SerializedObject so, IQuantumEditorGUI QuantumEditorGUI) {
    var sourceCollider = so.FindPropertyOrThrow(nameof(EntityComponentPhysicsCollider3D.SourceCollider3D));

    EditorGUILayout.PropertyField(sourceCollider);

    bool enterChildren = true;
    for (var p = so.FindPropertyOrThrow("Prototype"); p.Next(enterChildren) && p.depth >= 1; enterChildren = false) {
      using (new EditorGUI.DisabledScope(sourceCollider.objectReferenceValue != null &&
                                         (p.name == nameof(Quantum.Prototypes.PhysicsCollider3D_Prototype.Layer) ||
                                          p.name == nameof(Quantum.Prototypes.PhysicsCollider3D_Prototype.IsTrigger)))) {
        QuantumEditorGUI.PropertyField(p);
      }
    }

    try {
      // sync with Unity collider, if set
      ((EntityComponentPhysicsCollider3D)so.targetObject).Refresh();
    } catch (System.Exception ex) {
      EditorGUILayout.HelpBox(ex.Message, MessageType.Error);
    }
  }

#endif
#endif
}

[RequireComponent(typeof(EntityPrototype))]
public partial class EntityComponentPhysicsJoints2D {
  private void OnValidate() => AutoConfigureDistance();

  public override void Refresh() => AutoConfigureDistance();

  private void AutoConfigureDistance() {
    if (Prototype.JointConfigs == null) {
      return;
    }

    FPMathUtils.LoadLookupTables();

    foreach (var config in Prototype.JointConfigs) {
      if (config.AutoConfigureDistance && config.JointType != Quantum.Physics2D.JointType.None) {
        var anchorPos = transform.position.ToFPVector2() + FPVector2.Rotate(config.Anchor, transform.rotation.ToFPRotation2D());
        var connectedPos = config.ConnectedAnchor;

        if (config.ConnectedEntity != null) {
          var connectedTransform = config.ConnectedEntity.transform;
          connectedPos = FPVector2.Rotate(connectedPos, connectedTransform.rotation.ToFPRotation2D());
          connectedPos += connectedTransform.position.ToFPVector2();
        }

        config.Distance = FPVector2.Distance(anchorPos, connectedPos);
        config.MinDistance = config.Distance;
        config.MaxDistance = config.Distance;
      }

      if (config.MinDistance > config.MaxDistance) {
        config.MinDistance = config.MaxDistance;
      }
    }
  }

#if UNITY_EDITOR
  private void OnDrawGizmos() {
    DrawGizmos(selected: false);
  }

  private void OnDrawGizmosSelected() {
    DrawGizmos(selected: true);
  }

  private void DrawGizmos(bool selected) {
    if (!QuantumGameGizmos.ShouldDraw(QuantumEditorSettings.Instance.DrawJointGizmos, selected)) {
      return;
    }

    var entity = GetComponent<EntityPrototype>();

    if (entity == null || Prototype.JointConfigs == null) {
      return;
    }

    FPMathUtils.LoadLookupTables();

    var editorSettings = QuantumEditorSettings.Instance;
    foreach (var config in Prototype.JointConfigs) {
      GizmoUtils.DrawGizmosJoint2D(config, transform, config.ConnectedEntity == null ? null : config.ConnectedEntity.transform, selected, editorSettings, editorSettings.JointGizmosStyle);
    }
  }
#endif
}

[RequireComponent(typeof(EntityPrototype))]
public partial class EntityComponentPhysicsJoints3D {
  private void OnValidate() => Refresh();

  public override void Refresh() {
    AutoConfigureDistance();
  }

  private void AutoConfigureDistance() {
    if (Prototype.JointConfigs == null) {
      return;
    }

    FPMathUtils.LoadLookupTables();

    foreach (var config in Prototype.JointConfigs) {
      if (config.AutoConfigureDistance && config.JointType != Quantum.Physics3D.JointType3D.None) {
        var anchorPos = transform.position.ToFPVector3() + transform.rotation.ToFPQuaternion() * config.Anchor;
        var connectedPos = config.ConnectedAnchor;

        if (config.ConnectedEntity != null) {
          var connectedTransform = config.ConnectedEntity.transform;
          connectedPos = connectedTransform.rotation.ToFPQuaternion() * connectedPos;
          connectedPos += connectedTransform.position.ToFPVector3();
        }

        config.Distance = FPVector3.Distance(anchorPos, connectedPos);
        config.MinDistance = config.Distance;
        config.MaxDistance = config.Distance;
      }

      if (config.MinDistance > config.MaxDistance) {
        config.MinDistance = config.MaxDistance;
      }
    }
  }

#if UNITY_EDITOR
  private void OnDrawGizmos() {
    DrawGizmos(selected: false);
  }

  private void OnDrawGizmosSelected() {
    DrawGizmos(selected: true);
  }

  private void DrawGizmos(bool selected) {
    if (!QuantumGameGizmos.ShouldDraw(QuantumEditorSettings.Instance.DrawJointGizmos, selected)) {
      return;
    }

    var entity = GetComponent<EntityPrototype>();

    if (entity == null || Prototype.JointConfigs == null) {
      return;
    }

    FPMathUtils.LoadLookupTables();

    var editorSettings = QuantumEditorSettings.Instance;
    foreach (var config in Prototype.JointConfigs) {
      GizmoUtils.DrawGizmosJoint3D(config, transform, config.ConnectedEntity == null ? null : config.ConnectedEntity.transform, selected, editorSettings, editorSettings.JointGizmosStyle);
    }
  }
#endif
}

public partial class EntityComponentTransform2D {
  public bool AutoSetPosition = true;
  public bool AutoSetRotation = true;

  private void OnValidate() {
    Refresh();
  }

  public override void Refresh() {
    if (AutoSetPosition) {
      Prototype.Position = transform.position.ToFPVector2();
    }
    if (AutoSetRotation) {
      Prototype.Rotation = transform.rotation.ToFPRotation2DDegrees();
    }
  }

#if UNITY_EDITOR

  [ContextMenu("Migrate To EntityPrototype")]
  public void Migrate() {
    var parent = GetComponent<EntityPrototype>();
    UnityEditor.Undo.RecordObject(parent, "Migrate");
    parent.TransformMode = EntityPrototypeTransformMode.Transform2D;
    UnityEditor.Undo.DestroyObjectImmediate(this);
  }

  public override void OnInspectorGUI(SerializedObject so, IQuantumEditorGUI QuantumEditorGUI) {
    var autoSetPosition = so.FindPropertyOrThrow(nameof(EntityComponentTransform2D.AutoSetPosition));
    var autoSetRotation = so.FindPropertyOrThrow(nameof(EntityComponentTransform2D.AutoSetRotation));

    EditorGUILayout.PropertyField(autoSetPosition);
    EditorGUILayout.PropertyField(autoSetRotation);

    using (new EditorGUI.DisabledScope(autoSetPosition.boolValue)) {
      QuantumEditorGUI.PropertyField(so.FindPropertyOrThrow("Prototype.Position"));
    }
    using (new EditorGUI.DisabledScope(autoSetRotation.boolValue)) {
      QuantumEditorGUI.PropertyField(so.FindPropertyOrThrow("Prototype.Rotation"));
    }
  }
#endif
}

public partial class EntityComponentTransform2DVertical {

  [UnityEngine.Tooltip("If not set lossyScale.y of the transform will be used")]
  public bool AutoSetHeight = true;

  public bool AutoSetPosition = true;

  private void OnValidate() {
    Refresh();
  }

  public override void Refresh() {
    if (AutoSetPosition) {
      // based this on MapDataBaker for colliders
#if QUANTUM_XY
      Prototype.Position = -transform.position.z.ToFP();
#else
      Prototype.Position = transform.position.y.ToFP();
#endif
    }

    if (AutoSetHeight) {
      Prototype.Height = transform.lossyScale.y.ToFP();
    }
  }

#if UNITY_EDITOR

  [ContextMenu("Migrate To EntityPrototype")]
  public void Migrate() {
    var parent = GetComponent<EntityPrototype>();
    UnityEditor.Undo.RecordObject(parent, "Migrate");
    parent.Transform2DVertical.IsEnabled = true;
    parent.Transform2DVertical.Height = Prototype.Height;
    parent.Transform2DVertical.PositionOffset = Prototype.Position - transform.position.ToFPVerticalPosition();
    parent.TransformMode = EntityPrototypeTransformMode.Transform2D;
    UnityEditor.Undo.DestroyObjectImmediate(this);
  }

  public override void OnInspectorGUI(SerializedObject so, IQuantumEditorGUI QuantumEditorGUI) {
    var autoSetPosition = so.FindPropertyOrThrow(nameof(EntityComponentTransform2DVertical.AutoSetPosition));
    var autoSetHeight = so.FindPropertyOrThrow(nameof(EntityComponentTransform2DVertical.AutoSetHeight));

    EditorGUILayout.PropertyField(autoSetPosition);
    EditorGUILayout.PropertyField(autoSetHeight);

    using (new EditorGUI.DisabledScope(autoSetPosition.boolValue)) {
      QuantumEditorGUI.PropertyField(so.FindPropertyOrThrow("Prototype.Position"));
    }
    using (new EditorGUI.DisabledScope(autoSetHeight.boolValue)) {
      QuantumEditorGUI.PropertyField(so.FindPropertyOrThrow("Prototype.Height"));
    }
  }
#endif
}

public partial class EntityComponentTransform3D {
  public bool AutoSetPosition = true;
  public bool AutoSetRotation = true;

  private void OnValidate() {
    Refresh();
  }

  public override void Refresh() {
    if (AutoSetPosition) {
      Prototype.Position = transform.position.ToFPVector3();
    }
    if (AutoSetRotation) {
      Prototype.Rotation = transform.rotation.eulerAngles.ToFPVector3();
    }
  }

#if UNITY_EDITOR

  [ContextMenu("Migrate To EntityPrototype")]
  public void Migrate() {
    var parent = GetComponent<EntityPrototype>();
    UnityEditor.Undo.RecordObject(parent, "Migrate");
    parent.TransformMode = EntityPrototypeTransformMode.Transform3D;
    UnityEditor.Undo.DestroyObjectImmediate(this);
  }

  public override void OnInspectorGUI(SerializedObject so, IQuantumEditorGUI QuantumEditorGUI) {
    var autoSetPosition = so.FindPropertyOrThrow(nameof(EntityComponentTransform2D.AutoSetPosition));
    var autoSetRotation = so.FindPropertyOrThrow(nameof(EntityComponentTransform2D.AutoSetRotation));

    EditorGUILayout.PropertyField(autoSetPosition);
    EditorGUILayout.PropertyField(autoSetRotation);

    using (new EditorGUI.DisabledScope(autoSetPosition.boolValue)) {
      QuantumEditorGUI.PropertyField(so.FindPropertyOrThrow("Prototype.Position"));
    }
    using (new EditorGUI.DisabledScope(autoSetRotation.boolValue)) {
      QuantumEditorGUI.PropertyField(so.FindPropertyOrThrow("Prototype.Rotation"));
    }
  }

#endif
}