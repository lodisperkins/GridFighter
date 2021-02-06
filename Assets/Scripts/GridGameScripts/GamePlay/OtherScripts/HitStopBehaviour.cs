using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GridGame.GamePlay.OtherScripts
{
    public class HitStopBehaviour : MonoBehaviour
    {
        private void Start()
        {
            BlackBoard.hitStopHandler = this;
        }
        public void Stop(float duration)
        {
            Time.timeScale = 0;
            StartCoroutine(Wait(duration));
        }
        IEnumerator Wait(float duration)
        {
            yield return new WaitForSecondsRealtime(duration);
            Time.timeScale = 1;
        }
    }
}


