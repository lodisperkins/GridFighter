using GridGame.Movement;
using System.Collections;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Serialization;

namespace GridGame.GamePlay.GridScripts
{
    public class PanelBehaviour : MonoBehaviour
    {

        //The position of the panel on the grid
        [SerializeField]
        public Vector2 Position;
        //Is true if the panel has been selected or highlighted by the player
        public string Owner;
        //Is true if a gameobjects position has been set to be the same as the panels
        public bool Occupied;
        private GameObject _occupier;
        //the color the panel is set to by default in the editor
        private Color DefaultColor;
        //the current color the panel is being set to
        private Color _currentColor;
        //the color the panel should be highlighted when selected
        public Color SelectionColor;
        //whether or not the panel is currently being selected
        private bool _selected;
        private bool triggerEmpty;
        //the reference tho the players material
        private Material _player1Mat;
        private Material _player2Mat;
        public float G;
        public float F;
        public PanelBehaviour previousPanel;
        [SerializeField]
        private int _blockLimit;
        private float xAbsolute;
        private float yAbsolute;
        private float zAbsolute;
        [SerializeField]
        private bool _isBroken;
        [SerializeField] private BlockBehaviour currentBlock;
        private RaycastHit _detectionRay;
        public bool IsBroken
        {
            get
            {
                return _isBroken;
            }
        }
        [SerializeField] private GameObject _explosionParticles;
        [FormerlySerializedAs("_blockCounter")] public int blockCounter;
        public int BlockLimit
        {
            get { return _blockLimit; }
        }

        [FormerlySerializedAs("_BlockCapacityReached")] public bool BlockCapacityReached;
        private bool _attackHighlight;
        private float timer;
        private bool TimerSet;
        //the current material on the panel
        private Material _panelMat;
        private void Start()
        {
            _currentColor = new Color();
            blockCounter = 0;
            _blockLimit = 3;
            G = 1;
            TimerSet = false;
            _panelMat = GetComponent<MeshRenderer>().material;
            
        }

        private void OnEnable()
        {
            _panelMat = GetComponent<MeshRenderer>().material;
        }

        private void Awake()
        {
            _panelMat = GetComponent<MeshRenderer>().material;
        }

        public bool CheckPanelCapacity(BlockBehaviour block)
        {
            return blockCounter + block.BlockWeightVal > _blockLimit;
        }
        //one set calls the highlight panel fucntion with current status of the _selected variable
        public bool Selected
        {
            get
            {
                return _selected;
            }
            set
            {
                HighlightPanel(value);
                _selected = value;

            }

        }

        public float XAbsolute
        {
            get
            {
                return xAbsolute;
            }
        }

        public float YAbsolute
        {
            get
            {
                return yAbsolute;
            }
        }

        public float ZAbsolute
        {
            get
            {
                return zAbsolute;
            }
        }

        public BlockBehaviour CurrentBlock
        {
            get
            {
                return currentBlock;
            }
        }

        public GameObject Occupier
        {
            get
            {
                return _occupier;
            }
        }

        private bool CheckIfOccupied()
        {
            if (Physics.Raycast(transform.position, transform.up, out _detectionRay))
            {
                _occupier = _detectionRay.transform.gameObject;
                switch (_occupier.tag)
                {
                    case ("Block"):
                        {
                            BlockBehaviour blockScript = _occupier.GetComponent<BlockBehaviour>();
                            if(blockScript != null)
                            {
                                currentBlock = blockScript;
                            }
                            return true;
                        }
                    case ("Player"):
                        {
                            return true;
                        }
                }
            }
            else if(currentBlock == null)
            {
                _occupier = null;
                blockCounter = 0;
            }
            
            return false;
        }
        //highlights the panel when a bullet passes through it
        private void OnTriggerStay(Collider other)
        {
            if (other.CompareTag("Projectile") || other.CompareTag("Hazard"))
            {
                _attackHighlight = true;
                UpdateColor();
            }
            switch (other.tag)
            {
                case ("Block"):
                    {
                        BlockBehaviour blockScript = other.GetComponent<BlockBehaviour>();
                        GridPhysicsBehaviour gridPhysicsScript = other.GetComponent<GridPhysicsBehaviour>();
                        if (blockScript != null)
                        {
                            currentBlock = blockScript;
                            blockCounter = currentBlock.CurrentLevel;
                        }
                        Occupied = true;
                        break;
                    }
                case ("Player"):
                    {
                        Occupied = true;
                        break;
                    }
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if(other.CompareTag("Block"))
            {
                currentBlock = other.GetComponent<BlockBehaviour>();
            }
        }
        private void OnTriggerExit(Collider other)
        {
            if(other.CompareTag("Block"))
            {
               if(currentBlock == other.GetComponent<BlockBehaviour>())
               {
                    currentBlock = null;
               }
            }
            _attackHighlight = false;
            UpdateColor();
        }
        //initializes the panel with the players materials and colors itself accordingly
        public void Init(Material player1Mat, Material player2Mat)
        {
            _player1Mat = player1Mat;
            _player2Mat = player2Mat;
            UpdateColor();
        }
        //Highlights the panel if the value passed in is true
        private void HighlightPanel(bool isSelected)
        {
            if (isSelected)
            {
                _panelMat.color = SelectionColor;
                return;
            }
            UpdateColor();
        }
        //(not working)changes the panel back to the owners color after a certain amount of time
        private void RevertToOwnerColor(float RevertTime)
        {
            if (_attackHighlight && TimerSet == false)
            {
                timer = Time.time + RevertTime;
                TimerSet = true;
                return;
            }
            else if (Time.time >= timer && _attackHighlight)
            {
                _attackHighlight = false;
                TimerSet = false;
                UpdateColor();
            }
        }
        private void FixedUpdate()
        {
            if (currentBlock == null)
            {
                blockCounter = 0;
            }

            Occupied = false;
        }
        //Updates the coor of the panel to be that of its current owner
        public void UpdateColor()
        {
            _panelMat = GetComponent<MeshRenderer>().material;
            if(_attackHighlight)
             {
                 _panelMat.color = Color.grey;
                 _currentColor = Color.grey;
             }
            else if (Owner == "Player1")
            {
                if (_player1Mat == null)
                {
                    _panelMat.color = Color.black;
                    _currentColor = Color.red;
                }
                else
                {
                    _panelMat.color = _player1Mat.color;
                    _currentColor = _player1Mat.color;
                }
            }
            else if (Owner == "Player2")
            {
                if (_player2Mat == null)
                {
                    _panelMat.color = Color.black;
                    _currentColor = Color.cyan;
                }
                else
                {
                    _panelMat.color = _player2Mat.color;
                    _currentColor = _player2Mat.color;
                }
            }
        }
        public void BreakPanel(float time)
        {
            if (_isBroken == false && Occupied == false)
            {
                _isBroken = true;
                StartCoroutine(Break(time));
            }
        }
        public IEnumerator Break(float time)
        {
            MeshRenderer mesh = GetComponent<MeshRenderer>();
            PlayParticleSystems(.5f);
            mesh.enabled = false;
            Occupied = true;
            yield return new WaitForSeconds(time);
            for(int i = 0; i<= 5; i++)
            {
                mesh.enabled = true;
                yield return new WaitForSeconds(0.01f);
                mesh.enabled = false;
                yield return new WaitForSeconds(0.01f);
            }
            mesh.enabled = true;
            Occupied = false;
            _isBroken = false;
            
        }
        //changes the current owner of the panel to be the name of the item passsed in
        public void UpdateOwner(string newOwner)
        {
            Owner = newOwner;
            if (_selected)
            {
                return;
            }
            else
            {
                RevertToOwnerColor(.2f);
                UpdateColor();
            }
        }
        public void PlayParticleSystems(float duration)
        {
            var tempPs = Instantiate(_explosionParticles,transform.position,transform.rotation);
           
            Destroy(tempPs, duration);
        }
        public void DestroyPanel()
        {
            GameObject temp = gameObject;
            PlayParticleSystems(2);
            Destroy(temp,1.2f);
        }
        private void Update()
        {
            BlockCapacityReached = blockCounter == BlockLimit;
            Occupied = CheckIfOccupied();
            if(IsBroken)
            {
                BlockCapacityReached = true;
                Occupied = true;
            }
            xAbsolute = transform.position.x;
            yAbsolute = transform.position.y;
            zAbsolute = transform.position.z;
            UpdateOwner(Owner);
        }
    }
    
}
