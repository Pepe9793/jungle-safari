using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Triangle struct for area-weighted sampling
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

[RequireComponent(typeof(CharacterNavigationController))]
[RequireComponent(typeof(AnimalActionSystem))]
public class AnimalNavigator : MonoBehaviour
{
    [Header("Navigation Settings")]
    public ClosedArea roamingArea;
    [Range(0, 1)] public float actionProbability = 0.3f;
    public float movementSpeed = 3f;
    public float fixedPlaneY = 0f;

    private CharacterNavigationController controller;
    private AnimalActionSystem actionSystem;
    private Animator animator;
    private bool isPerformingAction = false;
    private bool pendingJitter = false;

    // Sampling triangulation data
    private List<Triangle> tris;
    private float[] areaCumSum;
    private float totalArea;

    // Polygon points and static visibility
    private Vector2[] poly2D;
    private Vector3[] poly3D;
    private List<int>[] staticAdj;

    // Dynamic path data
    private List<Vector3> pathWaypoints;
    private int pathIndex;

    public List<GameObject> nearbyAnimals;

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
        // Triangulate for sampling
        tris = EarClipTriangulate(poly2D, pts);
        int n = tris.Count;
        areaCumSum = new float[n];
        float cum = 0f;
        for (int i = 0; i < n; i++)
        {
            cum += tris[i].area;
            areaCumSum[i] = cum;
        }
        totalArea = cum;

        // Build static visibility adjacency for polygon vertices
        staticAdj = new List<int>[m];
        for (int i = 0; i < m; i++) staticAdj[i] = new List<int>();
        var nodes2D = new List<Vector2>(poly2D);

        for (int i = 0; i < m; i++)
        {
            for (int j = i + 1; j < m; j++)
            {
                if (IsVisible(i, j, nodes2D))
                {
                    staticAdj[i].Add(j);
                    staticAdj[j].Add(i);
                }
            }
        }
    }

    // Static visibility between two polygon vertices
    private bool IsVisible(int i, int j, List<Vector2> nodes2D)
    {
        Vector2 a = nodes2D[i];
        Vector2 b = nodes2D[j];
        int len = poly2D.Length;
        for (int k = 0; k < len; k++)
        {
            int next = (k + 1) % len;
            // skip edge adjacent to vertices
            if ((i == k && j == next) || (i == next && j == k)) continue;
            if (SegmentsIntersect(a, b, poly2D[k], poly2D[next])) return false;
        }
        Vector2 mid = (a + b) * 0.5f;
        return PointInPolygon(mid);
    }

    private List<Triangle> EarClipTriangulate(Vector2[] poly2D, Transform[] pts3D)
    {
        var result = new List<Triangle>();
        var idx = new List<int>();
        for (int i = 0; i < poly2D.Length; i++) idx.Add(i);
        int safety = 0;
        while (idx.Count > 3 && safety++ < 1000)
        {
            bool clipped = false;
            for (int i = 0; i < idx.Count; i++)
            {
                int iPrev = idx[(i - 1 + idx.Count) % idx.Count];
                int iCurr = idx[i];
                int iNext = idx[(i + 1) % idx.Count];
                Vector2 A = poly2D[iPrev], B = poly2D[iCurr], C = poly2D[iNext];
                if (Vector2.SignedAngle(B - A, C - B) <= 0) continue;
                bool contains = false;
                foreach (int j in idx)
                {
                    if (j == iPrev || j == iCurr || j == iNext) continue;
                    if (PointInTriangle(poly2D[j], A, B, C)) { contains = true; break; }
                }
                if (contains) continue;
                result.Add(new Triangle(pts3D[iPrev].position, pts3D[iCurr].position, pts3D[iNext].position));
                idx.RemoveAt(i);
                clipped = true;
                break;
            }
            if (!clipped) break;
        }
        if (idx.Count == 3)
            result.Add(new Triangle(pts3D[idx[0]].position, pts3D[idx[1]].position, pts3D[idx[2]].position));
        return result;
    }

    private bool PointInTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
    {
        float area = Mathf.Abs((b.x - a.x) * (c.y - a.y) - (c.x - a.x) * (b.y - a.y));
        float a1 = Mathf.Abs((a.x - p.x) * (b.y - p.y) - (b.x - p.x) * (a.y - p.y));
        float a2 = Mathf.Abs((b.x - p.x) * (c.y - p.y) - (c.x - p.x) * (b.y - p.y));
        float a3 = Mathf.Abs((c.x - p.x) * (a.y - p.y) - (a.x - p.x) * (c.y - p.y));
        return Mathf.Approximately(area, a1 + a2 + a3);
    }

    private Vector3 SampleRandomPosition()
    {
        float r = Random.value * totalArea;
        int i = System.Array.FindIndex(areaCumSum, cum => cum >= r);
        Triangle T = tris[i];
        float u = Random.value;
        float v = Random.value * (1 - u);
        return T.a * u + T.b * v + T.c * (1 - u - v);
    }

    void Start()
    {
        SetNewDestination();
        animator.SetFloat("Speed", 0f);
    }

    void Update()
    {
        if (!isPerformingAction && controller.reachedDestinations)
        {
            if (pathWaypoints != null && pathIndex < pathWaypoints.Count - 1)
            {
                pathIndex++;
                controller.SetDestination(pathWaypoints[pathIndex]);
            }
            else if (!pendingJitter)
            {
                pendingJitter = true;
                StartCoroutine(DecideNextActionWithJitter());
            }
        }
    }

    IEnumerator DecideNextActionWithJitter()
    {
        yield return new WaitForSeconds(Random.Range(0f, 1f));
        pendingJitter = false;
        if (Random.value <= actionProbability)
            yield return StartCoroutine(PerformAnimalAction());
        else
            SetNewDestination();
    }

    void SetNewDestination()
    {
        Vector3 start = transform.position;
        Vector3 dest = SampleRandomPosition();
        dest.y = fixedPlaneY;

        // Micro-opt 1: direct line-of-sight shortcut
        if (IsVisibleDynamic(start, dest))
        {
            pathWaypoints = new List<Vector3> { start, dest };
            pathIndex = 1;
            controller.SetDestination(dest);
            return;
        }

        // Build optimized path using cached static adjacency
        pathWaypoints = BuildOptimizedPath(start, dest);
        if (pathWaypoints == null || pathWaypoints.Count == 0)
        {
            controller.SetDestination(dest);
            return;
        }
        pathIndex = 1;
        controller.SetDestination(pathWaypoints[pathIndex]);
    }

    private bool IsVisibleDynamic(Vector3 a3, Vector3 b3)
    {
        Vector2 a = new Vector2(a3.x, a3.z);
        Vector2 b = new Vector2(b3.x, b3.z);
        int polyLen = poly2D.Length;
        for (int i = 0; i < polyLen; i++)
        {
            int next = (i + 1) % polyLen;
            if (SegmentsIntersect(a, b, poly2D[i], poly2D[next])) return false;
        }
        Vector2 mid = (a + b) * 0.5f;
        return PointInPolygon(mid);
    }

    private List<Vector3> BuildOptimizedPath(Vector3 start, Vector3 goal)
    {
        int m = poly3D.Length;
        int startIdx = m;
        int goalIdx = m + 1;
        int N = m + 2;
        var nodes = new List<Vector3>(poly3D) { start, goal };
        var nodes2D = new List<Vector2>(poly2D) { new Vector2(start.x, start.z), new Vector2(goal.x, goal.z) };

        var adj = new List<int>[N];
        for (int i = 0; i < N; i++) adj[i] = new List<int>();
        for (int i = 0; i < m; i++)
            foreach (int j in staticAdj[i])
                if (j > i) { adj[i].Add(j); adj[j].Add(i); }
        for (int i = 0; i < m; i++)
            if (IsVisibleDynamic(start, poly3D[i])) { adj[startIdx].Add(i); adj[i].Add(startIdx); }
        for (int i = 0; i < m; i++)
            if (IsVisibleDynamic(goal, poly3D[i])) { adj[goalIdx].Add(i); adj[i].Add(goalIdx); }
        if (IsVisibleDynamic(start, goal)) { adj[startIdx].Add(goalIdx); adj[goalIdx].Add(startIdx); }

        var g = new float[N]; var f = new float[N]; var closed = new bool[N];
        for (int i = 0; i < N; i++) { g[i] = float.MaxValue; f[i] = float.MaxValue; }
        g[startIdx] = 0f; f[startIdx] = Vector3.Distance(start, goal);
        var open = new List<int> { startIdx };
        var cameFrom = new Dictionary<int, int>();

        while (open.Count > 0)
        {
            int current = open[0]; foreach (var idx in open) if (f[idx] < f[current]) current = idx;
            if (current == goalIdx) break;
            open.Remove(current); closed[current] = true;
            foreach (var nbr in adj[current])
            {
                if (closed[nbr]) continue;
                float tent = g[current] + Vector3.Distance(nodes[current], nodes[nbr]);
                if (!open.Contains(nbr)) open.Add(nbr);
                if (tent < g[nbr])
                {
                    cameFrom[nbr] = current;
                    g[nbr] = tent;
                    f[nbr] = tent + Vector3.Distance(nodes[nbr], goal);
                }
            }
        }
        var path = new List<Vector3>();
        if (!cameFrom.ContainsKey(goalIdx)) return new List<Vector3> { goal };
        int cur = goalIdx;
        while (cur != startIdx) { path.Add(nodes[cur]); cur = cameFrom[cur]; }
        path.Add(start); path.Reverse();
        return path;
    }

    private bool SegmentsIntersect(Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4)
    {
        float o1 = Orient(p1, p2, p3);
        float o2 = Orient(p1, p2, p4);
        float o3 = Orient(p3, p4, p1);
        float o4 = Orient(p3, p4, p2);
        return (o1 * o2 < 0f && o3 * o4 < 0f);
    }

    private float Orient(Vector2 a, Vector2 b, Vector2 c)
    {
        return (b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x);
    }

    private bool PointInPolygon(Vector2 pt)
    {
        int count = 0;
        for (int i = 0; i < poly2D.Length; i++)
        {
            Vector2 a = poly2D[i];
            Vector2 b = poly2D[(i + 1) % poly2D.Length];
            if ((a.y > pt.y) != (b.y > pt.y))
            {
                float x = (b.x - a.x) * (pt.y - a.y) / (b.y - a.y) + a.x;
                if (pt.x < x) count++;
            }
        }
        return (count % 2) == 1;
    }

    IEnumerator PerformAnimalAction()
    {
        isPerformingAction = true; controller.reachedDestinations = true;
        var (trigger, duration) = actionSystem.PerformRandomAction();
        yield return StartCoroutine(WaitUntilAnimationStarts(trigger));
        yield return new WaitForSeconds(duration);
        controller.reachedDestinations = false; SetNewDestination(); isPerformingAction = false;
    }

    IEnumerator WaitUntilAnimationStarts(string stateName)
    {
        float timer = 0f;
        float timeout = 2f; // Prevent infinite loop in case animation never starts

        while (!animator.GetCurrentAnimatorStateInfo(0).IsName(stateName))
        {
            if (timer > timeout)
            {
                Debug.LogWarning("Animation " + stateName + " did not start within timeout.");
                yield break;
            }

            timer += Time.deltaTime;
            yield return null;
        }
    }


    void OnDrawGizmos()
    {
        if (controller != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, controller.destination);
            Gizmos.DrawSphere(controller.destination, 0.3f);
        }
    }

    public void InitializeAreaManually()
    {
        InitializeNavigationArea();
    }

}
