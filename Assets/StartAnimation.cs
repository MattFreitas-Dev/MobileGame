using UnityEngine;

public class StartAnimation : MonoBehaviour
{
    public Animator animator;
    public AnimationClip animationClip;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (animator != null && animationClip != null)
        {
            animator.Play(animationClip.name);
        }
        else { animator.enabled = false; }
    }
}
