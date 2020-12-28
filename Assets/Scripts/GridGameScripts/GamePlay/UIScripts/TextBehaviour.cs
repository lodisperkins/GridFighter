using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace GridGame
{
    public class TextBehaviour : MonoBehaviour
    {
        [SerializeField]
        private VariableScripts.IntVariable Materials;
        // Update is called once per frame
        void Update()
        {
            GetComponent<Text>().text = "Materials: " + System.Convert.ToString(Materials.Val);
        }
    }
}
