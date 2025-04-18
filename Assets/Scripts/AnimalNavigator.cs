using UnityEngine;
using System.Collections;

[RequireComponent(typeof(CharacterNavigationController))]
[RequireComponent(typeof(AnimalActionSystem))]
public class AnimalNavigator : MonoBehaviour
{
    [Header("Navigation Settings")]
    public ClosedArea roamingArea;
    public float minActionDuration = 2f;
    public float maxActionDuration = 5f;
    [Range(0, 1)] public float actionProbability = 0.3f;
    public float movementSpeed = 3f;

    [Header("Animation Parameters")]
    public string movementBlendTree = "Speed";
    public float animationTransitionTime = 0.1f;

    private CharacterNavigationController controller;
    private AnimalActionSystem actionSystem;
    private Animator animator;
    private bool isPerformingAction;
    private Vector3[] areaVertices;

    void Awake()
    {
        controller = GetComponent<CharacterNavigationController>();
        actionSystem = GetComponent<AnimalActionSystem>();
        animator = GetComponentInChildren<Animator>();

        controller.movementspeed = movementSpeed;
        InitializeNavigationArea();
    }

    void InitializeNavigationArea()
    {
        if (roamingArea != null)
        {
            Transform[] points = roamingArea.GetAreaPoints();
            areaVertices = new Vector3[points.Length];
            for (int i = 0; i < points.Length; i++)
            {
                areaVertices[i] = points[i].position;
            }
        }
    }

    void Start()
    {
        SetNewDestination();
        animator.SetFloat(movementBlendTree, 0f);
    }

    void Update()
    {
        if (!isPerformingAction)
        {

            if (controller.reachedDestinations)
            {
                DecideNextAction();
            }
        }
    }


    void DecideNextAction()
    {
        if (Random.value <= actionProbability)
        {
            StartCoroutine(PerformAnimalAction());
        }
        else
        {
            SetNewDestination();
        }
    }

    void SetNewDestination()
    {
        if (roamingArea == null) return;
        controller.SetDestination(GetRandomPositionInArea());
    }

    Vector3 GetRandomPositionInArea()
    {
        if (roamingArea == null || areaVertices.Length < 3) return transform.position;

        // Triangle-based random position calculation
        int triangleIndex = Random.Range(0, areaVertices.Length - 2);
        Vector3 p0 = areaVertices[triangleIndex];
        Vector3 p1 = areaVertices[triangleIndex + 1];
        Vector3 p2 = areaVertices[triangleIndex + 2];

        float u = Random.value;
        float v = Random.value;
        if (u + v > 1)
        {
            u = 1 - u;
            v = 1 - v;
        }

        Vector3 randomPoint = p0 * (1 - u - v) + p1 * u + p2 * v;
        randomPoint.y = transform.position.y;
        return randomPoint;
    }

    IEnumerator PerformAnimalAction()
    {
        isPerformingAction = true;
        controller.reachedDestinations = true;

        // Trigger random animation action
        actionSystem.PerformRandomAction();

        // Wait for animation duration
        float actionDuration = Random.Range(minActionDuration, maxActionDuration);
        yield return new WaitForSeconds(actionDuration);

        // Resume movement
        controller.reachedDestinations = false;
        SetNewDestination();
        isPerformingAction = false;
    }

    void OnDrawGizmos()
    {
        if (controller?.destination != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, controller.destination);
            Gizmos.DrawSphere(controller.destination, 0.3f);
        }
    }
}