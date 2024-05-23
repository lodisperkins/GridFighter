using Lodis.Gameplay;
using Lodis.ScriptableObjects;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class EnergyBarBehaviour : MonoBehaviour
{
    [SerializeField]
    private IntVariable _playerID;

    private MovesetBehaviour _target;
    [SerializeField]
    private Image _backgroundImage;
    [SerializeField]
    private Image _fill;
    [SerializeField]
    private Slider _slider;
    [SerializeField]
    private RectTransform _meterTickRef;
    [SerializeField]
    private float _meterTickWidth;
    [SerializeField]
    private Image _energyTextCounterImage;
    [SerializeField]
    private Text _energyTextCounter;
    [SerializeField]
    private RectTransform _fillAreaTransform;
    [SerializeField]
    private FloatVariable _maxValue;
    [SerializeField]
    private float _meterTickHeight;
    [SerializeField]
    private Vector2 _meterTickOffset;
    [SerializeField]
    private BurstMeterBehaviour _burstMeter;

    public MovesetBehaviour Target { get => _target; set => _target = value; }
    public FloatVariable MaxValue { get => _maxValue; }


    // Start is called before the first frame update
    void Start()
    {
        GameObject player = BlackBoardBehaviour.Instance.GetPlayerFromID(_playerID);

        if (!player) return;

        Target = player.GetComponent<MovesetBehaviour>();
        _burstMeter.Init(Target);
        _slider = GetComponent<Slider>();
        _slider.maxValue = MaxValue.Value;
        _backgroundImage.color = BlackBoardBehaviour.Instance.GetPlayerColorByID(_playerID);
        //CreateMeterTicks();
    }

    /// <summary>
    /// Generates and evenly spaces the ticks on the meter.
    /// Used to scale the amount of ticks on the meter so that they are
    /// always matches the current maximum energy count.
    /// </summary>
    private void CreateMeterTicks()
    {
        //Get the x position at the front end of the rect
        float startXPos = _fillAreaTransform.anchoredPosition.x - (_fillAreaTransform.rect.width / 2);
        //Get the amount of space that should be between each tick
        float xOffset = _fillAreaTransform.rect.width / MaxValue.Value;
        //Set the current x position to be the first tick position
        float currentXPos = startXPos +  xOffset;

        //Loop until we reach the maximum amount of ticks possible
        for (int i = 0; i < (int)MaxValue.Value - 1; i++)
        {
            //Instantiate a new meter tick and store its rect transform
            RectTransform meterTick = Instantiate(_meterTickRef, _fillAreaTransform.parent);
            meterTick.localScale = Vector2.one;
            meterTick.anchorMin = _fillAreaTransform.anchorMin;
            meterTick.anchorMax = _fillAreaTransform.anchorMax;

            //Change the meter ticks position to the current x position
            meterTick.rect.Set(meterTick.rect.x, meterTick.rect.y, _meterTickWidth, _meterTickHeight);
            meterTick.anchoredPosition = new Vector2(currentXPos, meterTick.anchoredPosition.y) + _meterTickOffset;
            //Move the x position to the position of the next tick
            currentXPos += xOffset;
        }

    }

    // Update is called once per frame
    void Update()
    {
        if (_target != null)
            _slider.DOValue(_target.Energy, 0.1f);
        else return;

        _energyTextCounter.color = BlackBoardBehaviour.Instance.AbilityCostColors[(int)_target.Energy];

        int currentEnergy = (int)_target.Energy;

        if (currentEnergy == 0)
            _energyTextCounter.color = new Vector4(_energyTextCounter.color.r, _energyTextCounter.color.g, _energyTextCounter.color.b, 0.5f);
        else
            _energyTextCounter.color = new Vector4(_energyTextCounter.color.r, _energyTextCounter.color.g, _energyTextCounter.color.b, 1);

        _energyTextCounter.text = currentEnergy.ToString();
        _fill.color = BlackBoardBehaviour.Instance.AbilityCostColors[(int)_target.Energy];
        _energyTextCounterImage.color = _fill.color;
    }
}
