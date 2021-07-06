using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

[RequireComponent(typeof(Animator))]
public class AnimationHandlerBehaviour : MonoBehaviour
{
    private AnimationClip _currentClip;
    [SerializeField]
    private RuntimeAnimatorController _runtimeAnimator;
    private PlayableGraph _playableGraph;
    private AnimationPlayableOutput _output;
    private AnimationClipPlayable _currentClipPlayable;
    private int _animationPhase;
    private bool _animatingMotion;
    [SerializeField]
    private Animator _animator;

    // Start is called before the first frame update
    void Start()
    {
        _playableGraph = PlayableGraph.Create();
        _playableGraph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
        _output = AnimationPlayableOutput.Create(_playableGraph, "OutPose", _animator);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
