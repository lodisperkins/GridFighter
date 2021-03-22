using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
namespace GridGame
{
    delegate void Actions();
    public class GameEventListener:MonoBehaviour,IListener
    {
        //Delegate containing all functions to used when the event is invoked
        [SerializeField]
        UnityEvent actions;
        //the event the gameobject should be listening for

        public GridGame.Event Event;
        //The sender the gameobject is waiting for the event to be raiased by
        public GameObject intendedSender;
        // Use this for initialization
        void Start()
        {
            if (!Event)
                Event = new Event();

            Event.AddListener(this);
        }

        public void AddAction(UnityAction action)
        {
            actions.AddListener(action);
        }

        public void ClearListeners()
        {
            Event = new Event();
        }

        //Invokes the actions delegate
        public void Invoke(Object Sender)
        {
            if(intendedSender == null)
            {
                actions.Invoke();
                return;
            }
            else if(intendedSender == Sender)
            {
                actions.Invoke();
                return;
            }
        }
    }
}
