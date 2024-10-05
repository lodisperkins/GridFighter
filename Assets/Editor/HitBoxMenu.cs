using UnityEngine;
using UnityEditor;
using Lodis.Gameplay;
using Lodis.GridScripts;
using Lodis.Movement;

public class HitBoxMenu : MonoBehaviour
{
    // The ContextMenu attribute allows us to add a right-click menu option in the hierarchy
    [MenuItem("GameObject/Create Hit Box", false, 10)]
    static void CreateHitBox(MenuCommand menuCommand)
    {
        // Create a new GameObject
        GameObject hitBox = new GameObject("HitBox");

        // Add the necessary components to the GameObject

        HitColliderBehaviour hit = hitBox.AddComponent<HitColliderBehaviour>();
        hit.OnHitObject = Resources.Load<CustomEventSystem.Event>("Events/OnHit");
        hit.EntityCollider = new GridCollider();
        hit.EntityCollider.Overlap = true;
        hit.DebuggingEnabled = true;

        GridTrackerBehaviour tracker = hitBox.AddComponent<GridTrackerBehaviour>();
        tracker.Marker = MarkerType.DANGER;
        tracker.MarkCollider = true;
        tracker.ColliderToTrack = hit;

        // Ensure the new GameObject gets parented to the currently selected GameObject in the hierarchy, if applicable
        GameObjectUtility.SetParentAndAlign(hitBox, menuCommand.context as GameObject);

        // Register the creation in the undo system so that the action can be undone in the editor
        Undo.RegisterCreatedObjectUndo(hitBox, "Create Hit Box");

        // Select the newly created GameObject in the hierarchy
        Selection.activeObject = hitBox;
    }
}
