// AnimalActionSystem.cs (modified)
using UnityEngine;

public class AnimalActionSystem : MonoBehaviour
{
    [Header("Animation Settings")]
    public string[] actionTriggers = { "Eat", "Sit", "TailWag" };

    private Animator animalAnimator;

    void Awake()
    {
        animalAnimator = GetComponentInChildren<Animator>();
    }

    // Returns both the trigger name and its animation duration
    public (string trigger, float duration) PerformRandomAction()
    {
        string randomTrigger = actionTriggers[Random.Range(0, actionTriggers.Length)];
        animalAnimator.SetTrigger(randomTrigger);
        float duration = GetAnimationClipLength(randomTrigger);
        return (randomTrigger, duration);
    }

    // Public method to get the duration of an animation
    public float GetAnimationClipLength(string name)
    {
        foreach (var clip in animalAnimator.runtimeAnimatorController.animationClips)
        {
            if (clip.name == name)
                return clip.length;
        }
        return 0f; // Fallback
    }
}