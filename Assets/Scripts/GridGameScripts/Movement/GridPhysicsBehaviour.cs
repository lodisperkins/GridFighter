using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GridGame.GamePlay.GridScripts;
using GridGame.GamePlay.OtherScripts;
using UnityEditor;
using GridGame.Movement;
using UnityEngine.Experimental;
using System.Xml.Serialization;
using System.IO;
using System;
using System.Xml;
using System.Text;

namespace GridGame.Movement
{
    [RequireComponent(typeof(SeekBehaviour),typeof(Rigidbody))]
    public class GridPhysicsBehaviour : MonoBehaviour
    {
        
        private PanelBehaviour trailingPanel;
        public PanelBehaviour currentPanel;
        private SeekBehaviour seekScript;
        private Rigidbody rigidbody;
        private Vector3 heightOffset;
        private Vector2 currentVelocity;
        private PanelBehaviour targetPanel;
        private ScreenShakeBehaviour shakeScript;
        [SerializeField]
        public GridGame.VariableScripts.ObjectList _collisions;
        [SerializeField]
        private bool isMoving;
        [SerializeField]
        private GridGame.Event onHit;
        [SerializeField]
        private bool stopsWhenHit = true;
        [SerializeField]
        private bool damagesOnHit = true;
        [SerializeField]
        private float _snapDistance = 0.3f;
        [SerializeField]
        private bool _isMovable = true;

        public bool IsMoving
        {
            get
            {
                return isMoving;
            }
        }

        public bool StopsWhenHit
        {
            get
            {
                return stopsWhenHit;
            }

            set
            {
                stopsWhenHit = value;
            }
        }

        public bool IsMovable
        {
            get
            {
                return _isMovable;
            }

            set
            {
                _isMovable = value;
            }
        }

        // Use this for initialization
        void Start()
        {
            LoadCollisionData();
        }
        private void Awake()
        {
            seekScript = GetComponent<SeekBehaviour>();
            seekScript.SeekEnabled = false;
            rigidbody = GetComponent<Rigidbody>();
            currentVelocity = new Vector2(0, 0);
            heightOffset = new Vector3(0, transform.position.y, 0);
            shakeScript = GetComponent<ScreenShakeBehaviour>();
        }
        public void AddForce(Vector2 force)
        {
            if (!IsMovable || !seekScript)
                return;

            currentVelocity = force;
            Vector2 destination = currentPanel.Position + force;
            destination.Set(Mathf.Clamp(destination.x,0,9),Mathf.Clamp(destination.y,0,3));
            targetPanel = BlackBoard.grid.GetPanelFromGlobalList(destination);

            if (!targetPanel)
                return;

            seekScript.Init(targetPanel.transform.position + heightOffset, rigidbody.velocity, 20, _snapDistance, true, false,true);
            seekScript.SeekEnabled = true;
        
        }

        public void AddForce(float speed, PanelBehaviour destination)
        {
            if (!IsMovable)
                return;

            targetPanel = destination;
            currentVelocity = (targetPanel.Position - currentPanel.Position).normalized * speed;
            seekScript.Init(targetPanel.transform.position + heightOffset, rigidbody.velocity, 20, _snapDistance, true, false, true);
            seekScript.SeekEnabled = true;

        }
        public static Vector2 ConvertToGridVector(Vector3 worldVector)
        {
            return new Vector2(worldVector.z, -worldVector.x);
        }
        public bool CheckCollisions(string tag)
        {
            if (!_collisions)
                return false;

            foreach(GridGame.VariableScripts.CollisionChannel channel in _collisions)
            {
                if(channel)
                {
                    if (channel.name == tag && channel.collisionEnabled)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        public void OnTriggerEnter(Collider other)
        {
          
            if(seekScript.SeekEnabled && currentPanel)
            {
                if(other.CompareTag("Panel") && other != currentPanel.gameObject)
                {
                    trailingPanel = currentPanel;
                    currentPanel = other.GetComponent<PanelBehaviour>();
                }
                if (CheckCollisions(other.tag) == false)
                {
                    return;
                }
                ResolveCollsion(other);
            }
        }
        public void ResolveCollsion(Collider other)
        {
            onHit.Raise(gameObject);
            if (stopsWhenHit)
            {
                targetPanel = trailingPanel;
                seekScript.Init(targetPanel.transform.position + heightOffset, rigidbody.velocity, 20, _snapDistance, true, false,true);
                seekScript.SeekEnabled = true;
                currentPanel = trailingPanel;
            }
            if(damagesOnHit)
            {
                HealthBehaviour healthScript = other.GetComponent<HealthBehaviour>();
                if (healthScript)
                {
                    healthScript.takeDamage((int)currentVelocity.magnitude);
                } 
            }
        }
        public void CleanList()
        {
            if(_collisions != null)
            {
                if(_collisions[0] == null)
                {
                    _collisions.Clear();
                    return;
                }
            }
            else
            {
                Debug.LogError("Collision channel list has no value!");
            }
        }

        public void LoadCollisionData()
        {
            List<bool> collisions = new List<bool>();
            XmlSerializer serializer = new XmlSerializer(typeof(List<bool>));
            string clone = "(Clone)";
            string newName = name.Trim(clone.ToCharArray());
            string filePath = "CollisionData/" + newName + "CollisionData.xml";
            if (File.Exists(filePath))
            {
                StreamReader reader = new StreamReader(filePath);
                collisions = (List<bool>)serializer.Deserialize(reader);
                UpdateCollisionChannels(collisions);
                reader.Close();
            }
        }

        public List<bool> GetCollisionValues()
        {
            List<bool> collisionValues = new List<bool>();
            if(_collisions == null)
            {
                Debug.LogError("Collision channel list has no value!");
                return new List<bool>();
            }
            foreach(GridGame.VariableScripts.CollisionChannel channel in _collisions)
            {
                if(channel)
                    collisionValues.Add(channel.collisionEnabled);
            }
            return collisionValues;
        }
        public void UpdateCollisionChannels(List<bool> newValues)
        {
            if (_collisions == null)
            {
                Debug.LogWarning("Collision channel list has no value!");
                return;
            }
            _collisions.Clear();
            //for(int i =0; i< UnityEditorInternal.InternalEditorUtility.tags.Length; i++)
            //{
            //    _collisions.Add(GridGame.VariableScripts.CollisionChannel.CreateInstance(UnityEditorInternal.InternalEditorUtility.tags[i], newValues[i]));
            //}
        }

        
        // Update is called once per frame
        void Update()
        {
            isMoving = seekScript.SeekEnabled;
            if(shakeScript)
            {
                shakeScript.isMoving = isMoving;
            }
        }
    }
}

public class TestClass
{
    public int health = 100;
    public int armor = 22;
    public bool testBool = true;
}

#if UNITY_EDITOR
[CustomEditor(typeof(GridPhysicsBehaviour))]
public class GridPhysicsEditor : Editor
{
    XmlSerializer serializer = new XmlSerializer(typeof(List<bool>));
    public List<bool> collisions = new List<bool>(10);
    TestClass test = new TestClass();
    public void SaveCollisionData()
    {
        string clone = "(Clone)";
        string newName = target.name.Trim(clone.ToCharArray());
        string filePath = "CollisionData/" + newName + "CollisionData.xml";
        StreamWriter writer = new StreamWriter(filePath);
        serializer.Serialize(writer, collisions);
        writer.Close();
    }

    public void LoadCollisionData()
    {
        string clone = "(Clone)";
        string newName = target.name.Trim(clone.ToCharArray());
        string filePath = "CollisionData/" + newName + "CollisionData.xml";
        if (File.Exists(filePath))
        {
            
            StreamReader reader = new StreamReader(filePath);
            collisions = (List<bool>)serializer.Deserialize(reader);
            reader.Close();
        }
    }
    public override void OnInspectorGUI()
    {
        GridPhysicsBehaviour myscript = (GridPhysicsBehaviour)target;
        DrawDefaultInspector();

        if(collisions == null)
        {
            return;
        }

        LoadCollisionData();

        for (int i =0; i< UnityEditorInternal.InternalEditorUtility.tags.Length; i++)
        {
            if(i >= collisions.Count)
            {
                collisions.Add(new bool());
            }
            collisions[i] = EditorGUILayout.Toggle(UnityEditorInternal.InternalEditorUtility.tags[i], collisions[i]);
        }

        SaveCollisionData();
    }
}
#endif

