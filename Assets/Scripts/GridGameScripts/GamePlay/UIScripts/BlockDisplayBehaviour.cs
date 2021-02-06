using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GridGame.VariableScripts;

namespace GridGame
{
    public class BlockDisplayBehaviour : MonoBehaviour {
        [SerializeField]
        private GameObjectList _blocks;
        private GameObject currentBlock;
        [SerializeField]
        private float _blockScaleSize;

        public GameObjectList Blocks
        {
            get
            {
                return _blocks;
            }
        }

        public GameObject CurrentBlock
        {
            get
            {
                return currentBlock;
            }
        }

        public void DisplayBlock(GameObject block)
        {
            if (currentBlock != null)
            {
                GameObject temp = currentBlock;
                Destroy(temp);
            }
            currentBlock = Instantiate(block);
            currentBlock.transform.position = transform.position;
            currentBlock.GetComponent<BlockBehaviour>().ActivateDisplayMode();
            currentBlock.transform.localScale *= _blockScaleSize;
        }

        public void ClearDisplay()
        {
            if (currentBlock != null)
            {
                GameObject temp = currentBlock;
                Destroy(temp);
            }
        }


        // Use this for initialization
        public void DisplayBlock(int index)
        {
            if(currentBlock != null)
            {
                GameObject temp = currentBlock;
                Destroy(temp);
            }
            currentBlock = Instantiate(_blocks[index]);
            currentBlock.transform.position = transform.position;
            currentBlock.GetComponent<BlockBehaviour>().ActivateDisplayMode();
            currentBlock.transform.localScale *= _blockScaleSize;
        }
    }
}


