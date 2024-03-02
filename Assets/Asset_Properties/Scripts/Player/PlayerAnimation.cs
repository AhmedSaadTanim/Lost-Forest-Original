using System.Collections.Generic;
using UnityEngine;

public class PlayerAnimation : MonoBehaviour
{
    public Animator animator;
    string currentState;
    public bool isAnimationComplete;
    public Dictionary<string, bool> animationStateComplete;

    void Awake()
    {
        animator = GetComponent<Animator>();
    }

    void Start()
    {
        animationStateComplete = new Dictionary<string, bool>();
    }

    public void ChangeAnimationState(string newState)
    {
        if (currentState == newState)
            return;

        if (!animationStateComplete.ContainsKey(newState))
        {
            animationStateComplete.Add(newState, false);
        }


        animator.Play(newState);
        animationStateComplete[newState] = false;

        currentState = newState;
    }

    public void OnAnimationComplete(AnimationEvent animationEvent)
    {
        AnimatorClipInfo clipInfo = animationEvent.animatorClipInfo;
        string stateName = clipInfo.clip.name;
        animationStateComplete[stateName] = true;

        if (stateName.Equals(States.PLAYER_ATTACK_1))
        {
            ChangeAnimationState(States.PLAYER_IDLE);
        }
    }

}
