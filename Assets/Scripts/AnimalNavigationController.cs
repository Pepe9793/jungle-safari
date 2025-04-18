using Unity.Mathematics;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class AnimalNavigationController : MonoBehaviour
{
    public float movementspeed = 5f;
    public float stopDistance = 0.1f;
    public float rotationspeed = 360f;

    public Vector3 destination;
    public bool reachedDestinations = true;

    private Animator animator;

    [HideInInspector] public bool reachedDestination => reachedDestinations;
    void Start()
    {
        animator = GetComponent<Animator>();
    }

    public void SetMovementSpeed(float newSpeed)
    {
        movementspeed = newSpeed;
        animator.SetFloat("Speed", newSpeed);
    }
    void Update()
    {
        if (transform.position != destination)
        {
            Vector3 destinationDirection = destination - transform.position;
            destinationDirection.y = 0;

            float distance = destinationDirection.magnitude;

            if (distance > stopDistance)
            {
                reachedDestinations = false;

                // Rotation
                Quaternion targetRot = Quaternion.LookRotation(destinationDirection);
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation,
                    targetRot,
                    rotationspeed * Time.deltaTime
                );

                // Movement
                transform.position = Vector3.MoveTowards(
                    transform.position,
                    destination,
                    movementspeed * Time.deltaTime
                );

                animator.SetFloat("Speed", movementspeed);
            }
            else
            {
                reachedDestinations = true;
                animator.SetFloat("Speed", 0);
            }
        }
    }

    public void SetDestination(Vector3 destination)
    {
        this.destination = destination;
        reachedDestinations = false;
    }
}
