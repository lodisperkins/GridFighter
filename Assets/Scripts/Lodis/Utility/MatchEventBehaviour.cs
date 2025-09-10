using Lodis.Gameplay;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class MatchEventBehaviour : MonoBehaviour
{
    [SerializeField] private bool _destroyOnMatchEnd = true;
    [SerializeField] private bool _destroyOnMatchStart = false;
    [SerializeField] private UnityEvent _onMatchStart;
    [SerializeField] private UnityEvent _onMatchEnd;
    [Tooltip("OPTIONAL. If this has an Entity a part of the rollback simulation tied to it assigning it to this value will properly remove it from the simulation.")]
    [SerializeField] private EntityDataBehaviour _simulationEntity;

    // Start is called before the first frame update
    void Start()
    {
        MatchManagerBehaviour.Instance.AddOnMatchRestartAction(OnMatchStart);
        MatchManagerBehaviour.Instance.AddOnMatchOverAction(OnMatchEnd);
    }

    private void OnMatchStart()
    {
        _onMatchStart?.Invoke();

        if (_destroyOnMatchStart)
        {
            if (_simulationEntity != null)
            {
                _simulationEntity.RemoveFromGame(true);
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }

    private void OnMatchEnd()
    {
        _onMatchEnd?.Invoke();

        if (!_destroyOnMatchEnd)
            return;

        if (_simulationEntity != null)
        {
            _simulationEntity.RemoveFromGame(true);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
