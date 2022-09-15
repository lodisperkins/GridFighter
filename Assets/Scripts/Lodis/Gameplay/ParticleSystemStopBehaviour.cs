using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Lodis.Utility;

namespace Lodis.Gameplay
{
    public class ParticleSystemStopBehaviour : MonoBehaviour
    {
        private void OnParticleSystemStopped()
        {
            ObjectPoolBehaviour.Instance.ReturnGameObject(gameObject);
        }
    }
}
