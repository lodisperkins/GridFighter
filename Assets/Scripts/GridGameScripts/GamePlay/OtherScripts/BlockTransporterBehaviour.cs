using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
namespace GridGame.GamePlay
{
    public class BlockTransporterBehaviour : MonoBehaviour
    {
        public Vector3 moveDirection;
        public Transform ownerTransform;
        public PlayerSpawnBehaviour _playerSpawnScript;
        public string owner;
        public float flyForce;
        public SeekBehaviour seekScript;
        private bool _deployed;
        public Rigidbody rigidbody;
        private GridScripts.PanelBehaviour _currentPanel;
        private UnityAction _explodeOnImpact;
        private UnityAction _spawnBlockOnImpact;
        [SerializeField]
        private BlockDisplayBehaviour blockDisplay;
        private int _currentBlockIndex;
        private GameObject displayBlock;
        private bool _blockDropped;
        [SerializeField]
        private float _decentSpeed;
        [SerializeField]
        private float _deployDistance;
        private bool _placing;
        [SerializeField]
        private float _spawnDelay;
        private GameObject _spawnedBlock;
        public bool Deployed
        {
            get
            {
                return _deployed;
            }
        }

        // Use this for initialization
        void Start()
        {
            seekScript.Init(ownerTransform.position, rigidbody.velocity, 5, 5);
            _explodeOnImpact += ExplodePanel;
            _explodeOnImpact += Destroy;
            _spawnBlockOnImpact += SpawnBlock;
            _spawnBlockOnImpact += DestroyDisplayBlock;
            _currentBlockIndex = _playerSpawnScript.CurrentIndex;
            blockDisplay.DisplayBlock(_currentBlockIndex);
            displayBlock = blockDisplay.Blocks[_currentBlockIndex];
        }
        public void Idle()
        {
            seekScript.SeekEnabled = false;
        }
        public void ExplodePanel()
        {
            BlackBoard.grid.ExplodePanel(_currentPanel,true,5);
        }

        private IEnumerator Spawn()
        {
            yield return new WaitForSeconds(_spawnDelay);

            var position = new Vector3(_currentPanel.gameObject.transform.position.x, .6f, _currentPanel.transform.position.z);
            _spawnedBlock = Instantiate(blockDisplay.Blocks[_currentBlockIndex], position, _playerSpawnScript.Block_rotation);
            BlockBehaviour copyScript = _spawnedBlock.GetComponent<BlockBehaviour>();
            copyScript.currentPanel = _currentPanel.gameObject;
            copyScript.owner = _playerSpawnScript.gameObject;
            copyScript.InitializeBlock();
            copyScript.currentPanel.GetComponent<GridScripts.PanelBehaviour>().blockCounter -= 1;
            _spawnedBlock.GetComponent<Collider>().isTrigger = true;
            Destroy(gameObject);
        }

        public void SpawnBlock()
        {
            StartCoroutine(Spawn());
        }
        public void DestroyDisplayBlock()
        {
            Destroy(blockDisplay.CurrentBlock,_spawnDelay);
        }

        public void Destroy()
        {
            Destroy(gameObject);
            Destroy(blockDisplay.CurrentBlock);
        }
        public void Destroy(float time)
        {
            Destroy(gameObject, time);
            Destroy(blockDisplay.CurrentBlock, time);
        }
        private void UpdateCurrentPanel()
        {
            RaycastHit hit = new RaycastHit();
            int layerMask = 1 << 10;
            if (Physics.Raycast(transform.position, Vector3.down, out hit, 10,layerMask))
            {
                _currentPanel = hit.collider.GetComponent<GridScripts.PanelBehaviour>();
            }
        }
        public void Deploy()
        {
            seekScript.SeekEnabled = false;
            transform.position = ownerTransform.position;
            rigidbody.AddForce(moveDirection * flyForce, ForceMode.Acceleration);
            _deployed = true;
        }

        public void PlaceBlock()
        {
            if (!_currentPanel)
            {
                return;
            }
            _placing = true;
            blockDisplay.CurrentBlock.AddComponent<SeekBehaviour>();
            rigidbody.velocity = Vector3.zero;
            seekScript.Init(_currentPanel.transform.position, rigidbody.velocity, _decentSpeed, _deployDistance);
            seekScript.SeekEnabled = true;
            seekScript.onTargetReached.AddListener(_spawnBlockOnImpact);
        }
        void CheckPosition()
        {
            if (transform.position.x >= 1 || transform.position.x <= -7 || transform.position.z >= 29 || transform.position.z <= 8)
            {
                DestroyDisplayBlock();
                GameObject temp = gameObject;
                Destroy(temp);
            }
        }
        // Update is called once per frame
        void Update()
        {
            UpdateCurrentPanel();
            if(_currentBlockIndex != _playerSpawnScript.CurrentIndex && !Deployed)
            {
                _currentBlockIndex = _playerSpawnScript.CurrentIndex;
                blockDisplay.DisplayBlock(_currentBlockIndex);
                displayBlock = blockDisplay.Blocks[_currentBlockIndex];
            }
            if(blockDisplay.CurrentBlock && ! _blockDropped)
            {
                blockDisplay.CurrentBlock.transform.position = blockDisplay.transform.position;
            }
            CheckPosition();
            if(seekScript && ownerTransform && !Deployed)
            {
                seekScript.SetTarget(ownerTransform.position, 5);
            }

            if (_placing)
            {
                if ((_currentPanel.Occupied || _currentPanel.IsBroken) && !_blockDropped && _currentPanel.Occupier != _spawnedBlock)
                {
                    SeekBehaviour blockSeekScript = blockDisplay.CurrentBlock.GetComponent<SeekBehaviour>();
                    blockSeekScript.Init(BlackBoard.grid.GetPanelFromGlobalList(_currentPanel.Position).transform.position, blockDisplay.CurrentBlock.GetComponent<Rigidbody>().velocity, 10, 2, true);
                    blockSeekScript.onTargetReached.AddListener(_explodeOnImpact);
                    blockSeekScript.SeekEnabled = true;

                    GameObject temp = gameObject;
                    Destroy(temp, .5f);
                    _blockDropped = true;
                }
            }

            
        }
    }
}
