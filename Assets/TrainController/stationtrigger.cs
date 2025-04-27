using UnityEngine;

public class TrainTriggerZone : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        TrainController train = other.GetComponent<TrainController>();
        if (train != null)
        {
            train.Speed = 0f; // Stop the train
        }
    }
}
