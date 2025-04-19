using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct Triangle
{
    public Vector3 a, b, c;
    public float area;

    public Triangle(Vector3 a, Vector3 b, Vector3 c)
    {
        this.a = a; this.b = b; this.c = c;
        area = Vector3.Cross(b - a, c - a).magnitude * 0.5f;
    }
}

public class AnimalNavigator : MonoBehaviour
{
    [Header("Navigation Settings")]
    public ClosedArea roamingArea;
    public float[] speedVariations = new float[] { 5f, 20f };
    public float fixedPlaneY = 0f;
    public float slowingRadius = 1.0f;

    [Header("Behavior Settings")]
    public float idleDelayMin = 0.5f;
    public float idleDelayMax = 2f;

    private Animator animator;
    private Vector3 destination;
    private bool reachedDestination = true;
    private float currentSpeed;

    // Navigation data
    private List<Triangle> tris;
    private float[] areaCumSum;
    private float totalArea;
    private Vector2[] poly2D;
    private Vector3[] poly3D;

    private bool isIdlePlaying = false;


    void Awake()
    {
        animator = GetComponentInChildren<Animator>();
        InitializeNavigationArea();
    }

    void InitializeNavigationArea()
    {
        if (roamingArea == null) return;

        Transform[] pts = roamingArea.GetAreaPoints();
        int m = pts.Length;
        poly2D = new Vector2[m];
        poly3D = new Vector3[m];

        for (int i = 0; i < m; i++)
        {
            poly2D[i] = new Vector2(pts[i].position.x, pts[i].position.z);
            poly3D[i] = pts[i].position;
        }

        tris = EarClipTriangulate(poly2D, pts);
        if (tris == null || tris.Count == 0)
        {
            Debug.LogWarning("Triangulation failed: Check if area points are valid and properly ordered.");
            return;
        }

        InitializeTriangulationData();
    }

    void InitializeTriangulationData()
    {
        int n = tris.Count;
        areaCumSum = new float[n];
        float cum = 0f;

        for (int i = 0; i < n; i++)
        {
            cum += tris[i].area;
            areaCumSum[i] = cum;
        }
        totalArea = cum;
    }

    void Start()
    {
        SetNewDestination();
        animator.SetFloat("Speed", 0f);
    }

    void Update()
    {
        if (reachedDestination && !isIdlePlaying)
        {
            StartCoroutine(SetDestinationAfterDelay());
            reachedDestination = false;
            return;
        }

        if (!reachedDestination)
            HandleMovement();
    }



    void HandleMovement()
    {
        Vector3 flatPos = transform.position;
        flatPos.y = fixedPlaneY;

        float distanceToTarget = Vector3.Distance(flatPos, destination);
        float adjustedSpeed = currentSpeed;

        // Calculate speed reduction within slowing radius
        if (distanceToTarget < slowingRadius && slowingRadius > 0)
        {
            adjustedSpeed = currentSpeed * (distanceToTarget / slowingRadius);
        }

        // Check if destination is reached
        if (distanceToTarget < 0.1f)
        {
            if (!reachedDestination)
            {
                reachedDestination = true;
                animator.SetFloat("Speed", 0f);
            }
            return;
        }

        // Move with adjusted speed
        transform.position = Vector3.MoveTowards(flatPos, destination, adjustedSpeed * Time.deltaTime);

        // Rotation handling (unchanged)
        Vector3 direction = destination - transform.position;
        direction.y = 0f;
        if (direction != Vector3.zero)
        {
            Quaternion targetRot = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 5f);
        }

        // Smoothly update animation speed
        float currentAnimSpeed = animator.GetFloat("Speed");
        animator.SetFloat("Speed", Mathf.Lerp(currentAnimSpeed, adjustedSpeed, Time.deltaTime * 5f));

        reachedDestination = false;
    }


    IEnumerator SetDestinationAfterDelay()
    {
        isIdlePlaying = true;

        // Stop movement
        animator.SetFloat("Speed", 0f);

        // Pick a random idle animation
        int idleCount = 3; // Update based on your setup
        int selected = Random.Range(0, idleCount);
        animator.SetInteger("IdleVariation", selected);
        animator.SetBool("IdleDone", false); // Reset done flag

        float delay = Random.Range(idleDelayMin, idleDelayMax);
        yield return new WaitForSeconds(delay);

        // Let animator return to idle state
        animator.SetBool("IdleDone", true);
        animator.SetInteger("IdleVariation", -1); // Reset
        isIdlePlaying = false;

        SetNewDestination();
    }




    void SetNewDestination()
    {
        if (speedVariations.Length == 0)
        {
            currentSpeed = 2f;
        }
        else
        {
            currentSpeed = speedVariations[Random.Range(0, speedVariations.Length)];
        }

        Vector3 dest = SampleRandomPosition();
        dest.y = fixedPlaneY;
        destination = dest;
    }


    Vector3 SampleRandomPosition()
    {
        float r = Random.value * totalArea;
        int i = System.Array.FindIndex(areaCumSum, cum => cum >= r);
        Triangle T = tris[i];
        float u = Random.value;
        float v = Random.value * (1 - u);
        return T.a * u + T.b * v + T.c * (1 - u - v);
    }

    List<Triangle> EarClipTriangulate(Vector2[] poly2D, Transform[] pts3D)
    {
        var result = new List<Triangle>();
        var idx = new List<int>();
        for (int i = 0; i < poly2D.Length; i++) idx.Add(i);

        while (idx.Count > 3)
        {
            bool earFound = false;

            for (int i = 0; i < idx.Count; i++)
            {
                int iPrev = (i - 1 + idx.Count) % idx.Count;
                int iCurr = i;
                int iNext = (i + 1) % idx.Count;

                Vector2 A = poly2D[idx[iPrev]];
                Vector2 B = poly2D[idx[iCurr]];
                Vector2 C = poly2D[idx[iNext]];

                if (Vector2.SignedAngle(B - A, C - B) <= 0) continue;

                bool containsPoint = false;
                for (int j = 0; j < idx.Count; j++)
                {
                    if (j == iPrev || j == iCurr || j == iNext) continue;
                    if (PointInTriangle(poly2D[idx[j]], A, B, C))
                    {
                        containsPoint = true;
                        break;
                    }
                }

                if (!containsPoint)
                {
                    result.Add(new Triangle(
                        pts3D[idx[iPrev]].position,
                        pts3D[idx[iCurr]].position,
                        pts3D[idx[iNext]].position
                    ));
                    idx.RemoveAt(iCurr);
                    earFound = true;
                    break;
                }
            }

            if (!earFound)
            {
                Debug.LogWarning("Failed to find an ear during triangulation. Check polygon integrity.");
                break;
            }
        }

        if (idx.Count == 3)
        {
            result.Add(new Triangle(
                pts3D[idx[0]].position,
                pts3D[idx[1]].position,
                pts3D[idx[2]].position
            ));
        }

        return result;
    }

    bool PointInTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
    {
        float area = Mathf.Abs((b.x - a.x) * (c.y - a.y) - (c.x - a.x) * (b.y - a.y));
        float a1 = Mathf.Abs((a.x - p.x) * (b.y - p.y) - (b.x - p.x) * (a.y - p.y));
        float a2 = Mathf.Abs((b.x - p.x) * (c.y - p.y) - (c.x - p.x) * (b.y - p.y));
        float a3 = Mathf.Abs((c.x - p.x) * (a.y - p.y) - (a.x - p.x) * (c.y - p.y));
        return Mathf.Approximately(area, a1 + a2 + a3);
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, destination);
        Gizmos.DrawSphere(destination, 0.3f);
    }

    void OnDrawGizmosSelected()
    {
        if (tris == null) return;

        Gizmos.color = Color.green;
        foreach (var tri in tris)
        {
            Gizmos.DrawLine(tri.a, tri.b);
            Gizmos.DrawLine(tri.b, tri.c);
            Gizmos.DrawLine(tri.c, tri.a);
        }
    }

#if UNITY_EDITOR
    [ContextMenu("Recalculate Area")]
    void RecalculateArea() => InitializeAreaManually();
#endif

    public void InitializeAreaManually()
    {
        InitializeNavigationArea();
    }
}
