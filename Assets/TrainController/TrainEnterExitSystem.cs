using UnityEngine;

public class TrainEnterExitSystem : MonoBehaviour
{
    public Transform train;
    public Transform player;
    public GameObject playerCam;
    public GameObject trainCam;
    public GameObject interactionUI;
    public Vector3 exitOffset = new Vector3(2f, 0f, 0f);
    public KeyCode interactionKey = KeyCode.E;

    private bool canEnter = false;
    private bool isInTrain = false;

    void Start()
    {
        interactionUI.SetActive(false);
        playerCam.SetActive(true);
        trainCam.SetActive(false);
    }

    void Update()
    {
        if (canEnter && Input.GetKeyDown(interactionKey) && !isInTrain)
        {
            EnterTrain();
        }
        else if (isInTrain && Input.GetKeyDown(interactionKey))
        {
            ExitTrain();
        }
    }

    void EnterTrain()
    {
        isInTrain = true;
        interactionUI.SetActive(false);

        player.gameObject.SetActive(false);
        player.SetParent(train);
        playerCam.SetActive(false);
        trainCam.SetActive(true);
    }

    void ExitTrain()
    {
        isInTrain = false;

        player.SetParent(null);
        Vector3 worldExitPos = train.TransformPoint(exitOffset);
        player.position = worldExitPos;
        player.rotation = Quaternion.LookRotation(-train.forward);

        player.gameObject.SetActive(true);
        playerCam.SetActive(true);
        trainCam.SetActive(false);

        // Prevent immediate re-entry
        canEnter = false;
        player = null;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !isInTrain)
        {
            interactionUI.SetActive(true);
            canEnter = true;
            player = other.transform;
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            interactionUI.SetActive(false);
            canEnter = false;
            if (!isInTrain) player = null;
        }
    }
}