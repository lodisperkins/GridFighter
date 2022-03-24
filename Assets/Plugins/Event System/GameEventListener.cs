﻿using System.Collections;
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
        UnityEvent actions = new UnityEvent();
        //the event the gameobject should be listening for

        public GridGame.Event Event;
        //The sender the gameobject is waiting for the event to be raiased by
        public GameObject IntendedSender;

        public GameEventListener(Event listenerEvent, GameObject sender)
        {
            Event = listenerEvent;
            Event.AddListener(this);
            IntendedSender = sender;
            actions = new UnityEvent();
        }

        // Use this for initialization
        void Start()
        {
            if (!Event)
                Event = new Event();

            Event.AddListener(this);
        }

        public void AddAction(UnityAction action)
        {
            if (actions == null)
                actions = new UnityEvent();

            actions.AddListener(action);
        }

        public void ClearActions()
        {
            actions.RemoveAllListeners();
        }

        public void ClearEvent()
        {
            Event = new Event();
        }

        //Invokes the actions delegate
        public void Invoke(GameObject Sender)
        {
            if((IntendedSender == null || IntendedSender == Sender) && actions != null)
            {
                actions.Invoke();
                return;
            }
        }
    }
}
