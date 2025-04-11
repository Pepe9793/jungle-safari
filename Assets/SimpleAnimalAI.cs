using UnityEngine;
using UnityEngine.AI;

public class SimpleAnimalAI : MonoBehaviour
{
    public float wanderRadius = 10f;
    public float wanderTimer = 5f;
    public float idleTime = 3f;

    private NavMeshAgent agent;
    private float timer;
    private Animator animator;
    private float idleTimer;
    private bool isIdle;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>(); // Optional
        timer = wanderTimer;
    }

    void Update()
    {
        timer += Time.deltaTime;

        if (isIdle)
        {
            idleTimer += Time.deltaTime;
            if (idleTimer >= idleTime)
            {
                isIdle = false;
                timer = wanderTimer; // Force new destination
            }
        }
        else if (timer >= wanderTimer)
        {
            Vector3 newPos = RandomNavSphere(transform.position, wanderRadius, -1);
            agent.SetDestination(newPos);
            timer = 0;
            isIdle = true;
            idleTimer = 0;

            if (animator) animator.SetTrigger("Walk"); // Optional
        }

        // Check if reached destination
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance && !agent.hasPath)
        {
            if (animator) animator.SetTrigger("Idle"); // Optional
        }
    }

    public static Vector3 RandomNavSphere(Vector3 origin, float dist, int layermask)
    {
        for (int i = 0; i < 10; i++) // Try up to 10 times
        {
            Vector3 randDirection = Random.insideUnitSphere * dist;
            randDirection += origin;

            if (NavMesh.SamplePosition(randDirection, out NavMeshHit navHit, dist, layermask))
            {
                // Check if there's a direct path
                if (!Physics.Linecast(origin, navHit.position))
                {
                    return navHit.position;
                }
            }
        }

        return origin; // Fallback
    }

}
