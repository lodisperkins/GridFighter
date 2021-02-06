using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace GridGame
{
    public interface IListener
    {
        void Invoke(Object Sender);
    }
}
