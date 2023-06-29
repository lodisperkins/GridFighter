using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace CustomEventSystem
{
    public interface IListener
    {
        void Invoke(GameObject Sender);
    }
}
