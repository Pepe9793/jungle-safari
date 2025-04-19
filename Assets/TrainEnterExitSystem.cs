using UnityEngine;

public class TrainEnterExitSystem : MonoBehaviour
{
    public Camera trainCamera;
    public KeyCode interactionKey = KeyCode.E;
    public Vector3 exitOffset = new Vector3(2f, 0f, 0f); // Offset for exit position

    private bool playerInZone = false;
    private bool isPlayerInTrain = false;
    private GameObject player;

    private void Start()
    {
        if (trainCamera != null)
            trainCamera.enabled = false;
    }

    private void Update()
    {
        if (playerInZone && !isPlayerInTrain && Input.GetKeyDown(interactionKey))
        {
            EnterTrain();
        }
        else if (isPlayerInTrain && Input.GetKeyDown(interactionKey))
        {
            ExitTrain();
        }
    }

    private void EnterTrain()
    {
        isPlayerInTrain = true;
        if (trainCamera != null)
            trainCamera.enabled = true;

        if (player != null)
            player.SetActive(false);
    }

    private void ExitTrain()
    {
        isPlayerInTrain = false;

        if (trainCamera != null)
            trainCamera.enabled = false;

        if (player != null)
        {
            // Move player to world exit position
            Vector3 worldExitPos = transform.TransformPoint(exitOffset);
            player.transform.position = worldExitPos;
            player.transform.rotation = Quaternion.LookRotation(-transform.forward);

            player.SetActive(true);

            // ❗ Prevent re-entering without leaving trigger zone
            playerInZone = false;
            player = null;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isPlayerInTrain && other.CompareTag("Player"))
        {
            playerInZone = true;
            player = other.gameObject;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInZone = false;

            // Only clear reference if not in train
            if (!isPlayerInTrain)
                player = null;
        }
    }
}
