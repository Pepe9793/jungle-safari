using Unity.Mathematics;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class CharacterNavigationController : MonoBehaviour
{
    public float movementspeed = 5f;
    public float stopDistance = 0.1f;
    public float rotationspeed = 360f;

    public Vector3 destination;
    public bool reachedDestinations = true;

    private Animator animator;

    void Start()
    {
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        if (transform.position != destination)
        {
            Vector3 destinationDirection = destination - transform.position;
            destinationDirection.y = 0;

            float destinationDistance = destinationDirection.magnitude;

            if (destinationDistance > stopDistance)
            {
                reachedDestinations = false;

                // Rotate toward destination
                Quaternion targetRotation = Quaternion.LookRotation(destinationDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationspeed * Time.deltaTime);

                // Move toward destination
                transform.position += destinationDirection.normalized * movementspeed * Time.deltaTime;

                // Set animation parameter
                animator.SetFloat("Speed", movementspeed); // Use your actual parameter name

                if (Physics.Raycast(transform.position + Vector3.up * 0.5f, Vector3.down, out RaycastHit hit, 1f))
                {
                    transform.position = new Vector3(transform.position.x, hit.point.y, transform.position.z);
                }
            }
            else
            {
                reachedDestinations = true;
                animator.SetFloat("Speed", 0f);
            }
        }
        else
        {
            animator.SetFloat("Speed", 0f);
        }
    }

    public void SetDestination(Vector3 destination)
    {
        this.destination = destination;
        reachedDestinations = false;
    }
}
