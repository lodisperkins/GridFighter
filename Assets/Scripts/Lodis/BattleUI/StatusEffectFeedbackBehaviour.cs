using Lodis.Gameplay;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Handles updating the counter, icons and particle effects of each status effect.
/// </summary>
public class StatusEffectFeedbackBehaviour : MonoBehaviour
{
    [System.Serializable]
    public class StatusEffectVisual
    {
        public StatusEffect.StatusEffectType EffectType;
        public Sprite Icon;
        public GameObject[] ParticleEffects;
        public int LastStackEffectIndex;

        public void SetStackVisualActive(int countIndex)
        {
            ParticleEffects[LastStackEffectIndex].SetActive(false);

            LastStackEffectIndex = Mathf.Clamp(countIndex, 0, ParticleEffects.Length - 1);

            if (countIndex >= 0)
                ParticleEffects[LastStackEffectIndex].SetActive(true);
        }

        public void DisableVisuals()
        {
            ParticleEffects[LastStackEffectIndex].SetActive(false);
        }
    }

    [SerializeField] private StatusEffectManagerBehaviour _statusEffectManager;

    [Header("Helpful Status Effect")]
    [SerializeField] private GameObject _helpfulStatusEffectRoot;
    [SerializeField] private Image _helpfulStatusEffectIcon;
    [SerializeField] private Text _helpfulStatusEffectCounter;

    [Header("Harmful Status Effect")]
    [SerializeField] private GameObject _harmfulStatusEffectRoot;
    [SerializeField] private Image _harmfulStatusEffectIcon;
    [SerializeField] private Text _harmfulStatusEffectCounter;

    [Header("Status Effect Visuals")]
    [SerializeField] private StatusEffectVisual[] _statusEffectVisuals;

    private StatusEffect.StatusEffectType _lastAddedStatusEffect;

    // Start is called before the first frame update
    void Start()
    {
        _statusEffectManager.AddOnStatusEffectAddedListener(UpdateVisuals);
        _statusEffectManager.AddOnStatusEffectRemovedListener(UpdateVisuals);

        if (MatchManagerBehaviour.Instance)
        {
            MatchManagerBehaviour.Instance.AddOnMatchRestartAction(ClearVisuals);
        }
    }

    public void ClearVisuals()
    {
        _statusEffectVisuals[(int)_lastAddedStatusEffect].DisableVisuals();
    }

    /// <summary>
    /// Updates the status effect icon and particle effects based on the status effect type that just got added.
    /// </summary>
    private void UpdateVisuals(StatusEffect.StatusEffectType statusEffect)
    {
        if (statusEffect != _lastAddedStatusEffect)
        {
            ClearVisuals();
            _lastAddedStatusEffect = statusEffect;
        }

        StatusEffectVisual visual = _statusEffectVisuals[(int)statusEffect];

        // Status effect type greater than Chilled are considered helpful effects.
        if (statusEffect > StatusEffect.StatusEffectType.Chilled)
        {
            _helpfulStatusEffectRoot.SetActive(true);
            _helpfulStatusEffectIcon.sprite = visual.Icon;
            visual.SetStackVisualActive(_statusEffectManager.CurrentHelpfulEffect.StackCount - 1);
        }
        // Status effect type less than or equal to Chilled are considered harmful effects.
        else
        {
            _harmfulStatusEffectRoot.SetActive(true);
            _harmfulStatusEffectIcon.sprite = visual.Icon;
            visual.SetStackVisualActive(_statusEffectManager.CurrentHarmfulEffect.StackCount - 1);
        }
    }

    private void Update()
    {
        // Update the stack counters each frame.

        // Update helpful effect if there is one active.
        if (_statusEffectManager.CurrentHelpfulEffect != null && _statusEffectManager.CurrentHelpfulEffect.StackCount > 0)
        {
            _helpfulStatusEffectCounter.text = _statusEffectManager.CurrentHelpfulEffect.StackCount.ToString();
        }
        else if (_statusEffectManager.CurrentHelpfulEffect == null || _statusEffectManager.CurrentHelpfulEffect.StackCount == 0)
        {
            _helpfulStatusEffectRoot.SetActive(false);
        }

        // Update harmful effect if there is one active.
        if (_statusEffectManager.CurrentHarmfulEffect != null && _statusEffectManager.CurrentHarmfulEffect.StackCount > 0)
        {
            _harmfulStatusEffectCounter.text = _statusEffectManager.CurrentHarmfulEffect.StackCount.ToString();
        }
        else if(_statusEffectManager.CurrentHarmfulEffect == null || _statusEffectManager.CurrentHarmfulEffect.StackCount == 0)
        {
            _harmfulStatusEffectRoot.SetActive(false);
        }
    }
}
