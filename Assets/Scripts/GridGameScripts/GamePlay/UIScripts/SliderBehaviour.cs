using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GridGame
{
	public class SliderBehaviour : MonoBehaviour
    {
    
    	[SerializeField] private Slider _bar;
    
    	[SerializeField]
    	private HealthBehaviour hp;
        [SerializeField]
        private Color _overdriveColor;
        [SerializeField] private Color _defaultColor;
        private float lerpVal;
        [SerializeField] private GridGame.VariableScripts.IntVariable _value;
        [SerializeField] private Image _healthbarImage;
        public bool isHealthBar;
        public Image fillImage;
        public Slider Bar
        {
            get
            {
                return _bar;
            }

            set
            {
                _bar = value;
            }
        }

        // Use this for initialization
        void Start ()
        {
	        lerpVal = 1;
            if(transform.parent.parent.CompareTag("Block"))
            {
                _bar.maxValue = BlackBoard.maxBlockHealth;
            }
        }

        public void LerpHealthColor()
        {
	        if (_healthbarImage != null)
	        {
		        lerpVal = Bar.value / hp.HealthRef.Val;
		        _healthbarImage.color=Vector4.Lerp(Color.red,Color.yellow , lerpVal);
		        if (lerpVal >= 1)
		        {
			        _healthbarImage.color = Color.green;
		        }
		        
	        }
				
        }

        public void ChangeToOverdriveColor()

        {
	        _healthbarImage.color = Color.yellow;
        }

        public void ChangeToDefaultColor()
        {
	        _healthbarImage.color = Color.cyan;
        }
        
        public void SetValue(int value)
        {
            _value.Val = value;
        }
    	void Update ()
    	{
	        if (isHealthBar)
	        {
		        Bar.value = hp.health.Val;
		        LerpHealthColor();
	        }
	        else
	        {
		        Bar.value = _value.Val;
		        
	        }
    		
    	}
    }

}

