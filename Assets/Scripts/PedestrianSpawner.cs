using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PedestrianSpawner : MonoBehaviour
{
    public GameObject PedestrianPrefab;
    public int numberOfPedestrians = 10;
    public float spawnDelay = 0.2f; // Optional delay between spawns

    private List<GameObject> pedestrianPool = new List<GameObject>();

    private void Start()
    {
        InitializePool();
        StartCoroutine(Spawn());
    }

    void InitializePool()
    {
        for (int i = 0; i < numberOfPedestrians; i++)
        {
            GameObject obj = Instantiate(PedestrianPrefab);
            obj.SetActive(false);
            pedestrianPool.Add(obj);
        }
    }

    IEnumerator Spawn()
    {
        if (transform.childCount == 0)
        {
            Debug.LogError("Spawner has no waypoints! Disabling.");
            enabled = false; // Disable instead of continuing
            yield break;
        }

        foreach (GameObject ped in pedestrianPool)
        {
            if (transform.childCount > 0)
            {
                // Get random waypoint
                Transform child = transform.GetChild(Random.Range(0, transform.childCount));
                Waypoint waypoint = child.GetComponent<Waypoint>();

                // Configure pedestrian
                ped.GetComponent<WaypointNavigator>().currentWaypoint = waypoint;

                Vector3 spawnPos = child.position + new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f));
                if (Physics.Raycast(spawnPos + Vector3.up * 10f, Vector3.down, out RaycastHit hit, 12f))
                {
                    spawnPos.y = hit.point.y;
                }
                ped.transform.position = spawnPos;

                ped.SetActive(true);

                // Optional delay
                if (spawnDelay > 0)
                    yield return new WaitForSeconds(spawnDelay);
                else
                    yield return null;
            }
            else
            {
                Debug.LogError("No waypoints found in spawner!");
                yield break;
            }
        }
    }

    // Optional: For resetting pedestrians
    public void ResetPool()
    {
        foreach (GameObject ped in pedestrianPool)
        {
            ped.SetActive(false);
            ped.transform.position = Vector3.zero;
        }
        StartCoroutine(Spawn());
    }

#if UNITY_EDITOR
    [ContextMenu("Reset All Pedestrians")]
    void ResetPedestrians()
    {
        foreach (GameObject ped in pedestrianPool)
        {
            ped.transform.position = transform.GetChild(Random.Range(0, transform.childCount)).position;
        }
    }
#endif
}