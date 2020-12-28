using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GridGame.VariableScripts
{
    [CreateAssetMenu(menuName = "Variables/BlockVariable")]

    public class BlockVariable : ScriptableObject {

        [SerializeField]
        private GameObject block;
        public GameObject Block
        {
            get
            {
                return block;
            }
            set
            {
                block = value;
            }
        }
        public Color Color
        {

            get
            {
                return block.GetComponent<MeshRenderer>().sharedMaterial.color;
            }
            set
            {
                block.GetComponent<MeshRenderer>().sharedMaterial.color = value;
            }
        }
        public BlockBehaviour BlockScript
        {
            get
            {
                return block.GetComponent<BlockBehaviour>();
            }
        }
        public int Cost
        {
            get
            {
                return block.GetComponent<BlockBehaviour>().cost;
            }
        }


    }
}
