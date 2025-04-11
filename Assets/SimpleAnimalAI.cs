using UnityEngine;
using UnityEngine.AI;

public class SimpleAnimalAI : MonoBehaviour
{
    public float wanderRadius = 10f;
    public float waitTime = 3f;
    public float minSpeed = 1f;
    public float maxSpeed = 3f;

    private NavMeshAgent agent;
    private Animator animator;

    private float currentSpeed;
    private float targetSpeed;

    private bool isWaiting;
    private float waitTimer;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        GoToNewRandomPosition();
    }

    void Update()
    {
        // Smoothly interpolate to target speed
        currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, Time.deltaTime * 2f);
        agent.speed = currentSpeed;

        if (isWaiting)
        {
            waitTimer += Time.deltaTime;
            if (waitTimer >= waitTime)
            {
                isWaiting = false;
                GoToNewRandomPosition();
            }
        }
        else if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance && !agent.hasPath)
        {
            isWaiting = true;
            waitTimer = 0f;
        }

        // Update animator blend tree
        if (animator)
        {
            float normalizedSpeed = agent.velocity.magnitude;
            animator.SetFloat("Speed", normalizedSpeed, 0.1f, Time.deltaTime);

        }
    }

    void GoToNewRandomPosition()
    {
        Vector3 newPos = RandomNavSphere(transform.position, wanderRadius, -1);
        agent.SetDestination(newPos);

        // Assign a new random speed
        targetSpeed = Random.Range(minSpeed, maxSpeed);
        if (currentSpeed == 0f) currentSpeed = targetSpeed;
    }

    public static Vector3 RandomNavSphere(Vector3 origin, float dist, int layermask)
    {
        for (int i = 0; i < 10; i++)
        {
            Vector3 randDirection = Random.insideUnitSphere * dist;
            randDirection += origin;

            if (NavMesh.SamplePosition(randDirection, out NavMeshHit navHit, dist, layermask))
            {
                if (!Physics.Linecast(origin, navHit.position))
                {
                    return navHit.position;
                }
            }
        }

        return origin;
    }
}
