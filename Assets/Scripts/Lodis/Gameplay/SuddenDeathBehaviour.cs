using FixedPoints;
using Lodis.Gameplay;
using Lodis.GridScripts;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Types;
using UnityEngine;

public class SuddenDeathBehaviour : SimulationBehaviour
{
    [SerializeField] private Fixed32 _firstExplosionDelay;
    [SerializeField] private Fixed32 _secondExplosionDelay;
    [SerializeField] private Fixed32 _thirdExplosionDelay;
    [SerializeField] private Fixed32 _moveBarrierDelay;
    [SerializeField] private GameObject[] _objectsToDisable;
    [SerializeField] private GameObject _deathbarrierL;
    [SerializeField] private GameObject _deathbarrierR;
    [SerializeField] private EntityDataBehaviour _lhsWinCollider;
    [SerializeField] private EntityDataBehaviour _rhsWinCollider;
    [SerializeField] private Fixed32 _debugVal;
    [SerializeField] private GameObject _suddenDeathScreenEffect;

    private FixedTimeAction _currentTimer;
    private FixedTimeAction _currentBarrierMoveTimer;
    private int _currentX;
    private bool _hasStarted;
    private Fixed32 _winMovementScale;
    private FVector3 _lhsOriginalPos;
    private FVector3 _rhsOriginalPos;

    public override void Begin()
    {
        base.Begin();

        _lhsOriginalPos = _lhsWinCollider.FixedTransform.WorldPosition;
        _rhsOriginalPos = _rhsWinCollider.FixedTransform.WorldPosition;
        GridBehaviour grid = GridBehaviour.Instance;
        _winMovementScale = grid.FixedPanelScale.X;

        MatchManagerBehaviour.Instance.AddOnRingoutAction(OnSuddenDeathWon);
    }

    private void OnSuddenDeathWon()
    {
        _currentTimer?.Stop();
        _suddenDeathScreenEffect.SetActive(false);
    }

    public void SetDeathBarriersEnabled(bool enabled)
    {
        _deathbarrierL.SetActive(enabled);
        _deathbarrierR.SetActive(enabled);
    }

    public void BeginTimers()
    {
        _suddenDeathScreenEffect.SetActive(true);
        _currentTimer = FixedPointTimer.StartNewTimedAction(OnFirstExplosion, _firstExplosionDelay);

        foreach (var obj in _objectsToDisable)
        {
            obj.SetActive(false);
        }

        //SetDeathBarriersEnabled(true);

        _hasStarted = true;

        GridBehaviour.Instance.CollisionPlane.SetStagePiecesEnabled(false);
    }

    private void MoveBarriers()
    {
        _lhsWinCollider.FixedTransform.WorldPosition += (_lhsWinCollider.FixedTransform.Forward * _winMovementScale);
        _rhsWinCollider.FixedTransform.WorldPosition += (_rhsWinCollider.FixedTransform.Forward * _winMovementScale);
    }

    private void OnFirstExplosion()
    {
        //Disable panels for LHS
        _currentX = 0;
        PanelBehaviour[] targetPanels = null;

        GridBehaviour.Instance.GetAllPanels(CheckPanelOnCurrentX, out targetPanels);

        foreach (var panel in targetPanels)
        {
            panel.PanelEnabled = false;
        }

        // Disable panels for RHS
        _currentX = 5;

        GridBehaviour.Instance.GetAllPanels(CheckPanelOnCurrentX, out targetPanels);

        foreach (var panel in targetPanels)
        {
            panel.PanelEnabled = false;
        }

        _currentBarrierMoveTimer?.Stop();
        _currentBarrierMoveTimer = FixedPointTimer.StartNewTimedAction(MoveBarriers, _moveBarrierDelay);

        _currentTimer?.Stop();
        _currentTimer = FixedPointTimer.StartNewTimedAction(OnSecondExplosion, _secondExplosionDelay);
    }

    private void OnSecondExplosion()
    {
        //SetDeathBarriersEnabled(false);
        //Disable panels for LHS
        _currentX = 1;
        PanelBehaviour[] targetPanels = null;

        GridBehaviour.Instance.GetAllPanels(CheckPanelOnCurrentX, out targetPanels);

        foreach (var panel in targetPanels)
        {
            panel.PanelEnabled = false;
        }

        // Disable panels for RHS
        _currentX = 4;

        GridBehaviour.Instance.GetAllPanels(CheckPanelOnCurrentX, out targetPanels);

        foreach (var panel in targetPanels)
        {
            panel.PanelEnabled = false;
        }

        _currentBarrierMoveTimer?.Stop();
        _currentBarrierMoveTimer = FixedPointTimer.StartNewTimedAction(MoveBarriers, _moveBarrierDelay);

        _currentTimer?.Stop();
        _currentTimer = FixedPointTimer.StartNewTimedAction(OnThirdExplosion, _thirdExplosionDelay);
    }

    private void OnThirdExplosion()
    {
        _suddenDeathScreenEffect.SetActive(false);
        //Disable panels for LHS
        _currentX = 2;
        PanelBehaviour[] targetPanels = null;

        GridBehaviour.Instance.GetAllPanels(CheckPanelOnCurrentX, out targetPanels);

        foreach (var panel in targetPanels)
        {
            panel.PanelEnabled = false;
        }

        // Disable panels for RHS
        _currentX = 3;

        GridBehaviour.Instance.GetAllPanels(CheckPanelOnCurrentX, out targetPanels);

        foreach (var panel in targetPanels)
        {
            panel.PanelEnabled = false;
        }

        _currentBarrierMoveTimer?.Stop();
        _currentBarrierMoveTimer = FixedPointTimer.StartNewTimedAction(MoveBarriers, _moveBarrierDelay);
    }

    public void ResetAll()
    {
        if (!_hasStarted)
            return;

        //SetDeathBarriersEnabled(false);
        _suddenDeathScreenEffect.SetActive(false);

        _hasStarted = false;


        foreach (var obj in _objectsToDisable)
        {
            obj.SetActive(true);
        }

        GridBehaviour.Instance.EnableAllPanels();

        _lhsWinCollider.FixedTransform.WorldPosition = _lhsOriginalPos;
        _rhsWinCollider.FixedTransform.WorldPosition = _rhsOriginalPos;
        GridBehaviour.Instance.CollisionPlane.SetStagePiecesEnabled(true);
        _currentBarrierMoveTimer?.Stop();
        _currentTimer?.Stop();
    }

    private bool CheckPanelOnCurrentX(params object[] args)
    {
        PanelBehaviour panel = args[0] as PanelBehaviour;

        return panel != null && panel.Position.X == _currentX;
    }

    public override void Deserialize(BinaryReader br)
    {
    }

    public override void Serialize(BinaryWriter bw)
    {
    }
}
