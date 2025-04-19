using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimalSpawner : MonoBehaviour
{
    [Header("Spawner Settings")]
    public List<AnimalSpawnSettings> animalSettingsList;

    private float[] cumulativeArea;
    private float totalArea;

    private List<GameObject> spawnedAnimals = new List<GameObject>();

    void Start()
    {
        if (animalSettingsList == null || animalSettingsList.Count == 0)
        {
            Debug.LogError("AnimalSpawner: No animal settings defined.");
            return;
        }

        foreach (var animalSettings in animalSettingsList)
        {
            if (animalSettings.spawnArea == null || animalSettings.animalPrefab == null)
            {
                Debug.LogError("AnimalSpawner: Invalid spawn area or animal prefab.");
                continue;
            }

            InitTriangles(animalSettings);

            if (animalSettings.triangles == null || animalSettings.triangles.Count == 0 || animalSettings.totalArea <= 0f)
            {
                Debug.LogError($"AnimalSpawner: Failed to triangulate polygon for {animalSettings.animalPrefab.name}. Check ClosedArea points.");
                continue;
            }

            Debug.Log($"[AnimalSpawner] Triangulated {animalSettings.triangles.Count} triangles for {animalSettings.animalPrefab.name}, total area: {animalSettings.totalArea}");
            StartCoroutine(SpawnAnimals(animalSettings));
        }
    }

    void InitTriangles(AnimalSpawnSettings animalSettings)
    {
        Transform[] pts = animalSettings.spawnArea.GetAreaPoints();
        Vector3[] verts = new Vector3[pts.Length];
        Vector2[] verts2D = new Vector2[pts.Length];

        for (int i = 0; i < pts.Length; i++)
        {
            verts[i] = pts[i].position;
            verts2D[i] = new Vector2(verts[i].x, verts[i].z);
        }

        animalSettings.triangles = new List<Triangle>();
        List<int> indices = new List<int>();
        for (int i = 0; i < verts2D.Length; i++) indices.Add(i);

        int safe = 0;
        while (indices.Count > 3 && safe++ < 500)
        {
            bool clipped = false;
            for (int i = 0; i < indices.Count; i++)
            {
                int iPrev = indices[(i - 1 + indices.Count) % indices.Count];
                int iCurr = indices[i];
                int iNext = indices[(i + 1) % indices.Count];

                Vector2 A = verts2D[iPrev];
                Vector2 B = verts2D[iCurr];
                Vector2 C = verts2D[iNext];

                if (Vector2.SignedAngle(B - A, C - B) <= 0) continue;

                bool hasPointInside = false;
                foreach (int j in indices)
                {
                    if (j == iPrev || j == iCurr || j == iNext) continue;
                    if (PointInTriangle(verts2D[j], A, B, C)) { hasPointInside = true; break; }
                }
                if (hasPointInside) continue;

                animalSettings.triangles.Add(new Triangle(verts[iPrev], verts[iCurr], verts[iNext]));
                indices.RemoveAt(i);
                clipped = true;
                break;
            }
            if (!clipped) break;
        }

        if (indices.Count == 3)
        {
            animalSettings.triangles.Add(new Triangle(verts[indices[0]], verts[indices[1]], verts[indices[2]]));
        }

        animalSettings.cumulativeArea = new float[animalSettings.triangles.Count];
        animalSettings.totalArea = 0f;
        for (int i = 0; i < animalSettings.triangles.Count; i++)
        {
            animalSettings.totalArea += animalSettings.triangles[i].area;
            animalSettings.cumulativeArea[i] = animalSettings.totalArea;
        }
    }

    IEnumerator SpawnAnimals(AnimalSpawnSettings animalSettings)
    {
        for (int i = 0; i < animalSettings.maxAnimals; i++)
        {
            Vector3 pos = GetRandomPoint(animalSettings);
            Debug.Log($"Spawning {animalSettings.animalPrefab.name} at: {pos}");
            GameObject animal = Instantiate(animalSettings.animalPrefab, pos, Quaternion.identity);

            AnimalNavigator nav = animal.GetComponent<AnimalNavigator>();
            if (nav != null)
            {
                nav.roamingArea = animalSettings.spawnArea;
                var method = typeof(AnimalNavigator).GetMethod("InitializeNavigationArea", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                method?.Invoke(nav, null);
            }

            spawnedAnimals.Add(animal);
            yield return new WaitForSeconds(animalSettings.spawnInterval);
        }
    }

    Vector3 GetRandomPoint(AnimalSpawnSettings animalSettings)
    {
        if (animalSettings.totalArea <= 0f || animalSettings.triangles.Count == 0) return Vector3.zero;
        float r = Random.value * animalSettings.totalArea;
        int index = System.Array.FindIndex(animalSettings.cumulativeArea, cum => cum >= r);
        Triangle t = animalSettings.triangles[index];
        float u = Random.value;
        float v = Random.value * (1 - u);
        Vector3 p = t.a * u + t.b * v + t.c * (1 - u - v);
        p.y = animalSettings.fixedY;
        return p;
    }

    bool PointInTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
    {
        float area = Mathf.Abs((b.x - a.x) * (c.y - a.y) - (c.x - a.x) * (b.y - a.y));
        float a1 = Mathf.Abs((a.x - p.x) * (b.y - p.y) - (b.x - p.x) * (a.y - p.y));
        float a2 = Mathf.Abs((b.x - p.x) * (c.y - p.y) - (c.x - p.x) * (b.y - p.y));
        float a3 = Mathf.Abs((c.x - p.x) * (a.y - p.y) - (a.x - p.x) * (c.y - p.y));
        return Mathf.Approximately(area, a1 + a2 + a3);
    }

    [System.Serializable]
    public class AnimalSpawnSettings
    {
        public ClosedArea spawnArea;
        public GameObject animalPrefab;
        public int maxAnimals = 100;
        public float fixedY = 0f;
        public float spawnInterval = 0.01f;

        [HideInInspector] public List<Triangle> triangles;
        [HideInInspector] public float[] cumulativeArea;
        [HideInInspector] public float totalArea;
    }
}
