using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(menuName = "Voice Pack Data")]
public class VoicePackData : ScriptableObject
{
    [SerializeField]
    private AudioClip _hurt1;
    [SerializeField]
    private AudioClip _hurt2;
    [SerializeField]
    private AudioClip _hurt3;
    [SerializeField]
    private AudioClip _lightAttack1;
    [SerializeField]
    private AudioClip _lightAttack2;
    [SerializeField]
    private AudioClip _lightAttack3;
    [SerializeField]
    private AudioClip _heavyAttack1;
    [SerializeField]
    private AudioClip _heavyAttack2;
    [SerializeField]
    private AudioClip _heavyAttack3;
    [SerializeField]
    private AudioClip _superAttack;
    [SerializeField]
    private AudioClip _death;
    [SerializeField]
    private AudioClip _burst;
    private int _lastHurt;
    private int _lastLight;
    private int _lastHeavy;

    public AudioClip Hurt1 { get => _hurt1; private set => _hurt1 = value; }
    public AudioClip Hurt2 { get => _hurt2; private set => _hurt2 = value; }
    public AudioClip Hurt3 { get => _hurt3; private set => _hurt3 = value; }
    public AudioClip LightAttack1 { get => _lightAttack1; private set => _lightAttack1 = value; }
    public AudioClip LightAttack2 { get => _lightAttack2; private set => _lightAttack2 = value; }
    public AudioClip LightAttack3 { get => _lightAttack3; private set => _lightAttack3 = value; }
    public AudioClip HeavyAttack1 { get => _heavyAttack1; private set => _heavyAttack1 = value; }
    public AudioClip HeavyAttack2 { get => _heavyAttack2; private set => _heavyAttack2 = value; }
    public AudioClip HeavyAttack3 { get => _heavyAttack3; private set => _heavyAttack3 = value; }
    public AudioClip SuperAttack { get => _superAttack; private set => _superAttack = value; }
    public AudioClip Death { get => _death; private set => _death = value; }
    public AudioClip Burst { get => _burst; private set => _burst = value; }

    public AudioClip GetRandomHurtClip()
    {
        int choiceNum = Random.Range(1, 4);

        if (choiceNum == _lastHurt)
            choiceNum++;

        AudioClip choice = null;

        if (choiceNum == 1)
            choice = _hurt1;
        else if (choiceNum == 2)
            choice = _hurt2;
        else if (choiceNum == 3)
            choice = _hurt3;

        _lastHurt = choiceNum;
        return choice;
    }

    public AudioClip GetRandomLightAttackClip()
    {
        int choiceNum = Random.Range(1, 4);

        if (choiceNum == _lastLight)
            choiceNum++;

        AudioClip choice = null;

        if (choiceNum == 1)
            choice = _lightAttack1;
        else if (choiceNum == 2)
            choice = _lightAttack2;
        else if (choiceNum == 3)
            choice = _lightAttack3;

        _lastLight = choiceNum;

        return choice;
    }

    public AudioClip GetRandomHeavyAttackClip()
    {
        int choiceNum = Random.Range(1, 4);

        if (choiceNum == _lastHeavy)
            choiceNum++;

        AudioClip choice = null;

        if (choiceNum == 1)
            choice = _heavyAttack1;
        else if (choiceNum == 2)
            choice = _heavyAttack2;
        else if (choiceNum == 3)
            choice = _heavyAttack3;

        _lastHeavy = choiceNum;
        return choice;
    }
}
