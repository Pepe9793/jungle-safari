// AnimalActionSystem.cs
using UnityEngine;

public class AnimalActionSystem : MonoBehaviour
{
    [Header("Animation Settings")]
    public string[] actionTriggers = { "Eat", "Sit", "TailWag" };

    private Animator animalAnimator;

    void Awake()
    {
        animalAnimator = GetComponent<Animator>();
    }

    public void PerformRandomAction()
    {
        string randomTrigger = actionTriggers[Random.Range(0, actionTriggers.Length)];
        animalAnimator.SetTrigger(randomTrigger);
        Debug.Log($"Triggered animation: {randomTrigger}");
    }
}