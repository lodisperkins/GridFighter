using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Lodis
{
    public delegate bool Condition(params object[] args);
}

namespace Lodis.Utility
{
    public class Transition
    {
        public Condition TransitionCondition;
        public State TransitionState;

        public Transition(State state, Condition transitionCondition = null)
        {
            TransitionState = state;
            TransitionCondition = transitionCondition;
        }
    }

    [System.Serializable]
    public class State
    {
        public List<Transition> Transitions;
        public UnityAction OnStateEnter;
        public UnityAction OnStateExit;
        public string Name;

        public State(string name, params Transition[] transitions)
        {
            Name = name;
            Transitions = new List<Transition>();
            Transitions.AddRange(transitions);
            OnStateEnter = null;
            OnStateExit = null;
        }

        public State(string name, UnityAction onStateEnter, UnityAction onStateExit = null, params Transition[] transitions)
        {
            Name = name;
            OnStateEnter = onStateEnter;
            Transitions = new List<Transition>();
            Transitions.AddRange(transitions);
            OnStateExit = onStateExit;
        }
    }

    public class StateMachineBehaviour : MonoBehaviour
    {
        [SerializeField]
        private List<State> _states = new List<State>();
        [Tooltip("The current state the machine is in")]
        [SerializeField]
        private State _currentState;
        private State _anyState;

        public State CurrentState { get => _currentState; }

        private void Awake()
        {
            _anyState = new State("Any");
            _states.Add(_anyState);
        }

        public State GetState(string name)
        {
            return _states.Find(state => state.Name == name);
        }

        public void AddState(State state)
        {
            if (CurrentState.Name == "")
                _currentState = state;

            _states.Add(state);
        }

        public void AddGlobalState(State state, Condition transitionCondition)
        {
            if (CurrentState.Name == "")
                _currentState = state;

            Transition transition = new Transition(state, transitionCondition);
            _anyState.Transitions.Add(transition);
        }

        // Update is called once per frame
        void Update()
        {
            for (int i = 0; i < _anyState.Transitions.Count; i++)
            {
                if ((bool)_anyState.Transitions[i].TransitionCondition?.Invoke())
                    _currentState = _anyState.Transitions[i].TransitionState;
            }

            if (_currentState == _anyState)
                return;

            for (int i = 0; i < _currentState.Transitions.Count; i++)
            {
                if ((bool)_currentState.Transitions[i].TransitionCondition?.Invoke())
                    _currentState = _currentState.Transitions[i].TransitionState;
            }
        }
    }
}
