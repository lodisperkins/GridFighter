using FixedPoints;
using Lodis.FX;
using Lodis.Gameplay;
using Lodis.Movement;
using Lodis.ScriptableObjects;
using Lodis.Sound;
using Lodis.UI;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Types;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class SurgeMeterBehaviour : SimulationBehaviour
{
    [SerializeField] private IntVariable _playerID;
    [SerializeField] private IntVariable _surgeMeterMax;
    [SerializeField] private int _surgeMeterCurrent;
    [SerializeField] private ComboCounterBehaviour _comboCounter;
    [SerializeField] private Fixed32 _hotSpotPercentThreshold;
    [SerializeField] private GameObject _visualRoot;
    [SerializeField] private Slider _surgeMeterSlider;
    [SerializeField] private UnityEvent _onEnterHotSpotRange;
    [SerializeField] private UnityEvent _onExitHotSpotRange;

    private MovesetBehaviour _ownerMoveset;
    private KnockbackBehaviour _opponentKnockback;
    private bool _wasInHotSpotRange = false;
    private bool _applyingHotSpotBonus = false;

    private float _lerpSpeed = 8f;
    private float _targetSurgeMeterValue;

    public int HotSpotMinimum => _surgeMeterMax - (int)(_surgeMeterMax.Value * _hotSpotPercentThreshold);
    public bool InHotSpotRange => _surgeMeterCurrent >= HotSpotMinimum && _surgeMeterCurrent < _surgeMeterMax;
    public bool SurgeMeterFull => _surgeMeterCurrent >= _surgeMeterMax;

    public override string LogName => "SurgeMeterBehaviour";

    public override void Deserialize(BinaryReader br)
    {
        _surgeMeterCurrent = br.ReadInt32();
        _wasInHotSpotRange = br.ReadBoolean();
        _applyingHotSpotBonus = br.ReadBoolean();
    }

    public override void Serialize(BinaryWriter bw)
    {
        bw.Write(_surgeMeterCurrent);
        bw.Write(_wasInHotSpotRange);
        bw.Write(_applyingHotSpotBonus);
    }

    /// <summary>
    /// Hashes the serialized surge meter state so meter-specific divergences can be
    /// identified during sync-test investigation.
    /// </summary>
    protected override string[] GetLogItems()
    {
        return new string[]
        {
            $"Surge Meter Current: {_surgeMeterCurrent}",
            $"Was In Hot Spot Range: {_wasInHotSpotRange}",
            $"Applying Hot Spot Bonus: {_applyingHotSpotBonus}"
        };
    }

    public override void Begin()
    {
        base.Begin();

        _ownerMoveset = BlackBoardBehaviour.Instance.GetPlayerFromID(_playerID).GetComponent<MovesetBehaviour>();
        _opponentKnockback = BlackBoardBehaviour.Instance.GetOpponentForPlayer(_playerID).GetComponent<KnockbackBehaviour>();
        _opponentKnockback.AddOnTakeDamageAction(OnOpponentTakeDamage);

        if (_surgeMeterSlider != null)
        {
            _surgeMeterSlider.maxValue = _surgeMeterMax.Value;
            _surgeMeterSlider.value = _surgeMeterCurrent;
            _targetSurgeMeterValue = _surgeMeterCurrent;
        }
        _wasInHotSpotRange = InHotSpotRange;

        if (_playerID == 0)
        {
            BlackBoardBehaviour.Instance.Player1SurgeMeter = this;
        }
        else if (_playerID == 1)
        {
            BlackBoardBehaviour.Instance.Player2SurgeMeter = this;
        }
    }

    private void UpdateSurgeMeterUI()
    {
        if (_surgeMeterSlider != null)
        {
            _targetSurgeMeterValue = Mathf.Clamp(_surgeMeterCurrent, 0, _surgeMeterMax.Value);
        }
    }

    private void CheckHotSpotRange()
    {
        bool inHotSpot = InHotSpotRange;
        if (inHotSpot && !_wasInHotSpotRange)
        {
            _onEnterHotSpotRange?.Invoke();
        }
        else if (!inHotSpot && _wasInHotSpotRange)
        {
            _onExitHotSpotRange?.Invoke();
        }
        _wasInHotSpotRange = inHotSpot;
    }

    private void OnOpponentTakeDamage()
    {
        if (_comboCounter.PlayerComboLevel < 1)
            return;

        if (_surgeMeterCurrent >= _surgeMeterMax.Value && !_applyingHotSpotBonus)
        {
            _opponentKnockback.SetIntagibilityByCondition(c => _opponentKnockback.CheckIfIdle());
            return;
        }
        else if (_applyingHotSpotBonus)
        {
            _applyingHotSpotBonus = false;
            return;
        }

        if (_opponentKnockback.LastCollider == null)
        {
            Debug.LogWarning("Couldnt find last ability enemy was hit with for player " + _playerID.Value.ToString());
            return;
        }

        _visualRoot.SetActive(true);

        HitColliderData hitColliderData = _opponentKnockback.LastCollider.ColliderInfo;


        if (hitColliderData.IsEnder && InHotSpotRange)
        {
            _surgeMeterCurrent = _surgeMeterMax;

            HitColliderData addtionalHitData = hitColliderData.ScaleStats(Fixed32.One + Fixed32.One);
            
            int sign = addtionalHitData.HitAngle > Fixed32.One + Fixed32.PointFive ? -1 : 1;

            addtionalHitData.HitAngle += Fixed32.PointTwo * sign;

            FixedPointTimer.StartNewTimedAction(() => _opponentKnockback.TakeDamage(addtionalHitData, _ownerMoveset.Entity.Data), GridGame.FixedTimeStep);
            FXManagerBehaviour.Instance.StartSurgeStrikeVisual(_playerID);

            _applyingHotSpotBonus = true;
            UpdateSurgeMeterUI();
            CheckHotSpotRange();

            return;
        }
        _surgeMeterCurrent += hitColliderData.SurgeMeterValue;

        UpdateSurgeMeterUI();
        CheckHotSpotRange();

        if (_surgeMeterCurrent >= _surgeMeterMax.Value)
        {
            _opponentKnockback.SetIntagibilityByCondition(c => _opponentKnockback.CheckIfIdle());
            return;
        }
    }

    private void Update()
    {
        if (_surgeMeterSlider != null)
        {
            _surgeMeterSlider.value = Mathf.Lerp(_surgeMeterSlider.value, _targetSurgeMeterValue, Time.deltaTime * _lerpSpeed);
        }
    }

    public override void Tick(Fixed32 dt)
    {
        base.Tick(dt);

        if (_opponentKnockback.CheckIfIdle())
        {
            _surgeMeterCurrent = 0;
            _visualRoot?.SetActive(false);
            UpdateSurgeMeterUI();
        }
    }
}
