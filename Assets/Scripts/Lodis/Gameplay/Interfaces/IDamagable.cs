using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lodis.Gameplay
{
    public interface IDamagable
    {
        float TakeDamage(params object[] args);
    }
}


