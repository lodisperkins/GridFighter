using System.Collections;
using System.Collections.Generic;
using GridGame;
using UnityEngine;
using Event = GridGame.Event;
using GridGame.VariableScripts;
public class PlayerAttackBehaviour : MonoBehaviour
{

	[SerializeField]
	private GunBehaviour _gun;
	private float _time;
	[SerializeField]
	private float _hitboxActiveTime;
	private bool _meleeHitboxActive;
    [SerializeField]
    private GameObject _weapon;
	[SerializeField]
	private GameObject _weaponHitbox;
    [SerializeField]
    private Animator animator;
	private GridGame.Movement.PlayerMovementBehaviour player;
    private RaycastHit _interactionRay;
    private GameObject _currentBlock;
	[SerializeField] private Event _onInteractPressed;
    [SerializeField]
    private GameObject normalBullet;
    [SerializeField]
    private GameObject chargeBullet;
    private float chargeTimer;
    [SerializeField]
    private float timeUntilCharged;
    [SerializeField]
    private bool charged;
    [SerializeField]
    private GameObject chargeParticles;
    [SerializeField]
    private AudioSource chargingAudio;
    [SerializeField]
    private string shootAxis;
    private string secondaryShootAxis;
    private bool charging;
    private GameObject tempSource;
    public float weaponUseAmount;
    private GridGame.Interfaces.IUpgradable secondaryWeapon;
    private bool willInteract;
    [SerializeField] private SliderBehaviour sliderScript;
    [SerializeField] private IntVariable secondAbilityUseAmount;
    private PlayerSpawnBehaviour spawnScript;
    public bool secondaryInputCanBeHeld;

    public IntVariable SecondAbilityUseAmount
    {
        get
        {
            return secondAbilityUseAmount;
        }
    }

    // Use this for initialization
    void Start ()
	{
		player = GetComponent<GridGame.Movement.PlayerMovementBehaviour>();
        _interactionRay = new RaycastHit();
        charged = false;
        spawnScript = GetComponent<PlayerSpawnBehaviour>();
        InitializeBlackBoard();
	}
    public void InitializeBlackBoard()
    {
        SecondAbilityUseAmount.Val = 0;
        if (name == "Player1")
        {
            BlackBoard.altAmmoAmountP1 = SecondAbilityUseAmount;
            secondaryShootAxis = "Special1";
        }
        else
        {
            BlackBoard.altAmmoAmountP2 = SecondAbilityUseAmount;
            secondaryShootAxis = "Special2";
        }
    }
    public void TryInteract()
    {
        willInteract = true;
    }
	public void Interact()
	{
		if (player.canMove && willInteract)
		{
            int layerMask = 1 << 9;
            if(Physics.Raycast(transform.position,transform.forward,out _interactionRay,2,layerMask))
            {
                _currentBlock = _interactionRay.transform.gameObject;
                if(_currentBlock.CompareTag("Panel"))
                {
                    willInteract = false;
                    return;
                }
                _currentBlock.SendMessage("ActivateSpecialAction",name);
            }
		}
        willInteract = false;
    }
    public void EquipSecondaryWeapon()
    {
        willInteract = false;
        if (player.canMove)
        {
            int layerMask = 1 << 9;
            if (Physics.Raycast(transform.position, transform.forward, out _interactionRay, 2, layerMask) && spawnScript.CheckMaterial(30) && secondaryWeapon ==null)
            {
                _currentBlock = _interactionRay.transform.gameObject;
                if (_currentBlock.CompareTag("Panel"))
                {
                    return;
                }
                spawnScript.BuyItem(30);
                _currentBlock.SendMessage("UpgradePlayer",this);
                sliderScript.gameObject.SetActive(true);
                sliderScript.Bar.maxValue = weaponUseAmount;
                sliderScript.fillImage.color = secondaryWeapon.displayColor;
            }
        }
    }
    private void ChargeGun()
    {
        charged = true;
        chargeParticles.SetActive(true);
    }
	public void FireGun()
	{
		if (player.canMove)
		{
            _gun.ChangeBullet(normalBullet);
            _gun.FireBullet();
		}
	}
    public void FireChargeGun()
    {
        if(charged)
        {
            _gun.ChangeBullet(chargeBullet);
            _gun.FireBullet();
            charged = false;
            chargeParticles.SetActive(false);
        }
    }
    public void SetSecondaryWeapon(GridGame.Interfaces.IUpgradable weapon, int useAmount)
    {
        secondaryWeapon = weapon;
        SecondAbilityUseAmount.Val  = useAmount;
    }
    public void UseSecondaryWeapon()
    {
        if (secondaryWeapon != null)
        {
            secondaryInputCanBeHeld = secondaryWeapon.CanBeHeld;

            if (secondaryInputCanBeHeld)
            {
                return;
            }
            if (SecondAbilityUseAmount.Val > 0)
            {
                secondaryWeapon.ActivatePowerUp();
                SecondAbilityUseAmount.Val--;
            }
            
        }
    }
    public void DecreaseAmmuntion(int amount)
    {
        SecondAbilityUseAmount.Val -= amount;
    }
    public void IncreaseAmmuntion(int amount)
    {
        SecondAbilityUseAmount.Val += amount;
    }
    private void OnTriggerEnter(Collider other)
    {
        if(secondaryWeapon != null)
        {
            secondaryWeapon.ResolveCollision(other.gameObject); 
        }
    }
    private void OnCollisionEnter(Collision collision)
    {
        if (secondaryWeapon != null)
        {
            secondaryWeapon.ResolveCollision(collision.gameObject);
        }
    }
    private void FixedUpdate()
    {
        if(secondaryInputCanBeHeld && Input.GetButton(secondaryShootAxis))
        {
            if (SecondAbilityUseAmount.Val > 0)
            {
                secondaryWeapon.ActivatePowerUp();
                SecondAbilityUseAmount.Val--;
            }
            else if (secondaryWeapon != null)
            {
                sliderScript.gameObject.SetActive(false);
                secondaryWeapon.DetachFromPlayer();
                secondaryWeapon = null;
            }
        }
        else if(secondaryInputCanBeHeld)
        {
            secondaryWeapon.DeactivatePowerUp();
        }
    }
    // Update is called once per frame
    void Update () {
        if(Input.GetButtonDown(shootAxis))
        {
            tempSource = Instantiate(chargingAudio).gameObject;
        }
        else if(Input.GetButtonUp(shootAxis))
        {
            Destroy(tempSource);
        }
        if(SecondAbilityUseAmount.Val <= 0 && secondaryWeapon != null)
        {
            sliderScript.gameObject.SetActive(false);
            secondaryWeapon.DetachFromPlayer();
            secondaryWeapon = null;
        }
    }
}
