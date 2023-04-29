using Lodis.Animation;
using Lodis.Gameplay;
using Lodis.Movement;
using UnityEditor;
using UnityEngine;

namespace Lodis.Utility
{

    public class CharacterMeshManagerBehaviour : MonoBehaviour
    {

        [SerializeField]
        private GameObject _currentMesh;


        private KnockbackBehaviour _knockback;
        private MovesetBehaviour _moveset;
        private HitStopBehaviour _hitStop;

        private ColorManagerBehaviour _colorManager;
        private CharacterAnimationBehaviour _characterAnimation;
        private EyeAnimationBehaviour _eyeAnimator;

        private void Awake()
        {
            Animator animator = GetComponentInChildren<Animator>();
            Avatar avatar = animator.avatar;
            SkinnedMeshRenderer renderer = GetComponentInChildren<SkinnedMeshRenderer>();
            Mesh mesh = renderer.sharedMesh;
        }

        private SkinnedMeshRenderer GetRenderer(string name)
        {
            Transform meshTransform = transform.GetChild(1);

            for (int i = 0; i < meshTransform.childCount; i++)
            {
                Transform current = meshTransform.GetChild(i);
                for (int j = 0; j < current.childCount; j++)
                {
                    Transform currentChild = current.GetChild(j);
                    if (currentChild.name == name)
                        return currentChild.GetComponentInChildren<SkinnedMeshRenderer>();
                }
            }

            return null;
        }

        private Transform FindChild(Transform trans, string name)
        {
            Transform child = null;
            
            child = trans.Find(name);

            if (child)
                return child;
            
            for (int i = 0; i < trans.childCount; i++)
            {
                child = FindChild(trans.GetChild(i), name);
                if (child)
                    return child;
            }

            return null;
        }

        /// <summary>
        /// Sets values that require a mesh for all relevant objects in prefab.
        /// </summary>
        public void UpdateValues()
        {
            //Removes the children from the base skeletal mesh object and makes them a child of the prefab.
            GameObject meshInstance = Instantiate(_currentMesh.gameObject, transform);

            Transform child1 = meshInstance.transform.GetChild(0);
            Transform child2 = meshInstance.transform.GetChild(1);
            meshInstance.transform.DetachChildren();

            child1.parent = transform;
            child2.parent = transform;

            DestroyImmediate(meshInstance);

            //Get reference to all scripts to replace mesh values for.
            _knockback = GetComponentInParent<KnockbackBehaviour>();
            _moveset = GetComponentInParent<MovesetBehaviour>();
            _hitStop = GetComponentInParent<HitStopBehaviour>();
            _colorManager = GetComponentInParent<ColorManagerBehaviour>();
            _eyeAnimator = GetComponent<EyeAnimationBehaviour>();

            //Update renderers for in game feedback.
            _knockback.MeshRenderer = GetRenderer("body_low");
            _eyeAnimator.Renderer = GetRenderer("eyes_mesh");

            CharacterAnimationBehaviour characterAnimationBehaviour = GetComponent<CharacterAnimationBehaviour>();
            Animator animator = GetComponent<Animator>();

            _moveset.AnimationBehaviour = characterAnimationBehaviour;
            _hitStop.Animator = animator;

            //Set moveset transform using common body parts.
            Transform leftArm = FindChild(transform, "GridFighterBase_l_Arm_WristSHJnt");
            Transform leftLeg = FindChild(transform, "GridFighterBase_l_Leg_AnkleSHJnt");

            Transform rightArm = FindChild(transform, "GridFighterBase_r_Arm_WristSHJnt");
            Transform rightLeg = FindChild(transform, "GridFighterBase_r_Leg_AnkleSHJnt");

            _moveset.RightMeleeSpawns = new Transform[] { rightLeg, rightArm };
            _moveset.LeftMeleeSpawns = new Transform[] { leftLeg, leftArm };
        }
    }

#if UNITY_EDITOR

    [CustomEditor(typeof(CharacterMeshManagerBehaviour))]
    public class CharacterMeshManagerEditor : Editor
    {
        private CharacterMeshManagerBehaviour _owner;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            _owner = (CharacterMeshManagerBehaviour)target;

            if (GUILayout.Button("Apply Mesh"))
            {
                _owner.UpdateValues();
            }
        }

        /// <summary>
        /// Creates a new variant of the base character prefab and puts it in its own folder.
        /// </summary>
        [MenuItem("Assets/Create/Character", false, 5)]
        public static void CreateBaseCharacterVariant()
        {
            Object characterBasePrefab = AssetDatabase.LoadAssetAtPath<Object>("Assets/Resources/CharacterBase.prefab");
            GameObject prefabInstance = (GameObject)PrefabUtility.InstantiatePrefab(characterBasePrefab);
            AssetDatabase.CreateFolder("Assets/Characters", "NewCharacter");
            PrefabUtility.SaveAsPrefabAsset(prefabInstance, "Assets/Characters/NewCharacter/NewCharacter.prefab");
        }
    }

#endif
}