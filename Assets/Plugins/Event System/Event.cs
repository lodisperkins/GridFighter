using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace CustomEventSystem
{
    [CreateAssetMenu(menuName = "Event")]
    public class Event : ScriptableObject
    {
        //All listeners for the event
        private List<IListener> _listeners;

        //Adds a listener to the event
        public void AddListener(IListener newListener)
        {
            if (_listeners == null) _listeners = new List<IListener>();

            _listeners.Add(newListener);
        }
        //Raises the event with the gameobject information
        public void Raise(GameObject sender)
        {
            if (_listeners == null)
                return;

            foreach(IListener listener in _listeners)
            {
                listener.Invoke(sender);
            }
        }
        //Raises the game event with no information about who sent it
        public void Raise()
        {
            if (_listeners == null)
                return;

            foreach (IListener listener in _listeners)
            {
                listener.Invoke(null);
            }
        }
    }
}
