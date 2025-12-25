using FixedPoints;
using Lodis.Gameplay;
using Lodis.GridScripts;
using Lodis.Movement;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Types;
using UnityEngine;

public class Secur_TBehaviour : SimulationBehaviour
{
    [Header("Shooting")]
    [SerializeField] protected Fixed32 _activeTime = 10;
    [SerializeField] private int fireRange = 1;
    [SerializeField] protected Fixed32 _delayBetweenShots;
    [SerializeField] protected Fixed32 _cooldownTime;
    [SerializeField] protected ProjectileSpawnerBehaviour _projectileSpawner;

    [Header("Following")]
    [SerializeField] private Fixed32 followSpeed;
    [SerializeField] protected Fixed32 _followOffsetX;
    [SerializeField] protected Fixed32 _followOffsetY;

    [Header("Visuals")]
    [SerializeField] protected GameObject _chargingEffect;
    [SerializeField] protected ColorManagerBehaviour _colorManager;
    [SerializeField] protected GridTrackerBehaviour _gridTracker;

    private EntityDataBehaviour _owner;
    private GridMovementBehaviour _ownerMovement;
    private EntityDataBehaviour _target;
    private FixedTimeAction _activeTimer;
    private FixedTimeAction _currentShotTimer;
    private FixedTimeAction _cooldownTimer;
    private Fixed32 _fireDistance;
    private Fixed32 _projectileSpeed;
    private HitColliderData _projectileData;
    private Fixed32 _negOffsetX;

    private bool _firing;
    private bool _onCooldown;
    private bool _lockedOn;

    public int FireRange { get => fireRange; set => fireRange = value; }
    public bool LookAtTarget { get; set; } = true;
    public Fixed32 FollowSpeed { get => followSpeed; set => followSpeed = value; }

    public override void Deserialize(BinaryReader br)
    {
        _firing = br.ReadBoolean();
        _onCooldown = br.ReadBoolean();
    }

    public override void Serialize(BinaryWriter bw)
    {
        bw.Write(_firing);
        bw.Write(_onCooldown);
    }

    public override void Begin()
    {
        base.Begin();

        _fireDistance = (GridBehaviour.Instance.FixedPanelScale.X + GridBehaviour.Instance.FixedPanelSpacingX) * FireRange;
        _negOffsetX = -_followOffsetX;

        _gridTracker.XRange = FireRange;
    }

    public override void Tick(Fixed32 dt)
    {
        base.Tick(dt);

        //Follow owner
        if (followSpeed > 0)
        {
            Fixed32 currentOffsetX = _followOffsetX;

            if (_ownerMovement.Position.X == 0 || _ownerMovement.Position.X == GridBehaviour.Instance.Dimensions.x - 1)
            {
                currentOffsetX = _negOffsetX;
            }

            FVector3 followPosition = _owner.FixedTransform.WorldPosition +
                new FVector3(currentOffsetX * _ownerMovement.GetAlignmentX(), _followOffsetY, 0);

            if (FVector3.Distance(Entity.FixedTransform.WorldPosition, followPosition) > new Fixed32(6553))
            {
                FVector3 direction = (followPosition - Entity.FixedTransform.WorldPosition).GetNormalized();
                Entity.FixedTransform.WorldPosition += direction * FollowSpeed * dt;
            }
        }

        // Fire at target if in range
        Fixed32 distanceToTarget = FVector3.Distance(_target.FixedTransform.WorldPosition, _owner.FixedTransform.WorldPosition);

        if (_lockedOn && LookAtTarget)
        {
            FixedTransform.LookAt(_target.FixedTransform.WorldPosition);
        }
        else
        {
            FixedTransform.Forward = _owner.FixedTransform.Forward;
        }

        if (distanceToTarget <= _fireDistance && _target.FixedTransform.WorldPosition.Z == FixedTransform.WorldPosition.Z)
        {
            if (!_firing && !_onCooldown)
            {
                _firing = true;
                HandleFireShots();
            }
        }
    }

    public void Initialize(EntityDataBehaviour owner, EntityDataBehaviour target, HitColliderData colliderInfo, Fixed32 projectileSpeed)
    {
        _owner = owner;

        ResetTimer();

        _target = target;
        _projectileData = colliderInfo;
        _projectileSpeed = projectileSpeed;
        _projectileSpawner.Owner = owner;

        _ownerMovement = owner.GetComponent<GridMovementBehaviour>();

        int alignment = (int)_owner.GetComponent<GridMovementBehaviour>().Alignment;

        _colorManager.SetColors(alignment);
    }

    public void ResetTimer()
    {
        if (_activeTimer != null)
        {
            _activeTimer.Reset();
        }
        else
        {
            _activeTimer = FixedPointTimer.StartNewTimedAction(Deactivate, _activeTime);
        }
    }

    private void HandleFireShots()
    {
        _lockedOn = true;

        _activeTimer.Pause();

        _chargingEffect.SetActive(true);

        _currentShotTimer = FixedPointTimer.StartNewTimedAction(() =>
        {
            FireShot(0);

            _chargingEffect.SetActive(false);


        }, _delayBetweenShots);
    }

    private void FireShot(int index)
    {
        _projectileSpawner.FireProjectile(_projectileSpeed, _projectileData);

        if (index == 0)
        {
            _currentShotTimer = FixedPointTimer.StartNewTimedAction(() =>
            {
                FireShot(1);
                _firing = false;
            }, _delayBetweenShots);
        }
        else if (index == 1)
        {
            _activeTimer.Resume();
            _onCooldown = true;
            _lockedOn = false;
            _currentShotTimer = FixedPointTimer.StartNewTimedAction(() => _onCooldown = false, _cooldownTime);
        }
    }

    public void Deactivate()
    {
        GridGame.RemoveEntityFromGame(Entity);
    }

}
