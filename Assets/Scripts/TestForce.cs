using FixedPoints;
using System.Collections;
using System.Collections.Generic;
using System.Drawing.Printing;
using Types;
using UnityEditor;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;
using static UnityEngine.Rendering.DebugUI.Table;
using static UnityEngine.UIElements.UxmlAttributeDescription;

public class TestForce : MonoBehaviour
{
    public Fixed32 units;
    public Fixed32 radians;
    public Rigidbody body;

    // Start is called before the first frame update
    void Start()
    {
        body = GetComponent<Rigidbody>();
    }

    public void ApplyForce()
    {
        // Uses the total knockback and panel distance to find how far the object is travelling
        Fixed32 displacement = units;
        //Finds the magnitude of the force vector to be applied 
        Fixed32 val1 = displacement * Fixed32.Abs(Physics.gravity.y);
        Fixed32 val2 = Fixed32.Sin(2 * radians);
        Fixed32 val3 = Fixed32.Sqrt(val1 / Fixed32.Abs(val2));
        Fixed32 magnitude = val3;

        //If the magnitude is not a number the attack must be too weak. Return an empty vector
        

        //Return the knockback force
        body.AddForce(new Vector3(Fixed32.Cos(radians), Fixed32.Sin(radians), 0) * (magnitude * body.mass), ForceMode.Impulse);
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(TestForce))]
public class TestForceEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Draw default Inspector
        DrawDefaultInspector();

        // Reference to the TestForce script
        TestForce testForce = (TestForce)target;

        // Add a button to the Inspector
        if (GUILayout.Button("Apply Force"))
        {
            // Call the ApplyForce method when the button is clicked
            testForce.ApplyForce();
        }

        // Add a button to the Inspector
        if (GUILayout.Button("Reset"))
        {
            // Call the ApplyForce method when the button is clicked
            testForce.transform.position = Vector3.zero;
            testForce.body.velocity = Vector3.zero;
        }
    }
}
#endif