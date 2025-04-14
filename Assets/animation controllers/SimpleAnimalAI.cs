using UnityEngine;
using UnityEngine.AI;

public class SimpleAnimalAI : MonoBehaviour
{
    public float wanderRadius = 10f;
    public float waitTime = 3f;
    public float wanderTime = 2f;

    [Header("Custom Speed Presets")]
    public float[] speedOptions = new float[] { 1f, 2f, 3f }; // define your speed choices here

    private NavMeshAgent agent;
    private Animator animator;

    private float currentSpeed;
    private float targetSpeed;

    private bool isWaiting;
    private float waitTimer;
    private float wanderTimer;
    private bool readyToWander;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        GoToNewRandomPosition();
    }

    void Update()
    {
        // Smooth speed transition
        currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, Time.deltaTime * 2f);
        agent.speed = currentSpeed;

        if (isWaiting)
        {
            waitTimer += Time.deltaTime;

            if (waitTimer >= waitTime)
            {
                isWaiting = false;
                readyToWander = true;
                wanderTimer = 0f;
            }
        }
        else if (readyToWander)
        {
            wanderTimer += Time.deltaTime;

            if (wanderTimer >= wanderTime)
            {
                readyToWander = false;
                GoToNewRandomPosition();
            }
        }
        else if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance && !agent.hasPath)
        {
            isWaiting = true;
            waitTimer = 0f;
        }

        // Animator blend tree speed parameter
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

        // Pick a speed from the custom speed options
        if (speedOptions.Length > 0)
        {
            targetSpeed = speedOptions[Random.Range(0, speedOptions.Length)];
        }
        else
        {
            targetSpeed = 2f; // default fallback speed
        }

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
