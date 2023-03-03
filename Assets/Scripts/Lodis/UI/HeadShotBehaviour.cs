using Lodis.Gameplay;
using Lodis.ScriptableObjects;
using Lodis.Utility;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Lodis.UI
{
    public class HeadShotBehaviour : MonoBehaviour
    {
        [SerializeField]
        private IntVariable _playerID;
        [SerializeField]
        private GameObject _mask;
        [SerializeField]
        private Text _nameText;

        // Start is called before the first frame update
        void Start()
        {
            GameObject headShot = null;

            if (_playerID.Value == 1)
            {
                headShot = MatchManagerBehaviour.Instance.PlayerSpawner.Player1Data.HeadShot;
                _nameText.text = MatchManagerBehaviour.Instance.PlayerSpawner.Player1Data.DisplayName;
            }
            else if (_playerID.Value == 2)
            {
                headShot = MatchManagerBehaviour.Instance.PlayerSpawner.Player2Data.HeadShot;
                _nameText.text = MatchManagerBehaviour.Instance.PlayerSpawner.Player2Data.DisplayName;
            }

            headShot = Instantiate(headShot, _mask.transform);

            headShot.GetComponent<UIColorManagerBehaviour>().SetColors(_playerID.Value);
        }
    }
}